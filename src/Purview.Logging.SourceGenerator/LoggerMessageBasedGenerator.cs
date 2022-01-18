using System;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Logging.SourceGenerator.Emitters;

namespace Purview.Logging.SourceGenerator;

[Generator]
sealed class LoggerMessageBasedGenerator : ISourceGenerator
{
	string _defaultLevel = Helpers.DefaultLogLevel;

	public void Initialize(GeneratorInitializationContext context)
	{
		context.RegisterForPostInitialization(context => context.AddSource("LogAttributes.g.cs", Helpers.AttributeDefinitions));
		context.RegisterForSyntaxNotifications(() => new LoggerMessageSyntaxReceiver());
	}

	public void Execute(GeneratorExecutionContext context)
	{
		var receiver = context.SyntaxReceiver as LoggerMessageSyntaxReceiver;
		if ((receiver?.CandidateInterfaces?.Count() ?? 0) == 0)
		{
			// nothing to do yet
			return;
		}

		var defaultLogLevelAttribute = context.Compilation.Assembly
			.GetAttributes()
			.FirstOrDefault(attributeData =>
			{
				var name = attributeData.AttributeClass?.ToString();
				return name == $"{Helpers.MSLoggingNamespace}.{Helpers.PurviewDefaultLogEventSettingsAttributeName}"
					|| name == $"{Helpers.MSLoggingNamespace}.{Helpers.PurviewDefaultLogEventSettingsAttributeNameWithSuffix}";
			});

		var generateAddLogDIMethod = true;
		if (defaultLogLevelAttribute != null)
		{
			(string defaultLogLevel, bool includeDI) = GetDefaultLogEventValues(defaultLogLevelAttribute, context.CancellationToken);

			_defaultLevel = defaultLogLevel;
			generateAddLogDIMethod = includeDI;
		}

		foreach (var interfaceDeclaration in receiver!.CandidateInterfaces!)
		{
			// Default is internal, so only check to see if public
			// is on the interface.
			var isInterfacePublic = interfaceDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword);

			var source = GenerateSource(interfaceDeclaration, context);

			// Using a hash for the filename as there was some issues
			// with the length of filename when using ns + interface
			// (not file-system depth, but filename length).
			var hash = GenerateHash($"{source.@namespace}.{source.interfaceName}");
			var filename = $"{source.interfaceName}_{hash}";

			context.AddSource($"{filename}.g.cs", source.source);

			if (!generateAddLogDIMethod)
				continue;
			
			DependencyInjectionMethodEmitter dependencyInjectionMethod = new(source.interfaceName, source.className, source.@namespace, isInterfacePublic);

			var diSource = dependencyInjectionMethod.Generate();

			context.AddSource($"{filename}Extensions.g.cs", diSource);
		}

		static string GenerateHash(string inputString)
		{
			var hash = inputString.GetHashCode() % 10000;
			return hash.ToString("000000000000", System.Globalization.CultureInfo.InvariantCulture);
		}
	}

	(string source, string path, string interfaceName, string className, string? @namespace) GenerateSource(InterfaceDeclarationSyntax interfaceDeclaration, GeneratorExecutionContext context, CancellationToken cancellationToken = default)
	{
		(string defaultLogLevel, bool _) = GetDefaultLevel(interfaceDeclaration, context, cancellationToken);

		// We're disabling CA1812 here - https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1812
		// which warns us about non-instantiated classes. It's used via DI.

		StringBuilder builder = new();
		builder
			.AppendLine("#nullable disable")
			.AppendLine("#pragma warning disable CS8625")
			.AppendLine("#pragma warning disable CA1812")
			.AppendLine();

		// Add a namespace.
		var (hasNamespace, isFileScoped, @namespace) = Helpers.GetNamespaceFrom(interfaceDeclaration);
		if (hasNamespace)
		{
			builder
				.Append("namespace ")
				.Append(@namespace);

			if (isFileScoped)
			{
				// If the source is using a file-scoped namespace, generate that.
				builder
					.AppendLine(";")
					.AppendLine();
			}
			else
			{
				// If the source is using a non-file-scoped namespace, generate that instead.
				builder
					.AppendLine()
					.AppendLine("{");
			}
		}

		// Add the class declaration.
		var classDefinitionName = GetClassName(interfaceDeclaration);
		var loggerInterfaceName = interfaceDeclaration.Identifier.ValueText;

		var namespacePrefix = hasNamespace
			? $"{@namespace}."
			: null;

		// Append the debugger step through to prevent the debugger from stepping into the code...
		// it's largely irrevent to any debugging session anyway...then
		// ...append an internal class definition that implements the interface, and
		// a field of ILogger<T> where T is the interface type.
		builder
			.AppendLine("[System.Diagnostics.DebuggerStepThroughAttribute]")
			.AppendLine("[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]")
			.Append("sealed partial class ")
			.Append(classDefinitionName)
			.Append(" : ")
			.Append(namespacePrefix)
			.AppendLine(loggerInterfaceName)
			.AppendLine("{")
			.Append("readonly ")
			.Append(Helpers.MSLoggingILoggerNamespaceAndTypeName)
			.Append('<')
			.Append(namespacePrefix)
			.Append(loggerInterfaceName)
			.AppendLine("> _logger;")
			.AppendLine();

		// Generate the constructor that takes the ILogger<T> and
		// sets the field.
		builder
			.Append("public ")
			.Append(classDefinitionName)
			.Append('(')
			.Append(Helpers.MSLoggingILoggerNamespaceAndTypeName)
			.Append('<')
			.Append(namespacePrefix)
			.Append(loggerInterfaceName)
			.AppendLine("> logger)")
			.AppendLine("{")
			.AppendLine("_logger = logger;")
			.AppendLine("}")
			.AppendLine();

		// ...implement the interface!
		var memberIndex = 0;
		var generatedLogEventMethod = false;
		//var isNullable = false;
		foreach (var memberSyntax in interfaceDeclaration.Members)
		{
			memberIndex++;
			if (memberSyntax is PropertyDeclarationSyntax propertyDeclaration)
			{
				// WARNING
				ReportHelpers.ReportPropertyExistsOnLoggerInterface(propertyDeclaration, context);

				// We implement it anyway, but only because we raise a warning and
				// we don't want to stop compilation/ generation.
				builder
					.Append("public ")
					.Append(propertyDeclaration.Type)
					.Append(' ')
					.Append(propertyDeclaration.Identifier)
					.AppendLine(" { get; set; }")
					.AppendLine();

				continue;
			}

			if (memberSyntax is MethodDeclarationSyntax method)
			{
				LogMethodEmitter emitter = new(method, context, memberIndex, defaultLogLevel);
				var methodImplementation = emitter.Generate(cancellationToken);
				if (methodImplementation.source == null)
					continue;

				generatedLogEventMethod = true;

				builder.AppendLine(methodImplementation.source);
				//if (methodImplementation.isNullable)
				//	isNullable = true;
			}
		}

		if (!generatedLogEventMethod)
		{
			ReportHelpers.ReportNoLogEventsGenerated(context, interfaceDeclaration);
		}

		// End class.
		builder.AppendLine("}");

		// End namespace, if it's not file-scoped.
		if (hasNamespace && !isFileScoped)
			builder.AppendLine("}");

		builder
			.AppendLine()
			.AppendLine("#pragma warning restore CA1812")
			.AppendLine("#pragma warning restore CS8625")
			.AppendLine("#nullable restore");

		//if (isNullable)
		//{
		//	// It's too unreliable to determine if nullable is enabled or not.
		//	// The 'Define' exception is nullable, but there is no way to tell if it's available or not.

		//	//builder.Insert(0, $"#nullable enable{Environment.NewLine}");
		//	//builder.AppendLine("#nullable restore");
		//}

		// Add a final blank line.
		builder.AppendLine();

		return (source: builder.ToString(), path: namespacePrefix + classDefinitionName, interfaceName: loggerInterfaceName, className: classDefinitionName, @namespace: namespacePrefix);
	}

	(string defaultLogLevel, bool includeDI) GetDefaultLevel(InterfaceDeclarationSyntax interfaceSyntax, GeneratorExecutionContext context, CancellationToken cancellationToken)
	{
		var defaultLogEventAttribute = Helpers.GetAttributeSymbol(Helpers.PurviewDefaultLogEventSettingsAttributeNameWithSuffix, context, cancellationToken);
		if (defaultLogEventAttribute == null)
		{
			// Attribute not defined.
			return (_defaultLevel, true);
		}

		var model = context.Compilation.GetSemanticModel(interfaceSyntax.SyntaxTree);
		var declaredSymbol = model.GetDeclaredSymbol(interfaceSyntax, cancellationToken: cancellationToken);
		if (declaredSymbol == null)
		{
			// This doesn't sound good...
			return (_defaultLevel, true);
		}

		var attribute = declaredSymbol.GetAttributes().SingleOrDefault(m =>
			m.AttributeClass?.Name == Helpers.PurviewDefaultLogEventSettingsAttributeName ||
			m.AttributeClass?.Name == Helpers.PurviewDefaultLogEventSettingsAttributeNameWithSuffix);

		if (attribute == null)
			return (_defaultLevel, true);

		return GetDefaultLogEventValues(attribute, cancellationToken);
	}

	(string defaultLogLevel, bool includeDI) GetDefaultLogEventValues(AttributeData attribute, CancellationToken cancellationToken)
	{
		if (attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is not AttributeSyntax attributeSyntax)
			return (_defaultLevel, true);

		if (attributeSyntax.ArgumentList?.Arguments.Count == 0)
			return (_defaultLevel, true);

		var args = attributeSyntax.ArgumentList!.Arguments;
		var defaultLevel = _defaultLevel;
		var includeDI = true;
		foreach (var arg in args)
		{
			var argName = arg.NameEquals!.Name.Identifier.ValueText;
			var value = arg.Expression.NormalizeWhitespace().ToFullString();
			if (argName == "DefaultLevel")
			{
				if (Helpers.ValidLogLevels.Contains(value))
					defaultLevel = value;
			}
			else if (argName == "GenerateAddLogDIMethod")
			{
				if (bool.TryParse(value, out var generateAddLogDIMethod))
					includeDI = generateAddLogDIMethod;
			}
		}

		return (defaultLevel, includeDI);
	}

	static string GetClassName(InterfaceDeclarationSyntax interfaceDeclaration)
	{
		var className = interfaceDeclaration.Identifier.ValueText;

		if (className[0] == 'I')
			className = className.Substring(1);

		return $"{className}Core";
	}
}

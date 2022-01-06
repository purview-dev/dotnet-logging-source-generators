using System.Text;
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
		context.RegisterForPostInitialization(context => context.AddSource("_LoggerGenAttributes.cs", Helpers.AttributeDefinitions));
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

		// Attempt to find a default log level defined in the assembly.
		var defaultLogLevelAttribute = context.Compilation.Assembly
			.GetAttributes()
			.FirstOrDefault(attributeData =>
			{
				var name = attributeData.AttributeClass?.ToString();
				return name == $"{Helpers.MSLoggingNamespace}.{Helpers.PurviewDefaultLogLevelAttributeName}"
					|| name == $"{Helpers.MSLoggingNamespace}.{Helpers.PurviewDefaultLogLevelAttributeNameWithSuffix}";
			});

		if (defaultLogLevelAttribute is not null)
		{
			var defaultLevelArg = defaultLogLevelAttribute.ConstructorArguments.FirstOrDefault().Value as int?;
			if (defaultLevelArg.HasValue && Helpers.LogLevelValuesToNames.ContainsKey(defaultLevelArg.Value))
				_defaultLevel = Helpers.LogLevelValuesToNames[defaultLevelArg.Value];
		}

		foreach (var interfaceDeclaration in receiver!.CandidateInterfaces!)
		{
			// Default is internal, so only check to see if public
			// is on the interface.
			var isInterfacePublic = interfaceDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword);

			var source = GenerateSource(interfaceDeclaration, context);
			// Using a guid for the filename as there was some issues
			// with filename length (not file-system depth).
			var filename = $"{Guid.NewGuid()}".Replace("-", string.Empty);

			context.AddSource($"{filename}.gen.cs", source.source);

			DependencyInjectionMethodEmitter dependencyInjectionMethod = new(source.interfaceName, source.className, source.@namespace, isInterfacePublic);

			var diSource = dependencyInjectionMethod.Generate();

			context.AddSource($"{filename}Extensions.gen.cs", diSource);
		}
	}

	(string source, string path, string interfaceName, string className, string? @namespace) GenerateSource(InterfaceDeclarationSyntax interfaceDeclaration, GeneratorExecutionContext context, CancellationToken cancellationToken = default)
	{
		var defaultLevel = GetDefaultLevel(interfaceDeclaration, context, cancellationToken);

		// We're disabling CA1812 here - https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1812
		// which warns us about non-instantiated classes. It's used via DI.

		StringBuilder builder = new();
		builder
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

		// Append an internal class definition that implements the interface, and
		// a field of ILogger<T> where T is the interface type.
		builder
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
				LogMethodEmitter emitter = new(method, context, memberIndex, defaultLevel);
				var methodImplementation = emitter.Generate(cancellationToken);
				if (methodImplementation == null)
					continue;

				generatedLogEventMethod = true;

				builder.AppendLine(methodImplementation);
			}
		}

		if (!generatedLogEventMethod)
		{
			ReportHelpers.ReportNoLogEventsGenerated(context, interfaceDeclaration);
		}

		builder.AppendLine("}");

		if (hasNamespace && !isFileScoped)
			builder.AppendLine("}");

		builder
			.AppendLine()
			.AppendLine("#pragma warning restore CA1812");

		// Add a final blank line.
		builder.AppendLine();

		return (source: builder.ToString(), path: namespacePrefix + classDefinitionName, interfaceName: loggerInterfaceName, className: classDefinitionName, @namespace: namespacePrefix);
	}

	string GetDefaultLevel(InterfaceDeclarationSyntax interfaceDeclaration, GeneratorExecutionContext context, CancellationToken cancellationToken = default)
	{
		var defaultLogEventAttribute = Helpers.GetAttributeSymbol(Helpers.PurviewDefaultLogLevelAttributeNameWithSuffix, context, cancellationToken);
		if (defaultLogEventAttribute == null)
		{
			// Attribute not defined.
			return _defaultLevel;
		}

		var model = context.Compilation.GetSemanticModel(interfaceDeclaration.SyntaxTree);
		var declaredSymbol = model.GetDeclaredSymbol(interfaceDeclaration, cancellationToken: cancellationToken);
		if (declaredSymbol == null)
		{
			// This doesn't sound good...
			return _defaultLevel;
		}

		var attribute = declaredSymbol.GetAttributes().SingleOrDefault(m =>
			m.AttributeClass?.Name == Helpers.PurviewDefaultLogLevelAttributeName ||
			m.AttributeClass?.Name == Helpers.PurviewDefaultLogLevelAttributeNameWithSuffix);

		if (attribute == null)
			return _defaultLevel;

		if (attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is not AttributeSyntax attributeSyntax)
			return _defaultLevel;

		if (attributeSyntax.ArgumentList?.Arguments.Count == 0)
			return _defaultLevel;

		var args = attributeSyntax.ArgumentList!.Arguments;
		foreach (var arg in args)
		{
			var value = model.GetConstantValue(arg.Expression, cancellationToken);
			if (!value.HasValue)
				continue;

			if (value.Value is int id && Helpers.LogLevelValuesToNames.ContainsKey(id))
				return Helpers.LogLevelValuesToNames[id];
		}

		return _defaultLevel;
	}

	static string GetClassName(InterfaceDeclarationSyntax interfaceDeclaration)
	{
		var className = interfaceDeclaration.Identifier.ValueText;

		if (className[0] == 'I')
			className = className.Substring(1);

		return $"{className}Core";
	}
}

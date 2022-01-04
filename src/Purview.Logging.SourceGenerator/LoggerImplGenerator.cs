using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Purview.Logging.SourceGenerator;

namespace Logger.Gen;

[Generator]
partial class LoggerImplGenerator : ISourceGenerator
{
	public const bool STORE_DEBUG_DATA = true;

	public void Initialize(GeneratorInitializationContext context)
		=> context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

	public void Execute(GeneratorExecutionContext context)
	{
		IncludeAttributes(context);

		var receiver = context.SyntaxReceiver as SyntaxReceiver;
		if ((receiver?.CandidateInterfaces?.Count ?? 0) == 0)
		{
			// nothing to do yet
			return;
		}

		foreach (var interfaceDeclaration in receiver!.CandidateInterfaces!)
		{
			// Default is internal, so only check to see if public
			// is on the interface.
			var isInterfacePublic = interfaceDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword);

			var source = GenerateSource(interfaceDeclaration, context);

			context.AddSource($"{source.path}.gen.cs", source.source);

			DependencyInjectionMethodEmitter dependencyInjectionMethod = new(source.interfaceName, source.className, source.@namespace, isInterfacePublic);

			var diSource = dependencyInjectionMethod.Generate();

			context.AddSource($"{source.path}.di.gen.cs", diSource);
		}
	}

	static void IncludeAttributes(GeneratorExecutionContext context)
		=> context.AddSource("_LoggerGenAttributes.cs", Helpers.AttributeDefinitions);

	static (string source, string path, string interfaceName, string className, string? @namespace) GenerateSource(InterfaceDeclarationSyntax interfaceDeclaration, GeneratorExecutionContext context, CancellationToken cancellationToken = default)
	{
		var defaultLevel = GetDefaultLevel(interfaceDeclaration, context, cancellationToken);

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
				builder
					.AppendLine(";")
					.AppendLine();
			}
			else
			{
				builder
					.AppendLine()
					.AppendLine("{");
			}
		}

		// Add the class declaration.
		var className = GetClassName(interfaceDeclaration);
		var interfaceName = interfaceDeclaration.Identifier.ValueText;

		var namespacePrefix = hasNamespace
			? $"{@namespace}."
			: null;

		builder
			.Append("sealed partial class ")
			.Append(className)
			.Append(" : ")
			.Append(namespacePrefix)
			.AppendLine(interfaceName)
			.AppendLine("{")
			.Append("readonly ")
			.Append(Helpers.MSLoggingNamespace)
			.Append(".ILogger<")
			.Append(namespacePrefix)
			.Append(interfaceName)
			.AppendLine("> _logger;")
			.AppendLine();

		builder
			.Append("public ")
			.Append(className)
			.Append('(')
			.Append(Helpers.MSLoggingNamespace)
			.Append(".ILogger<")
			.Append(namespacePrefix)
			.Append(interfaceName)
			.AppendLine("> logger)")
			.AppendLine("{")
			.AppendLine("_logger = logger;")
			.AppendLine("}")
			.AppendLine();

		var methodInfo = "";
		var index = 0;
		foreach (var memberSyntax in interfaceDeclaration.Members)
		{
			index++;
			if (memberSyntax is PropertyDeclarationSyntax propertyDeclaration)
			{
				// WARNING
				ReportProperty(propertyDeclaration, context);
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
				LogMethodEmitter emitter = new(method, context, index, defaultLevel);
				var genResult = emitter.Generate(cancellationToken);

				if (genResult != null)
					methodInfo += genResult + "\n";

				builder.AppendLine(genResult);
			}
		}

		if (methodInfo.Length > 0)
			context.AddSource($"{className}.methodInfo.cs", "/*\n" + methodInfo + "\n*/");

		builder.AppendLine("}");

		if (hasNamespace && !isFileScoped)
			builder.AppendLine("}");

		builder
			.AppendLine()
			.AppendLine("#pragma warning restore CA1812");

		// Add final blank line.
		builder.AppendLine();

		return (source: builder.ToString(), path: namespacePrefix + className, interfaceName, className, namespacePrefix);
	}

	static string GetDefaultLevel(InterfaceDeclarationSyntax interfaceDeclaration, GeneratorExecutionContext context, CancellationToken cancellationToken = default)
	{
		var options = (context.Compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;
		var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(Helpers.AttributeDefinitions, Encoding.UTF8), options, cancellationToken: cancellationToken));

		var defaultLogEventAttribute = compilation.GetTypeByMetadataName($"{Helpers.MSLoggingNamespace}.{Helpers.PurviewDefaultLogLevelAttributeNameWithSuffix}");
		if (defaultLogEventAttribute == null)
		{
			// Attribute not defined.
			return Helpers.DefaultLogLevel;
		}

		var model = context.Compilation.GetSemanticModel(interfaceDeclaration.SyntaxTree);
		var declaredSymbol = model.GetDeclaredSymbol(interfaceDeclaration, cancellationToken: cancellationToken);
		if (declaredSymbol == null)
		{
			// This doesn't sound good...
			return Helpers.DefaultLogLevel;
		}

		var attribute = declaredSymbol.GetAttributes().SingleOrDefault(m =>
			m.AttributeClass?.Name == Helpers.PurviewDefaultLogLevelAttributeName ||
			m.AttributeClass?.Name == Helpers.PurviewDefaultLogLevelAttributeNameWithSuffix);

		if (attribute == null)
			return Helpers.DefaultLogLevel;

		if (attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is not AttributeSyntax attributeSyntax)
			return Helpers.DefaultLogLevel;

		if (attributeSyntax.ArgumentList?.Arguments.Count == 0)
			return Helpers.DefaultLogLevel;

		var args = attributeSyntax.ArgumentList!.Arguments;
		foreach (var arg in args)
		{
			var value = model.GetConstantValue(arg.Expression, cancellationToken);
			if (!value.HasValue)
				continue;

			if (value.Value is int id && Helpers.LogLevelValuesToNames.ContainsKey(id))
				return Helpers.LogLevelValuesToNames[id];
		}

		// At this point, we haven't found one... can we find one at the attribute level?
		// TODO: Read assembly...

		return Helpers.DefaultLogLevel;
	}

	static string GetClassName(InterfaceDeclarationSyntax interfaceDeclaration)
	{
		var className = interfaceDeclaration.Identifier.ValueText;

		if (className[0] == 'I')
			className = className.Substring(1);

		return $"{className}Core";
	}

	static void ReportProperty(PropertyDeclarationSyntax propertyDeclaration, GeneratorExecutionContext context)
	{
		context.ReportDiagnostic(Diagnostic.Create(
			new DiagnosticDescriptor(
				"PVL-0003",
				"Properties are not valid on logger interfaces.",
				"Properties are not supported on logger interfaces, please remove the {0} property.",
				"Purview.Logging",
				DiagnosticSeverity.Error,
				true),
			propertyDeclaration.GetLocation(),
			messageArgs: new object[] { propertyDeclaration.Identifier })
		);
	}

	sealed private class SyntaxReceiver : ISyntaxReceiver
	{
		readonly static string[] _suffixes = new[] { "Log", "Logs", "Logger" };

		List<InterfaceDeclarationSyntax>? _candidateInterfaces;

		public List<InterfaceDeclarationSyntax>? CandidateInterfaces => _candidateInterfaces;

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			// Only classes
			if (syntaxNode is InterfaceDeclarationSyntax interfaceDeclaration)
			{
				if (!_suffixes.Any(a => interfaceDeclaration.Identifier.ValueText.EndsWith(a, StringComparison.Ordinal)))
					return;

				// Match add to candidates
				_candidateInterfaces ??= new List<InterfaceDeclarationSyntax>();
				_candidateInterfaces.Add(interfaceDeclaration);
			}
		}
	}
}

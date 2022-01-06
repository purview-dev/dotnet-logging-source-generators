using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Logging.SourceGenerator;

static class Helpers
{
	public const string MSLoggingNamespace = "Microsoft.Extensions.Logging";

	public const string MSLoggingLogLevelTypeName = "LogLevel";
	public const string MSLoggingLogLevelNamespaceAndTypeName = $"{MSLoggingNamespace}.{MSLoggingLogLevelTypeName}";

	public const string MSLoggingILoggerTypeName = "ILogger";
	public const string MSLoggingILoggerNamespaceAndTypeName = $"{MSLoggingNamespace}.{MSLoggingILoggerTypeName}";

	public const string PurviewLoggingNamespace = "Purview.Logging.SourceGenerator";

	public const string PurviewDefaultLogLevelAttributeName = "DefaultLogLevel";
	public const string PurviewDefaultLogLevelAttributeNameWithSuffix = $"{PurviewDefaultLogLevelAttributeName}Attribute";

	public const string PurviewLogEventAttributeName = "LogEvent";
	public const string PurviewLogEventAttributeNameWithSuffix = $"{PurviewLogEventAttributeName}Attribute";

	public const int MaximumLoggerDefineParameters = 6;

	public const string DefaultLogLevel = "Information";

	readonly static public string IDisposableType = typeof(IDisposable).FullName;

	static public string[] ValidLogLevels => LogLevelValuesToNames.Values.ToArray();

	readonly static public Dictionary<int, string> LogLevelValuesToNames = new() {
		{ 0, "Trace" },
		{ 1, "Debug" },
		{ 2, DefaultLogLevel },
		{ 3, "Warning" },
		{ 4, "Error" },
		{ 5, "Critical" }
	};

	// Attributes are internal to avoid collisions.
	public const string AttributeDefinitions = @$"
using System;

namespace {MSLoggingNamespace}
{{	
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Assembly, AllowMultiple = false)]
	sealed class {PurviewDefaultLogLevelAttributeNameWithSuffix} : Attribute
	{{
		public {PurviewDefaultLogLevelAttributeNameWithSuffix}({MSLoggingLogLevelNamespaceAndTypeName} defaultLevel)
		{{
			DefaultLevel = defaultLevel;
		}}

		public {MSLoggingLogLevelNamespaceAndTypeName} DefaultLevel {{ get; }}
	}}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	sealed class {PurviewLogEventAttributeNameWithSuffix} : Attribute
	{{
		public int Id {{ get; set; }}

		public string? Name {{ get; set; }}

		public {MSLoggingLogLevelNamespaceAndTypeName} Level {{ get; set; }} = {MSLoggingLogLevelNamespaceAndTypeName}.None;

		public string? Message {{ get; set; }}
	}}
}}
";

	static public INamedTypeSymbol? GetAttributeSymbol(string attributeTypeName, GeneratorExecutionContext context, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// If we include the attributes in all referenced assemblies, we don't need to dyanmically parse and add...
		return context.Compilation.GetTypeByMetadataName($"{MSLoggingNamespace}.{attributeTypeName}");

		//var options = (context.Compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;
		//var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(AttributeDefinitions, Encoding.UTF8), options, cancellationToken: cancellationToken));

		//return compilation.GetTypeByMetadataName($"{MSLoggingNamespace}.{attributeTypeName}");
	}

	static public (bool hasNamespace, bool isFileScoped, string? @namespace) GetNamespaceFrom(SyntaxNode syntaxNode)
	{
		SyntaxNode? tempCurCls = syntaxNode;
		var tempFullName = new Stack<string>();

		var isFileScoped = false;
		do
		{
			if (tempCurCls.IsKind(SyntaxKind.ClassDeclaration))
			{
				tempFullName.Push(((ClassDeclarationSyntax)tempCurCls).Identifier.ToString());
			}
			else if (tempCurCls.IsKind(SyntaxKind.NamespaceDeclaration) || tempCurCls.IsKind(SyntaxKind.FileScopedNamespaceDeclaration))
			{
				if (tempCurCls.IsKind(SyntaxKind.FileScopedNamespaceDeclaration))
					isFileScoped = true;

				tempFullName.Push(((BaseNamespaceDeclarationSyntax)tempCurCls).Name.ToString());
			}

			tempCurCls = tempCurCls.Parent;
		} while (tempCurCls != null);

		var @namespace = tempFullName.Count == 0
			? null
			: string.Join(".", tempFullName);

		return (@namespace != null, isFileScoped, @namespace);
	}

	//static int GenerateMaximumLoggerDefineParameters()
	//{
	//	var defineMethodParameterCount = typeof(LoggerMessage)
	//		.GetMethods()
	//		.Where(m => m.Name == nameof(LoggerMessage.Define))
	//		.Select(m => m.GetParameters().Length)
	//		.OrderByDescending(l => l)
	//		.First();

	//	// Subtract the ILogger and the Exception.
	//	return defineMethodParameterCount - 2;
	//}
}

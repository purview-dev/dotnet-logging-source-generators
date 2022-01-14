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
#nullable disable

using System;

namespace {MSLoggingNamespace}
{{	
	/// <summary>
	/// Controls the default <see cref=""{MSLoggingLogLevelTypeName}""/> used when generating log events.
	/// </summary>
	[System.Diagnostics.DebuggerStepThroughAttribute]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Assembly, AllowMultiple = false)]
	sealed class {PurviewDefaultLogLevelAttributeNameWithSuffix} : Attribute
	{{
		/// <summary>
		/// Initializes a new <see cref=""{PurviewDefaultLogLevelAttributeNameWithSuffix}""/>
		/// </summary>
		/// <param name=""defaultLevel"">The default <see cref=""{MSLoggingLogLevelTypeName}""/> to use for generating log events.</param>
		public {PurviewDefaultLogLevelAttributeNameWithSuffix}({MSLoggingLogLevelTypeName} defaultLevel)
		{{
			DefaultLevel = defaultLevel;
		}}

		/// <summary>
		/// The default <see cref=""{MSLoggingLogLevelTypeName}""/> used for generating log events.
		/// </summary>
		public {MSLoggingLogLevelTypeName} DefaultLevel {{ get; }}
	}}

	/// <summary>
	/// Overrides/ configures settings for log event generation.
	/// </summary>
	[System.Diagnostics.DebuggerStepThroughAttribute]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	sealed class {PurviewLogEventAttributeNameWithSuffix} : Attribute
	{{
		/// <summary>
		/// The <see cref=""EventId.Id""/> to use. If no value is specified, the index of the method is
		/// used (as-per the order it is read by Rosyln).
		/// </summary>
		public int Id {{ get; set; }}

		/// <summary>
		/// The name of the <see cref=""EventId.Name""/> to use,
		/// if this value is not set then the method name is used.
		/// </summary>
		public string Name {{ get; set; }}

		/// <summary>
		/// The <see cref=""{MSLoggingLogLevelTypeName}""/> used for generation. If non is specified,
		/// the <see cref=""{PurviewDefaultLogLevelAttributeNameWithSuffix}.DefaultLevel""/> is used.
		/// </summary>
		/// <remarks>
		/// If the log event contains an <see cref=""Exception""/> and
		/// no level is defined, <see cref=""{MSLoggingLogLevelTypeName}.Error""/> is used.
		/// </remarks>
		public {MSLoggingLogLevelTypeName} Level {{ get; set; }} = {MSLoggingLogLevelTypeName}.None;

		/// <summary>
		/// The message template to use, if none is specified the following pattern is used:
		/// {{MethodName}}: [{{ParameterName}}: {{ParameterValue}}, ...]
		/// 
		/// For example:
		/// <code>
		///		Operation1(int operationId, string value)
		/// </code>
		/// 
		/// Would generate:
		/// <code>
		///		""Operation1: operationId: {{OperationId}}, value: {{Value}}""
		/// </code>
		/// </summary>
		public string Message {{ get; set; }}
	}}
}}

#nullable restore
";

	static public INamedTypeSymbol? GetAttributeSymbol(string attributeTypeName, GeneratorExecutionContext context, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// If we include the attributes in all referenced assemblies, we don't need to dynamically parse it...
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
			else if (isFileScopedNamespace(tempCurCls))
			{
#if VS2019
				tempFullName.Push(((NamespaceDeclarationSyntax)tempCurCls).Name.ToString());
#else
				if (tempCurCls.IsKind(SyntaxKind.FileScopedNamespaceDeclaration))
					isFileScoped = true;

				tempFullName.Push(((BaseNamespaceDeclarationSyntax)tempCurCls).Name.ToString());
#endif
			}

			tempCurCls = tempCurCls.Parent;
		} while (tempCurCls != null);

		var @namespace = tempFullName.Count == 0
			? null
			: string.Join(".", tempFullName);

		return (@namespace != null, isFileScoped, @namespace);

		static bool isFileScopedNamespace(SyntaxNode tempCurCls)
		{
			if (tempCurCls.IsKind(SyntaxKind.NamespaceDeclaration))
				return true;

#if VS2019
			return false;
#else
			return tempCurCls.IsKind(SyntaxKind.FileScopedNamespaceDeclaration);
#endif
		}
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

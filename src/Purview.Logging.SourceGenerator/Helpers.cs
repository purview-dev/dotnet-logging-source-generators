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

	public const string PurviewDefaultLogEventSettingsAttributeName = "DefaultLogEventSettings";
	public const string PurviewDefaultLogEventSettingsAttributeNameWithSuffix = $"{PurviewDefaultLogEventSettingsAttributeName}Attribute";

	public const string PurviewLogEventAttributeName = "LogEvent";
	public const string PurviewLogEventAttributeNameWithSuffix = $"{PurviewLogEventAttributeName}Attribute";

	public const int MaximumLoggerDefineParameters = 6;

	readonly static public string IDisposableType = typeof(IDisposable).FullName;

	static public string[] ValidLogLevels => LogLevelValuesToNames
		.Values.Concat(
			LogLevelValuesToNames.Values.Select(m => $"{MSLoggingLogLevelTypeName}.{m}")
		).ToArray();

	readonly static public Dictionary<int, string> LogLevelValuesToNames = new() {
		{ 0, "Trace" },
		{ 1, "Debug" },
		{ 2, LogLevelDefault },
		{ 3, "Warning" },
		{ 4, "Error" },
		{ 5, "Critical" }
	};

	public const string LogLevelDefault = "Information";

	public const string MessageTemplateDefault = "{ContextName}{ContextSeparator}{MethodName}{ContextArgumentSeparator}{ArgumentList}";

	public const bool GenerateAddLogDIMethodDefault = true;

	public const bool IncludeContextInEventNameDefault = true;

	public const string ContextSeparatorDefault = ".";

	public const string ContextArgumentListSeparatorDefault = "> ";

	public const string ArgumentNameValueSerparatorDefault = ": ";

	public const string ArgumentSerparatorDefault = ", ";

	// Default Property Names
	public const string LogLevelPropertyName = "Level";

	public const string GenerateAddLogDIMethodPropertyName = "GenerateAddLogMethod";

	public const string MessageTemplatePropertyName = "MessageTemplate";

	public const string IncludeContextInEventNamePropertyName = "IncludeContextInEventName";

	public const string ContextSeparatorPropertyName = "ContextSeparator";

	public const string ContextArgumentListSeparatorPropertyName = "ContextArgumentListSeparator";

	public const string ArgumentNameValueSerparatorPropertyName = "ArgumentNameValueSerparator";

	public const string ArgumentSerparatorPropertyName = "ArgumentSerparator";

	// Attributes are internal to avoid collisions.
	readonly static public string AttributeDefinitions = @$"
#nullable disable

using System;

namespace {MSLoggingNamespace}
{{	
	/// <summary>
	/// Controls the default <see cref=""{MSLoggingLogLevelTypeName}""/> used when generating log events.
	/// </summary>
	[System.Diagnostics.DebuggerStepThroughAttribute]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
	[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Assembly, AllowMultiple = false)]
	sealed class {PurviewDefaultLogEventSettingsAttributeNameWithSuffix} : Attribute
	{{
		/// <summary>
		/// The default <see cref=""{MSLoggingLogLevelTypeName}""/> used for generating log events.
		/// </summary>
		public {MSLoggingLogLevelTypeName} {LogLevelPropertyName} {{ get; set; }} = {MSLoggingLogLevelTypeName}.{LogLevelDefault};

		/// <summary>
		/// Indicates if the generator should include the .AddLog<T> method required
		/// for use in log implementation registration using the IServiceCollection.
		/// </summary>
		public bool {GenerateAddLogDIMethodPropertyName} {{ get; set; }} = {$"{GenerateAddLogDIMethodDefault}".ToLowerInvariant()};

		/// <summary>
		/// The default message template.
		/// <code>
		/// ContextName == The interface name.
		/// ContextSeperator == Separates the ContextName from the MethodName.
		/// MethodName == The name of the method, as it appears on the interface.
		/// ContextArgumentSeparator == Separates the Arguments from the Context/ Method name.
		/// ArgumentList == The arguments, formatted by {{ArgumentName}}{{ArgumentSerparator}}{{ArgumentValue}}
		/// ArgumentSeparator == separates the arguments.
		/// </code>
		/// </summary>
		public string {MessageTemplatePropertyName} {{ get; set; }} = ""{MessageTemplateDefault}"";
	
		/// <summary>
		/// If true includes the EventId.Name is made from the interface and method name, 
		/// otherwise just the method name is used.
		/// </summary>
		public bool {IncludeContextInEventNamePropertyName} {{ get; set; }} = {$"{IncludeContextInEventNameDefault}".ToLowerInvariant()};

		/// <summary>
		/// The default separator used when the ContextName is included.
		/// </summary>
		public string {ContextSeparatorPropertyName} {{ get; set; }} = ""{ContextSeparatorDefault}"";

		/// <summary>
		/// The default separator used when arguments are included.
		/// </summary>
		public string {ContextArgumentListSeparatorPropertyName} {{ get; set; }} = ""{ContextArgumentListSeparatorDefault}"";

		/// <summary>
		/// The default separator used to separate the argument's name and value.
		/// </summary>
		public string {ArgumentNameValueSerparatorPropertyName} {{ get; set; }} = ""{ArgumentNameValueSerparatorDefault}"";

		/// <summary>
		/// The default separator used to separate multiple arguments.
		/// </summary>
		public string {ArgumentSerparatorPropertyName} {{ get; set; }} = ""{ArgumentSerparatorDefault}"";
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
		/// The <see cref=""{MSLoggingLogLevelTypeName}""/> used for generation. If none is specified,
		/// the <see cref=""{PurviewDefaultLogEventSettingsAttributeNameWithSuffix}.DefaultLevel""/> is used.
		/// </summary>
		/// <remarks>
		/// If the log event contains an <see cref=""Exception""/> and
		/// no level is defined, <see cref=""{MSLoggingLogLevelTypeName}.Error""/> is used.
		/// </remarks>
		public {MSLoggingLogLevelTypeName} {LogLevelPropertyName} {{ get; set; }} = {MSLoggingLogLevelTypeName}.None;

		/// <summary>
		/// The message template to use, if none is specified the following pattern is used:
		/// {MessageTemplateDefault}
		/// 
		/// For example:
		/// <code>
		///		IOperationalServiceLogs.Operation1(int operationId, string value)
		/// </code>
		/// 
		/// Would generate:
		/// <code>
		///		""IOperationalService.Operation1> OperationId: {{OperationId}}, Value: {{Value}}""
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

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Logging.SourceGenerator.Emitters;

sealed partial class LogMethodEmitter
{
	const string _voidReturnType = "void";

	readonly static string _exceptionType = typeof(Exception).FullName;
	readonly string _loggerName;
	readonly MethodDeclarationSyntax _methodDeclaration;
	readonly GeneratorExecutionContext _context;
	readonly int _methodIndex;
	readonly DefaultLoggerSettings _defaultLoggerSettings;

	readonly Lazy<bool> _hasLogOptions;

	public LogMethodEmitter(string loggerName, MethodDeclarationSyntax methodDeclaration, GeneratorExecutionContext context, int methodIndex, DefaultLoggerSettings defaultLoggerSettings)
	{
		_loggerName = loggerName;
		_methodDeclaration = methodDeclaration;
		_context = context;
		_methodIndex = methodIndex;
		_defaultLoggerSettings = defaultLoggerSettings;

		_hasLogOptions = new(HasLogOptionsDefined);
	}

	bool HasLogOptionsDefined()
	{
		return _context.Compilation.GetTypeByMetadataName($"{Helpers.MSLoggingNamespace}.LogDefineOptions") != null;
	}

	public (string? source, bool isNullable) Generate(CancellationToken cancellationToken = default)
	{
		var methodReturnType = GetReturnType();
		if (methodReturnType == MethodReturnType.None)
			return (null, false);

		List<ParameterData> parameterData = new();
		ParameterData? exceptionData = null;
		var parameters = _methodDeclaration.ParameterList.Parameters;
		foreach (var parameter in parameters)
		{
			var paramInfo = CreateParameterData(parameter);
			if (paramInfo == null)
				continue;

			parameterData.Add(paramInfo.Value);

			if (methodReturnType == MethodReturnType.Void && paramInfo.Value.IsException)
			{
				// Should always take the last exception defined. Convention over configuration...
				exceptionData = paramInfo.Value;
			}
		}

		// If there are multiple exceptions defined, should be put up a warning?
		var methodName = _methodDeclaration.Identifier.ToString();
		var paramsWithoutException = parameterData.ToArray();

		// Remove the last exception, if there is one.
		if (exceptionData != null)
		{
			paramsWithoutException = paramsWithoutException.Where(m => m.Name != exceptionData.Value.Name).ToArray();
			if (parameterData.Count == paramsWithoutException.Length)
			{
				_context.ReportUnableToDetermineExceptionParameter(_methodDeclaration.GetLocation(), parameterData.Count, exceptionData.Value.Name);

				return (null, false);
			}
		}

		if (paramsWithoutException.Length > Helpers.MaximumLoggerDefineParameters)
		{
			_context.ReportMaximumNumberOfParmaetersExceeded(_methodDeclaration.GetLocation(), methodName, paramsWithoutException.Length);

			return (null, false);
		}

		var logSettings = LoggerSettingsParser.GetLogSettings(_context, _methodDeclaration, cancellationToken);

		StringBuilder builder = new(Helpers.DefaultStringBuilderCapacity);

		// Build logger message.
		var logActionName = methodName;
		if (char.IsUpper(logActionName[0]))
			logActionName = char.ToLowerInvariant(logActionName[0]) + logActionName.Substring(1);

		logActionName = $"_{logActionName}";

		var methodParams = paramsWithoutException.Select(p => p.Type).ToArray();
		var actionParams = new[] { Helpers.MSLoggingILoggerNamespaceAndTypeName }
			.Concat(methodParams)
			.Concat(new[] { methodReturnType == MethodReturnType.Void ? _exceptionType : Helpers.IDisposableType });
			//.Concat(new[] { methodReturnType == MethodReturnType.Void ? $"{_exceptionType}?" : Helpers.IDisposableType }); // Cause' nullable hell.

		// Define the field name - we'll append the method index, just to avoid any clashes.
		// i.e. LogAThing(int) and LogAThing(string) would create the same field name.
		var loggerMessageFieldName = $"{logActionName}_{_methodIndex}";

		AppendBeginFieldDefinition(methodReturnType, builder, methodParams, actionParams, loggerMessageFieldName);

		// Get the default local log level, if we know we contained an exception,
		// set the default to Error, otherwise use the configured default.
		var localDefault = exceptionData == null
			? _defaultLoggerSettings.LogLevelDefault
			: "Error";

		// If the method has a defined level use it, or use the local default.
		var methodLogLevel = logSettings?.Level ?? localDefault;

		AppendEndFieldDefinition(methodReturnType, methodName, paramsWithoutException, logSettings, builder, methodLogLevel);

		AppendPublicMethodDefinitionFromInterface(methodReturnType, parameterData, methodName, builder);

		AppendMethodBody(methodReturnType, exceptionData, paramsWithoutException, builder, loggerMessageFieldName, methodLogLevel);

		return (builder.ToString(), parameterData.Any(p => p.IsNullable));
	}

	string BuildMessage(string messageTemplate, string methodName, ParameterData[] paramsWithoutException)
	{
		messageTemplate = messageTemplate
			.Replace("{ContextName}", _loggerName)
			.Replace("{ContextSeparator}", _defaultLoggerSettings.ContextSeparator)
			.Replace("{MethodName}", methodName);

		if (paramsWithoutException.Length == 0)
		{
			// No parameters, so replace the separator value with nothing.
			messageTemplate = messageTemplate
				.Replace("{ContextArgumentSeparator}", string.Empty)
				.Replace("{ArgumentList}", string.Empty);
		}
		else
		{
			var argumentList = string.Join(_defaultLoggerSettings.ArgumentSerparator, paramsWithoutException.Select(p =>
			{
				var titledCasedParameterName = p.Name;
				if (char.IsLower(titledCasedParameterName[0]))
					titledCasedParameterName = char.ToUpperInvariant(titledCasedParameterName[0]) + titledCasedParameterName.Substring(1);

				return p.Name + _defaultLoggerSettings.ArgumentNameValueSerparator + "{" + titledCasedParameterName + "}";
			}));

			messageTemplate = messageTemplate
				.Replace("{ContextArgumentSeparator}", _defaultLoggerSettings.ContextArgumentSeparator)
				.Replace("{ArgumentList}", argumentList);
		}

		return messageTemplate;
	}

	MethodReturnType GetReturnType()
	{
		var isVoid = _methodDeclaration.ReturnType is PredefinedTypeSyntax predefinedTypeSyntax
			&& predefinedTypeSyntax.Keyword.ToString() == _voidReturnType;
		if (isVoid)
			return MethodReturnType.Void;

		if (_methodDeclaration.ReturnType.ToString().EndsWith(nameof(IDisposable), StringComparison.Ordinal))
		{
			var semanticModel = _context.Compilation.GetSemanticModel(_methodDeclaration.ReturnType.SyntaxTree);
			var typeInfo = semanticModel.GetTypeInfo(_methodDeclaration.ReturnType);

			var isIDiposable = typeInfo.Type?.ToString() == Helpers.IDisposableType;
			if (isIDiposable)
				return MethodReturnType.Scope;
		}

		_context.ReportInvalidLogMethodReturnType(_methodDeclaration);

		return MethodReturnType.None;
	}

	ParameterData? CreateParameterData(ParameterSyntax parameterSyntax)
	{
		if (parameterSyntax.Type == null)
			return null;

		var parameterTypeSync = parameterSyntax.Type;

		var semanticModel = _context.Compilation.GetSemanticModel(parameterTypeSync.SyntaxTree);
		var typeInfo = semanticModel.GetTypeInfo(parameterTypeSync);

		var parameterType = typeInfo.Type?.ToString();
		var paramterName = parameterSyntax.Identifier.ToString();

		if (parameterType == null)
			return null;

		var isNullable = false;
		if ((typeInfo.Nullability.Annotation == NullableAnnotation.Annotated)
			|| (parameterSyntax.Type is NullableTypeSyntax && typeInfo.Type?.IsReferenceType == true))
		{
			isNullable = true;
		}

		// We're ignoring all things nullable.

		//if (isNullable)
		//	parameterType += "?";

		var isException = IsException(typeInfo.Type);

		return new(paramterName, parameterType, isException, isNullable);
	}

	static bool IsException(ITypeSymbol? t)
	{
		if (t == null)
			return false;

		if (t.ToString() == typeof(Exception).FullName)
			return true;

		return IsException(t.BaseType);
	}

	enum MethodReturnType
	{
		Void,
		Scope,
		None
	}

	readonly record struct ParameterData(string Name, string Type, bool IsException, bool IsNullable);
}

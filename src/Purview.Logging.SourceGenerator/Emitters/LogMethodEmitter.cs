using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Logging.SourceGenerator.Emitters;

sealed partial class LogMethodEmitter
{
	const string _voidReturnType = "void";
	readonly static string _exceptionType = typeof(Exception).FullName;

	readonly MethodDeclarationSyntax _methodDeclaration;
	readonly GeneratorExecutionContext _context;
	readonly int _methodIndex;
	readonly string _defaultLevel;

	readonly Lazy<bool> _hasLogOptions;

	public LogMethodEmitter(MethodDeclarationSyntax methodDeclaration, GeneratorExecutionContext context, int methodIndex, string defaultLevel)
	{
		_methodDeclaration = methodDeclaration;
		_context = context;
		_methodIndex = methodIndex;
		_defaultLevel = defaultLevel;

		_hasLogOptions = new(HasLogOptionsDefined);
	}

	bool HasLogOptionsDefined()
	{
		return _context.Compilation.GetTypeByMetadataName($"{Helpers.MSLoggingNamespace}.LogDefineOptions") != null;
	}

	public string? Generate(CancellationToken cancellationToken = default)
	{
		var methodReturnType = GetReturnType();
		if (methodReturnType == MethodReturnType.None)
			return null;

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

				return null;
			}
		}

		if (paramsWithoutException.Length > Helpers.MaximumLoggerDefineParameters)
		{
			_context.ReportMaximumNumberOfParmaetersExceeded(_methodDeclaration.GetLocation(), methodName, paramsWithoutException.Length);

			return null;
		}

		var logSettings = LoggerSettingsParser.GetLogSettings(_context, _methodDeclaration, cancellationToken);

		StringBuilder builder = new();

		// Build logger message.
		var logActionName = methodName;
		if (char.IsUpper(logActionName[0]))
			logActionName = char.ToLowerInvariant(logActionName[0]) + logActionName.Substring(1);

		logActionName = $"_{logActionName}";

		var methodParams = paramsWithoutException.Select(p => p.Type).ToArray();
		var actionParams = new[] { Helpers.MSLoggingILoggerNamespaceAndTypeName }
			.Concat(methodParams)
			.Concat(new[] { methodReturnType == MethodReturnType.Void ? $"{_exceptionType}?" : Helpers.IDisposableType });

		// Define the field name - we'll append the method index, just to avoid any clashes.
		// i.e. LogAThing(int) and LogAThing(string) would create the same field name.
		var loggerMessageFieldName = $"{logActionName}_{_methodIndex}";

		AppendBeginFieldDefinition(methodReturnType, builder, methodParams, actionParams, loggerMessageFieldName);

		// Get the default local log level, if we know we contained an exception,
		// set the default to Error, otherwise use the configured default.
		var localDefault = exceptionData == null
			? _defaultLevel
			: "Error";

		// If the method has a defined level use it, or use the local default.
		var methodLogLevel = logSettings?.Level ?? localDefault;

		AppendEndFieldDefinition(methodReturnType, methodName, paramsWithoutException, logSettings, builder, methodLogLevel);

		AppendPublicMethodDefinitionFromInterface(methodReturnType, parameterData, methodName, builder);

		AppendMethodBody(methodReturnType, exceptionData, paramsWithoutException, builder, loggerMessageFieldName, methodLogLevel);

		return builder.ToString();
	}

	static string? BuildMessage(string methodName, IEnumerable<ParameterData> paramsWithoutException)
	{
		var message = methodName;
		if (paramsWithoutException.Any())
		{
			message += ": " + string.Join(", ", paramsWithoutException.Select(p =>
			{
				var pName = p.Name;
				if (char.IsLower(pName[0]))
					pName = char.ToUpperInvariant(pName[0]) + pName.Substring(1);

				return "{" + pName + "}";
			}));
		}

		return message;
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

		if (isNullable)
			parameterType += "?";

		var isException = IsException(typeInfo.Type);

		return new(paramterName, parameterType, isException);
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

	readonly record struct ParameterData(string Name, string Type, bool IsException);
}

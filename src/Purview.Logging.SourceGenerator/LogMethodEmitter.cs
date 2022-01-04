using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Purview.Logging.SourceGenerator;

namespace Logger.Gen;

sealed class LogMethodEmitter
{
	const string _voidReturnType = "void";
	readonly static string _idisposableType = typeof(IDisposable).FullName;
	readonly static string _exceptionType = typeof(Exception).FullName;

	readonly MethodDeclarationSyntax _methodDeclaration;
	readonly GeneratorExecutionContext _context;
	readonly int _methodIndex;
	readonly string _defaultLevel;

	public LogMethodEmitter(MethodDeclarationSyntax methodDeclaration, GeneratorExecutionContext context, int methodIndex, string defaultLevel)
	{
		_methodDeclaration = methodDeclaration;
		_context = context;
		_methodIndex = methodIndex;
		_defaultLevel = defaultLevel;
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
				// Should always take the last. Convention over configuration...
				exceptionData = paramInfo.Value;
			}
		}

		var methodName = _methodDeclaration.Identifier.ToString();
		var paramsWithoutException = parameterData.ToArray();

		// Remove the last exception, if there is one.
		if (exceptionData != null)
		{
			paramsWithoutException = paramsWithoutException.Where(m => m.Name != exceptionData.Value.Name).ToArray();
			if (parameterData.Count == paramsWithoutException.Length)
			{
				_context.ReportDiagnostic(Diagnostic.Create(
					new DiagnosticDescriptor(
						"PVL-0001",
						"Unable to determine exception parameter",
						"Out of the {0} parameter(s), unable to determine the exception parameter named '{1}'.",
						"Purview.Logging",
						DiagnosticSeverity.Warning,
						true),
					_methodDeclaration.GetLocation(),
					messageArgs: new object[] { parameterData.Count, exceptionData.Value.Name })
				);

				return null;
			}
		}

		if (paramsWithoutException.Length > Helpers.MaximumLoggerDefineParameters)
		{
			_context.ReportDiagnostic(Diagnostic.Create(
				new DiagnosticDescriptor(
					"PVL-0004",
					"Maximum number of parameters exceeded.",
					"{0} is the maximum number of parameters allowed, {1} has {2} (excluding the last exception, if one exists).",
					"Purview.Logging",
					DiagnosticSeverity.Error,
					true),
				_methodDeclaration.GetLocation(),
				messageArgs: new object[] { Helpers.MaximumLoggerDefineParameters, methodName, paramsWithoutException.Length })
			);
		}

		var logSettings = GetLogSettings(cancellationToken);

		StringBuilder builder = new();

		// Build logger message.
		var logActionName = methodName;
		if (char.IsUpper(logActionName[0]))
			logActionName = char.ToLowerInvariant(logActionName[0]) + logActionName.Substring(1);

		logActionName = $"_{logActionName}";

		var methodParams = paramsWithoutException.Select(p => p.Type).ToArray();
		var actionParams = new[] { $"{Helpers.MSLoggingNamespace}.ILogger" }
			.Concat(methodParams)
			.Concat(new[] { methodReturnType == MethodReturnType.Void ? $"{_exceptionType}?" : _idisposableType });

		var loggerMessageFieldName = $"{logActionName}_{_methodIndex}";
		builder
			.Append("readonly static System.")
			.Append(methodReturnType == MethodReturnType.Void ? nameof(Action) : "Func")
			.Append('<')
			.Append(string.Join(", ", actionParams))
			.Append("> ")
			.Append(loggerMessageFieldName)
			.Append(" = ");

		builder
			.Append(Helpers.MSLoggingNamespace)
			.Append(".LoggerMessage.Define");

		if (methodReturnType == MethodReturnType.Scope)
			builder.Append("Scope");

		// Add generic types.
		if (methodParams.Length > 0)
		{
			builder
				.Append('<')
				.Append(string.Join(", ", methodParams))
				.Append('>');
		}

		builder.Append('(');

		var localDefault = exceptionData == null
			? _defaultLevel
			: "Error";

		var methodLogLevel = logSettings?.Level ?? localDefault;

		if (methodReturnType == MethodReturnType.Void)
		{
			// Append the parameters.
			// Log level..
			builder
				.Append(Helpers.MSLoggingNamespace)
				.Append(".LogLevel.")
				.Append(methodLogLevel)
				.Append(", ");

			// Event Id.
			builder
				.Append("new ")
				.Append(Helpers.MSLoggingNamespace)
				.Append(".EventId(")
				.Append(logSettings?.EventId ?? _methodIndex)
				.Append(", \"")
				.Append(logSettings?.Name ?? methodName)
				.Append("\"), ");
		}

		// Format message..
		builder
			.Append('"')
			.Append(logSettings?.Message ?? BuildMessage(methodName, paramsWithoutException))
			.Append('"');

		if (methodReturnType == MethodReturnType.Void)
		{
			// Log define options... disable skip because we do it.
			builder
				.Append(", new ")
				.Append(Helpers.MSLoggingNamespace)
				.Append(".LogDefineOptions { SkipEnabledCheck = true }");
		}

		builder
			.AppendLine(");")
			.AppendLine();

		builder
			.Append("public ")
			.Append(methodReturnType == MethodReturnType.Void ? _voidReturnType : _idisposableType)
			.Append(' ')
			.Append(methodName)
			.Append('(');

		builder.Append(string.Join(", ", parameterData.Select(p => $"{p.Type} {p.Name}")));

		builder.AppendLine(")");
		builder.AppendLine("{");

		if (methodReturnType == MethodReturnType.Void)
		{
			// Do the IsEnabled test.
			builder
				.Append("if (_logger.IsEnabled(")
				.Append(Helpers.MSLoggingLogLevelNamespaceAndTypeName)
				.Append('.')
				.Append(methodLogLevel)
				.AppendLine("))")
				.AppendLine("{");
		}

		if (methodReturnType == MethodReturnType.Scope)
			builder.Append("return ");

		builder
			.Append(loggerMessageFieldName)
			.Append("(_logger, ");

		foreach (var parameter in paramsWithoutException)
		{
			builder
				.Append(parameter.Name)
				.Append(", ");
		}

		if (exceptionData == null)
		{
			if (methodReturnType == MethodReturnType.Scope)
			{
				// Remove the last two chars - ', '
				builder.Remove(builder.Length - 2, 2);
			}
			else
			{
				builder.Append("null");
			}
		}
		else
		{
			builder.Append(exceptionData.Value.Name);
		}

		builder
			.AppendLine(");")
			.AppendLine("}");

		if (methodReturnType == MethodReturnType.Void)
			builder.AppendLine("}");

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

	LoggerSetttings? GetLogSettings(CancellationToken cancellationToken = default)
	{
		var options = (_context.Compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;
		var compilation = _context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(Helpers.AttributeDefinitions, Encoding.UTF8), options, cancellationToken: cancellationToken));

		var logEventAttribute = compilation.GetTypeByMetadataName($"{Helpers.MSLoggingNamespace}.{Helpers.PurviewLogEventAttributeNameWithSuffix}");
		if (logEventAttribute == null)
		{
			// No attribute defined.
			return null;
		}

		var model = _context.Compilation.GetSemanticModel(_methodDeclaration.SyntaxTree);
		var declaredSymbol = model.GetDeclaredSymbol(_methodDeclaration, cancellationToken: cancellationToken);
		if (declaredSymbol == null)
		{
			// This doesn't sound good...
			return null;
		}

		var attribute = declaredSymbol.GetAttributes().SingleOrDefault(m =>
			m.AttributeClass?.Name == Helpers.PurviewLogEventAttributeName
			|| m.AttributeClass?.Name == Helpers.PurviewLogEventAttributeNameWithSuffix);

		if (attribute == null)
		{
			// Doesn't contain an attribute.
			return null;
		}

		if (attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is not AttributeSyntax attributeSyntax)
			return null;

		if (attributeSyntax.ArgumentList?.Arguments.Count == 0)
			return null;

		int? eventId = null;
		string? eventName = null;
		string? logLevel = null;
		string? messageTemplate = null;

		var args = attributeSyntax.ArgumentList!.Arguments;
		foreach (var arg in args)
		{
			var argName = arg.NameEquals?.Name.ToString();
			if (string.IsNullOrWhiteSpace(argName))
				continue;

			var value = model.GetConstantValue(arg.Expression, cancellationToken);
			if (!value.HasValue)
				continue;

			if (argName == "Id")
			{
				if (value.Value is int id)
				{
					eventId = id;
					if (id == 0)
						eventId = null;
				}
			}
			else if (argName == "Name")
			{
				if (value.Value is string name && !string.IsNullOrWhiteSpace(name))
					eventName = name;
			}
			else if (argName == "Level")
			{
				if (value.Value is int id && Helpers.LogLevelValuesToNames.ContainsKey(id))
					logLevel = Helpers.LogLevelValuesToNames[id];
			}
			else if (argName == "Message")
			{
				if (value.Value is string message && !string.IsNullOrWhiteSpace(message))
					messageTemplate = message;
			}
		}

		return new(eventId, eventName, logLevel, messageTemplate);
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

			var isIDiposable = typeInfo.Type?.ToString() == _idisposableType;
			if (isIDiposable)
				return MethodReturnType.Scope;
		}

		_context.ReportDiagnostic(Diagnostic.Create(
			new DiagnosticDescriptor(
				"PVL-0002",
				"Invalid log method return type.",
				"{0} is not a valid return type, only void or {1} are valid.",
				"Purview.Logging",
				DiagnosticSeverity.Error,
				true),
			_methodDeclaration.GetLocation(),
			messageArgs: new object[] { _methodDeclaration.ReturnType, _idisposableType })
		);

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

	readonly record struct LoggerSetttings(int? EventId, string? Name, string? Level, string? Message);
}

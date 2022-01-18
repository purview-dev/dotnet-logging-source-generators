using System.Text;

namespace Purview.Logging.SourceGenerator.Emitters;

partial class LogMethodEmitter
{
	static void AppendBeginFieldDefinition(MethodReturnType methodReturnType, StringBuilder builder, string[] methodParams, IEnumerable<string> actionParams, string loggerMessageFieldName)
	{
		/*
		 * New lines added for clarity.
		 * Generate:
		 * 
		 * readonly static System.{Action|Func} {FieldName}<{FieldParameters}> =
		 *		{MSLoggerNS}.LoggerMessage.{Define|DefineScope}
		 *		{<Parameters>}
		 *		(
		*/

		// Create the Action/ Func field for the LoggerMessage.Define(Scope).
		builder
			.Append("readonly static System.")
			.Append(methodReturnType == MethodReturnType.Void ? nameof(Action) : "Func")
			.Append('<')
			.Append(string.Join(", ", actionParams))
			.Append("> ")
			.Append(loggerMessageFieldName)
			.Append(" = ")
			.Append(Helpers.MSLoggingNamespace)
			.Append(".LoggerMessage.Define");

		// If it's a scoped method (Func)...
		if (methodReturnType == MethodReturnType.Scope)
			builder.Append("Scope");

		// Add generic types if there are any parameters.
		if (methodParams.Length > 0)
		{
			builder
				.Append('<')
				.Append(string.Join(", ", methodParams))
				.Append('>');
		}

		builder.Append('(');
	}

	void AppendEndFieldDefinition(MethodReturnType methodReturnType, string methodName, ParameterData[] paramsWithoutException, LoggerSetttings? logSettings, StringBuilder builder, string methodLogLevel)
	{
		/*
		 * New lines added for clarity.
		 * Field definition is already generated here... XXX.Define( or DefineScope(
		 * 
		 * if the event is not scoped (i.e. is a void method), then generate the parameters:
		 *		logLevel, new MSLogNS.EventId(eventId, "eventName"),
		 *	
		 *	then append the formatted message, either generated or defined from LogEventAttribute:
		 *		"formattedMessage"
		 *		
		 *	if the event is not scoped, append the LogDefineOptions which disabled the enabled check
		 *	because we do it ourselves:
		 *		, new MSLogNS.LogDefineOptions { SkipEnabledCheck = true }
		 *		
		 *	finally, close the method off with:
		 *		);	
		*/

		// If it's not a scoped method, append the parameters (LoggerMessage.Define).
		if (methodReturnType == MethodReturnType.Void)
		{
			// Append the parameters...!
			// Log level..
			builder
				.Append(Helpers.MSLoggingLogLevelNamespaceAndTypeName)
				.Append('.')
				.Append(methodLogLevel)
				.Append(", ");

			// Event Id and Event Name.
			// If we don't have an event Id, use the index (i.e. the order in which it appears
			// in the interface).
			builder
				.Append("new ")
				.Append(Helpers.MSLoggingNamespace)
				.Append(".EventId(")
				.Append(logSettings?.EventId ?? _methodIndex)
				.Append(", \"");

			if (_defaultLoggerSettings.IncludeContextInEventName)
			{
				builder
					.Append(_loggerName)
					.Append('.');
			}

			builder
				.Append(logSettings?.Name ?? methodName)
				.Append("\"), ");
		}

		// Format message... if one isn't defined, create one.
		builder
			.Append('"')
			.Append(logSettings?.Message ?? BuildMessage(methodName, paramsWithoutException))
			.Append('"');

		if (methodReturnType == MethodReturnType.Void && _hasLogOptions.Value)
		{
			// If LogDefineOptions is defined... skip the log level enable check because we do it.
			builder
				.Append(", new ")
				.Append(Helpers.MSLoggingNamespace)
				.Append(".LogDefineOptions { SkipEnabledCheck = true }");
		}

		builder
			.AppendLine(");")
			.AppendLine();
	}

	static void AppendPublicMethodDefinitionFromInterface(MethodReturnType methodReturnType, List<ParameterData> parameterData, string methodName, StringBuilder builder)
	{
		/*
		 * Generate the interface defined public method, i.e.
		 *		interface IBasicLogger
		 *		{
		 *			void AThing(string stringParam, int intParam);
		 *		}
		 * 
		 * the output would be:
		 *		public void AThing(string stringParam, intParam);
		 *  
		 */

		// Create the public method, i.e. the interface defined method.
		builder
			.Append("public ")
			.Append(methodReturnType == MethodReturnType.Void ? _voidReturnType : Helpers.IDisposableType)
			.Append(' ')
			.Append(methodName)
			.Append('(');

		// Append the parameters (Type and Name) to the method.
		builder
			.Append(string.Join(", ", parameterData.Select(p => $"{p.Type} {p.Name}")))
			.AppendLine(")");
	}

	static void AppendMethodBody(MethodReturnType methodReturnType, ParameterData? exceptionData, ParameterData[] paramsWithoutException, StringBuilder builder, string loggerMessageFieldName, string methodLogLevel)
	{
		/*
		 * Generates the body of the method, i.e.
		 * 
		 * Scoped method:
		 *		IDisposable BeginAProcess(int messageId):
		 * 				
		 *		Body:
		 *		{
		 *			return _beginAProcess_1(_logger, messageId);
		 *		}
		 *		
		 *	Non-scoped method:
		 *		void AThingHappenedInTheProcess(string someData):
		 * 				
		 *		Body:
		 *		{
		 *			if (!_logger.IsEnabled({logLevel})
		 *			{
		 *				return;
		 *			}
		 *		
		 *			_aThingHappenedInTheProcess_1(_logger, someData);
		 *		}
		 * 
		 */
		builder.AppendLine("{");

		if (methodReturnType == MethodReturnType.Void)
		{
			// Do the IsEnabled test with the log level.
			builder
				.Append("if (!_logger.IsEnabled(")
				.Append(Helpers.MSLoggingLogLevelNamespaceAndTypeName)
				.Append('.')
				.Append(methodLogLevel)
				.AppendLine("))")
				.AppendLine("{")
				.AppendLine("return;")
				.AppendLine("}")
				.AppendLine();
		}

		// If we're a scoped method, append the return so we return the IDisposable...
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

		// If we have an exception parameter, append it here...
		// If this is scoped, then it's created like any other parameter,
		// but if it's non-scoped we take the last defined exception param
		// and push to the exception parameter of the LoggerMessage.Define Action.
		AppendExceptionParameter(methodReturnType, exceptionData, builder);

		// Close the call and the body of the method.
		builder
			.AppendLine(");")
			.AppendLine("}");
	}

	static void AppendExceptionParameter(MethodReturnType methodReturnType, ParameterData? exceptionData, StringBuilder builder)
	{
		if (exceptionData == null)
		{
			if (methodReturnType == MethodReturnType.Scope)
			{
				// Remove the last two chars - ', '
				builder.Remove(builder.Length - 2, 2);
			}
			else
			{
				// Pass in null to the exception parameter because there wasn't one defined.
				builder.Append("null");
			}
		}
		else
		{
			builder.Append(exceptionData.Value.Name);
		}
	}
}

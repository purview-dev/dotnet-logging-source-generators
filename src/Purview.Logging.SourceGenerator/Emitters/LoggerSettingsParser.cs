using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Logging.SourceGenerator.Emitters;

static class LoggerSettingsParser
{
	static public LoggerSetttings? GetLogSettings(GeneratorExecutionContext context, MethodDeclarationSyntax methodDeclarationSyntax, CancellationToken cancellationToken = default)
	{
		var logEventAttribute = Helpers.GetAttributeSymbol(Helpers.PurviewLogEventAttributeNameWithSuffix, context, cancellationToken);
		if (logEventAttribute == null)
		{
			// No attribute defined.
			return null;
		}

		var model = context.Compilation.GetSemanticModel(methodDeclarationSyntax.SyntaxTree);
		var declaredSymbol = model.GetDeclaredSymbol(methodDeclarationSyntax, cancellationToken: cancellationToken);
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
}

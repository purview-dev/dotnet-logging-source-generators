namespace Purview.Logging.SourceGenerator.Emitters;

sealed class DefaultLoggerSettings
{
	public string LogLevelDefault { get; set; } = Helpers.LogLevelDefault;

	public bool GenerateAddLogDIMethod { get; set; } = Helpers.GenerateAddLogDIMethodDefault;

	public string MessageTemplate { get; set; } = Helpers.MessageTemplateDefault;

	public bool IncludeContextInEventName { get; set; } = Helpers.IncludeContextInEventNameDefault;

	public string ContextSeparator { get; set; } = Helpers.ContextSeparatorDefault;

	public string ContextArgumentSeparator { get; set; } = Helpers.ContextArgumentListSeparatorDefault;

	public string ArgumentSerparator { get; set; } = Helpers.ArgumentSerparatorDefault;

	public string ArgumentNameValueSerparator { get; set; } = Helpers.ArgumentNameValueSerparatorDefault;
}

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

	public DefaultLoggerSettings Merge(DefaultLoggerSettings overrides)
	{
		// Current: Most Important...
		// overrides: Not default, override... 

		if (overrides.LogLevelDefault != Helpers.LogLevelDefault)
			LogLevelDefault = overrides.LogLevelDefault;
		if (overrides.GenerateAddLogDIMethod != Helpers.GenerateAddLogDIMethodDefault)
			GenerateAddLogDIMethod = overrides.GenerateAddLogDIMethod;
		if (overrides.MessageTemplate != Helpers.MessageTemplateDefault)
			MessageTemplate = overrides.MessageTemplate;
		if (overrides.IncludeContextInEventName != Helpers.IncludeContextInEventNameDefault)
			IncludeContextInEventName = overrides.IncludeContextInEventName;
		if (overrides.ContextSeparator != Helpers.ContextSeparatorDefault)
			ContextSeparator = overrides.ContextSeparator;
		if (overrides.ContextArgumentSeparator != Helpers.ContextArgumentListSeparatorDefault)
			ContextArgumentSeparator = overrides.ContextArgumentSeparator;
		if (overrides.ArgumentSerparator != Helpers.ArgumentSerparatorDefault)
			ArgumentSerparator = overrides.ArgumentSerparator;
		if (overrides.ArgumentNameValueSerparator != Helpers.ArgumentNameValueSerparatorDefault)
			ArgumentNameValueSerparator = overrides.ArgumentNameValueSerparator;

		return this;
	}
}

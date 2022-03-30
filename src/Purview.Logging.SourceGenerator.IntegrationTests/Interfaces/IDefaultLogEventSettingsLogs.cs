using Microsoft.Extensions.Logging;

namespace Purview.Logging.SourceGenerator.Interfaces;

[DefaultLogEventSettings(Level = LogLevel.Debug)]
public interface IDefaultLogEventSettingsLogs
{
	void InterfaceDefaultToDebug();

	[LogEvent(Level = LogLevel.Warning)]
	void MethodDefinedAsWarning();
}

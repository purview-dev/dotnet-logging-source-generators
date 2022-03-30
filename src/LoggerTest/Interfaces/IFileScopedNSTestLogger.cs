using Microsoft.Extensions.Logging;

namespace LoggerTest.Interfaces;

[DefaultLogEventSettings(Level = LogLevel.Critical)]
public interface IFileScopedNSTestLogger
{
	[LogEvent(Id = 5959)]
	void LogTest();
}

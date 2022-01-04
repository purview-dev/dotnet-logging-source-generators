using Microsoft.Extensions.Logging;

namespace LoggerTest;

[DefaultLogLevel(LogLevel.Critical)]
public interface IFileScopedNSTestLogger
{
	[LogEvent(Id = 5959)]
	void LogTest();
}

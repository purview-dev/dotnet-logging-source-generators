using Microsoft.Extensions.Logging;

namespace LoggerTest.Interfaces;

[DefaultLogLevel(LogLevel.Critical)]
public interface IFileScopedNSTestLogger
{
	[LogEvent(Id = 5959)]
	void LogTest();
}

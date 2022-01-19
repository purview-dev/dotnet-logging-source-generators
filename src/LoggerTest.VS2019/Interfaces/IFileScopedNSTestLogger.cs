using Microsoft.Extensions.Logging;

namespace LoggerTest.Interfaces
{
	[DefaultLogEventSettings(LogLevel = LogLevel.Critical)]
	public interface IFileScopedNSTestLogger
	{
		[LogEvent(Id = 5959)]
		void LogTest();
	}
}

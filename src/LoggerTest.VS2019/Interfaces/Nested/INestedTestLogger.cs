using Microsoft.Extensions.Logging;

namespace LoggerTest.Interfaces.Nested
{
	public interface INestedTestLogger
{
		[LogEvent(Level = LogLevel.Debug, Id = 1)]
		void LogTest();
    }
}

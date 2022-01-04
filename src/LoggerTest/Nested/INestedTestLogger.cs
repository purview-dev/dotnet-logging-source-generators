#pragma warning disable IDE0161 // Convert to file-scoped namespace
using Microsoft.Extensions.Logging;

namespace LoggerTest.Nested
#pragma warning restore IDE0161 // Convert to file-scoped namespace
{
	public interface INestedTestLogger
{
		[LogEvent(Level = LogLevel.Debug, Id = 1)]
		void LogTest();
    }
}

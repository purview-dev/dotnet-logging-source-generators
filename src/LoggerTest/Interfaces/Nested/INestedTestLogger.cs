using Microsoft.Extensions.Logging;

#pragma warning disable IDE0161 // Convert to file-scoped namespace
namespace LoggerTest.Interfaces.Nested
#pragma warning restore IDE0161 // Convert to file-scoped namespace
{
	public interface INestedTestLogger
{
		[LogEvent(Level = LogLevel.Debug, Id = 1)]
		void LogTest();
    }
}

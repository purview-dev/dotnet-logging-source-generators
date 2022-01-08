using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.Services;

interface IServiceLogger
{
	[LogEvent(Level = LogLevel.Trace)]
	void TestTrace(string? stringParameter, int? intParameter);

	[LogEvent(Level = LogLevel.Debug)]
	void TestDebug(string? stringParameter, int? intParameter);

	[LogEvent(Level = LogLevel.Information)]
	void TestInformation(string? stringParameter, int? intParameter);

	[LogEvent(Level = LogLevel.Warning)]
	void TestWarning(string? stringParameter, int? intParameter);

	[LogEvent(Level = LogLevel.Error)]
	void TestError(string? stringParameter, int? intParameter);

	[LogEvent(Level = LogLevel.Critical)]
	void TestCritical(string? stringParameter, int? intParameter);
}

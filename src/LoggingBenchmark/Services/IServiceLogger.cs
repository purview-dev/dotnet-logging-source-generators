using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.Services;

interface IServiceLogger
{
	[LogEvent(Level = LogLevel.Error, Message = LoggingBenchmarkConsts.TestErrorMessage)]
	void TestError(string? stringParameter, int? intParameter);

	IDisposable TestStart(DateTimeOffset started);
}

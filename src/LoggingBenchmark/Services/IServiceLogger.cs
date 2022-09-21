using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.Services;

interface IServiceLogger
{
	[LogEvent(Level = LogLevel.Error)]
	void TestError(string? stringParameter, int? intParameter);

	IDisposable TestStart(DateTimeOffset started);
}

using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.Services;

static class ILoggerExtensions
{
	readonly static Func<ILogger, DateTimeOffset, IDisposable> _testStart = LoggerMessage.DefineScope<DateTimeOffset>(LoggingBenchmarkConsts.TestStartMessage);

	readonly static Action<ILogger, string?, int?, Exception?> _testError = LoggerMessage.Define<string?, int?>(
		LogLevel.Error,
		new EventId(5, nameof(TestError)),
		LoggingBenchmarkConsts.TestErrorMessage
	);

	static public IDisposable TestStart(this ILogger logger, DateTimeOffset started)
		=> _testStart(logger, started);

	static public void TestError(this ILogger logger, string? stringParam, int? intParam, Exception? exception = null)
	{
		if (logger.IsEnabled(LogLevel.Error))
		{
			_testError(logger, stringParam, intParam, exception);
		}
	}
}

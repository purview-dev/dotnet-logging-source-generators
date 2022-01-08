using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.Services;

static class ILoggerExtensions
{
	readonly static Action<ILogger, string?, int?, Exception?> _testTrace = LoggerMessage.Define<string?, int?>(
		LogLevel.Trace,
		new EventId(1, nameof(TestTrace)),
		"TestTrace: {StringParam}, {IntParam}"
	);

	readonly static Action<ILogger, string?, int?, Exception?> _testDebug = LoggerMessage.Define<string?, int?>(
		LogLevel.Debug,
		new EventId(2, nameof(TestDebug)),
		"TestDebug: {StringParam}, {IntParam}"
	);

	readonly static Action<ILogger, string?, int?, Exception?> _testInformation = LoggerMessage.Define<string?, int?>(
		LogLevel.Information,
		new EventId(3, nameof(TestDebug)),
		"TestInformation: {StringParam}, {IntParam}"
	);

	readonly static Action<ILogger, string?, int?, Exception?> _testWarning = LoggerMessage.Define<string?, int?>(
		LogLevel.Warning,
		new EventId(4, nameof(TestWarning)),
		"TestWarning: {StringParam}, {IntParam}"
	);

	readonly static Action<ILogger, string?, int?, Exception?> _testError = LoggerMessage.Define<string?, int?>(
		LogLevel.Error,
		new EventId(5, nameof(TestWarning)),
		"TestError: {StringParam}, {IntParam}"
	);

	readonly static Action<ILogger, string?, int?, Exception?> _testCritical = LoggerMessage.Define<string?, int?>(
		LogLevel.Critical,
		new EventId(6, nameof(TestWarning)),
		"TestCritical: {StringParam}, {IntParam}"
	);

	static public void TestTrace(this ILogger logger, string? stringParam, int? intParam, Exception? exception = null)
	{
		if (!logger.IsEnabled(LogLevel.Trace))
			return;

		_testTrace(logger, stringParam, intParam, exception);
	}

	static public void TestDebug(this ILogger logger, string? stringParam, int? intParam, Exception? exception = null)
	{
		if (!logger.IsEnabled(LogLevel.Debug))
			return;

		_testDebug(logger, stringParam, intParam, exception);
	}

	static public void TestInformation(this ILogger logger, string? stringParam, int? intParam, Exception? exception = null)
	{
		if (!logger.IsEnabled(LogLevel.Information))
			return;

		_testInformation(logger, stringParam, intParam, exception);
	}

	static public void TestWarning(this ILogger logger, string? stringParam, int? intParam, Exception? exception = null)
	{
		if (!logger.IsEnabled(LogLevel.Warning))
			return;

		_testWarning(logger, stringParam, intParam, exception);
	}

	static public void TestError(this ILogger logger, string? stringParam, int? intParam, Exception? exception = null)
	{
		if (!logger.IsEnabled(LogLevel.Error))
			return;

		_testError(logger, stringParam, intParam, exception);
	}

	static public void TestCritical(this ILogger logger, string? stringParam, int? intParam, Exception? exception = null)
	{
		if (!logger.IsEnabled(LogLevel.Critical))
			return;

		_testCritical(logger, stringParam, intParam, exception);
	}
}

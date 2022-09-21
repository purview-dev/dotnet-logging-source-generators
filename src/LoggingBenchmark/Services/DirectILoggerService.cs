using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.Services;

sealed class DirectILoggerService
{
	readonly ILogger<DirectILoggerService> _logger;

	public DirectILoggerService(ILogger<DirectILoggerService> logger)
	{
		_logger = logger;
	}

	public void Execute(string stringParam, int intParam)
	{
		using (_logger.BeginScope(LoggingBenchmarkConsts.TestStartMessage, DateTimeOffset.UtcNow))
		{
			_logger.LogError(LoggingBenchmarkConsts.TestErrorMessage, stringParam, intParam);
		}
	}
}

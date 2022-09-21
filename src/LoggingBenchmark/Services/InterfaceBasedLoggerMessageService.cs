using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.Services;

sealed class InterfaceBasedLoggerMessageService
{
	readonly IServiceLogger _logger;

	public InterfaceBasedLoggerMessageService(IServiceLogger logger)
	{
		_logger = logger;
	}

	public void Execute(string stringParam, int intParam)
	{
		using (_logger.TestStart(DateTimeOffset.UtcNow))
		{
			_logger.TestError(stringParam, intParam);
		}
	}
}

using LoggingBenchmark.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.Services;

sealed class ExtensionBasedLoggerMessageService
{
	readonly ILogger<ExtensionBasedLoggerMessageService> _logger;

	public ExtensionBasedLoggerMessageService(ILogger<ExtensionBasedLoggerMessageService> logger)
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


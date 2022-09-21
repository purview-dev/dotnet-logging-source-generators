using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoggingBenchmark.Services;
sealed class LoggerViaLoggerMessageAttributeServiceTest
{
	readonly LoggerViaLoggerMessageAttributeService _logger;

	public LoggerViaLoggerMessageAttributeServiceTest(LoggerViaLoggerMessageAttributeService logger)
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

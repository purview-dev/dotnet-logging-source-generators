using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.Services;

// otherwise we get warning that eventId is -1 for every log
#pragma warning disable SYSLIB1006

public partial class LoggerViaLoggerMessageAttributeService
{
	readonly ILogger<LoggerViaLoggerMessageAttributeService> _logger;

	public LoggerViaLoggerMessageAttributeService(ILogger<LoggerViaLoggerMessageAttributeService> logger)
	{
		_logger = logger;
	}

	public void Execute(string stringParam, int intParam)
	{
		_logger.TestStart(DateTimeOffset.UtcNow);
		_logger.TestError(stringParam, intParam);
	}


	[LoggerMessage(Level = LogLevel.Error)]
	public partial void TestError(string? stringParameter, int? intParameter);

	[LoggerMessage(Level = LogLevel.Information, Message = "TestStart => Started: {Started}")]
	public partial void TestStart(DateTimeOffset started);
}

#pragma warning restore SYSLIB1006

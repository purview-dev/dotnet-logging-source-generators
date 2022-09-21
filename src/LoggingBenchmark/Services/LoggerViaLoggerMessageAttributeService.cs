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

	[LoggerMessage(Level = LogLevel.Error, Message = LoggingBenchmarkConsts.TestErrorMessage)]
	public partial void TestError(string? stringParam, int? intParam);


	//[LoggerMessage(Level = LogLevel.Information, Message = "TestStart => Started: {Started}")]
	//public partial IDisposable TestStart(DateTimeOffset started);

	// with LoggerMessageAttribute you can't define method which will return IDisposable
	// for the proper testing we will use LoggerMessage static class, as in the end LoggerMessageAttribute produces it
	readonly static Func<ILogger, DateTimeOffset, IDisposable> _testStart = LoggerMessage.DefineScope<DateTimeOffset>(LoggingBenchmarkConsts.TestStartMessage);

	public IDisposable TestStart(DateTimeOffset started) => _testStart(_logger, started);
}
#pragma warning restore SYSLIB1006

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
	readonly ILogger _logger;

	public LoggerViaLoggerMessageAttributeService(ILogger logger)
	{
		_logger = logger;
	}

	[LoggerMessage(Level = LogLevel.Trace)]
	public partial void TestTrace(string? stringParameter, int? intParameter);

	[LoggerMessage(Level = LogLevel.Debug)]
	public partial void TestDebug(string? stringParameter, int? intParameter);

	[LoggerMessage(Level = LogLevel.Information)]
	public partial void TestInformation(string? stringParameter, int? intParameter);

	[LoggerMessage(Level = LogLevel.Warning)]
	public partial void TestWarning(string? stringParameter, int? intParameter);

	[LoggerMessage(Level = LogLevel.Error)]
	public partial void TestError(string? stringParameter, int? intParameter);

	[LoggerMessage(Level = LogLevel.Critical)]
	public partial void TestCritical(string? stringParameter, int? intParameter);

	[LoggerMessage(Level = LogLevel.Information, Message = "TestStart => Started: {Started}")]
	public partial void TestStart(DateTimeOffset started);
}

#pragma warning restore SYSLIB1006

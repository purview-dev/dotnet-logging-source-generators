using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.Services;
class LoggerMessageServiceLogger : ServiceBase
{
	public void Execute()
	{
		Logger.TestStart(DateTimeOffset.UtcNow);
		Logger.TestTrace("A Trace Parameter", 1);
		Logger.TestDebug("A Debug Parameter", 11);
		Logger.TestInformation("A Information Parameter", 111);
		Logger.TestWarning("A Warning Parameter", 1111);
		Logger.TestError("A Error Parameter", 11111);
		Logger.TestCritical("A Critical Parameter", 11111);
	}

	LoggerViaLoggerMessageAttributeService Logger
		=> ServiceProvider.GetRequiredService<LoggerViaLoggerMessageAttributeService>();

	override protected void Register(IServiceCollection services)
		=> services
		.AddSingleton<ILoggerFactory, LoggerFactory>()
		.AddSingleton<ILogger>(c => c.GetService<ILoggerFactory>()!.CreateLogger(typeof(LoggerViaLoggerMessageAttributeService)))
		.AddSingleton<LoggerViaLoggerMessageAttributeService>();
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.Services;

sealed class ExtensionBasedLoggerMessageService : ServiceBase
{
	public void Execute()
	{
		using (Logger.TestStart(DateTimeOffset.UtcNow))
		{
			Logger.TestTrace("A Trace Parameter", 1);
			Logger.TestDebug("A Debug Parameter", 11);
			Logger.TestInformation("A Information Parameter", 111);
			Logger.TestWarning("A Warning Parameter", 1111);
			Logger.TestError("A Error Parameter", 11111);
			Logger.TestCritical("A Critical Parameter", 11111);
		}
	}

	ILogger<ExtensionBasedLoggerMessageService> Logger
		=> ServiceProvider.GetRequiredService<ILogger<ExtensionBasedLoggerMessageService>>();
}


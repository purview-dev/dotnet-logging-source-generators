using Microsoft.Extensions.DependencyInjection;

namespace LoggingBenchmark.Services;

sealed class InterfaceBasedLoggerMessageService : ServiceBase
{
	override protected void Register(IServiceCollection services)
		=> services.AddLog<IServiceLogger>();

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

	IServiceLogger Logger
		=> ServiceProvider.GetRequiredService<IServiceLogger>();
}

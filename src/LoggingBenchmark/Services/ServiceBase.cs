using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.Services;

abstract class ServiceBase
{
	readonly Lazy<IServiceProvider> _serviceProvider;

	protected ServiceBase()
	{
		_serviceProvider = new Lazy<IServiceProvider>(Create);
	}

	public IServiceProvider ServiceProvider => _serviceProvider.Value;

	IServiceProvider Create()
	{
		ServiceCollection services = new();

		services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));

		Register(services);

		return services.BuildServiceProvider();
	}

	virtual protected void Register(IServiceCollection services)
	{ }
}

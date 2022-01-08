using Microsoft.Extensions.DependencyInjection;

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

		services.AddLogging(builder =>
		{
		});

		Register(services);

		return services.BuildServiceProvider();
	}

	virtual protected void Register(IServiceCollection services)
	{ }
}

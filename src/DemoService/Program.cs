var serviceProvider = BuildServiceProvider();
var processingService = serviceProvider.GetRequiredService<IProcessingService>();

// Create/ Get the state...
var contextId = Guid.NewGuid();
DemoService.Models.SomeData someData = new() {
	ACount = 7,
	Payload = "...likely coming from input or external service..."
};

// Run the service.
processingService.Process(contextId, someData);

// Build and configure the service provider.
static IServiceProvider BuildServiceProvider()
{
	ServiceCollection services = new();

	services
		.AddLogging(builder =>
		{
			builder
				.AddSimpleConsole(consoleOptions =>
				{
					consoleOptions.IncludeScopes = true;
					consoleOptions.UseUtcTimestamp = true;
				})
				.SetMinimumLevel(LogLevel.Trace);
		});

	services
		.AddTransient<IProcessingService, DemoService.ApplicationServices.ProcessingService>()
		.AddLog<IProcessingServiceLogs>();

	return services.BuildServiceProvider(new ServiceProviderOptions {
		ValidateOnBuild = true,
		ValidateScopes = true
	});
}

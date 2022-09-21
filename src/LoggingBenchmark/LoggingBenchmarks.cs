using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using LoggingBenchmark.LoggerProviders;
using LoggingBenchmark.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoggingBenchmark;

//[SimpleJob(RuntimeMoniker.Net462)]
//[SimpleJob(RuntimeMoniker.Net472)]
//[SimpleJob(RuntimeMoniker.Net48)]
//[SimpleJob(RuntimeMoniker.Net50)]
[SimpleJob(RuntimeMoniker.Net60, baseline: true)]
[MemoryDiagnoser]
public class LoggingBenchmarks
{
	DirectILoggerService _directILoggerService = default!;
	ExtensionBasedLoggerMessageService _extensionLoggerMessageService = default!;
	InterfaceBasedLoggerMessageService _interfaceLoggerMessageService = default!;
	LoggerViaLoggerMessageAttributeServiceTest _loggerMessageServiceViaLoggerMessageAttribute = default!;

	[Params(10, 100)]
	public int Iterations { get; set; }

	[ParamsAllValues]
	public bool IsLogEnabled { get; set; }

	[GlobalSetup]
	public void GlobalSetup()
	{
		var services = new ServiceCollection();

		// the only diffrence between NullLogger and VoidLogger is that for VoidLogger.IsEnabled will be always returning true.
		if (IsLogEnabled)
		{
			services.AddSingleton<ILoggerFactory, VoidLoggerFactory>();
			services.AddSingleton(typeof(ILogger), typeof(VoidLogger));
			services.AddSingleton(typeof(ILogger<>), typeof(VoidLogger<>));
		}
		else
		{
			services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
			services.AddSingleton(typeof(ILogger), typeof(NullLogger));
			services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
		}

		services.AddSingleton<DirectILoggerService>();

		services.AddSingleton<ExtensionBasedLoggerMessageService>();

		services.AddLog<IServiceLogger>();
		services.AddSingleton<InterfaceBasedLoggerMessageService>();

		services.AddSingleton<LoggerViaLoggerMessageAttributeService>();
		services.AddSingleton<LoggerViaLoggerMessageAttributeServiceTest>();

		var container = services.BuildServiceProvider();

		_directILoggerService = container.GetRequiredService<DirectILoggerService>();
		_extensionLoggerMessageService = container.GetRequiredService<ExtensionBasedLoggerMessageService>();
		_interfaceLoggerMessageService = container.GetRequiredService<InterfaceBasedLoggerMessageService>();
		_loggerMessageServiceViaLoggerMessageAttribute = container.GetRequiredService<LoggerViaLoggerMessageAttributeServiceTest>();
	}

	[Benchmark(Baseline = true, Description = "Direct:ILogger<T>")]
	public void DirectILoggerService()
	{
		for (var i = 0; i < Iterations; i++)
		{
			_directILoggerService.Execute(LoggingBenchmarkConsts.StringParamVal, LoggingBenchmarkConsts.IntParamVal);
		}
	}

	[Benchmark(Description = "Extension:LoggerMessage")]
	public void ExtensionLoggerMessageService()
	{
		for (var i = 0; i < Iterations; i++)
		{
			_extensionLoggerMessageService.Execute(LoggingBenchmarkConsts.StringParamVal, LoggingBenchmarkConsts.IntParamVal);
		}
	}

	[Benchmark(Description = "Interface:LoggerMessage")]
	public void InterfaceLoggerMessageService()
	{
		for (var i = 0; i < Iterations; i++)
		{
			_interfaceLoggerMessageService.Execute(LoggingBenchmarkConsts.StringParamVal, LoggingBenchmarkConsts.IntParamVal);
		}
	}

	[Benchmark(Description = "PartialClassGenerated:LoggerMessage")]
	public void LoggerViaLoggerMessageAttributeService()
	{
		for (var i = 0; i < Iterations; i++)
		{
			_loggerMessageServiceViaLoggerMessageAttribute.Execute(LoggingBenchmarkConsts.StringParamVal, LoggingBenchmarkConsts.IntParamVal);
		}
	}
}

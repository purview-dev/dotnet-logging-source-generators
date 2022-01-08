using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace LoggingBenchmark;

[SimpleJob(RuntimeMoniker.Net462)]
[SimpleJob(RuntimeMoniker.Net472)]
[SimpleJob(RuntimeMoniker.Net48)]
[SimpleJob(RuntimeMoniker.NetCoreApp31)]
[SimpleJob(RuntimeMoniker.Net50)]
[SimpleJob(RuntimeMoniker.Net60, baseline: true)]
public class LoggingBenchmarks
{
	readonly Services.InterfaceBasedService _interfaceService = new();
	readonly Services.MSILoggerService _msILoggerService = new();
	readonly Services.MSLoggerMessageService _msLoggerMessageService = new();

	[Params(1, 10, 100)]
	public int Iterations { get; set; }

	[Benchmark(Baseline = true, Description = "ILogger<T>")]
	public void MSILoggerService()
	{
		for (var i = 0; i < Iterations; i++)
		{
			_msILoggerService.Execute();
		}
	}

	[Benchmark(Description = "LoggerMessage.Define")]
	public void MSLoggerMessageService()
	{
		for (var i = 0; i < Iterations; i++)
		{
			_msLoggerMessageService.Execute();
		}
	}

	[Benchmark(Description = "Interface-based LoggerMessage.Define")]
	public void InterfaceBased()
	{
		for (var i = 0; i < Iterations; i++)
		{
			_interfaceService.Execute();
		}
	}
}

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace LoggingBenchmark;

//[SimpleJob(RuntimeMoniker.Net462)]
//[SimpleJob(RuntimeMoniker.Net472)]
//[SimpleJob(RuntimeMoniker.Net48)]
//[SimpleJob(RuntimeMoniker.Net50)]
[SimpleJob(RuntimeMoniker.Net60, baseline: true)]
[MemoryDiagnoser]
public class LoggingBenchmarks
{
	readonly Services.DirectILoggerService _directILoggerService = new();
	readonly Services.ExtensionBasedLoggerMessageService _extensionLoggerMessageService = new();
	readonly Services.InterfaceBasedLoggerMessageService _interfaceLoggerMessageService = new();
	readonly Services.LoggerMessageServiceLogger _loggerMessageServiceViaLoggerMessageAttribute = new();

	[Params(10)]
	public int Iterations { get; set; }

	[Benchmark(Baseline = true, Description = "Direct:ILogger<T>")]
	public void DirectILoggerService()
	{
		for (var i = 0; i < Iterations; i++)
		{
			_directILoggerService.Execute();
		}
	}

	[Benchmark(Description = "Extension:LoggerMessage")]
	public void ExtensionLoggerMessageService()
	{
		for (var i = 0; i < Iterations; i++)
		{
			_extensionLoggerMessageService.Execute();
		}
	}

	[Benchmark(Description = "Interface:LoggerMessage")]
	public void InterfaceLoggerMessageService()
	{
		for (var i = 0; i < Iterations; i++)
		{
			_interfaceLoggerMessageService.Execute();
		}
	}

	[Benchmark(Description = "PartialClassGenerated:LoggerMessage")]
	public void LoggerViaLoggerMessageAttributeService()
	{
		for (var i = 0; i < Iterations; i++)
		{
			_loggerMessageServiceViaLoggerMessageAttribute.Execute();
		}
	}
}

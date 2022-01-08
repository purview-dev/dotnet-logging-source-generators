using BenchmarkDotNet.Running;
namespace LoggingBenchmark;

static public class Program
{
	static public void Main(string[] args)
	{
		var summary = BenchmarkRunner.Run<LoggingBenchmarks>();
	}
}

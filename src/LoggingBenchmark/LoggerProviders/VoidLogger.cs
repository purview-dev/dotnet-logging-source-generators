using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.LoggerProviders;

public class VoidLogger : ILogger
{
	public static VoidLogger Instance { get; } = new VoidLogger();

	private VoidLogger()
	{
	}

	public IDisposable BeginScope<TState>(TState state) => VoidScope.Instance;
	public bool IsEnabled(LogLevel logLevel) => true;
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{

	}
}


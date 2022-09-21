using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.LoggerProviders;

public class VoidLogger<T> : ILogger<T>
{
	public static readonly VoidLogger<T> Instance = new VoidLogger<T>();

	public IDisposable BeginScope<TState>(TState state) where TState : notnull
	{
		return VoidScope.Instance;
	}

	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		return true;
	}
}

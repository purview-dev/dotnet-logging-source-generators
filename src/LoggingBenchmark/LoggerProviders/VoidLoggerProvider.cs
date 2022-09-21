using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.LoggerProviders;

public class VoidLoggerProvider : ILoggerProvider
{
	public static VoidLoggerProvider Instance => new VoidLoggerProvider();

	private VoidLoggerProvider()
	{

	}

	public ILogger CreateLogger(string categoryName) => VoidLogger.Instance;

	public void Dispose() { }
}


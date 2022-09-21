using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LoggingBenchmark.LoggerProviders;

public class VoidLoggerFactory : ILoggerFactory
{
	public VoidLoggerFactory() { }

	/// <summary>
	/// Returns the shared instance of <see cref="NullLoggerFactory"/>.
	/// </summary>
	public static readonly VoidLoggerFactory Instance = new VoidLoggerFactory();

	public void AddProvider(ILoggerProvider provider)
	{
	}

	public ILogger CreateLogger(string categoryName)
	{
		return VoidLogger.Instance;
	}

	public void Dispose()
	{
	}
}

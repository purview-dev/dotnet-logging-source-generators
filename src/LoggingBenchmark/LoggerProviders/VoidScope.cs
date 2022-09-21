using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoggingBenchmark.LoggerProviders;
public class VoidScope : IDisposable
{
	public static VoidScope Instance => new VoidScope();

	private VoidScope()
	{

	}

	public void Dispose()
	{
	}
}


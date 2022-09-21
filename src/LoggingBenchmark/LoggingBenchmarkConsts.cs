using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoggingBenchmark;

static public class LoggingBenchmarkConsts
{
	public const string TestStartMessage = "TestStart => Started: {Started}";
	public const string TestErrorMessage = "TestError: {StringParam}, {IntParam}";

	public const string StringParamVal = "1111";
	public const int IntParamVal = int.MaxValue;
}

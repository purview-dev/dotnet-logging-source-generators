#pragma warning disable IDE0161 // Convert to file-scoped namespace
using System.Reflection.Emit;
using Microsoft.Extensions.Logging;

namespace LoggerTest
#pragma warning restore IDE0161 // Convert to file-scoped namespace
{
	public interface IScopedTestLogger
	{
		IDisposable LogTest();

		IDisposable LogTester(string hello, int isItMe, bool youre, uint lookingFor);

		[LogEvent(Level = LogLevel.Warning)]
		void SomeThing();

		[LogEvent(Level = LogLevel.Debug)]
		void SomeOtherThing();
	}
}

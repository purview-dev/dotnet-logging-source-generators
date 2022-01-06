using Microsoft.Extensions.Logging;

#pragma warning disable IDE0161 // Convert to file-scoped namespace
namespace LoggerTest.Interfaces
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

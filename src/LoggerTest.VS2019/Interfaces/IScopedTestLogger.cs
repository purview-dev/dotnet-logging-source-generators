using System;
using Microsoft.Extensions.Logging;

namespace LoggerTest.Interfaces
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

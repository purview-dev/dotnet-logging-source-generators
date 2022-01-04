using Microsoft.Extensions.Logging;

#pragma warning disable IDE0161 // Convert to file-scoped namespace
namespace LoggerTest
#pragma warning restore IDE0161 // Convert to file-scoped namespace
{
    public interface ITestLogger
    {
		[LogEvent(Name = "DifferentLogName", Message = "HELLO...!!!")]
        void LogTest();

		void LogTest(string stringParam);

		void LogTest(string stringParam1, string stringParam2, string stringParam3, string stringParam4, string stringParam5, string stringParam6, Exception exception);

        void LogTest(int intParam);

        void LogTest(SomeData someData, Exception exception);

        void LogTest(Exception exception);

        void LogTest(NotImplementedException exception);
    }
}

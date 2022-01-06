using Microsoft.Extensions.Logging;

namespace LoggerTest.Interfaces.ApplicationServices;

public interface IDemoServiceLogs
{
	IDisposable BeginProcessing(Guid contextId);

	void OperationPart1(string aStringParam);

	void OperationPart2(int anIntParam);

	void OperationPart3(SomeData aComplexTypeParam);

	void CompletedProcessing(TimeSpan duration);

	[LogEvent(Level = LogLevel.Warning)]
	void MissingPayload(string name);
}

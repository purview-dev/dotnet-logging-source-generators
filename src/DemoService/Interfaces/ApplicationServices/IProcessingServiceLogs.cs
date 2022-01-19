using DemoService.Models;

namespace DemoService.Interfaces.ApplicationServices;

public interface IProcessingServiceLogs
{
	IDisposable BeginProcessing(Guid contextId, DateTimeOffset startedAt);

	void OperationPart1(string aStringParam);

	void OperationPart2(int anIntParam);

	[LogEvent(Level = LogLevel.Trace)]
	void OperationPart3(SomeData aComplexTypeParam);

	void CompletedProcessing(TimeSpan duration);

	[LogEvent(Level = LogLevel.Warning)]
	void MissingPayload(string name);
}

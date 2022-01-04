namespace LoggerTest;

public interface IBasicLogger
{
	IDisposable BeginProcessing(Guid contextId);

	void OperationPart1(string aStringParam);

	void OperationPart2(int anIntParam);

	void OperationPart3(SomeData aComplexTypeParam);

	void CompletedProcessing(TimeSpan duration);

	void FailedToProcess(Exception ex);
}

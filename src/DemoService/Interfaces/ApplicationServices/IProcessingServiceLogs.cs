﻿using DemoService.Models;

namespace DemoService.Interfaces.ApplicationServices;

[DefaultLogEventSettings(Level = LogLevel.Warning)]
public interface IProcessingServiceLogs
{
	IDisposable BeginProcessing(Guid contextId, DateTimeOffset startedAt);

	void OperationPart1(string aStringParam);

	void OperationPart2(int anIntParam);

	[LogEvent(Level = LogLevel.Trace)]
	void OperationPart3(SomeData aComplexTypeParam);

	void OperationPart4();

	void CompletedProcessing(TimeSpan duration);

	[LogEvent(Level = LogLevel.Warning)]
	void MissingPayload(string name);
}

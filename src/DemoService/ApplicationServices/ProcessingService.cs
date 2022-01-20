using System.Diagnostics;
using DemoService.Models;

namespace DemoService.ApplicationServices;

sealed class ProcessingService : IProcessingService
{
	readonly IProcessingServiceLogs _logs;

	public ProcessingService(IProcessingServiceLogs logs)
	{
		_logs = logs;
	}

	public void Process(Guid contextId, SomeData someData)
	{
		var sw = Stopwatch.StartNew();
		using (_logs.BeginProcessing(contextId, DateTimeOffset.UtcNow))
		{
			if (string.IsNullOrWhiteSpace(someData.Payload))
				_logs.MissingPayload(nameof(someData.Payload));
			else
				_logs.OperationPart1(someData.Payload);

			if (someData.ACount == null)
				_logs.MissingPayload(nameof(someData.ACount));
			else
				_logs.OperationPart2(someData.ACount.Value);

			_logs.OperationPart3(someData);

			_logs.OperationPart4();

			sw.Stop();

			// Super-quick elapsed time...!
			_logs.CompletedProcessing(sw.Elapsed);
		}
	}
}

using System;
using System.Diagnostics;
using LoggerTest.Interfaces.ApplicationServices;

namespace LoggerTest.ApplicationServices
{
	public class DemoService
	{
		readonly IDemoServiceLogs _logs;

		public DemoService(IDemoServiceLogs logs)
		{
			_logs = logs;
		}

		public void Process(Guid contextId, SomeData someData)
		{
			var sw = Stopwatch.StartNew();
			using (_logs.BeginProcessing(contextId))
			{
				if (someData.Payload == null)
					_logs.MissingPayload(nameof(someData.Payload));
				else
					_logs.OperationPart1(someData.Payload);

				if (someData.ACount == null)
					_logs.MissingPayload(nameof(someData.ACount));
				else
					_logs.OperationPart2(someData.ACount.Value);

				_logs.OperationPart3(someData);

				sw.Stop();

				// Super-quick elapsed time...!
				_logs.CompletedProcessing(sw.Elapsed);
			}
		}
	}
}

using DemoService.ApplicationServices;
using DemoService.Interfaces.ApplicationServices;
using DemoService.Models;
using NSubstitute;
using Xunit;

namespace DemoService.UnitTests.cs;

public class ProcessingServiceTests
{
	[Fact]
	public void Process_WhenProcessIsCalled_LogsBeginProcessingWithContext()
	{
		// Arrange
		Guid contextId = Guid.NewGuid();

		IProcessingServiceLogs logs = CreateLogs();
		ProcessingService processingService = CreateProcessingService(logs: logs);
		SomeData someData = new();

		// Act
		processingService.Process(contextId, someData);

		// Assert
		logs
			.Received(1)
			.BeginProcessing(contextId: Arg.Is(contextId));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("     ")]
	public void Process_GivenPayloadIsNull_RaisesMissingPropertyEventWithPayload(string? payload)
	{
		// Arrange
		Guid contextId = Guid.NewGuid();

		IProcessingServiceLogs logs = CreateLogs();
		ProcessingService processingService = CreateProcessingService(logs: logs);
		SomeData someData = new() { Payload = payload };

		// Act
		processingService.Process(contextId, someData);

		// Assert
		logs
			.Received(1)
			.MissingPayload(name: Arg.Is(nameof(SomeData.Payload)));
	}

	[Fact]
	public void Process_GivenPayloadIsValid_RaisesOperationPart1Event()
	{
		// Arrange
		const string payload = "expected-payload";
		Guid contextId = Guid.NewGuid();

		IProcessingServiceLogs logs = CreateLogs();
		ProcessingService processingService = CreateProcessingService(logs: logs);
		SomeData someData = new() { Payload = payload };

		// Act
		processingService.Process(contextId, someData);

		// Assert
		logs
			.Received(1)
			.OperationPart1(aStringParam: Arg.Is(payload));

		// Add this for completeness.
		logs
			.DidNotReceive()
			.MissingPayload(name: Arg.Is(nameof(SomeData.Payload)));
	}

	[Fact]
	public void Process_GivenACountIsNull_RaisesMissingPropertyEventWithACount()
	{
		// Arrange
		Guid contextId = Guid.NewGuid();

		IProcessingServiceLogs logs = CreateLogs();
		ProcessingService processingService = CreateProcessingService(logs: logs);
		SomeData someData = new() { ACount = null };

		// Act
		processingService.Process(contextId, someData);

		// Assert
		logs
			.Received(1)
			.MissingPayload(name: Arg.Is(nameof(SomeData.ACount)));
	}

	[Fact]
	public void Process_GivenSomeDataIsProvided_RaisesOperationPart3Event()
	{
		// Arrange
		Guid contextId = Guid.NewGuid();

		IProcessingServiceLogs logs = CreateLogs();
		ProcessingService processingService = CreateProcessingService(logs: logs);
		SomeData someData = new();

		// Act
		processingService.Process(contextId, someData);

		// Assert
		logs
			.Received(1)
			.OperationPart3(aComplexTypeParam: Arg.Is(someData));
	}

	[Theory]
	[InlineData(1)]
	[InlineData(10)]
	[InlineData(100)]
	[InlineData(1000)]
	public void Process_GivenACountIsValid_RaisesOperationPart2Event(int aCount)
	{
		// Arrange
		Guid contextId = Guid.NewGuid();

		IProcessingServiceLogs logs = CreateLogs();
		ProcessingService processingService = CreateProcessingService(logs: logs);
		SomeData someData = new() { ACount = aCount };

		// Act
		processingService.Process(contextId, someData);

		// Assert
		logs
			.Received(1)
			.OperationPart2(anIntParam: Arg.Is(aCount));

		// Add this for completeness.
		logs
			.DidNotReceive()
			.MissingPayload(name: Arg.Is(nameof(SomeData.ACount)));
	}

	[Fact]
	public void Process_GivenOperationCompletes_RaisesCompletedProcessingEvent()
	{
		// Arrange
		Guid contextId = Guid.NewGuid();

		IProcessingServiceLogs logs = CreateLogs();
		ProcessingService processingService = CreateProcessingService(logs: logs);
		SomeData someData = new();

		// Act
		processingService.Process(contextId, someData);

		// Assert
		logs
			.Received(1)
			.CompletedProcessing(duration: Arg.Any<TimeSpan>());
	}

	static ProcessingService CreateProcessingService(IProcessingServiceLogs? logs = null)
		=> new(logs ?? CreateLogs());

	static IProcessingServiceLogs CreateLogs()
		=> Substitute.For<IProcessingServiceLogs>();
}

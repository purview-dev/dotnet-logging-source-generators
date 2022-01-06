using DemoService.ApplicationServices;
using DemoService.Interfaces.ApplicationServices;
using NSubstitute;
using Xunit;

namespace DemoService.UnitTests.cs;

public class ProcessingServiceTests
{
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

	[Theory]
	[InlineData(1)]
	[InlineData(10)]
	[InlineData(100)]
	[InlineData(1000)]
	public void Process_GivenACountIsValid_DoesNotRaiseMissingProperty(int aCount)
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

		logs
			.DidNotReceive()
			.MissingPayload(name: Arg.Is(nameof(SomeData.ACount)));
	}

	static ProcessingService CreateProcessingService(IProcessingServiceLogs? logs = null)
		=> new(logs ?? CreateLogs());

	static IProcessingServiceLogs CreateLogs()
		=> Substitute.For<IProcessingServiceLogs>();
}


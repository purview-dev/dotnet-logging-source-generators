using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DemoService.Interfaces.ApplicationServices;

public class IProcessingServiceLogsTests
{
	[Fact]
	public void OperationPart1_GivenDefaultIsInterfaceIsWarningAndNoOverrideSet_LogsAsWarning()
	{
		// Arrange
		MockLogger<IProcessingServiceLogs> mockLogger = CreateLogger<IProcessingServiceLogs>();
		IProcessingServiceLogs logs = CreateLogInstance(logs: mockLogger);

		// Act
		logs.OperationPart1("anything");

		// Assert
		mockLogger
			.LastIsEnabled()
			.Should()
			.Be(LogLevel.Warning);
	}

	[Fact]
	public void OperationPart3_GivenLogLevelIsSetOnMethodAndIsTrace_LogsAsTrace()
	{
		// Arrange
		MockLogger<IProcessingServiceLogs> mockLogger = CreateLogger<IProcessingServiceLogs>();
		IProcessingServiceLogs logs = CreateLogInstance(logs: mockLogger);

		// Act
		logs.OperationPart3(new Models.SomeData());

		// Assert
		mockLogger
			.LastIsEnabled()
			.Should()
			.Be(LogLevel.Trace);
	}

	[Fact]
	public void Test_GivenDefaultLogLevelIsOnAssemblyAndIsDebug_LogsAsDebug()
	{
		// Arrange
		MockLogger<IAnotherLog> mockLogger = CreateLogger<IAnotherLog>();
		IAnotherLog logs = CreateLogInstance(logs: mockLogger);

		// Act
		logs.Test();

		// Assert
		mockLogger
			.LastIsEnabled()
			.Should()
			.Be(LogLevel.Debug);
	}

	static MockLogger<T> CreateLogger<T>()
		=> new();

	static T CreateLogInstance<T>(ILogger<T> logs)
	{
		Type t = typeof(T);
		string ns = t.Namespace!;
		string name = string.Concat(t.Name.AsSpan(1), "Core");
		string implementationFullTypeName = $"{ns}.{name}";

		Type implemenationType = t.Assembly.GetType(implementationFullTypeName, true)!;

		return (T)Activator.CreateInstance(implemenationType, args: new[] { logs })!;
	}
}

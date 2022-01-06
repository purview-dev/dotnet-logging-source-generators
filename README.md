# Purview Logging Source Generator

.NET Logging Source Generator, used for generating [LoggerMessage](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage)-based High Performance logging from a custom interface.

The interface-based approach has a few key benefits:

* allows better testing through the use of mocks and assertions in your tests
* interfaces and their methods are also more readable than `LogXXX` and strings.
* natively supports DI.

## How to

Reference the source generator in your CSPROJ file:

```xml
<ItemGroup>
  <PackageReference Include="Purview.Logging.SourceGenerator" Version="0.8.2-prerelease" />
</ItemGroup>
```

Create an `interface` (`public` or `internal`), make sure the name ends with any of the following (**case-sensitive**):

* `Log`
* `Logs`
* `Logger`

Call `services.AddLog<TInterfaceType>()` on your DI registration and you're good to go! Inject or resolve as you see fit.

Currently you must have the `Microsoft.Extensions.DepdencyInjection` and `Microsoft.Extensions.Logging` packages installed along with the `Purview.Logging.SourceGenerator` package in your target project.

## Quick demo:

### Define the interface:

```c#
public interface IProcessingServiceLogs
{
  IDisposable BeginProcessing(Guid contextId);

  void OperationPart1(string aStringParam);

  void OperationPart2(int anIntParam);

  [LogEvent(Level = LogLevel.Trace)]
  void OperationPart3(SomeData aComplexTypeParam);

  void CompletedProcessing(TimeSpan duration);

  [LogEvent(Level = LogLevel.Warning)]
  void MissingPayload(string name);
}
```

Notice here we're also using `IDisposable` for [scoped](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line#log-scopes)-supported logging.

### Register with DI

```c#
services.AddLog<IProcessingServiceLogs>() // this is an auto-generated extension method.
```

### Use... !

```c#
sealed class ProcessingService
{
  readonly IProcessingServiceLogs _logs;

  public ProcessingService(IProcessingServiceLogs logs)
  {
    _logs = logs;
  }

  public void Process(Guid contextId, SomeData someData)
  {
    var sw = Stopwatch.StartNew();
    using (_logs.BeginProcessing(contextId))
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
      
      sw.Stop();
      
      // Super-quick elapsed time...!
      _logs.CompletedProcessing(sw.Elapsed);
    }
  }
}
```

### Testing...!

Full example is in the `DemoService.UnitTests` project, this is just the abridged version. 

It uses the excellent [`xunit`](https://www.nuget.org/packages/xunit/)  and [`NSubstitute`](https://www.nuget.org/packages/NSubstitute/).

```c#
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
```

## Log Event Configuration

By default each assembly where a logging interface is defined get two attributes generated that can be used to control the log event:

1. `DefaultLogLevelAttribute` - use on an interface to control the default log level for all events on that interface, or as an assembly attribute to control the default for all log events within an assembly. If declared on both, the one on the interface takes precedence.
2. `LogEventAttributte` - use to configure individual log events, including their Event Id, Event Name, Log Level and Message Template. If the level is specified, this will overwrite any defined by the `DefaultLogLevelAttribute`.

If no log level is defined (via the `LogEventAttribute`) and the method contains an `Exception` parameter, the level is automatically set to `Error` regardless of other defaults. 

The exception is also passed to the `Exception` parameter of the `Define` method from the `LoggerMessage` class. 

## Extensions

The generated classes are partial, and match the interfaces accessibility modifier (public or internal), their name is the interface name, with the `I` removed and `Core` suffixed to the end - simply as a means of preventing clashes.

It does mean you can extend the class if you really need too:

```
public interface IImportantLogger {  }	// Your interface.

public partial class ImportantLoggerCore : IImportantLogger {} // Generated logger.

partial class ImportantLoggerCore 
{
	public void MyAdditionalMethod()
	{
	     // ... 
	}
}
```

## Notes

This project is very early days - code is very messy at the moment, and it doesn't have much in the way of testing currently. All this is in-part because Source Generators are incredibly hard to debug and test currently. As I get time, I'll improve the codebase and testability of the whole project.

There is a demo project called LoggerTest. It's a bit of a mish-mash at the moment! The DemoService project is nothing more than a few classes and interface to demo the unit testing.

The history of this project was a little interesting, I've been doing this for years, but using C# generated at runtime and creating a dynamic assembly to enable this behaviour. Using Source Generators was a natural step forward.

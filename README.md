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
  <PackageReference Include="Purview.Logging.SourceGenerator" Version="0.9.0-prerelease" />
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

```c#
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
## Performance

As you can see, the performance isn't bad either in this canned example where we compare:

* `ILogger<T>`
* `LoggerMessage.Define` via extension methods
* the interface-based source generation approach

|                                 Method |                  Job |              Runtime | Iterations |         Mean |       Error |      StdDev | Ratio | RatioSD |
|--------------------------------------- |--------------------- |--------------------- |----------- |-------------:|------------:|------------:|------:|--------:|
|                             ILogger<T> |             .NET 5.0 |             .NET 5.0 |          1 |     624.8 ns |    11.67 ns |    11.46 ns |  0.95 |    0.02 |
|                   LoggerMessage.Define |             .NET 5.0 |             .NET 5.0 |          1 |     224.7 ns |     3.82 ns |     3.39 ns |  0.34 |    0.01 |
| 'Interface-based LoggerMessage.Define' |             .NET 5.0 |             .NET 5.0 |          1 |     201.4 ns |     3.65 ns |     3.42 ns |  0.31 |    0.01 |
|                             ILogger<T> |             .NET 6.0 |             .NET 6.0 |          1 |     654.5 ns |    10.54 ns |     9.86 ns |  1.00 |    0.00 |
|                   LoggerMessage.Define |             .NET 6.0 |             .NET 6.0 |          1 |     187.0 ns |     2.80 ns |     2.48 ns |  0.29 |    0.01 |
| 'Interface-based LoggerMessage.Define' |             .NET 6.0 |             .NET 6.0 |          1 |     205.3 ns |     4.04 ns |     3.77 ns |  0.31 |    0.01 |
|                             ILogger<T> |        .NET Core 3.1 |        .NET Core 3.1 |          1 |     989.5 ns |    19.05 ns |    22.68 ns |  1.51 |    0.04 |
|                   LoggerMessage.Define |        .NET Core 3.1 |        .NET Core 3.1 |          1 |     393.7 ns |     4.81 ns |     4.50 ns |  0.60 |    0.01 |
| 'Interface-based LoggerMessage.Define' |        .NET Core 3.1 |        .NET Core 3.1 |          1 |     233.8 ns |     2.97 ns |     2.63 ns |  0.36 |    0.01 |
|                             ILogger<T> | .NET Framework 4.6.2 | .NET Framework 4.6.2 |          1 |   1,288.8 ns |    25.41 ns |    24.95 ns |  1.97 |    0.06 |
|                   LoggerMessage.Define | .NET Framework 4.6.2 | .NET Framework 4.6.2 |          1 |     549.7 ns |     9.85 ns |     8.73 ns |  0.84 |    0.02 |
| 'Interface-based LoggerMessage.Define' | .NET Framework 4.6.2 | .NET Framework 4.6.2 |          1 |     271.3 ns |     5.38 ns |     6.20 ns |  0.41 |    0.01 |
|                             ILogger<T> | .NET Framework 4.7.2 | .NET Framework 4.7.2 |          1 |   1,278.7 ns |    22.31 ns |    20.87 ns |  1.95 |    0.05 |
|                   LoggerMessage.Define | .NET Framework 4.7.2 | .NET Framework 4.7.2 |          1 |     548.0 ns |    10.46 ns |    10.75 ns |  0.84 |    0.02 |
| 'Interface-based LoggerMessage.Define' | .NET Framework 4.7.2 | .NET Framework 4.7.2 |          1 |     271.2 ns |     5.35 ns |     5.00 ns |  0.41 |    0.01 |
|                             ILogger<T> |   .NET Framework 4.8 |   .NET Framework 4.8 |          1 |   1,293.4 ns |    22.32 ns |    20.87 ns |  1.98 |    0.03 |
|                   LoggerMessage.Define |   .NET Framework 4.8 |   .NET Framework 4.8 |          1 |     545.2 ns |     9.68 ns |     9.06 ns |  0.83 |    0.02 |
| 'Interface-based LoggerMessage.Define' |   .NET Framework 4.8 |   .NET Framework 4.8 |          1 |     272.0 ns |     4.15 ns |     3.89 ns |  0.42 |    0.01 |
|                                        |                      |                      |            |              |             |             |       |         |
|                             ILogger<T> |             .NET 5.0 |             .NET 5.0 |         10 |   6,426.7 ns |    92.34 ns |    86.37 ns |  1.00 |    0.02 |
|                   LoggerMessage.Define |             .NET 5.0 |             .NET 5.0 |         10 |   2,154.7 ns |    41.93 ns |    43.06 ns |  0.34 |    0.01 |
| 'Interface-based LoggerMessage.Define' |             .NET 5.0 |             .NET 5.0 |         10 |   2,006.7 ns |    39.11 ns |    40.16 ns |  0.31 |    0.01 |
|                             ILogger<T> |             .NET 6.0 |             .NET 6.0 |         10 |   6,399.3 ns |    84.90 ns |    75.26 ns |  1.00 |    0.00 |
|                   LoggerMessage.Define |             .NET 6.0 |             .NET 6.0 |         10 |   1,854.4 ns |    19.39 ns |    18.14 ns |  0.29 |    0.00 |
| 'Interface-based LoggerMessage.Define' |             .NET 6.0 |             .NET 6.0 |         10 |   1,987.6 ns |    38.18 ns |    42.44 ns |  0.31 |    0.01 |
|                             ILogger<T> |        .NET Core 3.1 |        .NET Core 3.1 |         10 |   9,896.0 ns |   123.61 ns |   115.62 ns |  1.55 |    0.03 |
|                   LoggerMessage.Define |        .NET Core 3.1 |        .NET Core 3.1 |         10 |   3,973.1 ns |    72.35 ns |    67.67 ns |  0.62 |    0.01 |
| 'Interface-based LoggerMessage.Define' |        .NET Core 3.1 |        .NET Core 3.1 |         10 |   2,319.2 ns |    46.19 ns |    49.42 ns |  0.36 |    0.01 |
|                             ILogger<T> | .NET Framework 4.6.2 | .NET Framework 4.6.2 |         10 |  12,813.3 ns |   155.78 ns |   145.72 ns |  2.00 |    0.04 |
|                   LoggerMessage.Define | .NET Framework 4.6.2 | .NET Framework 4.6.2 |         10 |   5,435.8 ns |    60.80 ns |    56.87 ns |  0.85 |    0.01 |
| 'Interface-based LoggerMessage.Define' | .NET Framework 4.6.2 | .NET Framework 4.6.2 |         10 |   2,728.7 ns |    36.24 ns |    30.26 ns |  0.43 |    0.01 |
|                             ILogger<T> | .NET Framework 4.7.2 | .NET Framework 4.7.2 |         10 |  12,801.2 ns |   117.61 ns |   110.02 ns |  2.00 |    0.03 |
|                   LoggerMessage.Define | .NET Framework 4.7.2 | .NET Framework 4.7.2 |         10 |   5,418.8 ns |    56.85 ns |    53.17 ns |  0.85 |    0.01 |
| 'Interface-based LoggerMessage.Define' | .NET Framework 4.7.2 | .NET Framework 4.7.2 |         10 |   2,722.9 ns |    51.31 ns |    59.08 ns |  0.43 |    0.01 |
|                             ILogger<T> |   .NET Framework 4.8 |   .NET Framework 4.8 |         10 |  12,877.3 ns |   165.35 ns |   154.67 ns |  2.02 |    0.04 |
|                   LoggerMessage.Define |   .NET Framework 4.8 |   .NET Framework 4.8 |         10 |   5,415.8 ns |    65.15 ns |    60.94 ns |  0.85 |    0.01 |
| 'Interface-based LoggerMessage.Define' |   .NET Framework 4.8 |   .NET Framework 4.8 |         10 |   2,708.7 ns |    48.58 ns |    45.44 ns |  0.42 |    0.01 |
|                                        |                      |                      |            |              |             |             |       |         |
|                             ILogger<T> |             .NET 5.0 |             .NET 5.0 |        100 |  65,957.6 ns | 1,307.48 ns | 1,605.70 ns |  0.99 |    0.02 |
|                   LoggerMessage.Define |             .NET 5.0 |             .NET 5.0 |        100 |  21,124.5 ns |   291.12 ns |   272.32 ns |  0.32 |    0.01 |
| 'Interface-based LoggerMessage.Define' |             .NET 5.0 |             .NET 5.0 |        100 |  20,083.2 ns |   220.38 ns |   206.14 ns |  0.30 |    0.01 |
|                             ILogger<T> |             .NET 6.0 |             .NET 6.0 |        100 |  66,544.1 ns | 1,285.99 ns | 1,429.37 ns |  1.00 |    0.00 |
|                   LoggerMessage.Define |             .NET 6.0 |             .NET 6.0 |        100 |  18,280.1 ns |   333.64 ns |   312.09 ns |  0.28 |    0.01 |
| 'Interface-based LoggerMessage.Define' |             .NET 6.0 |             .NET 6.0 |        100 |  20,411.3 ns |   303.75 ns |   284.12 ns |  0.31 |    0.01 |
|                             ILogger<T> |        .NET Core 3.1 |        .NET Core 3.1 |        100 |  99,146.2 ns | 1,102.03 ns | 1,030.84 ns |  1.49 |    0.04 |
|                   LoggerMessage.Define |        .NET Core 3.1 |        .NET Core 3.1 |        100 |  39,630.8 ns |   768.35 ns |   718.71 ns |  0.60 |    0.01 |
| 'Interface-based LoggerMessage.Define' |        .NET Core 3.1 |        .NET Core 3.1 |        100 |  23,426.2 ns |   364.09 ns |   322.75 ns |  0.35 |    0.01 |
|                             ILogger<T> | .NET Framework 4.6.2 | .NET Framework 4.6.2 |        100 | 128,325.9 ns | 2,375.83 ns | 2,222.35 ns |  1.93 |    0.04 |
|                   LoggerMessage.Define | .NET Framework 4.6.2 | .NET Framework 4.6.2 |        100 |  54,008.5 ns |   645.62 ns |   539.12 ns |  0.81 |    0.02 |
| 'Interface-based LoggerMessage.Define' | .NET Framework 4.6.2 | .NET Framework 4.6.2 |        100 |  26,966.8 ns |   252.36 ns |   236.06 ns |  0.41 |    0.01 |
|                             ILogger<T> | .NET Framework 4.7.2 | .NET Framework 4.7.2 |        100 | 129,285.9 ns | 2,577.77 ns | 3,068.66 ns |  1.94 |    0.04 |
|                   LoggerMessage.Define | .NET Framework 4.7.2 | .NET Framework 4.7.2 |        100 |  54,440.2 ns |   704.24 ns |   658.75 ns |  0.82 |    0.02 |
| 'Interface-based LoggerMessage.Define' | .NET Framework 4.7.2 | .NET Framework 4.7.2 |        100 |  26,895.6 ns |   450.48 ns |   421.38 ns |  0.41 |    0.01 |
|                             ILogger<T> |   .NET Framework 4.8 |   .NET Framework 4.8 |        100 | 128,481.8 ns | 1,561.31 ns | 1,218.97 ns |  1.94 |    0.04 |
|                   LoggerMessage.Define |   .NET Framework 4.8 |   .NET Framework 4.8 |        100 |  54,080.0 ns |   894.77 ns |   836.97 ns |  0.81 |    0.02 |
| 'Interface-based LoggerMessage.Define' |   .NET Framework 4.8 |   .NET Framework 4.8 |        100 |  26,845.0 ns |   280.51 ns |   262.39 ns |  0.40 |    0.01 |


## Notes

This project is very early days - code is very messy at the moment, and it doesn't have much in the way of testing currently. All this is in-part because Source Generators are incredibly hard to debug and test currently. As I get time, I'll improve the codebase and testability of the whole project.

There is a demo project called LoggerTest. It's a bit of a mish-mash at the moment! The DemoService project is nothing more than a few classes and interface to demo the unit testing.

The history of this project was a little interesting, I've been doing this for years, but using C# generated at runtime and creating a dynamic assembly to enable this behaviour. Using Source Generators was a natural step forward.

# Purview Logging Source Generator

.NET Logging Source Generator, used for generating [LoggerMessage](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage)-based High Performance logging from a custom interface.

## What problem does this solve?

Creating **readable**, **performant**, **testable** logging with **minimum effort**. 

The interface-based approach has a few key benefits:

* better testing through the use of mocks and assertions in your tests
* interfaces and their methods are also more readable than `LogXXX` and strings.
* natively supports DI.

Turns this:

```c#
_logger.LogInformation("Received A Request To Process {State}", e.SomeData);
```

into:

```c#
_logger.ReceivedRequest(e.SomeData);
```

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

Create an `interface` (`public` or `internal`). Log interfaces must end with `Log`, `Logs` or `Logger` (case-sensitive) to be picked up by the source generator.

Notice here we're also using `IDisposable` for [scoped](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line#log-scopes)-supported logging.

### Register with DI

```c#
services.AddLog<IProcessingServiceLogs>() // this is an auto-generated extension method.
```

### ...Log!

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
    // Note here we're using the scoped based log event,
    // so all other logs will contain the contextId. 
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

### How does testing work?

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

### Reference the NuGet package

Reference the appropriate NuGet package in your CSPROJ file:

```xml
<ItemGroup>
  <!-- For VS2022 -->
  <PackageReference Include="Purview.Logging.SourceGenerator" Version="0.9.3-prerelease" />
  <!-- For VS2019 -->
  <PackageReference Include="Purview.Logging.SourceGenerator.VS2019" Version="0.9.3-prerelease" />
</ItemGroup>
```

*Found an issue using VS2019/ .NET 5 SDK that requires a different build of the generator. You may have better luck, but if you encounter issues with `Microsoft.CodeAnalysis.CSharp` version 4 missing then using the VS2019 version.* 

Currently you must have the `Microsoft.Extensions.DepdencyInjection` and `Microsoft.Extensions.Logging` (version 5 or higher) packages installed along with the `Purview.Logging.SourceGenerator` package in your target project.

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

partial class ImportantLoggerCore // Mark your class as file and give it the same name...
{
  public void MyAdditionalMethod()
  {
    // ... 
  }
}
```
## Performance

Using BenchmarkDotNet I've tested the following examples where we compare:

* Direct calls to`ILogger<T>`
* `LoggerMessage.Define` via extension methods
* the interface-based source generation approach

Each test is setup in the following way:

* Has it's own `IServiceProvider` and `ILoggerFactory` to generate `ILogger`s.
* Logging level is set to `Trace`.
* Benchmarking is setup for the following:
  * Iterations: 1, 10, 100
  * Frameworks: net462, net472, net48, netcoreapp3.1, net5.0, net6.0
* Each iteration calls a scoped log event wrapping calls to log events for each level - `Trace` through to `Critical`. 

E.g.

```c#
// This is direct ILogger approach.
using (Logger.BeginScope("TestStart => Started: {Started}", DateTimeOffset.UtcNow))
{
  Logger.LogTrace("TestTrace: {StringParam}, {IntParam}", "A Trace Parameter", 1);
  Logger.LogDebug("TestDebug: {StringParam}, {IntParam}", "A Debug Parameter", 11);
  Logger.LogInformation("LogInformation: {StringParam}, {IntParam}", "A Information Parameter", 111);
  Logger.LogWarning("LogWarning: {StringParam}, {IntParam}", "A Warning Parameter", 1111);
  Logger.LogError("LogError: {StringParam}, {IntParam}", "A Error Parameter", 11111);
  Logger.LogCritical("LogCritical: {StringParam}, {IntParam}", "A Critical Parameter", 111111);
}
```

### BenchmarkDotNet Results 

It appears as though the interface approach is nearly always as fast (or faster depending on the runtime) than the extension method approach, and certainly always faster than directly calling `ILogger`.

This project is available in the repo as the `LoggingBenchmark` project.

```
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
Intel Core i9-10900KF CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
  [Host]               : .NET Framework 4.8 (4.8.4420.0), X64 RyuJIT
  .NET 5.0             : .NET 5.0.13 (5.0.1321.56516), X64 RyuJIT
  .NET 6.0             : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  .NET Framework 4.6.2 : .NET Framework 4.8 (4.8.4420.0), X64 RyuJIT
  .NET Framework 4.7.2 : .NET Framework 4.8 (4.8.4420.0), X64 RyuJIT
  .NET Framework 4.8   : .NET Framework 4.8 (4.8.4420.0), X64 RyuJIT
```

|                  Method |                  Job |              Runtime | Iterations |         Mean |       Error |      StdDev | Ratio | RatioSD |
|------------------------ |--------------------- |--------------------- |----------- |-------------:|------------:|------------:|------:|--------:|
|       Direct:ILogger<T> |             .NET 5.0 |             .NET 5.0 |          1 |     727.7 ns |     8.42 ns |     7.88 ns |  1.02 |    0.02 |
| Extension:LoggerMessage |             .NET 5.0 |             .NET 5.0 |          1 |     298.7 ns |     4.64 ns |     4.34 ns |  0.42 |    0.01 |
| Interface:LoggerMessage |             .NET 5.0 |             .NET 5.0 |          1 |     300.6 ns |     4.54 ns |     4.24 ns |  0.42 |    0.01 |
|       Direct:ILogger<T> |             .NET 6.0 |             .NET 6.0 |          1 |     713.8 ns |    10.44 ns |     9.76 ns |  1.00 |    0.00 |
| Extension:LoggerMessage |             .NET 6.0 |             .NET 6.0 |          1 |     242.8 ns |     3.04 ns |     2.69 ns |  0.34 |    0.01 |
| Interface:LoggerMessage |             .NET 6.0 |             .NET 6.0 |          1 |     256.2 ns |     3.99 ns |     3.73 ns |  0.36 |    0.01 |
|       Direct:ILogger<T> | .NET Framework 4.6.2 | .NET Framework 4.6.2 |          1 |   1,266.6 ns |    15.97 ns |    14.93 ns |  1.77 |    0.04 |
| Extension:LoggerMessage | .NET Framework 4.6.2 | .NET Framework 4.6.2 |          1 |     691.2 ns |     5.16 ns |     4.83 ns |  0.97 |    0.01 |
| Interface:LoggerMessage | .NET Framework 4.6.2 | .NET Framework 4.6.2 |          1 |     392.6 ns |     5.15 ns |     4.57 ns |  0.55 |    0.01 |
|       Direct:ILogger<T> | .NET Framework 4.7.2 | .NET Framework 4.7.2 |          1 |   1,274.8 ns |    17.66 ns |    15.66 ns |  1.79 |    0.04 |
| Extension:LoggerMessage | .NET Framework 4.7.2 | .NET Framework 4.7.2 |          1 |     693.2 ns |     6.65 ns |     5.89 ns |  0.97 |    0.02 |
| Interface:LoggerMessage | .NET Framework 4.7.2 | .NET Framework 4.7.2 |          1 |     393.1 ns |     5.46 ns |     5.11 ns |  0.55 |    0.01 |
|       Direct:ILogger<T> |   .NET Framework 4.8 |   .NET Framework 4.8 |          1 |   1,270.0 ns |    11.16 ns |     9.89 ns |  1.78 |    0.03 |
| Extension:LoggerMessage |   .NET Framework 4.8 |   .NET Framework 4.8 |          1 |     689.6 ns |     6.99 ns |     6.19 ns |  0.97 |    0.02 |
| Interface:LoggerMessage |   .NET Framework 4.8 |   .NET Framework 4.8 |          1 |     383.9 ns |     3.72 ns |     3.48 ns |  0.54 |    0.01 |
|                         |                      |                      |            |              |             |             |       |         |
|       Direct:ILogger<T> |             .NET 5.0 |             .NET 5.0 |         10 |   7,551.2 ns |    89.23 ns |    83.47 ns |  1.11 |    0.02 |
| Extension:LoggerMessage |             .NET 5.0 |             .NET 5.0 |         10 |   2,976.4 ns |    49.82 ns |    46.60 ns |  0.44 |    0.01 |
| Interface:LoggerMessage |             .NET 5.0 |             .NET 5.0 |         10 |   3,057.5 ns |    46.90 ns |    41.58 ns |  0.45 |    0.01 |
|       Direct:ILogger<T> |             .NET 6.0 |             .NET 6.0 |         10 |   6,829.4 ns |   118.51 ns |   110.85 ns |  1.00 |    0.00 |
| Extension:LoggerMessage |             .NET 6.0 |             .NET 6.0 |         10 |   2,409.6 ns |    44.43 ns |    41.56 ns |  0.35 |    0.01 |
| Interface:LoggerMessage |             .NET 6.0 |             .NET 6.0 |         10 |   2,587.5 ns |    50.32 ns |    47.07 ns |  0.38 |    0.01 |
|       Direct:ILogger<T> | .NET Framework 4.6.2 | .NET Framework 4.6.2 |         10 |  12,785.1 ns |   178.99 ns |   158.67 ns |  1.87 |    0.03 |
| Extension:LoggerMessage | .NET Framework 4.6.2 | .NET Framework 4.6.2 |         10 |   6,915.7 ns |    98.57 ns |    87.38 ns |  1.01 |    0.02 |
| Interface:LoggerMessage | .NET Framework 4.6.2 | .NET Framework 4.6.2 |         10 |   3,875.9 ns |    64.67 ns |    60.49 ns |  0.57 |    0.01 |
|       Direct:ILogger<T> | .NET Framework 4.7.2 | .NET Framework 4.7.2 |         10 |  12,655.5 ns |   110.20 ns |   103.08 ns |  1.85 |    0.04 |
| Extension:LoggerMessage | .NET Framework 4.7.2 | .NET Framework 4.7.2 |         10 |   6,923.7 ns |    82.25 ns |    72.91 ns |  1.01 |    0.02 |
| Interface:LoggerMessage | .NET Framework 4.7.2 | .NET Framework 4.7.2 |         10 |   3,887.0 ns |    64.38 ns |    57.07 ns |  0.57 |    0.01 |
|       Direct:ILogger<T> |   .NET Framework 4.8 |   .NET Framework 4.8 |         10 |  12,659.1 ns |   193.24 ns |   171.31 ns |  1.85 |    0.02 |
| Extension:LoggerMessage |   .NET Framework 4.8 |   .NET Framework 4.8 |         10 |   6,887.3 ns |    72.48 ns |    60.52 ns |  1.01 |    0.02 |
| Interface:LoggerMessage |   .NET Framework 4.8 |   .NET Framework 4.8 |         10 |   3,827.9 ns |    62.79 ns |    55.66 ns |  0.56 |    0.01 |
|                         |                      |                      |            |              |             |             |       |         |
|       Direct:ILogger<T> |             .NET 5.0 |             .NET 5.0 |        100 |  71,814.6 ns | 1,410.08 ns | 1,567.30 ns |  1.02 |    0.03 |
| Extension:LoggerMessage |             .NET 5.0 |             .NET 5.0 |        100 |  32,709.5 ns |   468.23 ns |   437.98 ns |  0.46 |    0.01 |
| Interface:LoggerMessage |             .NET 5.0 |             .NET 5.0 |        100 |  30,099.5 ns |   317.69 ns |   297.17 ns |  0.43 |    0.01 |
|       Direct:ILogger<T> |             .NET 6.0 |             .NET 6.0 |        100 |  70,630.2 ns | 1,235.24 ns | 1,155.44 ns |  1.00 |    0.00 |
| Extension:LoggerMessage |             .NET 6.0 |             .NET 6.0 |        100 |  25,182.6 ns |   272.66 ns |   241.70 ns |  0.36 |    0.01 |
| Interface:LoggerMessage |             .NET 6.0 |             .NET 6.0 |        100 |  25,886.0 ns |   228.56 ns |   213.79 ns |  0.37 |    0.01 |
|       Direct:ILogger<T> | .NET Framework 4.6.2 | .NET Framework 4.6.2 |        100 | 124,006.1 ns | 1,251.65 ns | 1,109.55 ns |  1.76 |    0.04 |
| Extension:LoggerMessage | .NET Framework 4.6.2 | .NET Framework 4.6.2 |        100 |  68,775.5 ns |   904.29 ns |   845.87 ns |  0.97 |    0.02 |
| Interface:LoggerMessage | .NET Framework 4.6.2 | .NET Framework 4.6.2 |        100 |  38,525.1 ns |   414.12 ns |   367.11 ns |  0.55 |    0.01 |
|       Direct:ILogger<T> | .NET Framework 4.7.2 | .NET Framework 4.7.2 |        100 | 126,823.3 ns | 2,426.16 ns | 2,269.43 ns |  1.80 |    0.05 |
| Extension:LoggerMessage | .NET Framework 4.7.2 | .NET Framework 4.7.2 |        100 |  68,657.4 ns |   933.39 ns |   827.42 ns |  0.97 |    0.02 |
| Interface:LoggerMessage | .NET Framework 4.7.2 | .NET Framework 4.7.2 |        100 |  38,530.6 ns |   461.48 ns |   431.67 ns |  0.55 |    0.01 |
|       Direct:ILogger<T> |   .NET Framework 4.8 |   .NET Framework 4.8 |        100 | 124,608.3 ns | 1,106.96 ns |   981.29 ns |  1.77 |    0.03 |
| Extension:LoggerMessage |   .NET Framework 4.8 |   .NET Framework 4.8 |        100 |  69,327.0 ns |   860.28 ns |   804.70 ns |  0.98 |    0.01 |
| Interface:LoggerMessage |   .NET Framework 4.8 |   .NET Framework 4.8 |        100 |  38,490.2 ns |   447.05 ns |   396.30 ns |  0.55 |    0.01 |

## Notes

This project is very early days - code is pretty messy at the moment, and it doesn't have much in the way of testing currently. All this is in-part because Source Generators are incredibly hard to debug and test in their current state. As I get time, I'll improve the codebase and testability of the whole project.

There is a demo project called `LoggerTest`. It's a bit of a mish-mash at the moment! The `DemoService` project is nothing more than a few classes and interface to demo how to test the logging interfaces.

The history of this project was a little interesting, I've been doing this for years, but using C# generated at runtime and creating a dynamic assembly to enable this behaviour. Using Source Generators was a natural step forward.

### Unsupported

Currently unsupported are logging interfaces nested in classes, i.e.:

```c#
sealed class ClassWithNestedLogInterface
{
  interface INestedLogger
  {
    void Test();
  }
}
```


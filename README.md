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

It appears as though the interface approach is nearly always fast than the extension method approach, and certainly always faster than directly calling `ILogger`.

```
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
  [Host]               : .NET Framework 4.8 (4.8.4420.0), X64 RyuJIT
  .NET 5.0             : .NET 5.0.13 (5.0.1321.56516), X64 RyuJIT
  .NET 6.0             : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  .NET Core 3.1        : .NET Core 3.1.22 (CoreCLR 4.700.21.56803, CoreFX 4.700.21.57101), X64 RyuJIT
  .NET Framework 4.6.2 : .NET Framework 4.8 (4.8.4420.0), X64 RyuJIT
  .NET Framework 4.7.2 : .NET Framework 4.8 (4.8.4420.0), X64 RyuJIT
  .NET Framework 4.8   : .NET Framework 4.8 (4.8.4420.0), X64 RyuJIT
```

|                  Method |                  Job |              Runtime | Iterations |         Mean |        Error |       StdDev |       Median | Ratio | RatioSD |
|------------------------ |--------------------- |--------------------- |----------- |-------------:|-------------:|-------------:|-------------:|------:|--------:|
|       Direct:ILogger<T> |             .NET 5.0 |             .NET 5.0 |          1 |     956.1 ns |     19.05 ns |     21.17 ns |     955.0 ns |  1.04 |    0.05 |
| Extension:LoggerMessage |             .NET 5.0 |             .NET 5.0 |          1 |     393.3 ns |      7.55 ns |      7.41 ns |     393.2 ns |  0.43 |    0.02 |
| Interface:LoggerMessage |             .NET 5.0 |             .NET 5.0 |          1 |     395.3 ns |      7.81 ns |     10.43 ns |     398.0 ns |  0.43 |    0.02 |
|       Direct:ILogger<T> |             .NET 6.0 |             .NET 6.0 |          1 |     914.3 ns |     17.03 ns |     40.13 ns |     916.6 ns |  1.00 |    0.00 |
| Extension:LoggerMessage |             .NET 6.0 |             .NET 6.0 |          1 |     331.4 ns |      6.60 ns |      6.18 ns |     331.3 ns |  0.36 |    0.01 |
| Interface:LoggerMessage |             .NET 6.0 |             .NET 6.0 |          1 |     346.6 ns |      6.03 ns |      5.03 ns |     346.2 ns |  0.37 |    0.02 |
|       Direct:ILogger<T> |        .NET Core 3.1 |        .NET Core 3.1 |          1 |   1,473.6 ns |     26.55 ns |     24.83 ns |   1,481.0 ns |  1.59 |    0.07 |
| Extension:LoggerMessage |        .NET Core 3.1 |        .NET Core 3.1 |          1 |     727.5 ns |     14.03 ns |     17.74 ns |     730.6 ns |  0.79 |    0.04 |
| Interface:LoggerMessage |        .NET Core 3.1 |        .NET Core 3.1 |          1 |     523.9 ns |     16.49 ns |     46.25 ns |     514.8 ns |  0.57 |    0.06 |
|       Direct:ILogger<T> | .NET Framework 4.6.2 | .NET Framework 4.6.2 |          1 |   2,015.7 ns |     38.97 ns |     94.86 ns |   2,005.1 ns |  2.21 |    0.15 |
| Extension:LoggerMessage | .NET Framework 4.6.2 | .NET Framework 4.6.2 |          1 |   1,033.5 ns |     20.45 ns |     51.67 ns |   1,031.2 ns |  1.14 |    0.08 |
| Interface:LoggerMessage | .NET Framework 4.6.2 | .NET Framework 4.6.2 |          1 |     586.2 ns |     11.71 ns |     24.19 ns |     581.7 ns |  0.64 |    0.04 |
|       Direct:ILogger<T> | .NET Framework 4.7.2 | .NET Framework 4.7.2 |          1 |   1,978.2 ns |     34.90 ns |     53.29 ns |   1,977.7 ns |  2.16 |    0.12 |
| Extension:LoggerMessage | .NET Framework 4.7.2 | .NET Framework 4.7.2 |          1 |   1,017.3 ns |     20.06 ns |     33.52 ns |   1,009.0 ns |  1.11 |    0.07 |
| Interface:LoggerMessage | .NET Framework 4.7.2 | .NET Framework 4.7.2 |          1 |     580.0 ns |     11.25 ns |     13.81 ns |     580.6 ns |  0.63 |    0.03 |
|       Direct:ILogger<T> |   .NET Framework 4.8 |   .NET Framework 4.8 |          1 |   1,991.8 ns |     39.64 ns |     65.12 ns |   1,986.8 ns |  2.18 |    0.10 |
| Extension:LoggerMessage |   .NET Framework 4.8 |   .NET Framework 4.8 |          1 |   1,019.1 ns |     20.19 ns |     42.15 ns |   1,010.8 ns |  1.12 |    0.06 |
| Interface:LoggerMessage |   .NET Framework 4.8 |   .NET Framework 4.8 |          1 |     589.6 ns |     11.61 ns |     21.53 ns |     588.4 ns |  0.65 |    0.04 |
|                         |                      |                      |            |              |              |              |              |       |         |
|       Direct:ILogger<T> |             .NET 5.0 |             .NET 5.0 |         10 |  10,135.7 ns |    201.29 ns |    557.79 ns |  10,096.5 ns |  1.11 |    0.07 |
| Extension:LoggerMessage |             .NET 5.0 |             .NET 5.0 |         10 |   4,284.5 ns |     85.06 ns |    202.16 ns |   4,289.1 ns |  0.45 |    0.01 |
| Interface:LoggerMessage |             .NET 5.0 |             .NET 5.0 |         10 |   4,248.5 ns |     83.86 ns |    180.52 ns |   4,225.1 ns |  0.45 |    0.01 |
|       Direct:ILogger<T> |             .NET 6.0 |             .NET 6.0 |         10 |   9,225.3 ns |    129.51 ns |    101.11 ns |   9,255.6 ns |  1.00 |    0.00 |
| Extension:LoggerMessage |             .NET 6.0 |             .NET 6.0 |         10 |   3,396.3 ns |     64.20 ns |     73.94 ns |   3,406.7 ns |  0.37 |    0.01 |
| Interface:LoggerMessage |             .NET 6.0 |             .NET 6.0 |         10 |   3,451.3 ns |     68.09 ns |    126.21 ns |   3,474.2 ns |  0.37 |    0.01 |
|       Direct:ILogger<T> |        .NET Core 3.1 |        .NET Core 3.1 |         10 |  14,680.5 ns |    283.13 ns |    278.07 ns |  14,654.8 ns |  1.59 |    0.04 |
| Extension:LoggerMessage |        .NET Core 3.1 |        .NET Core 3.1 |         10 |   7,394.2 ns |    147.44 ns |    297.84 ns |   7,357.9 ns |  0.79 |    0.02 |
| Interface:LoggerMessage |        .NET Core 3.1 |        .NET Core 3.1 |         10 |   4,839.2 ns |     94.39 ns |    129.20 ns |   4,771.2 ns |  0.53 |    0.02 |
|       Direct:ILogger<T> | .NET Framework 4.6.2 | .NET Framework 4.6.2 |         10 |  17,982.3 ns |    355.29 ns |    593.61 ns |  17,764.6 ns |  1.96 |    0.07 |
| Extension:LoggerMessage | .NET Framework 4.6.2 | .NET Framework 4.6.2 |         10 |   9,122.3 ns |    181.12 ns |    357.52 ns |   9,217.3 ns |  1.00 |    0.05 |
| Interface:LoggerMessage | .NET Framework 4.6.2 | .NET Framework 4.6.2 |         10 |   5,177.0 ns |    102.01 ns |    158.82 ns |   5,186.2 ns |  0.56 |    0.01 |
|       Direct:ILogger<T> | .NET Framework 4.7.2 | .NET Framework 4.7.2 |         10 |  18,021.2 ns |    355.44 ns |    684.81 ns |  17,775.9 ns |  1.93 |    0.06 |
| Extension:LoggerMessage | .NET Framework 4.7.2 | .NET Framework 4.7.2 |         10 |   9,054.3 ns |    178.13 ns |    197.99 ns |   9,030.9 ns |  0.98 |    0.03 |
| Interface:LoggerMessage | .NET Framework 4.7.2 | .NET Framework 4.7.2 |         10 |   5,150.5 ns |    101.12 ns |    135.00 ns |   5,183.5 ns |  0.56 |    0.02 |
|       Direct:ILogger<T> |   .NET Framework 4.8 |   .NET Framework 4.8 |         10 |  18,063.9 ns |    328.61 ns |    307.38 ns |  18,181.3 ns |  1.96 |    0.03 |
| Extension:LoggerMessage |   .NET Framework 4.8 |   .NET Framework 4.8 |         10 |  12,752.6 ns |  1,638.35 ns |  4,727.01 ns |   9,818.4 ns |  0.99 |    0.05 |
| Interface:LoggerMessage |   .NET Framework 4.8 |   .NET Framework 4.8 |         10 |   5,413.7 ns |    107.74 ns |    154.52 ns |   5,422.6 ns |  0.58 |    0.02 |
|                         |                      |                      |            |              |              |              |              |       |         |
|       Direct:ILogger<T> |             .NET 5.0 |             .NET 5.0 |        100 | 106,258.1 ns |  1,998.95 ns |  4,672.48 ns | 105,718.8 ns |  0.78 |    0.17 |
| Extension:LoggerMessage |             .NET 5.0 |             .NET 5.0 |        100 |  44,809.5 ns |    878.33 ns |  1,418.34 ns |  44,730.8 ns |  0.35 |    0.09 |
| Interface:LoggerMessage |             .NET 5.0 |             .NET 5.0 |        100 |  44,655.0 ns |    884.53 ns |  1,377.11 ns |  44,650.5 ns |  0.35 |    0.10 |
|       Direct:ILogger<T> |             .NET 6.0 |             .NET 6.0 |        100 | 146,016.1 ns | 10,145.55 ns | 27,429.09 ns | 147,172.8 ns |  1.00 |    0.00 |
| Extension:LoggerMessage |             .NET 6.0 |             .NET 6.0 |        100 |  78,746.7 ns |  5,716.02 ns | 16,853.80 ns |  78,688.1 ns |  0.56 |    0.16 |
| Interface:LoggerMessage |             .NET 6.0 |             .NET 6.0 |        100 |  45,796.8 ns |  2,822.26 ns |  8,321.49 ns |  46,332.1 ns |  0.33 |    0.11 |
|       Direct:ILogger<T> |        .NET Core 3.1 |        .NET Core 3.1 |        100 | 146,099.0 ns |  6,170.77 ns | 17,505.45 ns | 141,205.1 ns |  1.06 |    0.32 |
| Extension:LoggerMessage |        .NET Core 3.1 |        .NET Core 3.1 |        100 |  63,755.9 ns |  1,245.27 ns |  1,482.40 ns |  63,690.2 ns |  0.57 |    0.12 |
| Interface:LoggerMessage |        .NET Core 3.1 |        .NET Core 3.1 |        100 |  43,128.8 ns |    834.95 ns |  1,085.68 ns |  43,185.6 ns |  0.37 |    0.09 |
|       Direct:ILogger<T> | .NET Framework 4.6.2 | .NET Framework 4.6.2 |        100 | 167,813.2 ns |  3,337.23 ns |  3,709.32 ns | 168,202.0 ns |  1.53 |    0.32 |
| Extension:LoggerMessage | .NET Framework 4.6.2 | .NET Framework 4.6.2 |        100 |  83,632.8 ns |  1,446.25 ns |  1,352.82 ns |  83,743.7 ns |  0.78 |    0.18 |
| Interface:LoggerMessage | .NET Framework 4.6.2 | .NET Framework 4.6.2 |        100 |  47,880.2 ns |    912.99 ns |    854.01 ns |  48,114.9 ns |  0.45 |    0.10 |
|       Direct:ILogger<T> | .NET Framework 4.7.2 | .NET Framework 4.7.2 |        100 | 167,800.6 ns |  3,338.73 ns |  3,844.88 ns | 168,011.0 ns |  1.52 |    0.33 |
| Extension:LoggerMessage | .NET Framework 4.7.2 | .NET Framework 4.7.2 |        100 |  83,190.6 ns |  1,558.01 ns |  2,025.85 ns |  83,273.8 ns |  0.71 |    0.18 |
| Interface:LoggerMessage | .NET Framework 4.7.2 | .NET Framework 4.7.2 |        100 |  47,548.8 ns |    932.56 ns |    778.73 ns |  47,916.8 ns |  0.48 |    0.06 |
|       Direct:ILogger<T> |   .NET Framework 4.8 |   .NET Framework 4.8 |        100 | 167,651.7 ns |  3,216.27 ns |  4,402.47 ns | 167,050.6 ns |  1.39 |    0.38 |
| Extension:LoggerMessage |   .NET Framework 4.8 |   .NET Framework 4.8 |        100 |  82,504.4 ns |  1,639.97 ns |  1,888.59 ns |  82,270.6 ns |  0.75 |    0.17 |
| Interface:LoggerMessage |   .NET Framework 4.8 |   .NET Framework 4.8 |        100 |  47,673.5 ns |    684.95 ns |    607.19 ns |  47,827.7 ns |  0.46 |    0.09 |


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


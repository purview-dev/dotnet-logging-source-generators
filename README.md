# Purview Logging Source Generator

.NET Logging Source Generator, used for generating [LoggerMessage](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage)-based High Performance logging from a custom interface.

The interface-based approach has a few key benefits:

* allows better testing through the use of mocks and assertions in your tests
* interfaces are also more readable than generate `LogXXX` and strings.
* natively supports DI.

## How to

Reference the source generator in your CSPROJ file:

```xml
<ItemGroup>
	<PackageReference Include="Purview.Logging.SourceGenerator"
					Versions="1.0.0"
    				 OutputItemType="Analyzer"
    				 ReferenceOutputAssembly="false" />
</ItemGroup>
```



Create an interface (public or internal), make sure the name ends with any of the following (case-sensitive):

* Log
* Logs
* Logger

Call `services.AddLog<TInterfaceType>()` on your DI registration and you're good to go! Inject or resolve as you see fit.

Currently you must have the `Microsoft.Extensions.DepdencyInjection` and `Microsoft.Extensions.Logging` packages installed.

## Quick demo:

### Define the interface:

```c#
public interface IBasicLogger
{
	IDisposable BeginProcessing();

	void OperationPart1(string aStringParam);

	void OperationPart2(int anIntParam);

	void OperationPart3(SomeData aComplexTypeParam);

	void CompletedProcessing(TimeSpan duration);

	void FailedToProcess(Exception ex);
}
```

Notice here we're also using `IDisposable` for [scoped]([Logging in .NET | Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line#log-scopes))-supported logging.

### Register with DI

```c#
services.AddLog<ITestLogger>() // this is an auto-generated extension method.
```

### Use... !

```c#
// inject or resolve the IBasicLogger.

var contextId = Guid.NewGuid();
using (logger.BeginProcessing(contextId))
{
	// Do stuff...
	logger.OperationPart1("Some Param...!");

	// Do more stuff...
	logger.OperationPart2(99);

	// Do even more stuff...
	logger.OperationPart3(new SomeData {
		ACount = 1,
		Payload = "abc123"
	});
	
    // Completed...
	logger.CompletedProcessing(TimeSpan.FromSeconds(1.1));

	try
	{
		throw new InvalidOperationException("For Completeness we'll raise this too.");
	}
	catch (Exception ex)
	{
		logger.FailedToProcess(ex);
	}
}
```

### See the output

#### Microsoft.Extensions.Logging.Console

```powershell
info: LoggerTest.IBasicLogger[2]
      => BeginProcessing: fd0d99c9-bbf6-42e9-a90f-e99b5f217a89
      OperationPart1: Some Param...!
info: LoggerTest.IBasicLogger[3]
      => BeginProcessing: fd0d99c9-bbf6-42e9-a90f-e99b5f217a89
      OperationPart2: 99
info: LoggerTest.IBasicLogger[4]
      => BeginProcessing: fd0d99c9-bbf6-42e9-a90f-e99b5f217a89
      OperationPart3: Count: 1 @ 04/01/2022 15:21:29 +00:00: abc123
info: LoggerTest.IBasicLogger[5]
      => BeginProcessing: fd0d99c9-bbf6-42e9-a90f-e99b5f217a89
      CompletedProcessing: 00:00:01.1000000
fail: LoggerTest.IBasicLogger[6]
      => BeginProcessing: fd0d99c9-bbf6-42e9-a90f-e99b5f217a89
      FailedToProcess
      System.InvalidOperationException: For Completeness we'll raise this too.
```

#### Serilog.Extensions.Logger + Serilog.Sinks.Console

```powershell
[15:13:17 INF] OperationPart1: Some Param...!
[15:13:17 INF] OperationPart2: 99
[15:13:17 INF] OperationPart3: Count: 1 @ 04/01/2022 15:21:30 +00:00: abc123
[15:13:17 INF] CompletedProcessing: 00:00:01.1000000
[15:13:17 ERR] FailedToProcess
System.InvalidOperationException: For Completeness we'll raise this too.
```

## Log Event Configuration

By default each assembly where a logging interface is defined get two attributes generated that can be used to control the log event:

1. `DefaultLogLevelAttribute` - use on an interface to control the default log level - system-wide, the default is `Information`.
2. `LogEventAttributte` - use to configure individual log events, including Event Id, Event Name, Log Level and Message Template.

I was hoping to get the `DefaultLogLevelAttribute` working as an  `assembly` attribute too to define the default at the assembly level, but haven't find a way of making this work yet so for now it's on a per-interface basis.

If no log level is defined (via the `LogEventAttribute`) and the method contains an `Exception` parameter, the level is automatically set to `Error`. That parameter is also passed to the `Exception` parameter of the `Define` method of the `LoggerMessage`. 

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

This project is very early days - code is very messy at the moment, and it doesn't have much in the way of testing currently. All this is in-part because Source Generators in incredibly hard to debug currently. As I get time, I'll improve the codebase and testability of the whole project.

The history of this project was a little interesting, I've been doing this for years, but using C# generated at runtime and creating a dynamic assembly to enable this behaviour. Using Source Generators was a natural step forward.
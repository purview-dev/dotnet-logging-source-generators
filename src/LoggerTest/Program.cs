using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace LoggerTest;

partial class Program
{
	static void Main()
	{
		TestLoggers(false);
		TestLoggers(true);
	}

	static void TestLoggers(bool useSerilog)
	{
		var serviceProvider = CreateServiceProvider(useSerilog);

		//Console.WriteLine("UN-SCOPED:");
		All(serviceProvider);

		//Console.WriteLine("SCOPED:");
		//using (var scope = serviceProvider.CreateScope())
		//	All(scope.ServiceProvider);

		//Console.WriteLine("RABBIT:");

		//var rmql = serviceProvider.GetRequiredService<IRabbitMQLogger>();

		//using (rmql.MessageReceived("A Message Id"))
		//{
		//	rmql.Processing("A payload..");

		//	rmql.SuccessfullyProcessedMessage(TimeSpan.FromSeconds(1));

		//	rmql.FailedToProcessMessage(new FileNotFoundException("Just Testing"));
		//}

		Console.ReadLine();
	}

	static void All(IServiceProvider serviceProvider)
	{
		TestIBasicLogger(serviceProvider);

		return;

		TestIFileScopedNSTestLogger(serviceProvider);
		TestIScopedTestLogger(serviceProvider);
		TestITestLogger(serviceProvider);
		TestINestedFileScopedNSTestLogger(serviceProvider);
		TestINestedTestLogger(serviceProvider);
	}

	static void TestIFileScopedNSTestLogger(IServiceProvider serviceProvider)
	{
		var logger = serviceProvider.GetRequiredService<IFileScopedNSTestLogger>();

		logger.LogTest();
	}

	static void TestIScopedTestLogger(IServiceProvider serviceProvider)
	{
		var logger = serviceProvider.GetRequiredService<IScopedTestLogger>();

		using (logger.LogTest())
		{
			logger.SomeThing();
		}

		using (logger.LogTester(hello: "Hello", isItMe: 25, youre: true, lookingFor: 10))
		{
			logger.SomeOtherThing();
		}
	}

	static void TestITestLogger(IServiceProvider serviceProvider)
	{
		var logger = serviceProvider.GetRequiredService<ITestLogger>();

		logger.LogTest();
		logger.LogTest(stringParam: "String Parameter!");
		logger.LogTest(intParam: 99);
		logger.LogTest(someData: new SomeData { ACount = 123, Payload = "ABC_123" }, exception: new Exception("exception...!"));
		logger.LogTest(exception: new Exception("Single Exception"));
		logger.LogTest(exception: new NotImplementedException("Not Implemented...!"));
	}

	static void TestIInternalTestLogger(IServiceProvider serviceProvider)
	{
		var logger = serviceProvider.GetRequiredService<IInternalTestLogger>();

		logger.LogTest();
	}

	static void TestINestedFileScopedNSTestLogger(IServiceProvider serviceProvider)
	{
		var logger = serviceProvider.GetRequiredService<Nested.INestedFileScopedNSTestLogger>();

		logger.LogTest();
	}

	static void TestINestedTestLogger(IServiceProvider serviceProvider)
	{
		var logger = serviceProvider.GetRequiredService<Nested.INestedTestLogger>();

		logger.LogTest();
	}

	static void TestIBasicLogger(IServiceProvider serviceProvider)
	{
		var logger = serviceProvider.GetRequiredService<IBasicLogger>();

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
	}

	static IServiceProvider CreateServiceProvider(bool useSerilog)
	{
		ServiceCollection services = new();

		services
			.AddLogging(a =>
			{
				if (useSerilog)
				{
					var x = new LoggerConfiguration()
						.MinimumLevel
							.Verbose()
						.WriteTo
							.Console()
						.CreateLogger();

					a.AddSerilog(logger: x);
				}
				else
				{
					a.AddSimpleConsole(a =>
					{
						a.IncludeScopes = true;
						a.UseUtcTimestamp = true;
					});

					a.SetMinimumLevel(LogLevel.Trace);
				}
			})
			.AddLog<IRabbitMQLogger>()
			.AddLog<IFileScopedNSTestLogger>()
			.AddLog<IScopedTestLogger>()
			.AddLog<ITestLogger>()
			.AddLog<IInternalTestLogger>()
			.AddLog<IBasicLogger>()
			.AddLog<Nested.INestedFileScopedNSTestLogger>()
			.AddLog<Nested.INestedTestLogger>();

		return services.BuildServiceProvider(new ServiceProviderOptions {
			ValidateOnBuild = true,
			ValidateScopes = true
		});
	}
}

using System;
using LoggerTest.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace LoggerTest
{
	sealed class Program
	{
		static void Main()
		{
			TestLoggers(false);
			TestLoggers(true);
		}

		static void TestLoggers(bool useSerilog)
		{
			Console.WriteLine("==========================================================");
			Console.WriteLine("   Testing {0}Microsoft Extensions Logging", useSerilog ? "Serilog via " : null);
			Console.WriteLine("==========================================================");

			var serviceProvider = CreateServiceProvider(useSerilog);

			Console.WriteLine("   | Testing ServiceProvider non-scoped loggers:");
			Console.WriteLine("==========================================================");

			All(serviceProvider, useSerilog);

			Console.WriteLine("==========================================================");

			Console.WriteLine("   | Testing ServiceProvider scoped loggers:");
			Console.WriteLine("==========================================================");

			using (var scope = serviceProvider.CreateScope())
				All(scope.ServiceProvider, useSerilog);

			Console.WriteLine("==========================================================");
			Console.WriteLine("   Testing complete, press ENTER to continue.");
			Console.ReadLine();
		}

		static void All(IServiceProvider serviceProvider, bool usingSeriLog)
		{
			TestILogger(serviceProvider, usingSeriLog);
			TestIBasicLogger(serviceProvider);
			TestIInternalTestLogger(serviceProvider);
			TestIFileScopedNSTestLogger(serviceProvider);
			TestIScopedTestLogger(serviceProvider);
			TestITestLogger(serviceProvider);
			TestINestedFileScopedNSTestLogger(serviceProvider);
			TestINestedTestLogger(serviceProvider);
		}

		static void TestILogger(IServiceProvider serviceProvider, bool usingSeriLog)
		{
			var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

			var levels = new[] {
			LogLevel.Trace,
			LogLevel.Debug,
			LogLevel.Information,
			LogLevel.Warning,
			LogLevel.Error,
			LogLevel.Critical
		};

			using (logger.BeginScope("Testing ILogger<Program>, Logger Type: {LoggerType}Microsoft.Extensions.Logging", usingSeriLog ? "Serilog via " : string.Empty))
			{
				foreach (var level in levels)
				{
					logger.Log(level, "Logging {Level} Output: A Guid Parameter : {GuidParameter}", level, Guid.NewGuid());
				}
			}
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
			var logger = serviceProvider.GetRequiredService<Interfaces.Nested.INestedFileScopedNSTestLogger>();

			logger.LogTest();
		}

		static void TestINestedTestLogger(IServiceProvider serviceProvider)
		{
			var logger = serviceProvider.GetRequiredService<Interfaces.Nested.INestedTestLogger>();

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
#if NETCOREAPP3_1 || NET462
					a.AddConsole(a => a.IncludeScopes = true);
#else
					a.AddSimpleConsole(a =>
					{
						a.IncludeScopes = true;
						a.UseUtcTimestamp = true;
					});
#endif

					a.SetMinimumLevel(LogLevel.Trace);
					}
				})
				.AddLog<IFileScopedNSTestLogger>()
				.AddLog<IScopedTestLogger>()
				.AddLog<ITestLogger>()
				.AddLog<IInternalTestLogger>()
				.AddLog<IBasicLogger>()
				.AddLog<Interfaces.Nested.INestedFileScopedNSTestLogger>()
				.AddLog<Interfaces.Nested.INestedTestLogger>();

			return services.BuildServiceProvider(new ServiceProviderOptions {
				ValidateOnBuild = true,
				ValidateScopes = true
			});
		}
	}
}

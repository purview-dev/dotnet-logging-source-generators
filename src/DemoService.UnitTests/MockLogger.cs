using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DemoService;

sealed class MockLogger<T> : ILogger<T>
{
	readonly Stack<ReceivedLogEvent> _events = new();
	readonly Stack<LogLevel> _enabledCheck = new();
	readonly Dictionary<LogLevel, bool> _enabled;
	readonly bool _defaultEnabled;

	public MockLogger(Dictionary<LogLevel, bool>? enabled = null, bool defaultEnabled = false)
	{
		_enabled = enabled ?? new();
		_defaultEnabled = defaultEnabled;
	}

	public IDisposable BeginScope<TState>(TState state)
		=> Substitute.For<IDisposable>();

	public bool IsEnabled(LogLevel logLevel)
	{
		_enabledCheck.Push(logLevel);

		if (_enabled.ContainsKey(logLevel))
			return _enabled[logLevel];

		return _defaultEnabled;
	}

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) 
		=> _events.Push(new() { Level = logLevel, Message = state?.ToString() });

	public void ReceivedOnce(LogLevel level, string message)
	{
		var matchedEventsCount = _events.Count(e => e.Level == level && e.Message == message);

		if (matchedEventsCount != 1)
		{
			throw new Exception($"Expected one call to Log with the following arguments: {level}, \"{message}\". Actual received count: {matchedEventsCount}");
		}
	}

	public LogLevel LastIsEnabled()
		=> _enabledCheck.Pop();
}

sealed class ReceivedLogEvent
{
	public LogLevel Level { get; set; }

	public string? Message { get; set; }
}

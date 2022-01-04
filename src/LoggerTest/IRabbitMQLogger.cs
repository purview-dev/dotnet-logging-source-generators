using Microsoft.Extensions.Logging;

namespace LoggerTest;

[DefaultLogLevel(LogLevel.Trace)]
public interface IRabbitMQLogger
{
	IDisposable MessageReceived(string messageId);

	void Processing(string somePayload);

	void FailedToProcessMessage(Exception exception);

	void SuccessfullyProcessedMessage(TimeSpan duration);
}

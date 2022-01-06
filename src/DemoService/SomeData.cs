namespace DemoService;

public class SomeData
{
	public DateTimeOffset When { get; set; } = DateTimeOffset.UtcNow;

	public string? Payload { get; set; }

	public int? ACount { get; set; }

	override public string ToString()
		=> $"Count: {ACount} @ {When}: {Payload ?? "<null>"}";
}

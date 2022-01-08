namespace Purview.Logging.SourceGenerator.Emitters;

readonly record struct LoggerSetttings(int? EventId, string? Name, string? Level, string? Message);

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Logging.SourceGenerator;

sealed class LoggerMessageSyntaxReceiver : ISyntaxReceiver
{
	readonly static string[] _suffixes = new[] { "Log", "Logs", "Logger" };

	List<InterfaceDeclarationSyntax>? _candidateInterfaces;

	public IEnumerable<InterfaceDeclarationSyntax>? CandidateInterfaces => _candidateInterfaces;

	public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
	{
		// Only classes
		if (syntaxNode is InterfaceDeclarationSyntax interfaceDeclaration)
		{
			if (!_suffixes.Any(suffix => interfaceDeclaration.Identifier.ValueText.EndsWith(suffix, StringComparison.Ordinal)))
				return;

			// Match add to candidates
			_candidateInterfaces ??= new List<InterfaceDeclarationSyntax>();
			_candidateInterfaces.Add(interfaceDeclaration);
		}
	}
}

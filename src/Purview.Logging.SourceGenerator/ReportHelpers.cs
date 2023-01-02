using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Logging.SourceGenerator;

static class ReportHelpers
{
	const string _category = "Purview.Logging";

	static public void ReportUnableToDetermineExceptionParameter(Action<Diagnostic> reportDiagnostic, Location location, int methodParameterCount, string exceptionParameterName)
	{
		reportDiagnostic(Diagnostic.Create(
			new DiagnosticDescriptor(
				GenerateId(1),
				"Unable to determine exception parameter",
				"Out of the {0} parameter(s), unable to determine the exception parameter named '{1}'.",
				_category,
				DiagnosticSeverity.Warning,
				true),
			location,
			messageArgs: new object[] { methodParameterCount, exceptionParameterName })
		);
	}

	static public void ReportInvalidLogMethodReturnType(Action<Diagnostic> reportDiagnostic, MethodDeclarationSyntax methodDeclarationSyntax)
	{
		reportDiagnostic(Diagnostic.Create(
			new DiagnosticDescriptor(
				GenerateId(2),
				"Invalid log method return type.",
				"{0} is not a valid return type, only void or {1} are valid.",
				_category,
				DiagnosticSeverity.Error,
				true),
			methodDeclarationSyntax.GetLocation(),
			messageArgs: new object[] { methodDeclarationSyntax.ReturnType, Helpers.IDisposableType })
		);
	}

	static public void ReportPropertyExistsOnLoggerInterface(Action<Diagnostic> reportDiagnostic, PropertyDeclarationSyntax propertyDeclaration)
	{
		reportDiagnostic(Diagnostic.Create(
			new DiagnosticDescriptor(
				GenerateId(3),
				"Properties are not valid on logger interfaces.",
				"Properties are not supported on logger interfaces, please remove the {0} property.",
				_category,
				DiagnosticSeverity.Warning,
				true),
			propertyDeclaration.GetLocation(),
			messageArgs: new object[] { propertyDeclaration.Identifier })
		);
	}

	static public void ReportMaximumNumberOfParmaetersExceeded(Action<Diagnostic> reportDiagnostic, Location location, string methodName, int parameterCount)
	{
		reportDiagnostic(Diagnostic.Create(
			new DiagnosticDescriptor(
				GenerateId(4),
				"Maximum number of parameters exceeded.",
				"{0} is the maximum number of parameters allowed, {1} has {2} (excluding the last exception, if one exists).",
				_category,
				DiagnosticSeverity.Error,
				true),
			location,
			messageArgs: new object[] { Helpers.MaximumLoggerDefineParameters, methodName, parameterCount })
		);
	}

	static public void ReportNoLogEventsGenerated(Action<Diagnostic> reportDiagnostic, InterfaceDeclarationSyntax interfaceDeclaration)
	{
		reportDiagnostic(Diagnostic.Create(
			new DiagnosticDescriptor(
				GenerateId(5),
				"No logging events were generated.",
				"There were no events generated on the logger interface {0}. Make sure there are correctly defined methods to implement.",
				_category,
				DiagnosticSeverity.Warning,
				true),
			interfaceDeclaration.GetLocation(),
			messageArgs: new object[] { interfaceDeclaration.Identifier.ToString() })
		);
	}

	static string GenerateId(int id)
		=> "PVL" + $"{id}".PadLeft(4, '0');
}

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class StrictCompatibilityAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.AvoidStrictCompatibilityProperty);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleAssignmentExpression);
        }

        private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignmentExpression = (AssignmentExpressionSyntax)context.Node;
            if (!FastMoqAnalysisHelpers.TryGetPropertySymbol(assignmentExpression.Left, context.SemanticModel, context.CancellationToken, out var property) ||
                property is null ||
                !FastMoqAnalysisHelpers.IsFastMoqMockerProperty(property, "Strict"))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.AvoidStrictCompatibilityProperty,
                FastMoqAnalysisHelpers.GetTargetNameLocation(assignmentExpression.Left)));
        }
    }
}
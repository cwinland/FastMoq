using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TrackedMockResetAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.UseProviderFirstReset);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;
            if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess || memberAccess.Name.Identifier.ValueText != "Reset")
            {
                return;
            }

            if (!FastMoqAnalysisHelpers.TryResolveTrackedMockOrigin(memberAccess.Expression, context.SemanticModel, context.CancellationToken, out var origin))
            {
                return;
            }

            var replacement = FastMoqAnalysisHelpers.BuildResetReplacement(origin, context.SemanticModel, invocationExpression.SpanStart);
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UseProviderFirstReset,
                memberAccess.Name.GetLocation(),
                replacement));
        }
    }
}
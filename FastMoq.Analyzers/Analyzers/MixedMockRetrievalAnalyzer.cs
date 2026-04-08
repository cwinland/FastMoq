using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MixedMockRetrievalAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.UseConsistentMockRetrieval);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax) context.Node;
            if (!FastMoqAnalysisHelpers.IsSafeMixedRetrievalCandidate(invocationExpression, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            var root = invocationExpression.SyntaxTree.GetRoot(context.CancellationToken);
            if (!FastMoqAnalysisHelpers.ContainsGetOrCreateMock(root, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UseConsistentMockRetrieval,
                memberAccess.Name.GetLocation()));
        }
    }
}
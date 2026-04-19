using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace FastMoq.Analyzers.Analyzers
{
    /// <summary>
    /// Detects Moq matcher helpers inside provider-first Verify expressions and steers them toward FastArg.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ProviderFirstVerifyMatcherAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.UseFastArgMatcherInProviderFirstVerify,
            DiagnosticDescriptors.AvoidUnsupportedMoqMatcherInProviderFirstVerify);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax) context.Node;
            if (!FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, context.SemanticModel, context.CancellationToken, out var method) ||
                method is null ||
                !FastMoqAnalysisHelpers.IsMoqItMethod(method) ||
                !IsInsideProviderFirstVerifyExpression(invocationExpression, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            var matcherText = invocationExpression.WithoutTrivia().ToString();
            if (FastMoqAnalysisHelpers.TryBuildFastArgMatcherReplacement(invocationExpression, context.SemanticModel, context.CancellationToken, out var replacement))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UseFastArgMatcherInProviderFirstVerify,
                    invocationExpression.GetLocation(),
                    replacement,
                    matcherText));
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.AvoidUnsupportedMoqMatcherInProviderFirstVerify,
                invocationExpression.GetLocation(),
                matcherText));
        }

        private static bool IsInsideProviderFirstVerifyExpression(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, System.Threading.CancellationToken cancellationToken)
        {
            foreach (var ancestorInvocation in invocationExpression.Ancestors().OfType<InvocationExpressionSyntax>())
            {
                if (FastMoqAnalysisHelpers.TryGetProviderFirstVerifyExpressionArgument(ancestorInvocation, semanticModel, cancellationToken, out var expressionArgument) &&
                    expressionArgument.Span.Contains(invocationExpression.Span))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class VerifyLoggerAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.UseVerifyLogged);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;
            if (!FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, context.SemanticModel, context.CancellationToken, out var method) || method is null)
            {
                return;
            }

            if (!FastMoqAnalysisHelpers.IsFastMoqVerifyLogger(method))
            {
                return;
            }

            if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            if (!FastMoqAnalysisHelpers.TryResolveTrackedMockOrigin(memberAccess.Expression, context.SemanticModel, context.CancellationToken, out var origin))
            {
                return;
            }

            if (!FastMoqAnalysisHelpers.TryBuildVerifyLoggedReplacement(origin, context.SemanticModel, invocationExpression, context.CancellationToken, out var replacement))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UseVerifyLogged,
                memberAccess.Name.GetLocation(),
                replacement));
        }
    }
}
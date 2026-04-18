using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class BareTrackedVerifyAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.AvoidBareTrackedVerify);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax) context.Node;
            if (invocationExpression.ArgumentList.Arguments.Count != 0 ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, context.SemanticModel, context.CancellationToken, out var method) ||
                method is null ||
                !FastMoqAnalysisHelpers.IsMoqVerifyMethod(method) ||
                !FastMoqAnalysisHelpers.TryResolveTrackedMockOrigin(memberAccess.Expression, context.SemanticModel, context.CancellationToken, out _))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.AvoidBareTrackedVerify,
                memberAccess.Name.GetLocation()));
        }
    }
}
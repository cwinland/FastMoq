using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class KeyedDependencyAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.PreserveKeyedServiceDistinctness);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax) context.Node;
            if (!FastMoqAnalysisHelpers.TryGetUnkeyedDependencyCandidate(invocationExpression, context.SemanticModel, context.CancellationToken, out var serviceType, out var apiName) ||
                serviceType is null)
            {
                return;
            }

            var root = invocationExpression.SyntaxTree.GetRoot(context.CancellationToken);
            if (FastMoqAnalysisHelpers.DocumentContainsKeyedRegistration(root, context.SemanticModel, serviceType, context.CancellationToken) ||
                !FastMoqAnalysisHelpers.TryGetTargetTypeWithDuplicateKeyedDependency(invocationExpression, context.SemanticModel, serviceType, context.CancellationToken, out var targetTypeName))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PreserveKeyedServiceDistinctness,
                FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                targetTypeName,
                serviceType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                apiName));
        }
    }
}
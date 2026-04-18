using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ServiceProviderShimAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.PreferTypedServiceProviderHelpers,
            DiagnosticDescriptors.PreferFunctionContextExecutionHelpers);

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
                method is null)
            {
                return;
            }

            if (FastMoqAnalysisHelpers.TryGetTypedServiceProviderHelperSuggestion(invocationExpression, context.SemanticModel, context.CancellationToken, out var typedHelperApi))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.PreferTypedServiceProviderHelpers,
                    FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                    typedHelperApi));
                return;
            }

            if (FastMoqAnalysisHelpers.HasFunctionContextInvocationIdMockHelper(context.SemanticModel) &&
                FastMoqAnalysisHelpers.TryGetFunctionContextInvocationIdHelperSuggestion(invocationExpression, context.SemanticModel, context.CancellationToken, out var invocationIdApi))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.PreferFunctionContextExecutionHelpers,
                    FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                    invocationIdApi));
                return;
            }

            if (!FastMoqAnalysisHelpers.HasFunctionContextInstanceServicesMockHelper(context.SemanticModel) ||
                !FastMoqAnalysisHelpers.TryGetFunctionContextInstanceServicesHelperSuggestion(invocationExpression, context.SemanticModel, context.CancellationToken, out var functionContextApi))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PreferTypedServiceProviderHelpers,
                FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                functionContextApi));
        }
    }
}
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class WebHelperAuthoringAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.PreferWebTestHelpers);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ImplicitObjectCreationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax) context.Node;
            if (!FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, context.SemanticModel, context.CancellationToken, out var method) ||
                method is null ||
                !FastMoqAnalysisHelpers.TryGetFastMoqWebHelperSuggestion(method, out var helperName, out var setupKind))
            {
                return;
            }

            if (context.ContainingSymbol?.ContainingAssembly?.Name == "FastMoq.Web")
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PreferWebTestHelpers,
                FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                helperName,
                setupKind));
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var expression = (ExpressionSyntax) context.Node;
            if (!FastMoqAnalysisHelpers.TryGetRawWebHelperSuggestion(expression, context.SemanticModel, context.CancellationToken, out var helperName, out var setupKind))
            {
                return;
            }

            if (context.ContainingSymbol?.ContainingAssembly?.Name == "FastMoq.Web")
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PreferWebTestHelpers,
                expression.GetLocation(),
                helperName,
                setupKind));
        }
    }
}
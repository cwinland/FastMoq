using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MissingHelperPackageAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.ReferenceFastMoqHelperPackage);

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
            if (context.ContainingSymbol?.ContainingAssembly?.Name != "FastMoq.Web" &&
                !FastMoqAnalysisHelpers.HasWebHelperPackage(context.SemanticModel) &&
                FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, context.SemanticModel, context.CancellationToken, out var webMethod) &&
                webMethod is not null &&
                FastMoqAnalysisHelpers.TryGetFastMoqWebHelperSuggestion(webMethod, out var webHelperName, out _))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ReferenceFastMoqHelperPackage,
                    FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                    webHelperName,
                    "FastMoq.Web",
                    "FastMoq.Web.Extensions"));
                return;
            }

            if (context.ContainingSymbol?.ContainingAssembly?.Name != "FastMoq.AzureFunctions" &&
                !FastMoqAnalysisHelpers.HasFunctionContextInvocationIdMockHelper(context.SemanticModel) &&
                FastMoqAnalysisHelpers.TryGetFunctionContextInvocationIdHelperSuggestion(invocationExpression, context.SemanticModel, context.CancellationToken, out _))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ReferenceFastMoqHelperPackage,
                    FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                    "AddFunctionContextInvocationId(...)",
                    "FastMoq.AzureFunctions",
                    "FastMoq.AzureFunctions.Extensions"));
                return;
            }

            if (context.ContainingSymbol?.ContainingAssembly?.Name == "FastMoq.AzureFunctions" ||
                FastMoqAnalysisHelpers.HasFunctionContextInstanceServicesMockHelper(context.SemanticModel) ||
                !FastMoqAnalysisHelpers.TryGetFunctionContextInstanceServicesHelperSuggestion(invocationExpression, context.SemanticModel, context.CancellationToken, out _))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ReferenceFastMoqHelperPackage,
                FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                "AddFunctionContextInstanceServices(...)",
                "FastMoq.AzureFunctions",
                "FastMoq.AzureFunctions.Extensions"));
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var expression = (ExpressionSyntax) context.Node;
            if (context.ContainingSymbol?.ContainingAssembly?.Name == "FastMoq.Web" ||
                FastMoqAnalysisHelpers.HasWebHelperPackage(context.SemanticModel) ||
                !FastMoqAnalysisHelpers.TryGetRawWebHelperSuggestion(expression, context.SemanticModel, context.CancellationToken, out var helperName, out _))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ReferenceFastMoqHelperPackage,
                expression.GetLocation(),
                helperName,
                "FastMoq.Web",
                "FastMoq.Web.Extensions"));
        }
    }
}
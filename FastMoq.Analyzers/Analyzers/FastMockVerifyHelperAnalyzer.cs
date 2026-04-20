using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FastMockVerifyHelperAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.AvoidFastMockVerifyHelperWrappers,
            DiagnosticDescriptors.AvoidProviderSpecificFastMockVerifyHelperWrappers);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax) context.Node;
            if (context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken) is not IMethodSymbol methodSymbol ||
                methodSymbol.Name != "Verify" ||
                methodSymbol.Parameters.Length == 0 ||
                !FastMoqAnalysisHelpers.IsFastMoqFastMockType(methodSymbol.Parameters[0].Type))
            {
                return;
            }

            var wrapperKind = FastMoqAnalysisHelpers.GetFastMockVerifyWrapperKind(methodSymbol, context.SemanticModel, context.CancellationToken);
            var descriptor = wrapperKind switch
            {
                FastMockVerifyWrapperKind.FastMoqBoundary => DiagnosticDescriptors.AvoidFastMockVerifyHelperWrappers,
                FastMockVerifyWrapperKind.ProviderSpecific => DiagnosticDescriptors.AvoidProviderSpecificFastMockVerifyHelperWrappers,
                _ => null,
            };

            if (descriptor is null)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                methodDeclaration.Identifier.GetLocation(),
                methodSymbol.Name));
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax) context.Node;
            if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, context.SemanticModel, context.CancellationToken, out var methodSymbol) ||
                methodSymbol is null)
            {
                return;
            }

            var wrapperKind = FastMoqAnalysisHelpers.GetFastMockVerifyWrapperKind(methodSymbol, context.SemanticModel, context.CancellationToken);
            var descriptor = wrapperKind switch
            {
                FastMockVerifyWrapperKind.FastMoqBoundary => DiagnosticDescriptors.AvoidFastMockVerifyHelperWrappers,
                FastMockVerifyWrapperKind.ProviderSpecific => DiagnosticDescriptors.AvoidProviderSpecificFastMockVerifyHelperWrappers,
                _ => null,
            };

            if (descriptor is null)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                memberAccess.Name.GetLocation(),
                methodSymbol.Name));
        }
    }
}
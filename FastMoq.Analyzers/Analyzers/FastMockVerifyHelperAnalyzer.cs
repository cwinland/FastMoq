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

            var wrapperKind = GetWrappedVerifyKind(methodDeclaration, methodSymbol.Parameters[0], context.SemanticModel, context.CancellationToken);
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

        private static FastMockVerifyWrapperKind GetWrappedVerifyKind(MethodDeclarationSyntax methodDeclaration, IParameterSymbol fastMockParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var analysisRoot = methodDeclaration.ExpressionBody?.Expression ?? (SyntaxNode?) methodDeclaration.Body;
            if (analysisRoot is null)
            {
                return FastMockVerifyWrapperKind.None;
            }

            var hasMoqVerify = false;
            var hasAsMoqOnFastMock = false;
            var hasDefaultVerifyOnFastMock = false;

            foreach (var invocationExpression in analysisRoot.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
            {
                if (IsDefaultVerifyOnFastMock(invocationExpression, fastMockParameter, semanticModel, cancellationToken))
                {
                    hasDefaultVerifyOnFastMock = true;
                    continue;
                }

                if (IsAsMoqOnFastMock(invocationExpression, fastMockParameter, semanticModel, cancellationToken))
                {
                    hasAsMoqOnFastMock = true;
                    continue;
                }

                if (FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) &&
                    method is not null &&
                    FastMoqAnalysisHelpers.IsMoqVerifyMethod(method))
                {
                    hasMoqVerify = true;
                }
            }

            if (hasAsMoqOnFastMock && hasMoqVerify)
            {
                return FastMockVerifyWrapperKind.ProviderSpecific;
            }

            if (hasDefaultVerifyOnFastMock)
            {
                return FastMockVerifyWrapperKind.FastMoqBoundary;
            }

            return FastMockVerifyWrapperKind.None;
        }

        private static bool IsAsMoqOnFastMock(InvocationExpressionSyntax invocationExpression, IParameterSymbol fastMockParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            return method.Name == "AsMoq" &&
                method.ContainingNamespace.ToDisplayString() == FastMoqAnalysisHelpers.MoqProviderNamespace &&
                ReferencesParameter(memberAccess.Expression, fastMockParameter, semanticModel, cancellationToken);
        }

        private static bool IsDefaultVerifyOnFastMock(InvocationExpressionSyntax invocationExpression, IParameterSymbol fastMockParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            return method.Name == "Verify" &&
                IsDefaultMockingProviderAccess(memberAccess.Expression, semanticModel, cancellationToken) &&
                invocationExpression.ArgumentList.Arguments.Count > 0 &&
                ReferencesParameter(invocationExpression.ArgumentList.Arguments[0].Expression, fastMockParameter, semanticModel, cancellationToken);
        }

        private static bool IsDefaultMockingProviderAccess(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            semanticModel = FastMoqAnalysisHelpers.GetSemanticModelForNode(expression, semanticModel);
            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
            var property = symbolInfo.Symbol as IPropertySymbol ?? symbolInfo.CandidateSymbols.OfType<IPropertySymbol>().FirstOrDefault();

            return property is not null &&
                property.Name == "Default" &&
                property.IsStatic &&
                property.ContainingType.ToDisplayString() == FastMoqAnalysisHelpers.MockingProviderRegistryTypeName;
        }

        private static bool ReferencesParameter(ExpressionSyntax expression, IParameterSymbol parameterSymbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(FastMoqAnalysisHelpers.Unwrap(expression), cancellationToken);
            var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
            return SymbolEqualityComparer.Default.Equals(symbol, parameterSymbol);
        }

        private enum FastMockVerifyWrapperKind
        {
            None,
            FastMoqBoundary,
            ProviderSpecific,
        }
    }
}
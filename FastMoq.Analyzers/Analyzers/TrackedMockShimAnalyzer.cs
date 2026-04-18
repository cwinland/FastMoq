using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TrackedMockShimAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.AvoidTrackedMockShimAlias);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeVariableDeclarator, Microsoft.CodeAnalysis.CSharp.SyntaxKind.VariableDeclarator);
            context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, Microsoft.CodeAnalysis.CSharp.SyntaxKind.PropertyDeclaration);
        }

        private static void AnalyzeVariableDeclarator(SyntaxNodeAnalysisContext context)
        {
            var variableDeclarator = (VariableDeclaratorSyntax) context.Node;
            if (variableDeclarator.Initializer?.Value is not ExpressionSyntax initializer ||
                context.SemanticModel.GetDeclaredSymbol(variableDeclarator, context.CancellationToken) is not ISymbol symbol ||
                !TryGetMockType(symbol, out _, context.CancellationToken) ||
                !FastMoqAnalysisHelpers.TryResolveTrackedMockOrigin(initializer, context.SemanticModel, context.CancellationToken, out _) ||
                !HasVerificationOnlyUsage(symbol, variableDeclarator, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.AvoidTrackedMockShimAlias,
                variableDeclarator.Identifier.GetLocation(),
                symbol.Name));
        }

        private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax) context.Node;
            if (context.SemanticModel.GetDeclaredSymbol(propertyDeclaration, context.CancellationToken) is not IPropertySymbol propertySymbol ||
                !TryGetMockType(propertySymbol, out _, context.CancellationToken) ||
                !TryGetPropertyInitializer(propertyDeclaration, out var initializer) ||
                !FastMoqAnalysisHelpers.TryResolveTrackedMockOrigin(initializer, context.SemanticModel, context.CancellationToken, out _) ||
                !HasVerificationOnlyUsage(propertySymbol, propertyDeclaration, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.AvoidTrackedMockShimAlias,
                propertyDeclaration.Identifier.GetLocation(),
                propertySymbol.Name));
        }

        private static bool TryGetMockType(ISymbol symbol, out ITypeSymbol mockedType, CancellationToken cancellationToken)
        {
            mockedType = symbol switch
            {
                ILocalSymbol localSymbol when FastMoqAnalysisHelpers.TryGetMoqMockedType(localSymbol.Type, out var localType) => localType,
                IFieldSymbol fieldSymbol when FastMoqAnalysisHelpers.TryGetMoqMockedType(fieldSymbol.Type, out var fieldType) => fieldType,
                IPropertySymbol propertySymbol when FastMoqAnalysisHelpers.TryGetMoqMockedType(propertySymbol.Type, out var propertyType) => propertyType,
                _ => null!,
            };

            return mockedType is not null;
        }

        private static bool TryGetPropertyInitializer(PropertyDeclarationSyntax propertyDeclaration, out ExpressionSyntax initializer)
        {
            if (propertyDeclaration.ExpressionBody?.Expression is ExpressionSyntax expressionBody)
            {
                initializer = expressionBody;
                return true;
            }

            if (propertyDeclaration.Initializer?.Value is ExpressionSyntax propertyInitializer)
            {
                initializer = propertyInitializer;
                return true;
            }

            initializer = null!;
            return false;
        }

        private static bool HasVerificationOnlyUsage(ISymbol symbol, SyntaxNode declarationNode, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var scope = declarationNode.AncestorsAndSelf().FirstOrDefault(ancestor =>
                            symbol is ILocalSymbol
                                ? ancestor is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax
                                : ancestor is TypeDeclarationSyntax)
                        ?? declarationNode.SyntaxTree.GetRoot(cancellationToken);

            var hasVerifyUsage = false;
            foreach (var expression in scope.DescendantNodes().OfType<ExpressionSyntax>())
            {
                var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
                var referencedSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
                if (!SymbolEqualityComparer.Default.Equals(referencedSymbol, symbol))
                {
                    continue;
                }

                if (expression.Parent is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression == expression &&
                    memberAccess.Parent is InvocationExpressionSyntax invocationExpression &&
                    FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) &&
                    method is not null &&
                    FastMoqAnalysisHelpers.IsMoqVerifyMethod(method))
                {
                    hasVerifyUsage = true;
                    continue;
                }

                return false;
            }

            return hasVerifyUsage;
        }
    }
}
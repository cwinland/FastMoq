using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Threading;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnnecessaryMockerTestBaseHelperIndirectionAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.UnnecessaryMockerTestBaseHelperIndirection);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, Microsoft.CodeAnalysis.CSharp.SyntaxKind.PropertyDeclaration);
        }

        private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax) context.Node;
            if (context.SemanticModel.GetDeclaredSymbol(propertyDeclaration, context.CancellationToken) is not IPropertySymbol propertySymbol ||
                propertySymbol.IsStatic ||
                propertySymbol.Name is not "Component" and not "Mocks" ||
                propertyDeclaration.Parent is not TypeDeclarationSyntax containingType ||
                !FastMoqAnalysisHelpers.TryGetDirectMockerTestBaseInheritanceCandidate(containingType, context.SemanticModel, context.CancellationToken, out var candidate) ||
                !TryGetHelperForwardingMemberName(propertyDeclaration, context.SemanticModel, context.CancellationToken, candidate.HelperMember, out var helperMemberName))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UnnecessaryMockerTestBaseHelperIndirection,
                propertyDeclaration.Identifier.GetLocation(),
                propertySymbol.Name,
                helperMemberName));
        }

        private static bool TryGetHelperForwardingMemberName(PropertyDeclarationSyntax propertyDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken, ISymbol helperMember, out string helperMemberName)
        {
            if (!TryGetPropertyReturnExpression(propertyDeclaration, out var expression) ||
                expression is not MemberAccessExpressionSyntax memberAccess ||
                semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).Symbol is not { } accessSymbol ||
                !SymbolEqualityComparer.Default.Equals(accessSymbol, helperMember))
            {
                helperMemberName = string.Empty;
                return false;
            }

            helperMemberName = memberAccess.Name.Identifier.ValueText;
            return true;
        }

        private static bool TryGetPropertyReturnExpression(PropertyDeclarationSyntax propertyDeclaration, out ExpressionSyntax expression)
        {
            if (propertyDeclaration.ExpressionBody is not null)
            {
                expression = propertyDeclaration.ExpressionBody.Expression;
                return true;
            }

            if (propertyDeclaration.AccessorList?.Accessors.Count == 1 && propertyDeclaration.AccessorList.Accessors[0].Keyword.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.GetKeyword))
            {
                var getter = propertyDeclaration.AccessorList.Accessors[0];
                if (getter.ExpressionBody is not null)
                {
                    expression = getter.ExpressionBody.Expression;
                    return true;
                }

                if (getter.Body?.Statements.Count == 1 && getter.Body.Statements[0] is ReturnStatementSyntax returnStatement && returnStatement.Expression is not null)
                {
                    expression = returnStatement.Expression;
                    return true;
                }
            }

            expression = default!;
            return false;
        }
    }
}
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
            if (!FastMoqAnalysisHelpers.TryGetPropertyReturnExpression(propertyDeclaration, out var expression))
            {
                helperMemberName = string.Empty;
                return false;
            }

            expression = FastMoqAnalysisHelpers.Unwrap(expression);
            if (expression is not MemberAccessExpressionSyntax memberAccess ||
                semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).Symbol is not { } accessSymbol ||
                !SymbolEqualityComparer.Default.Equals(accessSymbol, helperMember))
            {
                helperMemberName = string.Empty;
                return false;
            }

            helperMemberName = memberAccess.Name.Identifier.ValueText;
            return true;
        }
    }
}
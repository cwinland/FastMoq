using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RawMockCreationAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.AvoidRawMockCreationInFastMoqSuites);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ImplicitObjectCreationExpression);
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var expression = (ExpressionSyntax) context.Node;
            if (!HasNoArguments(expression) ||
                !FastMoqAnalysisHelpers.TryGetCreatedMoqMockedType(expression, context.SemanticModel, context.CancellationToken, out var serviceType) ||
                !FastMoqAnalysisHelpers.IsInsideFastMoqTestInfrastructure(expression, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            var guidance = FastMoqAnalysisHelpers.GetRawMockCreationGuidance(expression, serviceType, context.SemanticModel, context.CancellationToken);
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.AvoidRawMockCreationInFastMoqSuites,
                expression.GetLocation(),
                expression.WithoutTrivia().ToString(),
                guidance));
        }

        private static bool HasNoArguments(ExpressionSyntax expression)
        {
            return expression switch
            {
                ObjectCreationExpressionSyntax objectCreationExpression => objectCreationExpression.ArgumentList?.Arguments.Count is null or 0,
                ImplicitObjectCreationExpressionSyntax implicitObjectCreationExpression => implicitObjectCreationExpression.ArgumentList?.Arguments.Count is null or 0,
                _ => false,
            };
        }
    }
}
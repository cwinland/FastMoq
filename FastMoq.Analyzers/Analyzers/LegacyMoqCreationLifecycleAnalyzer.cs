using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LegacyMoqCreationLifecycleAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.AvoidLegacyMockCreationAndLifecycleApis);

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
                method is null ||
                !TryGetGuidance(method, out var guidance))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.AvoidLegacyMockCreationAndLifecycleApis,
                invocationExpression.Expression.GetLocation(),
                method.Name,
                guidance));
        }

        private static bool TryGetGuidance(IMethodSymbol method, out string guidance)
        {
            guidance = string.Empty;

            if (!FastMoqAnalysisHelpers.IsFastMoqMockerMethod(method, method.Name))
            {
                return false;
            }

            switch (method.Name)
            {
                case "CreateMock":
                    guidance = "'GetOrCreateMock(...)' for tracked provider-first mocks";
                    return true;

                case "CreateMockInstance":
                case "CreateDetachedMock":
                    guidance = "'GetOrCreateMock(...)' on a dedicated Mocker instance when possible, or provider-specific escape hatches only when raw Mock<T> is still required";
                    return true;

                case "AddMock":
                    guidance = "'GetOrCreateMock<T>()' for tracked mocks or 'AddType<T>(...)' for concrete instances";
                    return true;

                case "RemoveMock":
                    guidance = "provider-neutral mock lifecycle patterns instead of adding and removing legacy Mock instances";
                    return true;

                default:
                    return false;
            }
        }
    }
}
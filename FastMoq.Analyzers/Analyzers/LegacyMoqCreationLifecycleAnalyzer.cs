using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Threading;

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
                !TryGetGuidance(invocationExpression, context.SemanticModel, context.CancellationToken, method, out var guidance))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.AvoidLegacyMockCreationAndLifecycleApis,
                invocationExpression.Expression.GetLocation(),
                method.Name,
                guidance));
        }

        private static bool TryGetGuidance(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, IMethodSymbol method, out string guidance)
        {
            guidance = string.Empty;

            if (!FastMoqAnalysisHelpers.IsFastMoqMockerMethod(method, method.Name))
            {
                return false;
            }

            var serviceTypeName = TryGetServiceTypeName(invocationExpression, semanticModel, method, cancellationToken);

            switch (method.Name)
            {
                case "CreateMock":
                    guidance = serviceTypeName is null
                        ? "'GetOrCreateMock(...)' for tracked provider-first mocks"
                        : $"'GetOrCreateMock<{serviceTypeName}>()' for tracked provider-first mocks";
                    return true;

                case "CreateMockInstance":
                case "CreateDetachedMock":
                    guidance = serviceTypeName is null
                        ? "'CreateStandaloneFastMock<T>()' or 'MockingProviderRegistry.Default.CreateMock<T>()' for detached provider-first handles, and 'GetOrCreateMock(...)' only when the collaborator should stay tracked in the parent Mocker"
                        : $"'CreateStandaloneFastMock<{serviceTypeName}>()' or 'MockingProviderRegistry.Default.CreateMock<{serviceTypeName}>()' for detached provider-first handles, and 'GetOrCreateMock<{serviceTypeName}>()' only when the collaborator should stay tracked in the parent Mocker";
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

        private static string? TryGetServiceTypeName(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, IMethodSymbol method, CancellationToken cancellationToken)
        {
            method = method.ReducedFrom ?? method;
            if (method.TypeArguments.Length == 1)
            {
                return FastMoqAnalysisHelpers.GetMinimalTypeName(method.TypeArguments[0], semanticModel, invocationExpression.SpanStart);
            }

            if (invocationExpression.ArgumentList.Arguments.Count > 0 &&
                invocationExpression.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax typeOfExpression)
            {
                var serviceType = semanticModel.GetTypeInfo(typeOfExpression.Type, cancellationToken).Type;
                if (serviceType is not null)
                {
                    return FastMoqAnalysisHelpers.GetMinimalTypeName(serviceType, semanticModel, invocationExpression.SpanStart);
                }
            }

            return null;
        }
    }
}
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Threading;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LegacyMoqOnboardingAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.RequireExplicitMoqOnboarding);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(RegisterCompilationAnalysis);
        }

        private static void RegisterCompilationAnalysis(CompilationStartAnalysisContext context)
        {
            var moqResolvedAsDefaultProvider = FastMoqAnalysisHelpers.IsProviderSelectedByDefault(context.Compilation, FastMoqAnalysisHelpers.MoqProviderName, CancellationToken.None);
            var hasMoqProviderPackage = FastMoqAnalysisHelpers.HasMoqProviderPackage(context.Compilation);

            context.RegisterSyntaxNodeAction(nodeContext => AnalyzeInvocation(nodeContext, moqResolvedAsDefaultProvider, hasMoqProviderPackage), Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(nodeContext => AnalyzeMemberAccess(nodeContext, moqResolvedAsDefaultProvider, hasMoqProviderPackage), Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, bool moqResolvedAsDefaultProvider, bool hasMoqProviderPackage)
        {
            var invocationExpression = (InvocationExpressionSyntax) context.Node;
            if (!TryGetLegacyMoqApi(invocationExpression, context.SemanticModel, context.CancellationToken, out var apiName) ||
                IsMoqCompatibilityAvailable(invocationExpression, context.SemanticModel, context.CancellationToken, moqResolvedAsDefaultProvider, hasMoqProviderPackage))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.RequireExplicitMoqOnboarding,
                FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                apiName));
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context, bool moqResolvedAsDefaultProvider, bool hasMoqProviderPackage)
        {
            var memberAccessExpression = (MemberAccessExpressionSyntax) context.Node;
            if (!TryGetLegacyMoqApi(memberAccessExpression, context.SemanticModel, context.CancellationToken, out var apiName) ||
                IsMoqCompatibilityAvailable(memberAccessExpression, context.SemanticModel, context.CancellationToken, moqResolvedAsDefaultProvider, hasMoqProviderPackage))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.RequireExplicitMoqOnboarding,
                memberAccessExpression.Name.GetLocation(),
                apiName));
        }

        private static bool IsMoqCompatibilityAvailable(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken, bool moqResolvedAsDefaultProvider, bool hasMoqProviderPackage)
        {
            return hasMoqProviderPackage &&
                (moqResolvedAsDefaultProvider || FastMoqAnalysisHelpers.HasProviderSelectionInScope(node, semanticModel, FastMoqAnalysisHelpers.MoqProviderName, cancellationToken));
        }

        private static bool TryGetLegacyMoqApi(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string apiName)
        {
            apiName = string.Empty;
            if (!FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) || method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (FastMoqAnalysisHelpers.IsFastMoqVerifyLogger(method))
            {
                apiName = "VerifyLogger(...)";
                return true;
            }

            if (method.ContainingType.ToDisplayString() != FastMoqAnalysisHelpers.FastMoqMockerTypeName)
            {
                return false;
            }

            apiName = method.Name switch
            {
                "GetMock" => "GetMock<T>()",
                "GetRequiredMock" => "GetRequiredMock(...)",
                "CreateMock" => "CreateMock<T>(...)",
                "CreateMockInstance" => "CreateMockInstance<T>(...)",
                "CreateDetachedMock" => "CreateDetachedMock<T>(...)",
                _ => string.Empty,
            };

            return apiName.Length > 0;
        }

        private static bool TryGetLegacyMoqApi(MemberAccessExpressionSyntax memberAccessExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string apiName)
        {
            apiName = string.Empty;
            if (!FastMoqAnalysisHelpers.TryGetPropertySymbol(memberAccessExpression, semanticModel, cancellationToken, out var property) || property is null)
            {
                return false;
            }

            if (property.Name != "Mock")
            {
                return false;
            }

            if (!FastMoqAnalysisHelpers.IsFastMoqMockModelType(property.ContainingType))
            {
                return false;
            }

            apiName = "MockModel.Mock";
            return true;
        }
    }
}

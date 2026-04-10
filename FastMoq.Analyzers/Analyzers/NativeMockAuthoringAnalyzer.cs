using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NativeMockAuthoringAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.PreferTypedProviderExtensions);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax) context.Node;
            if (!FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, context.SemanticModel, context.CancellationToken, out var method) ||
                method is null ||
                !FastMoqAnalysisHelpers.IsFastMoqMockerMethod(method, "GetNativeMock") ||
                method.TypeArguments.Length != 1 ||
                !FastMoqAnalysisHelpers.TryGetSingleProviderNamespacePreference(invocationExpression, out var providerName, out var providerExtensionName))
            {
                return;
            }

            var preferredAccess = providerName == "moq"
                ? "GetOrCreateMock<T>().AsMoq()"
                : "GetOrCreateMock<T>().AsNSubstitute()";

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PreferTypedProviderExtensions,
                FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                preferredAccess,
                "GetNativeMock<T>()",
                providerName));
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccessExpression = (MemberAccessExpressionSyntax) context.Node;
            if (!FastMoqAnalysisHelpers.TryGetPropertySymbol(memberAccessExpression, context.SemanticModel, context.CancellationToken, out var property) ||
                property is null ||
                !FastMoqAnalysisHelpers.IsFastMoqNativeMockProperty(property) ||
                !FastMoqAnalysisHelpers.TryGetSingleProviderNamespacePreference(memberAccessExpression, out var providerName, out var providerExtensionName))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PreferTypedProviderExtensions,
                memberAccessExpression.Name.GetLocation(),
                providerExtensionName,
                "NativeMock",
                providerName));
        }
    }
}
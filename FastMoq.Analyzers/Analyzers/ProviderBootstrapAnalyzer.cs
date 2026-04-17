using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Threading;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ProviderBootstrapAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.SelectProviderBeforeProviderSpecificApi);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(RegisterCompilationAnalysis);
        }

        private static void RegisterCompilationAnalysis(CompilationStartAnalysisContext context)
        {
            var moqSelectedByDefault = FastMoqAnalysisHelpers.IsProviderSelectedByDefault(context.Compilation, FastMoqAnalysisHelpers.MoqProviderName, CancellationToken.None);
            var nsubstituteSelectedByDefault = FastMoqAnalysisHelpers.IsProviderSelectedByDefault(context.Compilation, FastMoqAnalysisHelpers.NSubstituteProviderName, CancellationToken.None);

            context.RegisterSyntaxNodeAction(nodeContext => AnalyzeInvocation(nodeContext, moqSelectedByDefault, nsubstituteSelectedByDefault), Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(nodeContext => AnalyzeMemberAccess(nodeContext, moqSelectedByDefault, nsubstituteSelectedByDefault), Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, bool moqSelectedByDefault, bool nsubstituteSelectedByDefault)
        {
            var invocationExpression = (InvocationExpressionSyntax) context.Node;
            if (!FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, context.SemanticModel, context.CancellationToken, out var method) ||
                method is null ||
                !FastMoqAnalysisHelpers.TryGetRequiredProvider(method, out var providerName, out var apiName))
            {
                return;
            }

            if ((providerName == FastMoqAnalysisHelpers.MoqProviderName && moqSelectedByDefault) ||
                (providerName == FastMoqAnalysisHelpers.NSubstituteProviderName && nsubstituteSelectedByDefault) ||
                FastMoqAnalysisHelpers.HasProviderSelectionInScope(invocationExpression, context.SemanticModel, providerName, context.CancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.SelectProviderBeforeProviderSpecificApi,
                FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                apiName,
                providerName));
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context, bool moqSelectedByDefault, bool nsubstituteSelectedByDefault)
        {
            var memberAccessExpression = (MemberAccessExpressionSyntax) context.Node;
            if (!FastMoqAnalysisHelpers.TryGetPropertySymbol(memberAccessExpression, context.SemanticModel, context.CancellationToken, out var property) ||
                property is null ||
                !FastMoqAnalysisHelpers.TryGetRequiredProvider(property, out var providerName, out var apiName))
            {
                return;
            }

            if ((providerName == FastMoqAnalysisHelpers.MoqProviderName && moqSelectedByDefault) ||
                (providerName == FastMoqAnalysisHelpers.NSubstituteProviderName && nsubstituteSelectedByDefault) ||
                FastMoqAnalysisHelpers.HasProviderSelectionInScope(memberAccessExpression, context.SemanticModel, providerName, context.CancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.SelectProviderBeforeProviderSpecificApi,
                memberAccessExpression.Name.GetLocation(),
                apiName,
                providerName));
        }
    }
}
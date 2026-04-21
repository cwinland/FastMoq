using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FastMoq.Analyzers.Analyzers
{
    /// <summary>
    /// Detects tracked ILogger setup callbacks that only mirror normalized log output and can move to Mocker.SetupLoggerCallback(...).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LoggerSetupCallbackAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.PreferSetupLoggerCallbackHelper);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax) context.Node;
            if (!FastMoqAnalysisHelpers.TryBuildSetupLoggerCallbackReplacement(invocationExpression, context.SemanticModel, context.CancellationToken, out _))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PreferSetupLoggerCallbackHelper,
                FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                "SetupLoggerCallback(...)"));
        }
    }
}

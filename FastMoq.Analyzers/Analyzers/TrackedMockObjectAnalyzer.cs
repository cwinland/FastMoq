using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TrackedMockObjectAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.UseProviderFirstObjectAccess);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            if (memberAccess.Name.Identifier.ValueText != "Object")
            {
                return;
            }

            if (!FastMoqAnalysisHelpers.TryResolveTrackedMockOrigin(memberAccess.Expression, context.SemanticModel, context.CancellationToken, out var origin))
            {
                return;
            }

            var replacement = FastMoqAnalysisHelpers.BuildObjectAccessReplacement(origin, context.SemanticModel, memberAccess.SpanStart);
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UseProviderFirstObjectAccess,
                memberAccess.Name.GetLocation(),
                replacement));
        }
    }
}
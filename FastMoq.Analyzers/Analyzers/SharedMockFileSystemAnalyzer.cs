using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SharedMockFileSystemAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.PreferSharedMockFileSystem);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreationExpression = (ObjectCreationExpressionSyntax) context.Node;
            if (objectCreationExpression.ArgumentList?.Arguments.Count > 0 ||
                !FastMoqAnalysisHelpers.ShouldPreferSharedMockFileSystem(objectCreationExpression, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PreferSharedMockFileSystem,
                objectCreationExpression.GetLocation(),
                objectCreationExpression.WithoutTrivia().ToString()));
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccessExpression = (MemberAccessExpressionSyntax) context.Node;
            if (memberAccessExpression.Name.Identifier.ValueText != "FileSystem" ||
                memberAccessExpression.Expression is not ObjectCreationExpressionSyntax objectCreationExpression ||
                objectCreationExpression.ArgumentList?.Arguments.Count > 0 ||
                !FastMoqAnalysisHelpers.ShouldPreferSharedMockFileSystem(memberAccessExpression, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PreferSharedMockFileSystem,
                memberAccessExpression.Name.GetLocation(),
                memberAccessExpression.WithoutTrivia().ToString()));
        }
    }
}
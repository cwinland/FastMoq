using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DirectMockerTestBaseInheritanceAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.DirectMockerTestBaseInheritance);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax) context.Node;
            if (!FastMoqAnalysisHelpers.TryGetDirectMockerTestBaseInheritanceCandidate(classDeclaration, context.SemanticModel, context.CancellationToken, out var candidate))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DirectMockerTestBaseInheritance,
                classDeclaration.Identifier.GetLocation(),
                candidate.OuterType.Name,
                candidate.TargetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                candidate.HelperType.Name));
        }
    }
}
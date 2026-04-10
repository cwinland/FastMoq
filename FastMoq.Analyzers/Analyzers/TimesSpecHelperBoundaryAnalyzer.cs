using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TimesSpecHelperBoundaryAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.UseTimesSpecAtHelperBoundary);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeParameter, Microsoft.CodeAnalysis.CSharp.SyntaxKind.Parameter);
        }

        private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
        {
            var parameter = (ParameterSyntax) context.Node;
            if (parameter.Type is null || !IsHelperBoundaryParameter(parameter))
            {
                return;
            }

            var type = context.SemanticModel.GetTypeInfo(parameter.Type, context.CancellationToken).Type;
            if (type is null || !FastMoqAnalysisHelpers.IsTimesLikeType(type))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UseTimesSpecAtHelperBoundary,
                parameter.Type.GetLocation(),
                type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }

        private static bool IsHelperBoundaryParameter(ParameterSyntax parameter)
        {
            return parameter.Parent?.Parent is MethodDeclarationSyntax or LocalFunctionStatementSyntax or DelegateDeclarationSyntax;
        }
    }
}
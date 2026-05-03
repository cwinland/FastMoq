using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MissingHelperPackageAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.ReferenceFastMoqHelperPackage);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(RegisterCompilationAnalysis);
        }

        private static void RegisterCompilationAnalysis(CompilationStartAnalysisContext context)
        {
            var packageMatrix = FastMoqAnalysisHelpers.GetGeneratedTestPackageMatrix(context.Compilation);
            var assemblyName = context.Compilation.AssemblyName;
            var hasWebHelperAssembly = assemblyName == FastMoqAnalysisHelpers.FastMoqWebAssemblyName;
            var hasAzureFunctionsHelperAssembly = assemblyName == FastMoqAnalysisHelpers.FastMoqAzureFunctionsAssemblyName;

            context.RegisterSyntaxNodeAction(nodeContext => AnalyzeInvocation(nodeContext, packageMatrix, hasWebHelperAssembly, hasAzureFunctionsHelperAssembly), Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(nodeContext => AnalyzeObjectCreation(nodeContext, packageMatrix, hasWebHelperAssembly), Microsoft.CodeAnalysis.CSharp.SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(nodeContext => AnalyzeObjectCreation(nodeContext, packageMatrix, hasWebHelperAssembly), Microsoft.CodeAnalysis.CSharp.SyntaxKind.ImplicitObjectCreationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, FastMoqGeneratedTestPackageMatrix packageMatrix, bool hasWebHelperAssembly, bool hasAzureFunctionsHelperAssembly)
        {
            var invocationExpression = (InvocationExpressionSyntax) context.Node;
            if (!hasWebHelperAssembly &&
                !packageMatrix.HasWebHelpers &&
                FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, context.SemanticModel, context.CancellationToken, out var webMethod) &&
                webMethod is not null &&
                FastMoqAnalysisHelpers.TryGetFastMoqWebHelperSuggestion(webMethod, out var webHelperName, out _))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ReferenceFastMoqHelperPackage,
                    FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                    webHelperName,
                    FastMoqAnalysisHelpers.FastMoqWebAssemblyName,
                    "FastMoq.Web.Extensions"));
                return;
            }

            if (!hasAzureFunctionsHelperAssembly &&
                !packageMatrix.HasAzureFunctionsHelpers &&
                FastMoqAnalysisHelpers.TryGetFunctionContextInvocationIdHelperSuggestion(invocationExpression, context.SemanticModel, context.CancellationToken, out _))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ReferenceFastMoqHelperPackage,
                    FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                    "AddFunctionContextInvocationId(...)",
                    FastMoqAnalysisHelpers.FastMoqAzureFunctionsAssemblyName,
                    "FastMoq.AzureFunctions.Extensions"));
                return;
            }

            if (hasAzureFunctionsHelperAssembly ||
                packageMatrix.HasAzureFunctionsHelpers ||
                !FastMoqAnalysisHelpers.TryGetFunctionContextInstanceServicesHelperSuggestion(invocationExpression, context.SemanticModel, context.CancellationToken, out _))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ReferenceFastMoqHelperPackage,
                FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                "AddFunctionContextInstanceServices(...)",
                FastMoqAnalysisHelpers.FastMoqAzureFunctionsAssemblyName,
                "FastMoq.AzureFunctions.Extensions"));
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context, FastMoqGeneratedTestPackageMatrix packageMatrix, bool hasWebHelperAssembly)
        {
            var expression = (ExpressionSyntax) context.Node;
            if (hasWebHelperAssembly ||
                packageMatrix.HasWebHelpers ||
                !FastMoqAnalysisHelpers.TryGetRawWebHelperSuggestion(expression, context.SemanticModel, context.CancellationToken, out var helperName, out _))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ReferenceFastMoqHelperPackage,
                expression.GetLocation(),
                helperName,
                FastMoqAnalysisHelpers.FastMoqWebAssemblyName,
                "FastMoq.Web.Extensions"));
        }
    }
}
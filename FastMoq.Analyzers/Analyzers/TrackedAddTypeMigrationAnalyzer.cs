using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace FastMoq.Analyzers.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TrackedAddTypeMigrationAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.PreserveTrackedResolutionDuringAddTypeMigration);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax) context.Node;
            if (!TryGetDiagnosticArguments(invocationExpression, context.SemanticModel, context.CancellationToken, out var serviceTypeName, out var conflictingApi))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PreserveTrackedResolutionDuringAddTypeMigration,
                FastMoqAnalysisHelpers.GetTargetNameLocation(invocationExpression.Expression),
                serviceTypeName,
                conflictingApi));
        }

        private static bool TryGetDiagnosticArguments(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string serviceTypeName, out string conflictingApi)
        {
            serviceTypeName = string.Empty;
            conflictingApi = string.Empty;

            if (!FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (!FastMoqAnalysisHelpers.IsFastMoqMockerAddTypeMethod(method) ||
                !TryGetRegisteredServiceType(invocationExpression, method, semanticModel, cancellationToken, out var serviceType) ||
                invocationExpression.ArgumentList.Arguments.Count == 0 ||
                !IsTrackedReplacementOrigin(invocationExpression.ArgumentList.Arguments[0].Expression, semanticModel, cancellationToken, serviceType, new HashSet<ISymbol>(SymbolEqualityComparer.Default)) ||
                !TryFindTrackedSensitiveUsage(invocationExpression, semanticModel, cancellationToken, serviceType, out conflictingApi))
            {
                return false;
            }

            serviceTypeName = serviceType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            return true;
        }

        private static bool TryGetRegisteredServiceType(InvocationExpressionSyntax invocationExpression, IMethodSymbol method, SemanticModel semanticModel, CancellationToken cancellationToken, out ITypeSymbol serviceType)
        {
            serviceType = null!;

            if (method.TypeArguments.Length > 0)
            {
                serviceType = method.TypeArguments[0];
                return true;
            }

            if (invocationExpression.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            var firstArgument = FastMoqAnalysisHelpers.Unwrap(invocationExpression.ArgumentList.Arguments[0].Expression);
            if (firstArgument is not TypeOfExpressionSyntax typeOfExpression)
            {
                return false;
            }

            serviceType = semanticModel.GetTypeInfo(typeOfExpression.Type, cancellationToken).Type!;
            return serviceType is not null;
        }

        private static bool IsTrackedReplacementOrigin(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, ITypeSymbol serviceType, HashSet<ISymbol> visitedSymbols)
        {
            expression = FastMoqAnalysisHelpers.Unwrap(expression);

            switch (expression)
            {
                case LambdaExpressionSyntax lambdaExpression:
                    return TryGetReturnedExpression(lambdaExpression, out var lambdaReturnExpression) &&
                        IsTrackedReplacementOrigin(lambdaReturnExpression, semanticModel, cancellationToken, serviceType, visitedSymbols);

                case AnonymousMethodExpressionSyntax anonymousMethodExpression:
                    return TryGetReturnedExpression(anonymousMethodExpression, out var anonymousReturnExpression) &&
                        IsTrackedReplacementOrigin(anonymousReturnExpression, semanticModel, cancellationToken, serviceType, visitedSymbols);

                case MemberAccessExpressionSyntax memberAccessExpression when memberAccessExpression.Name.Identifier.ValueText is "Object" or "Instance":
                    return IsTrackedReplacementOrigin(memberAccessExpression.Expression, semanticModel, cancellationToken, serviceType, visitedSymbols);

                case InvocationExpressionSyntax invocationExpression:
                    return IsTrackedSourceInvocation(invocationExpression, semanticModel, cancellationToken, serviceType);

                case IdentifierNameSyntax identifierName:
                    return TryFollowLocalInitializer(identifierName, semanticModel, cancellationToken, serviceType, visitedSymbols);

                case CastExpressionSyntax castExpression:
                    return IsTrackedReplacementOrigin(castExpression.Expression, semanticModel, cancellationToken, serviceType, visitedSymbols);

                case ConditionalExpressionSyntax conditionalExpression:
                    return IsTrackedReplacementOrigin(conditionalExpression.WhenTrue, semanticModel, cancellationToken, serviceType, visitedSymbols) ||
                        IsTrackedReplacementOrigin(conditionalExpression.WhenFalse, semanticModel, cancellationToken, serviceType, visitedSymbols);
            }

            return false;
        }

        private static bool IsTrackedSourceInvocation(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, ITypeSymbol serviceType)
        {
            if (!FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            return method.TypeArguments.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(method.TypeArguments[0], serviceType) &&
                method.ContainingType.ToDisplayString() == "FastMoq.Mocker" &&
                method.Name is "GetMock" or "GetRequiredMock" or "GetOrCreateMock" or "GetObject" or "GetRequiredObject";
        }

        private static bool TryFollowLocalInitializer(IdentifierNameSyntax identifierName, SemanticModel semanticModel, CancellationToken cancellationToken, ITypeSymbol serviceType, HashSet<ISymbol> visitedSymbols)
        {
            if (semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol is not ILocalSymbol localSymbol ||
                !visitedSymbols.Add(localSymbol))
            {
                return false;
            }

            var declaration = localSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) as VariableDeclaratorSyntax;
            return declaration?.Initializer?.Value is ExpressionSyntax initializer &&
                IsTrackedReplacementOrigin(initializer, semanticModel, cancellationToken, serviceType, visitedSymbols);
        }

        private static bool TryFindTrackedSensitiveUsage(InvocationExpressionSyntax addTypeInvocation, SemanticModel semanticModel, CancellationToken cancellationToken, ITypeSymbol serviceType, out string conflictingApi)
        {
            conflictingApi = string.Empty;
            var root = addTypeInvocation.SyntaxTree.GetRoot(cancellationToken);

            foreach (var invocationExpression in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (addTypeInvocation.Span.Contains(invocationExpression.Span))
                {
                    continue;
                }

                if (!FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                    method is null)
                {
                    continue;
                }

                method = method.ReducedFrom ?? method;
                if (!IsTrackedSensitiveUsage(method, serviceType, out conflictingApi))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool IsTrackedSensitiveUsage(IMethodSymbol method, ITypeSymbol serviceType, out string conflictingApi)
        {
            conflictingApi = string.Empty;

            if (method.TypeArguments.Length == 0 ||
                !SymbolEqualityComparer.Default.Equals(method.TypeArguments[0], serviceType))
            {
                return false;
            }

            if (method.ContainingType.ToDisplayString() == "FastMoq.Mocker")
            {
                conflictingApi = method.Name switch
                {
                    "GetObject" => "GetObject<T>()",
                    "GetRequiredObject" => "GetRequiredObject<T>()",
                    "GetRequiredTrackedMock" => "GetRequiredTrackedMock<T>()",
                    "TryGetTrackedMock" => "TryGetTrackedMock<T>(...)",
                    "GetMockModel" => "GetMockModel<T>()",
                    _ => string.Empty,
                };

                return conflictingApi.Length > 0;
            }

            if (!method.IsExtensionMethod || !method.ContainingNamespace.ToDisplayString().StartsWith("FastMoq", System.StringComparison.Ordinal))
            {
                return false;
            }

            conflictingApi = method.Name switch
            {
                "AddPropertyState" => "AddPropertyState<TService>(...)",
                "AddPropertySetterCapture" => "AddPropertySetterCapture<TService, TValue>(...)",
                _ => string.Empty,
            };

            return conflictingApi.Length > 0;
        }

        private static bool TryGetReturnedExpression(LambdaExpressionSyntax lambdaExpression, out ExpressionSyntax returnedExpression)
        {
            returnedExpression = null!;
            if (lambdaExpression.Body is ExpressionSyntax expressionBody)
            {
                returnedExpression = expressionBody;
                return true;
            }

            if (lambdaExpression.Body is BlockSyntax blockSyntax)
            {
                var returnStatement = blockSyntax.Statements.OfType<ReturnStatementSyntax>().SingleOrDefault();
                if (returnStatement?.Expression is ExpressionSyntax returned)
                {
                    returnedExpression = returned;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetReturnedExpression(AnonymousMethodExpressionSyntax anonymousMethodExpression, out ExpressionSyntax returnedExpression)
        {
            returnedExpression = null!;
            var returnStatement = anonymousMethodExpression.Block?.Statements.OfType<ReturnStatementSyntax>().SingleOrDefault();
            if (returnStatement?.Expression is not ExpressionSyntax returned)
            {
                return false;
            }

            returnedExpression = returned;
            return true;
        }
    }
}

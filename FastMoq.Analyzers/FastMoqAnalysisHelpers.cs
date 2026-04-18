using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FastMoq.Analyzers
{
    internal enum TrackedMockOriginKind
    {
        GetMock,
        GetOrCreateMock,
        GetRequiredTrackedMock,
    }

    internal readonly struct TrackedMockOrigin
    {
        public TrackedMockOrigin(ExpressionSyntax mockerExpression, ExpressionSyntax trackedMockExpression, ITypeSymbol serviceType, TrackedMockOriginKind kind)
        {
            MockerExpression = mockerExpression;
            TrackedMockExpression = trackedMockExpression;
            ServiceType = serviceType;
            Kind = kind;
        }

        public ExpressionSyntax MockerExpression { get; }

        public ExpressionSyntax TrackedMockExpression { get; }

        public ITypeSymbol ServiceType { get; }

        public TrackedMockOriginKind Kind { get; }

        public TrackedMockOrigin WithTrackedMockExpression(ExpressionSyntax trackedMockExpression)
        {
            return new TrackedMockOrigin(MockerExpression, trackedMockExpression, ServiceType, Kind);
        }
    }

    internal static class FastMoqAnalysisHelpers
    {
        internal const string FastMoqMockerTypeName = "FastMoq.Mocker";
        internal const string FastMoqMockModelTypeName = "FastMoq.Models.MockModel";
        internal const string FastMoqMockModelGenericTypeName = "FastMoq.Models.MockModel<T>";
        internal const string FastMoqProvidersNamespace = "FastMoq.Providers";
        internal const string FastMoqMoqProviderAssemblyName = "FastMoq.Provider.Moq";
        internal const string FastMoqNSubstituteProviderAssemblyName = "FastMoq.Provider.NSubstitute";
        internal const string FastMoqWebExtensionsMetadataName = "FastMoq.Web.Extensions.TestWebExtensions";
        internal const string FastMoqWebExtensionsTypeName = "FastMoq.Web.Extensions.TestWebExtensions";
        internal const string MoqProviderNamespace = "FastMoq.Providers.MoqProvider";
        internal const string NSubstituteProviderNamespace = "FastMoq.Providers.NSubstituteProvider";
        internal const string MockingProviderRegistryTypeName = "FastMoq.Providers.MockingProviderRegistry";
        internal const string MoqProviderMetadataName = "FastMoq.Providers.MoqProvider.MoqMockingProvider";
        internal const string MoqProviderTypeName = "MoqMockingProvider";
        internal const string MoqProviderName = "moq";
        internal const string NSubstituteProviderName = "nsubstitute";
        internal const string RegisterProviderSetAsDefaultPropertyName = "SetAsDefault";
        private const string FASTMOQ_DEFAULT_PROVIDER_ATTRIBUTE = "FastMoq.Providers.FastMoqDefaultProviderAttribute";
        private const string FASTMOQ_REGISTER_PROVIDER_ATTRIBUTE = "FastMoq.Providers.FastMoqRegisterProviderAttribute";
        private const string FASTMOQ_MOCKER_TEST_BASE_METADATA_NAME = "MockerTestBase`1";
        private const string CONTROLLER_CONTEXT_TYPE = "Microsoft.AspNetCore.Mvc.ControllerContext";
        private const string DEFAULT_HTTP_CONTEXT_TYPE = "Microsoft.AspNetCore.Http.DefaultHttpContext";
        private const string FROM_KEYED_SERVICES_ATTRIBUTE = "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute";
        private const string FUNCTION_CONTEXT_TYPE = "Microsoft.Azure.Functions.Worker.FunctionContext";
        private const string FUNCTION_CONTEXT_INVOCATION_ID_PROPERTY = "InvocationId";
        private const string FUNCTION_CONTEXT_INSTANCE_SERVICES_PROPERTY = "InstanceServices";
        private const string HTTP_CONTEXT_TYPE = "Microsoft.AspNetCore.Http.HttpContext";
        private const string HTTP_CONTEXT_ACCESSOR_TYPE = "Microsoft.AspNetCore.Http.HttpContextAccessor";
        private const string IHTTP_CONTEXT_ACCESSOR_TYPE = "Microsoft.AspNetCore.Http.IHttpContextAccessor";
        private const string ILOGGER_TYPE = "Microsoft.Extensions.Logging.ILogger";
        private const string HTTP_REQUEST_TYPE = "Microsoft.AspNetCore.Http.HttpRequest";
        private const string ILOGGER_FACTORY_TYPE = "Microsoft.Extensions.Logging.ILoggerFactory";
        private const string ITEST_OUTPUT_HELPER_TYPE = "Xunit.ITestOutputHelper";
        private const string ITEST_OUTPUT_HELPER_ABSTRACTIONS_TYPE = "Xunit.Abstractions.ITestOutputHelper";
        private const string STREAM_TYPE = "System.IO.Stream";
        private const string SERVICE_PROVIDER_TYPE = "System.IServiceProvider";
        private const string SERVICE_SCOPE_FACTORY_TYPE = "Microsoft.Extensions.DependencyInjection.IServiceScopeFactory";
        private const string SERVICE_SCOPE_TYPE = "Microsoft.Extensions.DependencyInjection.IServiceScope";

        private static readonly HashSet<string> DisallowedMixedRetrievalMembers = new(StringComparer.Ordinal)
        {
            "Object",
            "Reset",
            "VerifyLogger",
            "SetupSet",
            "Protected",
            "As",
            "CallBase",
        };

        public static ExpressionSyntax Unwrap(ExpressionSyntax expression)
        {
            while (expression is ParenthesizedExpressionSyntax parenthesizedExpression)
            {
                expression = parenthesizedExpression.Expression;
            }

            return expression;
        }

        public static bool TryGetMethodSymbol(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, out IMethodSymbol? method)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
            method = symbolInfo.Symbol as IMethodSymbol ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
            return method is not null;
        }

        public static bool HasMoqProviderPackage(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(MoqProviderMetadataName) is not null;
        }

        public static bool HasWebHelperPackage(SemanticModel semanticModel)
        {
            return semanticModel.Compilation.GetTypeByMetadataName(FastMoqWebExtensionsMetadataName) is not null;
        }

        public static bool IsFastMoqMockModelType(ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol namedType)
            {
                return false;
            }

            var originalDefinitionName = namedType.OriginalDefinition.ToDisplayString();
            return originalDefinitionName is FastMoqMockModelTypeName or FastMoqMockModelGenericTypeName;
        }

        public static bool IsFastMoqMockerMethod(IMethodSymbol method, string methodName)
        {
            method = method.ReducedFrom ?? method;
            return method.Name == methodName && method.ContainingType.ToDisplayString() == FastMoqMockerTypeName;
        }

        public static bool IsFastMoqVerifyLogger(IMethodSymbol method)
        {
            method = method.ReducedFrom ?? method;
            if (method.Name != "VerifyLogger")
            {
                return false;
            }

            return method.ContainingNamespace.ToDisplayString().StartsWith("FastMoq", StringComparison.Ordinal);
        }

        public static bool IsFastMoqInitializeMethod(IMethodSymbol method)
        {
            method = method.ReducedFrom ?? method;
            return method.Name == "Initialize" && method.ContainingType.ToDisplayString() == FastMoqMockerTypeName;
        }

        public static bool IsFastMoqMockerAddTypeMethod(IMethodSymbol method)
        {
            method = method.ReducedFrom ?? method;
            return method.Name == "AddType" && method.ContainingType.ToDisplayString() == FastMoqMockerTypeName;
        }

        public static bool TryGetRequiredProvider(IMethodSymbol method, out string providerName, out string apiName)
        {
            method = method.ReducedFrom ?? method;
            providerName = string.Empty;
            apiName = method.Name;

            if (method.ContainingType.ToDisplayString() == FastMoqMockerTypeName &&
                method.Name is "GetMock" or "GetRequiredMock" or "CreateMockInstance" or "CreateDetachedMock")
            {
                providerName = MoqProviderName;
                return true;
            }

            if (method.ContainingAssembly.Name == FastMoqMoqProviderAssemblyName &&
                (method.ContainingType.Name == "IFastMockMoqExtensions" || method.ContainingType.Name == "MockerHttpMoqExtensions"))
            {
                providerName = MoqProviderName;
                return true;
            }

            if (method.ContainingAssembly.Name == FastMoqNSubstituteProviderAssemblyName &&
                method.ContainingType.Name == "IFastMockNSubstituteExtensions")
            {
                providerName = NSubstituteProviderName;
                return true;
            }

            return false;
        }

        public static bool TryGetRequiredProvider(IPropertySymbol property, out string providerName, out string apiName)
        {
            providerName = string.Empty;
            apiName = property.Name;

            if (property.Name == "Mock")
            {
                for (var containingType = property.ContainingType; containingType is not null; containingType = containingType.BaseType)
                {
                    if (IsFastMoqMockModelType(containingType))
                    {
                        providerName = MoqProviderName;
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryGetPropertySymbol(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out IPropertySymbol? property)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
            property = symbolInfo.Symbol as IPropertySymbol ?? symbolInfo.CandidateSymbols.OfType<IPropertySymbol>().FirstOrDefault();
            return property is not null;
        }

        public static bool IsFastMoqMockerProperty(IPropertySymbol property, string propertyName)
        {
            return property.Name == propertyName && property.ContainingType.ToDisplayString() == FastMoqMockerTypeName;
        }

        public static bool IsFastMoqNativeMockProperty(IPropertySymbol property)
        {
            if (property.Name != "NativeMock")
            {
                return false;
            }

            if (property.ContainingType.ToDisplayString() == "FastMoq.Providers.IFastMock")
            {
                return true;
            }

            if (property.ContainingType.AllInterfaces.Any(item => item.ToDisplayString() == "FastMoq.Providers.IFastMock"))
            {
                return true;
            }

            for (var containingType = property.ContainingType; containingType is not null; containingType = containingType.BaseType)
            {
                if (IsFastMoqMockModelType(containingType))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetBooleanConstant(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out bool value)
        {
            var constantValue = semanticModel.GetConstantValue(expression, cancellationToken);
            if (constantValue.HasValue && constantValue.Value is bool boolValue)
            {
                value = boolValue;
                return true;
            }

            value = default;
            return false;
        }

        public static bool SupportsVerifyLoggedReplacement(IMethodSymbol method)
        {
            method = method.ReducedFrom ?? method;
            foreach (var parameter in method.Parameters)
            {
                if (IsTimesLikeType(parameter.Type))
                {
                    return false;
                }
            }

            return method.Parameters.Length >= 2 && method.Parameters.Length <= 5;
        }

        public static bool TryResolveTrackedMockOrigin(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out TrackedMockOrigin origin)
        {
            return TryResolveTrackedMockOrigin(expression, semanticModel, cancellationToken, new HashSet<ISymbol>(SymbolEqualityComparer.Default), out origin);
        }

        private static bool TryResolveTrackedMockOrigin(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, ISet<ISymbol> visitedSymbols, out TrackedMockOrigin origin)
        {
            expression = Unwrap(expression);

            if (expression is InvocationExpressionSyntax invocationExpression &&
                TryResolveTrackedMockOrigin(invocationExpression, semanticModel, cancellationToken, visitedSymbols, out origin))
            {
                return true;
            }

            if (TryResolveTrackedMockOriginFromSymbol(expression, semanticModel, cancellationToken, visitedSymbols, out origin))
            {
                return true;
            }

            origin = default;
            return false;
        }

        public static bool TryResolveTrackedMockOrigin(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out TrackedMockOrigin origin)
        {
            return TryResolveTrackedMockOrigin(invocationExpression, semanticModel, cancellationToken, new HashSet<ISymbol>(SymbolEqualityComparer.Default), out origin);
        }

        private static bool TryResolveTrackedMockOrigin(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, ISet<ISymbol> visitedSymbols, out TrackedMockOrigin origin)
        {
            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) || method is null)
            {
                origin = default;
                return false;
            }

            if (IsFastMoqMockerMethod(method, "GetMock") ||
                IsFastMoqMockerMethod(method, "GetOrCreateMock") ||
                IsFastMoqMockerMethod(method, "GetRequiredTrackedMock"))
            {
                if (method.TypeArguments.Length != 1 || invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess)
                {
                    origin = default;
                    return false;
                }

                origin = new TrackedMockOrigin(
                    memberAccess.Expression,
                    invocationExpression,
                    method.TypeArguments[0],
                    IsFastMoqMockerMethod(method, "GetMock")
                        ? TrackedMockOriginKind.GetMock
                        : IsFastMoqMockerMethod(method, "GetRequiredTrackedMock")
                            ? TrackedMockOriginKind.GetRequiredTrackedMock
                            : TrackedMockOriginKind.GetOrCreateMock);
                return true;
            }

            if ((method.Name == "AsMoq" || method.Name == "AsNSubstitute") && invocationExpression.Expression is MemberAccessExpressionSyntax adapterAccess)
            {
                if (TryResolveTrackedMockOrigin(adapterAccess.Expression, semanticModel, cancellationToken, visitedSymbols, out origin))
                {
                    origin = origin.WithTrackedMockExpression(adapterAccess.Expression);
                    return true;
                }
            }

            origin = default;
            return false;
        }

        private static bool TryResolveTrackedMockOriginFromSymbol(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, ISet<ISymbol> visitedSymbols, out TrackedMockOrigin origin)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
            var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
            if (symbol is null || !visitedSymbols.Add(symbol))
            {
                origin = default;
                return false;
            }

            foreach (var sourceExpression in GetTrackedMockSourceExpressions(symbol, semanticModel, cancellationToken))
            {
                if (TryResolveTrackedMockOrigin(sourceExpression, semanticModel, cancellationToken, visitedSymbols, out origin))
                {
                    origin = origin.WithTrackedMockExpression(expression);
                    return true;
                }
            }

            origin = default;
            return false;
        }

        private static IEnumerable<ExpressionSyntax> GetTrackedMockSourceExpressions(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            switch (symbol)
            {
                case ILocalSymbol localSymbol:
                    foreach (var syntaxReference in localSymbol.DeclaringSyntaxReferences)
                    {
                        if (syntaxReference.GetSyntax(cancellationToken) is VariableDeclaratorSyntax variableDeclarator &&
                            variableDeclarator.Initializer?.Value is ExpressionSyntax localInitializer)
                        {
                            yield return localInitializer;
                        }
                    }

                    yield break;

                case IFieldSymbol fieldSymbol:
                    foreach (var syntaxReference in fieldSymbol.DeclaringSyntaxReferences)
                    {
                        if (syntaxReference.GetSyntax(cancellationToken) is VariableDeclaratorSyntax variableDeclarator &&
                            variableDeclarator.Initializer?.Value is ExpressionSyntax fieldInitializer)
                        {
                            yield return fieldInitializer;
                        }
                    }

                    foreach (var assignment in GetAssignedExpressions(fieldSymbol, semanticModel, cancellationToken))
                    {
                        yield return assignment;
                    }

                    yield break;

                case IPropertySymbol propertySymbol:
                    foreach (var syntaxReference in propertySymbol.DeclaringSyntaxReferences)
                    {
                        if (syntaxReference.GetSyntax(cancellationToken) is not PropertyDeclarationSyntax propertyDeclaration)
                        {
                            continue;
                        }

                        if (propertyDeclaration.Initializer?.Value is ExpressionSyntax propertyInitializer)
                        {
                            yield return propertyInitializer;
                        }

                        if (propertyDeclaration.ExpressionBody?.Expression is ExpressionSyntax propertyExpressionBody)
                        {
                            yield return propertyExpressionBody;
                        }

                        if (propertyDeclaration.AccessorList is null)
                        {
                            continue;
                        }

                        foreach (var accessor in propertyDeclaration.AccessorList.Accessors)
                        {
                            if (!accessor.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.GetAccessorDeclaration))
                            {
                                continue;
                            }

                            if (accessor.ExpressionBody?.Expression is ExpressionSyntax accessorExpressionBody)
                            {
                                yield return accessorExpressionBody;
                            }

                            if (accessor.Body?.Statements.Count == 1 &&
                                accessor.Body.Statements[0] is ReturnStatementSyntax { Expression: ExpressionSyntax returnExpression })
                            {
                                yield return returnExpression;
                            }
                        }
                    }

                    yield break;
            }
        }

        private static IEnumerable<ExpressionSyntax> GetAssignedExpressions(IFieldSymbol fieldSymbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var syntaxReference in fieldSymbol.ContainingType.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax typeDeclaration)
                {
                    continue;
                }

                var assignmentSemanticModel = typeDeclaration.SyntaxTree == semanticModel.SyntaxTree
                    ? semanticModel
                    : semanticModel.Compilation.GetSemanticModel(typeDeclaration.SyntaxTree);

                foreach (var assignmentExpression in typeDeclaration.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    var leftSymbolInfo = assignmentSemanticModel.GetSymbolInfo(assignmentExpression.Left, cancellationToken);
                    var leftSymbol = leftSymbolInfo.Symbol ?? leftSymbolInfo.CandidateSymbols.FirstOrDefault();
                    if (SymbolEqualityComparer.Default.Equals(leftSymbol, fieldSymbol))
                    {
                        yield return assignmentExpression.Right;
                    }
                }
            }
        }

        public static bool ContainsGetOrCreateMock(SyntaxNode root, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(invocation =>
                    TryGetMethodSymbol(invocation, semanticModel, cancellationToken, out var method) &&
                    method is not null &&
                    IsFastMoqMockerMethod(method, "GetOrCreateMock"));
        }

        public static bool IsSafeProviderFirstMockRetrievalCandidate(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) || method is null)
            {
                return false;
            }

            if (!IsFastMoqMockerMethod(method, "GetMock") || method.TypeArguments.Length != 1)
            {
                return false;
            }

            if (method.Parameters.Length > 0 && method.Parameters[0].Type.TypeKind == TypeKind.Delegate)
            {
                return false;
            }

            if (invocationExpression.Parent is MemberAccessExpressionSyntax memberAccess && DisallowedMixedRetrievalMembers.Contains(memberAccess.Name.Identifier.ValueText))
            {
                return false;
            }

            if (invocationExpression.Parent is EqualsValueClauseSyntax equalsValueClause &&
                equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                variableDeclarator.Parent is VariableDeclarationSyntax variableDeclaration &&
                !variableDeclaration.Type.IsVar)
            {
                return false;
            }

            return true;
        }

        public static bool IsSafeMixedRetrievalCandidate(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return IsSafeProviderFirstMockRetrievalCandidate(invocationExpression, semanticModel, cancellationToken);
        }

        public static string GetMinimalTypeName(ITypeSymbol serviceType, SemanticModel semanticModel, int position)
        {
            return serviceType.ToMinimalDisplayString(semanticModel, position, SymbolDisplayFormat.MinimallyQualifiedFormat);
        }

        public static string BuildObjectAccessReplacement(TrackedMockOrigin origin, SemanticModel semanticModel, int position)
        {
            var mockerExpression = origin.MockerExpression.WithoutTrivia().ToString();
            var serviceType = GetMinimalTypeName(origin.ServiceType, semanticModel, position);
            return origin.Kind switch
            {
                TrackedMockOriginKind.GetOrCreateMock => $"{mockerExpression}.GetOrCreateMock<{serviceType}>().Instance",
                TrackedMockOriginKind.GetRequiredTrackedMock => $"{mockerExpression}.GetRequiredObject<{serviceType}>()",
                _ => $"{mockerExpression}.GetObject<{serviceType}>()",
            };
        }

        public static string BuildResetReplacement(TrackedMockOrigin origin, SemanticModel semanticModel, int position)
        {
            var mockerExpression = origin.MockerExpression.WithoutTrivia().ToString();
            var serviceType = GetMinimalTypeName(origin.ServiceType, semanticModel, position);
            return origin.Kind == TrackedMockOriginKind.GetRequiredTrackedMock
                ? $"{mockerExpression}.GetRequiredTrackedMock<{serviceType}>().Reset()"
                : $"{mockerExpression}.GetOrCreateMock<{serviceType}>().Reset()";
        }

        public static bool IsTimesLikeType(ITypeSymbol type)
        {
            return type.ToDisplayString() is "Moq.Times" or "System.Func<Moq.Times>";
        }

        public static bool TryBuildVerifyLoggedReplacement(TrackedMockOrigin origin, SemanticModel semanticModel, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken, out string replacement)
        {
            replacement = string.Empty;
            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) || method is null)
            {
                return false;
            }

            var arguments = invocationExpression.ArgumentList.Arguments;
            var replacementArguments = new List<string>(arguments.Count);

            if (method.Parameters.Length > 0 && IsTimesLikeType(method.Parameters[method.Parameters.Length - 1].Type))
            {
                if (arguments.Count != method.Parameters.Length)
                {
                    return false;
                }

                for (var index = 0; index < arguments.Count - 1; index++)
                {
                    replacementArguments.Add(arguments[index].WithoutTrivia().ToString());
                }

                if (!TryConvertVerifyLoggerTimesArgument(arguments[arguments.Count - 1], semanticModel, cancellationToken, out var convertedArgument, out var omitArgument))
                {
                    return false;
                }

                if (!omitArgument)
                {
                    replacementArguments.Add(convertedArgument);
                }
            }
            else
            {
                foreach (var argument in arguments)
                {
                    replacementArguments.Add(argument.WithoutTrivia().ToString());
                }
            }

            var mockerExpression = origin.MockerExpression.WithoutTrivia().ToString();
            replacement = $"{mockerExpression}.VerifyLogged({string.Join(", ", replacementArguments)})";
            return true;
        }

        public static bool TryBuildVerifyReplacement(TrackedMockOrigin origin, SemanticModel semanticModel, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken, out string replacement)
        {
            replacement = string.Empty;
            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null ||
                !IsMoqVerifyMethod(method))
            {
                return false;
            }

            var arguments = invocationExpression.ArgumentList.Arguments;
            if (arguments.Count == 0 || arguments.Count > 2)
            {
                return false;
            }

            var replacementArguments = new List<string>
            {
                arguments[0].WithoutTrivia().ToString(),
            };

            if (arguments.Count == 2)
            {
                if (!TryConvertTimesArgument(arguments[1], semanticModel, cancellationToken, invocationExpression.SpanStart, out var convertedArgument, out var omitArgument))
                {
                    return false;
                }

                if (!omitArgument)
                {
                    replacementArguments.Add(convertedArgument);
                }
            }

            var mockerExpression = origin.MockerExpression.WithoutTrivia().ToString();
            var serviceType = GetMinimalTypeName(origin.ServiceType, semanticModel, invocationExpression.SpanStart);
            replacement = $"{mockerExpression}.Verify<{serviceType}>({string.Join(", ", replacementArguments)})";
            return true;
        }

        public static bool TryBuildMockOptionalReplacement(AssignmentExpressionSyntax assignmentExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacement)
        {
            replacement = string.Empty;
            if (!TryGetPropertySymbol(assignmentExpression.Left, semanticModel, cancellationToken, out var property) ||
                property is null ||
                !IsFastMoqMockerProperty(property, "MockOptional") ||
                !TryGetBooleanConstant(assignmentExpression.Right, semanticModel, cancellationToken, out var boolValue))
            {
                return false;
            }

            string leftText;
            if (assignmentExpression.Left is MemberAccessExpressionSyntax memberAccess)
            {
                leftText = $"{memberAccess.Expression.WithoutTrivia()}.OptionalParameterResolution";
            }
            else if (assignmentExpression.Left is IdentifierNameSyntax)
            {
                leftText = "OptionalParameterResolution";
            }
            else
            {
                return false;
            }

            var rightText = boolValue
                ? "OptionalParameterResolutionMode.ResolveViaMocker"
                : "OptionalParameterResolutionMode.UseDefaultOrNull";
            replacement = $"{leftText} = {rightText}";
            return true;
        }

        public static bool TryBuildInitializeReplacement(InvocationExpressionSyntax invocationExpression, out string replacement)
        {
            replacement = string.Empty;
            if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return false;
            }

            SimpleNameSyntax renamedName = memberAccess.Name is GenericNameSyntax genericName
                ? genericName.WithIdentifier(Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Identifier("GetMock"))
                : Microsoft.CodeAnalysis.CSharp.SyntaxFactory.IdentifierName("GetMock");
            replacement = $"{memberAccess.Expression.WithoutTrivia()}.{renamedName.WithoutTrivia()}{invocationExpression.ArgumentList.WithoutTrivia()}";
            return true;
        }

        public static bool TryBuildSetupOptionsReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacement)
        {
            return TryBuildSetupOptionsReturnsReplacement(invocationExpression, semanticModel, cancellationToken, out replacement) ||
                TryBuildSetupOptionsAddTypeReplacement(invocationExpression, semanticModel, cancellationToken, out replacement);
        }

        public static bool TryBuildSetupSetGuidance(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string guidance)
        {
            guidance = string.Empty;

            if (!TryGetSetupSetProperty(invocationExpression, semanticModel, cancellationToken, out var origin, out var property, out var lambdaExpression))
            {
                return false;
            }

            var propertyTypeName = GetMinimalTypeName(property.Type, semanticModel, invocationExpression.SpanStart);
            if (origin.ServiceType.TypeKind == TypeKind.Interface &&
                invocationExpression.Parent is not MemberAccessExpressionSyntax &&
                property.SetMethod is not null &&
                TryBuildSetupSetPropertySelector(lambdaExpression, out var propertySelector))
            {
                var mockerExpression = origin.MockerExpression.WithoutTrivia().ToString();
                var serviceTypeName = GetMinimalTypeName(origin.ServiceType, semanticModel, invocationExpression.SpanStart);
                guidance = $"{mockerExpression}.AddPropertySetterCapture<{serviceTypeName}, {propertyTypeName}>({propertySelector})";
                return true;
            }

            guidance = $"a fake or stub plus PropertyValueCapture<{propertyTypeName}>";
            return true;
        }

        public static bool TryBuildSetupSetReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacement)
        {
            replacement = string.Empty;

            if (!TryGetSetupSetProperty(invocationExpression, semanticModel, cancellationToken, out var origin, out var property, out var lambdaExpression))
            {
                return false;
            }

            if (origin.ServiceType.TypeKind != TypeKind.Interface ||
                invocationExpression.Parent is MemberAccessExpressionSyntax ||
                property.SetMethod is null ||
                !TryBuildSetupSetPropertySelector(lambdaExpression, out var propertySelector))
            {
                return false;
            }

            var mockerExpression = origin.MockerExpression.WithoutTrivia().ToString();
            var serviceTypeName = GetMinimalTypeName(origin.ServiceType, semanticModel, invocationExpression.SpanStart);
            var propertyTypeName = GetMinimalTypeName(property.Type, semanticModel, invocationExpression.SpanStart);
            replacement = $"{mockerExpression}.AddPropertySetterCapture<{serviceTypeName}, {propertyTypeName}>({propertySelector})";
            return true;
        }

        public static bool TryBuildSetupAllPropertiesGuidance(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string guidance)
        {
            guidance = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name != "SetupAllProperties" ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
            {
                return false;
            }

            var receiverType = semanticModel.GetTypeInfo(memberAccessExpression.Expression, cancellationToken).Type as INamedTypeSymbol;
            if (receiverType is null ||
                receiverType.Name != "Mock" ||
                receiverType.ContainingNamespace.ToDisplayString() != "Moq" ||
                receiverType.TypeArguments.Length != 1)
            {
                return false;
            }

            var serviceType = receiverType.TypeArguments[0];
            var serviceTypeName = GetMinimalTypeName(serviceType, semanticModel, invocationExpression.SpanStart);
            if (serviceType.TypeKind == TypeKind.Interface && invocationExpression.Parent is not MemberAccessExpressionSyntax)
            {
                guidance = $"AddPropertyState<{serviceTypeName}>()";
                return true;
            }

            guidance = "a concrete fake or stub registered with AddType(...)";
            return true;
        }

        public static bool TryBuildSetupAllPropertiesReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacement)
        {
            replacement = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name != "SetupAllProperties" ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression ||
                invocationExpression.Parent is MemberAccessExpressionSyntax ||
                !TryResolveTrackedMockOrigin(memberAccessExpression.Expression, semanticModel, cancellationToken, out var origin) ||
                origin.ServiceType.TypeKind != TypeKind.Interface)
            {
                return false;
            }

            var mockerExpression = origin.MockerExpression.WithoutTrivia().ToString();
            var serviceTypeName = GetMinimalTypeName(origin.ServiceType, semanticModel, invocationExpression.SpanStart);
            replacement = $"{mockerExpression}.AddPropertyState<{serviceTypeName}>()";
            return true;
        }

        public static bool TryBuildTypedProviderExtensionReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacement)
        {
            replacement = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (!IsFastMoqMockerMethod(method, "GetNativeMock") ||
                method.TypeArguments.Length != 1 ||
                invocationExpression.ArgumentList.Arguments.Count != 0 ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression ||
                !TryGetSingleProviderNamespacePreference(invocationExpression, out _, out var providerExtensionName))
            {
                return false;
            }

            var mockerExpression = memberAccessExpression.Expression.WithoutTrivia().ToString();
            var serviceTypeName = GetMinimalTypeName(method.TypeArguments[0], semanticModel, invocationExpression.SpanStart);
            replacement = $"{mockerExpression}.GetOrCreateMock<{serviceTypeName}>().{providerExtensionName}";
            return true;
        }

        public static bool TryBuildTypedProviderExtensionReplacement(MemberAccessExpressionSyntax memberAccessExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacement)
        {
            replacement = string.Empty;

            if (!TryGetPropertySymbol(memberAccessExpression, semanticModel, cancellationToken, out var property) ||
                property is null ||
                !IsFastMoqNativeMockProperty(property) ||
                memberAccessExpression.Name.Identifier.ValueText != "NativeMock" ||
                !TryGetSingleProviderNamespacePreference(memberAccessExpression, out _, out var providerExtensionName))
            {
                return false;
            }

            replacement = $"{memberAccessExpression.Expression.WithoutTrivia()}.{providerExtensionName}";
            return true;
        }

        public static bool TryBuildWebHelperInvocationReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacement)
        {
            replacement = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (!IsFastMoqMockerAddTypeMethod(method) ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
            {
                return false;
            }

            var mockerExpression = memberAccessExpression.Expression.WithoutTrivia().ToString();
            return TryBuildHttpContextRegistrationReplacement(invocationExpression, semanticModel, cancellationToken, method, mockerExpression, out replacement) ||
                TryBuildHttpContextAccessorRegistrationReplacement(invocationExpression, semanticModel, cancellationToken, method, mockerExpression, out replacement);
        }

        public static bool TryBuildWebHelperRequestBodyReplacement(AssignmentExpressionSyntax assignmentExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionStatementSyntax targetStatement, out string replacement, out ExpressionStatementSyntax? linkedStatementToRemove)
        {
            targetStatement = null!;
            replacement = string.Empty;
            linkedStatementToRemove = null;

            if (!TryGetCreateHttpContextRequestAssignment(assignmentExpression, semanticModel, cancellationToken, out var createHttpContextInvocation, out var propertyName, out var assignedExpression) ||
                assignmentExpression.Parent is not ExpressionStatementSyntax currentStatement)
            {
                return false;
            }

            ExpressionSyntax bodyExpression;
            ExpressionSyntax? contentTypeExpression = null;
            ExpressionStatementSyntax bodyStatement;
            ExpressionStatementSyntax? contentTypeStatement = null;

            if (propertyName == "Body")
            {
                if (!IsSupportedRequestBodyExpression(assignedExpression, semanticModel, cancellationToken))
                {
                    return false;
                }

                bodyExpression = assignedExpression;
                bodyStatement = currentStatement;

                if (TryFindSiblingCreateHttpContextRequestAssignment(currentStatement, createHttpContextInvocation, "ContentType", semanticModel, cancellationToken, out var siblingContentTypeAssignment) &&
                    siblingContentTypeAssignment.Parent is ExpressionStatementSyntax siblingContentTypeStatement)
                {
                    contentTypeExpression = siblingContentTypeAssignment.Right;
                    contentTypeStatement = siblingContentTypeStatement;
                }
            }
            else
            {
                contentTypeExpression = assignedExpression;
                contentTypeStatement = currentStatement;

                if (!TryFindSiblingCreateHttpContextRequestAssignment(currentStatement, createHttpContextInvocation, "Body", semanticModel, cancellationToken, out var siblingBodyAssignment) ||
                    siblingBodyAssignment.Parent is not ExpressionStatementSyntax siblingBodyStatement ||
                    !IsSupportedRequestBodyExpression(siblingBodyAssignment.Right, semanticModel, cancellationToken))
                {
                    return false;
                }

                bodyExpression = siblingBodyAssignment.Right;
                bodyStatement = siblingBodyStatement;
            }

            targetStatement = bodyStatement;
            linkedStatementToRemove = contentTypeStatement is not null && contentTypeStatement != bodyStatement ? contentTypeStatement : null;
            replacement = BuildWebRequestBodyReplacement(createHttpContextInvocation, bodyExpression, contentTypeExpression);
            return true;
        }

        public static bool TryBuildFunctionContextInstanceServicesReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax targetInvocation, out string replacement)
        {
            if (!HasFunctionContextInstanceServicesMockHelper(semanticModel))
            {
                targetInvocation = null!;
                replacement = string.Empty;
                return false;
            }

            return TryBuildFunctionContextInstanceServicesReturnsReplacement(invocationExpression, semanticModel, cancellationToken, out targetInvocation, out replacement) ||
                TryBuildFunctionContextInstanceServicesSetupPropertyReplacement(invocationExpression, semanticModel, cancellationToken, out targetInvocation, out replacement);
        }

        public static bool TryGetLoggerFactoryHelperSuggestion(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string helperName)
        {
            helperName = string.Empty;

            if (!TryGetLoggerFactoryHelperInvocation(invocationExpression, semanticModel, cancellationToken, out _, out var objectCreationExpression, out _) ||
                objectCreationExpression.ArgumentList is not ArgumentListSyntax argumentList ||
                !TryBuildITestOutputHelperLineWriter(argumentList.Arguments[0].Expression, semanticModel, cancellationToken, out _))
            {
                return false;
            }

            helperName = "AddLoggerFactory(...)";
            return true;
        }

        public static bool TryBuildLoggerFactoryHelperReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacement)
        {
            replacement = string.Empty;

            if (!TryGetLoggerFactoryHelperInvocation(invocationExpression, semanticModel, cancellationToken, out var memberAccessExpression, out var objectCreationExpression, out var replaceArgument))
            {
                return false;
            }

            if (objectCreationExpression.ArgumentList is not ArgumentListSyntax argumentList ||
                !TryBuildITestOutputHelperLineWriter(argumentList.Arguments[0].Expression, semanticModel, cancellationToken, out var lineWriterExpression))
            {
                return false;
            }

            replacement = $"{memberAccessExpression.Expression.WithoutTrivia()}.AddLoggerFactory({lineWriterExpression}{replaceArgument})";
            return true;
        }

        private static bool TryGetLoggerFactoryHelperInvocation(
            InvocationExpressionSyntax invocationExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out MemberAccessExpressionSyntax memberAccessExpression,
            out ObjectCreationExpressionSyntax objectCreationExpression,
            out string replaceArgument)
        {
            memberAccessExpression = null!;
            objectCreationExpression = null!;
            replaceArgument = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (!IsFastMoqMockerAddTypeMethod(method) ||
                method.TypeArguments.Length != 1 ||
                !IsLoggerRegistrationType(method.TypeArguments[0]) ||
                method.Parameters.Any(parameter => IsContextAwareFactoryParameter(parameter.Type)) ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax resolvedMemberAccessExpression ||
                invocationExpression.ArgumentList.Arguments.Count == 0 ||
                invocationExpression.ArgumentList.Arguments.Count > 2)
            {
                return false;
            }

            if (invocationExpression.ArgumentList.Arguments.Count == 2 &&
                (method.Parameters.Length < 2 || method.Parameters[1].Type.SpecialType != SpecialType.System_Boolean))
            {
                return false;
            }

            var valueExpression = Unwrap(invocationExpression.ArgumentList.Arguments[0].Expression);
            if (valueExpression is not ObjectCreationExpressionSyntax resolvedObjectCreationExpression ||
                resolvedObjectCreationExpression.Initializer is not null ||
                resolvedObjectCreationExpression.ArgumentList?.Arguments.Count != 1)
            {
                return false;
            }

            var createdType = semanticModel.GetTypeInfo(resolvedObjectCreationExpression, cancellationToken).Type;
            if (createdType is null || !IsMatchingOrImplementingType(createdType, method.TypeArguments[0].ToDisplayString()))
            {
                return false;
            }

            memberAccessExpression = resolvedMemberAccessExpression;
            objectCreationExpression = resolvedObjectCreationExpression;
            replaceArgument = invocationExpression.ArgumentList.Arguments.Count == 2
                ? $", replace: {invocationExpression.ArgumentList.Arguments[1].Expression.WithoutTrivia()}"
                : string.Empty;
            return true;
        }

        public static Location GetTargetNameLocation(ExpressionSyntax expression)
        {
            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Name.GetLocation();
            }

            return expression.GetLocation();
        }

        public static bool TryGetSingleProviderNamespacePreference(SyntaxNode node, out string providerName, out string providerExtensionName)
        {
            providerName = string.Empty;
            providerExtensionName = string.Empty;

            var usesMoqProvider = HasUsingDirective(node, MoqProviderNamespace);
            var usesNSubstituteProvider = HasUsingDirective(node, NSubstituteProviderNamespace);

            if (usesMoqProvider == usesNSubstituteProvider)
            {
                return false;
            }

            providerName = usesMoqProvider ? MoqProviderName : NSubstituteProviderName;
            providerExtensionName = usesMoqProvider ? "AsMoq()" : "AsNSubstitute()";
            return true;
        }

        public static bool TryGetFastMoqWebHelperSuggestion(IMethodSymbol method, out string helperName, out string setupKind)
        {
            helperName = string.Empty;
            setupKind = string.Empty;

            method = method.ReducedFrom ?? method;
            if (!IsFastMoqMockerAddTypeMethod(method) || method.TypeArguments.Length == 0)
            {
                return false;
            }

            var typeNames = new HashSet<string>(method.TypeArguments.Select(type => type.ToDisplayString()), StringComparer.Ordinal);
            if (typeNames.Contains("Microsoft.AspNetCore.Mvc.ControllerContext"))
            {
                helperName = "CreateControllerContext(...)";
                setupKind = "ControllerContext";
                return true;
            }

            if (typeNames.Contains("Microsoft.AspNetCore.Http.IHttpContextAccessor") ||
                typeNames.Contains("Microsoft.AspNetCore.Http.HttpContextAccessor"))
            {
                helperName = "AddHttpContextAccessor(...)";
                setupKind = "IHttpContextAccessor";
                return true;
            }

            if (typeNames.Contains("Microsoft.AspNetCore.Http.HttpContext"))
            {
                helperName = "CreateHttpContext(...) or AddHttpContext(...)";
                setupKind = "HttpContext";
                return true;
            }

            return false;
        }

        public static bool TryGetHttpRequestBodyHelperSuggestion(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string helperName, out string setupKind)
        {
            helperName = string.Empty;
            setupKind = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) || method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name != "CreateHttpContext" ||
                method.ContainingType.ToDisplayString() != FastMoqWebExtensionsTypeName ||
                invocationExpression.Parent is not MemberAccessExpressionSyntax requestAccess ||
                requestAccess.Name.Identifier.ValueText != "Request" ||
                requestAccess.Parent is not MemberAccessExpressionSyntax targetAccess ||
                targetAccess.Parent is not AssignmentExpressionSyntax assignmentExpression ||
                assignmentExpression.Left != targetAccess)
            {
                return false;
            }

            if (targetAccess.Name.Identifier.ValueText == "Body")
            {
                helperName = "SetRequestBody(...) or SetRequestJsonBody(...)";
                setupKind = "HttpRequest.Body";
                return true;
            }

            if (targetAccess.Name.Identifier.ValueText == "ContentType")
            {
                helperName = "SetRequestBody(...) or SetRequestJsonBody(...)";
                setupKind = "HttpRequest.ContentType";
                return true;
            }

            return false;
        }

        public static bool TryGetRawWebHelperSuggestion(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out string helperName, out string setupKind)
        {
            helperName = string.Empty;
            setupKind = string.Empty;

            var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (type?.ToDisplayString() != DEFAULT_HTTP_CONTEXT_TYPE)
            {
                return false;
            }

            if (IsInsideFastMoqWebRegistration(expression, semanticModel, cancellationToken))
            {
                return false;
            }

            if (IsControllerContextHttpContextInitializer(expression, semanticModel, cancellationToken))
            {
                helperName = "CreateControllerContext(...)";
                setupKind = "ControllerContext";
                return true;
            }

            helperName = "CreateHttpContext(...)";
            setupKind = "DefaultHttpContext";
            return true;
        }

        public static bool TryGetProviderNeutralHttpHelperSuggestion(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string apiName)
        {
            apiName = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) || method is null)
            {
                return false;
            }

            if (TryGetProviderNeutralHttpHelperSuggestion(method, out apiName))
            {
                return true;
            }

            return TryGetProtectedSendAsyncSuggestion(invocationExpression, semanticModel, cancellationToken, method, out apiName);
        }

        public static bool TryGetProviderNeutralHttpHelperSuggestion(IMethodSymbol method, out string apiName)
        {
            apiName = string.Empty;

            method = method.ReducedFrom ?? method;
            if (method.ContainingAssembly.Name != FastMoqMoqProviderAssemblyName || method.ContainingType.Name != "MockerHttpMoqExtensions")
            {
                return false;
            }

            if (method.Name == "SetupHttpMessage")
            {
                apiName = "SetupHttpMessage(...)";
                return true;
            }

            if (method.Name is "SetupMessageProtected" or "SetupMessageProtectedAsync" &&
                method.TypeArguments.Length > 0 &&
                method.TypeArguments[0].ToDisplayString() == "System.Net.Http.HttpMessageHandler")
            {
                apiName = $"{method.Name}(...)";
                return true;
            }

            return false;
        }

        private static bool TryGetProtectedSendAsyncSuggestion(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, IMethodSymbol method, out string apiName)
        {
            apiName = string.Empty;

            method = method.ReducedFrom ?? method;
            if (method.Name is not "Setup" and not "SetupSequence" ||
                method.ContainingNamespace.ToDisplayString() != "Moq.Protected" ||
                method.ContainingType.Name != "IProtectedMock" ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !TryResolveProtectedTrackedMockOrigin(memberAccess.Expression, semanticModel, cancellationToken, out var origin) ||
                origin.ServiceType.ToDisplayString() != "System.Net.Http.HttpMessageHandler" ||
                invocationExpression.ArgumentList.Arguments.Count == 0 ||
                semanticModel.GetConstantValue(invocationExpression.ArgumentList.Arguments[0].Expression, cancellationToken) is not { HasValue: true, Value: string protectedMemberName } ||
                protectedMemberName != "SendAsync")
            {
                return false;
            }

            apiName = $"Protected().{method.Name}(\"SendAsync\", ...)";
            return true;
        }

        public static bool TryResolveProtectedTrackedMockOrigin(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out TrackedMockOrigin origin)
        {
            expression = Unwrap(expression);
            if (expression is not InvocationExpressionSyntax protectedInvocation ||
                !TryGetMethodSymbol(protectedInvocation, semanticModel, cancellationToken, out var protectedMethod) ||
                protectedMethod is null)
            {
                origin = default;
                return false;
            }

            protectedMethod = protectedMethod.ReducedFrom ?? protectedMethod;
            if (protectedMethod.Name != "Protected" ||
                protectedInvocation.Expression is not MemberAccessExpressionSyntax protectedAccess)
            {
                origin = default;
                return false;
            }

            return TryResolveTrackedMockOrigin(protectedAccess.Expression, semanticModel, cancellationToken, out origin);
        }

        public static bool TryGetTypedServiceProviderHelperSuggestion(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string currentApi)
        {
            currentApi = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            if (TryGetTrackedServiceGraphShimSuggestion(method, out currentApi))
            {
                return true;
            }

            if (TryGetScopeExtractionLookupSuggestion(invocationExpression, semanticModel, cancellationToken, out currentApi))
            {
                return true;
            }

            return TryGetScopeShimSetupSuggestion(invocationExpression, semanticModel, cancellationToken, method, out currentApi);
        }

        public static bool TryBuildTypedServiceProviderHelperReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax targetInvocation, out string replacement, out IReadOnlyList<string> requiredNamespaces)
        {
            if (TryBuildTrackedServiceGraphShimReplacement(invocationExpression, semanticModel, cancellationToken, out targetInvocation, out replacement, out requiredNamespaces))
            {
                return true;
            }

            return TryBuildScopeExtractionReplacement(invocationExpression, semanticModel, cancellationToken, out targetInvocation, out replacement, out requiredNamespaces);
        }

        public static bool TryBuildTypedServiceProviderHelperEdit(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax targetInvocation, out string replacement, out IReadOnlyList<string> requiredNamespaces, out InvocationExpressionSyntax? linkedInvocationToRemove)
        {
            linkedInvocationToRemove = null;
            if (TryBuildTypedServiceProviderHelperReplacement(invocationExpression, semanticModel, cancellationToken, out targetInvocation, out replacement, out requiredNamespaces))
            {
                return true;
            }

            if (TryBuildScopeServiceProviderSetupReplacement(invocationExpression, semanticModel, cancellationToken, out targetInvocation, out replacement, out requiredNamespaces, out linkedInvocationToRemove))
            {
                return true;
            }

            return TryBuildScopeFactoryCreateScopeReplacement(invocationExpression, semanticModel, cancellationToken, out targetInvocation, out replacement, out requiredNamespaces, out linkedInvocationToRemove);
        }

        public static bool TryGetFunctionContextInstanceServicesHelperSuggestion(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string currentApi)
        {
            currentApi = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) || method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name is not "Setup" and not "SetupGet" and not "SetupProperty")
            {
                return false;
            }

            if (invocationExpression.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            var candidateExpression = Unwrap(invocationExpression.ArgumentList.Arguments[0].Expression);
            if (candidateExpression is not LambdaExpressionSyntax lambdaExpression ||
                lambdaExpression.Body is not MemberAccessExpressionSyntax memberAccessExpression ||
                !TryGetPropertySymbol(memberAccessExpression, semanticModel, cancellationToken, out var property) ||
                property is null ||
                property.Name != FUNCTION_CONTEXT_INSTANCE_SERVICES_PROPERTY ||
                property.ContainingType.ToDisplayString() != FUNCTION_CONTEXT_TYPE)
            {
                return false;
            }

            currentApi = $"{method.Name}(x => x.InstanceServices)";
            return true;
        }

        public static bool TryGetFunctionContextInvocationIdHelperSuggestion(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string currentApi)
        {
            currentApi = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) || method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name is not "Setup" and not "SetupGet")
            {
                return false;
            }

            if (invocationExpression.Parent is not MemberAccessExpressionSyntax returnsAccess ||
                returnsAccess.Parent is not InvocationExpressionSyntax returnsInvocation ||
                !TryGetMethodSymbol(returnsInvocation, semanticModel, cancellationToken, out var returnsMethod) ||
                returnsMethod is null)
            {
                return false;
            }

            returnsMethod = returnsMethod.ReducedFrom ?? returnsMethod;
            if (returnsMethod.Name != "Returns")
            {
                return false;
            }

            if (!TryGetFunctionContextInvocationIdMemberAccess(invocationExpression, semanticModel, cancellationToken, out _))
            {
                return false;
            }

            currentApi = $"{method.Name}(x => x.InvocationId).Returns(...)";
            return true;
        }

        public static bool HasFunctionContextInstanceServicesMockHelper(SemanticModel semanticModel)
        {
            var helperType = semanticModel.Compilation.GetTypeByMetadataName("FastMoq.AzureFunctions.Extensions.FunctionContextTestExtensions");
            if (helperType is null)
            {
                return false;
            }

            return helperType
                .GetMembers("AddFunctionContextInstanceServices")
                .OfType<IMethodSymbol>()
                .Any(method =>
                    method.IsExtensionMethod &&
                    method.Parameters.Length == 2 &&
                    method.Parameters[0].Type.ToDisplayString() == "FastMoq.Providers.IFastMock" &&
                    method.Parameters[1].Type.ToDisplayString() == SERVICE_PROVIDER_TYPE);
        }

        public static bool HasFunctionContextInvocationIdMockHelper(SemanticModel semanticModel)
        {
            var helperType = semanticModel.Compilation.GetTypeByMetadataName("FastMoq.AzureFunctions.Extensions.FunctionContextTestExtensions");
            if (helperType is null)
            {
                return false;
            }

            return helperType
                .GetMembers("AddFunctionContextInvocationId")
                .OfType<IMethodSymbol>()
                .Any(method =>
                    method.IsExtensionMethod &&
                    method.Parameters.Length >= 2 &&
                    method.Parameters[0].Type.ToDisplayString() == "FastMoq.Providers.IFastMock" &&
                    method.Parameters[1].Type.SpecialType == SpecialType.System_String);
        }

        private static bool TryGetTrackedServiceGraphShimSuggestion(IMethodSymbol method, out string currentApi)
        {
            method = method.ReducedFrom ?? method;
            currentApi = string.Empty;

            if (!IsFastMoqMockerMethod(method, "GetOrCreateMock") &&
                !IsFastMoqMockerMethod(method, "GetMock") &&
                !IsFastMoqMockerMethod(method, "GetRequiredMock"))
            {
                return false;
            }

            if (method.TypeArguments.Length != 1 || !TryGetTypedServiceGraphShimTypeDisplay(method.TypeArguments[0], out var serviceTypeName))
            {
                return false;
            }

            currentApi = $"{method.Name}<{serviceTypeName}>()";
            return true;
        }

        private static bool TryGetScopeExtractionAddTypeSuggestion(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, IMethodSymbol method, out string currentApi)
        {
            currentApi = string.Empty;
            method = method.ReducedFrom ?? method;
            if (!IsFastMoqMockerAddTypeMethod(method) || invocationExpression.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            if (method.TypeArguments.Length == 0 || !TryGetTypedServiceGraphShimTypeDisplay(method.TypeArguments[0], out var addedTypeName))
            {
                return false;
            }

            var firstArgument = Unwrap(invocationExpression.ArgumentList.Arguments[0].Expression);
            if (firstArgument is not InvocationExpressionSyntax lookupInvocation ||
                !TryGetServiceProviderLookupTarget(lookupInvocation, semanticModel, cancellationToken, out var lookupTypeName, out var lookupApi) ||
                lookupTypeName != addedTypeName)
            {
                return false;
            }

            currentApi = $"AddType<{addedTypeName}>({lookupApi})";
            return true;
        }

        private static bool TryGetScopeExtractionLookupSuggestion(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string currentApi)
        {
            currentApi = string.Empty;
            if (!TryGetServiceProviderLookupTarget(invocationExpression, semanticModel, cancellationToken, out var serviceTypeName, out var lookupApi) ||
                invocationExpression.Parent is not ArgumentSyntax argumentSyntax ||
                argumentSyntax.Parent is not ArgumentListSyntax argumentListSyntax ||
                argumentListSyntax.Parent is not InvocationExpressionSyntax outerInvocation ||
                !TryGetMethodSymbol(outerInvocation, semanticModel, cancellationToken, out var outerMethod) ||
                outerMethod is null)
            {
                return false;
            }

            outerMethod = outerMethod.ReducedFrom ?? outerMethod;
            if (!IsFastMoqMockerAddTypeMethod(outerMethod))
            {
                return false;
            }

            currentApi = $"AddType<{serviceTypeName}>({lookupApi})";
            return true;
        }

        private static bool TryGetScopeShimSetupSuggestion(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, IMethodSymbol method, out string currentApi)
        {
            currentApi = string.Empty;
            method = method.ReducedFrom ?? method;
            if (method.Name is not "Setup" and not "SetupGet" and not "SetupProperty")
            {
                return false;
            }

            if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression ||
                !TryResolveTrackedMockOrigin(memberAccessExpression.Expression, semanticModel, cancellationToken, out var origin) ||
                invocationExpression.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            var candidateExpression = Unwrap(invocationExpression.ArgumentList.Arguments[0].Expression);
            if (origin.ServiceType.ToDisplayString() == SERVICE_SCOPE_FACTORY_TYPE &&
                candidateExpression is LambdaExpressionSyntax scopeFactoryLambda &&
                scopeFactoryLambda.Body is InvocationExpressionSyntax createScopeInvocation &&
                createScopeInvocation.Expression is MemberAccessExpressionSyntax createScopeAccess &&
                createScopeAccess.Name.Identifier.ValueText == "CreateScope")
            {
                currentApi = "Setup(x => x.CreateScope())";
                return true;
            }

            if (origin.ServiceType.ToDisplayString() == SERVICE_SCOPE_TYPE &&
                candidateExpression is LambdaExpressionSyntax scopeLambda &&
                scopeLambda.Body is MemberAccessExpressionSyntax propertyAccess &&
                propertyAccess.Name.Identifier.ValueText == "ServiceProvider")
            {
                currentApi = $"{method.Name}(x => x.ServiceProvider)";
                return true;
            }

            return false;
        }

        private static bool TryBuildTrackedServiceGraphShimReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax targetInvocation, out string replacement, out IReadOnlyList<string> requiredNamespaces)
        {
            targetInvocation = null!;
            replacement = string.Empty;
            requiredNamespaces = Array.Empty<string>();

            if (invocationExpression.Parent is MemberAccessExpressionSyntax ||
                !TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (!IsFastMoqMockerMethod(method, "GetOrCreateMock") &&
                !IsFastMoqMockerMethod(method, "GetMock") &&
                !IsFastMoqMockerMethod(method, "GetRequiredMock"))
            {
                return false;
            }

            if (method.TypeArguments.Length != 1 || invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
            {
                return false;
            }

            var mockerExpression = memberAccessExpression.Expression.WithoutTrivia().ToString();
            switch (method.TypeArguments[0].ToDisplayString())
            {
                case SERVICE_PROVIDER_TYPE:
                    targetInvocation = invocationExpression;
                    replacement = $"{mockerExpression}.CreateTypedServiceProvider()";
                    requiredNamespaces = ["FastMoq.Extensions"];
                    return true;

                case SERVICE_SCOPE_FACTORY_TYPE:
                    targetInvocation = invocationExpression;
                    replacement = $"{mockerExpression}.CreateTypedServiceProvider().GetRequiredService<IServiceScopeFactory>()";
                    requiredNamespaces = ["FastMoq.Extensions", "Microsoft.Extensions.DependencyInjection"];
                    return true;

                case SERVICE_SCOPE_TYPE:
                    targetInvocation = invocationExpression;
                    replacement = $"{mockerExpression}.CreateTypedServiceScope()";
                    requiredNamespaces = ["FastMoq.Extensions"];
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryBuildScopeExtractionReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax targetInvocation, out string replacement, out IReadOnlyList<string> requiredNamespaces)
        {
            targetInvocation = null!;
            replacement = string.Empty;
            requiredNamespaces = Array.Empty<string>();

            if (!TryGetServiceProviderLookupTarget(invocationExpression, semanticModel, cancellationToken, out var serviceTypeName, out _) ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax lookupAccess ||
                invocationExpression.Parent is not ArgumentSyntax argumentSyntax ||
                argumentSyntax.Parent is not ArgumentListSyntax argumentListSyntax ||
                argumentListSyntax.Parent is not InvocationExpressionSyntax outerInvocation ||
                !TryGetMethodSymbol(outerInvocation, semanticModel, cancellationToken, out var outerMethod) ||
                outerMethod is null)
            {
                return false;
            }

            outerMethod = outerMethod.ReducedFrom ?? outerMethod;
            if (!IsFastMoqMockerAddTypeMethod(outerMethod) ||
                outerInvocation.Expression is not MemberAccessExpressionSyntax outerAccess ||
                outerInvocation.ArgumentList.Arguments.Count == 0 ||
                outerInvocation.ArgumentList.Arguments.Count > 2)
            {
                return false;
            }

            if (outerInvocation.ArgumentList.Arguments.Count == 2 &&
                (outerMethod.Parameters.Length < 2 || outerMethod.Parameters[1].Type.SpecialType != SpecialType.System_Boolean))
            {
                return false;
            }

            var mockerExpression = outerAccess.Expression.WithoutTrivia().ToString();
            var receiverExpression = lookupAccess.Expression.WithoutTrivia().ToString();
            var trailingArguments = outerInvocation.ArgumentList.Arguments.Count == 2
                ? $", {outerInvocation.ArgumentList.Arguments[1].WithoutTrivia()}"
                : string.Empty;

            switch (serviceTypeName)
            {
                case "IServiceProvider":
                case "IServiceScopeFactory":
                    targetInvocation = outerInvocation;
                    replacement = $"{mockerExpression}.AddServiceProvider({receiverExpression}{trailingArguments})";
                    requiredNamespaces = ["FastMoq.Extensions"];
                    return true;

                case "IServiceScope":
                    targetInvocation = outerInvocation;
                    replacement = $"{mockerExpression}.AddServiceScope({invocationExpression.WithoutTrivia()}{trailingArguments})";
                    requiredNamespaces = ["FastMoq.Extensions"];
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryBuildScopeServiceProviderSetupReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax targetInvocation, out string replacement, out IReadOnlyList<string> requiredNamespaces, out InvocationExpressionSyntax? linkedInvocationToRemove)
        {
            linkedInvocationToRemove = null;
            targetInvocation = null!;
            replacement = string.Empty;
            requiredNamespaces = Array.Empty<string>();

            if (!TryGetScopeServiceProviderSetup(invocationExpression, semanticModel, cancellationToken, out var scopeOrigin, out var providerExpression, out var replacementTarget))
            {
                return false;
            }

            targetInvocation = replacementTarget;
            replacement = $"{scopeOrigin.MockerExpression.WithoutTrivia()}.AddServiceScope({providerExpression.WithoutTrivia()})";
            requiredNamespaces = ["FastMoq.Extensions"];

            if (TryFindMatchingScopeFactoryCreateScopeSetup(replacementTarget, scopeOrigin, semanticModel, cancellationToken, out var matchingInvocation))
            {
                linkedInvocationToRemove = matchingInvocation;
            }

            return true;
        }

        private static bool TryBuildScopeFactoryCreateScopeReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax targetInvocation, out string replacement, out IReadOnlyList<string> requiredNamespaces, out InvocationExpressionSyntax? linkedInvocationToRemove)
        {
            linkedInvocationToRemove = null;
            targetInvocation = null!;
            replacement = string.Empty;
            requiredNamespaces = Array.Empty<string>();

            if (!TryGetScopeFactoryCreateScopeSetup(invocationExpression, semanticModel, cancellationToken, out var scopeFactoryOrigin, out var scopeOrigin, out var replacementTarget) ||
                !TryFindMatchingScopeServiceProviderSetup(replacementTarget, scopeOrigin, semanticModel, cancellationToken, out var matchingInvocation, out var providerExpression))
            {
                return false;
            }

            targetInvocation = replacementTarget;
            replacement = $"{scopeFactoryOrigin.MockerExpression.WithoutTrivia()}.AddServiceScope({providerExpression.WithoutTrivia()})";
            requiredNamespaces = ["FastMoq.Extensions"];
            linkedInvocationToRemove = matchingInvocation;
            return true;
        }

        private static bool TryGetScopeServiceProviderSetup(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out TrackedMockOrigin origin, out ExpressionSyntax providerExpression, out InvocationExpressionSyntax targetInvocation)
        {
            origin = default;
            providerExpression = null!;
            targetInvocation = null!;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name is not "Setup" and not "SetupGet" and not "SetupProperty" ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression ||
                !TryResolveTrackedMockOrigin(memberAccessExpression.Expression, semanticModel, cancellationToken, out origin) ||
                origin.ServiceType.ToDisplayString() != SERVICE_SCOPE_TYPE ||
                invocationExpression.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            var candidateExpression = Unwrap(invocationExpression.ArgumentList.Arguments[0].Expression);
            if (candidateExpression is not LambdaExpressionSyntax scopeLambda ||
                scopeLambda.Body is not MemberAccessExpressionSyntax propertyAccess ||
                propertyAccess.Name.Identifier.ValueText != "ServiceProvider")
            {
                return false;
            }

            if (method.Name == "SetupProperty")
            {
                if (invocationExpression.ArgumentList.Arguments.Count != 2)
                {
                    return false;
                }

                providerExpression = invocationExpression.ArgumentList.Arguments[1].Expression;
                targetInvocation = invocationExpression;
                return true;
            }

            if (invocationExpression.Parent is not MemberAccessExpressionSyntax returnsAccess ||
                returnsAccess.Parent is not InvocationExpressionSyntax returnsInvocation ||
                !TryGetMethodSymbol(returnsInvocation, semanticModel, cancellationToken, out var returnsMethod) ||
                returnsMethod is null)
            {
                return false;
            }

            returnsMethod = returnsMethod.ReducedFrom ?? returnsMethod;
            if (returnsMethod.Name != "Returns" ||
                returnsInvocation.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            providerExpression = returnsInvocation.ArgumentList.Arguments[0].Expression;
            targetInvocation = returnsInvocation;
            return true;
        }

        private static bool TryGetScopeFactoryCreateScopeSetup(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out TrackedMockOrigin scopeFactoryOrigin, out TrackedMockOrigin scopeOrigin, out InvocationExpressionSyntax targetInvocation)
        {
            scopeFactoryOrigin = default;
            scopeOrigin = default;
            targetInvocation = null!;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name is not "Setup" and not "SetupGet" and not "SetupProperty" ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression ||
                !TryResolveTrackedMockOrigin(memberAccessExpression.Expression, semanticModel, cancellationToken, out scopeFactoryOrigin) ||
                scopeFactoryOrigin.ServiceType.ToDisplayString() != SERVICE_SCOPE_FACTORY_TYPE ||
                invocationExpression.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            var candidateExpression = Unwrap(invocationExpression.ArgumentList.Arguments[0].Expression);
            if (candidateExpression is not LambdaExpressionSyntax scopeFactoryLambda ||
                scopeFactoryLambda.Body is not InvocationExpressionSyntax createScopeInvocation ||
                createScopeInvocation.Expression is not MemberAccessExpressionSyntax createScopeAccess ||
                createScopeAccess.Name.Identifier.ValueText != "CreateScope" ||
                invocationExpression.Parent is not MemberAccessExpressionSyntax returnsAccess ||
                returnsAccess.Parent is not InvocationExpressionSyntax returnsInvocation ||
                !TryGetMethodSymbol(returnsInvocation, semanticModel, cancellationToken, out var returnsMethod) ||
                returnsMethod is null)
            {
                return false;
            }

            returnsMethod = returnsMethod.ReducedFrom ?? returnsMethod;
            if (returnsMethod.Name != "Returns" ||
                returnsInvocation.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            var returnedScopeExpression = Unwrap(returnsInvocation.ArgumentList.Arguments[0].Expression);
            if (returnedScopeExpression is not MemberAccessExpressionSyntax returnedScopeAccess ||
                returnedScopeAccess.Name.Identifier.ValueText != "Instance" ||
                !TryResolveTrackedMockOrigin(returnedScopeAccess.Expression, semanticModel, cancellationToken, out scopeOrigin) ||
                scopeOrigin.ServiceType.ToDisplayString() != SERVICE_SCOPE_TYPE)
            {
                return false;
            }

            targetInvocation = returnsInvocation;
            return true;
        }

        private static bool TryFindMatchingScopeServiceProviderSetup(SyntaxNode referenceNode, TrackedMockOrigin scopeOrigin, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax matchingInvocation, out ExpressionSyntax providerExpression)
        {
            matchingInvocation = null!;
            providerExpression = null!;

            foreach (var candidateInvocation in EnumerateCurrentBlockInvocations(referenceNode))
            {
                if (candidateInvocation.Span == referenceNode.Span)
                {
                    continue;
                }

                if (TryGetScopeServiceProviderSetup(candidateInvocation, semanticModel, cancellationToken, out var candidateOrigin, out var candidateProviderExpression, out var candidateTargetInvocation) &&
                    AreSameTrackedMockOrigin(scopeOrigin, candidateOrigin))
                {
                    matchingInvocation = candidateTargetInvocation;
                    providerExpression = candidateProviderExpression;
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindMatchingScopeFactoryCreateScopeSetup(SyntaxNode referenceNode, TrackedMockOrigin scopeOrigin, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax matchingInvocation)
        {
            matchingInvocation = null!;

            foreach (var candidateInvocation in EnumerateCurrentBlockInvocations(referenceNode))
            {
                if (candidateInvocation.Span == referenceNode.Span)
                {
                    continue;
                }

                if (TryGetScopeFactoryCreateScopeSetup(candidateInvocation, semanticModel, cancellationToken, out _, out var candidateScopeOrigin, out var candidateTargetInvocation) &&
                    AreSameTrackedMockOrigin(scopeOrigin, candidateScopeOrigin))
                {
                    matchingInvocation = candidateTargetInvocation;
                    return true;
                }
            }

            return false;
        }

        private static bool AreSameTrackedMockOrigin(TrackedMockOrigin left, TrackedMockOrigin right)
        {
            return SymbolEqualityComparer.Default.Equals(left.ServiceType, right.ServiceType) &&
                   SyntaxFactory.AreEquivalent(Unwrap(left.TrackedMockExpression), Unwrap(right.TrackedMockExpression)) &&
                   SyntaxFactory.AreEquivalent(Unwrap(left.MockerExpression), Unwrap(right.MockerExpression));
        }

        private static IEnumerable<InvocationExpressionSyntax> EnumerateCurrentBlockInvocations(SyntaxNode referenceNode)
        {
            if (referenceNode.FirstAncestorOrSelf<BlockSyntax>() is not BlockSyntax block)
            {
                yield break;
            }

            foreach (var statement in block.Statements)
            {
                if (statement is LocalFunctionStatementSyntax)
                {
                    continue;
                }

                foreach (var invocation in statement
                    .DescendantNodesAndSelf(static node => node is not BlockSyntax and not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax)
                    .OfType<InvocationExpressionSyntax>())
                {
                    yield return invocation;
                }
            }
        }

        private static bool TryGetServiceProviderLookupTarget(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string serviceTypeName, out string lookupApi)
        {
            serviceTypeName = string.Empty;
            lookupApi = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) || method is null)
            {
                return false;
            }

            var comparisonMethod = method.ReducedFrom ?? method;
            if (comparisonMethod.Name is not "GetRequiredService" and not "GetService")
            {
                return false;
            }

            if (method.TypeArguments.Length != 1 || !TryGetTypedServiceGraphShimTypeDisplay(method.TypeArguments[0], out serviceTypeName))
            {
                return false;
            }

            lookupApi = $"{method.Name}<{serviceTypeName}>()";
            return true;
        }

        private static bool TryGetTypedServiceGraphShimTypeDisplay(ITypeSymbol typeSymbol, out string serviceTypeName)
        {
            var metadataName = typeSymbol.ToDisplayString();
            if (metadataName == SERVICE_PROVIDER_TYPE)
            {
                serviceTypeName = "IServiceProvider";
                return true;
            }

            if (metadataName == SERVICE_SCOPE_FACTORY_TYPE)
            {
                serviceTypeName = "IServiceScopeFactory";
                return true;
            }

            if (metadataName == SERVICE_SCOPE_TYPE)
            {
                serviceTypeName = "IServiceScope";
                return true;
            }

            serviceTypeName = string.Empty;
            return false;
        }

        private static bool TryGetSetupSetProperty(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out TrackedMockOrigin origin, out IPropertySymbol property, out LambdaExpressionSyntax lambdaExpression)
        {
            origin = default;
            property = null!;
            lambdaExpression = null!;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name != "SetupSet" ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !TryResolveTrackedMockOrigin(memberAccess.Expression, semanticModel, cancellationToken, out origin) ||
                invocationExpression.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            var candidateExpression = Unwrap(invocationExpression.ArgumentList.Arguments[0].Expression);
            if (candidateExpression is not LambdaExpressionSyntax candidateLambda ||
                candidateLambda.Body is not AssignmentExpressionSyntax assignmentExpression ||
                assignmentExpression.Left is not MemberAccessExpressionSyntax propertyAccess ||
                !TryGetPropertySymbol(propertyAccess, semanticModel, cancellationToken, out var candidateProperty) ||
                candidateProperty is null)
            {
                return false;
            }

            property = candidateProperty;
            lambdaExpression = candidateLambda;
            return true;
        }

        private static bool TryBuildSetupSetPropertySelector(LambdaExpressionSyntax lambdaExpression, out string selector)
        {
            selector = string.Empty;
            if (lambdaExpression.Body is not AssignmentExpressionSyntax assignmentExpression ||
                assignmentExpression.Left is not MemberAccessExpressionSyntax propertyAccess)
            {
                return false;
            }

            var parameterName = lambdaExpression switch
            {
                SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.Parameter.Identifier.ValueText,
                ParenthesizedLambdaExpressionSyntax parenthesizedLambda when parenthesizedLambda.ParameterList.Parameters.Count == 1 => parenthesizedLambda.ParameterList.Parameters[0].Identifier.ValueText,
                _ => string.Empty,
            };

            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return false;
            }

            selector = $"{parameterName} => {propertyAccess.WithoutTrivia()}";
            return true;
        }

        private static bool TryBuildHttpContextRegistrationReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, IMethodSymbol method, string mockerExpression, out string replacement)
        {
            replacement = string.Empty;

            if (method.TypeArguments.Length != 1 || method.TypeArguments[0].ToDisplayString() != HTTP_CONTEXT_TYPE || invocationExpression.ArgumentList.Arguments.Count is 0 or > 2)
            {
                return false;
            }

            if (!TryGetSimpleFactoryExpression(invocationExpression.ArgumentList.Arguments[0].Expression, out var factoryExpression) ||
                semanticModel.GetTypeInfo(factoryExpression, cancellationToken).ConvertedType?.ToDisplayString() != HTTP_CONTEXT_TYPE)
            {
                return false;
            }

            var httpContextArgument = semanticModel.GetTypeInfo(factoryExpression, cancellationToken).Type?.ToDisplayString() == DEFAULT_HTTP_CONTEXT_TYPE
                ? null
                : factoryExpression.WithoutTrivia().ToString();
            var replaceArgument = TryBuildOptionalReplaceNamedArgument(invocationExpression, semanticModel, cancellationToken);
            replacement = BuildHttpContextHelperInvocation(mockerExpression, "AddHttpContext", httpContextArgument, replaceArgument);
            return true;
        }

        private static bool TryBuildHttpContextAccessorRegistrationReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, IMethodSymbol method, string mockerExpression, out string replacement)
        {
            replacement = string.Empty;

            var typeNames = new HashSet<string>(method.TypeArguments.Select(type => type.ToDisplayString()), StringComparer.Ordinal);
            if (!typeNames.Contains(IHTTP_CONTEXT_ACCESSOR_TYPE) && !typeNames.Contains(HTTP_CONTEXT_ACCESSOR_TYPE))
            {
                return false;
            }

            if (invocationExpression.ArgumentList.Arguments.Count is 0 or > 2 ||
                !TryGetSimpleFactoryExpression(invocationExpression.ArgumentList.Arguments[0].Expression, out var factoryExpression))
            {
                return false;
            }

            if (!TryGetHttpContextAccessorArgument(factoryExpression, semanticModel, cancellationToken, out var httpContextArgument))
            {
                return false;
            }

            var replaceArgument = TryBuildOptionalReplaceNamedArgument(invocationExpression, semanticModel, cancellationToken);
            replacement = BuildHttpContextHelperInvocation(mockerExpression, "AddHttpContextAccessor", httpContextArgument, replaceArgument);
            return true;
        }

        private static bool TryGetSimpleFactoryExpression(ExpressionSyntax expression, out ExpressionSyntax factoryExpression)
        {
            expression = Unwrap(expression);

            if (expression is ParenthesizedLambdaExpressionSyntax parenthesizedLambdaExpression)
            {
                if (parenthesizedLambdaExpression.Body is ExpressionSyntax lambdaExpressionBody)
                {
                    factoryExpression = Unwrap(lambdaExpressionBody);
                    return true;
                }

                if (parenthesizedLambdaExpression.Body is BlockSyntax lambdaBlock &&
                    lambdaBlock.Statements.Count == 1 &&
                    lambdaBlock.Statements[0] is ReturnStatementSyntax lambdaReturnStatement &&
                    lambdaReturnStatement.Expression is not null)
                {
                    factoryExpression = Unwrap(lambdaReturnStatement.Expression);
                    return true;
                }
            }

            if (expression is SimpleLambdaExpressionSyntax simpleLambdaExpression)
            {
                if (simpleLambdaExpression.Body is ExpressionSyntax lambdaExpressionBody)
                {
                    factoryExpression = Unwrap(lambdaExpressionBody);
                    return true;
                }

                if (simpleLambdaExpression.Body is BlockSyntax lambdaBlock &&
                    lambdaBlock.Statements.Count == 1 &&
                    lambdaBlock.Statements[0] is ReturnStatementSyntax lambdaReturnStatement &&
                    lambdaReturnStatement.Expression is not null)
                {
                    factoryExpression = Unwrap(lambdaReturnStatement.Expression);
                    return true;
                }
            }

            if (expression is AnonymousMethodExpressionSyntax anonymousMethodExpression &&
                anonymousMethodExpression.Body is BlockSyntax anonymousMethodBlock &&
                anonymousMethodBlock.Statements.Count == 1 &&
                anonymousMethodBlock.Statements[0] is ReturnStatementSyntax anonymousMethodReturnStatement &&
                anonymousMethodReturnStatement.Expression is not null)
            {
                factoryExpression = Unwrap(anonymousMethodReturnStatement.Expression);
                return true;
            }

            factoryExpression = null!;
            return false;
        }

        private static bool TryGetHttpContextAccessorArgument(ExpressionSyntax factoryExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string? httpContextArgument)
        {
            httpContextArgument = null;

            if (factoryExpression is not ObjectCreationExpressionSyntax objectCreationExpression ||
                semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken).Type?.ToDisplayString() != HTTP_CONTEXT_ACCESSOR_TYPE)
            {
                return false;
            }

            if (objectCreationExpression.Initializer is null || objectCreationExpression.Initializer.Expressions.Count == 0)
            {
                return true;
            }

            if (objectCreationExpression.Initializer.Expressions.Count != 1 ||
                objectCreationExpression.Initializer.Expressions[0] is not AssignmentExpressionSyntax assignmentExpression)
            {
                return false;
            }

            var propertyName = assignmentExpression.Left switch
            {
                IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
                MemberAccessExpressionSyntax memberAccessExpression => memberAccessExpression.Name.Identifier.ValueText,
                _ => string.Empty,
            };

            if (propertyName != "HttpContext")
            {
                return false;
            }

            var httpContextExpression = Unwrap(assignmentExpression.Right);
            var convertedTypeName = semanticModel.GetTypeInfo(httpContextExpression, cancellationToken).ConvertedType?.ToDisplayString();
            if (convertedTypeName != HTTP_CONTEXT_TYPE)
            {
                return false;
            }

            if (semanticModel.GetTypeInfo(httpContextExpression, cancellationToken).Type?.ToDisplayString() == DEFAULT_HTTP_CONTEXT_TYPE)
            {
                return true;
            }

            httpContextArgument = httpContextExpression.WithoutTrivia().ToString();
            return true;
        }

        private static string? TryBuildOptionalReplaceNamedArgument(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (invocationExpression.ArgumentList.Arguments.Count < 2)
            {
                return null;
            }

            var replaceExpression = invocationExpression.ArgumentList.Arguments[1].Expression;
            if (TryGetBooleanConstant(replaceExpression, semanticModel, cancellationToken, out var replaceConstant) && !replaceConstant)
            {
                return null;
            }

            return replaceExpression.WithoutTrivia().ToString();
        }

        private static string BuildHttpContextHelperInvocation(string mockerExpression, string helperName, string? primaryArgument, string? replaceArgument)
        {
            if (primaryArgument is null && replaceArgument is null)
            {
                return $"{mockerExpression}.{helperName}()";
            }

            if (primaryArgument is null)
            {
                return $"{mockerExpression}.{helperName}(replace: {replaceArgument})";
            }

            if (replaceArgument is null)
            {
                return $"{mockerExpression}.{helperName}({primaryArgument})";
            }

            return $"{mockerExpression}.{helperName}({primaryArgument}, replace: {replaceArgument})";
        }

        private static bool TryBuildFunctionContextInstanceServicesReturnsReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax targetInvocation, out string replacement)
        {
            targetInvocation = null!;
            replacement = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name is not "Setup" and not "SetupGet" ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax setupAccess ||
                !TryResolveTrackedMockOrigin(setupAccess.Expression, semanticModel, cancellationToken, out var origin) ||
                origin.ServiceType.ToDisplayString() != FUNCTION_CONTEXT_TYPE ||
                invocationExpression.Parent is not MemberAccessExpressionSyntax returnsAccess ||
                returnsAccess.Parent is not InvocationExpressionSyntax returnsInvocation ||
                !TryGetMethodSymbol(returnsInvocation, semanticModel, cancellationToken, out var returnsMethod) ||
                returnsMethod is null)
            {
                return false;
            }

            returnsMethod = returnsMethod.ReducedFrom ?? returnsMethod;
            if (returnsMethod.Name != "Returns" ||
                returnsInvocation.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            var providerExpression = Unwrap(returnsInvocation.ArgumentList.Arguments[0].Expression);
            if (providerExpression is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax)
            {
                return false;
            }

            if (!TryGetFunctionContextInstanceServicesMemberAccess(invocationExpression, semanticModel, cancellationToken, out _))
            {
                return false;
            }

            targetInvocation = returnsInvocation;
            replacement = BuildFunctionContextInstanceServicesReplacement(origin.TrackedMockExpression, providerExpression);
            return true;
        }

        private static bool TryBuildFunctionContextInvocationIdReturnsReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax targetInvocation, out string replacement)
        {
            targetInvocation = null!;
            replacement = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name is not "Setup" and not "SetupGet" ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax setupAccess ||
                !TryResolveTrackedMockOrigin(setupAccess.Expression, semanticModel, cancellationToken, out var origin) ||
                origin.ServiceType.ToDisplayString() != FUNCTION_CONTEXT_TYPE ||
                invocationExpression.Parent is not MemberAccessExpressionSyntax returnsAccess ||
                returnsAccess.Parent is not InvocationExpressionSyntax returnsInvocation ||
                !TryGetMethodSymbol(returnsInvocation, semanticModel, cancellationToken, out var returnsMethod) ||
                returnsMethod is null)
            {
                return false;
            }

            returnsMethod = returnsMethod.ReducedFrom ?? returnsMethod;
            if (returnsMethod.Name != "Returns" ||
                returnsInvocation.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            var invocationIdExpression = Unwrap(returnsInvocation.ArgumentList.Arguments[0].Expression);
            if (invocationIdExpression is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax)
            {
                return false;
            }

            if (!TryGetFunctionContextInvocationIdMemberAccess(invocationExpression, semanticModel, cancellationToken, out _))
            {
                return false;
            }

            targetInvocation = returnsInvocation;
            replacement = BuildFunctionContextInvocationIdReplacement(origin.TrackedMockExpression, invocationIdExpression);
            return true;
        }

        private static bool TryBuildFunctionContextInstanceServicesSetupPropertyReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax targetInvocation, out string replacement)
        {
            targetInvocation = null!;
            replacement = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name != "SetupProperty" ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !TryResolveTrackedMockOrigin(memberAccess.Expression, semanticModel, cancellationToken, out var origin) ||
                origin.ServiceType.ToDisplayString() != FUNCTION_CONTEXT_TYPE ||
                invocationExpression.ArgumentList.Arguments.Count < 2)
            {
                return false;
            }

            if (!TryGetFunctionContextInstanceServicesMemberAccess(invocationExpression, semanticModel, cancellationToken, out _))
            {
                return false;
            }

            var providerExpression = Unwrap(invocationExpression.ArgumentList.Arguments[1].Expression);
            if (providerExpression is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax)
            {
                return false;
            }

            targetInvocation = invocationExpression;
            replacement = BuildFunctionContextInstanceServicesReplacement(origin.TrackedMockExpression, providerExpression);
            return true;
        }

        private static bool TryGetFunctionContextInstanceServicesMemberAccess(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out MemberAccessExpressionSyntax memberAccessExpression)
        {
            memberAccessExpression = null!;

            if (invocationExpression.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            var candidateExpression = Unwrap(invocationExpression.ArgumentList.Arguments[0].Expression);
            if (candidateExpression is not LambdaExpressionSyntax lambdaExpression ||
                lambdaExpression.Body is not MemberAccessExpressionSyntax memberAccess ||
                !TryGetPropertySymbol(memberAccess, semanticModel, cancellationToken, out var property) ||
                property is null ||
                property.Name != FUNCTION_CONTEXT_INSTANCE_SERVICES_PROPERTY ||
                property.ContainingType.ToDisplayString() != FUNCTION_CONTEXT_TYPE)
            {
                return false;
            }

            memberAccessExpression = memberAccess;
            return true;
        }

        private static bool TryGetFunctionContextInvocationIdMemberAccess(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out MemberAccessExpressionSyntax memberAccessExpression)
        {
            memberAccessExpression = null!;

            if (invocationExpression.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            var candidateExpression = Unwrap(invocationExpression.ArgumentList.Arguments[0].Expression);
            if (candidateExpression is not LambdaExpressionSyntax lambdaExpression ||
                lambdaExpression.Body is not MemberAccessExpressionSyntax memberAccess ||
                !TryGetPropertySymbol(memberAccess, semanticModel, cancellationToken, out var property) ||
                property is null ||
                property.Name != FUNCTION_CONTEXT_INVOCATION_ID_PROPERTY ||
                property.ContainingType.ToDisplayString() != FUNCTION_CONTEXT_TYPE)
            {
                return false;
            }

            memberAccessExpression = memberAccess;
            return true;
        }

        public static bool TryBuildFunctionContextInvocationIdReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax targetInvocation, out string replacement)
        {
            return TryBuildFunctionContextInvocationIdReturnsReplacement(invocationExpression, semanticModel, cancellationToken, out targetInvocation, out replacement);
        }

        public static bool TryGetKnownTypeRegistrationSuggestion(IMethodSymbol method, out string currentApi)
        {
            method = method.ReducedFrom ?? method;
            currentApi = string.Empty;

            if (!IsFastMoqMockerMethod(method, "AddType"))
            {
                return false;
            }

            if (!method.Parameters.Any(parameter => IsContextAwareFactoryParameter(parameter.Type)))
            {
                return false;
            }

            currentApi = "AddType(...)";
            return true;
        }

        public static bool TryGetUnkeyedDependencyCandidate(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out ITypeSymbol? serviceType, out string apiName)
        {
            serviceType = null;
            apiName = string.Empty;

            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) || method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;

            if ((IsFastMoqMockerMethod(method, "GetOrCreateMock") ||
                 IsFastMoqMockerMethod(method, "GetMock") ||
                 IsFastMoqMockerMethod(method, "GetRequiredMock")) &&
                method.TypeArguments.Length == 1)
            {
                if (IsFastMoqMockerMethod(method, "GetOrCreateMock") && invocationExpression.ArgumentList.Arguments.Count > 0)
                {
                    if (invocationExpression.ArgumentList.Arguments.Any(argument => ContainsServiceKeyAssignment(argument.Expression, semanticModel, cancellationToken)))
                    {
                        return false;
                    }

                    return false;
                }

                serviceType = method.TypeArguments[0];
                apiName = method.Name + "<>()";
                return true;
            }

            if (!IsFastMoqMockerMethod(method, "AddType") || method.TypeArguments.Length == 0 || method.Parameters.Any(parameter => IsContextAwareFactoryParameter(parameter.Type)))
            {
                return false;
            }

            serviceType = method.TypeArguments[0];
            apiName = method.TypeArguments.Length == 1 ? "AddType<T>(...)" : "AddType<TInterface, TClass>(...)";
            return true;
        }

        public static bool DocumentContainsKeyedRegistration(SyntaxNode root, SemanticModel semanticModel, ITypeSymbol serviceType, CancellationToken cancellationToken)
        {
            foreach (var invocationExpression in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) || method is null)
                {
                    continue;
                }

                method = method.ReducedFrom ?? method;
                if (IsFastMoqMockerMethod(method, "AddKeyedType") && method.TypeArguments.Length > 0 && SymbolEqualityComparer.Default.Equals(method.TypeArguments[0], serviceType))
                {
                    return true;
                }

                if (IsFastMoqMockerMethod(method, "GetOrCreateMock") && method.TypeArguments.Length == 1 && SymbolEqualityComparer.Default.Equals(method.TypeArguments[0], serviceType))
                {
                    if (invocationExpression.ArgumentList.Arguments.Any(argument => ContainsServiceKeyAssignment(argument.Expression, semanticModel, cancellationToken)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryGetTargetTypeWithDuplicateKeyedDependency(SyntaxNode node, SemanticModel semanticModel, ITypeSymbol serviceType, CancellationToken cancellationToken, out string targetTypeName)
        {
            foreach (var targetType in GetCandidateTestTargetTypes(node, semanticModel, cancellationToken))
            {
                if (HasDuplicateKeyedDependencies(targetType, serviceType))
                {
                    targetTypeName = targetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                    return true;
                }
            }

            targetTypeName = string.Empty;
            return false;
        }

        private static bool ContainsServiceKeyAssignment(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            expression = Unwrap(expression);

            if (expression is ObjectCreationExpressionSyntax objectCreationExpression)
            {
                return objectCreationExpression.Initializer?.Expressions
                    .OfType<AssignmentExpressionSyntax>()
                    .Any(assignment => assignment.Left.ToString().EndsWith("ServiceKey", StringComparison.Ordinal)) == true;
            }

            if (expression is ImplicitObjectCreationExpressionSyntax implicitObjectCreationExpression)
            {
                return implicitObjectCreationExpression.Initializer?.Expressions
                    .OfType<AssignmentExpressionSyntax>()
                    .Any(assignment => assignment.Left.ToString().EndsWith("ServiceKey", StringComparison.Ordinal)) == true;
            }

            if (expression is IdentifierNameSyntax identifierName && semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol is ILocalSymbol localSymbol)
            {
                var declaration = localSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) as VariableDeclaratorSyntax;
                if (declaration?.Initializer?.Value is ExpressionSyntax initializer)
                {
                    return ContainsServiceKeyAssignment(initializer, semanticModel, cancellationToken);
                }
            }

            return false;
        }

        private static bool TryGetCreateHttpContextRequestAssignment(AssignmentExpressionSyntax assignmentExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax createHttpContextInvocation, out string propertyName, out ExpressionSyntax assignedExpression)
        {
            createHttpContextInvocation = null!;
            propertyName = string.Empty;
            assignedExpression = null!;

            if (assignmentExpression.Left is not MemberAccessExpressionSyntax targetAccess ||
                targetAccess.Expression is not MemberAccessExpressionSyntax requestAccess ||
                requestAccess.Name.Identifier.ValueText != "Request" ||
                requestAccess.Expression is not InvocationExpressionSyntax candidateInvocation ||
                !TryGetMethodSymbol(candidateInvocation, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name != "CreateHttpContext" ||
                method.ContainingType.ToDisplayString() != FastMoqWebExtensionsTypeName ||
                targetAccess.Name.Identifier.ValueText is not "Body" and not "ContentType")
            {
                return false;
            }

            createHttpContextInvocation = candidateInvocation;
            propertyName = targetAccess.Name.Identifier.ValueText;
            assignedExpression = assignmentExpression.Right;
            return true;
        }

        private static bool TryFindSiblingCreateHttpContextRequestAssignment(ExpressionStatementSyntax referenceStatement, InvocationExpressionSyntax createHttpContextInvocation, string propertyName, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax matchingAssignment)
        {
            matchingAssignment = null!;

            if (referenceStatement.Parent is not BlockSyntax block)
            {
                return false;
            }

            foreach (var statement in block.Statements.OfType<ExpressionStatementSyntax>())
            {
                if (statement == referenceStatement || statement.Expression is not AssignmentExpressionSyntax assignmentExpression)
                {
                    continue;
                }

                if (!TryGetCreateHttpContextRequestAssignment(assignmentExpression, semanticModel, cancellationToken, out var candidateInvocation, out var candidatePropertyName, out _))
                {
                    continue;
                }

                if (candidatePropertyName == propertyName && SyntaxFactory.AreEquivalent(Unwrap(candidateInvocation), Unwrap(createHttpContextInvocation)))
                {
                    matchingAssignment = assignmentExpression;
                    return true;
                }
            }

            return false;
        }

        private static bool IsSupportedRequestBodyExpression(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            expression = Unwrap(expression);
            if (expression is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax)
            {
                return false;
            }

            var convertedType = semanticModel.GetTypeInfo(expression, cancellationToken).ConvertedType;
            return convertedType?.ToDisplayString() == STREAM_TYPE || convertedType?.SpecialType == SpecialType.System_String;
        }

        private static string BuildWebRequestBodyReplacement(InvocationExpressionSyntax createHttpContextInvocation, ExpressionSyntax bodyExpression, ExpressionSyntax? contentTypeExpression)
        {
            var createHttpContextText = createHttpContextInvocation.WithoutTrivia().ToString();
            var bodyExpressionText = bodyExpression.WithoutTrivia().ToString();
            if (contentTypeExpression is null)
            {
                return $"{createHttpContextText}.SetRequestBody({bodyExpressionText});";
            }

            return $"{createHttpContextText}.SetRequestBody({bodyExpressionText}, {contentTypeExpression.WithoutTrivia()});";
        }

        private static bool IsControllerContextHttpContextInitializer(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (node.Parent is not AssignmentExpressionSyntax assignmentExpression ||
                assignmentExpression.Left is not IdentifierNameSyntax identifierName ||
                identifierName.Identifier.ValueText != "HttpContext")
            {
                return false;
            }

            return assignmentExpression
                .Ancestors()
                .OfType<BaseObjectCreationExpressionSyntax>()
                .Any(objectCreation => semanticModel.GetTypeInfo(objectCreation, cancellationToken).Type?.ToDisplayString() == CONTROLLER_CONTEXT_TYPE);
        }

        private static bool IsInsideFastMoqWebRegistration(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var invocationExpression in node.Ancestors().OfType<InvocationExpressionSyntax>())
            {
                if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) || method is null)
                {
                    continue;
                }

                if (TryGetFastMoqWebHelperSuggestion(method, out _, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryBuildSetupOptionsReturnsReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacement)
        {
            replacement = string.Empty;
            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name != "Returns" ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax returnsAccess ||
                invocationExpression.ArgumentList.Arguments.Count != 1 ||
                returnsAccess.Expression is not InvocationExpressionSyntax setupInvocation ||
                !TryGetMethodSymbol(setupInvocation, semanticModel, cancellationToken, out var setupMethod) ||
                setupMethod is null)
            {
                return false;
            }

            setupMethod = setupMethod.ReducedFrom ?? setupMethod;
            if (setupMethod.Name is not "Setup" and not "SetupGet" ||
                setupInvocation.Expression is not MemberAccessExpressionSyntax setupAccess ||
                !TryResolveTrackedMockOrigin(setupAccess.Expression, semanticModel, cancellationToken, out var origin) ||
                !TryGetIOptionsValueType(origin.ServiceType, out var optionsType) ||
                !IsOptionsValueAccessorSetup(setupInvocation) ||
                !TryGetSetupOptionsArgumentText(invocationExpression.ArgumentList.Arguments[0].Expression, out var setupArgument))
            {
                return false;
            }

            replacement = BuildSetupOptionsReplacement(origin.MockerExpression, optionsType, setupArgument, null, semanticModel, invocationExpression.SpanStart);
            return true;
        }

        private static bool TryBuildSetupOptionsAddTypeReplacement(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacement)
        {
            replacement = string.Empty;
            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (!IsFastMoqMockerMethod(method, "AddType") ||
                method.TypeArguments.Length != 1 ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !TryGetIOptionsValueType(method.TypeArguments[0], out var optionsType) ||
                invocationExpression.ArgumentList.Arguments.Count is 0 or > 2 ||
                !TryGetSetupOptionsArgumentFromAddType(invocationExpression.ArgumentList.Arguments[0].Expression, semanticModel, cancellationToken, out var setupArgument))
            {
                return false;
            }

            string? replaceArgument = null;
            if (invocationExpression.ArgumentList.Arguments.Count == 2)
            {
                var replaceExpression = invocationExpression.ArgumentList.Arguments[1].Expression;
                if (!TryGetBooleanConstant(replaceExpression, semanticModel, cancellationToken, out var replaceConstant) || replaceConstant)
                {
                    replaceArgument = replaceExpression.WithoutTrivia().ToString();
                }
            }

            replacement = BuildSetupOptionsReplacement(memberAccess.Expression, optionsType, setupArgument, replaceArgument, semanticModel, invocationExpression.SpanStart);
            return true;
        }

        private static bool TryGetIOptionsValueType(ITypeSymbol type, out ITypeSymbol optionsType)
        {
            if (type is INamedTypeSymbol namedType &&
                namedType.IsGenericType &&
                namedType.Name == "IOptions" &&
                namedType.ContainingNamespace.ToDisplayString() == "Microsoft.Extensions.Options" &&
                namedType.TypeArguments.Length == 1)
            {
                optionsType = namedType.TypeArguments[0];
                return true;
            }

            optionsType = default!;
            return false;
        }

        private static bool IsOptionsValueAccessorSetup(InvocationExpressionSyntax setupInvocation)
        {
            if (setupInvocation.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            var candidateExpression = Unwrap(setupInvocation.ArgumentList.Arguments[0].Expression);
            if (candidateExpression is not LambdaExpressionSyntax lambdaExpression ||
                lambdaExpression.Body is not ExpressionSyntax bodyExpression)
            {
                return false;
            }

            return Unwrap(bodyExpression) is MemberAccessExpressionSyntax memberAccessExpression &&
                memberAccessExpression.Name.Identifier.ValueText == "Value";
        }

        private static bool TryGetSetupOptionsArgumentText(ExpressionSyntax expression, out string setupArgument)
        {
            expression = Unwrap(expression);

            if (IsNullLikeExpression(expression))
            {
                setupArgument = string.Empty;
                return false;
            }

            if (expression is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
            {
                if (parenthesizedLambda.ParameterList.Parameters.Count != 0 || parenthesizedLambda.Body is not ExpressionSyntax)
                {
                    setupArgument = string.Empty;
                    return false;
                }

                setupArgument = parenthesizedLambda.WithoutTrivia().ToString();
                return true;
            }

            setupArgument = expression.WithoutTrivia().ToString();
            return true;
        }

        private static bool TryGetSetupOptionsArgumentFromAddType(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out string setupArgument)
        {
            expression = Unwrap(expression);

            if (TryUnwrapOptionsCreateValueExpression(expression, semanticModel, cancellationToken, out var valueExpression))
            {
                if (IsNullLikeExpression(valueExpression))
                {
                    setupArgument = string.Empty;
                    return false;
                }

                setupArgument = valueExpression.WithoutTrivia().ToString();
                return true;
            }

            if (expression is LambdaExpressionSyntax lambdaExpression &&
                TryBuildSetupOptionsFactoryArgument(lambdaExpression, semanticModel, cancellationToken, out setupArgument))
            {
                return true;
            }

            setupArgument = string.Empty;
            return false;
        }

        private static bool TryBuildSetupOptionsFactoryArgument(LambdaExpressionSyntax lambdaExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string setupArgument)
        {
            setupArgument = string.Empty;
            if (lambdaExpression.Body is not ExpressionSyntax bodyExpression ||
                !TryUnwrapOptionsCreateValueExpression(bodyExpression, semanticModel, cancellationToken, out var valueExpression) ||
                IsNullLikeExpression(valueExpression))
            {
                return false;
            }

            if (LambdaReferencesParameters(lambdaExpression, valueExpression))
            {
                return false;
            }

            setupArgument = $"() => {valueExpression.WithoutTrivia()}";
            return true;
        }

        private static bool TryUnwrapOptionsCreateValueExpression(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax valueExpression)
        {
            expression = Unwrap(expression);
            if (expression is InvocationExpressionSyntax invocationExpression &&
                TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) &&
                method is not null)
            {
                method = method.ReducedFrom ?? method;
                if (method.Name == "Create" &&
                    method.ContainingType.ToDisplayString() == "Microsoft.Extensions.Options.Options" &&
                    invocationExpression.ArgumentList.Arguments.Count == 1)
                {
                    valueExpression = invocationExpression.ArgumentList.Arguments[0].Expression;
                    return true;
                }
            }

            valueExpression = default!;
            return false;
        }

        private static bool LambdaReferencesParameters(LambdaExpressionSyntax lambdaExpression, ExpressionSyntax bodyExpression)
        {
            IEnumerable<string> parameterNames = lambdaExpression switch
            {
                SimpleLambdaExpressionSyntax simpleLambda => [simpleLambda.Parameter.Identifier.ValueText],
                ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.ParameterList.Parameters.Select(parameter => parameter.Identifier.ValueText),
                _ => []
            };

            var parameterNameSet = new HashSet<string>(parameterNames, StringComparer.Ordinal);
            if (parameterNameSet.Count == 0)
            {
                return false;
            }

            return bodyExpression.DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .Any(identifier => parameterNameSet.Contains(identifier.Identifier.ValueText));
        }

        private static bool IsNullLikeExpression(ExpressionSyntax expression)
        {
            expression = Unwrap(expression);
            return expression is LiteralExpressionSyntax literalExpression && literalExpression.Token.Value is null ||
                expression is DefaultExpressionSyntax ||
                expression.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.DefaultLiteralExpression);
        }

        private static string BuildSetupOptionsReplacement(ExpressionSyntax mockerExpressionSyntax, ITypeSymbol optionsType, string setupArgument, string? replaceArgument, SemanticModel semanticModel, int position)
        {
            var mockerExpression = mockerExpressionSyntax.WithoutTrivia().ToString();
            var optionsTypeName = GetMinimalTypeName(optionsType, semanticModel, position);
            return string.IsNullOrWhiteSpace(replaceArgument)
                ? $"{mockerExpression}.SetupOptions<{optionsTypeName}>({setupArgument})"
                : $"{mockerExpression}.SetupOptions<{optionsTypeName}>({setupArgument}, replace: {replaceArgument})";
        }

        private static string BuildFunctionContextInstanceServicesReplacement(ExpressionSyntax trackedMockExpressionSyntax, ExpressionSyntax providerExpressionSyntax)
        {
            var trackedMockExpression = trackedMockExpressionSyntax.WithoutTrivia().ToString();
            var providerExpression = providerExpressionSyntax.WithoutTrivia().ToString();
            return $"{trackedMockExpression}.AddFunctionContextInstanceServices({providerExpression})";
        }

        private static string BuildFunctionContextInvocationIdReplacement(ExpressionSyntax trackedMockExpressionSyntax, ExpressionSyntax invocationIdExpressionSyntax)
        {
            var trackedMockExpression = trackedMockExpressionSyntax.WithoutTrivia().ToString();
            var invocationIdExpression = invocationIdExpressionSyntax.WithoutTrivia().ToString();
            return $"{trackedMockExpression}.AddFunctionContextInvocationId({invocationIdExpression})";
        }

        private static IEnumerable<INamedTypeSymbol> GetCandidateTestTargetTypes(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var results = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            if (TryGetContainingMockerTestBaseTargetType(node, semanticModel, cancellationToken, out var baseTargetType))
            {
                results.Add(baseTargetType);
            }

            var scope = node.AncestorsAndSelf().FirstOrDefault(ancestor =>
                ancestor is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)
                ?? node.SyntaxTree.GetRoot(cancellationToken);

            foreach (var invocationExpression in scope.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) || method is null)
                {
                    continue;
                }

                method = method.ReducedFrom ?? method;
                if (IsFastMoqMockerMethod(method, "CreateInstance") && method.TypeArguments.Length == 1 && method.TypeArguments[0] is INamedTypeSymbol createInstanceType)
                {
                    results.Add(createInstanceType);
                }
            }

            return results;
        }

        private static bool TryGetContainingMockerTestBaseTargetType(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken, out INamedTypeSymbol targetType)
        {
            var typeDeclaration = node.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (typeDeclaration is null || semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is not INamedTypeSymbol containingType)
            {
                targetType = default!;
                return false;
            }

            for (var current = containingType.BaseType; current is not null; current = current.BaseType)
            {
                if (current.OriginalDefinition.MetadataName == FASTMOQ_MOCKER_TEST_BASE_METADATA_NAME &&
                    current.OriginalDefinition.ContainingNamespace.ToDisplayString() == "FastMoq" &&
                    current.TypeArguments.Length == 1 &&
                    current.TypeArguments[0] is INamedTypeSymbol namedTargetType)
                {
                    targetType = namedTargetType;
                    return true;
                }
            }

            targetType = default!;
            return false;
        }

        public static bool IsMoqVerifyMethod(IMethodSymbol method)
        {
            method = method.ReducedFrom ?? method;
            return method.Name == "Verify" &&
                   method.ContainingType.Name == "Mock" &&
                   method.ContainingType.ContainingNamespace.ToDisplayString() == "Moq";
        }

        public static bool TryGetMoqMockedType(ITypeSymbol? type, out ITypeSymbol mockedType)
        {
            if (type is INamedTypeSymbol namedType &&
                namedType.ContainingNamespace.ToDisplayString() == "Moq" &&
                namedType.Name == "Mock" &&
                namedType.TypeArguments.Length == 1)
            {
                mockedType = namedType.TypeArguments[0];
                return true;
            }

            mockedType = default!;
            return false;
        }

        public static bool TryGetCreatedMoqMockedType(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken, out ITypeSymbol mockedType)
        {
            ITypeSymbol? createdType = node switch
            {
                ObjectCreationExpressionSyntax objectCreationExpression => semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken).Type,
                ImplicitObjectCreationExpressionSyntax implicitObjectCreationExpression => semanticModel.GetTypeInfo(implicitObjectCreationExpression, cancellationToken).Type,
                _ => null,
            };

            return TryGetMoqMockedType(createdType, out mockedType);
        }

        public static bool IsInsideFastMoqTestInfrastructure(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (TryGetContainingMockerTestBaseTargetType(node, semanticModel, cancellationToken, out _))
            {
                return true;
            }

            if (node.AncestorsAndSelf().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault() is { } methodDeclaration &&
                semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken) is IMethodSymbol methodSymbol &&
                methodSymbol.Parameters.Any(parameter => parameter.Type.ToDisplayString() == FastMoqMockerTypeName))
            {
                return true;
            }

            if (node.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault() is { } typeDeclaration &&
                semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is INamedTypeSymbol containingType &&
                containingType.GetMembers().Any(member => member switch
                {
                    IFieldSymbol fieldSymbol => fieldSymbol.Type.ToDisplayString() == FastMoqMockerTypeName,
                    IPropertySymbol propertySymbol => propertySymbol.Type.ToDisplayString() == FastMoqMockerTypeName,
                    _ => false,
                }))
            {
                return true;
            }

            var scope = GetAnalysisScope(node, cancellationToken);
            return scope.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(invocationExpression =>
                    TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) &&
                    method is not null &&
                    method.ContainingType.ToDisplayString() == FastMoqMockerTypeName) ||
                scope.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>().Any(objectCreationExpression =>
                    semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken).Type?.ToDisplayString() == FastMoqMockerTypeName) ||
                scope.DescendantNodesAndSelf().OfType<ImplicitObjectCreationExpressionSyntax>().Any(implicitObjectCreationExpression =>
                    semanticModel.GetTypeInfo(implicitObjectCreationExpression, cancellationToken).Type?.ToDisplayString() == FastMoqMockerTypeName);
        }

        public static string GetRawMockCreationGuidance(SyntaxNode node, ITypeSymbol serviceType, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var serviceTypeName = GetMinimalTypeName(serviceType, semanticModel, node.SpanStart);
            return CountRawMoqMockCreations(node, serviceType, semanticModel, cancellationToken) > 1
                ? $"'CreateStandaloneFastMock<{serviceTypeName}>()' for additional independent handles of the same service type"
                : $"'GetOrCreateMock<{serviceTypeName}>()' for the tracked single-instance path";
        }

        private static int CountRawMoqMockCreations(SyntaxNode node, ITypeSymbol serviceType, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var scope = GetAnalysisScope(node, cancellationToken);
            return scope.DescendantNodesAndSelf().Count(candidate =>
                TryGetCreatedMoqMockedType(candidate, semanticModel, cancellationToken, out var createdServiceType) &&
                SymbolEqualityComparer.Default.Equals(createdServiceType, serviceType));
        }

        private static SyntaxNode GetAnalysisScope(SyntaxNode node, CancellationToken cancellationToken)
        {
            return node.AncestorsAndSelf().FirstOrDefault(ancestor =>
                       ancestor is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax or TypeDeclarationSyntax)
                   ?? node.SyntaxTree.GetRoot(cancellationToken);
        }

        private static bool HasDuplicateKeyedDependencies(INamedTypeSymbol targetType, ITypeSymbol serviceType)
        {
            foreach (var constructor in targetType.InstanceConstructors)
            {
                if (constructor.IsStatic)
                {
                    continue;
                }

                var keyedParameters = constructor.Parameters
                    .Where(parameter => SymbolEqualityComparer.Default.Equals(parameter.Type, serviceType) && HasFromKeyedServicesAttribute(parameter))
                    .ToArray();

                if (keyedParameters.Length >= 2)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasFromKeyedServicesAttribute(IParameterSymbol parameter)
        {
            return parameter.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == FROM_KEYED_SERVICES_ATTRIBUTE);
        }

        private static bool IsContextAwareFactoryParameter(ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol namedType || namedType.DelegateInvokeMethod is null)
            {
                return false;
            }

            var invokeMethod = namedType.DelegateInvokeMethod;
            return namedType.Name == "Func" &&
                   invokeMethod.Parameters.Length == 2 &&
                     invokeMethod.Parameters[0].Type.ToDisplayString() == FastMoqMockerTypeName;
        }

        public static bool IsProviderSelectedByDefault(Compilation compilation, string providerName, CancellationToken cancellationToken)
        {
            if (HasAssemblyDefaultProvider(compilation.Assembly, providerName))
            {
                return true;
            }

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = syntaxTree.GetRoot(cancellationToken);

                foreach (var invocationExpression in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (IsProviderSelectionInvocation(invocationExpression, semanticModel, providerName, requireDefaultSelection: true, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool HasAssemblyDefaultProvider(IAssemblySymbol assemblySymbol, string providerName)
        {
            return TryGetAssemblyDefaultProviderName(assemblySymbol, out var assemblyDefaultProvider) &&
                   string.Equals(assemblyDefaultProvider, providerName, StringComparison.OrdinalIgnoreCase)
                   || HasAssemblyRegisteredDefaultProvider(assemblySymbol, providerName);
        }

        public static bool TryGetAssemblyDefaultProviderName(IAssemblySymbol assemblySymbol, out string providerName)
        {
            foreach (var attribute in assemblySymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != FASTMOQ_DEFAULT_PROVIDER_ATTRIBUTE ||
                    attribute.ConstructorArguments.Length != 1 ||
                    attribute.ConstructorArguments[0].Value is not string declaredProvider ||
                    string.IsNullOrWhiteSpace(declaredProvider))
                {
                    continue;
                }

                providerName = declaredProvider;
                return true;
            }

            providerName = string.Empty;
            return false;
        }

        public static bool HasAssemblyRegisteredDefaultProvider(IAssemblySymbol assemblySymbol, string providerName)
        {
            foreach (var attribute in assemblySymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != FASTMOQ_REGISTER_PROVIDER_ATTRIBUTE ||
                    attribute.ConstructorArguments.Length < 2 ||
                    attribute.ConstructorArguments[0].Value is not string declaredProvider ||
                    string.IsNullOrWhiteSpace(declaredProvider))
                {
                    continue;
                }

                var setAsDefault = attribute.NamedArguments.Any(argument =>
                    argument.Key == RegisterProviderSetAsDefaultPropertyName &&
                    argument.Value.Value is bool isDefault &&
                    isDefault);

                if (setAsDefault && string.Equals(declaredProvider, providerName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasProviderSelectionInScope(SyntaxNode node, SemanticModel semanticModel, string providerName, CancellationToken cancellationToken)
        {
            foreach (var scope in EnumerateProviderSelectionScopes(node, cancellationToken))
            {
                foreach (var invocationExpression in EnumerateScopeInvocations(scope))
                {
                    if (IsProviderSelectionInvocation(invocationExpression, semanticModel, providerName, requireDefaultSelection: false, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static IEnumerable<SyntaxNode> EnumerateProviderSelectionScopes(SyntaxNode node, CancellationToken cancellationToken)
        {
            var seenScopes = new HashSet<SyntaxNode>();
            SyntaxNode? current = node;

            while (current is not null)
            {
                var scope = current.AncestorsAndSelf().FirstOrDefault(ancestor =>
                    ancestor is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax);

                if (scope is null)
                {
                    var root = node.SyntaxTree.GetRoot(cancellationToken);
                    if (seenScopes.Add(root))
                    {
                        yield return root;
                    }

                    yield break;
                }

                if (seenScopes.Add(scope))
                {
                    yield return scope;
                }

                current = scope.Parent;
            }
        }

        private static IEnumerable<InvocationExpressionSyntax> EnumerateScopeInvocations(SyntaxNode scope)
        {
            return scope
                .DescendantNodesAndSelf(child => child == scope || !IsNestedExecutableScope(child))
                .OfType<InvocationExpressionSyntax>();
        }

        private static bool IsNestedExecutableScope(SyntaxNode node)
        {
            return node is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax;
        }

        private static bool IsITestOutputHelperType(ITypeSymbol? type)
        {
            if (type is null)
            {
                return false;
            }

            var typeName = type.ToDisplayString();
            return typeName is ITEST_OUTPUT_HELPER_TYPE or ITEST_OUTPUT_HELPER_ABSTRACTIONS_TYPE;
        }

        private static bool IsLoggerRegistrationType(ITypeSymbol type)
        {
            if (type.ToDisplayString() is ILOGGER_FACTORY_TYPE or ILOGGER_TYPE)
            {
                return true;
            }

            return type is INamedTypeSymbol namedType &&
                namedType.IsGenericType &&
                namedType.Name == "ILogger" &&
                namedType.ContainingNamespace.ToDisplayString() == "Microsoft.Extensions.Logging";
        }

        private static bool IsMatchingOrImplementingType(ITypeSymbol type, string metadataName)
        {
            if (type.ToDisplayString() == metadataName)
            {
                return true;
            }

            return type.AllInterfaces.Any(interfaceType => interfaceType.ToDisplayString() == metadataName);
        }

        private static bool TryBuildITestOutputHelperLineWriter(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacement)
        {
            replacement = string.Empty;

            var candidateExpression = Unwrap(expression);
            var typeInfo = semanticModel.GetTypeInfo(candidateExpression, cancellationToken);
            var expressionType = typeInfo.ConvertedType ?? typeInfo.Type;
            if (IsITestOutputHelperType(expressionType))
            {
                replacement = $"line => ({candidateExpression.WithoutTrivia()}).WriteLine(line)";
                return true;
            }

            if (expressionType is not INamedTypeSymbol delegateType || delegateType.TypeKind != TypeKind.Delegate)
            {
                return false;
            }

            var invokeMethod = delegateType.DelegateInvokeMethod;
            if (invokeMethod is null || !IsITestOutputHelperType(invokeMethod.ReturnType))
            {
                return false;
            }

            replacement = $"line => ({candidateExpression.WithoutTrivia()})().WriteLine(line)";
            return true;
        }

        private static bool HasUsingDirective(SyntaxNode node, string namespaceName)
        {
            if (node.SyntaxTree.GetRoot() is not CompilationUnitSyntax compilationUnit)
            {
                return false;
            }

            return compilationUnit.Usings.Any(usingDirective =>
                string.Equals(usingDirective.Name?.ToString(), namespaceName, StringComparison.Ordinal));
        }

        private static bool IsProviderSelectionInvocation(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, string providerName, bool requireDefaultSelection, CancellationToken cancellationToken)
        {
            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null ||
                method.ContainingType.ToDisplayString() != MockingProviderRegistryTypeName ||
                invocationExpression.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            if (!IsMatchingProviderLiteral(invocationExpression.ArgumentList.Arguments[0].Expression, semanticModel, providerName, cancellationToken))
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name == "Push")
            {
                return !requireDefaultSelection;
            }

            if (method.Name == "SetDefault")
            {
                return true;
            }

            if (method.Name == "Register")
            {
                if (invocationExpression.ArgumentList.Arguments.Count < 3)
                {
                    return false;
                }

                return TryGetBooleanConstant(invocationExpression.ArgumentList.Arguments[2].Expression, semanticModel, cancellationToken, out var setAsDefault) && setAsDefault;
            }

            return false;
        }

        private static bool IsMatchingProviderLiteral(ExpressionSyntax expression, SemanticModel semanticModel, string providerName, CancellationToken cancellationToken)
        {
            var constantValue = semanticModel.GetConstantValue(expression, cancellationToken);
            return constantValue.HasValue &&
                constantValue.Value is string providerLiteral &&
                string.Equals(providerLiteral, providerName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryConvertTimesArgument(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, int position, out string replacement, out bool omitArgument)
        {
            return TryConvertTimesArgument(argument, semanticModel, cancellationToken, GetTimesSpecTypeName(semanticModel, position), out replacement, out omitArgument);
        }

        private static bool TryConvertVerifyLoggerTimesArgument(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacement, out bool omitArgument)
        {
            return TryConvertTimesArgument(argument, semanticModel, cancellationToken, "TimesSpec", out replacement, out omitArgument);
        }

        private static bool TryConvertTimesArgument(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, string timesSpecTypeName, out string replacement, out bool omitArgument)
        {
            replacement = string.Empty;
            omitArgument = false;

            var expression = Unwrap(argument.Expression);
            if (expression is ParenthesizedLambdaExpressionSyntax { Body: ExpressionSyntax parenthesizedLambdaExpression })
            {
                expression = Unwrap(parenthesizedLambdaExpression);
            }
            else if (expression is SimpleLambdaExpressionSyntax { Body: ExpressionSyntax simpleLambdaExpression })
            {
                expression = Unwrap(simpleLambdaExpression);
            }

            return TryConvertTimesExpression(expression, semanticModel, cancellationToken, timesSpecTypeName, out replacement, out omitArgument);
        }

        private static string GetTimesSpecTypeName(SemanticModel semanticModel, int position)
        {
            var timesSpecType = semanticModel.Compilation.GetTypeByMetadataName("FastMoq.Providers.TimesSpec");
            return timesSpecType?.ToMinimalDisplayString(semanticModel, position, SymbolDisplayFormat.MinimallyQualifiedFormat)
                ?? "FastMoq.Providers.TimesSpec";
        }

        private static bool TryConvertTimesExpression(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, string timesSpecTypeName, out string replacement, out bool omitArgument)
        {
            replacement = string.Empty;
            omitArgument = false;

            if (expression is InvocationExpressionSyntax invocationExpression)
            {
                if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                    method is null ||
                    method.ContainingType.ToDisplayString() != "Moq.Times")
                {
                    return false;
                }

                switch (method.Name)
                {
                    case "AtLeastOnce":
                        omitArgument = true;
                        return true;
                    case "Once":
                        replacement = $"{timesSpecTypeName}.Once";
                        return true;
                    case "Never":
                        replacement = $"{timesSpecTypeName}.NeverCalled";
                        return true;
                    case "Exactly":
                    case "AtLeast":
                    case "AtMost":
                        if (invocationExpression.ArgumentList.Arguments.Count != 1)
                        {
                            return false;
                        }

                        replacement = $"{timesSpecTypeName}.{method.Name}({invocationExpression.ArgumentList.Arguments[0].WithoutTrivia()})";
                        return true;
                    default:
                        return false;
                }
            }

            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
            var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
            if (symbol?.ContainingType?.ToDisplayString() != "Moq.Times")
            {
                return false;
            }

            switch (symbol.Name)
            {
                case "AtLeastOnce":
                    omitArgument = true;
                    return true;
                case "Once":
                    replacement = $"{timesSpecTypeName}.Once";
                    return true;
                case "Never":
                    replacement = $"{timesSpecTypeName}.NeverCalled";
                    return true;
                default:
                    return false;
            }
        }
    }
}
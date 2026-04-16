using Microsoft.CodeAnalysis;
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
        private const string FASTMOQ_DEFAULT_PROVIDER_ATTRIBUTE = "FastMoq.Providers.FastMoqDefaultProviderAttribute";
        private const string FASTMOQ_REGISTER_PROVIDER_ATTRIBUTE = "FastMoq.Providers.FastMoqRegisterProviderAttribute";
        private const string FASTMOQ_MOCKER_TEST_BASE_METADATA_NAME = "MockerTestBase`1";
        private const string CONTROLLER_CONTEXT_TYPE = "Microsoft.AspNetCore.Mvc.ControllerContext";
        private const string DEFAULT_HTTP_CONTEXT_TYPE = "Microsoft.AspNetCore.Http.DefaultHttpContext";
        private const string FROM_KEYED_SERVICES_ATTRIBUTE = "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute";
        private const string FUNCTION_CONTEXT_TYPE = "Microsoft.Azure.Functions.Worker.FunctionContext";
        private const string FUNCTION_CONTEXT_INSTANCE_SERVICES_PROPERTY = "InstanceServices";
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

        public static bool IsFastMoqMockerMethod(IMethodSymbol method, string methodName)
        {
            method = method.ReducedFrom ?? method;
            return method.Name == methodName && method.ContainingType.ToDisplayString() == "FastMoq.Mocker";
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
            return method.Name == "Initialize" && method.ContainingType.ToDisplayString() == "FastMoq.Mocker";
        }

        public static bool IsFastMoqMockerAddTypeMethod(IMethodSymbol method)
        {
            method = method.ReducedFrom ?? method;
            return method.Name == "AddType" && method.ContainingType.ToDisplayString() == "FastMoq.Mocker";
        }

        public static bool TryGetRequiredProvider(IMethodSymbol method, out string providerName, out string apiName)
        {
            method = method.ReducedFrom ?? method;
            providerName = string.Empty;
            apiName = method.Name;

            if (method.ContainingType.ToDisplayString() == "FastMoq.Mocker" &&
                method.Name is "GetMock" or "GetRequiredMock" or "CreateMockInstance" or "CreateDetachedMock")
            {
                providerName = "moq";
                return true;
            }

            if (method.ContainingAssembly.Name == "FastMoq.Provider.Moq" &&
                (method.ContainingType.Name == "IFastMockMoqExtensions" || method.ContainingType.Name == "MockerHttpMoqExtensions"))
            {
                providerName = "moq";
                return true;
            }

            if (method.ContainingAssembly.Name == "FastMoq.Provider.NSubstitute" &&
                method.ContainingType.Name == "IFastMockNSubstituteExtensions")
            {
                providerName = "nsubstitute";
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
                    if (containingType.ToDisplayString() == "FastMoq.Models.MockModel")
                    {
                        providerName = "moq";
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
            return property.Name == propertyName && property.ContainingType.ToDisplayString() == "FastMoq.Mocker";
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
                if (containingType.ToDisplayString() == "FastMoq.Models.MockModel")
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
            expression = Unwrap(expression);

            if (expression is InvocationExpressionSyntax invocationExpression && TryResolveTrackedMockOrigin(invocationExpression, semanticModel, cancellationToken, out origin))
            {
                return true;
            }

            if (expression is IdentifierNameSyntax identifierName && semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol is ILocalSymbol localSymbol)
            {
                var declaration = localSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) as VariableDeclaratorSyntax;
                if (declaration?.Initializer?.Value is ExpressionSyntax initializer &&
                    TryResolveTrackedMockOrigin(initializer, semanticModel, cancellationToken, out origin))
                {
                    origin = origin.WithTrackedMockExpression(identifierName);
                    return true;
                }
            }

            origin = default;
            return false;
        }

        public static bool TryResolveTrackedMockOrigin(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out TrackedMockOrigin origin)
        {
            if (!TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) || method is null)
            {
                origin = default;
                return false;
            }

            if (IsFastMoqMockerMethod(method, "GetMock") || IsFastMoqMockerMethod(method, "GetOrCreateMock"))
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
                    IsFastMoqMockerMethod(method, "GetMock") ? TrackedMockOriginKind.GetMock : TrackedMockOriginKind.GetOrCreateMock);
                return true;
            }

            if ((method.Name == "AsMoq" || method.Name == "AsNSubstitute") && invocationExpression.Expression is MemberAccessExpressionSyntax adapterAccess)
            {
                if (TryResolveTrackedMockOrigin(adapterAccess.Expression, semanticModel, cancellationToken, out origin))
                {
                    origin = origin.WithTrackedMockExpression(adapterAccess.Expression);
                    return true;
                }
            }

            origin = default;
            return false;
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
            return origin.Kind == TrackedMockOriginKind.GetOrCreateMock
                ? $"{mockerExpression}.GetOrCreateMock<{serviceType}>().Instance"
                : $"{mockerExpression}.GetObject<{serviceType}>()";
        }

        public static string BuildResetReplacement(TrackedMockOrigin origin, SemanticModel semanticModel, int position)
        {
            var mockerExpression = origin.MockerExpression.WithoutTrivia().ToString();
            var serviceType = GetMinimalTypeName(origin.ServiceType, semanticModel, position);
            return $"{mockerExpression}.GetOrCreateMock<{serviceType}>().Reset()";
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

            var usesMoqProvider = HasUsingDirective(node, "FastMoq.Providers.MoqProvider");
            var usesNSubstituteProvider = HasUsingDirective(node, "FastMoq.Providers.NSubstituteProvider");

            if (usesMoqProvider == usesNSubstituteProvider)
            {
                return false;
            }

            providerName = usesMoqProvider ? "moq" : "nsubstitute";
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

        public static bool TryGetProviderNeutralHttpHelperSuggestion(IMethodSymbol method, out string apiName)
        {
            apiName = string.Empty;

            method = method.ReducedFrom ?? method;
            if (method.ContainingAssembly.Name != "FastMoq.Provider.Moq" || method.ContainingType.Name != "MockerHttpMoqExtensions")
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
                   invokeMethod.Parameters[0].Type.ToDisplayString() == "FastMoq.Mocker";
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
                    argument.Key == "SetAsDefault" &&
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
            var scope = node.AncestorsAndSelf().FirstOrDefault(ancestor =>
                ancestor is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)
                ?? node.SyntaxTree.GetRoot(cancellationToken);

            foreach (var invocationExpression in scope.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (IsProviderSelectionInvocation(invocationExpression, semanticModel, providerName, requireDefaultSelection: false, cancellationToken))
                {
                    return true;
                }
            }

            return false;
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
                method.ContainingType.ToDisplayString() != "FastMoq.Providers.MockingProviderRegistry" ||
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

        private static bool TryConvertVerifyLoggerTimesArgument(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacement, out bool omitArgument)
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

            return TryConvertTimesExpression(expression, semanticModel, cancellationToken, out replacement, out omitArgument);
        }

        private static bool TryConvertTimesExpression(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacement, out bool omitArgument)
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
                        replacement = "TimesSpec.Once";
                        return true;
                    case "Never":
                        replacement = "TimesSpec.NeverCalled";
                        return true;
                    case "Exactly":
                    case "AtLeast":
                    case "AtMost":
                        if (invocationExpression.ArgumentList.Arguments.Count != 1)
                        {
                            return false;
                        }

                        replacement = $"TimesSpec.{method.Name}({invocationExpression.ArgumentList.Arguments[0].WithoutTrivia()})";
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
                    replacement = "TimesSpec.Once";
                    return true;
                case "Never":
                    replacement = "TimesSpec.NeverCalled";
                    return true;
                default:
                    return false;
            }
        }
    }
}
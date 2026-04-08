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
        public TrackedMockOrigin(ExpressionSyntax mockerExpression, ITypeSymbol serviceType, TrackedMockOriginKind kind)
        {
            MockerExpression = mockerExpression;
            ServiceType = serviceType;
            Kind = kind;
        }

        public ExpressionSyntax MockerExpression { get; }

        public ITypeSymbol ServiceType { get; }

        public TrackedMockOriginKind Kind { get; }
    }

    internal static class FastMoqAnalysisHelpers
    {
        private const string FASTMOQ_DEFAULT_PROVIDER_ATTRIBUTE = "FastMoq.Providers.FastMoqDefaultProviderAttribute";

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
                if (declaration?.Initializer?.Value is ExpressionSyntax initializer)
                {
                    return TryResolveTrackedMockOrigin(initializer, semanticModel, cancellationToken, out origin);
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
                    method.TypeArguments[0],
                    IsFastMoqMockerMethod(method, "GetMock") ? TrackedMockOriginKind.GetMock : TrackedMockOriginKind.GetOrCreateMock);
                return true;
            }

            if ((method.Name == "AsMoq" || method.Name == "AsNSubstitute") && invocationExpression.Expression is MemberAccessExpressionSyntax adapterAccess)
            {
                return TryResolveTrackedMockOrigin(adapterAccess.Expression, semanticModel, cancellationToken, out origin);
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

        public static bool IsSafeMixedRetrievalCandidate(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
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

        public static bool IsProviderSelectedByDefault(Compilation compilation, string providerName, CancellationToken cancellationToken)
        {
            if (TryGetAssemblyDefaultProviderName(compilation.Assembly, out var assemblyDefaultProvider) &&
                string.Equals(assemblyDefaultProvider, providerName, StringComparison.OrdinalIgnoreCase))
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
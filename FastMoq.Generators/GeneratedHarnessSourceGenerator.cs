using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;

namespace FastMoq.Generators
{
    [Generator]
    public sealed class GeneratedHarnessSourceGenerator : IIncrementalGenerator
    {
        internal const string GeneratedTestTargetAttributeMetadataName = "FastMoq.Generators.FastMoqGeneratedTestTargetAttribute";
        private const string XUnitFactAttributeMetadataName = "Xunit.FactAttribute";
        private const string FastMoqGeneratedTestFrameworkPropertyName = "build_property.FastMoqGeneratedTestFramework";
        private const string FrameworkSettingNone = "none";
        private const string ComponentConstructorParameterTypesPropertyName = "ComponentConstructorParameterTypes";
        private const string SetupMocksActionPropertyName = "SetupMocksAction";
        private const string CreatedComponentActionPropertyName = "CreatedComponentAction";
        private const string ConfigureMockerPolicyPropertyName = "ConfigureMockerPolicy";
        private const string MockerTestBaseTypeName = "MockerTestBase`1";
        private const string MockerTestBaseNamespace = "FastMoq";
        private const string ThreadingTasksNamespace = "System.Threading.Tasks";
        private const string TaskMetadataName = "Task";
        private const string GenericTaskMetadataName = "Task`1";
        private const string ValueTaskMetadataName = "ValueTask";
        private const string GenericValueTaskMetadataName = "ValueTask`1";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var targets = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    GeneratedTestTargetAttributeMetadataName,
                    static (node, _) => node is ClassDeclarationSyntax,
                    static (generatorContext, _) => TryCreateTargetModel(generatorContext))
                .Where(static model => model is not null);

            var frameworkSetting = context.AnalyzerConfigOptionsProvider
                .Select(static (options, _) =>
                {
                    options.GlobalOptions.TryGetValue(FastMoqGeneratedTestFrameworkPropertyName, out var value);
                    return value?.Trim() ?? string.Empty;
                });

            context.RegisterSourceOutput(
                targets.Combine(frameworkSetting),
                static (productionContext, pair) => EmitSource(productionContext, pair.Left!, pair.Right));
        }

        private static GeneratedHarnessTargetModel? TryCreateTargetModel(GeneratorAttributeSyntaxContext context)
        {
            if (context.TargetNode is not ClassDeclarationSyntax classDeclaration ||
                !classDeclaration.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
            {
                return null;
            }

            var targetType = (INamedTypeSymbol)context.TargetSymbol;
            var propertyNames = new global::System.Collections.Generic.HashSet<string>(
                targetType.GetMembers().OfType<IPropertySymbol>().Select(static property => property.Name));
            if (targetType.Arity != 0 ||
                targetType.ContainingType is not null ||
                propertyNames.Contains(ComponentConstructorParameterTypesPropertyName))
            {
                return null;
            }

            var componentType = TryGetMockerTestBaseComponentType(targetType);
            if (componentType is null)
            {
                return null;
            }

            var attribute = context.Attributes[0];
            if (attribute.ConstructorArguments.Length == 0 ||
                attribute.ConstructorArguments[0].Value is not ITypeSymbol attributeComponentType ||
                !SymbolEqualityComparer.Default.Equals(attributeComponentType, componentType))
            {
                return null;
            }

            var explicitConstructorParameterTypes = GetExplicitConstructorParameterTypes(attribute);
            if (!TryResolveConstructor(componentType, explicitConstructorParameterTypes, out var selectedConstructor))
            {
                return null;
            }

            return new GeneratedHarnessTargetModel(
                targetType.ContainingNamespace.IsGlobalNamespace
                    ? null
                    : targetType.ContainingNamespace.ToDisplayString(),
                targetType.Name,
                componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                context.SemanticModel.Compilation.GetTypeByMetadataName(XUnitFactAttributeMetadataName) is not null,
                GetGeneratedTestMethods(componentType),
                !propertyNames.Contains(SetupMocksActionPropertyName),
                !propertyNames.Contains(CreatedComponentActionPropertyName),
                !propertyNames.Contains(ConfigureMockerPolicyPropertyName),
                selectedConstructor!.Parameters
                    .Select(parameter => parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .ToImmutableArray(),
                selectedConstructor.Parameters
                    .Select(parameter => parameter.Name)
                    .ToImmutableArray());
        }

        private static INamedTypeSymbol? TryGetMockerTestBaseComponentType(INamedTypeSymbol targetType)
        {
            for (var current = targetType; current != null; current = current.BaseType)
            {
                if (current.IsGenericType &&
                    current.MetadataName == MockerTestBaseTypeName &&
                    string.Equals(current.ContainingNamespace.ToDisplayString(), MockerTestBaseNamespace, StringComparison.Ordinal))
                {
                    return current.TypeArguments[0] as INamedTypeSymbol;
                }
            }

            return null;
        }

        private static ImmutableArray<ITypeSymbol> GetExplicitConstructorParameterTypes(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length < 2)
            {
                return default;
            }

            if (attribute.ConstructorArguments[1].Kind != TypedConstantKind.Array)
            {
                return default;
            }

            var builder = ImmutableArray.CreateBuilder<ITypeSymbol>();
            foreach (var value in attribute.ConstructorArguments[1].Values)
            {
                if (value.Value is ITypeSymbol typeSymbol)
                {
                    builder.Add(typeSymbol);
                }
            }

            return builder.ToImmutable();
        }

        private static ImmutableArray<GeneratedComponentTestMethodModel> GetGeneratedTestMethods(INamedTypeSymbol componentType)
        {
            var candidateMethods = componentType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(static method =>
                    method.MethodKind == MethodKind.Ordinary &&
                    method.DeclaredAccessibility == Accessibility.Public &&
                    !method.IsStatic &&
                    !method.IsImplicitlyDeclared &&
                    !IsObjectOverride(method))
                .OrderBy(static method => method.Name, StringComparer.Ordinal)
                .ThenBy(static method => method.Parameters.Length)
                .ThenBy(static method => method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), StringComparer.Ordinal);

            var builder = ImmutableArray.CreateBuilder<GeneratedComponentTestMethodModel>();
            var ordinal = 1;
            foreach (var method in candidateMethods)
            {
                builder.Add(CreateGeneratedTestMethodModel(method, ordinal));
                ordinal++;
            }

            return builder.ToImmutable();
        }

        private static bool IsObjectOverride(IMethodSymbol method)
        {
            return method.IsOverride &&
                method.OverriddenMethod?.ContainingType.SpecialType == SpecialType.System_Object;
        }
        private static string EscapeIdentifierIfKeyword(string identifier)
        {
            var keywordKind = SyntaxFacts.GetKeywordKind(identifier);
            return keywordKind != SyntaxKind.None ? "@" + identifier : identifier;
        }
        private static GeneratedComponentTestMethodModel CreateGeneratedTestMethodModel(IMethodSymbol method, int ordinal)
        {
            var methodDisplayName = method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var escapedMethodName = EscapeIdentifierIfKeyword(method.Name);
            var methodIdentifier = CreateGeneratedMethodIdentifier(method.Name);

            if (method.IsGenericMethod)
            {
                return GeneratedComponentTestMethodModel.CreateDeferred(escapedMethodName, ordinal, methodIdentifier, methodDisplayName, "is generic.");
            }

            if (!TryCreateInvocationArguments(method, out var invocationArguments, out var deferredReasonSuffix))
            {
                return GeneratedComponentTestMethodModel.CreateDeferred(escapedMethodName, ordinal, methodIdentifier, methodDisplayName, deferredReasonSuffix);
            }

            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            {
                return GeneratedComponentTestMethodModel.CreateDeferred(escapedMethodName, ordinal, methodIdentifier, methodDisplayName, "returns by reference.");
            }

            if (TryGetUnsupportedReturnTypeReason(method.ReturnType, out var unsupportedReason))
            {
                return GeneratedComponentTestMethodModel.CreateDeferred(escapedMethodName, ordinal, methodIdentifier, methodDisplayName, unsupportedReason);
            }

            return GeneratedComponentTestMethodModel.CreateSupported(
                escapedMethodName,
                ordinal,
                methodIdentifier,
                invocationArguments,
                IsAsyncReturnType(method.ReturnType),
                ReturnsValue(method.ReturnType));
        }

        private static bool TryCreateInvocationArguments(IMethodSymbol method, out string invocationArguments, out string deferredReasonSuffix)
        {
            if (method.Parameters.Length == 0)
            {
                invocationArguments = string.Empty;
                deferredReasonSuffix = string.Empty;
                return true;
            }

            var argumentBuilder = ImmutableArray.CreateBuilder<string>(method.Parameters.Length);
            foreach (var parameter in method.Parameters)
            {
                if (parameter.RefKind != RefKind.None)
                {
                    invocationArguments = string.Empty;
                    deferredReasonSuffix = "uses ref, in, or out parameters.";
                    return false;
                }

                if (!parameter.IsOptional)
                {
                    invocationArguments = string.Empty;
                    deferredReasonSuffix = "requires non-optional parameters.";
                    return false;
                }

                if (!TryCreateDefaultArgumentExpression(parameter, out var argumentExpression))
                {
                    invocationArguments = string.Empty;
                    deferredReasonSuffix = "has an unsupported optional-parameter default for '" + parameter.Name + "'.";
                    return false;
                }

                argumentBuilder.Add(argumentExpression);
            }

            invocationArguments = string.Join(", ", argumentBuilder);
            deferredReasonSuffix = string.Empty;
            return true;
        }

        private static bool TryCreateDefaultArgumentExpression(IParameterSymbol parameter, out string argumentExpression)
        {
            if (!parameter.HasExplicitDefaultValue)
            {
                argumentExpression = string.Empty;
                return false;
            }

            if (TryFormatDefaultValue(parameter.Type, parameter.ExplicitDefaultValue, out var rawArgumentExpression))
            {
                argumentExpression = CreateTypedArgumentExpression(parameter.Type, rawArgumentExpression);
                return true;
            }

            argumentExpression = string.Empty;
            return false;
        }

        private static string CreateTypedArgumentExpression(ITypeSymbol parameterType, string rawArgumentExpression)
        {
            return "(" + parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")" + rawArgumentExpression;
        }

        private static bool TryFormatDefaultValue(ITypeSymbol parameterType, object? explicitDefaultValue, out string expression)
        {
            if (explicitDefaultValue is null)
            {
                if (parameterType.IsReferenceType || IsNullableValueType(parameterType))
                {
                    expression = "null";
                    return true;
                }

                expression = string.Empty;
                return false;
            }

            if (parameterType.TypeKind == TypeKind.Enum &&
                TryFormatEnumDefaultValue(parameterType, explicitDefaultValue, out expression))
            {
                return true;
            }

            switch (explicitDefaultValue)
            {
                case bool booleanValue:
                    expression = booleanValue ? "true" : "false";
                    return true;
                case char charValue:
                    expression = SymbolDisplay.FormatLiteral(charValue, quote: true);
                    return true;
                case string stringValue:
                    expression = SymbolDisplay.FormatLiteral(stringValue, quote: true);
                    return true;
                case sbyte sbyteValue:
                    expression = sbyteValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                case byte byteValue:
                    expression = byteValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                case short shortValue:
                    expression = shortValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                case ushort ushortValue:
                    expression = ushortValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                case int intValue:
                    expression = intValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                case uint uintValue:
                    expression = uintValue.ToString(CultureInfo.InvariantCulture) + "U";
                    return true;
                case long longValue:
                    expression = longValue.ToString(CultureInfo.InvariantCulture) + "L";
                    return true;
                case ulong ulongValue:
                    expression = ulongValue.ToString(CultureInfo.InvariantCulture) + "UL";
                    return true;
                case float floatValue:
                    expression = floatValue.ToString("R", CultureInfo.InvariantCulture) + "F";
                    return true;
                case double doubleValue:
                    expression = doubleValue.ToString("R", CultureInfo.InvariantCulture);
                    return true;
                case decimal decimalValue:
                    expression = decimalValue.ToString(CultureInfo.InvariantCulture) + "M";
                    return true;
                default:
                    expression = string.Empty;
                    return false;
            }
        }

        private static bool TryFormatEnumDefaultValue(ITypeSymbol parameterType, object explicitDefaultValue, out string expression)
        {
            if (parameterType is not INamedTypeSymbol namedEnumType)
            {
                expression = string.Empty;
                return false;
            }

            foreach (var field in namedEnumType.GetMembers().OfType<IFieldSymbol>())
            {
                if (!field.HasConstantValue)
                {
                    continue;
                }

                if (Equals(field.ConstantValue, explicitDefaultValue))
                {
                    expression = namedEnumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + field.Name;
                    return true;
                }
            }

            expression = string.Empty;
            return false;
        }

        private static bool IsNullableValueType(ITypeSymbol typeSymbol)
        {
            return typeSymbol is INamedTypeSymbol namedType &&
                namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }

        private static string CreateGeneratedMethodIdentifier(string methodName)
        {
            return new string(methodName.Select(static character =>
                char.IsLetterOrDigit(character) || character == '_'
                    ? character
                    : '_').ToArray());
        }

        private static bool TryGetUnsupportedReturnTypeReason(ITypeSymbol returnType, out string reason)
        {
            if (returnType.TypeKind == TypeKind.Pointer || returnType.TypeKind == TypeKind.FunctionPointer)
            {
                reason = "has unsupported return type '" + returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "'.";
                return true;
            }

            reason = string.Empty;
            return false;
        }

        private static bool IsAsyncReturnType(ITypeSymbol returnType)
        {
            return IsNonGenericTaskReturnType(returnType) ||
                IsGenericTaskReturnType(returnType) ||
                IsNonGenericValueTaskReturnType(returnType) ||
                IsGenericValueTaskReturnType(returnType);
        }

        private static bool ReturnsValue(ITypeSymbol returnType)
        {
            return returnType.SpecialType != SpecialType.System_Void &&
                !IsNonGenericTaskReturnType(returnType) &&
                !IsNonGenericValueTaskReturnType(returnType);
        }

        private static bool IsNonGenericTaskReturnType(ITypeSymbol returnType)
        {
            return returnType is INamedTypeSymbol namedType &&
                string.Equals(namedType.ContainingNamespace.ToDisplayString(), ThreadingTasksNamespace, StringComparison.Ordinal) &&
                string.Equals(namedType.MetadataName, TaskMetadataName, StringComparison.Ordinal);
        }

        private static bool IsGenericTaskReturnType(ITypeSymbol returnType)
        {
            return returnType is INamedTypeSymbol namedType &&
                string.Equals(namedType.ContainingNamespace.ToDisplayString(), ThreadingTasksNamespace, StringComparison.Ordinal) &&
                string.Equals(namedType.MetadataName, GenericTaskMetadataName, StringComparison.Ordinal);
        }

        private static bool IsNonGenericValueTaskReturnType(ITypeSymbol returnType)
        {
            return returnType is INamedTypeSymbol namedType &&
                string.Equals(namedType.ContainingNamespace.ToDisplayString(), ThreadingTasksNamespace, StringComparison.Ordinal) &&
                string.Equals(namedType.MetadataName, ValueTaskMetadataName, StringComparison.Ordinal);
        }

        private static bool IsGenericValueTaskReturnType(ITypeSymbol returnType)
        {
            return returnType is INamedTypeSymbol namedType &&
                string.Equals(namedType.ContainingNamespace.ToDisplayString(), ThreadingTasksNamespace, StringComparison.Ordinal) &&
                string.Equals(namedType.MetadataName, GenericValueTaskMetadataName, StringComparison.Ordinal);
        }

        private static bool TryResolveConstructor(
            INamedTypeSymbol componentType,
            ImmutableArray<ITypeSymbol> explicitConstructorParameterTypes,
            out IMethodSymbol? selectedConstructor)
        {
            var instanceConstructors = componentType.InstanceConstructors
                .Where(static constructor => !constructor.IsStatic)
                .ToImmutableArray();

            if (!explicitConstructorParameterTypes.IsDefault)
            {
                selectedConstructor = instanceConstructors.FirstOrDefault(constructor =>
                    ParametersMatch(constructor, explicitConstructorParameterTypes));
                return selectedConstructor is not null;
            }

            var publicConstructors = instanceConstructors
                .Where(static constructor => constructor.DeclaredAccessibility == Accessibility.Public)
                .ToImmutableArray();
            if (publicConstructors.Length != 1)
            {
                selectedConstructor = null;
                return false;
            }

            selectedConstructor = publicConstructors[0];
            return true;
        }

        private static bool ParametersMatch(IMethodSymbol constructor, ImmutableArray<ITypeSymbol> explicitConstructorParameterTypes)
        {
            if (constructor.Parameters.Length != explicitConstructorParameterTypes.Length)
            {
                return false;
            }

            for (var index = 0; index < constructor.Parameters.Length; index++)
            {
                if (!SymbolEqualityComparer.Default.Equals(constructor.Parameters[index].Type, explicitConstructorParameterTypes[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static void EmitSource(SourceProductionContext context, GeneratedHarnessTargetModel target, string frameworkSetting)
        {
            var emitXUnitSmokeTests = target.EmitXUnitSmokeTests &&
                !string.Equals(frameworkSetting, FrameworkSettingNone, StringComparison.OrdinalIgnoreCase);

            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("// <auto-generated/>");
            sourceBuilder.AppendLine("#nullable enable");

            if (!string.IsNullOrWhiteSpace(target.NamespaceName))
            {
                sourceBuilder.Append("namespace ")
                    .Append(target.NamespaceName)
                    .AppendLine();
                sourceBuilder.AppendLine("{");
            }

            AppendIndentedLine(sourceBuilder, 1, $"partial class {target.TargetTypeName}");
            AppendIndentedLine(sourceBuilder, 1, "{");
            AppendIndentedLine(sourceBuilder, 2, "protected override global::System.Type?[]? ComponentConstructorParameterTypes =>");
            AppendIndentedLine(sourceBuilder, 3, "FastMoqGeneratedHarnessMetadata.ConstructorParameterTypes;");
            sourceBuilder.AppendLine();
            if (target.EmitConfigureMockerPolicyOverride)
            {
                AppendIndentedLine(sourceBuilder, 2, "protected override global::System.Action<global::FastMoq.MockerPolicyOptions>? ConfigureMockerPolicy =>");
                AppendIndentedLine(sourceBuilder, 3, "options =>");
                AppendIndentedLine(sourceBuilder, 3, "{");
                AppendIndentedLine(sourceBuilder, 4, "base.ConfigureMockerPolicy?.Invoke(options);");
                AppendIndentedLine(sourceBuilder, 4, "ConfigureGeneratedMockerPolicy(options);");
                AppendIndentedLine(sourceBuilder, 3, "};");
                sourceBuilder.AppendLine();
                AppendIndentedLine(sourceBuilder, 2, "partial void ConfigureGeneratedMockerPolicy(global::FastMoq.MockerPolicyOptions options);");
                sourceBuilder.AppendLine();
            }

            if (target.EmitSetupMocksActionOverride)
            {
                AppendIndentedLine(sourceBuilder, 2, "protected override global::System.Action<global::FastMoq.Mocker>? SetupMocksAction =>");
                AppendIndentedLine(sourceBuilder, 3, "mocker =>");
                AppendIndentedLine(sourceBuilder, 3, "{");
                AppendIndentedLine(sourceBuilder, 4, "base.SetupMocksAction?.Invoke(mocker);");
                AppendIndentedLine(sourceBuilder, 4, "ConfigureGeneratedMocks(mocker);");
                AppendIndentedLine(sourceBuilder, 3, "};");
                sourceBuilder.AppendLine();
                AppendIndentedLine(sourceBuilder, 2, "partial void ConfigureGeneratedMocks(global::FastMoq.Mocker mocker);");
                sourceBuilder.AppendLine();
            }

            if (target.EmitCreatedComponentActionOverride)
            {
                AppendIndentedLine(sourceBuilder, 2, $"protected override global::System.Action<{target.ComponentTypeName}>? CreatedComponentAction =>");
                AppendIndentedLine(sourceBuilder, 3, "component =>");
                AppendIndentedLine(sourceBuilder, 3, "{");
                AppendIndentedLine(sourceBuilder, 4, "base.CreatedComponentAction?.Invoke(component);");
                AppendIndentedLine(sourceBuilder, 4, "AfterGeneratedComponentCreated(component);");
                AppendIndentedLine(sourceBuilder, 3, "};");
                sourceBuilder.AppendLine();
                AppendIndentedLine(sourceBuilder, 2, $"partial void AfterGeneratedComponentCreated({target.ComponentTypeName} component);");
                sourceBuilder.AppendLine();
            }

            AppendIndentedLine(sourceBuilder, 2, "/// <summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// Executes the generated arrange, act, assert, and verify scaffold synchronously.");
            AppendIndentedLine(sourceBuilder, 2, "/// </summary>");
            AppendIndentedLine(sourceBuilder, 2, "public void ExecuteGeneratedScenarioScaffold() => CreateGeneratedScenarioScaffold().Execute();");
            AppendIndentedLine(sourceBuilder, 2, "/// <summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// Executes the generated arrange, act, assert, and verify scaffold asynchronously.");
            AppendIndentedLine(sourceBuilder, 2, "/// </summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// <returns>A task that completes when the generated scaffold finishes running.</returns>");
            AppendIndentedLine(sourceBuilder, 2, "public global::System.Threading.Tasks.Task ExecuteGeneratedScenarioScaffoldAsync() => CreateGeneratedScenarioScaffold().ExecuteAsync();");
            AppendIndentedLine(sourceBuilder, 2, "/// <summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// Executes the generated scaffold with an act phase that expects the specified exception type.");
            AppendIndentedLine(sourceBuilder, 2, "/// </summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// <typeparam name=\"TException\">The exception type expected from the generated act phase.</typeparam>");
            AppendIndentedLine(sourceBuilder, 2, "public void ExecuteGeneratedExpectedExceptionScenarioScaffold<TException>() where TException : global::System.Exception => CreateGeneratedExpectedExceptionScenarioScaffold<TException>().Execute();");
            AppendIndentedLine(sourceBuilder, 2, "/// <summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// Executes the generated scaffold asynchronously with an act phase that expects the specified exception type.");
            AppendIndentedLine(sourceBuilder, 2, "/// </summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// <typeparam name=\"TException\">The exception type expected from the generated act phase.</typeparam>");
            AppendIndentedLine(sourceBuilder, 2, "/// <returns>A task that completes when the generated scaffold finishes running.</returns>");
            AppendIndentedLine(sourceBuilder, 2, "public global::System.Threading.Tasks.Task ExecuteGeneratedExpectedExceptionScenarioScaffoldAsync<TException>() where TException : global::System.Exception => CreateGeneratedExpectedExceptionScenarioScaffold<TException>().ExecuteAsync();");
            sourceBuilder.AppendLine();
            AppendIndentedLine(sourceBuilder, 2, $"private global::FastMoq.ScenarioBuilder<{target.ComponentTypeName}> CreateGeneratedScenarioScaffold()");
            AppendIndentedLine(sourceBuilder, 2, "{");
            AppendIndentedLine(sourceBuilder, 3, "var scenario = Scenario;");
            AppendIndentedLine(sourceBuilder, 3, "ArrangeGeneratedScenario(scenario);");
            AppendIndentedLine(sourceBuilder, 3, "ActGeneratedScenario(scenario);");
            AppendIndentedLine(sourceBuilder, 3, "AssertGeneratedScenario(scenario);");
            AppendIndentedLine(sourceBuilder, 3, "VerifyGeneratedScenario(scenario);");
            AppendIndentedLine(sourceBuilder, 3, "return scenario;");
            AppendIndentedLine(sourceBuilder, 2, "}");
            sourceBuilder.AppendLine();
            AppendIndentedLine(sourceBuilder, 2, $"private global::FastMoq.ScenarioBuilder<{target.ComponentTypeName}> CreateGeneratedExpectedExceptionScenarioScaffold<TException>() where TException : global::System.Exception");
            AppendIndentedLine(sourceBuilder, 2, "{");
            AppendIndentedLine(sourceBuilder, 3, "var scenario = Scenario;");
            AppendIndentedLine(sourceBuilder, 3, "ArrangeGeneratedScenario(scenario);");
            AppendIndentedLine(sourceBuilder, 3, "ExpectedExceptionGeneratedScenario<TException>(scenario);");
            AppendIndentedLine(sourceBuilder, 3, "AssertGeneratedScenario(scenario);");
            AppendIndentedLine(sourceBuilder, 3, "VerifyGeneratedScenario(scenario);");
            AppendIndentedLine(sourceBuilder, 3, "return scenario;");
            AppendIndentedLine(sourceBuilder, 2, "}");
            sourceBuilder.AppendLine();
            AppendIndentedLine(sourceBuilder, 2, "/// <summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// Adds arrange steps to the generated scenario scaffold.");
            AppendIndentedLine(sourceBuilder, 2, "/// </summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// <param name=\"scenario\">The scenario builder to configure.</param>");
            AppendIndentedLine(sourceBuilder, 2, $"partial void ArrangeGeneratedScenario(global::FastMoq.ScenarioBuilder<{target.ComponentTypeName}> scenario);");
            AppendIndentedLine(sourceBuilder, 2, "/// <summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// Adds act steps to the generated scenario scaffold.");
            AppendIndentedLine(sourceBuilder, 2, "/// </summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// <param name=\"scenario\">The scenario builder to configure.</param>");
            AppendIndentedLine(sourceBuilder, 2, $"partial void ActGeneratedScenario(global::FastMoq.ScenarioBuilder<{target.ComponentTypeName}> scenario);");
            AppendIndentedLine(sourceBuilder, 2, "/// <summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// Adds an act step to the generated scaffold that expects the specified exception type.");
            AppendIndentedLine(sourceBuilder, 2, "/// </summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// <typeparam name=\"TException\">The exception type expected from the generated act phase.</typeparam>");
            AppendIndentedLine(sourceBuilder, 2, "/// <param name=\"scenario\">The scenario builder to configure.</param>");
            AppendIndentedLine(sourceBuilder, 2, $"partial void ExpectedExceptionGeneratedScenario<TException>(global::FastMoq.ScenarioBuilder<{target.ComponentTypeName}> scenario) where TException : global::System.Exception;");
            AppendIndentedLine(sourceBuilder, 2, "/// <summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// Adds assertion steps to the generated scenario scaffold.");
            AppendIndentedLine(sourceBuilder, 2, "/// </summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// <param name=\"scenario\">The scenario builder to configure.</param>");
            AppendIndentedLine(sourceBuilder, 2, $"partial void AssertGeneratedScenario(global::FastMoq.ScenarioBuilder<{target.ComponentTypeName}> scenario);");
            AppendIndentedLine(sourceBuilder, 2, "/// <summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// Adds verification steps to the generated scenario scaffold.");
            AppendIndentedLine(sourceBuilder, 2, "/// </summary>");
            AppendIndentedLine(sourceBuilder, 2, "/// <param name=\"scenario\">The scenario builder to configure.</param>");
            AppendIndentedLine(sourceBuilder, 2, $"partial void VerifyGeneratedScenario(global::FastMoq.ScenarioBuilder<{target.ComponentTypeName}> scenario);");
            sourceBuilder.AppendLine();
            AppendGeneratedXUnitSmokeTests(sourceBuilder, target, emitXUnitSmokeTests);
            AppendIndentedLine(sourceBuilder, 2, "internal static class FastMoqGeneratedHarnessMetadata");
            AppendIndentedLine(sourceBuilder, 2, "{");
            AppendIndentedLine(sourceBuilder, 3, $"internal static global::System.Type ComponentType {{ get; }} = typeof({target.ComponentTypeName});");
            AppendIndentedLine(sourceBuilder, 3, "internal static global::System.Type?[] ConstructorParameterTypes { get; } = new global::System.Type?[]");
            AppendIndentedLine(sourceBuilder, 3, "{");
            foreach (var parameterTypeName in target.ConstructorParameterTypeNames)
            {
                AppendIndentedLine(sourceBuilder, 4, $"typeof({parameterTypeName}),");
            }
            AppendIndentedLine(sourceBuilder, 3, "};");
            sourceBuilder.AppendLine();
            AppendIndentedLine(sourceBuilder, 3, "internal static global::System.String[] DependencyNames { get; } = new global::System.String[]");
            AppendIndentedLine(sourceBuilder, 3, "{");
            foreach (var parameterName in target.DependencyNames)
            {
                AppendIndentedLine(sourceBuilder, 4, SymbolDisplay.FormatLiteral(parameterName, quote: true) + ",");
            }
            AppendIndentedLine(sourceBuilder, 3, "};");
            sourceBuilder.AppendLine();
            AppendIndentedLine(sourceBuilder, 3, "internal static global::System.Type?[] DependencyTypes => ConstructorParameterTypes;");
            AppendIndentedLine(sourceBuilder, 2, "}");
            AppendIndentedLine(sourceBuilder, 1, "}");

            if (!string.IsNullOrWhiteSpace(target.NamespaceName))
            {
                sourceBuilder.AppendLine("}");
            }

            context.AddSource(GetHintName(target), sourceBuilder.ToString());
        }

        private static void AppendIndentedLine(StringBuilder builder, int indentLevel, string text)
        {
            builder.Append(' ', indentLevel * 4)
                .AppendLine(text);
        }

        private static void AppendGeneratedXUnitSmokeTests(StringBuilder sourceBuilder, GeneratedHarnessTargetModel target, bool emitXUnitSmokeTests)
        {
            if (!emitXUnitSmokeTests)
            {
                return;
            }

            AppendIndentedLine(sourceBuilder, 2, "[global::Xunit.Fact]");
            AppendIndentedLine(sourceBuilder, 2, "public void FastMoqGeneratedSmokeTest_00_Component_ShouldCreateComponent()");
            AppendIndentedLine(sourceBuilder, 2, "{");
            AppendIndentedLine(sourceBuilder, 3, "_ = Component;");
            AppendIndentedLine(sourceBuilder, 2, "}");
            sourceBuilder.AppendLine();

            foreach (var generatedTestMethod in target.GeneratedTestMethods)
            {
                if (generatedTestMethod.IsDeferred)
                {
                    var skipReasonLiteral = SymbolDisplay.FormatLiteral(generatedTestMethod.DeferredReason!, quote: true);
                    AppendIndentedLine(sourceBuilder, 2, "[global::Xunit.Fact(Skip = " + skipReasonLiteral + ")]");
                    AppendIndentedLine(sourceBuilder, 2, "public void " + generatedTestMethod.GeneratedMethodName + "()");
                    AppendIndentedLine(sourceBuilder, 2, "{");
                    AppendIndentedLine(sourceBuilder, 3, "throw new global::System.NotSupportedException(" + skipReasonLiteral + ");");
                    AppendIndentedLine(sourceBuilder, 2, "}");
                    sourceBuilder.AppendLine();
                    continue;
                }

                AppendIndentedLine(sourceBuilder, 2, "[global::Xunit.Fact]");
                if (generatedTestMethod.IsAsync)
                {
                    AppendIndentedLine(sourceBuilder, 2, "public async global::System.Threading.Tasks.Task " + generatedTestMethod.GeneratedMethodName + "()");
                }
                else
                {
                    AppendIndentedLine(sourceBuilder, 2, "public void " + generatedTestMethod.GeneratedMethodName + "()");
                }

                AppendIndentedLine(sourceBuilder, 2, "{");
                AppendIndentedLine(sourceBuilder, 3, "var component = Component;");

                if (generatedTestMethod.IsAsync)
                {
                    if (generatedTestMethod.ReturnsValue)
                    {
                        AppendIndentedLine(sourceBuilder, 3, "_ = await component." + generatedTestMethod.ComponentMethodName + "(" + generatedTestMethod.InvocationArguments + ");");
                    }
                    else
                    {
                        AppendIndentedLine(sourceBuilder, 3, "await component." + generatedTestMethod.ComponentMethodName + "(" + generatedTestMethod.InvocationArguments + ");");
                    }
                }
                else if (generatedTestMethod.ReturnsValue)
                {
                    AppendIndentedLine(sourceBuilder, 3, "_ = component." + generatedTestMethod.ComponentMethodName + "(" + generatedTestMethod.InvocationArguments + ");");
                }
                else
                {
                    AppendIndentedLine(sourceBuilder, 3, "component." + generatedTestMethod.ComponentMethodName + "(" + generatedTestMethod.InvocationArguments + ");");
                }

                AppendIndentedLine(sourceBuilder, 2, "}");
                sourceBuilder.AppendLine();
            }
        }

        private static string GetHintName(GeneratedHarnessTargetModel target)
        {
            var identifier = string.IsNullOrWhiteSpace(target.NamespaceName)
                ? target.TargetTypeName
                : target.NamespaceName + "." + target.TargetTypeName;
            var sanitizedIdentifier = new string(identifier.Select(static character =>
                char.IsLetterOrDigit(character)
                    ? character
                    : '_').ToArray());
            return sanitizedIdentifier + ".FastMoq.GeneratedHarness.g.cs";
        }

        private sealed class GeneratedHarnessTargetModel
        {
            public GeneratedHarnessTargetModel(
                string? namespaceName,
                string targetTypeName,
                string componentTypeName,
                bool emitXUnitSmokeTests,
                ImmutableArray<GeneratedComponentTestMethodModel> generatedTestMethods,
                bool emitSetupMocksActionOverride,
                bool emitCreatedComponentActionOverride,
                bool emitConfigureMockerPolicyOverride,
                ImmutableArray<string> constructorParameterTypeNames,
                ImmutableArray<string> dependencyNames)
            {
                NamespaceName = namespaceName;
                TargetTypeName = targetTypeName;
                ComponentTypeName = componentTypeName;
                EmitXUnitSmokeTests = emitXUnitSmokeTests;
                GeneratedTestMethods = generatedTestMethods;
                EmitSetupMocksActionOverride = emitSetupMocksActionOverride;
                EmitCreatedComponentActionOverride = emitCreatedComponentActionOverride;
                EmitConfigureMockerPolicyOverride = emitConfigureMockerPolicyOverride;
                ConstructorParameterTypeNames = constructorParameterTypeNames;
                DependencyNames = dependencyNames;
            }

            public string? NamespaceName { get; }

            public string TargetTypeName { get; }

            public string ComponentTypeName { get; }

            public bool EmitXUnitSmokeTests { get; }

            public ImmutableArray<GeneratedComponentTestMethodModel> GeneratedTestMethods { get; }

            public bool EmitSetupMocksActionOverride { get; }

            public bool EmitCreatedComponentActionOverride { get; }

            public bool EmitConfigureMockerPolicyOverride { get; }

            public ImmutableArray<string> ConstructorParameterTypeNames { get; }

            public ImmutableArray<string> DependencyNames { get; }
        }

        private sealed class GeneratedComponentTestMethodModel
        {
            private GeneratedComponentTestMethodModel(
                string componentMethodName,
                string generatedMethodName,
                string invocationArguments,
                bool isDeferred,
                string? deferredReason,
                bool isAsync,
                bool returnsValue)
            {
                ComponentMethodName = componentMethodName;
                GeneratedMethodName = generatedMethodName;
                InvocationArguments = invocationArguments;
                IsDeferred = isDeferred;
                DeferredReason = deferredReason;
                IsAsync = isAsync;
                ReturnsValue = returnsValue;
            }

            public string ComponentMethodName { get; }

            public string GeneratedMethodName { get; }

            public string InvocationArguments { get; }

            public bool IsDeferred { get; }

            public string? DeferredReason { get; }

            public bool IsAsync { get; }

            public bool ReturnsValue { get; }

            public static GeneratedComponentTestMethodModel CreateSupported(
                string componentMethodName,
                int ordinal,
                string methodIdentifier,
                string invocationArguments,
                bool isAsync,
                bool returnsValue)
            {
                return new GeneratedComponentTestMethodModel(
                    componentMethodName,
                    "FastMoqGeneratedSmokeTest_" + ordinal.ToString("D2") + "_" + methodIdentifier + "_ShouldExecuteWithoutThrowing",
                    invocationArguments,
                    isDeferred: false,
                    deferredReason: null,
                    isAsync: isAsync,
                    returnsValue: returnsValue);
            }

            public static GeneratedComponentTestMethodModel CreateDeferred(
                string componentMethodName,
                int ordinal,
                string methodIdentifier,
                string methodDisplayName,
                string deferredReasonSuffix)
            {
                var deferredReason = "FastMoq generated smoke test deferred: method '" + methodDisplayName + "' " + deferredReasonSuffix;
                return new GeneratedComponentTestMethodModel(
                    componentMethodName,
                    "FastMoqGeneratedPlaceholder_" + ordinal.ToString("D2") + "_" + methodIdentifier + "_IsDeferred",
                    string.Empty,
                    isDeferred: true,
                    deferredReason,
                    isAsync: false,
                    returnsValue: false);
            }
        }
    }
}
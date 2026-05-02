using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace FastMoq.Generators
{
    [Generator]
    public sealed class GeneratedHarnessSourceGenerator : IIncrementalGenerator
    {
        internal const string GeneratedTestTargetAttributeMetadataName = "FastMoq.Generators.FastMoqGeneratedTestTargetAttribute";
        private const string ComponentConstructorParameterTypesPropertyName = "ComponentConstructorParameterTypes";
        private const string MockerTestBaseTypeName = "MockerTestBase`1";
        private const string MockerTestBaseNamespace = "FastMoq";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var targets = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    GeneratedTestTargetAttributeMetadataName,
                    static (node, _) => node is ClassDeclarationSyntax,
                    static (generatorContext, _) => TryCreateTargetModel(generatorContext))
                .Where(static model => model is not null);

            context.RegisterSourceOutput(targets, static (productionContext, model) =>
                EmitSource(productionContext, model!));
        }

        private static GeneratedHarnessTargetModel? TryCreateTargetModel(GeneratorAttributeSyntaxContext context)
        {
            if (context.TargetNode is not ClassDeclarationSyntax classDeclaration ||
                !classDeclaration.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
            {
                return null;
            }

            var targetType = (INamedTypeSymbol)context.TargetSymbol;
            if (targetType.Arity != 0 ||
                targetType.ContainingType is not null ||
                targetType.GetMembers().OfType<IPropertySymbol>().Any(static property => property.Name == ComponentConstructorParameterTypesPropertyName))
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
            if (attribute.ConstructorArguments.Length < 2 ||
                attribute.ConstructorArguments[1].Kind != TypedConstantKind.Array)
            {
                return ImmutableArray<ITypeSymbol>.Empty;
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

        private static bool TryResolveConstructor(
            INamedTypeSymbol componentType,
            ImmutableArray<ITypeSymbol> explicitConstructorParameterTypes,
            out IMethodSymbol? selectedConstructor)
        {
            var instanceConstructors = componentType.InstanceConstructors
                .Where(static constructor => !constructor.IsStatic)
                .ToImmutableArray();

            if (!explicitConstructorParameterTypes.IsDefaultOrEmpty)
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

        private static void EmitSource(SourceProductionContext context, GeneratedHarnessTargetModel target)
        {
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
            AppendIndentedLine(sourceBuilder, 2, "internal static class FastMoqGeneratedHarnessMetadata");
            AppendIndentedLine(sourceBuilder, 2, "{");
            AppendIndentedLine(sourceBuilder, 3, $"internal static global::System.Type ComponentType {{ get; }} = typeof({target.ComponentTypeName});");
            AppendIndentedLine(sourceBuilder, 3, "internal static global::System.Type[] ConstructorParameterTypes { get; } = new global::System.Type[]");
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
            AppendIndentedLine(sourceBuilder, 3, "internal static global::System.Type[] DependencyTypes => ConstructorParameterTypes;");
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
                ImmutableArray<string> constructorParameterTypeNames,
                ImmutableArray<string> dependencyNames)
            {
                NamespaceName = namespaceName;
                TargetTypeName = targetTypeName;
                ComponentTypeName = componentTypeName;
                ConstructorParameterTypeNames = constructorParameterTypeNames;
                DependencyNames = dependencyNames;
            }

            public string? NamespaceName { get; }

            public string TargetTypeName { get; }

            public string ComponentTypeName { get; }

            public ImmutableArray<string> ConstructorParameterTypeNames { get; }

            public ImmutableArray<string> DependencyNames { get; }
        }
    }
}
using System;
using FastMoq.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace FastMoq.Analyzers.Tests
{
    public sealed class GeneratedHarnessSourceGeneratorTests
    {
        private const string RepresentativeConsumingScenarioSource = @"
using FastMoq;
using FastMoq.Generators;
using FastMoq.Models;
using System;
using System.IO.Abstractions;

namespace Demo.Tests;

public sealed class ConstructorSelectionTarget
{
    public ConstructorSelectionTarget()
    {
        ConstructorKind = ""parameterless"";
    }

    public ConstructorSelectionTarget(IFileSystem fileSystem, string value)
    {
        ConstructorKind = ""selected"";
        ValueWasNull = value is null;
    }

    public string ConstructorKind { get; }

    public bool ValueWasNull { get; }
}

[FastMoqGeneratedTestTarget(typeof(ConstructorSelectionTarget), typeof(IFileSystem), typeof(string))]
public partial class GeneratedConstructorHarness : MockerTestBase<ConstructorSelectionTarget>
{
    public string DescribeConstructorKind() => Component.ConstructorKind;

    public bool DescribeValueWasNull() => Component.ValueWasNull;

    public Type?[]? DescribeConstructorTypes() => ComponentConstructorParameterTypes;

    public InstanceConstructionPlan DescribeComponentConstruction() => GetComponentConstructionPlan();
}

public sealed class ManualConstructorHarness : MockerTestBase<ConstructorSelectionTarget>
{
    protected override Type?[]? ComponentConstructorParameterTypes => new Type?[] { typeof(IFileSystem), typeof(string) };

    public string DescribeConstructorKind() => Component.ConstructorKind;

    public bool DescribeValueWasNull() => Component.ValueWasNull;

    public Type?[]? DescribeConstructorTypes() => ComponentConstructorParameterTypes;

    public InstanceConstructionPlan DescribeComponentConstruction() => GetComponentConstructionPlan();
}
";

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldEmitHarnessMetadata_ForSinglePublicConstructorTarget()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public interface IOrderGateway { }

public sealed class OrderSubmitter
{
    public OrderSubmitter(IOrderGateway gateway)
    {
    }
}

[FastMoqGeneratedTestTarget(typeof(OrderSubmitter))]
public partial class OrderSubmitterTests : MockerTestBase<OrderSubmitter>
{
}
";

            var result = await RunGeneratorAsync(source);

            result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();

            var generatedSource = result.GeneratedSources.Should().ContainSingle().Subject;
            generatedSource.SourceText.ToString().Should().Contain("protected override global::System.Type?[]? ComponentConstructorParameterTypes =>");
            generatedSource.SourceText.ToString().Should().Contain("typeof(global::Demo.Tests.IOrderGateway)");
            generatedSource.SourceText.ToString().Should().Contain("\"gateway\"");
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldUseExplicitConstructorSignature_ForMultiConstructorTarget()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public interface IOrderGateway { }

public sealed class OrderSubmitter
{
    public OrderSubmitter()
    {
    }

    public OrderSubmitter(IOrderGateway gateway)
    {
    }
}

[FastMoqGeneratedTestTarget(typeof(OrderSubmitter), typeof(IOrderGateway))]
public partial class OrderSubmitterTests : MockerTestBase<OrderSubmitter>
{
}
";

            var result = await RunGeneratorAsync(source);

            result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();

            var generatedSource = result.GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
            generatedSource.Should().Contain("typeof(global::Demo.Tests.IOrderGateway)");
            generatedSource.Should().NotContain("new global::System.Type[]\r\n            {\r\n            };");
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldNotEmit_WhenMultiplePublicConstructorsRemainAmbiguous()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public interface IOrderGateway { }
public interface IAuditWriter { }

public sealed class OrderSubmitter
{
    public OrderSubmitter(IOrderGateway gateway)
    {
    }

    public OrderSubmitter(IAuditWriter auditWriter)
    {
    }
}

[FastMoqGeneratedTestTarget(typeof(OrderSubmitter))]
public partial class OrderSubmitterTests : MockerTestBase<OrderSubmitter>
{
}
";

            var result = await RunGeneratorAsync(source);

            result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            result.GeneratedSources.Should().BeEmpty();
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldWorkInRepresentativeCompiledConsumerScenario()
        {
            var loadedAssembly = await LoadGeneratedAssemblyAsync(RepresentativeConsumingScenarioSource);

            var generatedHarness = CreateInstance(loadedAssembly, "Demo.Tests.GeneratedConstructorHarness");

            Invoke<string>(generatedHarness, "DescribeConstructorKind").Should().Be("selected");
            Invoke<Type?[]>(generatedHarness, "DescribeConstructorTypes")
                .Should()
                .Equal([typeof(System.IO.Abstractions.IFileSystem), typeof(string)]);

            var generatedPlan = Invoke<FastMoq.Models.InstanceConstructionPlan>(generatedHarness, "DescribeComponentConstruction");
            generatedPlan.Parameters.Should().HaveCount(2);
            generatedPlan.Parameters[0].Name.Should().Be("fileSystem");
            generatedPlan.Parameters[0].ParameterType.Should().Be(typeof(System.IO.Abstractions.IFileSystem));
            generatedPlan.Parameters[1].Name.Should().Be("value");
            generatedPlan.Parameters[1].ParameterType.Should().Be(typeof(string));
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldMatchManualRuntimeHarness_ForRepresentativeConsumerScenario()
        {
            var loadedAssembly = await LoadGeneratedAssemblyAsync(RepresentativeConsumingScenarioSource);

            var generatedHarness = CreateInstance(loadedAssembly, "Demo.Tests.GeneratedConstructorHarness");
            var manualHarness = CreateInstance(loadedAssembly, "Demo.Tests.ManualConstructorHarness");

            var generatedPlan = Invoke<FastMoq.Models.InstanceConstructionPlan>(generatedHarness, "DescribeComponentConstruction");
            var manualPlan = Invoke<FastMoq.Models.InstanceConstructionPlan>(manualHarness, "DescribeComponentConstruction");

            Invoke<string>(generatedHarness, "DescribeConstructorKind").Should().Be(Invoke<string>(manualHarness, "DescribeConstructorKind"));
            Invoke<bool>(generatedHarness, "DescribeValueWasNull").Should().Be(Invoke<bool>(manualHarness, "DescribeValueWasNull"));
            Invoke<Type?[]>(generatedHarness, "DescribeConstructorTypes").Should().Equal(Invoke<Type?[]>(manualHarness, "DescribeConstructorTypes"));

            generatedPlan.RequestedType.Should().Be(manualPlan.RequestedType);
            generatedPlan.ResolvedType.Should().Be(manualPlan.ResolvedType);
            generatedPlan.UsedNonPublicConstructor.Should().Be(manualPlan.UsedNonPublicConstructor);
            generatedPlan.UsedPreferredConstructorAttribute.Should().Be(manualPlan.UsedPreferredConstructorAttribute);
            generatedPlan.UsedAmbiguityFallback.Should().Be(manualPlan.UsedAmbiguityFallback);
            generatedPlan.Parameters.Select(static parameter => parameter.Name)
                .Should()
                .Equal(manualPlan.Parameters.Select(static parameter => parameter.Name));
            generatedPlan.Parameters.Select(static parameter => parameter.ParameterType)
                .Should()
                .Equal(manualPlan.Parameters.Select(static parameter => parameter.ParameterType));
            generatedPlan.Parameters.Select(static parameter => parameter.Source)
                .Should()
                .Equal(manualPlan.Parameters.Select(static parameter => parameter.Source));

            var metadataType = generatedHarness.GetType().GetNestedType("FastMoqGeneratedHarnessMetadata", BindingFlags.NonPublic);
            metadataType.Should().NotBeNull();
            var dependencyNames = (string[]) metadataType!.GetProperty("DependencyNames", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            var dependencyTypes = (Type[]) metadataType.GetProperty("DependencyTypes", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

            dependencyNames.Should().Equal(generatedPlan.Parameters.Select(static parameter => parameter.Name));
            dependencyTypes.Should().Equal(generatedPlan.Parameters.Select(static parameter => parameter.ParameterType));
        }

        private static async Task<GeneratorTestResult> RunGeneratorAsync(string source)
        {
            var document = AnalyzerTestHelpers.CreateDocumentForTest(
                source,
                includeAzureFunctionsHelpers: false,
                includeMoqProviderPackage: false,
                includeNSubstituteProviderPackage: false,
                includeWebHelpers: false,
                includeDatabaseHelpers: false,
                includeAzureHelpers: false,
                includeAggregatePackage: false);
            var compilation = await document.Project.GetCompilationAsync();
            compilation.Should().NotBeNull();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [new GeneratedHarnessSourceGenerator().AsSourceGenerator()],
                parseOptions: (CSharpParseOptions) document.Project.ParseOptions!);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation!, out var outputCompilation, out var diagnostics);
            var runResult = driver.GetRunResult();

            return new GeneratorTestResult(
                outputCompilation,
                diagnostics,
                runResult.Results.SelectMany(static result => result.GeneratedSources).ToImmutableArray());
        }

        private static async Task<Assembly> LoadGeneratedAssemblyAsync(string source)
        {
            var result = await RunGeneratorAsync(source);
            result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();

            using var assemblyStream = new MemoryStream();
            var emitResult = result.OutputCompilation
                .WithAssemblyName("GeneratedHarnessTests_" + System.Guid.NewGuid().ToString("N"))
                .Emit(assemblyStream);
            emitResult.Success.Should().BeTrue(string.Join(System.Environment.NewLine, emitResult.Diagnostics));

            return Assembly.Load(assemblyStream.ToArray());
        }

        private static object CreateInstance(Assembly assembly, string typeName)
        {
            var type = assembly.GetType(typeName, throwOnError: true)!;
            return Activator.CreateInstance(type)!;
        }

        private static T Invoke<T>(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            method.Should().NotBeNull();
            return (T) method!.Invoke(instance, null)!;
        }

        private sealed class GeneratorTestResult
        {
            public GeneratorTestResult(
                Compilation outputCompilation,
                ImmutableArray<Diagnostic> driverDiagnostics,
                ImmutableArray<GeneratedSourceResult> generatedSources)
            {
                OutputCompilation = outputCompilation;
                DriverDiagnostics = driverDiagnostics;
                GeneratedSources = generatedSources;
            }

            public Compilation OutputCompilation { get; }

            public ImmutableArray<Diagnostic> DriverDiagnostics { get; }

            public ImmutableArray<GeneratedSourceResult> GeneratedSources { get; }
        }
    }
}
using FastMoq.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FastMoq.Analyzers.Tests
{
    public sealed class GeneratedHarnessSourceGeneratorTests
    {
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

            Assert.Empty(result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            Assert.Empty(result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

            var generatedSource = Assert.Single(result.GeneratedSources);
            Assert.Contains("protected override global::System.Type?[]? ComponentConstructorParameterTypes =>", generatedSource.SourceText.ToString());
            Assert.Contains("typeof(global::Demo.Tests.IOrderGateway)", generatedSource.SourceText.ToString());
            Assert.Contains("\"gateway\"", generatedSource.SourceText.ToString());
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

            Assert.Empty(result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            Assert.Empty(result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

            var generatedSource = Assert.Single(result.GeneratedSources).SourceText.ToString();
            Assert.Contains("typeof(global::Demo.Tests.IOrderGateway)", generatedSource);
            Assert.DoesNotContain("new global::System.Type[]\r\n            {\r\n            };", generatedSource);
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

            Assert.Empty(result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            Assert.Empty(result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            Assert.Empty(result.GeneratedSources);
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
            Assert.NotNull(compilation);

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
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FastMoq.Analyzers.Tests
{
    public sealed class GeneratorPackageMatrixTests
    {
        [Fact]
        public async Task GetGeneratedTestPackageMatrix_ShouldClassifyCoreOnlyLayout()
        {
            var document = AnalyzerTestHelpers.CreateDocumentForTest(
                "class Sample { }",
                includeAzureFunctionsHelpers: false,
                includeMoqProviderPackage: false,
                includeNSubstituteProviderPackage: false,
                includeWebHelpers: false,
                includeDatabaseHelpers: false,
                includeAzureHelpers: false,
                includeAggregatePackage: false);

            var matrix = await GetMatrixAsync(document);

            Assert.Equal(FastMoqGeneratedTestPackageLayout.CoreOnly, matrix.Layout);
            Assert.False(matrix.HasAggregatePackage);
            Assert.False(matrix.HasWebHelpers);
            Assert.False(matrix.HasDatabaseHelpers);
            Assert.False(matrix.HasAzureHelpers);
            Assert.False(matrix.HasAzureFunctionsHelpers);

            var rule = Assert.Single(matrix.SupportedTargetShapes);
            Assert.Equal(GeneratedTestTargetShape.Core, rule.Shape);
            Assert.Equal("FastMoq.Core", rule.RequiredPackageName);
            Assert.Equal("FastMoq.MockerTestBase<TComponent>", rule.DefaultTestBaseTypeDisplayName);
            Assert.Equal(new[] { "FastMoq" }, rule.DefaultNamespaces);
        }

        [Fact]
        public async Task GetGeneratedTestPackageMatrix_ShouldClassifySplitHelperLayout()
        {
            var document = AnalyzerTestHelpers.CreateDocumentForTest(
                "class Sample { }",
                includeAzureFunctionsHelpers: false,
                includeMoqProviderPackage: false,
                includeNSubstituteProviderPackage: false,
                includeWebHelpers: true,
                includeDatabaseHelpers: true,
                includeAzureHelpers: false,
                includeAggregatePackage: false);

            var matrix = await GetMatrixAsync(document);

            Assert.Equal(FastMoqGeneratedTestPackageLayout.SplitHelpers, matrix.Layout);
            Assert.False(matrix.HasAggregatePackage);
            Assert.True(matrix.HasWebHelpers);
            Assert.True(matrix.HasDatabaseHelpers);
            Assert.False(matrix.HasAzureHelpers);
            Assert.False(matrix.HasAzureFunctionsHelpers);
            Assert.Equal(
                new[]
                {
                    GeneratedTestTargetShape.Core,
                    GeneratedTestTargetShape.Web,
                    GeneratedTestTargetShape.Blazor,
                    GeneratedTestTargetShape.Database,
                },
                matrix.SupportedTargetShapes.Select(rule => rule.Shape).ToArray());
            Assert.Equal("FastMoq.Web", matrix.SupportedTargetShapes[1].RequiredPackageName);
            Assert.Equal("FastMoq.Database", matrix.SupportedTargetShapes[3].RequiredPackageName);
        }

        [Fact]
        public async Task GetGeneratedTestPackageMatrix_ShouldClassifyAggregateLayout()
        {
            var document = AnalyzerTestHelpers.CreateDocumentForTest(
                "class Sample { }",
                includeAzureFunctionsHelpers: false,
                includeMoqProviderPackage: false,
                includeNSubstituteProviderPackage: false,
                includeWebHelpers: false,
                includeDatabaseHelpers: false,
                includeAzureHelpers: false,
                includeAggregatePackage: true);

            var matrix = await GetMatrixAsync(document);

            Assert.Equal(FastMoqGeneratedTestPackageLayout.Aggregate, matrix.Layout);
            Assert.True(matrix.HasAggregatePackage);
            Assert.True(matrix.HasWebHelpers);
            Assert.True(matrix.HasDatabaseHelpers);
            Assert.True(matrix.HasAzureHelpers);
            Assert.True(matrix.HasAzureFunctionsHelpers);
            Assert.Equal(
                new[]
                {
                    GeneratedTestTargetShape.Core,
                    GeneratedTestTargetShape.Web,
                    GeneratedTestTargetShape.Blazor,
                    GeneratedTestTargetShape.Database,
                    GeneratedTestTargetShape.Azure,
                    GeneratedTestTargetShape.AzureFunctions,
                },
                matrix.SupportedTargetShapes.Select(rule => rule.Shape).ToArray());
            Assert.All(matrix.SupportedTargetShapes, rule => Assert.Equal("FastMoq", rule.RequiredPackageName));
        }

        private static async Task<FastMoqGeneratedTestPackageMatrix> GetMatrixAsync(Document document)
        {
            var compilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
            Assert.NotNull(compilation);
            return FastMoqAnalysisHelpers.GetGeneratedTestPackageMatrix(compilation!);
        }
    }
}
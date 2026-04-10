using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using FluentAssertions;
using Moq;
using Xunit;

namespace FastMoq.TestingExample
{
    public class ProviderSelectionExampleTests : MockerTestBase<ProviderSelectionService>
    {
        [Fact]
        public void AssemblyDefaultProviderAttribute_ShouldSetMoqAsAppWideDefault()
        {
            MockingProviderRegistry.Default.Should().BeSameAs(MoqMockingProvider.Instance);
        }

        [Fact]
        public void AppWideDefaultProvider_ShouldEnableMoqCompatibilitySurface()
        {
            Mocks.GetMock<IProviderSelectionDependency>()
                .Setup(x => x.GetValue())
                .Returns("configured via moq");

            Component.GetValue().Should().Be("configured via moq");
            Mocks.GetMock<IProviderSelectionDependency>()
                .Verify(x => x.GetValue(), Times.Once);
        }
    }

    public interface IProviderSelectionDependency
    {
        string GetValue();
    }

    public class ProviderSelectionService(IProviderSelectionDependency dependency)
    {
        public string GetValue() => dependency.GetValue();
    }
}
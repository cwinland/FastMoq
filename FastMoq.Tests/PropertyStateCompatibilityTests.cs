using FastMoq.Extensions;
using FastMoq.Providers;

namespace FastMoq.Tests
{
    public class PropertyStateCompatibilityTests
    {
        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public void AddPropertyState_ShouldReplaceResolvedInstance_WhenTrackedMockAlreadyExists(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            var trackedMock = mocker.GetOrCreateMock<IPropertyStateCompatibilityGateway>();
            var trackedInstance = trackedMock.Instance;

            var proxy = mocker.AddPropertyState<IPropertyStateCompatibilityGateway>();
            var resolved = mocker.GetObject<IPropertyStateCompatibilityGateway>();

            trackedInstance.Should().NotBeNull();
            proxy.Should().BeSameAs(resolved);
            proxy.Should().NotBeSameAs(trackedInstance);
            trackedMock.Instance.Should().BeSameAs(trackedInstance);
            trackedMock.Instance.Should().NotBeSameAs(resolved);

            proxy.Mode = "fast";
            proxy.Mode.Should().Be("fast");
            trackedMock.Instance.Mode.Should().Be("fast");
        }

        public interface IPropertyStateCompatibilityGateway
        {
            string? Mode { get; set; }
        }
    }
}
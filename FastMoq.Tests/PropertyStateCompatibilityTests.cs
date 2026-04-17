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

            // The proxy replaces the resolved registration, but the default mode
            // still writes property assignments through to the wrapped inner instance.
            proxy.Mode = "fast";
            proxy.Mode.Should().Be("fast");
            trackedMock.Instance.Mode.Should().Be("fast");
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public void AddPropertyState_ProxyOnlyMode_ShouldKeepAssignmentsDetached_FromWrappedInstance(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            var trackedMock = mocker.GetOrCreateMock<IPropertyStateCompatibilityGateway>();
            var trackedInstance = trackedMock.Instance;
            var originalMode = trackedInstance.Mode;

            var proxy = mocker.AddPropertyState<IPropertyStateCompatibilityGateway>(PropertyStateMode.ProxyOnly);
            var resolved = mocker.GetObject<IPropertyStateCompatibilityGateway>();

            proxy.Should().BeSameAs(resolved);
            proxy.Should().NotBeSameAs(trackedInstance);

            proxy.Mode = "fast";

            proxy.Mode.Should().Be("fast");
            trackedMock.Instance.Mode.Should().Be(originalMode);
        }

        public interface IPropertyStateCompatibilityGateway
        {
            string? Mode { get; set; }
        }
    }
}
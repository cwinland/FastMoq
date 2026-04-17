using System;
using System.Threading.Tasks;
using FastMoq.Extensions;
using FastMoq.Providers;

namespace FastMoq.Tests
{
    public class PropertySetterCaptureTests
    {
        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public void AddPropertyState_ShouldPreserveAssignments_AndForwardOtherMembers(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            _ = mocker.GetOrCreateMock<IPropertySetterCaptureGateway>();
            var gateway = mocker.AddPropertyState<IPropertySetterCaptureGateway>();

            gateway.Mode = "fast";
            gateway.Publish("alpha");

            gateway.Mode.Should().Be("fast");
            mocker.Verify<IPropertySetterCaptureGateway>(x => x.Publish("alpha"), TimesSpec.Once);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public void AddPropertyState_ProxyOnlyMode_ShouldKeepAssignmentsDetached_AndForwardOtherMembers(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            var trackedMock = mocker.GetOrCreateMock<IPropertySetterCaptureGateway>();
            var originalMode = trackedMock.Instance.Mode;
            var gateway = mocker.AddPropertyState<IPropertySetterCaptureGateway>(PropertyStateMode.ProxyOnly);

            gateway.Mode = "fast";
            gateway.Publish("alpha");

            gateway.Mode.Should().Be("fast");
            trackedMock.Instance.Mode.Should().Be(originalMode);
            mocker.Verify<IPropertySetterCaptureGateway>(x => x.Publish("alpha"), TimesSpec.Once);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public void AddPropertySetterCapture_ShouldCaptureAssignments_AndForwardOtherMembers(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            _ = mocker.GetOrCreateMock<IPropertySetterCaptureGateway>();
            var modeCapture = mocker.AddPropertySetterCapture<IPropertySetterCaptureGateway, string?>(x => x.Mode);

            var gateway = mocker.GetObject<IPropertySetterCaptureGateway>();
            gateway.Should().NotBeNull();

            gateway!.Mode = "fast";
            gateway.Publish("alpha");

            modeCapture.HasValue.Should().BeTrue();
            modeCapture.Value.Should().Be("fast");
            gateway.Mode.Should().Be("fast");
            mocker.Verify<IPropertySetterCaptureGateway>(x => x.Publish("alpha"), TimesSpec.Once);
        }

        [Fact]
        public void AddPropertySetterCapture_ShouldForwardSetterToExistingConcreteRegistration()
        {
            var mocker = new Mocker();
            mocker.AddType<IPropertySetterCaptureGateway>(new PropertySetterCaptureGatewayFake());

            var modeCapture = mocker.AddPropertySetterCapture<IPropertySetterCaptureGateway, string?>(x => x.Mode);
            var gateway = mocker.GetObject<IPropertySetterCaptureGateway>();

            gateway.Should().NotBeNull();
            gateway!.Mode = "beta";

            modeCapture.Value.Should().Be("beta");
            gateway.ReadMode().Should().Be("beta");
        }

        [Fact]
        public void AddPropertySetterCapture_ShouldRejectNonInterfaceTypes()
        {
            var mocker = new Mocker();

            Action action = () => mocker.AddPropertySetterCapture<PropertySetterCaptureConcreteTarget, string?>(x => x.Mode);

            action.Should().Throw<NotSupportedException>()
                .WithMessage("*interface types only*");
        }

        [Fact]
        public void AddPropertySetterCapture_ShouldSupportMockerTestBase_WhenComponentIsRecreated()
        {
            using var testBase = new PropertySetterCaptureComponentTestBase();

            var modeCapture = testBase.AddModeCapture();

            testBase.Submit("alpha", expedited: true);

            modeCapture.Value.Should().Be("fast");
            testBase.VerifyPublished("alpha");
        }

        [Fact]
        public void AddPropertyState_ShouldRejectNonInterfaceTypes()
        {
            var mocker = new Mocker();

            Action action = () => mocker.AddPropertyState<PropertySetterCaptureConcreteTarget>();

            action.Should().Throw<NotSupportedException>()
                .WithMessage("*interface types only*");
        }

        [Fact]
        public void AddPropertyState_ShouldSupportMockerTestBase_WhenComponentIsRecreated()
        {
            using var testBase = new PropertySetterCaptureComponentTestBase();

            var gateway = testBase.AddModeState();

            testBase.Submit("alpha", expedited: true);

            gateway.Mode.Should().Be("fast");
            testBase.VerifyPublished("alpha");
        }

        public interface IPropertySetterCaptureGateway
        {
            string? Mode { get; set; }

            void Publish(string value);

            string? ReadMode();
        }

        private sealed class PropertySetterCaptureGatewayFake : IPropertySetterCaptureGateway
        {
            public string? Mode { get; set; }

            public void Publish(string value)
            {
            }

            public string? ReadMode()
            {
                return Mode;
            }
        }

        private sealed class PropertySetterCaptureConcreteTarget
        {
            public string? Mode { get; set; }
        }

        private sealed class PropertySetterCaptureComponent
        {
            private readonly IPropertySetterCaptureGateway _gateway;

            public PropertySetterCaptureComponent(IPropertySetterCaptureGateway gateway)
            {
                _gateway = gateway;
            }

            public void Submit(string value, bool expedited)
            {
                _gateway.Mode = expedited ? "fast" : "standard";
                _gateway.Publish(value);
            }
        }

        private sealed class PropertySetterCaptureComponentTestBase : MockerTestBase<PropertySetterCaptureComponent>
        {
            public IPropertySetterCaptureGateway AddModeState()
            {
                var gateway = Mocks.AddPropertyState<IPropertySetterCaptureGateway>();
                CreateComponent();
                return gateway;
            }

            public PropertyValueCapture<string?> AddModeCapture()
            {
                var capture = Mocks.AddPropertySetterCapture<IPropertySetterCaptureGateway, string?>(x => x.Mode);
                CreateComponent();
                return capture;
            }

            public void Submit(string value, bool expedited)
            {
                Component.Submit(value, expedited);
            }

            public void VerifyPublished(string value)
            {
                Mocks.Verify<IPropertySetterCaptureGateway>(x => x.Publish(value), TimesSpec.Once);
            }
        }
    }
}
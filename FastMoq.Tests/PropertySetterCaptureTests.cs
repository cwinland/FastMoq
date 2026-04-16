using System;
using FastMoq.Extensions;
using FastMoq.Providers;

namespace FastMoq.Tests
{
    public class PropertySetterCaptureTests
    {
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
    }
}
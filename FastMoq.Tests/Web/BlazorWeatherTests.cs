using Bunit;
using FastMoq.Tests.Blazor.Data;
using FastMoq.Tests.Blazor.Pages;
using FastMoq.Web.Blazor;
using FluentAssertions;
using System;
using Xunit;

#pragma warning disable CS8604 // Possible null reference argument for parameter.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'.
#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable.
#pragma warning disable CS8974 // Converting method group to non-delegate type
#pragma warning disable CS0472 // The result of the expression is always 'value1' since a value of type 'value2' is never equal to 'null' of type 'value3'.

namespace FastMoq.Tests.Web
{
    public class BlazorWeatherTests : MockerBlazorTestBase<FetchData> {

        public BlazorWeatherTests() : base(true) { }

        [Fact]
        public void ComponentCreated()
        {
            Setup();
            Component.Should().NotBeNull();
            Instance.Should().NotBeNull();
            Component.Instance.Should().Be(Instance);
            Component.Markup.Contains("Temp.").Should().BeTrue();

            // IWeatherForecastService is automatically injected and should not be null.
            Instance.WeatherService.Should().NotBeNull();
        }

        [Fact]
        public void InjectedService_ShouldNotHaveInjectedParameter()
        {
            Setup();
            Component = RenderComponent(true);
            // Will be null because interface does not have inject attribute
            Instance.WeatherService.FileSystem.Should().BeNull();
        }

        [Fact]
        public void InjectedService_ShouldHaveInjectedParameter()
        {
            Mocks.AddType<IWeatherForecastService, WeatherForecastService>();
            Setup();
            Component = RenderComponent(true);
            // Will be null because interface does not have inject attribute
            Instance.WeatherService.FileSystem.Should().NotBeNull();
        }
    }
}

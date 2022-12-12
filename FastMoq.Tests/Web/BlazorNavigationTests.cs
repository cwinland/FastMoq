using Bunit.TestDoubles;
using FastMoq.Tests.Blazor.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Index = FastMoq.Tests.Blazor.Pages.Index;

namespace FastMoq.Tests.Web
{
    public class BlazorNavigationTests : MockerBlazorTestBase<Index>
    {
        #region Overrides of MockerBlazorTestBase<Index>

        /// <inheritdoc />
        protected override Action<TestServiceProvider, IConfiguration, Mocker> ConfigureServices => (provider, configuration, mocks) =>
            provider.AddSingleton<IWeatherForecastService, WeatherForecastService>();

        #endregion

        [Fact]
        public void Created() => Component.Should().NotBeNull();

        [Fact]
        public void NavigateTest()
        {
            var manager = Services.GetRequiredService<FakeNavigationManager>();

            IsExists("button").Should().BeTrue();
            ButtonClick("button", () => manager.History.Count > 0);

        }
    }
}

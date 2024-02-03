using FastMoq.Tests.Blazor.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Index = FastMoq.Tests.Blazor.Pages.Index;

namespace FastMoq.Tests.Web
{
    /// <summary>
    ///     Class BlazorNavigationTests.
    ///     Implements the <see cref="MockerBlazorTestBase{Index}" />
    /// </summary>
    /// <inheritdoc />
    /// <seealso cref="MockerBlazorTestBase{Index}" />
    public class BlazorNavigationTests : MockerBlazorTestBase<Index>
    {
        #region Overrides of MockerBlazorTestBase<Index>

        /// <inheritdoc />
        protected override Action<TestServiceProvider, IConfiguration, Mocker> ConfigureServices => (provider, configuration, mocks) =>
            provider.AddSingleton<IWeatherForecastService, WeatherForecastService>();

        #endregion

        /// <summary>
        ///     Defines the test method Created.
        /// </summary>
        [Fact]
        public void Created() => Component.Should().NotBeNull();

        /// <summary>
        ///     Defines the test method NavigateTest.
        /// </summary>
        [Fact]
        public void NavigateTest()
        {
            IsExists("button").Should().BeTrue();
            ClickButton("button", () => NavigationManager.History.Count > 0);

        }
    }
}

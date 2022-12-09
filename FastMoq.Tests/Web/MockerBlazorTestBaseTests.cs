using FastMoq.Tests.Blazor.Data;
using FastMoq.Tests.Blazor.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Index = FastMoq.Tests.Blazor.Pages.Index;

namespace FastMoq.Tests.Web
{
    public class MockerBlazorTestBaseTests : MockerBlazorTestBase<Index>
    {
        #region Properties

        /// <inheritdoc />
        protected override Action<TestServiceProvider, IConfiguration, Mocker> ConfigureServices =>
            (s, c, m) => s.AddSingleton<IWeatherForecastService, WeatherForecastService>();

        #endregion

        [Fact]
        public void Create() => Component.Should().NotBeNull();

        [Fact]
        public void GetChildComponent_ShouldNotBeNull()
        {
            GetComponent<FetchData>(_ => true).Should().NotBeNull();

            new Action(() => GetComponent<FetchData>(x => x.ComponentId == 12345)).Should().Throw<InvalidOperationException>()
                .WithMessage("Sequence contains no matching element");
        }

        [Fact]
        public void GetComponent_ShouldBeGetComponentPredicate() => GetComponent<FetchData>().Should().Be(GetComponent<FetchData>(_ => true));

        [Fact]
        public void GetComponent_ShouldNotBeNull()
        {
            Dictionary<ComponentBase, ComponentState> allComponents = GetAllComponents();
            allComponents.Should().NotBeNull();
            var fetchData = allComponents.First(x => x.Key is FetchData).Key;
            IRenderedComponent<FetchData> childComponent = GetComponent<FetchData>(p => true);
            childComponent.Should().NotBeNull();
            childComponent.Instance.Should().Be(fetchData);
        }

        [Fact]
        public void GetComponents_ShouldReturnFetchData() => GetComponents<FetchData>().First().Should().Be(GetComponent<FetchData>(_ => true));
    }
}

using AngleSharp.Dom;
using Bunit;
using Bunit.Extensions;
using Bunit.Rendering;
using FastMoq.Tests.Blazor.Data;
using FastMoq.Tests.Blazor.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
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
            GetComponent<FetchData>().Should().NotBeNull();

            new Action(() => GetComponent<FetchData>(x => x.ComponentId == 12345)).Should().Throw<InvalidOperationException>()
                .WithMessage("Sequence contains no matching element");
        }

        [Fact]
        public void GetComponent_ShouldBeGetComponentPredicate() => GetComponent<FetchData>().Should().Be(GetComponent<FetchData>((IElement _) => true));

        [Fact]
        public void GetComponent_ShouldNotBeNull()
        {
            var allComponents = GetAllComponents();
            allComponents.Should().NotBeNull();
            var fetchData = allComponents.First(x => x.Key is FetchData);
            IRenderedComponent<FetchData> childComponent = GetComponent<FetchData>((IElement _) => true);
            childComponent.Should().NotBeNull();
            childComponent.Instance.Should().Be(fetchData.Key);
            var a = fetchData.Value.Component;
            fetchData.Value.ComponentId.Should().Be(childComponent.ComponentId);
            var c = fetchData.Value.ParentComponentState;
            fetchData.Value.CurrentRenderTree.Should().NotBeNull();
            fetchData.Value.IsComponentBase.Should().BeTrue();
        }

        [Fact]
        public void GetComponents_ShouldReturnGetComponentEquivalent()
        {
            var componentState = GetAllComponents<FetchData>().First().Value;
            var comp = componentState.RenderedComponent as IRenderedComponent<FetchData>;
            comp.Should().BeEquivalentTo(GetComponent<FetchData>((IElement _) => true));
        }

        [Fact]
        public void ClickButton_Invalid_ShouldThrow()
        {
            IElement? button = null;
            new Action(() => ButtonClick(button, () => true)).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ClickButton_ShouldClick()
        {
            ButtonClick("button", () => true).Should().BeTrue();
            ButtonClick("button[id='testbutton']", () => true).Should().BeTrue();
            ButtonClick(Component.FindAll("button").First(x => x.Id == "testbutton"), () => true).Should().BeTrue();
        }

        [Fact]
        public void FindById_ShouldFind()
        {
            Component.FindAll("button").First(x => x.Id == "testbutton").Should().Be(FindById("testbutton"));
        }

        [Fact]
        public void FindByName_ShouldFind()
        {
            FindAllByTag("button").Should().HaveCount(1);
        }
    }
}

﻿using AngleSharp.Dom;
using FastMoq.Tests.Blazor.Data;
using FastMoq.Tests.Blazor.Pages;
using FastMoq.Web.Blazor.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
            var componentState = GetAllComponents<FetchData>().First().Value as ComponentState<FetchData>;
            var renderedComponent = componentState.GetOrCreateRenderedComponent<FetchData>();
            (componentState.Component is FetchData).Should().BeTrue();
            renderedComponent.Should().BeEquivalentTo(GetComponent<FetchData>((IElement _) => true));
        }

        [Fact]
        public void ClickButton_Invalid_ShouldThrow()
        {
            IElement? button = null;
            new Action(() => ClickButton(button, () => true)).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ClickButton_ShouldClick()
        {
            NavigationManager.History.Count.Should().Be(0);
            ClickButton("button", () => NavigationManager.History.Count == 1);
            NavigationManager.History.Count.Should().Be(1);
            ClickButton("button[id='testbutton']", () => NavigationManager.History.Count == 2);
            NavigationManager.History.Count.Should().Be(2);
            ClickButton(FindAllByTag("button").First(x => x.Id == "testbutton"), () => NavigationManager.History.Count == 3);
            NavigationManager.History.Count.Should().Be(3);
            ClickButton(FindById("testbutton"), () => NavigationManager.History.Count == 4);
            NavigationManager.History.Count.Should().Be(4);
            ClickButton("button", () => NavigationManager.History.Count == 5, Component, TimeSpan.FromSeconds(5));
            NavigationManager.History.Count.Should().Be(5);
            ClickButton(e => e.Id == "testbutton", () => NavigationManager.History.Count == 6);
            NavigationManager.History.Count.Should().Be(6);
        }

        [Fact]
        public void FindById_ShouldFind() => Component.FindAll("button").First(x => x.Id == "testbutton").Should().Be(FindById("testbutton"));

        [Fact]
        public void FindByName_ShouldFind() => FindAllByTag("button").Should().HaveCount(1);

        [Fact]
        public void AuthUser_Set_ShouldChangeUser()
        {
            AuthContext.UserName.Should().Be("TestUser");
            AuthContext.IsAuthenticated.Should().BeTrue();
            AuthUsername.Should().Be("TestUser");
            AuthUsername = "test1";
            AuthContext.UserName.Should().Be("test1");
            AuthContext.IsAuthenticated.Should().BeTrue();
        }

        private void TestAuth<T>(Func<IEnumerable<T>> authCollection, ICollection<T> baseCollection, T newItem)
        {
            authCollection.Invoke().Should().BeEquivalentTo(baseCollection.ToArray());
            authCollection.Invoke().Should().HaveCount(0);
            baseCollection.Add(newItem);
            authCollection.Invoke().Should().HaveCount(1);
            authCollection.Invoke().Should().BeEquivalentTo(baseCollection.ToArray());
            authCollection.Invoke().Should().Contain(newItem);
        }

        [Fact]
        public void AuthRoles_Set_ShouldChangeRoles() => TestAuth<string>(() => AuthContext.Roles, AuthorizedRoles, "testRole");

        [Fact]
        public void AuthClaims_Set_ShouldChangeClaims() => TestAuth<Claim>(() => AuthContext.Claims, AuthorizedClaims, new Claim("group", "testClaim"));

        [Fact]
        public void AuthPolicies_Set_ShouldChange() => TestAuth<string>(() => AuthContext.Policies, AuthorizedPolicies, "testPolicy");
    }
}
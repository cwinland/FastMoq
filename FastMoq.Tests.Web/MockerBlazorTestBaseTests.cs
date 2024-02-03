using AngleSharp.Dom;
using FastMoq.Tests.Blazor.Data;
using FastMoq.Tests.Blazor.Pages;
using FastMoq.Web.Blazor.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Index = FastMoq.Tests.Blazor.Pages.Index;

#pragma warning disable CS8604 // Possible null reference argument for parameter.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'.
#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable.
#pragma warning disable CS8974 // Converting method group to non-delegate type
#pragma warning disable CS0472 // The result of the expression is always 'value1' since a value of type 'value2' is never equal to 'null' of type 'value3'.

namespace FastMoq.Tests.Web
{
    /// <summary>
    ///     Class MockerBlazorTestBaseTests.
    ///     Implements the <see cref="MockerBlazorTestBase{Index}" />
    /// </summary>
    /// <inheritdoc />
    /// <seealso cref="MockerBlazorTestBase{Index}" />
    public class MockerBlazorTestBaseTests : MockerBlazorTestBase<Index>
    {
        #region Properties

        /// <inheritdoc />
        protected override Action<TestServiceProvider, IConfiguration, Mocker> ConfigureServices =>
            (s, c, m) => s.AddSingleton<IWeatherForecastService, WeatherForecastService>();

        #endregion

        /// <summary>
        ///     Defines the test method Create.
        /// </summary>
        [Fact]
        public void Create() => Component.Should().NotBeNull();

        /// <summary>
        ///     Defines the test method GetChildComponent_ShouldNotBeNull.
        /// </summary>
        [Fact]
        public void GetChildComponent_ShouldNotBeNull()
        {
            GetComponent<FetchData>().Should().NotBeNull();

            new Action(() => GetComponent<FetchData>(x => x.ComponentId == 12345)).Should().Throw<InvalidOperationException>()
                .WithMessage("Sequence contains no matching element");
        }

        /// <summary>
        ///     Defines the test method GetComponent_ShouldBeGetComponentPredicate.
        /// </summary>
        [Fact]
        public void GetComponent_ShouldBeGetComponentPredicate() => GetComponent<FetchData>().Should().Be(GetComponent<FetchData>((IElement _) => true));

        /// <summary>
        ///     Defines the test method GetComponent_ShouldNotBeNull.
        /// </summary>
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

        /// <summary>
        ///     Defines the test method GetComponents_ShouldReturnGetComponentEquivalent.
        /// </summary>
        [Fact]
        public void GetComponents_ShouldReturnGetComponentEquivalent()
        {
            var componentState = GetAllComponents<FetchData>().First().Value as ComponentState<FetchData>;
            var renderedComponent = componentState.GetOrCreateRenderedComponent<FetchData>();
            (componentState.Component is FetchData).Should().BeTrue();
            renderedComponent.Should().BeEquivalentTo(GetComponent<FetchData>((IElement _) => true));
        }

        /// <summary>
        ///     Defines the test method ClickButton_Invalid_ShouldThrow.
        /// </summary>
        [Fact]
        public void ClickButton_Invalid_ShouldThrow()
        {
            IElement? button = null;
            new Action(() => ClickButton(button, () => true)).Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        ///     Defines the test method ClickButton_ShouldClick.
        /// </summary>
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

        /// <summary>
        ///     Defines the test method FindById_ShouldFind.
        /// </summary>
        [Fact]
        public void FindById_ShouldFind() => Component.FindAll("button").First(x => x.Id == "testbutton").Should().Be(FindById("testbutton"));

        /// <summary>
        ///     Defines the test method FindByName_ShouldFind.
        /// </summary>
        [Fact]
        public void FindByName_ShouldFind() => FindAllByTag("button").Should().HaveCount(1);

        /// <summary>
        ///     Defines the test method AuthUser_Set_ShouldChangeUser.
        /// </summary>
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

        private static void TestAuth<T>(Func<IEnumerable<T>> authCollection, ICollection<T> baseCollection, T newItem)
        {
            authCollection.Invoke().Should().BeEquivalentTo(baseCollection.ToArray());
            authCollection.Invoke().Should().HaveCount(0);
            baseCollection.Add(newItem);
            authCollection.Invoke().Should().HaveCount(1);
            authCollection.Invoke().Should().BeEquivalentTo(baseCollection.ToArray());
            authCollection.Invoke().Should().Contain(newItem);
        }

        /// <summary>
        ///     Defines the test method AuthRoles_Set_ShouldChangeRoles.
        /// </summary>
        [Fact]
        public void AuthRoles_Set_ShouldChangeRoles() => TestAuth(() => AuthContext.Roles, AuthorizedRoles, "testRole");

        /// <summary>
        ///     Defines the test method AuthClaims_Set_ShouldChangeClaims.
        /// </summary>
        [Fact]
        public void AuthClaims_Set_ShouldChangeClaims() => TestAuth(() => AuthContext.Claims, AuthorizedClaims, new Claim("group", "testClaim"));

        /// <summary>
        ///     Defines the test method AuthPolicies_Set_ShouldChange.
        /// </summary>
        [Fact]
        public void AuthPolicies_Set_ShouldChange() => TestAuth(() => AuthContext.Policies, AuthorizedPolicies, "testPolicy");

        /// <summary>
        ///     Defines the test method InterfaceProperties.
        /// </summary>
        [Fact]
        public void InterfaceProperties()
        {
            var obj = Mocks.GetObject<IHttpContextAccessor>();
            obj.HttpContext.Should().NotBeNull();

            var obj2 = Mocks.GetMock<IHttpContextAccessor>().Object;
            obj2.HttpContext.Should().NotBeNull();
        }

        /// <summary>
        ///     Defines the test method ClassProperties.
        /// </summary>
        [Fact]
        public void ClassProperties()
        {
            var obj = Mocks.CreateInstance<HttpContextAccessor>();
            obj.HttpContext.Should().NotBeNull();

            var obj2 = Mocks.CreateInstance<Microsoft.AspNetCore.Http.HttpContextAccessor>();
            obj2.HttpContext.Should().NotBeNull();

            var obj3 = Mocks.GetObject<HttpContextAccessor>();
            obj3.HttpContext.Should().NotBeNull();

            var obj4 = Mocks.GetMock<HttpContextAccessor>().Object;
            obj4.HttpContext.Should().NotBeNull();
        }

        /// <summary>
        ///     Defines the test method ClassProperties_NoResolution.
        /// </summary>
        [Fact]
        public void ClassProperties_NoResolution()
        {
            Mocks.InnerMockResolution = false;
            var obj4 = Mocks.GetMock<HttpContextAccessor>().Object;
            obj4.HttpContext.Should().BeNull();
        }
    }

    /// <summary>
    ///     Class HttpContextAccessor.
    ///     Implements the <see cref="IHttpContextAccessor" />
    /// </summary>
    /// <inheritdoc />
    /// <seealso cref="IHttpContextAccessor" />
    public class HttpContextAccessor : IHttpContextAccessor
    {
        /// <inheritdoc />
        public HttpContext? HttpContext { get; set; }
    }
}

﻿using AngleSharp.Dom;
using AngleSharpWrappers;
using Bunit;
using Bunit.Rendering;
using Bunit.TestDoubles;
using FastMoq.Collections;
using FastMoq.Web.Blazor.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
using System.Security.Claims;

namespace FastMoq.Web.Blazor
{
    /// <summary>
    ///     Class MockerBlazorTestBase.
    /// Implements the <see cref="TestContext" />
    /// Implements the <see cref="IMockerBlazorTestHelpers{T}" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="TestContext" />
    /// <seealso cref="IMockerBlazorTestHelpers{T}" />
    /// <seealso cref="ComponentBase" />
    /// <inheritdoc cref="TestContext" />
    /// <inheritdoc cref="IMockerBlazorTestHelpers{T}" />
    /// <example>
    /// Basic Example
    /// <code language="cs"><![CDATA[
    /// public class IndexTests : MockerBlazorTestBase<Index>
    /// {
    ///     [Fact]
    ///     public void Create() => Component.Should().NotBeNull();
    /// }
    /// ]]></code></example>
    /// <example>
    /// Setup Services
    /// <code language="cs"><![CDATA[
    /// protected override Action<TestServiceProvider, IConfiguration, Mocker> ConfigureServices => (services, c, m) => services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
    /// ]]></code></example>
    /// <example>
    /// Setup Roles.
    /// <code language="cs"><![CDATA[
    /// protected override MockerObservableCollection<string> AuthorizedRoles => new MockerObservableCollection<string>() { "Role1", "Role2"}
    /// ]]></code></example>
    /// <example>
    /// Setup Http Response Message
    /// <code language="cs"><![CDATA[
    /// protected override Action<Mocker> SetupComponent => mocker => mocker.SetupHttpMessage(() => new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("ContextGoesHere")});
    /// ]]></code></example>
    /// <example>
    /// Setup Mocks
    /// <code language="cs"><![CDATA[
    /// protected override Action<Mocker> SetupComponent => mocker =>
    /// {
    ///     mocker.GetMock<IFile>().Setup(f => f.Exists(It.IsAny<string>())).Returns(true); // Add setup
    ///     mocker.Initialize<IDirectory>(mock => mock.Setup(d => d.Exists(It.IsAny<string>())).Returns(true)); // Clears existing mocks
    ///     mocker.GetMock<IDirectory>().Setup(d=>d.Exists("C:\\testfile.txt")).Returns(false); // add setup
    /// };
    /// ]]></code></example>
    /// <example>
    /// Click Button by class, tag, or id.
    /// <code language="cs"><![CDATA[
    /// ButtonClick("button", () => true).Should().BeTrue();
    /// ButtonClick("button[id='testbutton']", () => count > 0).Should().BeTrue();
    /// ButtonClick(Component.FindAll("button").First(x => x.Id == "testbutton"), () => IsPressed == true).Should().BeTrue();
    /// ButtonClick(FindAllByTag("button").First(x => x.Id == "testbutton"), () => true).Should().BeTrue();
    /// ButtonClick(FindById("testbutton"), () => true).Should().BeTrue();
    /// ]]></code></example>
    public abstract class MockerBlazorTestBase<T> : TestContext, IMockerBlazorTestHelpers<T> where T : ComponentBase
    {
        #region Fields

        private const string COMPONENT_LIST_NAME = "_componentStateByComponent";

        /// <summary>
        ///     The authentication username
        /// </summary>
        private string authUsername = "TestUser";

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the authentication context.
        /// </summary>
        /// <value>The authentication context.</value>
        /// <seealso cref="AuthorizedClaims" />
        /// <seealso cref="AuthorizedPolicies" />
        /// <seealso cref="AuthorizedRoles" />
        /// <seealso cref="AuthUsername" />
        /// <example>
        /// Set not authorized.
        /// <code language="cs"><![CDATA[
        /// AuthContext.SetNotAuthorized()
        /// ]]></code></example>
        /// <example>
        /// Set authorized user.
        /// <code language="cs"><![CDATA[
        /// AuthContext.SetAuthorized("username")
        /// ]]></code></example>
        protected TestAuthorizationContext AuthContext { get; }

        /// <summary>
        ///     Gets the authorized claims.
        /// </summary>
        /// <value>The authorized claims.</value>
        /// <seealso cref="AuthorizedPolicies" />
        /// <seealso cref="AuthorizedRoles" />
        /// <seealso cref="AuthContext" />
        /// <seealso cref="AuthUsername" />
        protected virtual MockerObservableCollection<Claim> AuthorizedClaims { get; } = new();

        /// <summary>
        ///     Gets the authorized policies.
        /// </summary>
        /// <value>The authorized policies.</value>
        /// <example>
        /// Setup Policies.
        /// <code language="cs"><![CDATA[
        /// protected override MockerObservableCollection<string> AuthorizedPolicies => new MockerObservableCollection<string>() { "Policy1", "Policy2"}
        /// ]]></code></example>
        /// <seealso cref="AuthorizedClaims" />
        /// <seealso cref="AuthorizedRoles" />
        /// <seealso cref="AuthContext" />
        /// <seealso cref="AuthUsername" />
        protected virtual MockerObservableCollection<string> AuthorizedPolicies { get; } = new();

        /// <summary>
        ///     Gets the authorized roles.
        /// </summary>
        /// <value>The authorized roles.</value>
        /// <example>
        /// Setup Roles.
        /// <code language="cs"><![CDATA[
        /// protected override MockerObservableCollection<string> AuthorizedRoles => new MockerObservableCollection<string>() { "Role1", "Role2"}
        /// ]]></code></example>
        /// <seealso cref="AuthorizedPolicies" />
        /// <seealso cref="AuthorizedClaims" />
        /// <seealso cref="AuthContext" />
        /// <seealso cref="AuthUsername" />
        protected virtual MockerObservableCollection<string> AuthorizedRoles { get; } = new();

        /// <summary>
        ///     Gets or sets the authentication username.
        /// </summary>
        /// <value>The authentication username.</value>
        /// <seealso cref="AuthorizedClaims" />
        /// <seealso cref="AuthorizedPolicies" />
        /// <seealso cref="AuthorizedRoles" />
        /// <seealso cref="AuthContext" />
        /// <seealso cref="AuthUsername" />
        /// <example>
        /// Set authorized user.
        /// <code language="cs"><![CDATA[
        /// AuthContext.SetAuthorized("username")
        /// AuthUsername = "TestUser";
        /// ]]></code></example>
        [ExcludeFromCodeCoverage]
        protected virtual string AuthUsername
        {
            get => authUsername;
            set
            {
                authUsername = value;
                AuthContext.SetAuthorized(value);
            }
        }

        /// <summary>
        ///     Gets or sets the component.
        /// </summary>
        /// <value>The component.</value>
        protected IRenderedComponent<T>? Component { get; set; }

        /// <summary>
        ///     Gets the configure services.
        /// </summary>
        /// <value>The configure services.</value>
        /// <example>
        /// Setup Services
        /// <code language="cs"><![CDATA[
        /// protected override Action<TestServiceProvider, IConfiguration, Mocker> ConfigureServices => (services, c, m) => services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
        /// ]]></code></example>
        [ExcludeFromCodeCoverage]
        protected virtual Action<TestServiceProvider, IConfiguration, Mocker> ConfigureServices { get; set; } = (_, _, _) => { };

        /// <summary>
        ///     Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        protected T? Instance => Component?.Instance;

        /// <summary>
        ///     Gets the mocks.
        /// </summary>
        /// <value>The mocks.</value>
        protected Mocker Mocks { get; } = new();

        /// <summary>
        ///     Gets the render parameters.
        /// </summary>
        /// <value>The render parameters.</value>
        [ExcludeFromCodeCoverage]
        protected virtual List<ComponentParameter> RenderParameters { get; } = new();

        /// <summary>
        ///     Gets or sets the setup component.
        /// </summary>
        /// <value>The setup component.</value>
        [ExcludeFromCodeCoverage]
        protected virtual Action<Mocker> SetupComponent { get; set; } = _ => { };

        /// <summary>
        ///     Gets the token source.
        /// </summary>
        /// <value>The token source.</value>
        protected CancellationTokenSource TokenSource { get; } = new();

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockerBlazorTestBase{T}" /> class.
        /// </summary>
        /// <inheritdoc />
        protected MockerBlazorTestBase() : this(false) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.Web.Blazor.MockerBlazorTestBase`1" /> class.
        /// </summary>
        /// <param name="skipSetup">if set to <c>true</c> [skip setup].</param>
        /// <inheritdoc />
        protected MockerBlazorTestBase(bool skipSetup)
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            AuthContext = this.AddTestAuthorization();

            if (skipSetup)
            {
                return;
            }

            Setup();
        }

        /// <summary>
        ///     Gets all components.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <returns>Dictionary&lt;TComponent, ComponentState&gt;.</returns>
        /// <exception cref="System.ArgumentNullException">Component</exception>
        protected internal Dictionary<TComponent, ComponentState> GetAllComponents<TComponent>() where TComponent : ComponentBase
        {
            var list = new Dictionary<TComponent, ComponentState>();

            var renderer = Component?.Services.GetRequiredService<ITestRenderer>() as TestRenderer ??
                           throw new ArgumentNullException(nameof(Component));

            var componentList = renderer.GetFieldValue<IDictionary, Renderer>(COMPONENT_LIST_NAME);

            if (componentList == null)
            {
                return list;
            }

            foreach (DictionaryEntry obj in componentList)
            {
                if (obj.Key is TComponent component)
                {
                    var componentState = new ComponentState<TComponent>(obj.Value, Services);
                    list.Add(component, componentState);
                }
            }

            return list;
        }

        /// <summary>
        ///     Gets all components.
        /// </summary>
        /// <returns>Dictionary&lt;IComponent, ComponentState&gt;.</returns>
        protected internal Dictionary<ComponentBase, ComponentState> GetAllComponents() => GetAllComponents<ComponentBase>();

        /// <summary>
        ///     Setups this instance.
        /// </summary>
        protected internal void Setup()
        {
            SetupMocks();
            SetupServices();
            SetupAuthorization();

            AuthorizedPolicies.Changed += (_, _) => AuthContext.SetPolicies(AuthorizedPolicies.ToArray());
            AuthorizedRoles.Changed += (_, _) => AuthContext.SetRoles(AuthorizedRoles.ToArray());
            AuthorizedClaims.Changed += (_, _) => AuthContext.SetClaims(AuthorizedClaims.ToArray());

            Component = RenderComponent(true);
        }

        /// <summary>
        ///     Setups the authorization.
        /// </summary>
        /// <seealso cref="AuthorizedClaims" />
        /// <seealso cref="AuthorizedPolicies" />
        /// <seealso cref="AuthorizedRoles" />
        /// <seealso cref="AuthContext" />
        /// <seealso cref="AuthUsername" />
        protected virtual void SetupAuthorization()
        {
            AuthContext.SetAuthorized(AuthUsername);
            AuthContext.SetPolicies(AuthorizedPolicies.ToArray());
            AuthContext.SetRoles(AuthorizedRoles.ToArray());
            AuthContext.SetClaims(AuthorizedClaims.ToArray());
        }

        /// <summary>
        ///     Setups the mocks.
        /// </summary>
        protected virtual void SetupMocks() => SetupComponent(Mocks);

        /// <summary>
        ///     Setups the services.
        /// </summary>
        /// <exception cref="System.IO.InvalidDataException">Unable to get {nameof(IConfigurationRoot)} object.</exception>
        protected virtual void SetupServices()
        {
            IConfiguration configuration = Mocks.GetObject<IConfigurationRoot>() ??
                                           throw new InvalidDataException($"Unable to get {nameof(IConfigurationRoot)} object.");

            // Insert component injections
            InjectComponent<T>();

            ConfigureServices.Invoke(Services, configuration, Mocks);
        }

        #region IMockerBlazorTestHelpers<T>

        /// <inheritdoc />
        public bool ButtonClick(IElement button, Func<bool> waitFunc, TimeSpan? waitTimeout = null)
        {
            if (button == null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            button.Click();
            WaitForState(waitFunc, waitTimeout);

            return true;
        }

        /// <inheritdoc />
        public bool ButtonClick(string cssSelector, Func<bool> waitFunc, TimeSpan? waitTimeout = null)
        {
            if (Component == null)
            {
                throw new ArgumentNullException(nameof(Component));
            }

            return ButtonClick(Component.Find(cssSelector), waitFunc, waitTimeout);
        }

        /// <inheritdoc />
        public bool ButtonClick<TComponent>(string cssSelector, Func<bool> waitFunc, IRenderedComponent<TComponent> startingComponent,
            TimeSpan? waitTimeout = null)
            where TComponent : class, IComponent =>
            ButtonClick(startingComponent.Find(cssSelector), waitFunc, waitTimeout);

        /// <inheritdoc />
        public bool ButtonClick(Func<IElement, bool> cssSelector, Func<bool> waitFunc, TimeSpan? waitTimeout = null)
        {
            if (cssSelector == null)
            {
                throw new ArgumentNullException(nameof(cssSelector));
            }

            if (Component == null)
            {
                throw new ArgumentNullException(nameof(Component));
            }

            return Component.FindAll("*").ToCollection()
                .Where(cssSelector)
                .Select(b => ButtonClick(b, waitFunc, waitTimeout))
                .FirstOrDefault();
        }

        /// <inheritdoc />
        public bool ButtonClick<TComponent>(string cssSelector, Func<bool> waitFunc, TimeSpan? waitTimeout = null)
            where TComponent : class, IComponent
        {
            if (string.IsNullOrWhiteSpace(cssSelector))
            {
                throw new ArgumentNullException(nameof(cssSelector));
            }

            return ButtonClick(GetComponent<TComponent>().Find(cssSelector), waitFunc, waitTimeout);
        }

        /// <inheritdoc />
        public IRenderedComponent<TComponent> ClickDropdownItem<TComponent>(IRenderedComponent<TComponent> component,
            string cssSelector, string propName, Func<bool> waitFunc) where TComponent : class, IComponent
        {
            ButtonClick(component
                    .FindAll(cssSelector)
                    .First(e => e.InnerHtml == propName),
                waitFunc
            );

            return component;
        }

        /// <inheritdoc />
        /// <exception cref="T:System.ArgumentNullException">Component</exception>
        public IRenderedComponent<TComponent> ClickDropdownItem<TComponent>(string propName, Func<bool> waitFunc,
            string itemCssSelector = "a.dropdown-item") where TComponent : class, IComponent
        {
            if (Component == null)
            {
                throw new ArgumentNullException(nameof(Component));
            }

            IRenderedComponent<TComponent> dropDown = Component.FindComponent<TComponent>();
            return ClickDropdownItem(dropDown, itemCssSelector, propName, waitFunc);
        }

        /// <inheritdoc />
        public IEnumerable<IElement> FindAllByTag(string tagName) =>
            Component?.FindAll("*")
                .Where(x => x.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase)) ??
            new List<IElement>();

        /// <inheritdoc />
        public IElement? FindById(string id) => Component?.FindAll("*")
            .First(x => x.Id?.Equals(id, StringComparison.OrdinalIgnoreCase) ?? false);

        /// <inheritdoc />
        public IRenderedComponent<TComponent> GetComponent<TComponent>() where TComponent : class, IComponent =>
            GetComponent((IRenderedComponent<TComponent> _) => true);

        /// <inheritdoc />
        public IRenderedComponent<TComponent> GetComponent<TComponent>(Func<IRenderedComponent<TComponent>, bool> predicate)
            where TComponent : class, IComponent
        {
            IReadOnlyList<IRenderedComponent<TComponent>> components = predicate == null
                ? throw new ArgumentNullException(nameof(predicate))
                : Component?.FindComponents<TComponent>() ?? throw new ArgumentNullException(nameof(Component));

            return components.Count <= 1
                ? components.First(predicate)
                : throw new AmbiguousImplementationException($"Multiple components of type '{typeof(TComponent)}' was found.");
        }

        /// <inheritdoc />
        public IRenderedComponent<TComponent> GetComponent<TComponent>(Func<IElement, bool> predicate) where TComponent : class, IComponent
        {
            List<IRenderedComponent<TComponent>> components = GetComponents<TComponent>(predicate);

            return components.Count <= 1
                ? components.First()
                : throw new AmbiguousImplementationException($"Multiple components of type '{typeof(TComponent)}' was found.");
        }

        /// <inheritdoc />
        public List<IRenderedComponent<TOfType>> GetComponents<TOfType>(Func<IRenderedComponent<TOfType>, bool>? predicate)
            where TOfType : class, IComponent
        {
            IEnumerable<IRenderedComponent<TOfType>> components = GetAllComponents()
                .Where(x => x.Key is TOfType)
                .Select(x => GetComponent<TOfType>(y => y.ComponentId == (int) (x.Value.GetPropertyValue("ComponentId") ?? 0)));

            return (predicate == null ? components : components.Where(predicate)).ToList();
        }

        /// <inheritdoc />
        public List<IRenderedComponent<TOfType>> GetComponents<TOfType>(Func<IElement, bool>? predicate) where TOfType : class, IComponent
        {
            IEnumerable<IRenderedComponent<TOfType>> components = GetAllComponents()
                .Where(x => x.Key is TOfType)
                .Select(x => GetComponent<TOfType>(y => y.ComponentId == (int) (x.Value.GetPropertyValue("ComponentId") ?? 0)));

            return (predicate == null ? components : components.Where(x => x.FindAll("*").Any(predicate))).ToList();
        }

        /// <inheritdoc />
        public IEnumerable<PropertyInfo> GetInjections(Type type, Type injectAttribute) =>
            type
                .GetRuntimeProperties()
                .Where(x => x.CustomAttributes.Any(y => y.AttributeType == injectAttribute));

        /// <inheritdoc />
        public IEnumerable<PropertyInfo> GetInjections<TComponent>() => GetInjections(typeof(TComponent));

        /// <inheritdoc />
        public IEnumerable<PropertyInfo> GetInjections(Type type) => GetInjections(type, typeof(InjectAttribute));

        /// <inheritdoc />
        public void InjectComponent(Type type) => InjectComponent(type, typeof(InjectAttribute));

        /// <inheritdoc />
        public void InjectComponent(Type type, Type injectAttribute) => GetInjections(type, injectAttribute)
            .ForEach(y =>
                {
                    InjectComponent(y.PropertyType);

                    Services
                        .TryAddSingleton(y.PropertyType,
                            _ => Mocks.GetObject(y.PropertyType) ??
                                 throw new NullReferenceException($"Mock object of {y.PropertyType} cannot be null.")
                        );
                }
            );

        /// <inheritdoc />
        public void InjectComponent<TComponent>() => InjectComponent(typeof(TComponent));

        /// <inheritdoc />
        public void InjectComponent<TComponent, TInjectAttribute>() where TInjectAttribute : Attribute =>
            InjectComponent(typeof(TComponent), typeof(TInjectAttribute));

        /// <inheritdoc />
        public bool IsExists(string cssSelector, bool throwOnNotExist = false)
        {
            var exists = Component?.FindAll(cssSelector).Any() ?? false;

            if (throwOnNotExist)
            {
                throw new ApplicationException($"Component with {cssSelector} selector not found.");
            }

            return exists;
        }

        /// <inheritdoc />
        public IRenderedComponent<T> RenderComponent(bool forceNew = false)
        {
            if (Component == null || forceNew)
            {
                Component = RenderComponent<T>(RenderParameters.ToArray());
            }
            else
            {
                Component.SetParametersAndRender(RenderParameters.ToArray());
            }

            WaitDelay();
            return Component;
        }

        /// <inheritdoc />
        public IRenderedComponent<T> RenderComponent(Action<ComponentParameterCollectionBuilder<T>> parameterBuilder, bool forceNew = false)
        {
            if (Component == null || forceNew)
            {
                Component = base.RenderComponent(parameterBuilder);
            }
            else
            {
                Component.SetParametersAndRender(parameterBuilder);
            }

            WaitDelay();
            return Component;
        }

        /// <inheritdoc />
        public async Task SetAutoComplete(string cssSelector, string filterText, Func<bool> waitFunc,
            string itemCssSelector = ".b-is-autocomplete-suggestion")
        {
            if (Component == null)
            {
                throw new ArgumentNullException(nameof(Component));
            }

            await Component.InvokeAsync(() =>
                {
                    var filterElement = Component.Find(cssSelector);
                    filterElement.Focus();
                    filterElement.Input(filterText);

                    var suggestion = Component.WaitForElement(itemCssSelector, TimeSpan.FromSeconds(2));

                    if (suggestion.TextContent.Contains(filterText, StringComparison.OrdinalIgnoreCase))
                    {
                        suggestion.MouseUp();
                    }

                    WaitForState(waitFunc);
                }
            );
        }

        /// <inheritdoc />
        public void SetElementCheck<TComponent>(string cssSelector, bool isChecked, Func<bool> waitFunc, TimeSpan? waitTimeout = null,
            IRenderedFragment? startingPoint = null) where TComponent : class, IComponent
        {
            IElement? nameFilter = null;

            if (Component == null)
            {
                throw new ArgumentNullException(nameof(Component));
            }

            if (string.IsNullOrWhiteSpace(cssSelector))
            {
                throw new ArgumentNullException(nameof(cssSelector));
            }

            if (startingPoint == null)
            {
                Component.FindComponents<T>()
                    .ForEach(check =>
                        {
                            try
                            {
                                nameFilter = check.Find(cssSelector);
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                    );
            }
            else
            {
                startingPoint.FindComponents<T>()
                    .ForEach(check =>
                        {
                            try
                            {
                                nameFilter = check.Find(cssSelector);
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                    );
            }

            if (nameFilter == null)
            {
                throw new ElementNotFoundException(cssSelector);
            }

            var checkbox = ((Wrapper<IElement>) nameFilter).WrappedElement;
            checkbox.Change(isChecked);
            WaitForState(waitFunc, waitTimeout);
        }

        /// <inheritdoc />
        public void SetElementSwitch<TComponent>(string cssSelector, bool isChecked, Func<bool> waitFunc, TimeSpan? waitTimeout = null,
            IRenderedFragment? startingPoint = null) where TComponent : class, IComponent
        {
            IElement? nameFilter = null;

            if (Component == null)
            {
                throw new ArgumentNullException(nameof(Component));
            }

            if (string.IsNullOrWhiteSpace(cssSelector))
            {
                throw new ArgumentNullException(nameof(cssSelector));
            }

            if (startingPoint == null)
            {
                Component.FindComponents<TComponent>()
                    .ForEach(toggle =>
                        {
                            try
                            {
                                nameFilter = toggle.Find(cssSelector);
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                    );
            }
            else
            {
                startingPoint.FindComponents<TComponent>()
                    .ForEach(toggle =>
                        {
                            try
                            {
                                nameFilter = toggle.Find(cssSelector);
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                    );
            }

            if (nameFilter == null)
            {
                throw new InvalidOperationException($"{cssSelector} not found.");
            }

            var theSwitch = ((Wrapper<IElement>) nameFilter).WrappedElement;
            theSwitch.Change(isChecked);
            WaitForState(waitFunc, waitTimeout);
        }

        /// <inheritdoc />
        public void SetElementText(IElement element, string text, Func<bool> waitFunc, TimeSpan? waitTimeout = null)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.Input(text);
            WaitForState(waitFunc, waitTimeout);
        }

        /// <inheritdoc />
        public void SetElementText(string cssSelector, string text, Func<bool> waitFunc, TimeSpan? waitTimeout = null, IRenderedFragment? startingPoint = null)
        {
            if (string.IsNullOrWhiteSpace(cssSelector))
            {
                throw new ArgumentNullException(nameof(cssSelector));
            }

            if (Component == null)
            {
                throw new ArgumentNullException(nameof(Component));
            }

            if (startingPoint == null)
            {
                if (!(Component.FindAll(cssSelector).Count > 0))
                {
                    WaitForState(() => Component.FindAll(cssSelector).Count > 0);
                }
            }
            else
            {
                if (!(startingPoint.FindAll(cssSelector).Count > 0))
                {
                    WaitForState(() => startingPoint.FindAll(cssSelector).Count > 0);
                }
            }

            var nameFilter = startingPoint == null
                ? Component.Find(cssSelector)
                : startingPoint.Find(cssSelector);

            SetElementText(nameFilter, text, waitFunc, waitTimeout);
        }

        /// <inheritdoc />
        public void WaitDelay(TimeSpan? waitTimeout = null)
        {
            try
            {
                Task.Delay(waitTimeout ?? TimeSpan.FromMilliseconds(100), TokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
        }

        /// <inheritdoc />
        public void WaitForExists(string cssSelector, TimeSpan? waitTimeout = null) => WaitForState(() => IsExists(cssSelector), waitTimeout);

        /// <inheritdoc />
        public void WaitForNotExists(string cssSelector, TimeSpan? waitTimeout = null) => WaitForState(() => !IsExists(cssSelector), waitTimeout);

        /// <inheritdoc />
        public bool WaitForState(Func<bool> waitFunc, TimeSpan? waitTimeout = null)
        {
            if (Component == null)
            {
                throw new ArgumentNullException(nameof(Component));
            }

            Component.WaitForState(waitFunc, waitTimeout ?? TimeSpan.FromSeconds(3));
            return true;
        }

        #endregion
    }
}
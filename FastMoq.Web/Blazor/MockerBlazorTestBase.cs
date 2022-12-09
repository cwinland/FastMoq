using AngleSharp.Dom;
using AngleSharpWrappers;
using Bunit;
using Bunit.Rendering;
using Bunit.TestDoubles;
using FastMoq.Web.Blazor.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections;
using System.Reflection;
using System.Runtime;

namespace FastMoq.Web.Blazor
{
    /// <summary>
    ///     Common methods for all BUnit/XUnit Tests.
    /// </summary>
    /// <typeparam name="T">Type of the component being tested.</typeparam>
    /// <inheritdoc cref="TestContext" />
    /// <inheritdoc cref="IMockerBlazorTestHelpers{T}" />
    public abstract class MockerBlazorTestBase<T> : TestContext, IMockerBlazorTestHelpers<T> where T : ComponentBase
    {
        #region Properties

        /// <summary>
        ///     Gets the authentication context.
        /// </summary>
        /// <value>The authentication context.</value>
        protected TestAuthorizationContext AuthContext { get; }

        /// <summary>
        ///     Gets the authorized policies.
        /// </summary>
        /// <value>The authorized policies.</value>
        protected virtual List<string> AuthorizedPolicies { get; } = new();

        /// <summary>
        ///     Gets the authorized roles.
        /// </summary>
        /// <value>The authorized roles.</value>
        protected virtual List<string> AuthorizedRoles { get; } = new();

        /// <summary>
        ///     Gets or sets the authentication username.
        /// </summary>
        /// <value>The authentication username.</value>
        protected string AuthUsername { get; set; } = "TestUser";

        /// <summary>
        ///     Rendered Component being tested.
        /// </summary>
        /// <value>The component.</value>
        protected IRenderedComponent<T>? Component { get; set; }

        /// <summary>
        ///     Gets the configure services.
        /// </summary>
        /// <value>The configure services.</value>
        protected virtual Action<TestServiceProvider, IConfiguration, Mocker> ConfigureServices { get; } = (_, _, _) => { };

        /// <summary>
        ///     Gets the instance of the rendered component T.
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
        protected virtual List<ComponentParameter> RenderParameters { get; } = new();

        /// <summary>
        ///     Gets the setup component.
        /// </summary>
        /// <value>The setup component.</value>
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
        /// <returns>Dictionary&lt;ComponentBase, ComponentState&gt;.</returns>
        /// <exception cref="System.ArgumentNullException">Component</exception>
        protected internal Dictionary<ComponentBase, ComponentState> GetAllComponents()
        {
            var list = new Dictionary<ComponentBase, ComponentState>();

            var renderer = Component?.Services.GetRequiredService<ITestRenderer>() as Renderer ?? throw new ArgumentNullException(nameof(Component));
            var componentList = renderer.GetFieldValue<IDictionary, Renderer>("_componentStateByComponent");

            if (componentList == null)
            {
                return list;
            }

            foreach (DictionaryEntry obj in componentList)
            {
                if (obj.Key is ComponentBase component)
                {
                    list.Add(component,
                        new ComponentState
                        {
                            ComponentId = (int) obj.Value.GetPropertyValue(nameof(ComponentState.ComponentId)),
                            Component = obj.Value.GetPropertyValue(nameof(ComponentState.Component)) as CascadingAuthenticationState,
                            CurrentRenderTree = obj.Value.GetPropertyValue(nameof(ComponentState.CurrentRenderTree)) as RenderTreeBuilder,
                            ParentComponentState = obj.Value.GetPropertyValue(nameof(ComponentState.ParentComponentState)) as ComponentState
                        }
                    );
                }
            }

            return list;
        }

        protected internal void Setup()
        {
            SetupMocks();
            SetupComponent(Mocks);
            SetupServices();

            SetupAuthorization();

            Component = RenderComponent(true);
        }

        /// <summary>
        ///     Setups the authorization.
        /// </summary>
        protected void SetupAuthorization()
        {
            AuthContext.SetAuthorized(AuthUsername);
            AuthContext.SetPolicies(AuthorizedPolicies.ToArray());
            AuthContext.SetRoles(AuthorizedRoles.ToArray());
        }

        private void SetupMocks() { }

        private void SetupServices()
        {
            IConfiguration configuration = Mocks.GetObject<IConfigurationRoot>() ??
                                           throw new InvalidDataException($"Unable to get {nameof(IConfigurationRoot)} object.");

            // Insert component injections
            InjectComponent<T>();

            ConfigureServices.Invoke(Services, configuration, Mocks);
        }

        #region IMockerBlazorTestHelpers<T>

        /// <inheritdoc />
        /// <exception cref="T:System.ArgumentNullException">button</exception>
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
            where TComponent : IComponent =>
            ButtonClick(startingComponent.Find(cssSelector), waitFunc, waitTimeout);

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        public bool ButtonClick<TComponent>(Func<IRenderedComponent<TComponent>, IElement> cssSelector, Func<bool> waitFunc,
            TimeSpan? waitTimeout = null) where TComponent : IComponent
        {
            if (cssSelector == null)
            {
                throw new ArgumentNullException(nameof(cssSelector));
            }

            if (Component == null)
            {
                throw new ArgumentNullException(nameof(Component));
            }

            return Component.FindComponents<TComponent>()
                .Select(cssSelector)
                .Select(b => ButtonClick(b, waitFunc, waitTimeout))
                .FirstOrDefault();
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        public bool ButtonClick<TComponent>(Func<IRenderedComponent<TComponent>, bool> cssSelector, Func<bool> waitFunc, TimeSpan? waitTimeout = null)
            where TComponent : IComponent
        {
            if (cssSelector == null)
            {
                throw new ArgumentNullException(nameof(cssSelector));
            }

            IRenderedComponent<TComponent>? component = FindComponent(cssSelector);

            return component == null
                ? throw new NotImplementedException($"Unable to find {typeof(TComponent)}")
                : ButtonClick(component.Find("*"), waitFunc, waitTimeout);
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        public bool ButtonClick<TComponent>(string cssSelector, Func<bool> waitFunc, TimeSpan? waitTimeout = null) where TComponent : IComponent
        {
            if (string.IsNullOrWhiteSpace(cssSelector))
            {
                throw new ArgumentNullException(nameof(cssSelector));
            }

            return ButtonClick<TComponent>(c => c.Find(cssSelector), waitFunc, waitTimeout);
        }

        /// <inheritdoc />
        public IRenderedComponent<TComponent> ClickDropdownItem<TComponent>(IRenderedComponent<TComponent> component, string cssSelector,
            string propName,
            Func<bool> waitFunc) where TComponent : IComponent
        {
            ButtonClick(component
                    .FindAll(cssSelector)
                    .First(e => e.InnerHtml == propName),
                waitFunc
            );

            return component;
        }

        /// <inheritdoc />
        public IRenderedComponent<TComponent> ClickDropdownItem<TComponent>(string propName, Func<bool> waitFunc,
            string itemCssSelector = "a.dropdown-item") where TComponent : IComponent
        {
            if (Component == null)
            {
                throw new ArgumentNullException(nameof(Component));
            }

            IRenderedComponent<TComponent> dropDown = Component.FindComponent<TComponent>();
            return ClickDropdownItem(dropDown, itemCssSelector, propName, waitFunc);
        }

        /// <inheritdoc />
        /// <exception cref="System.NotImplementedException">Unable to find {typeof(TComponent)}</exception>
        public IRenderedComponent<TComponent> FindComponent<TComponent>(Func<IRenderedComponent<TComponent>, bool> selector)
            where TComponent : IComponent
        {
            if (Component == null)
            {
                throw new ArgumentNullException(nameof(Component));
            }

            List<IRenderedComponent<TComponent>> componentList = Component.FindComponents<TComponent>().ToList();
            IRenderedComponent<TComponent>? component = componentList.FirstOrDefault(selector);

            return component ?? throw new NotImplementedException($"Unable to find {typeof(TComponent)}");
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException">predicate</exception>
        /// <exception cref="System.ArgumentNullException">Component</exception>
        /// <exception cref="System.Runtime.AmbiguousImplementationException">
        ///     Multiple components of type '{typeof(TComponent)}'
        ///     was found.
        /// </exception>
        public IRenderedComponent<TComponent> GetComponent<TComponent>() where TComponent : class, IComponent => GetComponent<TComponent>(_ => true);

        /// <summary>
        ///     Gets the component.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <param name="predicate">The predicate.</param>
        /// <returns>IRenderedComponent&lt;TComponent&gt;.</returns>
        /// <exception cref="System.ArgumentNullException">predicate</exception>
        /// <exception cref="System.ArgumentNullException">Component</exception>
        /// <exception cref="System.Runtime.AmbiguousImplementationException">
        ///     Multiple components of type '{typeof(TComponent)}'
        ///     was found.
        /// </exception>
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
        /// <exception cref="System.ArgumentNullException">whereFunc</exception>
        /// <exception cref="System.Runtime.AmbiguousImplementationException">
        ///     Multiple components of type '{typeof(TComponent)}'
        ///     was found.
        /// </exception>
        public List<IRenderedComponent<TOfType>> GetComponents<TOfType>(Func<IRenderedComponent<TOfType>, bool>? predicate = null)
            where TOfType : class, IComponent
        {
            IEnumerable<IRenderedComponent<TOfType>> components = GetAllComponents()
                .Where(x => x.Key is TOfType)

                //.Select(x => x.Key)
                //.Select(x => GetComponent<TOfType>(y => y.Instance.GetPropertyValue(propertyName) == x.GetPropertyValue(propertyName)));
                .Select(x => GetComponent<TOfType>(y => y.ComponentId == (int) x.Value.GetPropertyValue("ComponentId")));

            return (predicate == null ? components : components.Where(predicate)).ToList();
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
        /// <exception cref="NullReferenceException">When Mock object is null.</exception>
        public void InjectComponent(Type type) => InjectComponent(type, typeof(InjectAttribute));

        /// <inheritdoc />
        /// <exception cref="NullReferenceException">When Mock object is null.</exception>
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
        /// <exception cref="NullReferenceException">When Mock object is null.</exception>
        public void InjectComponent<TComponent>() => InjectComponent(typeof(TComponent));

        /// <inheritdoc />
        /// <exception cref="NullReferenceException">When Mock object is null.</exception>
        public void InjectComponent<TComponent, TInjectAttribute>() where TInjectAttribute : Attribute =>
            InjectComponent(typeof(TComponent), typeof(TInjectAttribute));

        /// <inheritdoc />
        /// <exception cref="ApplicationException">When throwOnNotExists: Component or Component with cssSelector is not found.</exception>
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
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        /// <exception cref="ElementNotFoundException"></exception>
        public void SetElementCheck<TComponent>(string cssSelector, bool isChecked, Func<bool> waitFunc, TimeSpan? waitTimeout = null,
            IRenderedFragment? startingPoint = null) where TComponent : IComponent
        {
            IElement? nameFilter = null;

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
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        public void SetElementSwitch<TComponent>(string cssSelector, bool isChecked, Func<bool> waitFunc, TimeSpan? waitTimeout = null,
            IRenderedFragment? startingPoint = null) where TComponent : IComponent
        {
            IElement? nameFilter = null;

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

            var theSwitch = ((Wrapper<IElement>) nameFilter).WrappedElement;
            theSwitch.Change(isChecked);
            WaitForState(waitFunc, waitTimeout);
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException">element</exception>
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
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        public void SetElementText(string cssSelector, string text, Func<bool> waitFunc, TimeSpan? waitTimeout = null,
            IRenderedFragment? startingPoint = null)
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

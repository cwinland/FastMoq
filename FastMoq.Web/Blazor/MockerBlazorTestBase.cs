using AngleSharp.Dom;
using AngleSharpWrappers;
using Bunit;
using Bunit.TestDoubles;
using FastMoq.Web.Blazor.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

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

        private void SetupMocks()
        {
        }

        private void SetupServices()
        {
            IConfiguration configuration = Mocks.GetObject<IConfigurationRoot>() ??
                                           throw new InvalidDataException($"Unable to get {nameof(IConfigurationRoot)} object.");

            // Insert component injections
            InjectComponent<T>();

            ConfigureServices?.Invoke(Services, configuration, Mocks);
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
        public bool ButtonClick(string cssSelector, Func<bool> waitFunc, TimeSpan? waitTimeout = null) =>
            ButtonClick(Component.Find(cssSelector), waitFunc, waitTimeout);

        /// <inheritdoc />
        public bool ButtonClick<TComponent>(string cssSelector, Func<bool> waitFunc, IRenderedComponent<TComponent> startingComponent,
            TimeSpan? waitTimeout = null)
            where TComponent : IComponent =>
            ButtonClick(startingComponent.Find(cssSelector), waitFunc, waitTimeout);

        public IRenderedComponent<TComponent> FindComponent<TComponent>(Func<IRenderedComponent<TComponent>, bool> selector) where TComponent : IComponent
        {
            var componentList = Component.FindComponents<TComponent>().ToList();
            var component = componentList.FirstOrDefault(selector);

            return component ?? throw new NotImplementedException($"Unable to find {typeof(TComponent)}");
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        public bool ButtonClick<TComponent>(Func<IRenderedComponent<TComponent>, IElement> cssSelector, Func<bool> waitFunc,
            TimeSpan? waitTimeout = null) where TComponent : IComponent
        {
            if (cssSelector == null)
            {
                throw new ArgumentNullException(nameof(cssSelector));
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

            var component = FindComponent(cssSelector);

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
        public IRenderedComponent<TComponent> ClickDropdownItem<TComponent>(string propName, Func<bool> waitFunc,
            string cssDropdownSelector = "a.dropdown-item")
            where TComponent : IComponent
        {
            IRenderedComponent<TComponent> dropDown = Component.FindComponent<TComponent>();

            ButtonClick(dropDown
                    .FindAll(cssDropdownSelector)
                    .First(e => e.InnerHtml == propName),
                waitFunc
            );

            WaitForState(waitFunc);

            return dropDown;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">predicate</exception>
        public IRenderedComponent<TComponent> GetComponent<TComponent>(Func<IRenderedComponent<TComponent>, bool> predicate)
            where TComponent : IComponent =>
            predicate == null
                ? throw new ArgumentNullException(nameof(predicate))
                : Component.FindComponents<TComponent>().First(predicate);

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
                        .TryAddSingleton(y.PropertyType, _ => Mocks.GetObject(y.PropertyType));
                }
            );

        /// <inheritdoc />
        public void InjectComponent<TComponent>() => InjectComponent(typeof(TComponent));

        /// <inheritdoc />
        public void InjectComponent<TComponent, TInjectAttribute>() where TInjectAttribute : Attribute =>
            InjectComponent(typeof(TComponent), typeof(TInjectAttribute));

        /// <inheritdoc />
        public bool IsExists(string cssSelector) => Component.FindAll(cssSelector).Any();

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
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        public void SetElementText(string cssSelector, string text, Func<bool> waitFunc, TimeSpan? waitTimeout = null,
            IRenderedFragment? startingPoint = null)
        {
            if (string.IsNullOrWhiteSpace(cssSelector))
            {
                throw new ArgumentNullException(nameof(cssSelector));
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

            nameFilter.Input(text);
            WaitForState(waitFunc, waitTimeout);
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
        public void WaitForState(Func<bool> waitFunc, TimeSpan? waitTimeout = null) =>
            Component.WaitForState(waitFunc, waitTimeout ?? TimeSpan.FromSeconds(3));

        #endregion
    }
}

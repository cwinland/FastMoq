using AngleSharp.Dom;
using AngleSharpWrappers;
using Bunit;
using Bunit.Rendering;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;

namespace FastMoq
{
    /// <summary>
    ///     Common methods for all BUnit/XUnit Tests.
    /// </summary>
    /// <typeparam name="TComponent">Type of the component being tested.</typeparam>
    /// <inheritdoc cref="TestContext"/>
    public class BlazorMockerTestBase<TComponent> : TestContext where TComponent : ComponentBase
    {
        #region Properties

        /// <summary>
        ///     Gets the authentication context.
        /// </summary>
        /// <value>The authentication context.</value>
        protected TestAuthorizationContext AuthContext { get; }

        /// <summary>
        ///     Rendered Component being tested.
        /// </summary>
        /// <value>The component.</value>
        protected IRenderedComponent<TComponent>? Component { get; set; }

        /// <summary>
        ///     Gets the mocks.
        /// </summary>
        /// <value>The mocks.</value>
        protected Mocker Mocks { get; } = new();

        /// <summary>
        ///     Gets the instance of the rendered component T.
        /// </summary>
        /// <value>The instance.</value>
        protected TComponent? Instance => Component?.Instance;

        /// <summary>
        ///     Gets or sets the setup mocks action. This action is run before the component is created.
        /// </summary>
        /// <value>The setup mocks action.</value>
        protected virtual Action<Mocker>? SetupMocksAction { get; } = _ => { };

        /// <summary>
        ///     Gets the setup services action.
        /// </summary>
        /// <value>The setup services action.</value>
        protected virtual Action<TestServiceProvider>? SetupServicesAction { get; } = _ => { };
        /// <summary>
        ///     Gets the setup authorization action.
        /// </summary>
        /// <value>The setup authorization action.</value>
        protected virtual Action<TestAuthorizationContext>? SetupAuthorizationAction { get; } = _ => { };

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="BlazorMockerTestBase{TComponent}"/> class.
        /// </summary>
        protected BlazorMockerTestBase()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            AuthContext = this.AddTestAuthorization();
            RenderComponent();
        }

        /// <summary>
        ///     Buttons the click.
        /// </summary>
        /// <param name="button">The button.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns><c>true</c> if success, <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">button</exception>
        protected bool ButtonClick(IElement? button, Func<bool> waitFunc, TimeSpan? waitTimeout = null)
        {
            if (button == null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            try
            {
                button.Click();
                WaitForState(waitFunc, waitTimeout);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Buttons the click.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns><c>true</c> if success, <c>false</c> otherwise.</returns>
        protected bool ButtonClick(string cssSelector, Func<bool> waitFunc, TimeSpan? waitTimeout = null) =>
            ButtonClick(Component?.Find(cssSelector), waitFunc, waitTimeout);

        /// <summary>
        ///     Buttons the click.
        /// </summary>
        /// <typeparam name="TButton">The type of the t button.</typeparam>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="startingComponent">The starting component.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns><c>true</c> if success, <c>false</c> otherwise.</returns>
        protected bool ButtonClick<TButton>(string cssSelector,
            Func<bool> waitFunc,
            IRenderedComponent<TButton> startingComponent,
            TimeSpan? waitTimeout = null) where TButton : IComponent =>
            ButtonClick(startingComponent.Find(cssSelector), waitFunc, waitTimeout);

        /// <summary>
        ///     Buttons the click.
        /// </summary>
        /// <typeparam name="TButton">The type of the t button.</typeparam>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns><c>true</c> if success, <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">cssSelector</exception>
        /// <exception cref="System.ArgumentNullException">Component</exception>
        protected bool ButtonClick<TButton>(Func<IRenderedComponent<TButton>, IElement>? cssSelector, Func<bool> waitFunc,
            TimeSpan? waitTimeout = null)
            where TButton : IComponent
        {
            if (cssSelector == null)
            {
                throw new ArgumentNullException(nameof(cssSelector));
            }

            if (Component == null)
            {
                throw new ArgumentNullException(nameof(Component));
            }

            try
            {
                return Component.FindComponents<TButton>()
                    .Select(cssSelector)
                    .Select(b => ButtonClick(b, waitFunc, waitTimeout))
                    .FirstOrDefault();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Buttons the click.
        /// </summary>
        /// <typeparam name="TButton">The type of the t button.</typeparam>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns><c>true</c> if success, <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">cssSelector</exception>
        /// <exception cref="System.ArgumentNullException">Component</exception>
        protected bool ButtonClick<TButton>(Func<IRenderedComponent<TButton>, bool> cssSelector, Func<bool> waitFunc,
            TimeSpan? waitTimeout = null)
            where TButton : IComponent
        {
            if (cssSelector == null)
            {
                throw new ArgumentNullException(nameof(cssSelector));
            }

            if (Component == null)
            {
                throw new ArgumentNullException(nameof(Component));
            }

            try
            {
                return Component.FindComponents<TButton>()
                    .Where(cssSelector)
                    .Select(x => x.Find("*"))
                    .Select(b => ButtonClick(b, waitFunc, waitTimeout))
                    .FirstOrDefault();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Buttons the click.
        /// </summary>
        /// <typeparam name="TButton">The type of the t button.</typeparam>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns><c>true</c> if success, <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">cssSelector</exception>
        protected bool ButtonClick<TButton>(string cssSelector, Func<bool> waitFunc, TimeSpan? waitTimeout = null)
            where TButton : IComponent
        {
            if (string.IsNullOrWhiteSpace(cssSelector))
            {
                throw new ArgumentNullException(nameof(cssSelector));
            }

            if (string.IsNullOrWhiteSpace(cssSelector))
            {
                throw new ArgumentNullException(nameof(cssSelector));
            }

            try
            {
                return ButtonClick<TButton>(c => c.Find(cssSelector), waitFunc, waitTimeout);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Gets the component.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate">The predicate.</param>
        /// <returns>IRenderedComponent&lt;T&gt;.</returns>
        /// <exception cref="System.ArgumentNullException">predicate</exception>
        /// <exception cref="System.ArgumentNullException">Component</exception>
        protected IRenderedComponent<T> GetComponent<T>(Func<IRenderedComponent<T>, bool> predicate)
            where T : IComponent
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (Component == null)
            {
                throw new ArgumentNullException(nameof(Component));
            }

            return Component.FindComponents<T>().First(predicate);
        }

        /// <summary>
        ///     Determines whether the specified CSS selector is exists.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <returns><c>true</c> if the specified CSS selector is exists; otherwise, <c>false</c>.</returns>
        protected bool IsExists(string cssSelector) => Component?.FindAll(cssSelector).Any() ?? false;

        /// <summary>
        ///     Renders the component. If the component is already rendered, it will act like a stateChanged.
        /// </summary>
        /// <param name="forceNew">if set to <c>true</c> [force new].</param>
        protected void RenderComponent(bool forceNew = false)
        {
            if (Component == null || forceNew)
            {
                SetupMocksAction?.Invoke(Mocks);
                SetupServicesAction?.Invoke(Services);
                SetupAuthorizationAction?.Invoke(AuthContext);

                Component = RenderComponent<TComponent>();
            }
            else
            {
                Component.Render();
            }
        }

        /// <summary>
        ///     Renders the component. If the component is already rendered, it will act like a stateChanged.
        /// </summary>
        /// <param name="parameterBuilder">The parameter builder.</param>
        /// <param name="forceNew">if set to <c>true</c> [force new].</param>
        protected void RenderComponent(Action<ComponentParameterCollectionBuilder<TComponent>> parameterBuilder,
            bool forceNew = false)
        {
            if (Component == null || forceNew)
            {
                SetupMocksAction?.Invoke(Mocks);
                SetupServicesAction?.Invoke(Services);
                SetupAuthorizationAction?.Invoke(AuthContext);

                Component = RenderComponent<TComponent>(parameterBuilder);
            }
            else
            {
                Component.SetParametersAndRender(parameterBuilder);
            }
        }

        /// <summary>
        ///     Sets the element check.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <param name="startingPoint">The starting point.</param>
        /// <exception cref="System.ArgumentNullException">cssSelector</exception>
        /// <exception cref="System.ArgumentNullException">Component</exception>
        protected void SetElementCheck<T>(string cssSelector, bool isChecked, Func<bool> waitFunc,
            TimeSpan? waitTimeout = null, IRenderedFragment? startingPoint = null) where T : ComponentBase
        {
            IElement? nameFilter = null;

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
                Component.FindComponents<T>().ToList()
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
                    });
            }
            else
            {
                startingPoint.FindComponents<T>().ToList()
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
                    });
            }

            var checkbox = nameFilter is Wrapper<IElement> filter && filter.WrappedElement != null
                ? filter.WrappedElement
                : throw new ComponentNotFoundException(typeof(T));

            checkbox.Change(isChecked);
            WaitForState(waitFunc, waitTimeout);
        }

        /// <summary>
        ///     Sets the element switch.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <param name="startingPoint">The starting point.</param>
        /// <exception cref="System.ArgumentNullException">cssSelector</exception>
        /// <exception cref="System.ArgumentNullException">Component</exception>
        protected void SetElementSwitch<T>(string cssSelector, bool isChecked, Func<bool> waitFunc,
            TimeSpan? waitTimeout = null, IRenderedFragment? startingPoint = null) where T : ComponentBase
        {
            IElement? nameFilter = null;

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
                Component.FindComponents<T>().ToList()
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
                    });
            }
            else
            {
                startingPoint.FindComponents<T>().ToList()
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
                    });
            }

            var theSwitch = nameFilter is Wrapper<IElement> filter && filter.WrappedElement != null
                ? filter.WrappedElement
                : throw new ComponentNotFoundException(typeof(T));

            theSwitch.Change(isChecked);
            WaitForState(waitFunc, waitTimeout);
        }

        /// <summary>
        ///     Sets the element text.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="text">The text.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <param name="startingPoint">The starting point.</param>
        /// <exception cref="System.ArgumentNullException">cssSelector</exception>
        /// <exception cref="System.ArgumentNullException">Component</exception>
        protected void SetElementText(string cssSelector, string text, Func<bool> waitFunc,
            TimeSpan? waitTimeout = null, IRenderedFragment? startingPoint = null)
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

            nameFilter.Input(text);
            WaitForState(waitFunc, waitTimeout);
        }

        /// <summary>
        ///     Setups the authorization.
        /// </summary>
        /// <param name="authContext">The authentication context.</param>
        protected virtual void SetupAuthorization(TestAuthorizationContext authContext) { }

        /// <summary>
        ///     Setups the mocks.
        /// </summary>
        /// <param name="mocks">The mocks.</param>
        protected virtual void SetupMocks(Mocker mocks) { }

        /// <summary>
        ///     Setups the services.
        /// </summary>
        /// <param name="services">The services.</param>
        protected virtual void SetupServices(TestServiceProvider services) { }

        /// <summary>
        ///     Waits for exists.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        protected void WaitForExists(string cssSelector, TimeSpan? waitTimeout = null) =>
            WaitForState(() => IsExists(cssSelector), waitTimeout);

        /// <summary>
        ///     Waits for not exists.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        protected void WaitForNotExists(string cssSelector, TimeSpan? waitTimeout = null) =>
            WaitForState(() => !IsExists(cssSelector), waitTimeout);

        /// <summary>
        ///     Waits for state.
        /// </summary>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        protected void WaitForState(Func<bool> waitFunc, TimeSpan? waitTimeout = null) =>
            Component?.WaitForState(waitFunc, waitTimeout ?? TimeSpan.FromSeconds(3));
    }
}
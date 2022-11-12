#if NETCOREAPP3_1_OR_GREATER

using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace FastMoq.Blazor
{
    public interface IMockerBlazorTestHelpers<T> where T : ComponentBase
    {
        /// <summary>
        ///     Buttons the click.
        /// </summary>
        /// <param name="button">The button.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException">button</exception>
        bool ButtonClick(IElement button, Func<bool> waitFunc, TimeSpan? waitTimeout = null);

        /// <summary>
        ///     Buttons the click.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool ButtonClick(string cssSelector, Func<bool> waitFunc, TimeSpan? waitTimeout = null);

        /// <summary>
        ///     Buttons the click.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="startingComponent">The starting component.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool ButtonClick<TComponent>(string cssSelector, Func<bool> waitFunc, IRenderedComponent<TComponent> startingComponent,
            TimeSpan? waitTimeout = null) where TComponent : IComponent;

        /// <summary>
        ///     Buttons the click.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        bool ButtonClick<TComponent>(Func<IRenderedComponent<TComponent>, IElement> cssSelector, Func<bool> waitFunc,
            TimeSpan? waitTimeout = null) where TComponent : IComponent;

        /// <summary>
        ///     Buttons the click.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        bool ButtonClick<TComponent>(Func<IRenderedComponent<TComponent>, bool> cssSelector, Func<bool> waitFunc,
            TimeSpan? waitTimeout = null) where TComponent : IComponent;

        /// <summary>
        ///     Buttons the click.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        bool ButtonClick<TComponent>(string cssSelector, Func<bool> waitFunc, TimeSpan? waitTimeout = null) where TComponent : IComponent;

        /// <summary>
        ///     Clicks the dropdown item.
        /// </summary>
        /// <typeparam name="TKey">The type of the t key.</typeparam>
        /// <typeparam name="TValue">The type of the t value.</typeparam>
        /// <param name="propName">Name of the property.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <returns>IRenderedComponent&lt;DropdownList&lt;TKey, TValue&gt;&gt;.</returns>
        IRenderedComponent<TComponent> ClickDropdownItem<TComponent>(string propName, Func<bool> waitFunc, string cssDropdownSelector = "a.dropdown-item")
            where TComponent : IComponent;

        /// <summary>
        ///     Gets the component.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <param name="predicate">The predicate.</param>
        /// <returns>IRenderedComponent&lt;TComponent&gt;.</returns>
        /// <exception cref="ArgumentNullException">predicate</exception>
        IRenderedComponent<TComponent> GetComponent<TComponent>(Func<IRenderedComponent<TComponent>, bool> predicate)
            where TComponent : IComponent;

        /// <summary>
        ///     Gets the injections.
        /// </summary>
        /// <returns>IEnumerable&lt;PropertyInfo&gt;.</returns>
        IEnumerable<PropertyInfo> GetInjections<TComponent>();

        /// <summary>
        ///     Injects the component.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        void InjectComponent<TComponent>();

        /// <summary>
        ///     Determines whether the specified CSS selector is exists.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <returns><c>true</c> if the specified CSS selector is exists; otherwise, <c>false</c>.</returns>
        bool IsExists(string cssSelector);

        /// <summary>
        ///     Renders the component. If the component is already rendered, it will act like a stateChanged.
        /// </summary>
        /// <param name="forceNew">if set to <c>true</c> [force new].</param>
        IRenderedComponent<T> RenderComponent(bool forceNew = false);

        /// <summary>
        ///     Renders the component. If the component is already rendered, it will act like a stateChanged.
        /// </summary>
        /// <param name="parameterBuilder">The parameter builder.</param>
        /// <param name="forceNew">if set to <c>true</c> [force new].</param>
        IRenderedComponent<T> RenderComponent(Action<ComponentParameterCollectionBuilder<T>> parameterBuilder, bool forceNew = false);

        /// <summary>
        ///     Sets the element check.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <param name="startingPoint">The starting point.</param>
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        /// <exception cref="ElementNotFoundException"></exception>
        void SetElementCheck<TComponent>(string cssSelector, bool isChecked, Func<bool> waitFunc, TimeSpan? waitTimeout = null, IRenderedFragment? startingPoint = null)
            where TComponent : IComponent;

        /// <summary>
        ///     Sets the element switch.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <param name="startingPoint">The starting point.</param>
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        void SetElementSwitch<TComponent>(string cssSelector, bool isChecked, Func<bool> waitFunc, TimeSpan? waitTimeout = null,
            IRenderedFragment? startingPoint = null) where TComponent : IComponent;

        /// <summary>
        ///     Sets the element text.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="text">The text.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <param name="startingPoint">The starting point.</param>
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        void SetElementText(string cssSelector, string text, Func<bool> waitFunc,
            TimeSpan? waitTimeout = null, IRenderedFragment? startingPoint = null);

        /// <summary>
        ///     Waits the delay time. Use only when absolutely needed. Prefer use of WaitForState, WaitForExists, or
        ///     WaitForNotExists.
        /// </summary>
        /// <param name="waitTimeout">The wait timeout.</param>
        void WaitDelay(TimeSpan? waitTimeout = null);

        /// <summary>
        ///     Waits for exists.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        void WaitForExists(string cssSelector, TimeSpan? waitTimeout = null);

        /// <summary>
        ///     Waits for not exists.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        void WaitForNotExists(string cssSelector, TimeSpan? waitTimeout = null);

        /// <summary>
        ///     Waits for state.
        /// </summary>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        void WaitForState(Func<bool> waitFunc, TimeSpan? waitTimeout = null);
    }
}
#endif
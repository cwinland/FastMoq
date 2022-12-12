using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace FastMoq.Web.Blazor.Interfaces
{
    /// <summary>
    ///     Interface IMockerBlazorTestHelpers
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
            TimeSpan? waitTimeout = null) where TComponent : class, IComponent;

        /// <summary>
        ///     Buttons the click.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        bool ButtonClick(Func<IElement, bool> cssSelector, Func<bool> waitFunc, TimeSpan? waitTimeout = null);

        /// <summary>
        ///     Buttons the click.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        bool ButtonClick<TComponent>(string cssSelector, Func<bool> waitFunc, TimeSpan? waitTimeout = null) where TComponent : class, IComponent;

        /// <summary>
        ///     Clicks the dropdown item.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <param name="component">The component.</param>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <returns>IRenderedComponent&lt;TComponent&gt;.</returns>
        IRenderedComponent<TComponent> ClickDropdownItem<TComponent>(IRenderedComponent<TComponent> component, string cssSelector, string propName,
            Func<bool> waitFunc) where TComponent : class, IComponent;

        /// <summary>
        ///     Clicks the dropdown item.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <param name="propName">Name of the property.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="cssDropdownSelector">The CSS dropdown selector.</param>
        /// <returns>IRenderedComponent&lt;DropdownList&lt;TKey, TValue&gt;&gt;.</returns>
        IRenderedComponent<TComponent> ClickDropdownItem<TComponent>(string propName, Func<bool> waitFunc,
            string cssDropdownSelector = "a.dropdown-item")
            where TComponent : class, IComponent;

        /// <summary>
        ///     Finds all by tag.
        /// </summary>
        /// <param name="tagName">Name of the tag.</param>
        /// <returns>IEnumerable&lt;IElement&gt;.</returns>
        public IEnumerable<IElement> FindAllByTag(string tagName);

        /// <summary>
        ///     Finds the by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IElement.</returns>
        public IElement? FindById(string id);

        /// <summary>
        ///     Gets the component.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <returns>IRenderedComponent&lt;TComponent&gt;.</returns>
        /// <example>
        ///     Get FetchData component. /&gt;
        ///     <code language="cs"><![CDATA[
        /// var comp = GetComponent<FetchData>();
        /// ]]></code>
        /// </example>
        IRenderedComponent<TComponent> GetComponent<TComponent>() where TComponent : class, IComponent;

        /// <summary>
        ///     Gets the component.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <param name="predicate">The predicate.</param>
        /// <returns>IRenderedComponent&lt;TComponent&gt;.</returns>
        /// <example>
        ///     Get FetchData component with Id 1234. /&gt;
        ///     <code language="cs"><![CDATA[
        /// var comp = GetComponent<FetchData>(x => x.ComponentId == 1234));
        /// ]]></code>
        /// </example>
        /// <example>
        ///     Get FetchData instance property. /&gt;
        ///     <code language="cs"><![CDATA[
        /// var comp = GetComponent<FetchData>(x => x.Instance.IsRunning));
        /// ]]></code>
        /// </example>
        IRenderedComponent<TComponent> GetComponent<TComponent>(Func<IRenderedComponent<TComponent>, bool> predicate)
            where TComponent : class, IComponent;

        /// <summary>
        ///     Gets the component.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <param name="predicate">The predicate.</param>
        /// <returns>IRenderedComponent&lt;TComponent&gt;.</returns>
        /// <example>
        ///     Get FetchData component with inner html containing text. /&gt;
        ///     <code language="cs"><![CDATA[
        /// var comp = GetComponent<FetchData>(element => element.InnerHtml.Contains("hello"));
        /// ]]></code>
        /// </example>
        IRenderedComponent<TComponent> GetComponent<TComponent>(Func<IElement, bool> predicate) where TComponent : class, IComponent;

        /// <summary>
        ///     Gets the components.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t of type.</typeparam>
        /// <param name="predicate">The where function.</param>
        /// <returns>List&lt;IRenderedComponent&lt;TComponent&gt;&gt;.</returns>
        /// <example>
        ///     Get FetchData component with Id 1234. /&gt;
        ///     <code language="cs"><![CDATA[
        /// var list = GetComponent<FetchData>(x => x.ComponentId == 1234));
        /// ]]></code>
        /// </example>
        /// <example>
        ///     Get FetchData instance property. /&gt;
        ///     <code language="cs"><![CDATA[
        /// var list = GetComponent<FetchData>(x => x.Instance.IsRunning));
        /// ]]></code>
        /// </example>
        List<IRenderedComponent<TComponent>> GetComponents<TComponent>(Func<IRenderedComponent<TComponent>, bool>? predicate = null)
            where TComponent : class, IComponent;

        /// <summary>
        ///     Gets the components.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t of type.</typeparam>
        /// <param name="predicate">The predicate.</param>
        /// <returns>List&lt;IRenderedComponent&lt;TComponent&gt;&gt;.</returns>
        /// <example>
        ///     Get FetchData component with inner html containing text. /&gt;
        ///     <code language="cs"><![CDATA[
        /// var list = GetComponent<FetchData>(element => element.InnerHtml.Contains("hello"));
        /// ]]></code>
        /// </example>
        List<IRenderedComponent<TComponent>> GetComponents<TComponent>(Func<IElement, bool>? predicate = null) where TComponent : class, IComponent;

        /// <summary>
        ///     Gets the injections.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>IEnumerable&lt;PropertyInfo&gt;.</returns>
        IEnumerable<PropertyInfo> GetInjections(Type type);

        /// <summary>
        ///     Gets the injections.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <returns>IEnumerable&lt;PropertyInfo&gt;.</returns>
        IEnumerable<PropertyInfo> GetInjections<TComponent>();

        /// <summary>
        ///     Gets the injections.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="injectAttribute">The inject attribute.</param>
        /// <returns>IEnumerable&lt;PropertyInfo&gt;.</returns>
        IEnumerable<PropertyInfo> GetInjections(Type type, Type injectAttribute);

        /// <summary>
        ///     Injects the component.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="injectAttribute">The inject attribute.</param>
        void InjectComponent(Type type, Type injectAttribute);

        /// <summary>
        ///     Injects the component.
        /// </summary>
        /// <param name="type">The type.</param>
        void InjectComponent(Type type);

        /// <summary>
        ///     Injects the component.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        void InjectComponent<TComponent>();

        /// <summary>
        ///     Injects the component.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <typeparam name="TInjectAttribute">The type of the t inject attribute.</typeparam>
        void InjectComponent<TComponent, TInjectAttribute>() where TInjectAttribute : Attribute;

        /// <summary>
        ///     Determines whether the specified CSS selector is exists.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="throwOnNotExist">if set to <c>true</c> [throw on not exist].</param>
        /// <returns><c>true</c> if the specified CSS selector is exists; otherwise, <c>false</c>.</returns>
        bool IsExists(string cssSelector, bool throwOnNotExist = false);

        /// <summary>
        ///     Renders the component. If the component is already rendered, it will act like a stateChanged.
        /// </summary>
        /// <param name="forceNew">if set to <c>true</c> [force new].</param>
        /// <returns>IRenderedComponent&lt;T&gt;.</returns>
        /// <example>
        ///     Render again without losing context. This honors any parameters in the RenderParameters action. /&gt;
        ///     <code language="cs"><![CDATA[
        /// RenderComponent()
        /// ]]></code>
        /// </example>
        /// <example>
        ///     Force initial render.
        ///     <code language="cs"><![CDATA[
        /// RenderComponent(true);
        /// ]]></code>
        /// </example>
        IRenderedComponent<T> RenderComponent(bool forceNew = false);

        /// <summary>
        ///     Renders the component. If the component is already rendered, it will act like a stateChanged.
        /// </summary>
        /// <param name="parameterBuilder">The parameter builder.</param>
        /// <param name="forceNew">if set to <c>true</c> [force new].</param>
        /// <returns>IRenderedComponent&lt;T&gt;.</returns>
        /// <example>
        ///     Render again with parameters without losing context
        ///     <code language="cs"><![CDATA[
        /// RenderComponent(b => b.Add(x => x.WeatherService, Mocks.GetObject<IWeatherForecastService>()));
        /// ]]></code>
        /// </example>
        /// <example>
        ///     Force initial render with parameters
        ///     <code language="cs"><![CDATA[
        /// RenderComponent(b => b.Add(x => x.WeatherService, Mocks.GetObject<IWeatherForecastService>()), true);
        /// ]]></code>
        /// </example>
        IRenderedComponent<T> RenderComponent(Action<ComponentParameterCollectionBuilder<T>> parameterBuilder, bool forceNew = false);

        /// <summary>
        ///     Sets the automatic complete.
        /// </summary>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="filterText">The filter text.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="itemCssSelector">The item CSS selector.</param>
        /// <returns>Task.</returns>
        Task SetAutoComplete(string cssSelector, string filterText, Func<bool> waitFunc, string itemCssSelector = ".b-is-autocomplete-suggestion");

        /// <summary>
        ///     Sets the element check.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <param name="startingPoint">The starting point.</param>
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        /// <exception cref="ElementNotFoundException"></exception>
        void SetElementCheck<TComponent>(string cssSelector, bool isChecked, Func<bool> waitFunc, TimeSpan? waitTimeout = null,
            IRenderedFragment? startingPoint = null)
            where TComponent : class, IComponent;

        /// <summary>
        ///     Sets the element switch.
        /// </summary>
        /// <typeparam name="TComponent">The type of the t component.</typeparam>
        /// <param name="cssSelector">The CSS selector.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <param name="startingPoint">The starting point.</param>
        /// <exception cref="ArgumentNullException">cssSelector</exception>
        void SetElementSwitch<TComponent>(string cssSelector, bool isChecked, Func<bool> waitFunc, TimeSpan? waitTimeout = null,
            IRenderedFragment? startingPoint = null) where TComponent : class, IComponent;

        /// <summary>
        ///     Sets the element text.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="text">The text.</param>
        /// <param name="waitFunc">The wait function.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        void SetElementText(IElement element, string text, Func<bool> waitFunc, TimeSpan? waitTimeout = null);

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
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool WaitForState(Func<bool> waitFunc, TimeSpan? waitTimeout = null);
    }
}

using Bunit;
using Bunit.Rendering;
using FastMoq.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Collections;
using System.Reflection;
using IComponent = Microsoft.AspNetCore.Components.IComponent;

namespace FastMoq.Web.Blazor.Models
{
    /// <summary>
    /// Typed wrapper around Blazor component render state information.
    /// </summary>
    /// <typeparam name="T">The component type represented by the state wrapper.</typeparam>
    /// <inheritdoc />
    /// <seealso cref="FastMoq.Web.Blazor.Models.ComponentState" />
    public class ComponentState<T> : ComponentState where T : ComponentBase
    {
        #region Properties

        /// <summary>
        ///     Gets the component.
        /// </summary>
        /// <value>The component.</value>
        public new T? Component => base.Component as T;

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComponentState{T}"/> class.
        /// </summary>
        /// <param name="obj">The raw renderer state object.</param>
        /// <param name="renderer">The renderer.</param>
        /// <param name="rootComponent">The rendered root component used for nested component lookups when needed.</param>
        public ComponentState(object? obj, BunitRenderer renderer, IRenderedComponent<IComponent>? rootComponent = null)
            : base(obj, renderer, rootComponent)
        {
            ComponentType = typeof(T);
        }
    }

    /// <summary>
    /// Wrapper around the renderer state used by FastMoq's Blazor helpers.
    /// </summary>
    /// <remarks>
    /// FastMoq uses this wrapper instead of exposing raw renderer internals directly because bUnit 2 changed the shape of the
    /// renderer state it stores for rendered components. Consumers should prefer <see cref="GetOrCreateRenderedComponent(Type)"/>
    /// or <see cref="GetOrCreateRenderedComponent{T}()"/> over reflecting on renderer state objects themselves.
    /// </remarks>
    public class ComponentState
    {
        private readonly BunitRenderer _renderer;
        private readonly IRenderedComponent<IComponent>? _rootComponent;
        private readonly object? _stateObject;

        #region Properties

        /// <summary>
        ///     Gets the component.
        /// </summary>
        /// <value>The component.</value>
        public IComponent? Component { get; internal set; }

        /// <summary>
        ///     Gets the component identifier.
        /// </summary>
        /// <value>The component identifier.</value>
        public int ComponentId { get; internal set; }

        /// <summary>
        ///     Gets the current render tree.
        /// </summary>
        /// <value>The current render tree.</value>
        public RenderTreeBuilder? CurrentRenderTree { get; internal set; }

        /// <summary>
        ///     Gets a value indicating whether this instance is component base.
        /// </summary>
        /// <value><c>true</c> if this instance is component base; otherwise, <c>false</c>.</value>
        public bool IsComponentBase { get; internal set; }

        /// <summary>
        ///     Gets the state of the parent component.
        /// </summary>
        /// <value>The state of the parent component.</value>
        public ComponentState? ParentComponentState { get; internal set; }

        /// <summary>
        ///     Gets or sets the type of the component.
        /// </summary>
        /// <value>The type of the component.</value>
        protected Type ComponentType { get; set; } = typeof(IComponent);

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComponentState" /> class.
        /// </summary>
        /// <param name="obj">The raw renderer state object.</param>
        /// <param name="renderer">The renderer.</param>
        /// <param name="rootComponent">The rendered root component used for nested component lookups when needed.</param>
        /// <exception cref="System.ArgumentNullException">renderer</exception>
        public ComponentState(object? obj, BunitRenderer renderer, IRenderedComponent<IComponent>? rootComponent = null)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _rootComponent = rootComponent;
            _stateObject = obj;

            if (obj == null)
            {
                return;
            }

            var component = obj.GetPropertyValue(nameof(Component)) as IComponent;
            var componentIdValue = obj.GetPropertyValue(nameof(ComponentId));

            if (component == null && componentIdValue == null)
            {
                return;
            }

            var parentState = obj.GetPropertyValue("LogicalParentComponentState") ?? obj.GetPropertyValue(nameof(ParentComponentState));
            ComponentId = int.TryParse(componentIdValue?.ToString() ?? "0", out var id) ? id : 0;
            Component = component;
            CurrentRenderTree = obj.GetPropertyValue(nameof(CurrentRenderTree)) as RenderTreeBuilder;
            ParentComponentState = parentState != null ? new ComponentState(parentState, renderer, rootComponent) : null;
            IsComponentBase = Component?.GetType().IsAssignableTo(typeof(ComponentBase)) ?? false;
            ComponentType = Component?.GetType() ?? typeof(IComponent);
        }

        /// <summary>
        ///     Gets the or create rendered component.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System ofNullable&lt;IRenderedComponent&lt;IComponent&gt;&gt; of the or create rendered component.</returns>
        /// <remarks>
        /// This method understands both direct rendered-component state values and the fallback lookup path required by bUnit 2's
        /// renderer state dictionary.
        /// </remarks>
        public IRenderedComponent<IComponent>? GetOrCreateRenderedComponent(Type type)
        {
            if (_stateObject is IRenderedComponent<IComponent> renderedComponent && Component != null && type.IsAssignableFrom(Component.GetType()))
            {
                return renderedComponent;
            }

            if (_rootComponent == null)
            {
                return null;
            }

            var findComponents = typeof(BunitRenderer).GetRuntimeMethods()
                .First(x => x.Name.Equals("FindComponents", StringComparison.Ordinal) && x.GetParameters().Length == 1);
            var makeMe = findComponents.MakeGenericMethod(type);
            var renderedComponents = makeMe.Invoke(_renderer, new object?[] { _rootComponent }) as IEnumerable;

            if (renderedComponents == null)
            {
                return null;
            }

            foreach (var candidate in renderedComponents)
            {
                var candidateId = candidate.GetPropertyValue(nameof(ComponentId));
                var instance = candidate.GetPropertyValue("Instance");

                if (ReferenceEquals(instance, Component) || Equals(candidateId, ComponentId))
                {
                    return candidate as IRenderedComponent<IComponent>;
                }
            }

            return null;
        }

        /// <summary>
        ///     Gets the or create rendered component.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>System ofNullable&lt;IRenderedComponent&lt;T&gt;&gt; of the or create rendered component.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var state = GetAllComponents<FetchData>().First().Value as ComponentState<FetchData>;
        /// var rendered = state?.GetOrCreateRenderedComponent<FetchData>();
        /// ]]></code>
        /// </example>
        public virtual IRenderedComponent<T>? GetOrCreateRenderedComponent<T>() where T : ComponentBase =>
            GetOrCreateRenderedComponent(typeof(T)) as IRenderedComponent<T>;
    }
}

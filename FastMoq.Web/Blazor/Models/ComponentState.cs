using Bunit;
using Bunit.Rendering;
using FastMoq.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.ComponentModel;
using System.Reflection;
using IComponent = Microsoft.AspNetCore.Components.IComponent;

namespace FastMoq.Web.Blazor.Models
{
    /// <summary>
    ///     Class ComponentState.
    ///     Implements the <see cref="FastMoq.Web.Blazor.Models.ComponentState" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
        /// <param name="obj">The object.</param>
        /// <param name="renderer">The renderer.</param>
        /// <inheritdoc />
        public ComponentState(object? obj, TestRenderer renderer) : base(obj, renderer) => ComponentType = typeof(T);
    }

    /// <summary>
    ///     Class ComponentState.
    ///     Implements the <see cref="FastMoq.Web.Blazor.Models.ComponentState" />
    /// </summary>
    /// <seealso cref="FastMoq.Web.Blazor.Models.ComponentState" />
    public class ComponentState
    {
        /// <summary>
        ///     The renderer
        /// </summary>
        private readonly TestRenderer renderer;

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
        /// <param name="obj">The object.</param>
        /// <param name="renderer">The renderer.</param>
        /// <exception cref="System.ArgumentNullException">renderer</exception>
        /// <exception cref="System.ArgumentNullException">services</exception>
        public ComponentState(object? obj, TestRenderer renderer)
        {
            this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

            if (obj == null ||
                !(obj.GetType().FullName ?? string.Empty).Equals("Microsoft.AspNetCore.Components.Rendering.ComponentState", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var parentState = obj.GetPropertyValue(nameof(ParentComponentState));
            ComponentId = int.TryParse(obj.GetPropertyValue(nameof(ComponentId))?.ToString() ?? "0", out var id) ? id : 0;
            Component = obj.GetPropertyValue(nameof(Component)) as IComponent;
            CurrentRenderTree = obj.GetPropertyValue(nameof(CurrentRenderTree)) as RenderTreeBuilder;
            ParentComponentState = parentState != null ? new ComponentState(parentState, renderer) : null;
            IsComponentBase = Component?.GetType().IsAssignableTo(typeof(ComponentBase)) ?? false;
            ComponentType = typeof(Component);
        }

        /// <summary>
        ///     Gets the or create rendered component.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System ofNullable&lt;IRenderedComponentBase&lt;ComponentBase&gt;&gt; of the or create rendered component.</returns>
        public IRenderedComponentBase<ComponentBase>? GetOrCreateRenderedComponent(Type type)
        {
            var d1 = typeof(TestRenderer).GetRuntimeMethods().First(x => x.Name.StartsWith("GetOrCreateRenderedComponent"));
            var makeMe = d1.MakeGenericMethod(type);
            var d = new Mocker().CreateInstanceNonPublic<RenderTreeFrameDictionary>();
            var args = new object?[] { d, ComponentId, Component };
            return (IRenderedComponentBase<ComponentBase>?)makeMe.Invoke(renderer, args);
        }

        /// <summary>
        ///     Gets the or create rendered component.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>System ofNullable&lt;IRenderedComponentBase&lt;T&gt;&gt; of the or create rendered component.</returns>
        public virtual IRenderedComponentBase<T>? GetOrCreateRenderedComponent<T>() where T : ComponentBase =>
            (IRenderedComponentBase<T>?)GetOrCreateRenderedComponent(typeof(T));
    }
}

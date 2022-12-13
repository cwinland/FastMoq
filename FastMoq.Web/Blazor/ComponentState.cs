using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Reflection;
using IComponent = Microsoft.AspNetCore.Components.IComponent;

namespace FastMoq.Web.Blazor
{
    /// <summary>
    ///     Class ComponentState.
    ///     Implements the <see cref="FastMoq.Web.Blazor.ComponentState" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="FastMoq.Web.Blazor.ComponentState" />
    /// <inheritdoc />
    public class ComponentState<T> : ComponentState where T : ComponentBase
    {
        #region Properties

        /// <summary>
        ///     Gets the component.
        /// </summary>
        /// <value>The component.</value>
        public new T? Component => base.Component as T;

        #endregion

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.Web.Blazor.ComponentState`1" /> class.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="services">The services.</param>
        public ComponentState(object? obj, IServiceProvider services) : base(obj, services) => ComponentType = typeof(T);
    }

    /// <summary>
    ///     Class ComponentState.
    /// </summary>
    public class ComponentState
    {
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

        private IServiceProvider Services { get; }

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComponentState" /> class.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException">services</exception>
#pragma warning disable CS8618
        public ComponentState(object? obj, IServiceProvider services)
#pragma warning restore CS8618
        {
            if (obj == null ||
                !(obj.GetType().FullName ?? string.Empty).Equals("Microsoft.AspNetCore.Components.Rendering.ComponentState", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var parentState = obj.GetPropertyValue(nameof(ParentComponentState));
            ComponentId = int.TryParse(obj.GetPropertyValue(nameof(ComponentId))?.ToString() ?? "0", out var id) ? id : 0;
            Component = obj.GetPropertyValue(nameof(Component)) as IComponent;
            CurrentRenderTree = obj.GetPropertyValue(nameof(CurrentRenderTree)) as RenderTreeBuilder;
            ParentComponentState = parentState != null ? new ComponentState(parentState, services) : null;
            IsComponentBase = Component?.GetType().IsAssignableTo(typeof(ComponentBase)) ?? false;
            Services = services ?? throw new ArgumentNullException(nameof(services));
            ComponentType = typeof(Component);
        }

        /// <summary>
        ///     Gets the or create rendered component.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>IRenderedComponentBase&lt;ComponentBase&gt;.</returns>
        public IRenderedComponentBase<ComponentBase>? GetOrCreateRenderedComponent(Type type)
        {
            var renderer = Services.GetRequiredService<ITestRenderer>() as TestRenderer;
            var d1 = typeof(TestRenderer).GetRuntimeMethods().First(x => x.Name.StartsWith("GetOrCreateRenderedComponent"));
            var makeMe = d1.MakeGenericMethod(type);
            var d = new Mocker().CreateInstanceNonPublic<RenderTreeFrameDictionary>();
            var args = new object?[] {d, ComponentId, Component};
            return (IRenderedComponentBase<ComponentBase>?) makeMe.Invoke(renderer, args);
        }

        /// <summary>
        ///     Creates the rendered component.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>System.Nullable&lt;System.Object&gt;.</returns>
        public virtual IRenderedComponentBase<T>? GetOrCreateRenderedComponent<T>() where T : ComponentBase =>
            (IRenderedComponentBase<T>?) GetOrCreateRenderedComponent(typeof(T));
    }
}

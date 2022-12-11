using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Reflection;

namespace FastMoq.Web.Blazor
{
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
        public IRenderedFragment? RenderedComponent { get; internal set; }
        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComponentState" /> class.
        /// </summary>
        /// <param name="obj">The object.</param>
        public ComponentState(object? obj, IServiceProvider services)
        {
            if (obj == null)
            {
                return;
            }

            var parentState = obj.GetPropertyValue(nameof(ParentComponentState));
            ComponentId = int.TryParse(obj.GetPropertyValue(nameof(ComponentId))?.ToString() ?? "0", out var id) ? id : 0;
            Component = obj.GetPropertyValue(nameof(Component)) as IComponent;
            CurrentRenderTree = obj.GetPropertyValue(nameof(CurrentRenderTree)) as RenderTreeBuilder;
            ParentComponentState = parentState != null ? new ComponentState(parentState, services) : null;
            IsComponentBase = Component?.GetType().IsAssignableTo(typeof(ComponentBase)) ?? false;

            if (IsComponentBase)
            {
                RenderedComponent = CreateRenderedComponent(services) as IRenderedFragment;
            }
        }
        /*
         * // GetOrCreateRenderedComponent<TComponent>(RenderTreeFrameDictionary framesCollection, int componentId, TComponent component)
            var renderer = Component?.Services.GetRequiredService<ITestRenderer>() as TestRenderer ??
                           throw new ArgumentNullException(nameof(Component));
            Mocks.InvokeMethod(typeof(TestRenderer), "GetOrCreateRenderedComponent")
         */
        private object? CreateRenderedComponent(IServiceProvider services)
        {
            var renderer = services.GetRequiredService<ITestRenderer>() as TestRenderer;
            var d1 = typeof(TestRenderer).GetRuntimeMethods().First(x => x.Name.StartsWith("GetOrCreateRenderedComponent"));
            var makeMe = d1.MakeGenericMethod(Component.GetType());
            var d = new Mocker().CreateInstanceNonPublic<RenderTreeFrameDictionary>();
            var o = makeMe.Invoke(renderer, new object?[] {d, ComponentId, Component });

            return o;

            //var d1 = typeof(IRenderedComponent<>).Assembly.GetTypes().First(t=>t.Name.Equals("RenderedComponent`1"));
            //Type[] typeArgs = {type};
            //var makeMe = d1.MakeGenericType(typeArgs);
            //var o = new Mocker().CreateInstanceNonPublic(makeMe, componentId, services);

            //return o;
        }
    }
}

using Microsoft.AspNetCore.Components.Authorization;

namespace FastMoq.Web.Blazor
{
    public class ComponentState
    {
        public CascadingAuthenticationState Component { get; internal set; }
        public Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder CurrentRenderTree { get; internal set; }
        public int ComponentId { get; internal set; }
        public ComponentState ParentComponentState { get; internal set; }
    }
}

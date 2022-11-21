using FastMoq.Web.Blazor;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Routing;
using Xunit;
using Index = FastMoq.Tests.Blazor.Pages.Index;

namespace FastMoq.Tests.Web
{
    public class BlazorNavigationTests : MockerBlazorTestBase<Index>
    {
        [Fact]
        public void Created()
        {
            Component.Should().NotBeNull();
        }

        [Fact]
        public void NavigateTest()
        {
            IsExists("button.btn-primary").Should().BeFalse();
            ButtonClick<NavLink>(component => component.Instance.AdditionalAttributes["href"].ToString() == "counter",
                () => this.IsExists("button.btn-primary")
            );
        }
    }
}

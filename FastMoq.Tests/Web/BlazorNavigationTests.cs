using Bunit.TestDoubles;
using FastMoq.Tests.Blazor.Pages;
using FastMoq.Web.Blazor;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

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
            var manager = Services.GetRequiredService<FakeNavigationManager>();

            IsExists("button").Should().BeTrue();
            ButtonClick("button", () => manager.History.Count > 0);
            
        }
    }
}

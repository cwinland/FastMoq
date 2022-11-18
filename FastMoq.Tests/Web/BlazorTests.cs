using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Bunit;
using FastMoq.Tests.Blazor.Pages;
using FastMoq.Web.Blazor;
using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace FastMoq.Tests.Web
{
    public class BlazorTests : MockerBlazorTestBase<Counter> {
        public BlazorTests() { }

        [Fact]
        public void ComponentCreated()
        {
            Component.Should().NotBeNull();
            Instance.Should().NotBeNull();
            Component.Instance.Should().Be(Instance);
            Component.Markup.Contains("Current count:").Should().BeTrue();

            IsExists("p[role=\"status\"]").Should().BeTrue();
            var status = Component.Find("p[role=\"status\"]");
            status.InnerHtml.Should().Be("Current count: 0");
        }

        [Fact]
        public void ClickCounterButton()
        {
            Instance.currentCount.Should().Be(0);

            Func<IElement> GetStatus = () => Component.Find("p[role=\"status\"]");
            ButtonClick(".btn.btn-primary", () => GetStatus().InnerHtml.Equals("Current count: 1"));
            GetStatus().InnerHtml.Should().Be("Current count: 1");

            ButtonClick(".btn.btn-primary", () => GetStatus().InnerHtml.Equals("Current count: 2"));
            GetStatus().InnerHtml.Should().Be("Current count: 2");

            Instance.currentCount.Should().Be(2);
        }
    }
}

using AngleSharp.Dom;
using FastMoq.Tests.Blazor.Pages;
using System;

#pragma warning disable CS8604 // Possible null reference argument for parameter.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'.
#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable.
#pragma warning disable CS8974 // Converting method group to non-delegate type
#pragma warning disable CS0472 // The result of the expression is always 'value1' since a value of type 'value2' is never equal to 'null' of type 'value3'.

namespace FastMoq.Tests.Web
{
    public class BlazorCounterTests : MockerBlazorTestBase<Counter> {
        public BlazorCounterTests() { }

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
            Instance.FileSystem.Should().NotBeNull();
        }

        [Fact]
        public void ClickCounterButton()
        {
            Instance.currentCount.Should().Be(0);

            Func<IElement> GetStatus = () => Component.Find("p[role=\"status\"]");
            ClickButton(".btn.btn-primary", () => GetStatus().InnerHtml.Equals("Current count: 1"));
            GetStatus().InnerHtml.Should().Be("Current count: 1");

            ClickButton(".btn.btn-primary", () => GetStatus().InnerHtml.Equals("Current count: 2"));
            GetStatus().InnerHtml.Should().Be("Current count: 2");

            Instance.currentCount.Should().Be(2);
        }
    }
}

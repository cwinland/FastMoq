using FastMoq.Tests.Blazor.Pages;
using FastMoq.Tests.Blazor.Shared;
using FastMoq.Web.Blazor.Models;
using System.Linq;

namespace FastMoq.Tests.Web
{
    /// <summary>
    /// End-to-end compatibility tests for the bUnit 2 migration surface used by <see cref="MockerBlazorTestBase{T}"/>.
    /// </summary>
    public class BlazorBunitMigrationTests : MockerBlazorTestBase<OrdersMigrationPage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlazorBunitMigrationTests"/> class.
        /// </summary>
        public BlazorBunitMigrationTests() : base(true)
        {
            AuthUsername = "migration.user";
            RenderParameters.Add((nameof(OrdersMigrationPage.Title), "Queued Orders"));
            RenderParameters.Add((nameof(OrdersMigrationPage.PrimaryQuery), "contoso"));
            RenderParameters.Add((nameof(OrdersMigrationPage.SecondaryQuery), "fabrikam"));
            RenderParameters.Add(("Accent", "Ocean", true));

            Setup();
        }

        /// <summary>
        /// Verifies that direct parameters, cascading parameters, and authorization-backed helpers all apply on initial render.
        /// </summary>
        [Fact]
        public void InitialRender_ShouldApplyRenderParametersAndAuthorizationState()
        {
            Component.Should().NotBeNull();
            Component.Find("h1").TextContent.Should().Be("Queued Orders");
            Component.Find("#current-user").TextContent.Should().Be("migration.user");

            var editors = GetComponents<OrderMigrationEditor>((System.Func<IRenderedComponent<OrderMigrationEditor>, bool>?) null);
            editors.Should().HaveCount(2);
            editors.Select(x => x.Instance.FilterText).Should().ContainInOrder("contoso", "fabrikam");
            editors.Select(x => x.Instance.Accent).Should().OnlyContain(x => x == "Ocean");
        }

        /// <summary>
        /// Verifies that direct parameter updates rerender through the current bUnit 2 parameter pipeline.
        /// </summary>
        [Fact]
        public void RenderComponent_WithBuilder_ShouldRerenderDirectParameters()
        {
            var rerendered = RenderComponent(parameters =>
            {
                parameters.Add(x => x.Title, "Reviewed Orders");
                parameters.Add(x => x.PrimaryQuery, "adatum");
            });

            rerendered.Find("h1").TextContent.Should().Be("Reviewed Orders");

            var primaryEditor = GetComponent<OrderMigrationEditor>(x => x.Instance.EditorId == "primary");
            var secondaryEditor = GetComponent<OrderMigrationEditor>(x => x.Instance.EditorId == "secondary");

            primaryEditor.Instance.FilterText.Should().Be("adatum");
            secondaryEditor.Instance.FilterText.Should().Be("fabrikam");
        }

        /// <summary>
        /// Verifies that changing a stored cascading render parameter forces a compatible rerender and refreshes descendants.
        /// </summary>
        [Fact]
        public void RenderComponent_ShouldRefreshCascadingRenderParameters()
        {
            SetRenderParameter("Accent", "Forest", true);

            var rerendered = RenderComponent();

            rerendered.Find("div.orders-migration-page").GetAttribute("data-accent").Should().Be("Forest");
            rerendered.FindComponents<OrderMigrationEditor>()
                .Select(x => x.Instance.Accent)
                .Should()
                .OnlyContain(x => x == "Forest");
        }

        /// <summary>
        /// Verifies that nested starting-point helpers scope element edits to the selected rendered component.
        /// </summary>
        [Fact]
        public void StartingPointHelpers_ShouldScopeNestedEditsToSelectedChild()
        {
            var component = Component;
            component.Should().NotBeNull();

            var editors = component!.FindComponents<OrderMigrationEditor>();
            var primaryEditor = editors.Single(x => x.Instance.EditorId == "primary");
            var secondaryEditor = editors.Single(x => x.Instance.EditorId == "secondary");

            SetElementText(
                "input.order-filter",
                "approved",
                () => secondaryEditor.Instance.FilterText == "approved",
                startingPoint: secondaryEditor);

            SetElementCheck<OrderMigrationEditor>(
                "input.include-archived",
                true,
                () => secondaryEditor.Instance.IncludeArchived,
                startingPoint: secondaryEditor);

            primaryEditor.Instance.FilterText.Should().Be("contoso");
            primaryEditor.Instance.IncludeArchived.Should().BeFalse();
            secondaryEditor.Instance.FilterText.Should().Be("approved");
            secondaryEditor.Instance.IncludeArchived.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that the compatibility navigation wrapper still captures page-level navigation flows.
        /// </summary>
        [Fact]
        public void ClickButton_ShouldDriveNavigationHistoryInMigrationScenario()
        {
            ClickButton("#review-orders", () => NavigationManager.History.Count == 1);

            NavigationManager.History.Should().HaveCount(1);
            NavigationManager.Uri.Should().Contain("/orders/review");
            NavigationManager.Uri.Should().Contain("primary=contoso");
        }

        private void SetRenderParameter(string name, object? value, bool isCascadingValue = false)
        {
            var existingIndex = RenderParameters.FindIndex(parameter => parameter.Name == name);
            var parameter = new RenderParameter(name, value, isCascadingValue);

            if (existingIndex >= 0)
            {
                RenderParameters[existingIndex] = parameter;
                return;
            }

            RenderParameters.Add(parameter);
        }
    }
}
# bUnit And Blazor Test Migration

This page is the dedicated migration guide for `FastMoq.Web` component tests that were written against older bUnit assumptions and now need to run on the current bUnit 2.x line.

Use this page when the migration churn is in `MockerBlazorTestBase<T>`, nested rendered-component helpers, render-parameter setup, authorization helpers, or navigation assertions.

## What changed

FastMoq.Web now uses bUnit 2.7.2. The Blazor helper layer was updated to match bUnit 2 API, renderer, and test-double changes without forcing a full rewrite of existing FastMoq-based component tests.

The important changes for consumers are:

- `bunit` moved from `1.38.5` to `2.7.2`
- `MockerBlazorTestBase<T>` now runs on top of `BunitContext`
- `RenderParameters` now stores `FastMoq.Web.Blazor.Models.RenderParameter` because bUnit 2 no longer exposes the old `ComponentParameter` type to consumers
- `SetElementCheck(...)`, `SetElementSwitch(...)`, and `SetElementText(...)` now use `IRenderedComponent<IComponent>?` for `startingPoint` because bUnit 2 removed `IRenderedFragment`
- compatibility wrappers keep the older helper names available for the most common service-provider, authorization, and navigation patterns

## Fast migration map

| Older test shape | Current FastMoq / bUnit 2 shape | Notes |
| --- | --- | --- |
| `TestContext` for direct bUnit tests | `BunitContext` | FastMoq consumers should normally stay on `MockerBlazorTestBase<T>` instead of switching to direct context management. |
| `ComponentParameter` | `RenderParameter` | Use `RenderParameter.Create(...)`, `RenderParameter.CreateCascading(...)`, or tuple conversion. |
| `IRenderedFragment` for nested helper starting points | `IRenderedComponent<IComponent>` | Pass the rendered child component returned by `GetComponent<TChild>(...)` or `GetComponents<TChild>(...)`. |
| `TestServiceProvider`, `TestAuthorizationContext`, `FakeNavigationManager` from old bUnit packages | FastMoq compatibility wrappers with the same names | Existing test code can usually keep the same names with lower source churn. |
| Raw assumptions about renderer `ComponentState` objects | Use `GetComponent(...)`, `GetComponents(...)`, and `ComponentState.GetOrCreateRenderedComponent(...)` | bUnit 2 changed the renderer state shape. Avoid rolling your own reflection unless you have to. |

## Recommended migration order

1. Upgrade the package reference and get the test project compiling.
1. Keep existing `MockerBlazorTestBase<T>` usage unless you are deliberately moving away from FastMoq's Blazor helpers.
1. Replace any old `ComponentParameter` usage with `RenderParameter`.
1. Update helper calls that pass nested starting points so they use `IRenderedComponent<IComponent>?`.
1. Rerun the web test project immediately after each helper migration batch.
1. Only after the suite is green, decide whether any compatibility wrappers should be replaced by direct bUnit 2 APIs.

That order keeps the upgrade mechanical. It avoids mixing a dependency migration with a broader test-style rewrite.

## Breaking changes to expect

### 1. `RenderParameters` now uses `RenderParameter`

Old bUnit-era tests could build or store `ComponentParameter` values directly. That type is no longer public in the current bUnit line, so FastMoq now owns the migration-safe parameter type.

Before:

```csharp
protected override List<ComponentParameter> RenderParameters { get; } = new()
{
    new ComponentParameter(nameof(OrdersPage.Title), "Queued Orders"),
};
```

After:

```csharp
protected override List<RenderParameter> RenderParameters { get; } = new()
{
    RenderParameter.Create(nameof(OrdersPage.Title), "Queued Orders"),
    RenderParameter.CreateCascading("Accent", "Ocean"),
};
```

Tuple conversions also work when you want terser setup:

```csharp
RenderParameters.Add((nameof(OrdersPage.Title), "Queued Orders"));
RenderParameters.Add(("Accent", "Ocean", true));
```

### 2. Nested helper starting points now use rendered components

If a test targets a nested component when calling `SetElementText(...)`, `SetElementCheck(...)`, or `SetElementSwitch(...)`, the starting point should now be a rendered child component.

Before:

```csharp
var editorFragment = Component.FindComponent<OrderEditor>();

SetElementText("input.order-filter", "approved", waitFunc, startingPoint: editorFragment);
```

After:

```csharp
var editor = GetComponent<OrderEditor>(x => x.Instance.EditorId == "secondary");

SetElementText(
    "input.order-filter",
    "approved",
    waitFunc,
    startingPoint: editor);
```

This is the preferred current pattern because the helper now scopes its lookup work to a real rendered component instance instead of a removed fragment abstraction.

### 3. Direct bUnit examples should use `BunitContext`

If you maintain documentation or helper code that shows direct bUnit usage without FastMoq, update the context type:

Before:

```csharp
public class CounterComponentTests : TestContext
{
    [Fact]
    public void Counter_ShouldIncrement_WhenButtonClicked()
    {
        var component = RenderComponent<Counter>();
        component.Find("button").Click();
    }
}
```

After:

```csharp
public class CounterComponentTests : BunitContext
{
    [Fact]
    public void Counter_ShouldIncrement_WhenButtonClicked()
    {
        var component = Render<Counter>();
        component.Find("button").Click();
    }
}
```

FastMoq tests should usually not need this change directly because `MockerBlazorTestBase<T>` already absorbed the context migration.

## Real-world FastMoq example

The repo now carries a migration-focused example that covers:

- direct parameters through `RenderParameter`
- cascading parameters through `RenderParameter.CreateCascading(...)`
- nested component targeting through `IRenderedComponent<IComponent>`
- authorization state from `AddTestAuthorization()`
- navigation assertions through `FakeNavigationManager`

The example lives in the web test project and companion sample components:

- `FastMoq.Tests.Web/BlazorBunitMigrationTests.cs`
- `FastMoq.Tests.Blazor/Pages/OrdersMigrationPage.razor`
- `FastMoq.Tests.Blazor/Shared/OrderMigrationEditor.razor`

The pattern looks like this:

```csharp
public class OrdersMigrationPageTests : MockerBlazorTestBase<OrdersMigrationPage>
{
    public OrdersMigrationPageTests() : base(true)
    {
        AuthUsername = "migration.user";
        RenderParameters.Add((nameof(OrdersMigrationPage.Title), "Queued Orders"));
        RenderParameters.Add((nameof(OrdersMigrationPage.PrimaryQuery), "contoso"));
        RenderParameters.Add((nameof(OrdersMigrationPage.SecondaryQuery), "fabrikam"));
        RenderParameters.Add(("Accent", "Ocean", true));

        Setup();
    }

    [Fact]
    public void SecondaryEditor_CanBeScopedByRenderedComponent()
    {
        var secondaryEditor = GetComponent<OrderMigrationEditor>(x => x.Instance.EditorId == "secondary");

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

        secondaryEditor.Instance.FilterText.Should().Be("approved");
        secondaryEditor.Instance.IncludeArchived.Should().BeTrue();
    }
}
```

This is a good migration target because it keeps the old FastMoq testing ergonomics while aligning the underlying bUnit behavior with the current package line.

## Compatibility wrappers you can keep using

FastMoq now ships compatibility wrappers so common test code does not have to rename every bUnit helper in one pass.

Current compatibility surfaces include:

- `TestServiceProvider`
- `TestAuthorizationContext`
- `FakeNavigationManager`
- `AddTestAuthorization(this BunitContext)`

Keep those names when the goal is a low-risk migration. Replace them with raw bUnit types later only if that improves clarity for your suite.

## Rendering and rerendering notes

`MockerBlazorTestBase<T>` now supports two important rerender paths:

- direct parameter rerendering when only direct parameters changed
- full rerender fallback when one of the configured parameters is a cascading value

That means the following patterns are both valid:

```csharp
RenderComponent(parameters => parameters.Add(x => x.Title, "Reviewed Orders"));
```

```csharp
RenderParameters.RemoveAll(x => x.Name == "Accent");
RenderParameters.Add(RenderParameter.CreateCascading("Accent", "Forest"));

RenderComponent();
```

Use the first form for ordinary parameter updates. Use the second when the test intentionally changes the stored migration-time parameter set, including cascading values.

## Recommended validation

For FastMoq's own repo, the minimum validation for this upgrade is:

```powershell
dotnet test .\FastMoq.Tests.Web\FastMoq.Tests.Web.csproj -c Debug
```

For a full branch validation pass, run the broader solution tests after the web project is green:

```powershell
dotnet test .\FastMoq.sln -c Debug
```

## Related docs

- [Migration guide front door](./README.md)
- [Framework and web helper migration](./framework-and-web-helpers.md)
- [Breaking changes](../breaking-changes/README.md)
- [What's new since 3.0.0](../whats-new/README.md)

# Framework And Web Helper Migration

This page collects the migration guidance that tends to live in shared helpers, framework-specific test plumbing, and `FastMoq.Web` setup rather than in individual test methods.

Open this page when the highest-churn migration work is in base classes, controller helpers, principals, `HttpContext`, keyed DI, or service-provider shims.

If the migration churn is specifically in Blazor component tests, `MockerBlazorTestBase<T>`, `RenderParameters`, nested component targeting, or bUnit package fallout, use [bUnit and Blazor test migration](./bunit-and-blazor-testing.md) first and come back here for the broader web-helper and framework-helper cleanup.

## Shared test helpers first

Before rewriting leaf tests, inspect shared base classes, helper wrappers, and test utilities first.

In mature suites, the highest-leverage migration changes often live there rather than in individual test methods.

Start with helpers that still centralize any of these patterns:

- `.Object` access on raw `Mock<T>` values
- `.Reset()` calls on provider-specific mocks
- `Func<Times>` or raw `Times` helper signatures
- test-framework output helpers passed through shared constructor-check or diagnostic helper wrappers
- local wrappers around principals, controller contexts, or request setup
- framework service-provider shims such as `InstanceServices`, `IServiceProvider`, or similar test bootstrap plumbing

One deliberate helper migration can remove the same churn from dozens of files.

### Keyed services: keep separate doubles when selection matters

If the constructor under test takes the same interface more than once and those parameters are distinguished by DI service keys, one unkeyed `GetMock<T>()`, `GetOrCreateMock<T>()`, or `AddType<T>()` can make the migration look correct while hiding a real behavior difference.

The suite passes, but it can no longer catch a public or private or primary or secondary swap because both production dependencies were collapsed into one double.

Migration rule:

- keep ordinary unit tests ordinary
- but if dependency selection is part of the behavior, use two distinct doubles
- in FastMoq, the preferred keyed options are `GetOrCreateMock<T>(new MockRequestOptions { ServiceKey = ... })`, `AddKeyedType(...)`, and `GetKeyedObject<T>()`
- explicit constructor injection with two separate doubles is equally valid when that is clearer
- add one small wiring-focused test only if you also want coverage that the keyed DI contract itself is honored

For the fuller keyed example, see [Testing Guide: Keyed Services And Same-Type Dependencies](../getting-started/testing-guide.md#keyed-services-and-same-type-dependencies).

### Azure Functions worker: typed `InstanceServices`

Azure Functions worker tests deserve one explicit warning: `FunctionContext.InstanceServices` should behave like a typed `IServiceProvider`, not like a shim that returns the same object for every requested service type.

If the suite uses Azure Functions worker helpers, prefer the built-in typed helper path:

```csharp
using FastMoq.AzureFunctions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

Mocks.AddFunctionContextInstanceServices(services =>
{
    services.AddSingleton(new WidgetClock());
});

var context = Mocks.GetObject<FunctionContext>();
var clock = context.InstanceServices.GetRequiredService<WidgetClock>();
```

Package note:

- if the test project references the aggregate `FastMoq` package, these Azure Functions helpers are already included
- if the test project references `FastMoq.Core` directly, add `FastMoq.AzureFunctions` and import `FastMoq.AzureFunctions.Extensions` before using `CreateFunctionContextInstanceServices(...)` or `AddFunctionContextInstanceServices(...)`
- the generic typed `IServiceProvider` helpers stay in `FastMoq.Core`

If a suite already has a local Azure helper wrapper, re-point that wrapper to `CreateFunctionContextInstanceServices(...)` or `AddFunctionContextInstanceServices(...)` first and keep the existing call sites stable until the suite is green.

Avoid helpers that return one object such as `ILoggerFactory` for every `GetService(Type)` request. Those shims can let the suite keep running while hiding cast failures for typed services such as `IOptions<WorkerOptions>`.

### Typed `IServiceProvider` helpers

The same rule applies outside Azure Functions.

If a base class or helper still does this:

```csharp
var provider = Mocks.GetOrCreateMock<IServiceProvider>();
provider.Setup(x => x.GetService(It.IsAny<Type>())).Returns(someSharedObject);
```

replace it with a typed provider shape:

```csharp
Mocks.AddServiceProvider(services =>
{
    services.AddLogging();
    services.AddOptions();
    services.AddSingleton(new WidgetClock());
});
```

That migration is usually the highest-leverage cleanup in suites that centralize framework bootstrap inside a shared helper. One helper rewrite removes a noisy anti-pattern from many tests at once.

Analyzer note:

- `FMOQ0013` warns on direct FastMoq `IServiceProvider` mock setup so those helpers move toward `CreateTypedServiceProvider(...)` or `AddServiceProvider(...)`.

### Temporary compatibility cleanup in shared helpers

When you are already touching shared framework helpers, treat these as high-priority cleanup targets:

- `GetMock<IServiceProvider>()` or `GetOrCreateMock<IServiceProvider>()` used to emulate typed DI
- context-aware compatibility `AddType(...)` overloads that are really framework-type customizations
- local `FunctionContext.InstanceServices` wrappers that do not build a real provider

Those patterns are still supported to keep v4 migrations moving, but they are the exact spots where a small helper rewrite gives the biggest long-term payback.

### Test-framework output helpers: keep the adapter local

If a shared helper still forwards a test-framework output object directly into FastMoq constructor-check helpers, keep the framework adapter local and pass a neutral line-writer callback into FastMoq instead.

Before:

```csharp
action.EnsureNullCheckThrown(parameterName, constructorName, output);
```

After:

```csharp
action.EnsureNullCheckThrown(parameterName, constructorName, output.WriteLine);
```

Why this matters:

- the `FastMoq.Core` surface for this path is now framework-neutral
- the helper migration is usually one local signature fix rather than a rewrite of every leaf test
- the same shape works with xUnit, NUnit, MSTest, or an in-memory list collector because FastMoq only needs `Action<string>`

Recommended helper shape:

- keep the test-framework output type in the test project if that project wants it
- adapt it to `Action<string>` at the helper boundary before calling FastMoq
- if the helper only used output for occasional diagnostics, prefer dropping the callback entirely instead of preserving framework-specific plumbing forever

## Web test helpers

For controller tests, request-driven tests, and `IHttpContextAccessor`-driven tests, prefer the `FastMoq.Web` helpers instead of continuing to hand-roll local request and principal setup.

For Blazor component tests, prefer the dedicated [bUnit and Blazor test migration](./bunit-and-blazor-testing.md) guide. This page stays focused on HTTP, controller, principal, and general framework-helper migration rather than component-rendering API changes.

Package note:

- if the test project references the aggregate `FastMoq` package, these helpers are already available
- if the test project references `FastMoq.Core` directly, add `FastMoq.Web` before migrating local controller, principal, or `HttpContext` helpers to the built-in web helper surface
- for the full package-choice overview, see [Getting Started package choices](../getting-started/README.md#package-choices)

The main helpers to look for during migration are:

- `CreateHttpContext(...)`
- `CreateControllerContext(...)`
- `SetupClaimsPrincipal(...)`
- `AddHttpContext(...)`
- `AddHttpContextAccessor(...)`

These helpers usually replace a large amount of repetitive local test setup. In many migrated suites, existing repo-local helpers become thin wrappers over `FastMoq.Web` first, and only later get simplified or removed.

### Principal decision table

| Test shape | Preferred helper | Notes |
| --- | --- | --- |
| Role-only test user | `SetupClaimsPrincipal(params roles)` or `CreateControllerContext(params roles)` | Best default when the test only cares about authenticated roles and the compatibility defaults are acceptable. |
| Explicit custom claims | `SetupClaimsPrincipal(claims, options)` | Use `IncludeDefaultIdentityClaims = false` when exact claim preservation matters more than compatibility backfilling. |
| Controller test reading `ControllerContext.HttpContext.User` | `CreateControllerContext(...)` | Prefer assigning `ControllerContext` directly instead of creating a principal separately and wiring `HttpContext.User` by hand. |

### Important custom-claims note

Custom-claim scenarios can behave differently from role-only helpers.

`FastMoq.Web` adds compatibility defaults for identity-style claims unless told not to. That means role-only or convenience overloads are useful for common tests, but exact-claim tests should be explicit about the options they want.

If a test must preserve the exact incoming claims for `ClaimTypes.Name`, `ClaimTypes.Email`, or related identity values, disable compatibility backfilling:

```csharp
var principal = Mocks.SetupClaimsPrincipal(
    claims,
    new TestClaimsPrincipalOptions
    {
        IncludeDefaultIdentityClaims = false,
    });
```

### Preferred v4 MVC controller pattern

When the controller reads from `ControllerContext.HttpContext.User`, prefer assigning the controller context during component creation.

```csharp
using FastMoq.Web.Extensions;

public class OrdersControllerTests : MockerTestBase<OrdersController>
{
    private HttpContext requestContext = default!;

    protected override Action<Mocker>? SetupMocksAction => mocker =>
    {
        requestContext = mocker
            .CreateHttpContext("Admin")
            .SetRequestHeader("X-Correlation-Id", "corr-123")
            .SetQueryParameter("includeInactive", "true");

        mocker.AddHttpContextAccessor(requestContext);
        mocker.AddHttpContext(requestContext);
    };

    protected override Action<OrdersController> CreatedComponentAction => controller =>
    {
        controller.ControllerContext = Mocks.CreateControllerContext(requestContext);
    };
}
```

That is the preferred v4 migration target for older controller helpers that manually created a `DefaultHttpContext`, assigned `User`, and then copied that state into both DI and `ControllerContext`.

### Preferred custom-claims pattern

When the test depends on specific `ClaimTypes.Name`, email, or other identity semantics, build the principal explicitly and keep compatibility defaults off when exact preservation matters.

```csharp
using System.Security.Claims;
using FastMoq.Web;
using FastMoq.Web.Extensions;

public class OrdersControllerClaimTests : MockerTestBase<OrdersController>
{
    protected override Action<OrdersController> CreatedComponentAction => controller =>
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Adele Vance"),
            new Claim(ClaimTypes.Email, "adele.vance@microsoft.com"),
            new Claim(ClaimTypes.Role, "Admin"),
        };

        controller.ControllerContext = Mocks.CreateControllerContext(
            claims,
            new TestClaimsPrincipalOptions
            {
                IncludeDefaultIdentityClaims = false,
            });
    };
}
```

This avoids the common migration mistake of assuming the role-only overloads and the custom-claims overloads behave identically.

### Migrating local web helpers

If a suite already has local `SetupClaimsPrincipal(...)`, `CreateControllerContext(...)`, or similar helpers, the recommended migration path is:

1. Re-point the local wrapper to `FastMoq.Web` first.
1. Keep existing call sites stable until the suite is green.
1. Simplify or remove the wrapper later only if the direct FastMoq call sites are actually clearer.

That approach keeps the migration low-risk. After v4 migration, those local helpers often become thin wrappers rather than remaining hand-rolled infrastructure.

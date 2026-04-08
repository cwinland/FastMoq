# Migration Guide: 3.0.0 To Current Repo

This guide is for maintainers and early adopters working from the last public `3.0.0` release toward the current repository behavior.

It is intentionally practical. The goal is not to list every internal refactor, but to show which test-authoring habits should change and which compatibility APIs still exist only as bridges.

## What good looks like

The goal is provider-centric tests by default, not removal of all provider-specific APIs.

If you only open one document to start a migration, open this one first.

For a successful v4 migration, use this boundary:

- move default arrangements and common verifications toward `GetOrCreateMock<T>()`, `GetObject<T>()`, `Verify(...)`, `VerifyNoOtherCalls(...)`, and `VerifyLogged(...)` where that makes the test clearer
- keep Moq-specific access only where the test still depends on Moq-only semantics such as `SetupSet`, `SetupAllProperties`, `Protected()`, or `out` / `ref` verification patterns, and prefer `GetOrCreateMock<T>()` plus `AsMoq()` or provider-package extensions before falling back to obsolete `GetMock<T>()`
- treat those Moq escape hatches as expected migration exceptions rather than as proof that the migration failed

## Start here for migration

Use this page as the migration front door, then open the next document only when you hit that specific question.

Recommended reading order:

1. This guide: you are here. Read this first for the migration boundary, the v3-to-v4 API ladder, and the practical replacement rules.
1. [Provider Selection and Setup](../getting-started/provider-selection.md): open this next when the suite still uses provider-specific APIs or you need to understand why a package install did not change runtime behavior.
1. [Provider Capabilities](../getting-started/provider-capabilities.md): open this when the question is "does this provider support the test shape I have?"
1. [Executable Testing Examples](../samples/testing-examples.md): open this when you want repo-backed examples of the preferred target style.
1. [Testing Guide](../getting-started/testing-guide.md): open this for deeper reference material once you are actively rewriting tests.

Quick routing:

- If the migration fails because Moq-style tests no longer behave the same, go to [Provider Selection and Setup](../getting-started/provider-selection.md).
- If you are unsure whether `moq`, `nsubstitute`, or `reflection` supports a feature, go to [Provider Capabilities](../getting-started/provider-capabilities.md).
- If you want to see what "good" migrated tests look like, go to [Executable Testing Examples](../samples/testing-examples.md).
- If you are replacing a specific API or helper and need broader context, go to [Testing Guide](../getting-started/testing-guide.md).
- Copilot prompt templates for this migration flow are collected in [Copilot migration prompts](./copilot-prompts.md).

## Scope

- Public release baseline: `3.0.0`
- Release date: May 12, 2025
- Baseline commit: `4035d0d`

This is migration guidance for the current v4 release line. Some references point to repository-backed examples and current documentation structure, but the migration behavior described here is intended to match the published v4 package surface.

## Migration summary

If you are moving tests forward from the public `3.0.0` package or from pre-v4 FastMoq assumptions, the main changes are:

1. Prefer provider-neutral APIs for new migration work, and treat obsolete compatibility APIs as migration targets rather than the intended end state.
2. Treat `AddType(...)` as an explicit type-resolution override, not as a general substitute for mocks.
3. Treat `Strict` as compatibility-only. Use `Behavior` or preset helpers when you mean broader behavior changes.
4. Prefer the newer provider-first retrieval and verification surfaces when you do not specifically need raw Moq APIs.
5. Use the executable examples in `FastMoq.TestingExample` as the best repo-backed reference for current patterns.
6. Treat DbContext helpers as an optional database-package concern when consuming `FastMoq.Core` directly.

## Recommended API ladder

To keep the migration path easy to follow, treat the public testing surface as a three-step ladder instead of a set of competing APIs.

### v3 baseline

- `GetMock<T>()`
- direct `Moq.Mock<T>` setup and verification
- `VerifyLogger(...)`

This is the old Moq-first shape.

### v4 transition

- replace existing `GetMock<T>()` calls by default when you are already touching the test, and keep them only when the goal is minimal churn from v3 and you are intentionally accepting a legacy compatibility API that will be removed in v5
- use `GetOrCreateMock<T>()` for the provider-first tracked mock handle
- use provider-package extensions such as `AsMoq()`, direct `Setup(...)` on `IFastMock<T>`, or `AsNSubstitute()` when a test still needs provider-specific arrangement behavior
- use provider-neutral verification such as `Verify(...)`, `VerifyNoOtherCalls(...)`, `VerifyLogged(...)`, and `TimesSpec`

This is the preferred v4 migration story because it lets old Moq-shaped tests stay stable while giving touched tests a forward-compatible path.

### v5 direction

- `FastMoq.Core` stays provider agnostic
- provider packages are installed explicitly
- provider-specific setup continues to live in provider packages, not in core wrappers

That means new transition surfaces should build on `IFastMock<T>` plus provider-package extensions rather than introducing a separate wrapper layer in core.

## Migration decision table

| Use this | When it is the right fit | Notes |
| --- | --- | --- |
| `GetOrCreateMock<T>()` | Default setup path for tracked mocks and most interaction verification | Preferred v4 migration target for touched tests |
| `GetObject<T>()` | You need the constructed dependency instance, not the tracked wrapper | Useful when the dependency is only consumed as an object during arrange or manual construction |
| `Mocks.Verify(...)` / `VerifyLogged(...)` | The assertion can be expressed without provider-specific verification APIs | Preferred verification surface for migrated tests |
| `GetMock<T>()` | Only when you are preserving a stable v3-shaped Moq test with minimal churn during the first stabilization pass | Obsolete compatibility path; recommended default is to replace it with `GetOrCreateMock<T>()` plus v4 provider/package APIs once the suite is stable |
| `AddType(...)` | A fake, stub, factory, or fixed instance reads better than a mock setup chain | Type-resolution override, not a mock-retrieval substitute |

In this guide, "previous FastMoq versions" means tests and helpers written against the public `3.0.0` package or against pre-v4 assumptions, especially code that assumes:

- Moq is the implicit default surface
- `Strict` is a broader profile switch
- compatibility APIs such as `Initialize<T>(...)`, `VerifyLogger(...)`, or `MockOptional` are still the normal path

## Choose a migration path

If you are taking over an existing suite, there are two valid v4 paths.

### Option 1: Stabilize first

Use this when the immediate goal is to get the upgraded suite passing with minimal churn.

Typical approach:

1. Upgrade to v4.
2. Run the test suite without rewriting test code.
3. If legacy Moq-shaped tests fail, select `moq` explicitly for the test assembly.
4. Keep existing `GetMock<T>()`, `VerifyLogger(...)`, and other compatibility surfaces where they are already working.
5. Defer broader provider-neutral cleanup until the suite is stable.

Pros:

- lowest short-term risk
- fastest path to a green test suite after upgrade
- easiest option for maintainers inheriting unfamiliar tests

Cons:

- leaves more Moq-specific coupling in place
- does less to prepare the suite for the long-term provider-neutral direction
- can make later modernization feel like a second migration pass

### Option 2: Modernize while you touch tests

Use this when you are already editing tests and want to move them toward the current preferred FastMoq shape.

Typical approach:

1. Upgrade to v4.
2. Stabilize only the areas that still require Moq compatibility.
3. Run tests frequently while you rewrite the suite so translation mistakes stay local to the last batch of changes.
4. For touched tests, replace legacy patterns with provider-neutral APIs where practical.
5. Move logger assertions toward `VerifyLogged(...)`.
6. Use `AddType(...)`, `GetOrCreateMock(...)`, provider-safe `Verify(...)`, and `TimesSpec.*` where they fit the test intent.

Pros:

- aligns new work with the current provider-neutral direction
- reduces future migration pressure
- makes provider assumptions more explicit at the test level

Cons:

- higher short-term change volume
- requires more FastMoq familiarity up front
- some legacy tests still need Moq compatibility because not every setup flow has a fully provider-neutral replacement yet

Practical note for full-suite rewrites:

- after the first stabilization pass, run tests often during the migration rather than waiting for a large final rewrite batch
- this is especially important when translating Moq-specific arrangement chains into NSubstitute or fake-based arrangements, because the failures are easier to localize while the change set is still small

Recommended default for takeover work:

- choose stabilize-first if you inherited the suite and need confidence quickly
- choose modernize-while-touched once the suite is green and you are editing tests for real feature work

## Full migration checklist

Use this when you are moving a larger suite instead of only touching a few tests.

1. Upgrade the package references and get the suite compiling.
2. Pick the provider that matches the current suite shape before rewriting arrangements.
3. Stabilize the suite first, especially if it still depends on `GetMock<T>()`, `VerifyLogger(...)`, `Protected()`, or other Moq-heavy flows.
4. Run tests immediately after each migration batch instead of waiting for a large final rewrite pass.
5. Translate Moq `Setup(...)` calls into provider-native arrangement syntax only in the tests you are actively modernizing.
6. Move asserts toward `Verify(...)`, `VerifyNoOtherCalls(...)`, `VerifyLogged(...)`, and `TimesSpec` where that improves clarity.
7. Keep Moq for the pockets that still depend on Moq-only semantics such as `SetupSet(...)`, `SetupAllProperties()`, `Protected()`, or `CallBase`.
8. Replace hard-to-port Moq arrangements with `AddType(...)` plus a fake or stub when that is clearer than forcing another mocking-library equivalent.

If the suite is large, prefer many small green batches over one broad rewrite. That keeps provider-translation mistakes localized and makes it easier to spot the tests that should remain on Moq.

## Copilot prompts

Reusable Copilot prompt templates now live in [Copilot migration prompts](./copilot-prompts.md).

That page supplements this guide with reusable prompt text. This guide remains the source of truth for the migration path, package boundaries, provider selection, and obsolete-surface guidance.

## Web test helpers

For controller tests, request-driven tests, and `IHttpContextAccessor`-driven tests, prefer the `FastMoq.Web` helpers instead of continuing to hand-roll local request and principal setup.

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
2. Keep existing call sites stable until the suite is green.
3. Simplify or remove the wrapper later only if the direct FastMoq call sites are actually clearer.

That approach keeps the migration low-risk. After v4 migration, those local helpers often become thin wrappers rather than remaining hand-rolled infrastructure.

## Provider selection first

The current v4 repository behavior differs from `3.0.0` in one important way:

- `FastMoq.Core` now bundles both `reflection` and `moq`
- `reflection` is the default provider if you do nothing
- tests carried forward from previous FastMoq versions that rely on `GetMock<T>()`, direct `Mock<T>` access, `Protected()`, or `VerifyLogger(...)` should select `moq` explicitly for the test assembly

This is easy to miss during migration because package installation, extension-method availability, and active-provider selection are three separate things:

- installing a provider package gives you its implementation and extension methods
- importing its namespace makes those extension methods visible
- registering it as the default provider is what actually makes FastMoq use it for new mocks

Installing `FastMoq.Core` plus `FastMoq.Provider.Moq` is not enough by itself. The Moq provider still needs to be registered as the default for that test assembly.

FastMoq is also not limited to the bundled providers. If your suite uses another mocking library, you can implement `IMockingProvider` and register your own provider instead of adopting `moq` or `nsubstitute`.

If you need a provider-by-provider answer for what is supported today, see [Provider Capabilities](../getting-started/provider-capabilities.md).

Treat this bootstrap as mandatory whenever the migrated test project still uses the Moq compatibility surface.

### Copy-paste bootstrap examples

#### xUnit

Use a module initializer because xUnit does not provide an assembly setup hook:

```csharp
using System.Runtime.CompilerServices;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;

namespace MyTests;

public static class TestAssemblyProviderBootstrap
{
    [ModuleInitializer]
    public static void Initialize()
    {
        MockingProviderRegistry.Register("moq", MoqMockingProvider.Instance, setAsDefault: true);
    }
}
```

#### NUnit

```csharp
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using NUnit.Framework;

namespace MyTests;

[SetUpFixture]
public sealed class TestAssemblyProviderBootstrap
{
    [OneTimeSetUp]
    public void Initialize()
    {
        MockingProviderRegistry.Register("moq", MoqMockingProvider.Instance, setAsDefault: true);
    }
}
```

#### MSTest

```csharp
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyTests;

[TestClass]
public sealed class TestAssemblyProviderBootstrap
{
    [AssemblyInitialize]
    public static void Initialize(TestContext _)
    {
        MockingProviderRegistry.Register("moq", MoqMockingProvider.Instance, setAsDefault: true);
    }
}
```

Migration guidance for v4:

- prefer provider-neutral surfaces such as `GetOrCreateMock(...)` and `VerifyLogged(...)` for new or actively refactored tests
- keep using Moq compatibility APIs where they materially reduce migration churn
- isolate Moq-specific usage behind an assembly-level provider selection so the tests that still depend on it are explicit

This keeps existing suites stable for v4 while steering new code toward the provider-neutral shape that will carry forward cleanly.

If you are following the stabilize-first path, the most important action is explicit Moq selection for the test assembly. If you are following the modernization path, treat explicit Moq selection as a narrow compatibility tool rather than the long-term destination.

## Replacing `Setup(...)` when moving from Moq to NSubstitute

There is not a single provider-neutral replacement for `Setup(...)`.

`Setup(...)` is Moq-native arrangement syntax. When you move a test to NSubstitute, replace the arrange side with native NSubstitute calls and keep FastMoq's provider-neutral verification on the assert side when that reads better.

Use this rule of thumb:

- Moq `Setup(...)` becomes a direct call specification on the substitute plus `Returns(...)`, `ReturnsForAnyArgs(...)`, or `When(...).Do(...)`
- Moq argument matchers such as `It.IsAny<T>()` and `It.Is<T>(...)` become `Arg.Any<T>()` and `Arg.Is<T>(...)`
- Moq `Verify(...)` can often become either `Received(...)` or FastMoq `Mocks.Verify(...)`

Common translations:

| Moq shape | NSubstitute shape |
| --- | --- |
| `dependency.Setup(x => x.GetValue()).Returns("configured");` | `dependency.AsNSubstitute().GetValue().Returns("configured");` |
| `dependency.Setup(x => x.GetValue(It.IsAny<string>())).Returns("configured");` | `dependency.AsNSubstitute().GetValue(Arg.Any<string>()).Returns("configured");` |
| `dependency.Setup(x => x.GetValue(It.Is<string>(x => x.StartsWith("a")))).Returns("configured");` | `dependency.AsNSubstitute().GetValue(Arg.Is<string>(x => x.StartsWith("a"))).Returns("configured");` |
| `dependency.Setup(x => x.Run("alpha")).Callback(() => observed = true);` | `dependency.AsNSubstitute().When(x => x.Run("alpha")).Do(_ => observed = true);` |
| `dependency.SetupSequence(x => x.GetValue()).Returns("first").Returns("second");` | `dependency.AsNSubstitute().GetValue().Returns("first", "second");` |
| `dependency.SetupGet(x => x.Mode).Returns("fast");` | `dependency.AsNSubstitute().Mode.Returns("fast");` |

Equivalent FastMoq-first examples:

```csharp
using var providerScope = MockingProviderRegistry.Push("nsubstitute");
var dependency = Mocks.GetOrCreateMock<IOrderGateway>();

dependency.AsNSubstitute().GetValue().Returns("configured");
dependency.Instance.Run("alpha");

Mocks.Verify<IOrderGateway>(x => x.Run("alpha"), TimesSpec.Once);
```

You can also arrange through `dependency.Instance` because the NSubstitute-backed instance is the substitute:

```csharp
dependency.Instance.GetValue().Returns("configured");
```

That is why NSubstitute migration usually looks like a syntax translation rather than a FastMoq wrapper translation.

Cases that do not translate cleanly:

- `SetupSet(...)`
- `SetupAllProperties()`
- `Protected()`
- Moq `CallBase`
- some `out` / `ref` verification patterns

When a test depends on those features, either keep it on the Moq provider or replace the collaborator with a fake or stub through `AddType(...)`.

## Old to new guidance

### `Initialize<T>(...)`

Old pattern:

```csharp
Mocks.Initialize<IOrderRepository>(mock =>
    mock.Setup(x => x.Load(123)).Returns(order));
```

Current guidance:

```csharp
Mocks.GetOrCreateMock<IOrderRepository>()
    .Setup(x => x.Load(123))
    .Returns(order);
```

Why:

- `Initialize<T>(...)` is now a compatibility wrapper, not the recommended setup entry point.
- `GetOrCreateMock<T>()` plus the Moq provider extensions is the preferred v4 transition path for Moq-specific setup.
- `GetMock<T>()` remains only as an obsolete compatibility path when the assembly is explicitly using the bundled Moq compatibility provider.

If you are already refactoring the test and do not specifically need a raw `Moq.Mock<T>`, prefer moving toward provider-neutral retrieval and verification instead of expanding direct `GetMock<T>()` usage further.

There is not yet a single provider-neutral drop-in replacement for `Initialize<T>(...)`, because `Initialize<T>(...)` wraps a Moq `Setup(...)` callback.

In the v4 transition, the preferred provider-package shortcut for Moq-specific setup is now:

```csharp
Mocks.GetOrCreateMock<IOrderRepository>()
    .Setup(x => x.Load(123))
    .Returns(order);
```

That shortcut comes from `FastMoq.Provider.Moq` and forwards to `AsMoq()` internally. It keeps the core abstractions provider agnostic while giving Moq-based test suites a smoother migration path.

Required namespace for those fluent Moq-backed shortcuts:

```csharp
using FastMoq.Providers.MoqProvider;
```

Without that namespace, `Setup(...)`, `SetupGet(...)`, `SetupSequence(...)`, and `Protected()` on `IFastMock<T>` can appear to have "disappeared" even though the provider package is installed.

Practical rule:

- use `GetOrCreateMock<T>()` plus the Moq provider extensions when you want tracked provider-first access with Moq-specific setup convenience in v4
- keep existing `GetMock<T>()` usage only when the test still depends on older direct `Mock<T>`-shaped compatibility code and you are minimizing churn during the v4 transition
- use `GetOrCreateMock(...)`, `Verify(...)`, and `VerifyLogged(...)` when the test can move forward without Moq-specific setup semantics
- use `AddType(...)` when the cleaner provider-neutral move is to supply a fake, stub, factory, or fixed instance rather than configure a mocking-library setup chain

Provider-neutral replacement depends on what the old `Initialize<T>(...)` callback was doing:

- if the old code mainly existed to verify later calls, start from `GetOrCreateMock<T>()` and provider-neutral verification
- if the old code mainly existed to supply behavior, prefer `AddType(...)` with a fake, stub, or fixed instance when practical
- if the old code still needs Moq `Setup(...)` semantics, prefer moving it to `GetOrCreateMock<T>()` with the Moq provider extensions and keep `GetMock<T>()` only when preserving the old Moq shape is materially cheaper than rewriting the test

### `SetupHttpMessage(...)` and HTTP helpers

Old Moq-oriented pattern:

```csharp
Mocks.SetupHttpMessage(HttpStatusCode.OK, "{\"id\":42}");
```

Current guidance for new or refactored tests:

```csharp
Mocks.WhenHttpRequestJson(HttpMethod.Get, "/orders/42", "{\"id\":42}");
```

Or, when the response needs full control:

```csharp
Mocks.WhenHttpRequest(HttpMethod.Get, "/orders/42", () =>
    new HttpResponseMessage(HttpStatusCode.OK));
```

Why:

- the provider-neutral HTTP behavior helpers now live in core and are the preferred long-term path
- the older Moq-shaped HTTP setup helpers are compatibility APIs for suites that still depend on Moq-specific behavior such as protected `SendAsync` setups
- the migration concern is package ownership, not namespace churn

Compatibility detail:

- keep `using FastMoq.Extensions;`
- keep or add the `FastMoq.Provider.Moq` package when you still need `SetupHttpMessage(...)`
- prefer `WhenHttpRequest(...)` and `WhenHttpRequestJson(...)` for newly written tests and while refactoring existing ones

That means existing source often does not need a namespace rewrite for the older HTTP helpers. The moved compatibility methods intentionally remain in `FastMoq.Extensions`; they are simply provided by the Moq package now instead of core.

### `VerifyLogger(...)` vs `VerifyLogged(...)`

Old Moq-oriented pattern:

```csharp
Mocks.GetMock<ILogger<MyComponent>>()
    .VerifyLogger(LogLevel.Information, "Processing started");
```

Current guidance:

```csharp
Mocks.VerifyLogged(LogLevel.Information, "Processing started");
```

Why:

- `VerifyLogged(...)` is the provider-safe logging assertion surface.
- `VerifyLogger(...)` remains a Moq compatibility API.
- existing logger assertions carried forward from previous FastMoq versions can stay on `VerifyLogger(...)` if the assembly explicitly selects `moq`, but new or touched tests should prefer `VerifyLogged(...)`.

### `TimesSpec` shape in v4

Use `TimesSpec` with this mental model during migration:

- property-style aliases for the common cases: `TimesSpec.Once` and `TimesSpec.NeverCalled`
- method calls when a count is required: `TimesSpec.Exactly(count)`, `TimesSpec.AtLeast(count)`, and `TimesSpec.AtMost(count)`
- `TimesSpec.Never()` still works as a compatibility form, but prefer `TimesSpec.NeverCalled` in new migration edits when you want the shape to read consistently with `TimesSpec.Once`

Examples:

```csharp
Mocks.Verify<IOrderRepository>(x => x.Save(order), TimesSpec.Once);
Mocks.Verify<IEmailGateway>(x => x.Send("finance@contoso.test"), TimesSpec.NeverCalled);
Mocks.VerifyLogged(LogLevel.Warning, "retrying", TimesSpec.Exactly(2));
```

This is one of the easiest compile-fix churn points in migration work, so it is worth being explicit.

### `Strict`

Old assumption:

```csharp
Mocks.Strict = true;
```

often implied "switch the whole runtime into a strict profile."

Current guidance:

```csharp
Mocks.Behavior.Enabled |= MockFeatures.FailOnUnconfigured;
```

or, if you want the full preset:

```csharp
Mocks.UseStrictPreset();
```

Why:

- `Strict` is now treated as a compatibility alias for `MockFeatures.FailOnUnconfigured`.
- Full-profile behavior changes belong to `Behavior` or the preset helpers.
- If older tests meant "use the full strict profile," they should move to `UseStrictPreset()` explicitly.
- New repo-era code should treat `FailOnUnconfigured` narrowly: it controls mock strictness, not every older strict-era fallback and built-in resolution rule.

Breaking-change note:

- `Strict` no longer implies the entire old strict profile by itself
- `UseStrictPreset()` is now the clearer replacement when the broader strict behavior profile is intended
- `Strict` still exists to stamp compatibility defaults onto the current policy surface, so this is a narrowing rather than a full removal of old semantics

- strict `IFileSystem` no longer guarantees a raw or empty mock in the current repo
- tracked `IFileSystem` mocks may still expose built-in members such as `File`, `Directory`, and `Path`
- if a test carried forward from previous FastMoq versions relied on null members, configure them explicitly on `GetMock<IFileSystem>()`
- this compatibility note is specific to `IFileSystem`; it is not a blanket statement that all built-in types ignore strict-mode behavior

Example:

```csharp
Mocks.UseStrictPreset();
```

```csharp
Mocks.Behavior.Enabled |= MockFeatures.FailOnUnconfigured;
Mocks.GetMock<IFileSystem>()
    .Setup(x => x.Directory)
    .Returns((IDirectory)null!);
```

For the dedicated compatibility summary, see [Breaking Changes](../breaking-changes/README.md).

For the fuller current-model explanation of `Strict`, `FailOnUnconfigured`, and the preset helpers, see [Testing Guide: Strict vs Presets](../getting-started/testing-guide.md#strict-vs-presets).

### Legacy `GetMock<T>()` vs `AddType(...)`

Old usage sometimes blurred these together.

Current guidance:

- prefer `GetOrCreateMock<T>()` when the dependency is still conceptually a tracked mock
- use `AddType(...)` when you are overriding resolution with a concrete type, custom factory, or fixed instance
- keep `GetMock<T>()` only when you are deliberately staying on the obsolete Moq compatibility path during v4 migration

Good `AddType(...)` use:

```csharp
Mocks.AddType<IClock>(_ => new FakeClock(DateTimeOffset.Parse("2026-04-01T12:00:00Z")));
```

Good provider-first mock use:

```csharp
Mocks.GetOrCreateMock<IEmailGateway>()
    .Setup(x => x.SendAsync(It.IsAny<string>()))
    .Returns(Task.CompletedTask);
```

### `DbContext` package boundary

In `3.0.0`, core directly carried the EF packages and the Moq-based `DbContextMock<TContext>` implementation.

Current repository direction:

- `GetMockDbContext<TContext>()` still lives in the `FastMoq` namespace for the main call site.
- `FastMoq.Core` is being kept lighter and no longer owns the EF package references.
- `FastMoq.Database` now owns the EF-specific helper implementation.

Practical guidance:

- if you install `FastMoq`, keep using `GetMockDbContext<TContext>()` as before
- if you install `FastMoq.Core` directly, also install `FastMoq.Database`
- use `GetDbContextHandle<TContext>(...)` when you need to choose explicitly between mocked sets and a real in-memory EF context
- do not assume the mocked-sets helper is provider-neutral yet; today that path still uses the moved Moq-based implementation

Current repo behavior now makes the mode split explicit:

```csharp
var handle = mocker.GetDbContextHandle<ApplicationDbContext>(new DbContextHandleOptions<ApplicationDbContext>
{
    Mode = DbContextTestMode.RealInMemory,
});

var dbContext = handle.Context;
```

### `AddKnownType(...)` vs `AddType(...)`

This distinction matters more in the current v4 release line than it did in previous FastMoq versions.

Use `AddType(...)` when you are overriding normal resolution for a dependency:

```csharp
Mocks.AddType<IClock>(_ => new FakeClock(DateTimeOffset.Parse("2026-04-01T12:00:00Z")));
```

Use `AddKnownType(...)` when you are extending FastMoq's built-in handling for a framework-style type:

```csharp
Mocks.AddKnownType<IFileSystem>(
    directInstanceFactory: (_, _) => new MockFileSystem().FileSystem,
    includeDerivedTypes: true);
```

The generic public API is now more strongly typed for value callbacks, so object-default hooks can work directly with `TKnown`:

```csharp
Mocks.AddKnownType<IHttpContextAccessor>(
    applyObjectDefaults: (_, accessor) =>
    {
        accessor.HttpContext!.TraceIdentifier = "integration-test";
    },
    includeDerivedTypes: true);
```

Practical rule:

- `AddType(...)` answers "resolve this abstraction differently"
- `AddKnownType(...)` answers "handle this kind of framework-heavy type differently inside FastMoq's known-type pipeline"

For most ordinary application dependencies, prefer `AddType(...)`.
For framework helpers like `IFileSystem`, `HttpClient`, `DbContext`, and `HttpContext`, prefer `AddKnownType(...)`.

### Construction APIs

The older entry points still exist:

- `CreateInstance<T>(...)`
- `CreateInstanceByType<T>(...)`

Current guidance for new code:

```csharp
var component = Mocks.CreateInstanceByType<MyComponent>(
    InstanceCreationFlags.AllowNonPublicConstructorFallback,
    typeof(int),
    typeof(string));
```

Why:

- the implementation now uses a single flags-based override model
- the call site communicates whether constructor fallback is using policy, public-only, or explicitly allowed

If you want to decouple constructor fallback from global policy, use an explicit flag:

```csharp
var component = Mocks.CreateInstance<MyComponent>(InstanceCreationFlags.AllowNonPublicConstructorFallback);
```

The default constructor-fallback policy is now also explicit at the `Mocker` level:

```csharp
Mocks.Policy.DefaultFallbackToNonPublicConstructors = false;
```

### Obsolete `MockOptional`

Old pattern:

```csharp
Mocks.MockOptional = true;
```

Current guidance for constructor creation:

```csharp
Mocks.OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker;

var component = Mocks.CreateInstance<MyComponent>();
```

Current guidance for method or delegate invocation:

```csharp
var result = Mocks.CallMethod<MyResult>(new InvocationOptions
{
    OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker,
}, (Func<IMyDependency?, MyResult>)CreateResult);
```

If reflected method fallback should be controlled independently of `Strict`, set it explicitly:

```csharp
var result = Mocks.InvokeMethod(new InvocationOptions
{
    FallbackToNonPublicMethods = false,
}, target, "Run");
```

The default reflected-method fallback policy is also configurable on the `Mocker` instance:

```csharp
Mocks.Policy.DefaultFallbackToNonPublicMethods = false;
```

Built-in framework helpers are now separately controllable through flags:

```csharp
Mocks.Policy.EnabledBuiltInTypeResolutions =
    BuiltInTypeResolutionFlags.FileSystem |
    BuiltInTypeResolutionFlags.DbContext;
```

That policy controls whether built-in direct or managed resolution is available for:

- `IFileSystem`
- `HttpClient`
- `Uri`
- `DbContext`

This keeps known-type behavior separate from `FailOnUnconfigured` so the option means what it says.

Provider-backed mock creation also has a `Mocker`-level default policy:

```csharp
Mocks.Policy.DefaultStrictMockCreation = true;
```

Use that when you want new provider-backed mocks to be created as strict without relying on the broader compatibility bundle.

`GetMockDbContext<TContext>()` remains a deliberate exception and stays on the supported DbContext helper behavior rather than adopting strict creation by default.

For `MockerTestBase<TComponent>`, the same defaults can be applied before component creation through the policy hook:

```csharp
protected override Action<MockerPolicyOptions>? ConfigureMockerPolicy => policy =>
{
    policy.DefaultStrictMockCreation = true;
};
```

Current guidance for `MockerTestBase<TComponent>`:

```csharp
protected override InstanceCreationFlags ComponentCreationFlags
    => InstanceCreationFlags.ResolveOptionalParametersViaMocker;
```

Why:

- focused component-construction overrides are clearer than an ambient toggle
- constructor creation and invocation now share the same policy model
- `MockOptional` is obsolete and remains available only as a compatibility alias

### Provider-first access

Tests carried forward from previous FastMoq versions often assumed `Moq.Mock` was the only meaningful tracked artifact.

Current guidance:

```csharp
var fastMock = Mocks.GetOrCreateMock<IOrderRepository>();
var moqMock = fastMock.AsMoq();
```

If you want the shortest Moq-specific form in v4, use the provider-package shortcut methods directly on `IFastMock<T>`:

```csharp
Mocks.GetOrCreateMock<IOrderRepository>()
    .Setup(x => x.Load(123))
    .Returns(order);
```

You can still inspect tracked models through `MockModel.NativeMock` or `GetNativeMock(...)` when you truly need the raw provider object.

Why:

- FastMoq is moving toward a provider-neutral core
- provider-specific convenience now belongs in provider packages instead of core
- raw `NativeMock` access is still available, but it is no longer the best primary guidance when a typed provider extension exists

## Expected migration exceptions

Some migrated tests should intentionally stay on raw Moq. That is normal in v4.

For practical fallback patterns when you do not want to stay on Moq, see [Provider Capabilities: Alternatives when a Moq feature is unavailable](../getting-started/provider-capabilities.md#alternatives-when-a-moq-feature-is-unavailable).

### `IMemoryCache` and `ICacheEntry`

Property-setter and cache-entry tests are still a common Moq-only pocket because they often rely on `SetupSet(...)` or `SetupAllProperties()`.

```csharp
var cacheEntry = Mocks.GetMock<ICacheEntry>();
cacheEntry.SetupAllProperties();
cacheEntry.SetupSet(x => x.Value = It.IsAny<object>());

Mocks.GetMock<IMemoryCache>()
    .Setup(x => x.CreateEntry("orders"))
    .Returns(cacheEntry.Object);
```

### `out` / `ref` verification

If the test already depends on `It.Ref<T>.IsAny` or other Moq-specific `out` / `ref` verification forms, keeping raw Moq is usually the clearest move. Prefer `GetOrCreateMock<T>().AsMoq()` when that still reads cleanly, and leave `GetMock<T>()` in place only when preserving the legacy Moq shape avoids churn.

```csharp
Mocks.GetMock<IParser>()
    .Verify(x => x.TryResolve("tenant-a", out It.Ref<object>.IsAny), Times.Once);
```

### Practical rule

- prefer provider-first APIs when they simplify the test
- keep raw Moq when setter-heavy, property-heavy, or `out` / `ref` heavy behavior would otherwise turn the migration into a rewrite
- do not treat these cases as migration failures

## Real migration examples

### Constructor and default setup migration

Before:

```csharp
Mocks.Initialize<IOrderRepository>(mock =>
    mock.Setup(x => x.Load(123)).Returns(order));
```

After:

```csharp
Mocks.GetOrCreateMock<IOrderRepository>()
    .Setup(x => x.Load(123))
    .Returns(order);
```

### Logger verification migration

Before:

```csharp
Mocks.GetMock<ILogger<OrderService>>()
    .VerifyLogger(LogLevel.Information, "Processing started");
```

After:

```csharp
Mocks.VerifyLogged(LogLevel.Information, "Processing started");
```

### Selective verify migration

Before:

```csharp
Mocks.GetMock<IOrderRepository>()
    .Verify(x => x.Save(It.IsAny<Order>()), Times.Once);
```

After:

```csharp
Mocks.Verify<IOrderRepository>(x => x.Save(It.IsAny<Order>()), TimesSpec.Once);
```

### Cache and property-setter edge case

Intentional Moq retention:

```csharp
var cacheEntry = Mocks.GetMock<ICacheEntry>();
cacheEntry.SetupAllProperties();
cacheEntry.SetupSet(x => x.Value = It.IsAny<object>());
```

That is still a valid v4 migration outcome because the test depends on Moq-only setter behavior.

### Known-type extensibility

Current guidance for framework-heavy types:

```csharp
Mocks.AddKnownType<IFileSystem>(
    directInstanceFactory: (_, _) => new MockFileSystem().FileSystem);
```

Why:

- known-type overrides are now scoped per `Mocker`
- extensions no longer need to rely on process-wide static mutation

### Fluent scenario style

This is new repo-era surface rather than a `3.0.0` migration requirement, but it is now worth adopting for readable workflow-style tests:

```csharp
Scenario
    .With(() => { /* arrange */ })
    .When(() => Component.Process())
    .Then(() => { /* assert */ })
    .Execute();
```

Use it when the test reads better as arrange/act/assert phases rather than as one large method body.
Inside `MockerTestBase<TComponent>`, prefer `Scenario` plus the parameterless `With` / `When` / `Then` overloads when `Component` is already available.
Use `WhenThrows<TException>(...)` when the act step is expected to fail but you still want trailing `Then(...)` assertions to run.
Use `ExecuteThrows<TException>()` or `ExecuteThrowsAsync<TException>()` when you want the thrown exception object back for direct inspection.

## Recommended migration order

### Stabilize-first order

1. Upgrade to v4 and run the suite unchanged.
2. If legacy Moq-shaped tests fail, select `moq` explicitly for the test assembly.
3. Audit `Strict` usage and decide whether each case means fail-on-unconfigured only or a full strict preset. Use [Testing Guide: Strict vs Presets](../getting-started/testing-guide.md#strict-vs-presets) when the replacement is unclear.
4. Replace new `MockOptional` usage with explicit `OptionalParameterResolution`, `InvocationOptions`, or `MockerTestBase<TComponent>` component-construction overrides. See [Obsolete `MockOptional`](#obsolete-mockoptional) and [Testing Guide: Optional constructor and method parameters](../getting-started/testing-guide.md#optional-constructor-and-method-parameters) when the right replacement is unclear.
5. Stop once the suite is stable unless you are already touching tests for other work.

### Modernize-while-touched order

1. Start by moving touched tests toward the provider-neutral APIs where practical. As part of that, replace `Initialize<T>(...)` case by case with `AddType(...)`, `GetOrCreateMock(...)`, `Verify(...)`, and `VerifyLogged(...)` where they fit the test intent, and keep `GetMock<T>()` only as an obsolete last-resort compatibility path in v4.
1. Decide which test areas still need explicit `moq` selection for compatibility and which ones can move directly to provider-neutral surfaces under the default `reflection` provider.
1. Separate legacy `GetMock<T>()` scenarios from `AddType(...)` scenarios so the test intent is obvious. See [Legacy `GetMock<T>()` vs `AddType(...)`](#legacy-getmockt-vs-addtype) when the replacement boundary is unclear.
1. Migrate logger assertions toward `VerifyLogged(...)` unless the test intentionally stays on the Moq compatibility surface. See [`VerifyLogger(...)` vs `VerifyLogged(...)`](#verifylogger-vs-verifylogged) and [Executable Testing Examples](../samples/testing-examples.md) for current assertion patterns.
1. Adopt provider-first surfaces where they make the test clearer, such as `GetOrCreateMock(...)`, provider-safe `Verify(...)`, and `TimesSpec.*` verification. Do not rewrite stable tests without a reason. See [Provider-first access](#provider-first-access) and [Executable Testing Examples](../samples/testing-examples.md).
1. Use the repo's executable examples as the reference for new tests, starting with [Executable Testing Examples](../samples/testing-examples.md) and the `FastMoq.TestingExample/RealWorldExampleTests.cs` source.

## Best source of examples

For current repo-backed examples, see:

- [Executable Testing Examples](../samples/testing-examples.md)
- `FastMoq.TestingExample/RealWorldExampleTests.cs`

For release delta context, see [What's New Since 3.0.0](../whats-new/README.md).

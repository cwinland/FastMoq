# Migration Guide: 3.0.0 To Current Repo

This page is the migration front door for maintainers and early adopters moving from the last public `3.0.0` release toward the current repository behavior.

It stays focused on migration boundaries, reading order, package and provider decisions, and the practical sequence for getting a suite green. Open the linked detail pages only when you hit that specific question.

## Start here for migration

If you are using Copilot or another AI assistant for migration work, start with [Copilot migration prompts](./copilot-prompts.md).

Use the prompt page as the operational entry point. Use this guide and the linked detail pages as the canonical migration rules.

Recommended reading order:

1. This guide: read it first for the migration boundary, the v3-to-v4 API ladder, and the decision between stabilization and modernization.
1. [Provider Selection and Setup](../getting-started/provider-selection.md): open this next when the suite still uses provider-specific APIs or you need to understand why a package install did not change runtime behavior.
1. [Provider Capabilities](../getting-started/provider-capabilities.md): open this when the question is "does this provider support the test shape I have?"
1. [Provider, package, and compatibility guidance](./provider-and-compatibility.md): open this for migration-specific bootstrap snippets, package-to-namespace mapping, and Moq-to-NSubstitute arrangement translation.
1. [bUnit and Blazor test migration](./bunit-and-blazor-testing.md): open this when the churn is in `MockerBlazorTestBase<T>`, `RenderParameters`, nested rendered-component helpers, authorization helpers, or bUnit upgrade fallout.
1. [Framework and web helper migration](./framework-and-web-helpers.md): open this for shared helper rewrites, keyed-service setup, Azure Functions `InstanceServices`, and `FastMoq.Web` migration guidance.
1. [API replacements and migration exceptions](./api-replacements-and-exceptions.md): open this for old-to-new replacements, compatibility-only edges, and worked migration examples.
1. [Executable Testing Examples](../samples/testing-examples.md): open this when you want repo-backed examples of the preferred target style.
1. [Testing Guide](../getting-started/testing-guide.md): open this for deeper reference material once you are actively rewriting tests.

Quick routing:

- If the migration fails because provider-specific APIs do not behave as expected, go to [Provider Selection and Setup](../getting-started/provider-selection.md) and [Provider, package, and compatibility guidance](./provider-and-compatibility.md).
- If the migration churn is in `MockerBlazorTestBase<T>`, nested component targeting, render parameters, authorization helpers, or navigation assertions after the bUnit upgrade, go to [bUnit and Blazor test migration](./bunit-and-blazor-testing.md).
- If the churn is in controller helpers, principals, `HttpContext`, `IHttpContextAccessor`, keyed DI, or framework service-provider shims, go to [Framework and web helper migration](./framework-and-web-helpers.md).
- If you are replacing a specific API such as `Initialize<T>(...)`, `VerifyLogger(...)`, `Strict`, `MockOptional`, or `GetMock<T>()`, go to [API replacements and migration exceptions](./api-replacements-and-exceptions.md).
- If you want a reusable AI workflow instead of writing prompts from scratch, go to [Copilot migration prompts](./copilot-prompts.md).

## Scope

- Public release baseline: `3.0.0`
- Release date: May 12, 2025
- Baseline commit: `4035d0d`

This is migration guidance for the current v4 release line. Some references point to repository-backed examples and current documentation structure, but the migration behavior described here is intended to match the published v4 package surface.

## What good looks like

The goal is provider-centric tests by default, not removal of all provider-specific APIs.

If your test project references the aggregate `FastMoq` package, the FastMoq Roslyn analyzers ship with it by default. That means common migration leftovers such as `.Object`, provider-native `Reset()`, `VerifyLogger(...)`, and obvious mixed `GetMock<T>()` / `GetOrCreateMock<T>()` usage can be surfaced while you modernize tests. Core-only consumers can opt in separately with `FastMoq.Analyzers`.

The analyzer pack now has two roles:

- migration guidance for mechanical provider-first rewrites and compatibility cleanup
- low-noise authoring guidance for new tests where FastMoq already has a clearer first-party pattern

Current examples include:

- `FMOQ0010` for preferring typed provider escape hatches such as `AsMoq()` or `AsNSubstitute()` over raw `NativeMock` / `GetNativeMock(...)`
- `FMOQ0011` for preferring `FastMoq.Web` helpers over hand-rolled `HttpContext`, `ControllerContext`, or `IHttpContextAccessor` registration via `AddType(...)`
- `FMOQ0012` for preferring `WhenHttpRequest(...)` or `WhenHttpRequestJson(...)` over Moq-specific HTTP compatibility helpers when the test only needs request and response behavior

Tune guidance severity in `.editorconfig` if a suite wants quieter or stricter defaults:

```ini
dotnet_diagnostic.FMOQ0010.severity = suggestion
dotnet_diagnostic.FMOQ0011.severity = suggestion
dotnet_diagnostic.FMOQ0012.severity = suggestion
```

For a successful v4 migration, use this boundary:

- move default arrangements and common verifications toward `GetOrCreateMock<T>()`, `GetObject<T>()`, `Verify(...)`, `VerifyNoOtherCalls(...)`, and `VerifyLogged(...)` where that makes the test clearer
- keep Moq-specific access only where the test still depends on Moq-only semantics such as `SetupSet(...)`, `SetupAllProperties()`, `Protected()`, or `out` / `ref` verification patterns, and prefer `GetOrCreateMock<T>()` plus `AsMoq()` or provider-package extensions before falling back to obsolete `GetMock<T>()`
- treat those Moq escape hatches as expected migration exceptions rather than as proof that the migration failed

## Migration summary

If you are moving tests forward from the public `3.0.0` package or from pre-v4 FastMoq assumptions, the main changes are:

1. Prefer provider-neutral APIs for new migration work, and treat obsolete compatibility APIs as migration targets rather than the intended end state.
1. Treat `AddType(...)` as an explicit type-resolution override, not as a general substitute for mocks.
1. Treat `Strict` as compatibility-only. Use `Behavior` or preset helpers when you mean broader behavior changes.
1. Prefer the newer provider-first retrieval and verification surfaces when you do not specifically need raw Moq APIs.
1. Use the executable examples in `FastMoq.TestingExample` as the best repo-backed reference for current patterns.
1. Treat DbContext helpers as an optional database-package concern when consuming `FastMoq.Core` directly.

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
| `GetMock<T>()` | Only when you are preserving a stable v3-shaped Moq test with minimal churn during the first stabilization pass | Obsolete compatibility surface and migration target for touched files; recommended default is to replace it with `GetOrCreateMock<T>()` plus v4 provider or package APIs once the suite is stable |
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
1. Run the test suite without rewriting test code.
1. If legacy Moq-shaped tests fail, select `moq` explicitly for the test assembly.
1. Keep existing `GetMock<T>()`, `VerifyLogger(...)`, and other compatibility surfaces where they are already working.
1. Defer broader provider-neutral cleanup until the suite is stable.

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
1. Stabilize only the areas that still require Moq compatibility.
1. Run tests frequently while you rewrite the suite so translation mistakes stay local to the last batch of changes.
1. For touched tests, replace legacy patterns with provider-neutral APIs where practical.
1. Move logger assertions toward `VerifyLogged(...)`.
1. Use `AddType(...)`, `GetOrCreateMock(...)`, provider-safe `Verify(...)`, and `TimesSpec.*` where they fit the test intent.

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
1. Pick the provider that matches the current suite shape before rewriting arrangements.
1. Stabilize the suite first, especially if it still depends on `GetMock<T>()`, `VerifyLogger(...)`, `Protected()`, or other Moq-heavy flows.
1. Run tests immediately after each migration batch instead of waiting for a large final rewrite pass.
1. Apply FastMoq analyzer fixes where the suggestions are high-confidence, then rerun the affected tests.
1. Translate Moq `Setup(...)` calls into provider-native arrangement syntax only in the tests you are actively modernizing.
1. Move asserts toward `Verify(...)`, `VerifyNoOtherCalls(...)`, `VerifyLogged(...)`, and `TimesSpec` where that improves clarity.
1. Keep Moq for the pockets that still depend on Moq-only semantics such as `SetupSet(...)`, `SetupAllProperties()`, `Protected()`, or `CallBase`.
1. Replace hard-to-port Moq arrangements with `AddType(...)` plus a fake or stub when that is clearer than forcing another mocking-library equivalent.

If the suite is large, prefer many small green batches over one broad rewrite. That keeps provider-translation mistakes localized and makes it easier to spot the tests that should remain on Moq.

## Detailed migration guides

Open these only when you hit the relevant problem. That keeps this page short without dropping the needed detail.

- [Provider, package, and compatibility guidance](./provider-and-compatibility.md): migration-specific provider bootstrap, package-to-namespace mapping, and Moq-to-NSubstitute arrangement translation.
- [bUnit and Blazor test migration](./bunit-and-blazor-testing.md): package-level and helper-level migration guidance for `FastMoq.Web`, `MockerBlazorTestBase<T>`, `RenderParameter`, nested rendered-component helpers, authorization wrappers, and navigation assertions.
- [Framework and web helper migration](./framework-and-web-helpers.md): shared-helper rewrites, keyed DI, Azure Functions `InstanceServices`, `FastMoq.Web` helper usage, principals, and controller-context migration.
- [API replacements and migration exceptions](./api-replacements-and-exceptions.md): old-to-new API guidance for `Initialize<T>(...)`, HTTP helpers, `VerifyLogger(...)`, `TimesSpec`, `Strict`, `GetMock<T>()`, `AddType(...)`, `DbContext`, `MockOptional`, provider-first access, and expected raw-Moq pockets.
- [Copilot migration prompts](./copilot-prompts.md): reusable prompt entry points for staged migration and stricter obsolete-surface cleanup.

## Recommended migration order

### Stabilize-first order

1. Upgrade to v4 and run the suite unchanged.
1. If legacy Moq-shaped tests fail, select `moq` explicitly for the test assembly.
1. Audit `Strict` usage and decide whether each case means fail-on-unconfigured only or a full strict preset. Use [API replacements and migration exceptions](./api-replacements-and-exceptions.md#strict) and [Testing Guide: Strict vs Presets](../getting-started/testing-guide.md#strict-vs-presets) when the replacement is unclear.
1. Replace new `MockOptional` usage with explicit `OptionalParameterResolution`, `InvocationOptions`, or `MockerTestBase<TComponent>` component-construction overrides. See [API replacements and migration exceptions](./api-replacements-and-exceptions.md#obsolete-mockoptional) and [Testing Guide: Optional constructor and method parameters](../getting-started/testing-guide.md#optional-constructor-and-method-parameters) when the right replacement is unclear.
1. Stop once the suite is stable unless you are already touching tests for other work.

### Modernize-while-touched order

1. Start by moving touched tests toward the provider-neutral APIs where practical. As part of that, replace `Initialize<T>(...)` case by case with `AddType(...)`, `GetOrCreateMock(...)`, `Verify(...)`, and `VerifyLogged(...)` where they fit the test intent, and keep `GetMock<T>()` only as an obsolete last-resort compatibility path in v4.
1. Decide which test areas still need explicit `moq` selection for compatibility and which ones can move directly to provider-neutral surfaces under the default `reflection` provider.
1. Separate legacy `GetMock<T>()` scenarios from `AddType(...)` scenarios so the test intent is obvious. See [API replacements and migration exceptions](./api-replacements-and-exceptions.md#legacy-getmockt-vs-addtype) when the replacement boundary is unclear.
1. Migrate logger assertions toward `VerifyLogged(...)` unless the test intentionally stays on the Moq compatibility surface. See [API replacements and migration exceptions](./api-replacements-and-exceptions.md#verifylogger-vs-verifylogged) and [Executable Testing Examples](../samples/testing-examples.md) for current assertion patterns.
1. Adopt provider-first surfaces where they make the test clearer, such as `GetOrCreateMock(...)`, provider-safe `Verify(...)`, and `TimesSpec.*` verification. Do not rewrite stable tests without a reason. See [API replacements and migration exceptions](./api-replacements-and-exceptions.md#provider-first-access) and [Executable Testing Examples](../samples/testing-examples.md).
1. Use the repo's executable examples as the reference for new tests, starting with [Executable Testing Examples](../samples/testing-examples.md) and the `FastMoq.TestingExample/RealWorldExampleTests.cs` source.

## Best source of examples

For current repo-backed examples, see:

- [Executable Testing Examples](../samples/testing-examples.md)
- `FastMoq.TestingExample/RealWorldExampleTests.cs`

For release delta context, see [What's New Since 3.0.0](../whats-new/README.md).

# What's New Since 3.0.0

This page summarizes the changes in the current v4 release line relative to the last public FastMoq release.

## Release baseline

- Last public release: `3.0.0`
- Release date: May 12, 2025
- Release baseline commit: `4035d0d`

This document describes the release delta after that baseline.

## Release status

The current repository work became the v4 release line.

At a high level:

- `3.0.0` is the last public baseline before the provider-first architecture
- v4 introduces explicit provider selection, a bundled reflection default, and clearer migration paths for Moq compatibility
- this document focuses on what changed, while the migration guide explains how to move existing tests forward

## Major changes since 3.0.0

### Provider architecture foundation

FastMoq core is being refactored around provider-neutral abstractions instead of assuming Moq everywhere internally.

Key additions include:

- `IMockingProvider`
- `IFastMock` and `IFastMock<T>`
- `MockingProviderRegistry`
- provider implementations for Moq, NSubstitute, and reflection-based fallback behavior

### Provider-first mock access

Tracked mocks now expose the active provider object directly, and provider packages can layer typed convenience extensions on top of that tracked surface.

Key surfaces include:

- `MockModel.NativeMock`
- `GetNativeMock(...)`
- `GetOrCreateMock<T>()`
- `GetOrCreateMock(Type, ...)`
- provider-package bridges such as `AsMoq()` and `AsNSubstitute()`

### Focused instance creation

The older options bag has been removed in favor of focused creation APIs.

Newer code can use:

```csharp
var component = Mocks.CreateInstanceByType<MyComponent>(
    InstanceCreationFlags.AllowNonPublicConstructorFallback,
    typeof(int),
    typeof(string));
```

Constructor visibility is now expressed directly by `InstanceCreationFlags`:

- `CreateInstance<T>(...)` follows `Mocks.Policy.DefaultFallbackToNonPublicConstructors`
- `CreateInstance<T>(InstanceCreationFlags.PublicConstructorsOnly, ...)` restricts selection to public constructors
- `CreateInstance<T>(InstanceCreationFlags.AllowNonPublicConstructorFallback, ...)` explicitly allows fallback to non-public constructors

`MockerTestBase<TComponent>` now exposes focused component-construction hooks instead of a shared options object.

### Explicit optional-parameter resolution

`MockOptional` is now obsolete and retained only as a compatibility alias rather than a primary API.

Current guidance is:

- set `Mocks.OptionalParameterResolution` for SUT creation defaults
- use `InvocationOptions.OptionalParameterResolution` for `CallMethod(...)` and `InvokeMethod(...)`
- use `ComponentCreationFlags` when the SUT is created by `MockerTestBase<TComponent>`

This brings constructor creation and helper invocation onto the same policy model instead of having different hidden rules for optional parameters.

Method invocation now also has an explicit `InvocationOptions.FallbackToNonPublicMethods` setting for reflected method fallback.

### Known-type extensibility

Current repo work adds a per-`Mocker` registration path through `AddKnownType(...)` so tests can override or extend behavior for framework-heavy types like `IFileSystem`, `HttpClient`, `DbContext`, and `HttpContext` patterns.

Built-in known-type resolution is now also explicitly controllable through `BuiltInTypeResolutionFlags`, instead of remaining implicitly tied to `FailOnUnconfigured`.

### Database package boundary

The repository now separates EF-specific helpers from the lighter core package.

What changed:

- `FastMoq.Core` no longer carries the EF-specific package references and helper model types.
- `FastMoq.Database` now owns the `DbContextMock<TContext>` implementation and related DbSet async-query helpers.
- the main `GetMockDbContext<TContext>()` call shape remains in the `FastMoq` namespace so aggregate-package consumers keep the same primary API surface.

What did not change yet:

- the mocked-sets helper path still relies on the existing Moq-based implementation moved into `FastMoq.Database`
- the real in-memory path is now explicitly available through `GetDbContextHandle<TContext>(...)` and `DbContextTestMode.RealInMemory`
- core currently uses a small runtime bridge so built-in DbContext resolution can stay optional without taking a compile-time EF dependency

The intended user-facing direction is not to split DbContext behavior by separate top-level helper methods again. The better direction is one primary DbContext helper surface with explicit mode or option selection so tests can choose between:

- pure mock-oriented behavior for mocked DbSet and query interactions
- real EF-backed behavior for in-memory provider scenarios

That distinction is now explicit in the public helper surface, while the mocked-sets internals remain intentionally Moq-based for the v4 transition.

Longer term, this likely needs a small EF-specific abstraction instead of treating DbContext support as just another plain provider mock. DbContext has extra behavior beyond generic mock creation:

- DbSet property wiring
- async query support
- named set behavior
- real provider-backed context creation when mock-only behavior is not enough

That EF abstraction is the more realistic path for separating pure mocks from real EF behavior without forcing the split through package choice alone.

### Behavior model cleanup

`Strict` no longer acts like a hidden preset switch.

Current semantics:

- `Strict` is a backward-compatible alias for `MockFeatures.FailOnUnconfigured`
- `Behavior` is the full feature-flag surface
- `UseStrictPreset()` and `UseLenientPreset()` control the broader preset profiles
- built-in type resolution and non-public fallback defaults now live on explicit `Mocker` policy properties

Breaking-change note:

- `Strict` should no longer be treated as the single switch for the whole strict behavior profile
- code that depended on the old monolithic interpretation should move to `UseStrictPreset()` when the broader preset is actually intended
- `FailOnUnconfigured` is now narrower by design: it controls mock strictness, while compatibility defaults are carried by `Strict` / preset application or explicit policy settings

New explicit policy surfaces include:

- `Mocker.Policy.EnabledBuiltInTypeResolutions`
- `Mocker.Policy.DefaultFallbackToNonPublicConstructors`
- `Mocker.Policy.DefaultFallbackToNonPublicMethods`
- `Mocker.Policy.DefaultStrictMockCreation`
- `InvocationOptions.FallbackToNonPublicMethods`

`MockerTestBase<TComponent>` also now exposes a policy hook so those defaults can be set before component creation without instantiating a separate `Mocker`.

`GetMockDbContext<TContext>()` remains intentionally excluded from the strict-mock creation override because the EF-oriented helper must stay on its supported creation path.

### Breaking change: strict `IFileSystem` mock enrichment

Current repo behavior keeps tracked `IFileSystem` mocks preconfigured against FastMoq's built-in in-memory file system even when `Strict` or `MockFeatures.FailOnUnconfigured` is enabled.

This is a breaking change from the `3.0.0` expectation that strict `IFileSystem` behaved like a raw Moq mock without built-in `MockFileSystem` defaults.

If older tests asserted null members such as `Directory` on strict `IFileSystem`, they now need to override those members explicitly.

This note is currently specific to `IFileSystem`. Adjacent built-ins such as `HttpClient` and `DbContext` do not use the same breaking path.

For the dedicated compatibility note, see [Breaking Changes](../breaking-changes/README.md).

### Scenario builder and provider-first verification

The repository now includes a minimal fluent scenario builder:

- `Scenario.With(...).When(...).Then(...).Verify(...)` inside `MockerTestBase<TComponent>`
- `WhenThrows<TException>(...)` for expected-failure act steps that still continue to `Then(...)`
- `ExecuteThrows<TException>()` and `ExecuteThrowsAsync<TException>()` for cases where the thrown exception is the primary assertion target

Verification also has a portable times model through `TimesSpec`, including `TimesSpec.Once`, `TimesSpec.Exactly(count)`, `TimesSpec.AtLeast(count)`, `TimesSpec.AtMost(count)`, and `TimesSpec.Never()`.

Inside `MockerTestBase<TComponent>`, the preferred current pattern is the `Scenario` property plus parameterless `With` / `When` / `Then` overloads when `Component` is already in scope.

Provider selection is also explicit through `MockingProviderRegistry`, with the v4 transition packages currently auto-registering `reflection` as default plus the bundled `moq` compatibility provider. Optional providers such as `nsubstitute` can be installed and registered explicitly.

### Improved docs and executable examples

Repository documentation has expanded substantially since `3.0.0`, including a repo-native testing guide, roadmap notes, richer sample documentation, and executable real-world examples in `FastMoq.TestingExample`.

## New executable examples in the repo

The example project now contains service-style samples that demonstrate current FastMoq guidance rather than only minimal constructor demos.

New examples cover:

- order processing with repository, inventory, payment, and logging
- customer import using the predefined `MockFileSystem`
- invoice reminders using the fluent `Scenario` API, `TimesSpec`, `WhenThrows`, and `ExecuteThrows`

For those examples, see [Executable Testing Examples](../samples/testing-examples.md).

## What is still intentionally not done

This repository work is still in progress.

Areas intentionally deferred or still evolving include:

- broader web-framework convenience layers beyond the current Blazor-centered support
- additional provider-specific convenience layers beyond the shared provider contract
- full cleanup of all obsolete Moq compatibility surfaces
- release packaging and migration guidance for the eventual major-version cut

For the maintainer backlog view, see [Roadmap Notes](../roadmap/README.md).

## Practical guidance for readers

If you are consuming the public NuGet package today, treat `3.0.0` as the released contract.

If you are working in this repository, use the current repo-native docs and executable examples as the source of truth for ongoing v4 behavior.

For old-to-new API guidance, see [Migration Guide: 3.0.0 To Current Repo](../migration/README.md).

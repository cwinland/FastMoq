# What's New Since 3.0.0

This page tracks the unreleased work in the repository since the last public FastMoq release.

## Release baseline

- Last public release: `3.0.0`
- Release date: May 12, 2025
- Release baseline commit: `4035d0d`

This document describes the repository delta after that release. It is not a claim that these changes are already available on NuGet.

## Release status

The current repository work is best treated as the next major-version line.

At the time of writing:

- the public package is still `3.0.0`
- the repository contains provider-era architecture and API changes beyond that release
- this work is not planned for release until additional features land and the .NET 6 support decision is settled

In practice, this is shaping more like a `4.x` release than a small `3.0.x` follow-up.

## Major changes since 3.0.0

### Provider architecture foundation

FastMoq core is being refactored around provider-neutral abstractions instead of assuming Moq everywhere internally.

Key additions include:

- `IMockingProvider`
- `IFastMock` and `IFastMock<T>`
- `MockingProviderRegistry`
- provider implementations for Moq, NSubstitute, and reflection-based fallback behavior

### Provider-first mock access

Tracked mocks now expose the active provider object directly.

Key surfaces include:

- `MockModel.NativeMock`
- `GetNativeMock(...)`
- `GetFastMock<T>()`
- `GetFastMock(Type, ...)`

### Unified instance creation

The old split among `CreateInstance(...)`, `CreateInstanceNonPublic(...)`, and `CreateInstanceByType(...)` is now backed by a shared options-based model.

Newer code can use:

```csharp
var component = Mocks.CreateInstance<MyComponent>(new InstanceCreationOptions
{
    AllowNonPublicConstructors = true,
    ConstructorParameterTypes = new[] { typeof(int), typeof(string) },
});
```

Optional constructor behavior now has an explicit setting in the same model:

```csharp
var component = Mocks.CreateInstance<MyComponent>(new InstanceCreationOptions
{
    OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker,
});
```

`MockerTestBase<TComponent>` also gained a `ComponentCreationOptions` hook so test bases can opt into the same behavior without relying on the legacy global toggle.

### Explicit optional-parameter resolution

`MockOptional` is now obsolete and retained only as a compatibility alias rather than a primary API.

New repo-era guidance is:

- use `InstanceCreationOptions.OptionalParameterResolution` for SUT creation
- use `InvocationOptions.OptionalParameterResolution` for `CallMethod(...)` and `InvokeMethod(...)`
- use `ComponentCreationOptions` when the SUT is created by `MockerTestBase<TComponent>`

This brings constructor creation and helper invocation onto the same policy model instead of having different hidden rules for optional parameters.

### Known-type extensibility

Current repo work adds a per-`Mocker` registration path through `AddKnownType(...)` so tests can override or extend behavior for framework-heavy types like `IFileSystem`, `HttpClient`, `DbContext`, and `HttpContext` patterns.

### Behavior model cleanup

`Strict` no longer acts like a hidden preset switch.

Current semantics:

- `Strict` is a backward-compatible alias for `MockFeatures.FailOnUnconfigured`
- `Behavior` is the full feature-flag surface
- `UseStrictPreset()` and `UseLenientPreset()` control the broader preset profiles

### Scenario builder and provider-first verification

The repository now includes a minimal fluent scenario builder:

- `Scenario(...).With(...).When(...).Then(...).Verify(...)`

Verification also has a portable times model through `TimesSpec`.

### Improved docs and executable examples

Repository documentation has expanded substantially since `3.0.0`, including a repo-native testing guide, roadmap notes, richer sample documentation, and executable real-world examples in `FastMoq.TestingExample`.

## New executable examples in the repo

The example project now contains service-style samples that demonstrate current FastMoq guidance rather than only minimal constructor demos.

New examples cover:

- order processing with repository, inventory, payment, and logging
- customer import using the predefined `MockFileSystem`
- invoice reminders using the fluent `Scenario(...)` API and `TimesSpec`

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

If you are working in this repository, use the current repo-native docs and executable examples as the source of truth for ongoing provider-era behavior.

For old-to-new API guidance, see [Migration Guide: 3.0.0 To Current Repo](../migration/README.md).

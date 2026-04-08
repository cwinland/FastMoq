# Provider Selection and Setup

This guide explains how FastMoq chooses a mocking provider, how to set an app-wide default for a test assembly, and when to use scoped overrides.

For a provider-by-provider support matrix, see [Provider Capabilities](./provider-capabilities.md).

## Start here

If you only remember four things from this page, make them these:

1. `reflection` stays the default provider unless you explicitly change it.
2. Installing `FastMoq.Provider.Moq` or another provider package does not select that provider automatically.
3. If the test project still uses `GetMock<T>()`, direct `Mock<T>` access, `VerifyLogger(...)`, `Protected()`, or other Moq compatibility APIs, register `moq` as the test-assembly default before running the suite.
4. You are not limited to the bundled providers. Any library can be used through FastMoq if you implement [IMockingProvider](../../api/FastMoq.Providers.IMockingProvider.yml) and register it with `MockingProviderRegistry`.

## Current defaults

For the current v4 release line:

- `FastMoq.Core` bundles the internal `reflection` provider and the bundled `moq` provider.
- `reflection` is the default provider if you do nothing.
- `moq` is still available without adding another package.
- optional providers such as `nsubstitute` must be added explicitly and registered with `MockingProviderRegistry`.

Important boundary:

- adding `FastMoq.Provider.Moq` gives you the Moq provider package and its extension methods
- it does not change the default provider by itself
- if the test assembly still uses `GetMock<T>()`, direct `Mock<T>` access, `VerifyLogger(...)`, `Protected()`, or other Moq compatibility APIs, register Moq explicitly as the default provider
- the same rule applies to any other provider package or custom provider implementation: registration controls selection

Why this matters:

- provider-neutral APIs such as `GetOrCreateMock(...)`, `VerifyLogged(...)`, and the scenario builder can work with any registered provider that supports the needed capability.
- Moq compatibility APIs such as `GetMock<T>()`, `VerifyLogger(...)`, `Protected()`, and direct `Mock<T>` setup require the Moq provider to be selected.
- provider-package extensions such as `AsMoq()`, `Setup(...)` on `IFastMock<T>`, and `AsNSubstitute()` also require their corresponding provider package and selected provider.

## First decision: do you need a non-default provider?

Use this quick check before reading the rest of the page:

- stay on `reflection` if the tests use only provider-neutral APIs and do not need mocking-library-specific setup helpers
- switch to `moq` if the tests still depend on `GetMock<T>()`, direct `Mock<T>` access, `VerifyLogger(...)`, `Protected()`, `SetupSet(...)`, or other Moq-specific semantics
- switch to `nsubstitute` only when the test project is intentionally written against NSubstitute behavior
- register a custom provider if your team uses a different mocking library or needs provider behavior that the bundled packages do not cover

## Mandatory bootstrap when the test assembly must not stay on `reflection`

If a migrated test project still uses the Moq compatibility surface, or if the suite is intentionally standardizing on another non-default provider, treat provider bootstrap as required setup, not as an optional cleanup step.

The examples below show the common Moq migration case, but the same assembly-startup rule applies to `nsubstitute` and custom providers: register the provider and set it as the default before the suite creates `Mocker` instances.

### xUnit

Use a module initializer:

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

What each argument means in that call:

- `"moq"` is the registry key, not the source of Moq behavior
- `MoqMockingProvider.Instance` is the actual provider implementation that creates mocks, verifies calls, exposes capabilities, and adapts FastMoq to Moq
- `setAsDefault: true` makes that registered provider instance the assembly default

That means the name matters only as the lookup key you will use later with `SetDefault(...)`, `Push(...)`, `TryGet(...)`, and diagnostics. The provider behavior itself comes from `MoqMockingProvider.Instance`.

In practice, still use the canonical names from the docs for built-in and packaged providers. If you registered Moq under a different name, the provider would still work, but any code or docs that later say `Push("moq")` would not match your custom alias.

Module-initializer note:

- some analyzers warn on `[ModuleInitializer]` usage in test projects, commonly `CA2255`
- for a dedicated test bootstrap type, a targeted suppression is a normal choice when xUnit is the active framework
- if your framework already offers a one-time assembly startup hook, use that hook instead of forcing the xUnit pattern into every test project

### NUnit

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

### MSTest

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

Without one of those bootstrap patterns, the active default remains `reflection`.

## How provider selection works

The selection code lives in `MockingProviderRegistry`.

Provider names are registry keys. FastMoq does not derive the string automatically from the provider type when you call `Register(...)`.

Use these rules:

- for built-in providers, use the canonical names already registered by FastMoq: `reflection` and `moq`
- for the NSubstitute package, this documentation uses `nsubstitute` as the recommended registration name
- for custom providers, choose a stable unique name such as `my-provider` and reuse that same name for `Register(...)`, `SetDefault(...)`, and `Push(...)`
- the registry is case-insensitive, but lowercase names keep examples and diagnostics consistent
- if you are unsure what is currently registered, inspect `MockingProviderRegistry.RegisteredProviderNames` or guard with `TryGet(...)`

Resolution order:

1. Use the current async-scoped override set by `Push(...)` if one exists.
2. Otherwise use the app-wide default provider.

The important methods are:

- `MockingProviderRegistry.Register(name, provider, setAsDefault: false)`
- `MockingProviderRegistry.SetDefault(name)`
- `MockingProviderRegistry.Push(name)`
- `MockingProviderRegistry.Default`

What each method is for:

- `SetDefault(name)` changes the app-wide default provider that new `Mocker` instances will use when no scoped override is active
- `Push(name)` is the temporary async-scoped override; use it when one test or one code path needs a different provider than the assembly default
- `TryGet(name, out provider)` is only a lookup and guard API; it tells you whether a provider name is registered, but it does not activate or switch anything
- `Default` returns the effective provider for the current context, which means the pushed provider if one is active, otherwise the app-wide default

Example mental model:

- `Register("moq", MoqMockingProvider.Instance, ...)` means `"moq"` is the lookup key you will later pass to `SetDefault("moq")` or `Push("moq")`
- `Register("my-provider", new MyMockingProvider(), ...)` means you chose `"my-provider"` as that provider's lookup key

## Recommended pattern: app-wide default for the test assembly

If a whole test project should use one provider, register it once at assembly startup and set it as the default.

For the bundled Moq provider in v4:

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

That runs once for the test assembly and makes Moq the default for all `Mocker` instances created afterward.

Use the two-step form only when registration and default selection happen in different places:

```csharp
MockingProviderRegistry.Register("moq", MoqMockingProvider.Instance);
MockingProviderRegistry.SetDefault("moq");
```

## Optional providers

Optional providers are not available until you add their package and register them.

Example package reference for NSubstitute:

```xml
<PackageReference Include="FastMoq.Provider.NSubstitute" Version="4.*" />
```

Then register it:

```csharp
using System.Runtime.CompilerServices;
using FastMoq.Providers;
using FastMoq.Providers.NSubstituteProvider;

namespace MyTests;

public static class TestAssemblyProviderBootstrap
{
    [ModuleInitializer]
    public static void Initialize()
    {
        MockingProviderRegistry.Register("nsubstitute", NSubstituteMockingProvider.Instance, setAsDefault: true);
    }
}
```

## Custom providers

You do not have to use the bundled FastMoq providers.

If your team wants to integrate a different mocking library, implement [IMockingProvider](../../api/FastMoq.Providers.IMockingProvider.yml), expose the needed [IMockingProviderCapabilities](../../api/FastMoq.Providers.IMockingProviderCapabilities.yml), and register that implementation with `MockingProviderRegistry`.

Typical shape:

```csharp
using System.Runtime.CompilerServices;
using FastMoq.Providers;

namespace MyTests;

public static class TestAssemblyProviderBootstrap
{
    [ModuleInitializer]
    public static void Initialize()
    {
        MockingProviderRegistry.Register("my-provider", new MyMockingProvider(), setAsDefault: true);
    }
}
```

The bundled `moq`, `reflection`, and `nsubstitute` implementations are examples of the registration model, not a closed list of allowed providers.

The built-in providers are also not intended to be subclassed and extended in place. If your team needs different behavior, create a new `IMockingProvider` implementation and register it directly. When the change is small, that implementation can delegate to an existing provider internally instead of replacing every behavior from scratch.

If you want the shortest API-reference path after reading this overview, use [API Quick Reference](../api/quick-reference.md) and follow the custom-provider navigation path there.

## Scoped override

If most of the assembly should use one provider but a specific test needs another, use a scoped override:

```csharp
using var _ = MockingProviderRegistry.Push("reflection");
var mocker = new Mocker();
```

That override applies only to the current async flow and is restored when the `IDisposable` is disposed.

## Real executable example in this repo

The repo now includes an executable provider bootstrap example in:

- `FastMoq.TestingExample/ProviderSelectionBootstrap.cs`
- `FastMoq.TestingExample/ProviderSelectionExampleTests.cs`

That example sets Moq as the app-wide default for the example test assembly and then uses the Moq compatibility surface in a real xUnit test.

## When to choose which provider in v4

- Leave the default as `reflection` if you want the provider-neutral baseline and do not need Moq-specific APIs.
- Set the default to `moq` if your test assembly relies on `GetMock<T>()`, direct `Mock<T>` access, `Protected()`, or other Moq compatibility behavior.
- Add and set another provider such as `nsubstitute` only when that test assembly is intentionally written against that provider's behavior.
- Register your own provider when neither the bundled providers nor the optional packages match the mocking library your tests actually use.

## v5 direction

The planned v5 direction is:

- `FastMoq.Core` keeps `reflection` as the default provider.
- bundled Moq compatibility is removed from core.
- provider packages such as Moq or NSubstitute are added explicitly, then registered and selected by the consuming test assembly.

In other words, the registry entry in core goes away, but the provider model does not. In `v5`, adding the Moq package alone will not activate Moq. The consuming test assembly must still register a provider implementation such as `MoqMockingProvider.Instance` and make it the default when the suite depends on Moq behavior.

That means the app-wide bootstrap pattern shown above is the forward-compatible way to select a provider.

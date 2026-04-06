# Provider Selection and Setup

This guide explains how FastMoq chooses a mocking provider, how to set an app-wide default for a test assembly, and when to use scoped overrides.

## Current defaults

For the current v4 release line:

- `FastMoq.Core` bundles the internal `reflection` provider and the bundled `moq` provider.
- `reflection` is the default provider if you do nothing.
- `moq` is still available without adding another package.
- optional providers such as `nsubstitute` must be added explicitly and registered with `MockingProviderRegistry`.

Why this matters:

- provider-neutral APIs such as `GetOrCreateMock(...)`, `VerifyLogged(...)`, and the scenario builder can work with any registered provider that supports the needed capability.
- Moq compatibility APIs such as `GetMock<T>()`, `VerifyLogger(...)`, `Protected()`, and direct `Mock<T>` setup require the Moq provider to be selected.
- provider-package extensions such as `AsMoq()`, `Setup(...)` on `IFastMock<T>`, and `AsNSubstitute()` also require their corresponding provider package and selected provider.

## How provider selection works

The selection code lives in `MockingProviderRegistry`.

Resolution order:

1. Use the current async-scoped override set by `Push(...)` if one exists.
2. Otherwise use the app-wide default provider.

The important methods are:

- `MockingProviderRegistry.Register(name, provider, setAsDefault: false)`
- `MockingProviderRegistry.SetDefault(name)`
- `MockingProviderRegistry.Push(name)`
- `MockingProviderRegistry.Default`

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

## v5 direction

The planned v5 direction is:

- `FastMoq.Core` keeps `reflection` as the default provider.
- bundled Moq compatibility is removed from core.
- provider packages such as Moq or NSubstitute are added explicitly and registered by the consuming test assembly.

That means the app-wide bootstrap pattern shown above is the forward-compatible way to select a provider.

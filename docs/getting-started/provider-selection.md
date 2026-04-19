# Provider Selection and Setup

This guide explains how FastMoq chooses a mocking provider, how to set an app-wide default for a test assembly, and when to use scoped overrides.

For a provider-by-provider support matrix, see [Provider Capabilities](./provider-capabilities.md).

## Start here

If you only remember four things from this page, make them these:

1. `reflection` remains the neutral fallback when no non-reflection provider is discoverable, or when more than one non-reflection provider is discoverable.
2. If exactly one non-reflection provider is discoverable and no explicit default was declared, FastMoq selects it automatically.
3. If the test project uses any provider-specific compatibility or extension APIs, still declare the matching provider explicitly at assembly scope so the suite is predictable.
4. You are not limited to the bundled providers. Any public concrete type that implements [IMockingProvider](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProvider.html) and exposes either a public static `Instance` or a public parameterless constructor can participate in discovery automatically. Explicit registration is still useful when you want a friendly alias, an explicit assembly default, or a guaranteed name when a fallback full-type-name collision would otherwise make auto-registration ambiguous.

## Current defaults

For the current v4 release line:

- `FastMoq.Core` bundles the internal `reflection` provider and the bundled `moq` provider.
- `reflection` is the default fallback if you do nothing and provider discovery is ambiguous.
- `moq` is still available without adding another package.
- optional providers such as `nsubstitute` must still be added explicitly as package references, but once the package is present FastMoq can discover and register them automatically.
- if exactly one non-reflection provider is discoverable, FastMoq promotes it as the effective default when no explicit default was declared.
- if multiple non-reflection providers are discoverable, FastMoq keeps `reflection` as the effective default until you select one explicitly.

Important boundary:

- adding `FastMoq.Provider.Moq` gives you the Moq provider package and its extension methods
- it becomes the effective default only when it is the only discoverable non-reflection provider and no explicit default was declared
- if the test assembly uses provider-specific APIs, declare the matching provider explicitly as the default provider
- selection can happen through [FastMoqDefaultProviderAttribute](https://help.fastmoq.com/api/FastMoq.Providers.FastMoqDefaultProviderAttribute.html), [FastMoqRegisterProviderAttribute](https://help.fastmoq.com/api/FastMoq.Providers.FastMoqRegisterProviderAttribute.html), `SetDefault(...)`, `Push(...)`, or `Register(..., setAsDefault: true)`
- custom providers can participate in discovery automatically when the provider type is public, concrete, and creatable; explicit registration is still the right tool when you want a stable friendly alias instead of the fallback full type name, or when multiple discoverable providers would otherwise collide on that fallback name

Why this matters:

- provider-neutral APIs such as `GetOrCreateMock(...)`, `VerifyLogged(...)`, and the scenario builder can work with any registered provider that supports the needed capability.
- Moq compatibility APIs such as `GetMock<T>()`, `VerifyLogger(...)`, `Protected()`, and direct `Mock<T>` setup require the Moq provider to be selected.
- provider-package extensions such as `AsMoq()`, `Setup(...)` on `IFastMock<T>`, `AsNSubstitute()`, and `Received(...)` also require their corresponding provider package and selected provider.

For concrete mocks that need constructor arguments, stay on the provider-first path instead of falling back to `GetMock<T>(...)`. When the request only needs constructor arguments, `GetOrCreateMockWithConstructorArgs<T>(...)` keeps that intent explicit without changing `GetOrCreateMock<T>(null)` binding:

```csharp
var queueClient = mocker.GetOrCreateMockWithConstructorArgs<QueueClient>(
    new Uri("https://account.queue.core.windows.net/work-items"),
    new QueueClientOptions());
```

Use `MockRequestOptions` when the request also needs a service key or non-public constructor selection. This still depends on the selected provider supporting concrete class mocking; the bundled `reflection` provider remains limited to interfaces and parameterless concrete types.

## First decision: do you need a non-default provider?

Use this quick check before reading the rest of the page:

- stay on `reflection` if the tests use only provider-neutral APIs and do not need mocking-library-specific setup helpers
- switch to `moq` if the tests still depend on `GetMock<T>()`, direct `Mock<T>` access, `VerifyLogger(...)`, `Protected()`, `SetupSet(...)`, or other Moq-specific semantics
- switch to `nsubstitute` only when the test project is intentionally written against NSubstitute behavior
- add a custom provider when your team uses a different mocking library or needs provider behavior that the bundled packages do not cover; register it explicitly only when you want a custom alias or custom construction logic

## Mandatory assembly default when the test assembly must not stay on `reflection`

If a migrated test project still uses any non-default provider-specific compatibility or extension surface, treat assembly-wide provider selection as required setup, not as an optional cleanup step.

For example, `GetMock<T>()`, direct `Mock<T>` access, `VerifyLogger(...)`, and `Protected()` still mean `moq`, while `AsNSubstitute()` and `Received(...)` mean `nsubstitute`.

For the built-in `moq` provider, and for `nsubstitute` after its package is referenced, the shortest path is [FastMoqDefaultProviderAttribute](https://help.fastmoq.com/api/FastMoq.Providers.FastMoqDefaultProviderAttribute.html):

```csharp
using FastMoq.Providers;

[assembly: FastMoqDefaultProvider("moq")]
```

That keeps the test assembly explicit without requiring a startup hook. This works with xUnit, NUnit, MSTest, or any other test framework because FastMoq reads the assembly attribute during its own provider bootstrap.

The attribute selects the assembly-wide default provider by name. It does not create a new provider registration or alias.

If an entire test subtree stays on one provider and your repository already uses `Directory.Build.props` or `Directory.Build.targets` to stamp assembly attributes, centralize the default there instead of repeating a bootstrap file in every test project:

```xml
<Project>
    <ItemGroup Condition="'$(IsTestProject)' == 'true'">
        <AssemblyAttribute Include="FastMoq.Providers.FastMoqDefaultProviderAttribute">
            <_Parameter1>moq</_Parameter1>
        </AssemblyAttribute>
    </ItemGroup>
</Project>
```

That pattern fits repositories that already stamp `InternalsVisibleTo` or similar assembly metadata from MSBuild. Avoid a repo-wide default when some test projects intentionally stay on `reflection` or use a different provider.

When registration and selection need to happen together at assembly scope, use [FastMoqRegisterProviderAttribute](https://help.fastmoq.com/api/FastMoq.Providers.FastMoqRegisterProviderAttribute.html):

```csharp
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;

[assembly: FastMoqRegisterProvider("moq", typeof(MoqMockingProvider), SetAsDefault = true)]
```

That registers the provider and makes it the assembly default during FastMoq bootstrap, without requiring framework-specific startup code.

Use startup code instead when you need more than declarative assembly metadata. Common cases are:

- choosing the provider dynamically at runtime from configuration, environment, or target-specific logic
- combining provider selection with other one-time test bootstrap work in the same startup path
- running custom registration logic that cannot be expressed as a provider type plus `SetAsDefault`

Analyzer note:

- `FMOQ0023` warns when legacy Moq-shaped FastMoq APIs remain in a project without explicit Moq onboarding. For core-only package graphs, that means adding `FastMoq.Provider.Moq` and selecting `moq` explicitly.

### Assembly startup alternatives

The examples below are alternatives to the assembly attributes. Use them when registration and selection need to happen together before the suite creates `Mocker` instances, but the decision cannot be expressed with `FastMoqDefaultProvider(...)` or `FastMoqRegisterProvider(...)`.

### xUnit

If you need startup code in xUnit, a module initializer is a portable option that works well for provider registration and default-provider selection. Consumers on xUnit v3 may also choose assembly fixtures or test pipeline startup when those fit their broader test-bootstrap model.

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
- for a dedicated test bootstrap type, a targeted suppression is a normal choice when you intentionally use the module-initializer pattern
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

Without the assembly attribute or one of those bootstrap patterns, the active default is chosen by discovery: a single discoverable non-reflection provider is selected automatically, otherwise FastMoq stays on `reflection`.

## How provider selection works

The selection code lives in `MockingProviderRegistry`.

Provider names are registry keys. FastMoq does not derive the string automatically from the provider type when you call `Register(...)`.

Use these rules:

- for built-in providers, use the canonical names already registered by FastMoq: `reflection` and `moq`
- for the NSubstitute package, this documentation uses `nsubstitute` as the recommended registration name
- for custom providers discovered by convention, the fallback registry key is the provider type's full name such as `MyCompany.Testing.MyMockingProvider`
- when you want a shorter or friendlier key such as `my-provider`, declare it explicitly through `Register(...)` or `[assembly: FastMoqRegisterProvider(...)]` and reuse that alias for `SetDefault(...)` and `Push(...)`
- FastMoq does not generate random fallback names for convention-discovered providers; if multiple discoverable providers would map to the same fallback full type name, FastMoq skips automatic registration for that name and expects an explicit registration instead
- the registry is case-insensitive, but lowercase names keep examples and diagnostics consistent
- if you are unsure what is currently registered, inspect `MockingProviderRegistry.RegisteredProviders` for the name, provider type, and registration source, inspect `MockingProviderRegistry.DiscoveryWarnings` for skipped convention-discovery collisions, or guard with `TryGet(...)`

Resolution order:

1. Use the current async-scoped override set by `Push(...)` if one exists.
2. Otherwise use the app-wide default provider, whether it came from [FastMoqDefaultProviderAttribute](https://help.fastmoq.com/api/FastMoq.Providers.FastMoqDefaultProviderAttribute.html), [FastMoqRegisterProviderAttribute](https://help.fastmoq.com/api/FastMoq.Providers.FastMoqRegisterProviderAttribute.html), `SetDefault(...)`, or `Register(..., setAsDefault: true)`.
3. Otherwise, if exactly one non-reflection provider is discoverable, FastMoq uses that provider implicitly.
4. Otherwise FastMoq falls back to `reflection`.

The important entry points are:

- `[assembly: FastMoqDefaultProvider("name")]`
- `[assembly: FastMoqRegisterProvider("name", typeof(...), SetAsDefault = true)]`
- `MockingProviderRegistry.Register(name, provider, setAsDefault: false)`
- `MockingProviderRegistry.SetDefault(name)`
- `MockingProviderRegistry.Push(name)`
- `MockingProviderRegistry.Default`

What each method is for:

- `FastMoqDefaultProviderAttribute` is the declarative assembly-wide default when the provider name is already resolvable
- `FastMoqRegisterProviderAttribute` is the declarative assembly-wide register-and-select path when FastMoq must instantiate the provider type first
- `SetDefault(name)` changes the app-wide default provider that new `Mocker` instances will use when no scoped override is active
- `Push(name)` is the temporary async-scoped override; use it when one test or one code path needs a different provider than the assembly default
- `TryGet(name, out provider)` is only a lookup and guard API; it tells you whether a provider name is registered, but it does not activate or switch anything
- `Default` returns the effective provider for the current context, which means the pushed provider if one is active, otherwise the app-wide default

In practice:

- use `[assembly: FastMoqDefaultProvider("moq")]` or `[assembly: FastMoqDefaultProvider("nsubstitute")]` when the canonical provider name is already known to FastMoq
- use `[assembly: FastMoqRegisterProvider("name", typeof(...), SetAsDefault = true)]` when you want a stable custom alias, explicit registration-plus-default behavior, or custom-provider onboarding that should not rely on the fallback full type name
- use startup code when provider choice is dynamic or registration requires additional logic beyond declarative assembly metadata

Inspection tip:

- `MockingProviderRegistry.RegisteredProviders` shows the active registry name, concrete provider type, and whether the registration came from built-in bootstrap, assembly metadata, runtime registration, or convention discovery
- `MockingProviderRegistry.DiscoveryWarnings` records convention-discovery cases that were skipped, including fallback-name collisions that require manual registration

Example mental model:

- `Register("moq", MoqMockingProvider.Instance, ...)` means `"moq"` is the lookup key you will later pass to `SetDefault("moq")` or `Push("moq")`
- `Register("my-provider", new MyMockingProvider(), ...)` means you chose `"my-provider"` as that provider's lookup key
- if FastMoq discovers `MyCompany.Testing.MyMockingProvider` by convention and you did not register an alias, the lookup key is `"MyCompany.Testing.MyMockingProvider"`

## Recommended pattern: app-wide default for the test assembly

If a whole test project should use one provider, declare it once at assembly level.

For the bundled Moq provider in v4, the shortest form is:

```csharp
using FastMoq.Providers;

[assembly: FastMoqDefaultProvider("moq")]
```

That is also the simplest path for `nsubstitute` once the package is referenced.

If you want the assembly to register the provider type and select it in one step, use:

```csharp
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;

[assembly: FastMoqRegisterProvider("moq", typeof(MoqMockingProvider), SetAsDefault = true)]
```

Use the startup-hook form when registration and selection need to happen together but the decision is dynamic, needs custom construction logic, or belongs with broader startup work:

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

Optional providers are not available until you add their package.

Example package reference for NSubstitute:

```xml
<PackageReference Include="FastMoq.Provider.NSubstitute" Version="4.*" />
```

Once the package is present, the shortest selection path is:

```csharp
using FastMoq.Providers;

[assembly: FastMoqDefaultProvider("nsubstitute")]
```

FastMoq can resolve the canonical `nsubstitute` provider name on demand when that package is available.

Use the assembly registration attribute when you want a custom alias or want registration and selection to stay declarative:

```csharp
using FastMoq.Providers;
using FastMoq.Providers.NSubstituteProvider;

[assembly: FastMoqRegisterProvider("nsubstitute", typeof(NSubstituteMockingProvider), SetAsDefault = true)]
```

Use startup code instead when registration needs additional runtime logic:

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

If your team wants to integrate a different mocking library, implement [IMockingProvider](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProvider.html), expose the needed [IMockingProviderCapabilities](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProviderCapabilities.html), and make the provider type public and concrete.

If the provider type also exposes a public static `Instance` or a public parameterless constructor, FastMoq can discover it automatically and register it under its full type name.

That fallback name is intentionally stable. FastMoq does not invent random names for convention-discovered providers because those names would be hard to target later from `FastMoqDefaultProvider(...)`, `SetDefault(...)`, or `Push(...)`. If two discoverable providers would land on the same full-type-name fallback, FastMoq leaves that name unregistered and expects you to register the intended provider explicitly under a stable alias.

For custom providers, [FastMoqRegisterProviderAttribute](https://help.fastmoq.com/api/FastMoq.Providers.FastMoqRegisterProviderAttribute.html) is still the shortest declarative path when you want a shorter alias, an assembly default, or a registration name that does not depend on the provider type name.

Convention-based custom-provider example:

```csharp
using FastMoq.Providers;

public sealed class MyMockingProvider : IMockingProvider
{
    public static readonly MyMockingProvider Instance = new();

    private MyMockingProvider()
    {
    }

    // IMockingProvider members omitted for brevity.
}
```

Without any registration metadata, that provider can be selected by its fallback full type name:

```csharp
using FastMoq.Providers;

[assembly: FastMoqDefaultProvider("MyCompany.Testing.MyMockingProvider")]
```

Declarative custom-provider example:

```csharp
using FastMoq.Providers;

[assembly: FastMoqRegisterProvider("my-provider", typeof(MyMockingProvider), SetAsDefault = true)]
```

Use a startup hook instead when the provider must be created with runtime state, external dependencies, or other initialization that cannot be expressed as `typeof(MyMockingProvider)` alone, or when you want to register the provider under a friendlier alias than the fallback full type name.

If you need to confirm which convention-based providers were actually registered, inspect `MockingProviderRegistry.RegisteredProviders`. If a provider did not register because its fallback name was ambiguous, `MockingProviderRegistry.DiscoveryWarnings` will include the skipped name and the candidate provider types.

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

The repo now includes an executable provider-selection example in:

- `FastMoq.TestingExample/ProviderSelectionBootstrap.cs`
- `FastMoq.TestingExample/ProviderSelectionExampleTests.cs`

That example declares Moq as the app-wide default for the example test assembly and then uses the Moq compatibility surface in a real xUnit test.

## When to choose which provider in v4

- Leave the default as `reflection` if you want the provider-neutral baseline and do not need Moq-specific APIs.
- Set the default to `moq` if your test assembly relies on `GetMock<T>()`, direct `Mock<T>` access, `Protected()`, or other Moq compatibility behavior.
- Add and set another provider such as `nsubstitute` only when that test assembly is intentionally written against that provider's behavior.
- Register your own provider when neither the bundled providers nor the optional packages match the mocking library your tests actually use.

## v5 direction

The planned v5 direction is:

- `FastMoq.Core` keeps `reflection` as the default provider.
- bundled Moq compatibility is removed from core.
- provider packages such as Moq or NSubstitute are added explicitly, then selected by canonical name or registered under a custom alias by the consuming test assembly.

In other words, the registry entry in core goes away, but the provider model does not. In `v5`, adding the Moq package alone will not activate Moq. The consuming test assembly must still make its intended provider explicit when the suite depends on provider-specific behavior.

That means the assembly-default pattern shown above, whether via the attribute for known provider names or a startup hook for custom registration, is the forward-compatible way to select a provider.

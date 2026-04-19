# Provider, Package, And Compatibility Guidance

This page collects the migration details that matter when package installation, namespace visibility, and active provider selection drift apart.

Open this page when tests compile but provider-specific behavior still fails, or when you are translating Moq-shaped arrangements to another provider.

Use this page together with [Provider Selection and Setup](../getting-started/provider-selection.md) when a migration issue looks like a package or bootstrap problem rather than a test-shape problem.

## Provider selection first

The current v4 repository behavior differs from `3.0.0` in one important way:

- `FastMoq.Core` now bundles both `reflection` and `moq`
- `reflection` is the default provider if you do nothing
- tests carried forward from previous FastMoq versions that rely on `GetMock<T>()`, direct `Mock<T>` access, `Protected()`, or `VerifyLogger(...)` should select `moq` explicitly for the test assembly

This is easy to miss during migration because package installation, extension-method availability, and active-provider selection are three separate things:

- installing a provider package gives you its implementation and extension methods
- importing its namespace makes those extension methods visible
- declaring or setting the default provider is what actually makes FastMoq use it for new mocks

### Package-to-namespace quick map

Use this table when package names and in-editor namespace discovery drift apart during migration.

| Package | Common namespace imports | Typical surfaces you expect to appear |
| --- | --- | --- |
| `FastMoq` | `FastMoq`, `FastMoq.Extensions`, `FastMoq.Providers`, plus any helper namespaces you choose to use | Aggregate package that brings the primary runtime, shared helper packages, and analyzer assets into one install |
| `FastMoq.Core` | `FastMoq`, `FastMoq.Extensions`, `FastMoq.Providers` | `Mocker`, `MockerTestBase<T>`, `GetOrCreateMock<T>()`, `GetObject<T>()`, `Verify(...)`, `VerifyLogged(...)`, `MockingProviderRegistry` |
| `FastMoq.Abstractions` | `FastMoq.Providers` | `IMockingProvider`, `IMockingProviderCapabilities`, `IFastMock`, `TimesSpec`, `MockCreationOptions`, `FastMoqDefaultProviderAttribute`, `FastMoqRegisterProviderAttribute` |
| `FastMoq.Azure` | `FastMoq.Azure.Pageable`, `FastMoq.Azure.Credentials`, `FastMoq.Azure.DependencyInjection`, `FastMoq.Azure.KeyVault`, `FastMoq.Azure.Storage` | `PageableBuilder`, credential helpers, Azure DI/config helpers, and common Azure client registration helpers |
| `FastMoq.AzureFunctions` | `FastMoq.AzureFunctions.Extensions` | `CreateFunctionContextInstanceServices(...)`, `AddFunctionContextInstanceServices(...)` |
| `FastMoq.Analyzers` | n/a at runtime | Roslyn diagnostics and targeted code fixes such as `FMOQ0003` and `FMOQ0013`, plus advisory migration diagnostics such as `FMOQ0030`, for provider-first authoring guidance |
| `FastMoq.Provider.Moq` | `FastMoq.Providers.MoqProvider` | `MoqMockingProvider`, `AsMoq()`, `Setup(...)`, `SetupGet(...)`, `SetupSequence(...)`, `Protected()` |
| `FastMoq.Provider.NSubstitute` | `FastMoq.Providers.NSubstituteProvider` | `NSubstituteMockingProvider`, `AsNSubstitute()`, `Received(...)`, `DidNotReceive()` |
| `FastMoq.Web` | `FastMoq.Web`, `FastMoq.Web.Extensions` | `TestClaimsPrincipalOptions`, `CreateHttpContext(...)`, `CreateControllerContext(...)`, `SetupClaimsPrincipal(...)`, `AddHttpContext(...)`, `AddHttpContextAccessor(...)` |
| `FastMoq.Database` | `FastMoq` | `GetMockDbContext<TContext>()`, `GetDbContextHandle<TContext>()`, `DbContextHandleOptions<TContext>`, `DbContextTestMode` |

Packages control availability. Namespaces control extension discovery and which APIs light up in the editor.

### Aggregate package and EF Core alignment

The aggregate `FastMoq` package intentionally includes `FastMoq.Database`, and that helper package brings EF Core test-helper dependencies such as `Microsoft.EntityFrameworkCore.InMemory`.

That is a package-topology choice, not a signal that every suite should always take the umbrella package.

Use this rule during migration:

- keep `FastMoq` when you want the umbrella package and the EF Core major versions across your graph already align
- prefer `FastMoq.Core` plus only the helper packages you actually need when you do not want the EF-specific helper dependency surface
- if you intentionally combine the aggregate package with separately pinned EF Core provider packages, align the EF Core major versions across the graph

Most test projects should start with `FastMoq` or `FastMoq.Core`. `FastMoq.Abstractions` is mainly for custom-provider or advanced extension work, while `FastMoq.Analyzers` only contributes diagnostics and code fixes at build time.

Installing `FastMoq.Core` plus `FastMoq.Provider.Moq` is not enough by itself. The Moq provider still needs to be selected as the default for that test assembly.

FastMoq is also not limited to the bundled providers. If your suite uses another mocking library, you can implement `IMockingProvider` and register your own provider instead of adopting `moq` or `nsubstitute`.

If you need a provider-by-provider answer for what is supported today, see [Provider Capabilities](../getting-started/provider-capabilities.md).

Treat explicit assembly-default selection as mandatory whenever the migrated test project still uses any non-default provider-specific compatibility or extension surface.

Current fallback rule of thumb:

- `GetOrCreateMock<T>()` is the default tracked path inside a `Mocker`
- `CreateStandaloneFastMock<T>()` or `MockingProviderRegistry.Default.CreateMock<T>()` is the detached path when the rewritten collaborator should not be tracked in the parent `Mocker`
- `AddType(...)` or keyed registrations are the honest fallback when interface resolution is ambiguous and the test really means one specific implementation or role

For example, `GetMock<T>()`, direct `Mock<T>` access, `VerifyLogger(...)`, and `Protected()` still mean `moq`, while `AsNSubstitute()` and `Received(...)` mean `nsubstitute`.

For the built-in `moq` provider, and for `nsubstitute` after its package is referenced, the shortest path is [FastMoqDefaultProviderAttribute](../../api/FastMoq.Providers.FastMoqDefaultProviderAttribute.yml):

```csharp
using FastMoq.Providers;

[assembly: FastMoqDefaultProvider("moq")]
```

That attribute works with xUnit too; it is not tied to a particular test framework. It selects the default provider by canonical name during FastMoq bootstrap.

When the provider must be registered and selected together at assembly scope, use [FastMoqRegisterProviderAttribute](../../api/FastMoq.Providers.FastMoqRegisterProviderAttribute.yml):

```csharp
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;

[assembly: FastMoqRegisterProvider("moq", typeof(MoqMockingProvider), SetAsDefault = true)]
```

That form registers the provider type and makes it the assembly default during FastMoq bootstrap, without depending on a framework-specific startup hook.

Use the startup-code examples below only when you need more than declarative assembly metadata. Common cases are:

- choosing the provider dynamically at runtime from configuration, environment, or target-specific logic
- combining provider selection with other one-time test bootstrap work in the same startup path
- running custom registration logic that cannot be expressed as a provider type plus `SetAsDefault`

### Copy-paste startup-hook examples

#### xUnit

If you need startup code in xUnit, a module initializer is a portable option that works well for provider registration and default-provider selection. Consumers on xUnit v3 may also choose assembly fixtures or test pipeline startup when those fit their broader test-bootstrap model:

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

Module-initializer note:

- some analyzers warn on `[ModuleInitializer]` usage in test projects, commonly `CA2255`
- for a dedicated test bootstrap type, a targeted suppression is an expected choice when you intentionally use the module-initializer pattern
- if your framework already offers a one-time assembly startup hook, use that hook instead of forcing the xUnit pattern into every test project

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
- isolate Moq-specific usage behind an assembly-level provider selection so the tests that still depend on it are explicit, whether that is the attribute form or a startup hook

This keeps existing suites stable for v4 while steering new code toward the provider-neutral shape that will carry forward cleanly.

If you are following the stabilize-first path, the most important action is explicit Moq selection for the test assembly. If you are following the modernization path, treat explicit Moq selection as a narrow compatibility tool rather than the long-term destination.

## Temporary compatibility APIs: keep them narrow

FastMoq v4 still carries several compatibility APIs so migrated suites can stay green while they move toward the provider-first shape.

Treat these APIs as temporary migration tools, not as the target style for new helpers:

| Compatibility path | Preferred destination |
| --- | --- |
| `GetMock<T>()`, `GetRequiredMock<T>()`, raw `Mock<T>` access | `GetOrCreateMock<T>()` plus provider-first verification, or provider-native escape hatches only where they still add value |
| `MockOptional` | `OptionalParameterResolutionMode.ResolveViaMocker` or explicit `InvocationOptions` / `InstanceCreationFlags` |
| `Strict` bundle toggles | `Mocker.Policy.DefaultStrictMockCreation` and explicit built-in resolution flags |
| context-aware `AddType(...)` overloads | `AddKnownType(...)` |
| mocked `IServiceProvider` / `IServiceScopeFactory` / `IServiceScope` shims | `CreateTypedServiceProvider(...)`, `CreateTypedServiceScope(...)`, `AddServiceProvider(...)`, `AddServiceScope(...)`, and `AddFunctionContextInstanceServices(...)` with `FastMoq.AzureFunctions` installed for the `FunctionContext` helper path |

Two practical rules help here:

- keep compatibility APIs at the edge of the suite, usually in a temporary wrapper or base-class migration layer
- when a shared helper already needs editing, prefer jumping straight to the provider-first or typed-helper destination instead of preserving the compatibility shape one more time

Do not keep a dedicated `Mocker` alive just because an older helper used detached Moq creation. Detached provider-first creation already exists today: use `CreateStandaloneFastMock<T>()` when you are already inside a `Mocker`, or `MockingProviderRegistry.Default.CreateMock<T>()` when you need the provider-selected handle directly.

Analyzer notes:

- `FMOQ0013` warns on raw `IServiceProvider`, `IServiceScopeFactory`, and `IServiceScope` shims, plus manual scope-factory extraction, and pushes them toward the typed helper path
- for Azure Functions worker tests, `FMOQ0013` also warns on direct `FunctionContext.InstanceServices` `Setup(...)`, `SetupGet(...)`, and `SetupProperty(...)` usage, and the auto-fix appears when `FastMoq.AzureFunctions` is already referenced for the direct provider-assignment shapes that can become `context.AddFunctionContextInstanceServices(provider)`
- `FMOQ0014` warns on context-aware compatibility `AddType(...)` usage and pushes it toward `AddKnownType(...)`
- `FMOQ0015` warns when same-type keyed constructor dependencies are accidentally collapsed into one unkeyed double

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

For simple interface-property `SetupSet(...)` cases, the preferred non-Moq migration target is `AddPropertySetterCapture<TService, TValue>(...)`. For simple interface-property `SetupAllProperties()` cases, prefer `AddPropertyState<TService>(...)`. When the component under test is already created through `MockerTestBase<TComponent>`, add the helper before construction or call `CreateComponent()` after the registration change. When the collaborator needs broader behavior, or the target is not an interface, fall back to a fake that records assignments through `PropertyValueCapture<TValue>` or exposes real property state directly.

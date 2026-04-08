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
- registering it as the default provider is what actually makes FastMoq use it for new mocks

### Package-to-namespace quick map

Use this table when package names and in-editor namespace discovery drift apart during migration.

| Package | Common namespace imports | Typical surfaces you expect to appear |
| --- | --- | --- |
| `FastMoq.Core` | `FastMoq`, `FastMoq.Extensions`, `FastMoq.Providers` | `Mocker`, `MockerTestBase<T>`, `GetOrCreateMock<T>()`, `GetObject<T>()`, `Verify(...)`, `VerifyLogged(...)`, `MockingProviderRegistry` |
| `FastMoq.Provider.Moq` | `FastMoq.Providers.MoqProvider` | `MoqMockingProvider`, `AsMoq()`, `Setup(...)`, `SetupGet(...)`, `SetupSequence(...)`, `Protected()` |
| `FastMoq.Provider.NSubstitute` | `FastMoq.Providers.NSubstituteProvider` | `NSubstituteMockingProvider`, `AsNSubstitute()`, `Received(...)`, `DidNotReceive()` |
| `FastMoq.Web` | `FastMoq.Web`, `FastMoq.Web.Extensions` | `TestClaimsPrincipalOptions`, `CreateHttpContext(...)`, `CreateControllerContext(...)`, `SetupClaimsPrincipal(...)`, `AddHttpContext(...)`, `AddHttpContextAccessor(...)` |
| `FastMoq.Database` | `FastMoq` | `GetMockDbContext<TContext>()`, `GetDbContextHandle<TContext>()`, `DbContextHandleOptions<TContext>`, `DbContextTestMode` |

Packages control availability. Namespaces control extension discovery and which APIs light up in the editor.

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

Module-initializer note:

- some analyzers warn on `[ModuleInitializer]` usage in test projects, commonly `CA2255`
- for a dedicated test bootstrap type, a targeted suppression is an expected choice when xUnit is the active framework
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

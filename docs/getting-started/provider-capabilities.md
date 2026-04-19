# Provider Capabilities

This page answers a practical question: what works with each FastMoq provider today, and what remains provider-specific or unsupported.

Use it together with [Provider Selection and Setup](./provider-selection.md):

- that page explains how provider registration and selection works
- this page explains what each provider can actually do once selected

## Why this page exists

FastMoq has two different layers of behavior:

- provider-neutral APIs such as `GetOrCreateMock(...)`, `Verify(...)`, `VerifyNoOtherCalls(...)`, and `VerifyLogged(...)`
- provider-specific capabilities and convenience APIs exposed by the selected provider

The selected provider determines whether features such as protected-member access, automatic property backing, base-call behavior, and logger capture are available.

## Capability matrix

These values reflect `IMockingProviderCapabilities` in the current v4 release line.

| Capability | Moq | NSubstitute | Reflection |
| --- | --- | --- | --- |
| `SupportsCallBase` | Yes | No | No |
| `SupportsSetupAllProperties` | Yes | No | No |
| `SupportsProtectedMembers` | Yes | No | No |
| `SupportsInvocationTracking` | Yes | Yes | Yes |
| `SupportsLoggerCapture` | Yes | Yes | No |

## What remains portable across providers

These are the safest v4 APIs to document as provider-first or provider-neutral:

- `GetOrCreateMock<T>()`
- `GetObject<T>()`
- `Verify(...)`
- `VerifyNoOtherCalls(...)`
- `TimesSpec`
- `VerifyLogged(...)` when the selected provider supports logger capture
- scenario-builder flows that rely on provider-neutral verification

If you want migration guidance that will carry forward into v5 cleanly, start from those APIs first.

## Alternatives when a Moq feature is unavailable

Some APIs are strongest in Moq today and do not have a fully equivalent provider-neutral shape.

When that happens, use this rule:

- prefer a provider-neutral FastMoq API when one exists
- otherwise prefer a fake, stub, or real test double through `AddType(...)`
- keep Moq only when the test fundamentally depends on behavior the other providers do not expose cleanly

| Moq-oriented feature | Alternative outside Moq | When Moq is still the right tool |
| --- | --- | --- |
| `Setup(...)` expression-based arrangement | Use the selected provider's native arrangement style when it exists. For NSubstitute, translate `Setup(...)` into direct substitute calls such as `substitute.Method(...).Returns(...)`, `substitute.When(...).Do(...)`, `Arg.Any<T>()`, and `Arg.Is<T>(...)`, or replace the collaborator with a fake/stub through `AddType(...)` when you want a provider-neutral path. Keep FastMoq's verification APIs for the assert side when possible. | When you are intentionally preserving existing Moq-shaped setup chains with minimal churn, or when the test depends on Moq-only expression setup behavior. |
| `VerifyLogger(...)` | Prefer `VerifyLogged(...)`. For a first-party registration story, use `AddLoggerFactory()` to register callback-backed `ILoggerFactory`, `ILogger`, and `ILogger<T>` services directly on `Mocker`, or use `CreateLoggerFactory()` when you want to plug the same capture-backed factory into a typed `IServiceProvider` recipe. | When you are intentionally preserving older Moq-shaped logger assertions with minimal churn. |
| `Protected()` for `HttpMessageHandler` | Prefer `WhenHttpRequest(...)` or `WhenHttpRequestJson(...)` for HTTP behavior. | When the test really depends on direct protected-member interception rather than request/response behavior. |
| `Protected()` for arbitrary protected members | Prefer testing through a public seam, extracted collaborator, or concrete fake. | When the implementation cannot reasonably be reshaped and protected-member interception is the behavior under test. |
| `SetupSet(...)` | For simple interface-property cases, prefer `AddPropertySetterCapture<TService, TValue>(...)`. For broader collaborator behavior, prefer a fake or stub registered with `AddType(...)` that captures assigned values, usually with `PropertyValueCapture<TValue>`, or verify the observable downstream behavior instead of the setter interception itself. | When the setter interception is the important behavior and introducing a helper-backed replacement or fake would create more churn than value. |
| `SetupAllProperties()` | For simple interface-property state, prefer `AddPropertyState<TService>(...)`. For broader collaborator behavior or class targets, prefer a concrete fake or lightweight test double with real property state via `AddType(...)`. | When you specifically want mocking-library-managed property backing without creating a custom fake. |
| `CallBase` / partial mock behavior | Prefer a real instance or `AddType(...)` factory for the concrete collaborator. | When the test intentionally relies on partial mocking rather than a real implementation or fake. |
| `out` / `ref` verification with `It.Ref<T>.IsAny` | Prefer wrapping the dependency behind a simpler interface, or assert on the public result / side effect instead of the raw `out` / `ref` interaction. | When the API shape is fixed and the `out` / `ref` interaction itself is important to the test. |

These alternatives are not identical replacements. They are the practical ways to stay productive when the provider-neutral or non-Moq providers do not expose the same mocking semantics.

## Moq provider

Provider package / namespace:

- package: `FastMoq.Provider.Moq`
- namespace for tracked-mock extensions: `FastMoq.Providers.MoqProvider`

Best fit:

- lowest-churn migration from older Moq-shaped suites
- tests that need protected-member access
- tests that need automatic property backing or call-base semantics
- tests that still rely on Moq-native verification or setup patterns

Supported capability flags:

- call base
- setup all properties
- protected members
- invocation tracking
- logger capture

Provider-specific tracked-mock conveniences already exposed on `IFastMock<T>`:

- `AsMoq()`
- `Setup(...)`
- `SetupGet(...)`
- `SetupSequence(...)`
- `Protected()`
- `VerifyLogger(...)`
- `SetupLoggerCallback(...)`

Practical note:

- if you need Moq-only APIs that are not wrapped as `IFastMock<T>` extensions, use `GetOrCreateMock<T>().AsMoq()` first
- keep obsolete `GetMock<T>()` only as a compatibility path when preserving the old Moq shape is materially cheaper than rewriting the test

Common Moq-only pockets:

- `SetupSet(...)`
- direct `Mock<T>` APIs not wrapped by FastMoq provider extensions
- `out` / `ref` verification patterns using `It.Ref<T>.IsAny`

Recommended style:

```csharp
using var providerScope = MockingProviderRegistry.Push("moq");
var dependency = Mocks.GetOrCreateMock<IOrderGateway>();

dependency.Setup(x => x.Publish("alpha"));
dependency.Instance.Publish("alpha");

Mocks.Verify<IOrderGateway>(x => x.Publish("alpha"), TimesSpec.Once);
```

When you need Moq-native behavior that is not exposed as a tracked shortcut, step through `AsMoq()`:

```csharp
Mocks.GetOrCreateMock<IOrderGateway>()
    .AsMoq()
    .SetupSet(x => x.Mode = It.IsAny<string>());
```

For simple `SetupSet(...)` cases on interface properties, the preferred first-party answer is `AddPropertySetterCapture<TService, TValue>(...)`:

```csharp
var modeCapture = Mocks.AddPropertySetterCapture<IOrderGateway, string?>(x => x.Mode);
CreateComponent();

Component.Run();

modeCapture.Value.Should().Be("fast");
```

When the component under test comes from `MockerTestBase<TComponent>`, call `CreateComponent()` after adding the capture unless you registered it in the test base setup path before component creation.

For simple `SetupAllProperties()` cases on interface collaborators, the preferred first-party answer is `AddPropertyState<TService>(...)`:

```csharp
var channel = Mocks.AddPropertyState<IOrderSubmissionChannel>();
CreateComponent();

await Component.SubmitAsync("order-42", expedited: true, CancellationToken.None);

channel.Mode.Should().Be("fast");
```

That keeps the important part of the test explicit: the collaborator needs real property state, not Moq-specific property plumbing.

`AddPropertyState<TService>(...)` keeps its original write-through behavior by default. If the test needs detached property state on the proxy registration without mutating the previously wrapped instance, use `PropertyStateMode.ProxyOnly`:

```csharp
var channel = Mocks.AddPropertyState<IOrderSubmissionChannel>(PropertyStateMode.ProxyOnly);
CreateComponent();

await Component.SubmitAsync("order-42", expedited: true, CancellationToken.None);

channel.Mode.Should().Be("fast");
```

If the collaborator needs more behavior than one captured property, or the target is not an interface, fall back to a fake plus [PropertyValueCapture&lt;TValue&gt;](xref:FastMoq.PropertyValueCapture`1):

```csharp
var modeCapture = new PropertyValueCapture<string?>();
Mocks.AddType<IOrderGateway>(_ => new OrderGatewayStub(modeCapture));

Component.Run();

modeCapture.Value.Should().Be("fast");

sealed class OrderGatewayStub(PropertyValueCapture<string?> capture) : IOrderGateway
{
    public string? Mode
    {
        get => capture.Value;
        set => capture.Record(value);
    }
}
```

That combination keeps the test portable across providers, makes the arranged state explicit, and avoids tying the test to Moq-only setter interception when the important behavior is the assigned value.

Repo-backed references:

- `FastMoq.Tests/MoqProviderExtensionTests.cs`
- `FastMoq.Tests/ProviderTests.cs`

## NSubstitute provider

Provider package / namespace:

- package: `FastMoq.Provider.NSubstitute`
- namespace for tracked-mock extensions: `FastMoq.Providers.NSubstituteProvider`

Best fit:

- suites intentionally written against NSubstitute behavior
- teams that want provider-neutral verification with NSubstitute-backed arrangement code

Supported capability flags:

- invocation tracking
- logger capture

Not supported by the provider capabilities:

- call base
- setup all properties
- protected members

Practical note:

- prefer portable FastMoq verification APIs after arranging substitute behavior
- if a test fundamentally depends on protected members or auto-backed property semantics, NSubstitute is not the right provider for that test shape today

Recommended style:

```csharp
using var providerScope = MockingProviderRegistry.Push("nsubstitute");
var dependency = Mocks.GetOrCreateMock<IOrderGateway>();

dependency.AsNSubstitute().GetValue().Returns("configured");
dependency.Instance.Publish("alpha");

dependency.Received(1).Publish("alpha");
Mocks.Verify<IOrderGateway>(x => x.Publish("alpha"), TimesSpec.Once);
```

Quick Moq-to-NSubstitute translation rules:

- `Setup(x => x.Method()).Returns(value)` becomes `AsNSubstitute().Method().Returns(value)`
- `Setup(x => x.Method(It.IsAny<T>()))` becomes `AsNSubstitute().Method(Arg.Any<T>())`
- `Setup(x => x.Method(It.Is<T>(predicate)))` becomes `AsNSubstitute().Method(Arg.Is<T>(predicate))`
- `Setup(x => x.VoidMethod()).Callback(...)` becomes `AsNSubstitute().When(x => x.VoidMethod()).Do(...)`
- `SetupSequence(...)` becomes `Returns(value1, value2, ...)`
- `SetupGet(...)` becomes a direct property `Returns(...)`

If a migrated test still needs `Protected()` or `CallBase`, that test should usually stay on Moq or move to a fake rather than trying to force an NSubstitute equivalent. For simple interface-property cases, prefer `AddPropertySetterCapture<TService, TValue>(...)` or `AddPropertyState<TService>(...)` before keeping `SetupSet(...)` or `SetupAllProperties()` purely for habit. `PropertyValueCapture<TValue>` remains the default FastMoq answer when the test only needs to observe property assignments rather than exercise Moq itself.

Repo-backed references:

- `FastMoq.Tests/NSubstituteProviderExtensionTests.cs`
- `FastMoq.Tests/ProviderTests.cs`

## Reflection provider

Provider package / namespace:

- built into `FastMoq.Core`
- namespace: `FastMoq.Providers.ReflectionProvider`

Best fit:

- dependency-light provider-neutral baseline
- suites that want FastMoq without bringing in an external mocking library
- tests that only need interface interception plus provider-neutral verification

Supported capability flags:

- invocation tracking

Not supported by the provider capabilities:

- call base
- setup all properties
- protected members
- logger capture

Important implementation constraints:

- interface interception is supported through `DispatchProxy`
- non-interface types fall back to public parameterless construction and do not get full interception behavior
- reflection is the default provider when you do nothing

Practical note:

- this is a baseline provider, not a drop-in replacement for full Moq semantics
- verification is best-effort: only direct method-call expressions are supported, direct constant arguments are compared with `Equals(...)`, and richer matcher or predicate semantics are not interpreted
- if your migrated tests rely on `GetMock<T>()`, direct `Mock<T>` access, `Protected()`, or `VerifyLogger(...)`, they should not stay on reflection

If argument intent matters beyond direct constant equality, prefer a provider that exposes richer matcher behavior or assert on the observable result instead of treating reflection verification as equivalent to Moq or NSubstitute matcher semantics.

Recommended style:

```csharp
using var providerScope = MockingProviderRegistry.Push("reflection");
var dependency = Mocks.GetOrCreateMock<IOrderGateway>();

dependency.Instance.Publish("alpha");

Mocks.Verify<IOrderGateway>(x => x.Publish("alpha"), TimesSpec.Once);
Mocks.VerifyNoOtherCalls<IOrderGateway>();
```

Repo-backed references:

- `FastMoq.Tests/ProviderTests.cs`

## Custom providers

Custom providers should document the same things this page documents for the built-in ones:

- capability flags from `IMockingProviderCapabilities`
- supported tracked-mock convenience APIs, if any
- known unsupported behaviors
- migration caveats for provider-specific patterns

Important extension-model note:

- the built-in providers are not inheritance extension points
- if you want custom behavior, implement a new `IMockingProvider`
- when the change is incremental rather than a full rewrite, prefer a wrapper or decorator provider that delegates to an existing provider and adjusts only the behavior you need

If your team writes its own provider, treat this matrix format as the minimum documentation bar.

## Recommended documentation pattern

For future provider docs, keep the structure consistent:

1. What the provider is for.
2. Which `IMockingProviderCapabilities` flags are true or false.
3. Which provider-specific helpers or namespaces are required.
4. Which common test shapes still need a different provider.

That keeps migration guidance, provider selection, and provider-specific limitations aligned instead of scattering the rules across multiple pages.

# FastMoq Testing Guide

This guide documents the testing patterns that match FastMoq's current behavior in this repository. It is intentionally practical: use it when you need to decide which FastMoq API to reach for in a real test.

If you want reusable AI workflows for new tests, modernization, or migration work instead of writing prompts from scratch, see [AI Prompt Templates](../ai/README.md).

## Start Here

Use these rules first:

1. Use [MockerTestBase&lt;TComponent&gt;](xref:FastMoq.MockerTestBase`1) when you want FastMoq to create the component under test and manage its dependencies.
2. If a `MockerTestBase<TComponent>` test must force a specific constructor, override `ComponentConstructorParameterTypes` first. Use `CreateComponentAction` only when the test needs custom creation logic beyond selecting a signature.
3. Use [Mocks.GetOrCreateMock&lt;T&gt;()](xref:FastMoq.Mocker.GetOrCreateMock``1(FastMoq.MockRequestOptions)) when you want the normal FastMoq tracked mock path for a dependency.
4. Use [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])) when you need to replace FastMoq's default resolution with a specific concrete type, factory, or fixed instance.
5. Use `CreateInstanceByType(...)` when direct `Mocker` usage must pick an exact constructor signature. Do not treat `GetObject<T>()` as the explicit constructor-selection API.
6. Use `CreateTypedServiceProvider(...)`, `CreateTypedServiceScope(...)`, `AddServiceProvider(...)`, and `AddServiceScope(...)` when framework code expects typed service-provider or service-scope behavior rather than a one-object-for-all-types shim.
7. If the constructor uses the same abstraction more than once under different DI service keys, use keyed mocks, keyed registrations, or explicit constructor injection for tests where dependency selection matters.
8. Use [AddKnownType(...)](xref:FastMoq.Mocker.AddKnownType(FastMoq.KnownTypeRegistration,System.Boolean)) when a framework-style type needs special resolution or post-processing behavior.
9. Use [GetMockDbContext&lt;TContext&gt;()](xref:FastMoq.DbContextMockerExtensions.GetMockDbContext``1(FastMoq.Mocker)) when testing EF Core contexts. Do not hand-roll DbContext setup unless you need behavior outside FastMoq's helper.

## Prefer FastMoq-Owned Setup

When FastMoq already has a first-party helper for the dependency or framework primitive, prefer that helper over handwritten setup.

That is true even when the handwritten setup is technically valid. The FastMoq-owned helper usually reduces repeated plumbing, keeps the dependency graph in one place, and makes the test easier for other FastMoq users to recognize.

Use this decision table first:

| If the test wants... | Prefer... | Usually avoid first... |
| --- | --- | --- |
| a tracked collaborator that you will arrange or verify | [GetOrCreateMock&lt;T&gt;()](xref:FastMoq.Mocker.GetOrCreateMock``1(FastMoq.MockRequestOptions)) | creating a mock and then re-registering `mock.Instance` with [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])) |
| a real fixed dependency instance | [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])) with the concrete instance | wrapping that instance in extra local indirection before handing it back to `Mocker` |
| FastMoq's built-in real `IFileSystem` | `GetFileSystem(...)` or `GetObject<IFileSystem>()` | creating a fresh `MockFileSystem` only to satisfy an `IFileSystem` slot when the built-in shared one would do |
| typed DI or scope behavior for framework resolution | `CreateTypedServiceProvider(...)`, `AddServiceProvider(...)`, `CreateTypedServiceScope(...)`, or `AddServiceScope(...)` | ad hoc `new ServiceCollection().BuildServiceProvider()` setup when the typed helper can express the same shape |
| a framework-style built-in type with special behavior | the matching built-in helper or [AddKnownType(...)](xref:FastMoq.Mocker.AddKnownType(FastMoq.KnownTypeRegistration,System.Boolean)) | treating that dependency like an ordinary manual registration first |

### Keep one dependency model per service

For one service in one test flow, pick one main model:

- tracked mock
- fixed concrete instance
- built-in known type
- typed provider registration

Avoid switching the same service back and forth between those models unless the test intentionally rebuilds the graph for a separate scenario.

For example, avoid this pattern:

```csharp
var dependency = Mocks.GetOrCreateMock<IMyService>();
Mocks.AddType(dependency.Instance, replace: true);
```

That creates a tracked mock and then replaces resolution with the mock's instance as a fixed registration, which blurs the test's intent.

### Reset and per-test state

In [MockerTestBase&lt;TComponent&gt;](xref:FastMoq.MockerTestBase`1), each test already starts from a fresh `Mocker`. Before adding extra setup only to reset state, check whether the current harness already gives you a clean per-test instance.

Use extra per-test instance creation only when the test truly needs an intentionally separate state boundary.

## Choose The Narrowest Harness

Do not default every test to the heaviest FastMoq surface. Start with the smallest harness that matches the behavior under test.

| If the test needs... | Prefer... | Why |
| --- | --- | --- |
| plain constructor or pure method behavior with no FastMoq-managed resolution | direct construction plus a plain fake, stub, or explicit dependency instance | keeps the test local and makes non-FastMoq behavior obvious |
| a few DI-managed collaborators without a reusable base class | direct `Mocker` usage | good fit when you want FastMoq resolution without introducing a harness type |
| repeated component tests where FastMoq should create the subject and own the dependency graph | [MockerTestBase&lt;TComponent&gt;](xref:FastMoq.MockerTestBase`1) | keeps creation, tracked verification, and shared setup in one place |
| framework-owned HTTP, ASP.NET, Azure Functions, or service-provider plumbing | the matching first-party helper package | avoids hand-rolled framework shims that analyzers now flag directly |
| real host, container, database, or transport contracts | a separate integration or contract test with explicit real infrastructure | keeps the infrastructure boundary visible instead of hiding it behind partial mocks |

Practical rules:

- keep plain DI tests plain when direct construction is clearer than a reusable harness
- use FastMoq to remove repeated plumbing, not to obscure the real boundary under test
- keep real infrastructure tests thin and explicit instead of turning them into half-real component tests

## Core Mental Model

FastMoq has three distinct resolution paths:

1. Type mapping: explicit registrations added with [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])).
2. Known types: built-in framework helpers for things like `IFileSystem`, `HttpClient`, `DbContext`, and `HttpContext` patterns.
3. Auto-mock / auto-create: default mock creation and constructor injection when no explicit mapping exists.

That distinction matters because the API choice communicates intent.

## Local Wrapper Boundary

Repo-local wrappers can still be useful, but they should compress repeated setup rather than create another verification abstraction on top of FastMoq.

Keep or add a local wrapper when it:

- builds the same request, context, or provider shape across many tests
- re-points an existing suite toward first-party FastMoq helpers with lower call-site churn
- standardizes repeated setup that would otherwise stay verbose and mechanical

Avoid wrapper layers when they:

- only forward to `Mocks.Verify(...)`, `MockingProviderRegistry.Default.Verify(...)`, or `VerifyNoOtherCalls(...)`
- route provider-neutral verification back through `AsMoq().Verify(...)` or provider-specific `Times` adapters
- hide whether the handle being verified is tracked or detached

Analyzer note:

- `FMOQ0031` and `FMOQ0032` are intentionally opinionated here: they discourage verification-only wrappers because those wrappers blur tracked-versus-detached intent without adding behavior

### Equality intent in matcher-style expressions

When you do step into provider-native matcher predicates, make the equality intent obvious instead of relying on readers to infer what `==` means for a particular type.

- record or value-object arguments can legitimately use value equality, for example `command => command == expectedCommand` or `command => command.Equals(expectedCommand)` when the type is intentionally value-based
- mutable class arguments usually need either identity or property-based intent, for example `session => ReferenceEquals(session, expectedSession)` when the exact instance matters
- if neither pure value equality nor identity is the real requirement, match the stable property that explains the behavior, such as `message => message.Id == expectedId`

That same rule applies whether the arrange side uses `It.Is(...)`, `Arg.Is(...)`, or another provider-native matcher surface.

## [GetOrCreateMock&lt;T&gt;()](xref:FastMoq.Mocker.GetOrCreateMock``1(FastMoq.MockRequestOptions)) vs [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[]))

These are not interchangeable.

### Use [GetOrCreateMock&lt;T&gt;()](xref:FastMoq.Mocker.GetOrCreateMock``1(FastMoq.MockRequestOptions)) when

- You want the dependency to stay on the default auto-mock path.
- You only need to arrange or verify behavior.
- You want the dependency tracked as the mock FastMoq would have created anyway.

The example below assumes the Moq provider extensions are in use for the arrange step. The tracked-mock concept is provider-first, but the `.Setup(...)` syntax itself is provider-specific. See [Provider Capabilities](./provider-capabilities.md) when you need the equivalent arrangement style for another provider.

```csharp
var repoMock = Mocks.GetOrCreateMock<IOrderRepository>();
repoMock.Setup(x => x.Load(123)).Returns(order);
```

### When [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])) is the right tool

- You need a specific implementation rather than a default mock.
- You need to control constructor arguments or non-public construction.
- You want a fixed singleton-like instance returned for that type.
- You need to disambiguate multiple concrete implementations.

```csharp
Mocks.AddType<IClock>(_ => new FakeClock(DateTimeOffset.Parse("2026-04-01T12:00:00Z")));
```

### Practical rule

If the dependency is still conceptually a mock, prefer [GetOrCreateMock&lt;T&gt;()](xref:FastMoq.Mocker.GetOrCreateMock``1(FastMoq.MockRequestOptions)). If you are changing how the type is resolved, prefer [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])).

Migration guardrail:

- do not rewrite a tracked helper to [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])) when the same service still flows through `GetObject<T>()`, `GetRequiredTrackedMock<T>()`, `GetMockModel<T>()`, `AddPropertyState<TService>(...)`, or `AddPropertySetterCapture<TService, TValue>(...)`
- that is still a tracked FastMoq dependency, not a concrete type-map override

Analyzer note:

- `FMOQ0022` warns when an `AddType(...)` rewrite comes from a tracked mock/object path and the same file still uses tracked-resolution APIs or property helpers for that service

### Ambiguous interface and detached fallback decision table

Current limitation:

- `GetOrCreateMock<T>()` can throw `AmbiguousImplementationException` for interface types when multiple concrete implementations are loaded and FastMoq cannot infer which implementation the test means

Treat that as a resolution-shape decision, not as a reason to keep retrying unkeyed tracked mocks.

| If the test needs... | Preferred path | Why |
| --- | --- | --- |
| one tracked dependency in the current `Mocker` | [GetOrCreateMock&lt;T&gt;()](xref:FastMoq.Mocker.GetOrCreateMock``1(FastMoq.MockRequestOptions)) | default tracked path reused by later `GetObject<T>()` and `Verify(...)` calls |
| a second distinct role of the same abstraction in the tracked graph | keyed [GetOrCreateMock&lt;T&gt;()](xref:FastMoq.Mocker.GetOrCreateMock``1(FastMoq.MockRequestOptions)), [AddKeyedType(...)](https://help.fastmoq.com/api/FastMoq.Mocker.html), or `CreateFastMock<T>()` when you intentionally want a new tracked registration | keeps public vs private or primary vs secondary roles separate inside the parent `Mocker` |
| a detached collaborator or manual wiring outside the tracked graph | `CreateStandaloneFastMock<T>()` or `MockingProviderRegistry.Default.CreateMock<T>()` | creates a provider-first handle that is not registered as the parent tracked dependency |
| a specific concrete implementation behind an ambiguous interface | [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])) or [AddKeyedType(...)](https://help.fastmoq.com/api/FastMoq.Mocker.html) | makes the intended implementation explicit instead of asking FastMoq to guess |

## Tracked vs Standalone Fast Mocks

Use `GetOrCreateMock<T>()` for the normal tracked dependency inside the current `Mocker`.

Use `CreateFastMock<T>()` when you intentionally want to create a new tracked registration immediately.

Use `CreateStandaloneFastMock<T>()` when the test needs a detached provider-selected mock handle that will be wired manually and must not be added to the parent tracked collection.

Important behavior differences:

- `GetOrCreateMock<T>()` reuses an existing tracked mock for the same unkeyed type or service key.
- `CreateFastMock<T>()` registers the new mock in the current `Mocker`; for unkeyed mocks it throws if that type is already tracked.
- `CreateStandaloneFastMock<T>()` does not register the mock, so it is the provider-first replacement for legacy detached mock creation.
- if you need two independently tracked collaborators of the same abstraction in one component graph, use keyed mocks or a separate `Mocker` instead of a second unkeyed `CreateFastMock<T>()` call.

```csharp
var trackedGateway = Mocks.GetOrCreateMock<IEmailGateway>();
var detachedGateway = Mocks.CreateStandaloneFastMock<IEmailGateway>();

var manuallyWiredService = new CheckoutService(detachedGateway.Instance);
```

### Detached verification and async-returning members

Tracked mocks use `Mocks.Verify<T>(...)` because the `Mocker` already owns the registration.

Detached mocks use `MockingProviderRegistry.Default.Verify(...)` because the handle is not registered in the parent tracked collection.

That provider-neutral detached verification shape still works for methods that return `Task` or `Task<T>` when the verification expression intentionally discards the return value:

```csharp
var emailGateway = MockingProviderRegistry.Default.CreateMock<IEmailGateway>();
var service = new CheckoutService(emailGateway.Instance);

await service.NotifyFinanceAsync(CancellationToken.None);

MockingProviderRegistry.Default.Verify(
    emailGateway,
    x => x.SendAsync("finance@contoso.test", CancellationToken.None),
    TimesSpec.Once);

MockingProviderRegistry.Default.VerifyNoOtherCalls(emailGateway);
```

Use tracked verification when the dependency lives in the current `Mocker`. Use detached verification when the test created an out-of-band `IFastMock<T>` handle directly.

Use `CreateFastMock<T>()` only when you want the new registration to become the tracked mock returned later by `GetOrCreateMock<T>()`.

## Typed IServiceProvider Helpers

Framework-heavy tests should not fake `IServiceProvider` with a single mocked `GetService(Type)` callback that returns the same object for every request.

That shape is convenient, but it hides real failures when code asks for multiple service types such as `ILoggerFactory`, `IOptions<T>`, or a concrete singleton.

Use `CreateTypedServiceProvider(...)` when you need a real provider instance:

```csharp
var instanceServices = Mocks.CreateTypedServiceProvider(services =>
{
    services.AddLogging();
    services.AddOptions();
    services.AddSingleton(new Uri("https://fastmoq.dev"));
});
```

If framework-owned resolution should fall back to the current `Mocker` for unregistered collaborators, opt in explicitly:

```csharp
var instanceServices = Mocks.CreateTypedServiceProvider(
    services => services.AddLogging(),
    includeMockerFallback: true);
```

Use `AddServiceProvider(...)` when the system under test resolves `IServiceProvider` or `IServiceScopeFactory` from the current [Mocker](xref:FastMoq.Mocker):

```csharp
Mocks.AddServiceProvider(services =>
{
    services.AddLogging();
    services.AddSingleton(new WidgetClock());
});
```

`AddServiceProvider(...)` registers the typed provider itself and, when the built container exposes them, also registers `IServiceScopeFactory` and `IServiceProviderIsService` for the current `Mocker`.

Use `CreateTypedServiceScope(...)` when the test needs an actual scope instance or wants to verify scoped lifetimes directly:

```csharp
using var scope = Mocks.CreateTypedServiceScope(services =>
{
    services.AddScoped<ScopedWidgetContext>();
});

var scopedService = scope.ServiceProvider.GetRequiredService<ScopedWidgetContext>();
```

Use `AddServiceScope(...)` when the current `Mocker` should expose a scope and its scope-owned provider:

```csharp
using var scope = Mocks.CreateTypedServiceScope(services =>
{
    services.AddScoped<ScopedWidgetContext>();
});

Mocks.AddServiceScope(scope, replace: true);
```

If a helper already has a real provider and only needs a fixed scope plus a matching scope factory, use the provider-backed overload:

```csharp
var provider = Mocks.CreateTypedServiceProvider(services =>
{
    services.AddScoped<ScopedWidgetContext>();
});

Mocks.AddServiceScope(provider, replace: true);
```

That overload exposes the supplied `IServiceProvider`, a fixed `IServiceScope`, and an `IServiceScopeFactory` that returns that registered scope, which maps well to older `scope.SetupGet(x => x.ServiceProvider)` and `scopeFactory.Setup(x => x.CreateScope())` shim patterns.

If a constructor takes `IServiceScopeFactory`, prefer this shape:

```csharp
var fileSystem = Mocks.GetFileSystem();

Mocks.AddServiceProvider(services =>
{
    services.AddLogging();
    services.AddOptions();
    services.AddSingleton<IFileSystem>(fileSystem);
    services.AddSingleton<ArchiveService>();
}, replace: true);

var scopeFactory = Mocks.GetRequiredObject<IServiceScopeFactory>();
```

Reuse `GetFileSystem()` when you want FastMoq's shared in-memory file system to stay aligned across constructor injection and the typed provider. Use `AddType<IFileSystem>(...)` only when you intentionally want to replace that shared instance with a custom file system.

Instead of building a provider manually and registering only `provider.GetRequiredService<IServiceScopeFactory>()`, keep the full typed provider registered so constructor injection, nested framework resolution, and service-scope behavior stay aligned.

When framework code should resolve a mix of real DI registrations and normal FastMoq collaborators, use `includeMockerFallback: true` on `CreateTypedServiceProvider(...)`, `CreateTypedServiceScope(...)`, `AddServiceProvider(...)`, or `AddServiceScope(...)`.

For Azure-oriented tests that also need configuration defaults, prefer `CreateAzureServiceProvider(...)` or `AddAzureServiceProvider(...)` from `FastMoq.Azure.DependencyInjection` instead of repeating `AddLogging()`, `AddOptions()`, and `IConfiguration` setup in every test.

Use `CreateFunctionContextInstanceServices(...)` and `AddFunctionContextInstanceServices(...)` for Azure Functions worker tests instead of hand-writing `FunctionContext.InstanceServices` plumbing:

```csharp
using FastMoq.AzureFunctions.Extensions;

Mocks.AddFunctionContextInstanceServices(services =>
{
    services.AddSingleton(new WidgetClock());
});

var context = Mocks.GetObject<FunctionContext>();
var clock = context.InstanceServices.GetRequiredService<WidgetClock>();
```

For HTTP-trigger tests, use `CreateHttpRequestData(...)` and `CreateHttpResponseData(...)` to build concrete worker request or response objects instead of hand-rolling `HttpRequestData` and `HttpResponseData` doubles:

```csharp
using FastMoq.AzureFunctions.Extensions;

var request = Mocks.CreateHttpRequestData(builder => builder
    .WithMethod("POST")
    .WithUrl("https://localhost/api/widgets?mode=test")
    .WithHeader("x-correlation-id", "123")
    .WithJsonBody(new CreateWidgetRequest
    {
        Name = "demo",
    }));

var payload = await request.ReadFromJsonAsync<CreateWidgetRequest>();
var response = request.CreateResponse();
```

Use `ReadBodyAsStringAsync(...)` and `ReadBodyAsJsonAsync<T>(...)` when you want to assert request or response bodies without manually rewinding the underlying stream.

Package note:

- `CreateTypedServiceProvider(...)` and `AddServiceProvider(...)` remain part of `FastMoq.Core`
- `CreateTypedServiceScope(...)` and `AddServiceScope(...)` remain part of `FastMoq.Core`
- direct `FastMoq.Core` consumers should add `FastMoq.AzureFunctions` and import `FastMoq.AzureFunctions.Extensions` before using `CreateFunctionContextInstanceServices(...)`, `AddFunctionContextInstanceServices(...)`, `CreateHttpRequestData(...)`, or `CreateHttpResponseData(...)`
- the aggregate `FastMoq` package includes the Azure Functions helper package already

Analyzer note:

- `FMOQ0013` warns on direct `GetOrCreateMock<IServiceProvider>()`, `GetMock<IServiceProvider>()`, `GetRequiredMock<IServiceProvider>()`, `IServiceScopeFactory` or `IServiceScope` shims, and manual scope-factory extraction so those patterns migrate toward the typed helper path.

## Construction APIs

FastMoq still supports the older entry points:

- [CreateInstance&lt;T&gt;(...)](xref:FastMoq.Mocker.CreateInstance``1(FastMoq.InstanceCreationFlags,System.Object[]))
- [CreateInstanceByType&lt;T&gt;(...)](xref:FastMoq.Mocker.CreateInstanceByType``1(FastMoq.InstanceCreationFlags,System.Type[]))

For new code, prefer the flags-based constructor overloads when you need an explicit override:

```csharp
var component = Mocks.CreateInstanceByType<MyComponent>(
    InstanceCreationFlags.AllowNonPublicConstructorFallback,
    typeof(int),
    typeof(string));
```

If constructor selection should be restricted to public constructors, use the explicit flag:

```csharp
var component = Mocks.CreateInstance<MyComponent>(InstanceCreationFlags.PublicConstructorsOnly);
```

If you want to change the default constructor-fallback policy for the whole [Mocker](xref:FastMoq.Mocker) instance, use:

```csharp
Mocks.Policy.DefaultFallbackToNonPublicConstructors = false;
```

### Internal And Protected Components

FastMoq can create components that use internal or protected constructors, but C# still enforces compile-time accessibility on the test type itself.

If a test framework requires the outward test class to remain public, use a public test class plus an internal FastMoq harness:

```csharp
public class InternalOrderRulesTests
{
    [Fact]
    public void PublicTestClass_CanExercise_InternalService()
    {
        using var harness = new InternalOrderRulesHarness();

        harness.Sut.IsPriority("P1").Should().BeTrue();
    }
}

internal sealed class InternalOrderRulesHarness : MockerTestBase<InternalOrderRules>
{
    protected override Action<MockerPolicyOptions>? ConfigureMockerPolicy => policy =>
    {
        policy.DefaultFallbackToNonPublicConstructors = false;
    };

    protected override InstanceCreationFlags ComponentCreationFlags
        => InstanceCreationFlags.AllowNonPublicConstructorFallback;

    internal InternalOrderRules Sut => Component;
}

internal sealed class InternalOrderRules
{
    internal InternalOrderRules()
    {
    }

    public bool IsPriority(string code)
    {
        return code == "P1";
    }
}
```

If the component under test lives in another assembly, expose it to the test assembly with `InternalsVisibleTo` or an equivalent visibility rule first.
FastMoq handles constructor resolution and injection at runtime; it does not bypass the compiler rule that prevents `public class MyTests : MockerTestBase<InternalType>` when `InternalType` is less accessible than the public test type.

### Explicit Constructor Selection In Tests

When a test needs a specific constructor, prefer a test-side override first.
That keeps constructor choice inside the test harness and avoids changing production code only to satisfy test setup.
If the selected constructor depends on `IServiceProvider` or `IServiceScopeFactory`, pair the constructor-selection hook with `AddServiceProvider(...)` from the earlier typed-provider section instead of registering only a manually extracted scope factory.

For `MockerTestBase<TComponent>`, override `ComponentConstructorParameterTypes` when you want a specific signature but still want the default FastMoq creation path:

```csharp
internal sealed class OrderRulesTestBase : MockerTestBase<OrderRules>
{
    protected override Type?[]? ComponentConstructorParameterTypes
        => new Type?[] { typeof(IFileSystem), typeof(string) };
}
```

Use `CreateComponentAction` when the test needs full control over creation, custom argument values, or logic that cannot be expressed as a parameter-type signature:

```csharp
internal sealed class OrderRulesTestBase : MockerTestBase<OrderRules>
{
    protected override Func<Mocker, OrderRules> CreateComponentAction => mocker =>
        mocker.CreateInstanceByType<OrderRules>(
            InstanceCreationFlags.AllowNonPublicConstructorFallback,
            typeof(IFileSystem),
            typeof(string))!;
}
```

The older `MockerTestBase(params Type[] createArgumentTypes)` constructor still works, but the override-based hook is the better default for new test bases because it keeps constructor intent local to the derived type.

### Constructor Ambiguity And Preferred Constructors

FastMoq still throws by default when multiple same-rank constructors remain viable after candidate filtering. That preserves the current behavior for existing suites.

If you control the production type and want it to advertise a preferred default constructor for all callers, you can mark it explicitly with `[PreferredConstructor]`.
That is secondary to the test-side hooks above and is most useful when constructor preference is part of the component's intended public shape, not just a test need:

```csharp
internal sealed class OrderRules
{
    [PreferredConstructor]
    public OrderRules()
    {
    }

    public OrderRules(IFileSystem fileSystem)
    {
    }
}
```

If you want FastMoq to fall back to a parameterless constructor when ambiguity remains, opt in explicitly through policy or a per-call flag:

```csharp
Mocks.Policy.DefaultConstructorAmbiguityBehavior = ConstructorAmbiguityBehavior.PreferParameterlessConstructor;

var component = Mocks.CreateInstance<MyComponent>(
    InstanceCreationFlags.PreferParameterlessConstructorOnAmbiguity);
```

FastMoq writes constructor-selection diagnostics into `Mocks.LogEntries` when `[PreferredConstructor]` is used or when ambiguity fallback is applied.

## Optional Constructor And Method Parameters

FastMoq now has an explicit option for [optional-parameter behavior](xref:FastMoq.OptionalParameterResolutionMode).

Default behavior:

- optional parameters use their declared default value when one exists
- otherwise FastMoq passes `null`

If you want FastMoq to resolve optional parameters through the normal mock/object pipeline, opt in explicitly.

### Constructor creation

```csharp
Mocks.OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker;

var component = Mocks.CreateInstance<MyComponent>();
```

### [MockerTestBase&lt;TComponent&gt;](xref:FastMoq.MockerTestBase`1)

For SUT creation through [MockerTestBase&lt;TComponent&gt;](xref:FastMoq.MockerTestBase`1), override `ComponentCreationFlags`:

```csharp
protected override InstanceCreationFlags ComponentCreationFlags
    => InstanceCreationFlags.ResolveOptionalParametersViaMocker;
```

### Delegate or reflected invocation

Use [InvocationOptions](xref:FastMoq.InvocationOptions) when calling helpers that fill omitted method parameters:

```csharp
var result = Mocks.CallMethod<MyResult>(new InvocationOptions
{
    OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker,
}, (Func<IMyDependency?, MyResult>)CreateResult);
```

```csharp
var result = Mocks.InvokeMethod(
    new InvocationOptions
    {
        OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker,
    },
    target,
    nameof(TargetType.Run));
```

If reflected method fallback should be controlled explicitly, use:

```csharp
var result = Mocks.InvokeMethod(
    new InvocationOptions
    {
        FallbackToNonPublicMethods = false,
    },
    target,
    nameof(TargetType.Run));
```

If you want to change the default reflected-method fallback policy for the whole [Mocker](xref:FastMoq.Mocker) instance, use:

```csharp
Mocks.Policy.DefaultFallbackToNonPublicMethods = false;
```

`MockOptional` is now obsolete and should be treated only as a compatibility alias for [OptionalParameterResolutionMode.ResolveViaMocker](xref:FastMoq.OptionalParameterResolutionMode.ResolveViaMocker).

## Built-In Known Types

FastMoq includes built-in handling for a small set of framework-heavy types.

That built-in resolution policy is now explicit. `FailOnUnconfigured` no longer silently changes which built-ins are available by itself.

Use [EnabledBuiltInTypeResolutions](xref:FastMoq.MockerPolicyOptions.EnabledBuiltInTypeResolutions) when you want to override the built-in defaults for a [Mocker](xref:FastMoq.Mocker) instance:

```csharp
Mocks.Policy.EnabledBuiltInTypeResolutions =
    BuiltInTypeResolutionFlags.FileSystem |
    BuiltInTypeResolutionFlags.HttpClient |
    BuiltInTypeResolutionFlags.Uri |
    BuiltInTypeResolutionFlags.DbContext;
```

`Strict` compatibility still stamps the older strict-era defaults onto that policy surface, but new code can now control those pieces independently.

## Mock Creation Defaults

For provider-backed mock creation, use the [Mocker](xref:FastMoq.Mocker)-level default when you want new mocks to be created as strict or loose without depending on the broader compatibility bundle:

```csharp
Mocks.Policy.DefaultStrictMockCreation = true;
```

This affects provider-backed and legacy mock creation helpers. It does not apply to [GetMockDbContext&lt;TContext&gt;()](xref:FastMoq.DbContextMockerExtensions.GetMockDbContext``1(FastMoq.Mocker)), which remains on the supported DbContext helper behavior.

If you are using [MockerTestBase&lt;TComponent&gt;](xref:FastMoq.MockerTestBase`1), apply the same defaults before component creation with:

```csharp
protected override Action<MockerPolicyOptions>? ConfigureMockerPolicy => policy =>
{
    policy.DefaultStrictMockCreation = true;
};
```

### `IFileSystem`

Use `GetFileSystem()` when you want FastMoq's shared in-memory file system explicitly. `GetObject<IFileSystem>()` resolves that same shared instance when built-in file-system resolution is enabled and you have not already registered or created an `IFileSystem` dependency.

Use this when you want a real in-memory file system quickly:

```csharp
var fileSystem = Mocks.GetFileSystem();
fileSystem.File.WriteAllText("/tmp/test.txt", "hello");
```

If you want mock arrangement and verification instead, stay on the mock path:

```csharp
Mocks.GetOrCreateMock<IFileSystem>()
    .Setup(x => x.File.Exists("orders.json"))
    .Returns(true);
```

FastMoq can automatically provide a built-in `IFileSystem` backed by its shared in-memory `MockFileSystem` when the built-in file-system resolution is enabled and you have not already registered `IFileSystem` explicitly.

Reach for `AddType<IFileSystem>(...)` only when you intentionally need to replace that shared file system with a custom or isolated instance.

If you need the wider filesystem abstraction family (`IFile`, `IPath`, `IDirectory`, and related factories) to resolve coherently alongside that shared file system, call [AddFileSystemAbstractionMapping()](https://help.fastmoq.com/api/FastMoq.Mocker.html).

### `HttpClient`

FastMoq has a built-in `HttpClient` helper path. Every new `Mocker` starts with a shared `HttpClient` plus a lightweight `IHttpClientFactory` compatibility registration backed by the same provider-neutral handler.

Use that built-in path when the subject depends on `HttpClient` directly or only needs `IHttpClientFactory.CreateClient(...)` to hand back a client. Prefer `WhenHttpRequest(...)` and `WhenHttpRequestJson(...)` for provider-neutral response setup instead of manually composing handlers for every test.

Use `GetObject<IHttpClientFactory>()`, `GetRequiredObject<IHttpClientFactory>()`, or normal constructor injection when you want that built-in factory. Do not call `GetOrCreateMock<IHttpClientFactory>()` unless you intentionally want to replace the built-in compatibility factory with a tracked mock.

The built-in compatibility factory accepts the requested client name but does not apply per-name configuration.

If you call `CreateHttpClient(...)` again later with a different base address or default response, FastMoq updates that built-in compatibility factory and handler to match the latest helper call.

If you intentionally replace `IHttpClientFactory` with `GetOrCreateMock<IHttpClientFactory>()` or `AddType<IHttpClientFactory>(...)`, that replacement wins and you own `CreateClient(...)` setup yourself.

If the subject depends on named-client, typed-client, or `AddHttpClient(...)` configuration semantics, register your own `IHttpClientFactory` or typed provider-backed container instead of relying on the built-in compatibility factory.

### `DbContext`

Use [GetMockDbContext&lt;TContext&gt;()](xref:FastMoq.DbContextMockerExtensions.GetMockDbContext``1(FastMoq.Mocker)) as the default entry point.

If you consume the aggregate `FastMoq` package, the database helpers remain available with the same API shape. If you consume `FastMoq.Core` directly, install `FastMoq.Database` for EF-specific helpers.

When you need to choose between pure mock behavior and a real EF in-memory context, use [GetDbContextHandle&lt;TContext&gt;(...)](https://help.fastmoq.com/api/FastMoq.DbContextMockerExtensions.html) with [DbContextHandleOptions&lt;TContext&gt;](https://help.fastmoq.com/api/FastMoq.DbContextHandleOptions-1.html). The default mode remains [MockedSets](https://help.fastmoq.com/api/FastMoq.DbContextTestMode.html), and [GetMockDbContext&lt;TContext&gt;()](xref:FastMoq.DbContextMockerExtensions.GetMockDbContext``1(FastMoq.Mocker)) is now the convenience wrapper over that default.

```csharp
protected override Action<Mocker> SetupMocksAction => mocker =>
{
    var dbContextMock = mocker.GetMockDbContext<ApplicationDbContext>();
    dbContextMock.Object.Database.EnsureCreated();
};
```

Recommended pattern:

1. Create the context mock with [GetMockDbContext&lt;TContext&gt;()](xref:FastMoq.DbContextMockerExtensions.GetMockDbContext``1(FastMoq.Mocker)).
2. Seed test data through the resolved context object or `dbContextMock.Object` before calling the system under test.
3. If you need the tracked provider-first handle for the same context, call `GetOrCreateMock<TContext>()` after the helper has tracked it; the returned mock exposes that same tracked context.

This is the supported path for EF Core tests in this repo. It keeps DbSet setup and context creation aligned with the framework's existing helper behavior.

Real in-memory example:

```csharp
var handle = mocker.GetDbContextHandle<ApplicationDbContext>(new DbContextHandleOptions<ApplicationDbContext>
{
    Mode = DbContextTestMode.RealInMemory,
});

handle.Context.Database.EnsureCreated();
```

Known-type override example:

```csharp
Mocks.AddKnownType<ApplicationDbContext>(
    managedInstanceFactory: (mocker, _) => mocker.GetDbContextHandle<ApplicationDbContext>(new DbContextHandleOptions<ApplicationDbContext>
    {
        Mode = DbContextTestMode.RealInMemory,
    }).Context,
    replace: true);
```

Use that pattern when the test needs the context to resolve through FastMoq's known-type pipeline rather than through a one-off `AddType(...)` mapping.

### `HttpContext` and `IHttpContextAccessor`

FastMoq applies built-in setup for `HttpContext`, `IHttpContextAccessor`, and `HttpContextAccessor` so common web tests have a usable context object without repetitive setup.

When you want explicit test setup for headers, query strings, or authenticated users, use the `FastMoq.Web.Extensions` helpers instead of wiring those pieces by hand.

Package note:

- if your project references the aggregate `FastMoq` package, the web helpers are already available
- if your project references `FastMoq.Core` directly, add `FastMoq.Web` before using `FastMoq.Web.Extensions`
- for the broader package-choice rules, see [Getting Started installation and package choices](./README.md#package-choices)

## Controller Testing

For controller and request-driven tests, prefer building the request shape explicitly with the web helpers.

```csharp
using FastMoq.Web.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class OrdersControllerTests : MockerTestBase<OrdersController>
{
    protected override Action<Mocker>? SetupMocksAction => mocker =>
    {
        var httpContext = mocker
            .CreateHttpContext("Admin")
            .SetRequestHeader("X-Correlation-Id", "corr-123")
            .SetQueryParameter("includeInactive", "true");

        mocker.AddHttpContextAccessor(httpContext);
        mocker.AddHttpContext(httpContext);
    };

    [Fact]
    public async Task Get_ShouldReturnOrders_WhenRequestIsValid()
    {
        var requestContext = Mocks.GetObject<HttpContext>();
        Component.ControllerContext = Mocks.CreateControllerContext(requestContext);

        Mocks.GetOrCreateMock<IOrderService>()
            .Setup(x => x.GetOrdersAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new OrderDto { Id = 42 }]);

        var result = await Component.Get();

        result.GetOkObjectResult()
            .Value
            .Should()
            .NotBeNull();
    }
}
```

Practical rules:

1. Use [CreateHttpContext(...)](https://help.fastmoq.com/api/FastMoq.Web.Extensions.TestWebExtensions.html) when you need a reusable request object for middleware or accessors.
2. Use [AddHttpContext(...)](https://help.fastmoq.com/api/FastMoq.Web.Extensions.TestWebExtensions.html) or [AddHttpContextAccessor(...)](https://help.fastmoq.com/api/FastMoq.Web.Extensions.TestWebExtensions.html) when the system under test resolves those types from DI.
3. Use `SetRequestHeader(...)`, `SetRequestHeaders(...)`, `SetQueryString(...)`, `SetQueryParameter(...)`, or `SetQueryParameters(...)` to make request intent obvious in the test.
4. Use [CreateControllerContext(...)](https://help.fastmoq.com/api/FastMoq.Web.Extensions.TestWebExtensions.html) when the controller itself reads from `ControllerContext.HttpContext.User`.
5. Use [GetOkObjectResult()](https://help.fastmoq.com/api/FastMoq.Web.Extensions.TestWebExtensions.html), [GetBadRequestObjectResult()](https://help.fastmoq.com/api/FastMoq.Web.Extensions.TestWebExtensions.html), [GetConflictObjectResult()](https://help.fastmoq.com/api/FastMoq.Web.Extensions.TestWebExtensions.html), and [GetObjectResultContent&lt;T&gt;()](https://help.fastmoq.com/api/FastMoq.Web.Extensions.TestWebExtensions.html) to keep result assertions short.

Quick decision table:

| If the test needs... | Prefer... | Why |
| --- | --- | --- |
| role-only authenticated user setup | `SetupClaimsPrincipal(params roles)` or `CreateControllerContext(params roles)` | Fast path for common controller and request tests |
| exact custom claims | `SetupClaimsPrincipal(claims, options)` | Use `IncludeDefaultIdentityClaims = false` when exact claim preservation matters |
| controller reads `ControllerContext.HttpContext.User` | `CreateControllerContext(...)` | Keeps controller user setup aligned with the underlying `HttpContext` |
| `HttpContext` or `IHttpContextAccessor` from DI | `AddHttpContext(...)` or `AddHttpContextAccessor(...)` | Replaces hand-rolled accessor wiring |

Important note for custom-claim tests:

- role-only helpers and custom-claim helpers are not identical because `FastMoq.Web` adds compatibility identity claims by default
- if the test is asserting exact `ClaimTypes.Name`, email, or related identity values, pass `IncludeDefaultIdentityClaims = false`
- if your suite already has local wrappers for these helpers, re-point those wrappers to `FastMoq.Web` first and simplify call sites later

## Accessor And Middleware Testing

The same helper surface works for middleware and `IHttpContextAccessor`-driven services.

```csharp
using FastMoq.Web.Extensions;
using Microsoft.AspNetCore.Http;

public class RequestContextReaderTests : MockerTestBase<RequestContextReader>
{
    protected override Action<Mocker>? SetupMocksAction => mocker =>
    {
        var httpContext = mocker
            .CreateHttpContext("Admin")
            .SetRequestHeader("X-Correlation-Id", "corr-456")
            .SetQueryParameter("tenant", "contoso");

        mocker.AddHttpContextAccessor(httpContext);
    };

    [Fact]
    public void Read_ShouldReturnHeaderAndQueryValues()
    {
        var result = Component.Read();

        result.CorrelationId.Should().Be("corr-456");
        result.Tenant.Should().Be("contoso");
    }
}
```

Use this pattern when the system under test reads directly from `IHttpContextAccessor`, middleware `InvokeAsync(HttpContext)`, or request headers/query collections without needing MVC controller infrastructure.

## Keyed Services And Same-Type Dependencies

When the constructor under test takes the same interface more than once and production distinguishes those parameters with DI service keys, a single unkeyed [GetOrCreateMock&lt;T&gt;()](xref:FastMoq.Mocker.GetOrCreateMock``1(FastMoq.MockRequestOptions)) or `GetMock<T>()` collapses those dependencies into one double.

That is fine for ordinary tests that only need "some `IBlobRepository`", but it is too weak for tests where public vs private, primary vs secondary, or similar selection is part of the behavior.

Use separate doubles when:

- the constructor uses keyed DI attributes such as `FromKeyedServices`
- swapping the dependencies would be a user-visible bug
- the test is asserting routing or repository-selection behavior

Use keyed tracked mocks when the dependency is still conceptually a mock:

```csharp
var publicRepo = Mocks.GetOrCreateMock<IBlobRepository>(new MockRequestOptions
{
    ServiceKey = "public",
});

var privateRepo = Mocks.GetOrCreateMock<IBlobRepository>(new MockRequestOptions
{
    ServiceKey = "private",
});

var controller = Mocks.CreateInstance<BlobAccessController>();
```

FastMoq keeps those tracked mocks separate. When the constructor uses `[FromKeyedServices("public")]` and `[FromKeyedServices("private")]`, [CreateInstance(...)](xref:FastMoq.Mocker.CreateInstance``1(FastMoq.InstanceCreationFlags,System.Object[])) and [MockerTestBase&lt;TComponent&gt;](xref:FastMoq.MockerTestBase`1) resolve the matching keyed dependency instead of collapsing them to one unkeyed instance.

Use [AddKeyedType(...)](https://help.fastmoq.com/api/FastMoq.Mocker.html) and [GetKeyedObject&lt;T&gt;()](https://help.fastmoq.com/api/FastMoq.Mocker.html) when a fake or fixed instance reads better than a mock:

```csharp
var publicRepo = new FakeBlobRepository();
var privateRepo = new FakeBlobRepository();

Mocks.AddKeyedType<IBlobRepository>("public", publicRepo);
Mocks.AddKeyedType<IBlobRepository>("private", privateRepo);

var controller = Mocks.CreateInstance<BlobAccessController>();
var resolvedPublicRepo = Mocks.GetKeyedObject<IBlobRepository>("public");
```

If repo selection itself is the behavior under test, explicit constructor injection with two separate doubles is equally valid and is often the clearest test shape.

### Choosing explicit doubles vs keyed FastMoq setup

Pick explicit constructor injection with two separate doubles when:

- the test is about controller or service logic, not DI wiring
- you want the two roles to be obvious in the arrange step
- manual construction makes the test shorter or easier to read
- you do not need the test to prove that keyed constructor metadata is honored

Pick keyed FastMoq setup when:

- you want to keep [MockerTestBase&lt;TComponent&gt;](xref:FastMoq.MockerTestBase`1) or [CreateInstance(...)](xref:FastMoq.Mocker.CreateInstance``1(FastMoq.InstanceCreationFlags,System.Object[])) as the construction path
- you want the test to mirror the production keyed DI contract closely
- the constructor metadata itself is part of what you are trying to protect
- the suite already uses FastMoq auto-construction heavily and keyed setup removes custom wiring noise

Practical default:

- use explicit separate doubles for most behavior-focused unit tests
- use keyed FastMoq setup for tests that should fail if the keyed constructor resolution changes
- if both concerns matter, keep most tests explicit and add one small keyed wiring-focused test

Ordinary unit tests can stay ordinary. The important rule is: if swapping the keyed dependencies would be a bug, the test should not collapse them into one double.

If the problem is not keyed DI but an `AmbiguousImplementationException` from multiple loaded implementations of the same interface, take the same explicit approach: use `AddType(...)` when the test means one concrete implementation, or step outside the parent tracked graph with `CreateStandaloneFastMock<T>()` or `MockingProviderRegistry.Default.CreateMock<T>()` when you really need a detached double.

Analyzer note:

- `FMOQ0015` warns on unkeyed `GetOrCreateMock<T>()`, `GetMock<T>()`, `GetRequiredMock<T>()`, and `AddType<T>(...)` usage when the target type has multiple keyed constructor parameters of that same abstraction and the current file is not already using keyed setup.

## MockModel Equality Semantics

`GetMockModel<T>()` is useful for inspection, but it is not an identity contract for distinct doubles.

Current runtime behavior is intentionally type-oriented:

- `MockModel.Equals(...)` and the `==` / `!=` operators compare the mocked type name (`Type.Name`) case-insensitively.
- `GetHashCode()` comes from the mocked `Type`.
- `CompareTo(...)` sorts by `Type.FullName`.
- tracked-versus-standalone status, service keys, and provider-native object identity are not part of that equality test.

Practical rule:

- use `MockModel` for display or loose type-level inspection
- use `FastMock`, `Instance`, `NativeMock`, or the service key you supplied when the question is whether two registrations are actually distinct

This matters most for same-type dependencies: two different doubles of the same abstraction can still compare equal at the `MockModel` level even when they represent different roles in the test.

Because equality uses the simple type name, do not use `MockModel` equality as a cross-namespace uniqueness check.

## Extending Known Types

Use [AddKnownType(...)](xref:FastMoq.Mocker.AddKnownType(FastMoq.KnownTypeRegistration,System.Boolean)) when a framework-style type needs special handling that does not belong in the normal type map.

Custom registrations are scoped to the current `Mocker` instance. They do not mutate global process state.

## [AddKnownType(...)](xref:FastMoq.Mocker.AddKnownType(FastMoq.KnownTypeRegistration,System.Boolean)) vs [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[]))

These APIs are related, but they are not interchangeable.

They can look similar in the simplest case because both can end with "FastMoq returns this object." The important difference is where they plug in:

- [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])) changes the normal type map for one dependency.
- [AddKnownType(...)](xref:FastMoq.Mocker.AddKnownType(FastMoq.KnownTypeRegistration,System.Boolean)) extends FastMoq's special handling pipeline for a category of framework-heavy types.

### Use [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])) when

- You want to replace FastMoq's normal resolution for a type.
- You want a specific concrete implementation, factory result, or fixed instance.
- The problem is "what object should be returned for this abstraction?"

```csharp
Mocks.AddType<IClock>(_ => new FakeClock(DateTimeOffset.Parse("2026-04-01T12:00:00Z")));
```

Another [AddType(...)](xref:FastMoq.Mocker.AddType``2(System.Func{FastMoq.Mocker,``1},System.Boolean,System.Object[])) example that should stay on the normal type-map path:

```csharp
Mocks.AddType<IPaymentGateway, FakePaymentGateway>(_ => new FakePaymentGateway("test-terminal"));
```

These are [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])) examples because:

- `IClock` and `IPaymentGateway` are ordinary application dependencies.
- You are swapping one dependency for a specific implementation.
- There is no special FastMoq framework lifecycle or post-processing involved.

### When [AddKnownType(...)](xref:FastMoq.Mocker.AddKnownType(FastMoq.KnownTypeRegistration,System.Boolean)) is the right tool

- The type is framework-heavy and needs special creation or post-processing behavior.
- The problem is not only "what object should be returned?" but also "how should FastMoq handle this type whenever it is resolved?"
- You want to plug into direct-instance resolution, managed-instance resolution, mock configuration, or object defaults.

```csharp
Mocks.AddKnownType<IFileSystem>(
    directInstanceFactory: (_, _) => new MockFileSystem().FileSystem,
    includeDerivedTypes: true);
```

Examples that are specific to [AddKnownType(...)](xref:FastMoq.Mocker.AddKnownType(FastMoq.KnownTypeRegistration,System.Boolean)) and do not fit ordinary [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])) usage:

```csharp
Mocks.AddKnownType<IHttpContextAccessor>(
    applyObjectDefaults: (_, accessor) =>
    {
        accessor.HttpContext!.TraceIdentifier = "integration-test";
    },
    includeDerivedTypes: true);
```

```csharp
Mocks.AddKnownType<HttpContext>(
    configureMock: (_, _, fastMock) =>
    {
        var moqMock = fastMock.AsMoq();
        moqMock.Setup(x => x.TraceIdentifier).Returns("trace-123");
    });
```

These are [AddKnownType(...)](xref:FastMoq.Mocker.AddKnownType(FastMoq.KnownTypeRegistration,System.Boolean)) examples because:

- They are not just returning an object.
- They modify mock setup or post-creation defaults inside FastMoq's known-type pipeline.
- They apply to framework-style types where FastMoq already has built-in behavior.

### Quick decision rule

If you are overriding one dependency in a test, use [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])).

If you are extending FastMoq's built-in handling for a framework-style type such as `IFileSystem`, `HttpClient`, `DbContext`, or `HttpContext`, use [AddKnownType(...)](xref:FastMoq.Mocker.AddKnownType(FastMoq.KnownTypeRegistration,System.Boolean)).

If the two APIs still look similar, ask this question:

- "Am I replacing one dependency?" -> [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[]))
- "Am I teaching FastMoq how to treat this kind of framework-heavy type?" -> [AddKnownType(...)](xref:FastMoq.Mocker.AddKnownType(FastMoq.KnownTypeRegistration,System.Boolean))

Analyzer note:

- `FMOQ0014` warns on the context-aware compatibility `AddType(...)` overloads and points them back toward [AddKnownType(...)](xref:FastMoq.Mocker.AddKnownType(FastMoq.KnownTypeRegistration,System.Boolean)).

### Example: override a built-in direct instance

```csharp
var customFileSystem = new MockFileSystem().FileSystem;

Mocks.AddKnownType<IFileSystem>(
    directInstanceFactory: (_, _) => customFileSystem);
```

### Example: apply custom post-processing

```csharp
Mocks.AddKnownType<IHttpContextAccessor>(
    applyObjectDefaults: (_, accessor) =>
    {
        accessor.HttpContext!.TraceIdentifier = "integration-test";
    },
    includeDerivedTypes: true);
```

### When to prefer `AddKnownType(...)`

- The type is framework-heavy and has special lifecycle or initialization requirements.
- The behavior should apply whenever the type is resolved, not only when one constructor path is used.
- You want to extend or override built-in known-type handling without replacing the whole resolution pipeline.

## Provider Notes

FastMoq is moving toward a provider-based architecture. The stable guidance for test authors is:

1. Use FastMoq's portable APIs for creation, injection, and common helpers.
2. Use the provider-native object only when you genuinely need library-specific arrangement behavior.
3. Assume Moq compatibility is currently strongest, but new extension points should avoid hard-coding Moq assumptions unless the scenario is explicitly Moq-only.

[ScenarioBuilder](xref:FastMoq.ScenarioBuilder`1) still works with each registered provider because it only orchestrates arrange, act, and assert steps and forwards provider-first verification through [Mocker.Verify(...)](xref:FastMoq.Mocker.Verify``1(System.Linq.Expressions.Expression{System.Action{``0}},System.Nullable{FastMoq.Providers.TimesSpec})). The provider-specific part is still the arrangement code you put inside `With(...)` or `When(...)`.

[VerifyLogged(...)](https://help.fastmoq.com/api/FastMoq.Extensions.TestClassExtensions.html) now follows the same default expectation model as provider-first verification: if you do not specify a count, it means at least once. Use [TimesSpec](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html) when you need [Exactly](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html), [AtLeast](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html), [AtMost](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html), or the zero-invocation aliases [NeverCalled](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html) / [Never()](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html) semantics for captured log entries.

If you need provider-specific behavior for a tracked mock, prefer the typed provider-package extensions first, such as [AsMoq()](https://help.fastmoq.com/api/FastMoq.Providers.MoqProvider.IFastMockMoqExtensions.html) or [AsNSubstitute()](https://help.fastmoq.com/api/FastMoq.Providers.NSubstituteProvider.IFastMockNSubstituteExtensions.html).

Use [GetNativeMock(...)](https://help.fastmoq.com/api/FastMoq.Mocker.html) or [MockModel.NativeMock](https://help.fastmoq.com/api/FastMoq.Models.MockModel.html) only when you truly need the raw provider object beyond those typed helpers.

You can also retrieve the provider-first abstraction directly:

```csharp
var fastMock = Mocks.GetOrCreateMock<IOrderRepository>();
var moqMock = fastMock.AsMoq();
```

For NSubstitute-backed tests:

```csharp
var fastMock = Mocks.GetOrCreateMock<IOrderRepository>();
fastMock.AsNSubstitute().Load(123).Returns(order);
```

## `Strict` vs Presets

`Strict` is now best understood as a compatibility alias for [MockFeatures.FailOnUnconfigured](https://help.fastmoq.com/api/FastMoq.MockFeatures.html).

That means:

```csharp
Mocks.Strict = true;
```

turns on fail-on-unconfigured behavior, but it does not replace the rest of the current [Behavior](https://help.fastmoq.com/api/FastMoq.Mocker.html) flags.

It also still influences some compatibility-era fallback rules, such as whether FastMoq falls back to non-public constructors or methods when public resolution fails.

If you want to switch the whole behavior profile, use the explicit [UseStrictPreset()](https://help.fastmoq.com/api/FastMoq.Mocker.html) and [UseLenientPreset()](https://help.fastmoq.com/api/FastMoq.Mocker.html) helpers instead:

```csharp
Mocks.UseStrictPreset();
Mocks.UseLenientPreset();
```

Use the preset helpers when you want a complete behavior profile. Use `Strict` only when you mean the fail-on-unconfigured compatibility behavior.

Breaking-change note:

- In `3.0.0`, `Strict` was often treated as a broader all-in-one switch.
- In the current v4 release line, [UseStrictPreset()](https://help.fastmoq.com/api/FastMoq.Mocker.html) is the explicit way to request the broader strict profile.
- `Strict` remains available, but it should now be read as the narrower compatibility path rather than the full profile selector.

Separate compatibility note for known types:

- strict tracked `IFileSystem` mocks can still expose built-in members such as `Directory`
- strict `HttpClient` does not use the same breaking path
- `DbContext` is not part of that `IFileSystem` break

## Executable Examples In This Repo

The best current examples are in `FastMoq.TestingExample`.

Start there if you want repo-backed samples for:

- multi-dependency service workflows
- built-in `IFileSystem` behavior
- logger verification
- fluent `Scenario` usage with parameterless arrange/act/assert overloads
- provider-first verification with [TimesSpec.Once](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html), [TimesSpec.NeverCalled](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html), [TimesSpec.Exactly(...)](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html), [TimesSpec.AtLeast(...)](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html), and [TimesSpec.AtMost(...)](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html)

See [Executable Testing Examples](../samples/testing-examples.md).

## Recommended Test Flow

For most tests in this repo, this order is the least surprising:

1. Register explicit type overrides with [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])) only when needed.
2. Configure default mocks with [GetOrCreateMock&lt;T&gt;()](xref:FastMoq.Mocker.GetOrCreateMock``1(FastMoq.MockRequestOptions)).
3. Use known-type helpers for `DbContext`, `HttpClient`, `IFileSystem`, and web abstractions.
4. Create the component through [MockerTestBase&lt;TComponent&gt;](xref:FastMoq.MockerTestBase`1) or [CreateInstance(...)](xref:FastMoq.Mocker.CreateInstance``1(FastMoq.InstanceCreationFlags,System.Object[])).
5. Assert behavior and verify the dependency interactions you actually care about.

## Pitfalls to Avoid

- Do not use [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])) as a general replacement for [GetOrCreateMock&lt;T&gt;()](xref:FastMoq.Mocker.GetOrCreateMock``1(FastMoq.MockRequestOptions)).
- Do not collapse two keyed constructor dependencies into one unkeyed mock or registration when selection between them is part of the behavior under test.
- Do not bypass [GetMockDbContext&lt;TContext&gt;()](xref:FastMoq.DbContextMockerExtensions.GetMockDbContext``1(FastMoq.Mocker)) unless FastMoq's EF Core support is the thing you are explicitly testing around.
- Do not assume `CreateInstanceByType(...)` alone is the best API for new code. Use `InstanceCreationFlags` when you need to express constructor-selection intent explicitly.
- Do not make known-type extensions global. Keep them scoped to the `Mocker` used by the test.

## See Also

- [Getting Started](./README.md)
- [Cookbook](../cookbook/README.md)
- [Documentation Index](../README.md)

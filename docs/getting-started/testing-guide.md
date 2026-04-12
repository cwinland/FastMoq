# FastMoq Testing Guide

This guide documents the testing patterns that match FastMoq's current behavior in this repository. It is intentionally practical: use it when you need to decide which FastMoq API to reach for in a real test.

## Start Here

Use these rules first:

1. Use [MockerTestBase&lt;TComponent&gt;](xref:FastMoq.MockerTestBase`1) when you want FastMoq to create the component under test and manage its dependencies.
2. Use [Mocks.GetOrCreateMock&lt;T&gt;()](xref:FastMoq.Mocker.GetOrCreateMock``1(FastMoq.MockRequestOptions)) when you want the normal FastMoq tracked mock path for a dependency.
3. Use [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])) when you need to replace FastMoq's default resolution with a specific concrete type, factory, or fixed instance.
4. Use `CreateTypedServiceProvider(...)` and `AddServiceProvider(...)` when framework code expects a typed `IServiceProvider` rather than a one-object-for-all-types shim.
5. If the constructor uses the same abstraction more than once under different DI service keys, use keyed mocks, keyed registrations, or explicit constructor injection for tests where dependency selection matters.
6. Use [AddKnownType(...)](xref:FastMoq.Mocker.AddKnownType(FastMoq.KnownTypeRegistration,System.Boolean)) when a framework-style type needs special resolution or post-processing behavior.
7. Use [GetMockDbContext&lt;TContext&gt;()](xref:FastMoq.DbContextMockerExtensions.GetMockDbContext``1(FastMoq.Mocker)) when testing EF Core contexts. Do not hand-roll DbContext setup unless you need behavior outside FastMoq's helper.

## Core Mental Model

FastMoq has three distinct resolution paths:

1. Type mapping: explicit registrations added with [AddType(...)](xref:FastMoq.Mocker.AddType``1(System.Func{FastMoq.Mocker,``0},System.Boolean,System.Object[])).
2. Known types: built-in framework helpers for things like `IFileSystem`, `HttpClient`, `DbContext`, and `HttpContext` patterns.
3. Auto-mock / auto-create: default mock creation and constructor injection when no explicit mapping exists.

That distinction matters because the API choice communicates intent.

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

Use `AddServiceProvider(...)` when the system under test resolves `IServiceProvider` or `IServiceScopeFactory` from the current [Mocker](xref:FastMoq.Mocker):

```csharp
Mocks.AddServiceProvider(services =>
{
    services.AddLogging();
    services.AddSingleton(new WidgetClock());
});
```

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
- direct `FastMoq.Core` consumers should add `FastMoq.AzureFunctions` and import `FastMoq.AzureFunctions.Extensions` before using `CreateFunctionContextInstanceServices(...)`, `AddFunctionContextInstanceServices(...)`, `CreateHttpRequestData(...)`, or `CreateHttpResponseData(...)`
- the aggregate `FastMoq` package includes the Azure Functions helper package already

Analyzer note:

- `FMOQ0013` warns on direct `GetOrCreateMock<IServiceProvider>()`, `GetMock<IServiceProvider>()`, and `GetRequiredMock<IServiceProvider>()` setup so those shims migrate toward the typed helper path.

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

`GetObject<IFileSystem>()` can return the predefined `MockFileSystem` instance when FastMoq is in lenient mode and you have not already registered or created an `IFileSystem` dependency.

Use this when you want a real in-memory file system quickly:

```csharp
var fileSystem = Mocks.GetObject<IFileSystem>();
fileSystem.File.WriteAllText("/tmp/test.txt", "hello");
```

If you want mock arrangement and verification instead, stay on the mock path:

```csharp
Mocks.GetOrCreateMock<IFileSystem>()
    .Setup(x => x.File.Exists("orders.json"))
    .Returns(true);
```

FastMoq can automatically provide a built-in `IFileSystem` backed by its shared in-memory `MockFileSystem` when the built-in file-system resolution is enabled and you have not already registered `IFileSystem` explicitly.

If you need the wider filesystem abstraction family (`IFile`, `IPath`, `IDirectory`, and related factories) to resolve coherently alongside that shared file system, call [AddFileSystemAbstractionMapping()](../../api/FastMoq.Mocker.yml).

### `HttpClient`

FastMoq has a built-in `HttpClient` helper path. Use the existing HTTP setup helpers when the subject depends on `HttpClient` directly instead of manually composing handlers for every test.

### `DbContext`

Use [GetMockDbContext&lt;TContext&gt;()](xref:FastMoq.DbContextMockerExtensions.GetMockDbContext``1(FastMoq.Mocker)) as the default entry point.

If you consume the aggregate `FastMoq` package, the database helpers remain available with the same API shape. If you consume `FastMoq.Core` directly, install `FastMoq.Database` for EF-specific helpers.

When you need to choose between pure mock behavior and a real EF in-memory context, use [GetDbContextHandle&lt;TContext&gt;(...)](../../api/FastMoq.DbContextMockerExtensions.yml) with [DbContextHandleOptions&lt;TContext&gt;](../../api/FastMoq.DbContextHandleOptions-1.yml). The default mode remains [MockedSets](../../api/FastMoq.DbContextTestMode.yml), and [GetMockDbContext&lt;TContext&gt;()](xref:FastMoq.DbContextMockerExtensions.GetMockDbContext``1(FastMoq.Mocker)) is now the convenience wrapper over that default.

```csharp
protected override Action<Mocker> SetupMocksAction => mocker =>
{
    var dbContextMock = mocker.GetMockDbContext<ApplicationDbContext>();
    mocker.AddType(_ => dbContextMock.Object);
};
```

Recommended pattern:

1. Create the context mock with [GetMockDbContext&lt;TContext&gt;()](xref:FastMoq.DbContextMockerExtensions.GetMockDbContext``1(FastMoq.Mocker)).
2. Add the context object into the type map when the component under test expects the context itself.
3. Seed test data through the resolved context object before calling the system under test.

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

1. Use [CreateHttpContext(...)](../../api/FastMoq.Web.Extensions.TestWebExtensions.yml) when you need a reusable request object for middleware or accessors.
2. Use [AddHttpContext(...)](../../api/FastMoq.Web.Extensions.TestWebExtensions.yml) or [AddHttpContextAccessor(...)](../../api/FastMoq.Web.Extensions.TestWebExtensions.yml) when the system under test resolves those types from DI.
3. Use `SetRequestHeader(...)`, `SetRequestHeaders(...)`, `SetQueryString(...)`, `SetQueryParameter(...)`, or `SetQueryParameters(...)` to make request intent obvious in the test.
4. Use [CreateControllerContext(...)](../../api/FastMoq.Web.Extensions.TestWebExtensions.yml) when the controller itself reads from `ControllerContext.HttpContext.User`.
5. Use [GetOkObjectResult()](../../api/FastMoq.Web.Extensions.TestWebExtensions.yml), [GetBadRequestObjectResult()](../../api/FastMoq.Web.Extensions.TestWebExtensions.yml), [GetConflictObjectResult()](../../api/FastMoq.Web.Extensions.TestWebExtensions.yml), and [GetObjectResultContent&lt;T&gt;()](../../api/FastMoq.Web.Extensions.TestWebExtensions.yml) to keep result assertions short.

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

Use [AddKeyedType(...)](../../api/FastMoq.Mocker.yml) and [GetKeyedObject&lt;T&gt;()](../../api/FastMoq.Mocker.yml) when a fake or fixed instance reads better than a mock:

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

Analyzer note:

- `FMOQ0015` warns on unkeyed `GetOrCreateMock<T>()`, `GetMock<T>()`, `GetRequiredMock<T>()`, and `AddType<T>(...)` usage when the target type has multiple keyed constructor parameters of that same abstraction and the current file is not already using keyed setup.

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

[VerifyLogged(...)](../../api/FastMoq.Extensions.TestClassExtensions.yml) now follows the same default expectation model as provider-first verification: if you do not specify a count, it means at least once. Use [TimesSpec](../../api/FastMoq.Providers.TimesSpec.yml) when you need [Exactly](../../api/FastMoq.Providers.TimesSpec.yml), [AtLeast](../../api/FastMoq.Providers.TimesSpec.yml), [AtMost](../../api/FastMoq.Providers.TimesSpec.yml), or the zero-invocation aliases [NeverCalled](../../api/FastMoq.Providers.TimesSpec.yml) / [Never()](../../api/FastMoq.Providers.TimesSpec.yml) semantics for captured log entries.

If you need provider-specific behavior for a tracked mock, prefer the typed provider-package extensions first, such as [AsMoq()](../../api/FastMoq.Providers.MoqProvider.IFastMockMoqExtensions.yml) or [AsNSubstitute()](../../api/FastMoq.Providers.NSubstituteProvider.IFastMockNSubstituteExtensions.yml).

Use [GetNativeMock(...)](../../api/FastMoq.Mocker.yml) or [MockModel.NativeMock](../../api/FastMoq.Models.MockModel.yml) only when you truly need the raw provider object beyond those typed helpers.

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

`Strict` is now best understood as a compatibility alias for [MockFeatures.FailOnUnconfigured](../../api/FastMoq.MockFeatures.yml).

That means:

```csharp
Mocks.Strict = true;
```

turns on fail-on-unconfigured behavior, but it does not replace the rest of the current [Behavior](../../api/FastMoq.Mocker.yml) flags.

It also still influences some compatibility-era fallback rules, such as whether FastMoq falls back to non-public constructors or methods when public resolution fails.

If you want to switch the whole behavior profile, use the explicit [UseStrictPreset()](../../api/FastMoq.Mocker.yml) and [UseLenientPreset()](../../api/FastMoq.Mocker.yml) helpers instead:

```csharp
Mocks.UseStrictPreset();
Mocks.UseLenientPreset();
```

Use the preset helpers when you want a complete behavior profile. Use `Strict` only when you mean the fail-on-unconfigured compatibility behavior.

Breaking-change note:

- In `3.0.0`, `Strict` was often treated as a broader all-in-one switch.
- In the current repo, [UseStrictPreset()](../../api/FastMoq.Mocker.yml) is the explicit way to request the broader strict profile.
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
- provider-first verification with [TimesSpec.Once](../../api/FastMoq.Providers.TimesSpec.yml), [TimesSpec.NeverCalled](../../api/FastMoq.Providers.TimesSpec.yml), [TimesSpec.Exactly(...)](../../api/FastMoq.Providers.TimesSpec.yml), [TimesSpec.AtLeast(...)](../../api/FastMoq.Providers.TimesSpec.yml), and [TimesSpec.AtMost(...)](../../api/FastMoq.Providers.TimesSpec.yml)

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

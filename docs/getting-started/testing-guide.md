# FastMoq Testing Guide

This guide documents the testing patterns that match FastMoq's current behavior in this repository. It is intentionally practical: use it when you need to decide which FastMoq API to reach for in a real test.

## Start Here

Use these rules first:

1. Use `MockerTestBase<TComponent>` when you want FastMoq to create the component under test and manage its dependencies.
2. Use `Mocks.GetMock<T>()` when you want the normal FastMoq auto-mock path for a dependency.
3. Use `AddType(...)` when you need to replace FastMoq's default resolution with a specific concrete type, factory, or fixed instance.
4. Use `AddKnownType(...)` when a framework-style type needs special resolution or post-processing behavior.
5. Use `GetMockDbContext<TContext>()` when testing EF Core contexts. Do not hand-roll DbContext setup unless you need behavior outside FastMoq's helper.

## Core Mental Model

FastMoq has three distinct resolution paths:

1. Type mapping: explicit registrations added with `AddType(...)`.
2. Known types: built-in framework helpers for things like `IFileSystem`, `HttpClient`, `DbContext`, and `HttpContext` patterns.
3. Auto-mock / auto-create: default mock creation and constructor injection when no explicit mapping exists.

That distinction matters because the API choice communicates intent.

## `GetMock<T>()` vs `AddType(...)`

These are not interchangeable.

### Use `GetMock<T>()` when

- You want the dependency to stay on the default auto-mock path.
- You only need to arrange or verify behavior.
- You want the dependency tracked as the mock FastMoq would have created anyway.

```csharp
var repoMock = Mocks.GetMock<IOrderRepository>();
repoMock.Setup(x => x.Load(123)).Returns(order);
```

### When `AddType(...)` is the right tool

- You need a specific implementation rather than a default mock.
- You need to control constructor arguments or non-public construction.
- You want a fixed singleton-like instance returned for that type.
- You need to disambiguate multiple concrete implementations.

```csharp
Mocks.AddType<IClock>(_ => new FakeClock(DateTimeOffset.Parse("2026-04-01T12:00:00Z")));
```

### Practical rule

If the dependency is still conceptually a mock, prefer `GetMock<T>()`. If you are changing how the type is resolved, prefer `AddType(...)`.

## Construction APIs

FastMoq still supports the older entry points:

- `CreateInstance<T>(...)`
- `CreateInstanceNonPublic<T>(...)`
- `CreateInstanceByType<T>(...)`

For new code, prefer the unified options-based overload:

```csharp
var component = Mocks.CreateInstance<MyComponent>(new InstanceCreationOptions
{
    UsePredefinedFileSystem = false,
    AllowNonPublicConstructors = true,
    ConstructorParameterTypes = new[] { typeof(int), typeof(string) },
});
```

Use the older methods when preserving existing tests or public API compatibility matters. Internally, FastMoq now routes those calls through the same options-based construction path.

## Optional Constructor And Method Parameters

FastMoq now has an explicit option for optional-parameter behavior.

Default behavior:

- optional parameters use their declared default value when one exists
- otherwise FastMoq passes `null`

If you want FastMoq to resolve optional parameters through the normal mock/object pipeline, opt in explicitly.

### Constructor creation

```csharp
var component = Mocks.CreateInstance<MyComponent>(new InstanceCreationOptions
{
    OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker,
});
```

### `MockerTestBase<TComponent>`

For SUT creation through `MockerTestBase<TComponent>`, override `ComponentCreationOptions`:

```csharp
protected override InstanceCreationOptions ComponentCreationOptions => new()
{
    OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker,
};
```

### Delegate or reflected invocation

Use `InvocationOptions` when calling helpers that fill omitted method parameters:

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

`MockOptional` is now obsolete and should be treated only as a compatibility alias for `OptionalParameterResolutionMode.ResolveViaMocker`.

## Built-In Known Types

FastMoq includes built-in handling for a small set of framework-heavy types.

### `IFileSystem`

`GetObject<IFileSystem>()` can return the predefined `MockFileSystem` instance when FastMoq is in lenient mode and you have not already registered or created an `IFileSystem` dependency.

Use this when you want a real in-memory file system quickly:

```csharp
var fileSystem = Mocks.GetObject<IFileSystem>();
fileSystem.File.WriteAllText("/tmp/test.txt", "hello");
```

If you want mock arrangement and verification instead, stay on the mock path:

```csharp
Mocks.GetMock<IFileSystem>()
    .Setup(x => x.File.Exists("orders.json"))
    .Returns(true);
```

If you need the filesystem abstraction family (`IFile`, `IPath`, `IDirectory`, and related factories) to resolve coherently, call `AddFileSystemAbstractionMapping()`.

### `HttpClient`

FastMoq has a built-in `HttpClient` helper path. Use the existing HTTP setup helpers when the subject depends on `HttpClient` directly instead of manually composing handlers for every test.

### `DbContext`

Use `GetMockDbContext<TContext>()` as the default entry point.

```csharp
protected override Action<Mocker> SetupMocksAction => mocker =>
{
    var dbContextMock = mocker.GetMockDbContext<ApplicationDbContext>();
    mocker.AddType(_ => dbContextMock.Object);
};
```

Recommended pattern:

1. Create the context mock with `GetMockDbContext<TContext>()`.
2. Add the context object into the type map when the component under test expects the context itself.
3. Seed test data through the resolved context object before calling the system under test.

This is the supported path for EF Core tests in this repo. It keeps DbSet setup and context creation aligned with the framework's existing helper behavior.

### `HttpContext` and `IHttpContextAccessor`

FastMoq applies built-in setup for `HttpContext`, `IHttpContextAccessor`, and `HttpContextAccessor` so common web tests have a usable context object without repetitive setup.

## Extending Known Types

Use `AddKnownType(...)` when a framework-style type needs special handling that does not belong in the normal type map.

Custom registrations are scoped to the current `Mocker` instance. They do not mutate global process state.

## `AddKnownType(...)` vs `AddType(...)`

These APIs are related, but they are not interchangeable.

They can look similar in the simplest case because both can end with "FastMoq returns this object." The important difference is where they plug in:

- `AddType(...)` changes the normal type map for one dependency.
- `AddKnownType(...)` extends FastMoq's special handling pipeline for a category of framework-heavy types.

### Use `AddType(...)` when

- You want to replace FastMoq's normal resolution for a type.
- You want a specific concrete implementation, factory result, or fixed instance.
- The problem is "what object should be returned for this abstraction?"

```csharp
Mocks.AddType<IClock>(_ => new FakeClock(DateTimeOffset.Parse("2026-04-01T12:00:00Z")));
```

Another `AddType(...)` example that should stay on the normal type-map path:

```csharp
Mocks.AddType<IPaymentGateway, FakePaymentGateway>(_ => new FakePaymentGateway("test-terminal"));
```

These are `AddType(...)` examples because:

- `IClock` and `IPaymentGateway` are ordinary application dependencies.
- You are swapping one dependency for a specific implementation.
- There is no special FastMoq framework lifecycle or post-processing involved.

### When `AddKnownType(...)` is the right tool

- The type is framework-heavy and needs special creation or post-processing behavior.
- The problem is not only "what object should be returned?" but also "how should FastMoq handle this type whenever it is resolved?"
- You want to plug into direct-instance resolution, managed-instance resolution, mock configuration, or object defaults.

```csharp
Mocks.AddKnownType<IFileSystem>(
    directInstanceFactory: (_, _) => new MockFileSystem().FileSystem,
    includeDerivedTypes: true);
```

Examples that are specific to `AddKnownType(...)` and do not fit ordinary `AddType(...)` usage:

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
        var nativeMock = (Mock<HttpContext>)fastMock.NativeMock;
        nativeMock.Setup(x => x.TraceIdentifier).Returns("trace-123");
    });
```

These are `AddKnownType(...)` examples because:

- They are not just returning an object.
- They modify mock setup or post-creation defaults inside FastMoq's known-type pipeline.
- They apply to framework-style types where FastMoq already has built-in behavior.

### Quick decision rule

If you are overriding one dependency in a test, use `AddType(...)`.

If you are extending FastMoq's built-in handling for a framework-style type such as `IFileSystem`, `HttpClient`, `DbContext`, or `HttpContext`, use `AddKnownType(...)`.

If the two APIs still look similar, ask this question:

- "Am I replacing one dependency?" -> `AddType(...)`
- "Am I teaching FastMoq how to treat this kind of framework-heavy type?" -> `AddKnownType(...)`

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

If you need the underlying provider object for a tracked mock, use `GetNativeMock(...)` or `MockModel.NativeMock`.

You can also retrieve the provider-first abstraction directly:

```csharp
var fastMock = Mocks.GetFastMock<IOrderRepository>();
var providerObject = fastMock.NativeMock;
```

## `Strict` vs Presets

`Strict` is now best understood as a compatibility alias for `MockFeatures.FailOnUnconfigured`.

That means:

```csharp
Mocks.Strict = true;
```

turns on fail-on-unconfigured behavior, but it does not replace the rest of the current `Behavior` flags.

If you want to switch the whole behavior profile, use the explicit helpers instead:

```csharp
Mocks.UseStrictPreset();
Mocks.UseLenientPreset();
```

Use the preset helpers when you want a complete behavior profile. Use `Strict` only when you mean the fail-on-unconfigured compatibility behavior.

## Executable Examples In This Repo

The best current examples are in `FastMoq.TestingExample`.

Start there if you want repo-backed samples for:

- multi-dependency service workflows
- built-in `IFileSystem` behavior
- logger verification
- fluent `Scenario(...)` usage
- provider-first verification with `TimesSpec`

See [Executable Testing Examples](../samples/testing-examples.md).

## Recommended Test Flow

For most tests in this repo, this order is the least surprising:

1. Register explicit type overrides with `AddType(...)` only when needed.
2. Configure default mocks with `GetMock<T>()`.
3. Use known-type helpers for `DbContext`, `HttpClient`, `IFileSystem`, and web abstractions.
4. Create the component through `MockerTestBase<T>` or `CreateInstance(...)`.
5. Assert behavior and verify the dependency interactions you actually care about.

## Pitfalls to Avoid

- Do not use `AddType(...)` as a general replacement for `GetMock<T>()`.
- Do not bypass `GetMockDbContext<TContext>()` unless FastMoq's EF Core support is the thing you are explicitly testing around.
- Do not assume `CreateInstanceByType(...)` is the best API for new code. It remains for compatibility, but the options-based overload is clearer.
- Do not make known-type extensions global. Keep them scoped to the `Mocker` used by the test.

## See Also

- [Getting Started](./README.md)
- [Cookbook](../cookbook/README.md)
- [Documentation Index](../README.md)

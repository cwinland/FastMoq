# Migration Guide: 3.0.0 To Current Repo

This guide is for maintainers and early adopters working from the last public `3.0.0` release toward the current repository behavior.

It is intentionally practical. The goal is not to list every internal refactor, but to show which test-authoring habits should change and which compatibility APIs still exist only as bridges.

## Scope

- Public release baseline: `3.0.0`
- Release date: May 12, 2025
- Baseline commit: `4035d0d`

This is migration guidance for the repository's current unreleased direction. It is not a claim that all of this is already available in the published NuGet package.

## Migration summary

If you are moving older FastMoq usage forward, the main changes are:

1. Prefer `GetMock<T>()` for normal dependency setup instead of older reset-oriented patterns.
2. Treat `AddType(...)` as an explicit type-resolution override, not as a general substitute for mocks.
3. Treat `Strict` as compatibility-only. Use `Behavior` or preset helpers when you mean broader behavior changes.
4. Prefer the newer provider-first retrieval and verification surfaces when you do not specifically need raw Moq APIs.
5. Use the executable examples in `FastMoq.TestingExample` as the best repo-backed reference for current patterns.

## Old to new guidance

### `Initialize<T>(...)`

Old pattern:

```csharp
Mocks.Initialize<IOrderRepository>(mock =>
    mock.Setup(x => x.Load(123)).Returns(order));
```

Current guidance:

```csharp
Mocks.GetMock<IOrderRepository>()
    .Setup(x => x.Load(123))
    .Returns(order);
```

Why:

- `Initialize<T>(...)` is now a compatibility wrapper, not the recommended setup entry point.
- `GetMock<T>()` better reflects the actual intent for the normal auto-mock path.

### `Strict`

Old assumption:

```csharp
Mocks.Strict = true;
```

often implied "switch the whole runtime into a strict profile."

Current guidance:

```csharp
Mocks.Behavior.Enabled |= MockFeatures.FailOnUnconfigured;
```

or, if you want the full preset:

```csharp
Mocks.UseStrictPreset();
```

Why:

- `Strict` is now treated as a compatibility alias for `MockFeatures.FailOnUnconfigured`.
- Full-profile behavior changes belong to `Behavior` or the preset helpers.
- If older tests meant "use the full strict profile," they should move to `UseStrictPreset()` explicitly.
- New repo-era code should treat `FailOnUnconfigured` narrowly: it controls mock strictness, not every older strict-era fallback and built-in resolution rule.

Breaking-change note:

- `Strict` no longer implies the entire old strict profile by itself
- `UseStrictPreset()` is now the clearer replacement when the broader strict behavior profile is intended
- `Strict` still exists to stamp compatibility defaults onto the current policy surface, so this is a narrowing rather than a full removal of old semantics

- strict `IFileSystem` no longer guarantees a raw or empty mock in the current repo
- tracked `IFileSystem` mocks may still expose built-in members such as `File`, `Directory`, and `Path`
- if an older test relied on null members, configure them explicitly on `GetMock<IFileSystem>()`
- this compatibility note is specific to `IFileSystem`; it is not a blanket statement that all built-in types ignore strict-mode behavior

Example:

```csharp
Mocks.UseStrictPreset();
```

```csharp
Mocks.Behavior.Enabled |= MockFeatures.FailOnUnconfigured;
Mocks.GetMock<IFileSystem>()
    .Setup(x => x.Directory)
    .Returns((IDirectory)null!);
```

For the dedicated compatibility summary, see [Breaking Changes](../breaking-changes/README.md).

### `GetMock<T>()` vs `AddType(...)`

Old usage sometimes blurred these together.

Current guidance:

- use `GetMock<T>()` when the dependency is still conceptually a mock
- use `AddType(...)` when you are overriding resolution with a concrete type, custom factory, or fixed instance

Good `AddType(...)` use:

```csharp
Mocks.AddType<IClock>(_ => new FakeClock(DateTimeOffset.Parse("2026-04-01T12:00:00Z")));
```

Good `GetMock<T>()` use:

```csharp
Mocks.GetMock<IEmailGateway>()
    .Setup(x => x.SendAsync(It.IsAny<string>()))
    .Returns(Task.CompletedTask);
```

### `AddKnownType(...)` vs `AddType(...)`

This distinction matters more in the current repo than it did in older FastMoq usage.

Use `AddType(...)` when you are overriding normal resolution for a dependency:

```csharp
Mocks.AddType<IClock>(_ => new FakeClock(DateTimeOffset.Parse("2026-04-01T12:00:00Z")));
```

Use `AddKnownType(...)` when you are extending FastMoq's built-in handling for a framework-style type:

```csharp
Mocks.AddKnownType<IFileSystem>(
    directInstanceFactory: (_, _) => new MockFileSystem().FileSystem,
    includeDerivedTypes: true);
```

The generic public API is now more strongly typed for value callbacks, so object-default hooks can work directly with `TKnown`:

```csharp
Mocks.AddKnownType<IHttpContextAccessor>(
    applyObjectDefaults: (_, accessor) =>
    {
        accessor.HttpContext!.TraceIdentifier = "integration-test";
    },
    includeDerivedTypes: true);
```

Practical rule:

- `AddType(...)` answers "resolve this abstraction differently"
- `AddKnownType(...)` answers "handle this kind of framework-heavy type differently inside FastMoq's known-type pipeline"

For most ordinary application dependencies, prefer `AddType(...)`.
For framework helpers like `IFileSystem`, `HttpClient`, `DbContext`, and `HttpContext`, prefer `AddKnownType(...)`.

### Construction APIs

The older entry points still exist:

- `CreateInstance<T>(...)`
- `CreateInstanceNonPublic<T>(...)`
- `CreateInstanceByType<T>(...)`

Current guidance for new code:

```csharp
var component = Mocks.CreateInstance<MyComponent>(new InstanceCreationOptions
{
    AllowNonPublicConstructors = true,
    ConstructorParameterTypes = new[] { typeof(int), typeof(string) },
});
```

Why:

- the implementation now routes through a unified options-based model
- the options object communicates intent more clearly than the older split overloads

If you want to decouple non-public constructor fallback from `Strict`, use the explicit option:

```csharp
var component = Mocks.CreateInstance<MyComponent>(new InstanceCreationOptions
{
    FallbackToNonPublicConstructors = true,
});
```

The default constructor-fallback policy is now also explicit at the `Mocker` level:

```csharp
Mocks.Policy.DefaultFallbackToNonPublicConstructors = false;
```

### Obsolete `MockOptional`

Old pattern:

```csharp
Mocks.MockOptional = true;
```

Current guidance for constructor creation:

```csharp
var component = Mocks.CreateInstance<MyComponent>(new InstanceCreationOptions
{
    OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker,
});
```

Current guidance for method or delegate invocation:

```csharp
var result = Mocks.CallMethod<MyResult>(new InvocationOptions
{
    OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker,
}, (Func<IMyDependency?, MyResult>)CreateResult);
```

If reflected method fallback should be controlled independently of `Strict`, set it explicitly:

```csharp
var result = Mocks.InvokeMethod(new InvocationOptions
{
    FallbackToNonPublicMethods = false,
}, target, "Run");
```

The default reflected-method fallback policy is also configurable on the `Mocker` instance:

```csharp
Mocks.Policy.DefaultFallbackToNonPublicMethods = false;
```

Built-in framework helpers are now separately controllable through flags:

```csharp
Mocks.Policy.EnabledBuiltInTypeResolutions =
    BuiltInTypeResolutionFlags.FileSystem |
    BuiltInTypeResolutionFlags.DbContext;
```

That policy controls whether built-in direct or managed resolution is available for:

- `IFileSystem`
- `HttpClient`
- `Uri`
- `DbContext`

This keeps known-type behavior separate from `FailOnUnconfigured` so the option means what it says.

Provider-backed mock creation also has a `Mocker`-level default policy:

```csharp
Mocks.Policy.DefaultStrictMockCreation = true;
```

Use that when you want new provider-backed mocks to be created as strict without relying on the broader compatibility bundle.

`GetMockDbContext<TContext>()` remains a deliberate exception and stays on the supported DbContext helper behavior rather than adopting strict creation by default.

For `MockerTestBase<TComponent>`, the same defaults can be applied before component creation through the policy hook:

```csharp
protected override Action<MockerPolicyOptions>? ConfigureMockerPolicy => policy =>
{
    policy.DefaultStrictMockCreation = true;
};
```

Current guidance for `MockerTestBase<TComponent>`:

```csharp
protected override InstanceCreationOptions ComponentCreationOptions => new()
{
    OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker,
};
```

Why:

- explicit options are clearer than an ambient toggle
- constructor creation and invocation now share the same policy model
- `MockOptional` is obsolete and remains available only as a compatibility alias

### Provider-first access

Older tests often assumed `Moq.Mock` was the only meaningful tracked artifact.

Current guidance:

```csharp
var fastMock = Mocks.GetOrCreateMock<IOrderRepository>();
var providerObject = fastMock.NativeMock;
```

You can also inspect tracked models through `MockModel.NativeMock` or `GetNativeMock(...)`.

Why:

- FastMoq is moving toward a provider-neutral core
- provider-first access is now a first-class path instead of an afterthought

### Known-type extensibility

Current guidance for framework-heavy types:

```csharp
Mocks.AddKnownType<IFileSystem>(
    directInstanceFactory: (_, _) => new MockFileSystem().FileSystem);
```

Why:

- known-type overrides are now scoped per `Mocker`
- extensions no longer need to rely on process-wide static mutation

### Fluent scenario style

This is new repo-era surface rather than a `3.0.0` migration requirement, but it is now worth adopting for readable workflow-style tests:

```csharp
Scenario
    .With(() => { /* arrange */ })
    .When(() => Component.Process())
    .Then(() => { /* assert */ })
    .Execute();
```

Use it when the test reads better as arrange/act/assert phases rather than as one large method body.
Inside `MockerTestBase<TComponent>`, prefer `Scenario` plus the parameterless `With` / `When` / `Then` overloads when `Component` is already available.
Use `WhenThrows<TException>(...)` when the act step is expected to fail but you still want trailing `Then(...)` assertions to run.
Use `ExecuteThrows<TException>()` or `ExecuteThrowsAsync<TException>()` when you want the thrown exception object back for direct inspection.

## Recommended migration order

1. Replace `Initialize<T>(...)` usage with `GetMock<T>()` setup where possible.
2. Audit `Strict` usage and decide whether each case means fail-on-unconfigured only or a full strict preset.
3. Replace new `MockOptional` usage with explicit `InstanceCreationOptions`, `InvocationOptions`, or `ComponentCreationOptions` overrides.
4. Separate `GetMock<T>()` scenarios from `AddType(...)` scenarios so the test intent is obvious.
5. Adopt provider-first surfaces only where they add value; do not rewrite stable tests without a reason.
6. Use the repo's executable examples as the reference for new tests.

## Best source of examples

For current repo-backed examples, see:

- [Executable Testing Examples](../samples/testing-examples.md)
- `FastMoq.TestingExample/RealWorldExampleTests.cs`

For release delta context, see [What's New Since 3.0.0](../whats-new/README.md).

# API Replacements And Migration Exceptions

This page collects the high-churn API replacement rules, compatibility-only exceptions, and worked migration examples that used to live in the main migration guide.

Open it when you know the specific API or helper you are replacing and need the detailed guidance rather than the front-door migration flow.

## Real-world gotchas

These are small migration edges that cause disproportionate churn in provider-first rewrites.

| Legacy habit | Preferred v4 move | Why it trips people up |
| --- | --- | --- |
| `mock.Object` or `GetMock<T>().Object` | `fastMock.Instance` or `Mocks.GetObject<T>()` | `GetOrCreateMock<T>()` returns `IFastMock<T>`, not a raw `Mock<T>` |
| `mock.Reset()` | `fastMock.Reset()` | The provider-first handle already has a reset surface; `AsMoq()` is only needed when you truly need raw Moq APIs |
| `TimesSpec.Never` during bulk conversion | `TimesSpec.Never()` or preferably `TimesSpec.NeverCalled` | The method call is easy to miss, and `NeverCalled` avoids the missing-parentheses mistake entirely |
| one unkeyed mock for two keyed constructor dependencies | keyed `MockRequestOptions.ServiceKey`, `AddKeyedType(...)`, or explicit constructor injection | a single type-based double cannot catch public or private or primary or secondary swaps |
| shared helper signatures such as `Func<Times>` | deliberate helper pass to `TimesSpec` | Fixing the helper boundary first is cheaper than patching repeated leaf call sites |

If you still need raw Moq APIs after moving to `GetOrCreateMock<T>()`, step through `AsMoq()` explicitly. Otherwise stay on `Instance`, `Reset()`, `GetObject<T>()`, and the provider-neutral verification surface.

Shared helper signature example:

```csharp
// Old helper boundary
void AssertLogged(Mock<ILogger> logger, Func<Times> times)

// Deliberate migration boundary
void AssertLogged(Mocker mocker, TimesSpec times)
```

## Old to new guidance

### `Initialize<T>(...)`

Old pattern:

```csharp
Mocks.Initialize<IOrderRepository>(mock =>
    mock.Setup(x => x.Load(123)).Returns(order));
```

Current guidance:

```csharp
Mocks.GetOrCreateMock<IOrderRepository>()
    .Setup(x => x.Load(123))
    .Returns(order);
```

Why:

- `Initialize<T>(...)` is now a compatibility wrapper, not the recommended setup entry point.
- `GetOrCreateMock<T>()` plus the Moq provider extensions is the preferred v4 transition path for Moq-specific setup.
- `GetMock<T>()` remains only as an obsolete compatibility surface when the assembly is explicitly using the bundled Moq compatibility provider.

If you are already refactoring the test and do not specifically need a raw `Moq.Mock<T>`, prefer moving toward provider-neutral retrieval and verification instead of expanding direct `GetMock<T>()` usage further.

There is not yet a single provider-neutral drop-in replacement for `Initialize<T>(...)`, because `Initialize<T>(...)` wraps a Moq `Setup(...)` callback.

In the v4 transition, the preferred provider-package shortcut for Moq-specific setup is now:

```csharp
Mocks.GetOrCreateMock<IOrderRepository>()
    .Setup(x => x.Load(123))
    .Returns(order);
```

That shortcut comes from `FastMoq.Provider.Moq` and forwards to `AsMoq()` internally. It keeps the core abstractions provider agnostic while giving Moq-based test suites a smoother migration path.

Required namespace for those fluent Moq-backed shortcuts:

```csharp
using FastMoq.Providers.MoqProvider;
```

Without that namespace, `Setup(...)`, `SetupGet(...)`, `SetupSequence(...)`, and `Protected()` on `IFastMock<T>` can appear to have "disappeared" even though the provider package is installed.

Practical rule:

- use `GetOrCreateMock<T>()` plus the Moq provider extensions when you want tracked provider-first access with Moq-specific setup convenience in v4
- keep existing `GetMock<T>()` usage only when the test still depends on older direct `Mock<T>`-shaped compatibility code and you are minimizing churn during the v4 transition
- use `GetOrCreateMock(...)`, `Verify(...)`, and `VerifyLogged(...)` when the test can move forward without Moq-specific setup semantics
- use `AddType(...)` when the cleaner provider-neutral move is to supply a fake, stub, factory, or fixed instance rather than configure a mocking-library setup chain

Provider-neutral replacement depends on what the old `Initialize<T>(...)` callback was doing:

- if the old code mainly existed to verify later calls, start from `GetOrCreateMock<T>()` and provider-neutral verification
- if the old code mainly existed to supply behavior, prefer `AddType(...)` with a fake, stub, or fixed instance when practical
- if the old code still needs Moq `Setup(...)` semantics, prefer moving it to `GetOrCreateMock<T>()` with the Moq provider extensions and keep `GetMock<T>()` only when preserving the old Moq shape is materially cheaper than rewriting the test

### `SetupHttpMessage(...)` and HTTP helpers

Old Moq-oriented pattern:

```csharp
Mocks.SetupHttpMessage(HttpStatusCode.OK, "{\"id\":42}");
```

Current guidance for new or refactored tests:

```csharp
Mocks.WhenHttpRequestJson(HttpMethod.Get, "/orders/42", "{\"id\":42}");
```

Or, when the response needs full control:

```csharp
Mocks.WhenHttpRequest(HttpMethod.Get, "/orders/42", () =>
    new HttpResponseMessage(HttpStatusCode.OK));
```

Why:

- the provider-neutral HTTP behavior helpers now live in core and are the preferred long-term path
- the older Moq-shaped HTTP setup helpers are compatibility APIs for suites that still depend on Moq-specific behavior such as protected `SendAsync` setups
- the migration concern is package ownership, not namespace churn

Compatibility detail:

- keep `using FastMoq.Extensions;`
- keep or add the `FastMoq.Provider.Moq` package when you still need `SetupHttpMessage(...)`
- prefer `WhenHttpRequest(...)` and `WhenHttpRequestJson(...)` for newly written tests and while refactoring existing ones

That means existing source often does not need a namespace rewrite for the older HTTP helpers. The moved compatibility methods intentionally remain in `FastMoq.Extensions`; they are simply provided by the Moq package now instead of core.

### `VerifyLogger(...)` vs `VerifyLogged(...)`

Old Moq-oriented pattern:

```csharp
Mocks.GetMock<ILogger<MyComponent>>()
    .VerifyLogger(LogLevel.Information, "Processing started");
```

Current guidance:

```csharp
Mocks.VerifyLogged(LogLevel.Information, "Processing started");
```

Why:

- `VerifyLogged(...)` is the provider-safe logging assertion surface.
- `VerifyLogger(...)` remains a Moq compatibility API.
- existing logger assertions carried forward from previous FastMoq versions can stay on `VerifyLogger(...)` if the assembly explicitly selects `moq`, but new or touched tests should prefer `VerifyLogged(...)`.

### `TimesSpec` shape in v4

Use `TimesSpec` with this mental model during migration:

- property-style aliases for the common cases: `TimesSpec.Once` and `TimesSpec.NeverCalled`
- method calls when a count is required: `TimesSpec.Exactly(count)`, `TimesSpec.AtLeast(count)`, and `TimesSpec.AtMost(count)`
- `TimesSpec.Never()` still works as a compatibility form, but prefer `TimesSpec.NeverCalled` in new migration edits when you want the shape to read consistently with `TimesSpec.Once`

Examples:

```csharp
Mocks.Verify<IOrderRepository>(x => x.Save(order), TimesSpec.Once);
Mocks.Verify<IEmailGateway>(x => x.Send("finance@contoso.test"), TimesSpec.NeverCalled);
Mocks.VerifyLogged(LogLevel.Warning, "retrying", TimesSpec.Exactly(2));
```

This is one of the easiest compile-fix churn points in migration work, so it is worth being explicit.

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
- if a test carried forward from previous FastMoq versions relied on null members, configure them explicitly on `GetMock<IFileSystem>()`
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

For the fuller current-model explanation of `Strict`, `FailOnUnconfigured`, and the preset helpers, see [Testing Guide: Strict vs Presets](../getting-started/testing-guide.md#strict-vs-presets).

### Legacy `GetMock<T>()` vs `AddType(...)`

Old usage sometimes blurred these together.

Current guidance:

- prefer `GetOrCreateMock<T>()` when the dependency is still conceptually a tracked mock
- use `AddType(...)` when you are overriding resolution with a concrete type, custom factory, or fixed instance
- keep `GetMock<T>()` only when you are deliberately staying on the obsolete Moq compatibility path during v4 migration

Good `AddType(...)` use:

```csharp
Mocks.AddType<IClock>(_ => new FakeClock(DateTimeOffset.Parse("2026-04-01T12:00:00Z")));
```

Good provider-first mock use:

```csharp
Mocks.GetOrCreateMock<IEmailGateway>()
    .Setup(x => x.SendAsync(It.IsAny<string>()))
    .Returns(Task.CompletedTask);
```

### `DbContext` package boundary

In `3.0.0`, core directly carried the EF packages and the Moq-based `DbContextMock<TContext>` implementation.

Current repository direction:

- `GetMockDbContext<TContext>()` still lives in the `FastMoq` namespace for the main call site.
- `FastMoq.Core` is being kept lighter and no longer owns the EF package references.
- `FastMoq.Database` now owns the EF-specific helper implementation.

Practical guidance:

- if you install `FastMoq`, keep using `GetMockDbContext<TContext>()` as before
- if you install `FastMoq.Core` directly, also install `FastMoq.Database`
- use `GetDbContextHandle<TContext>(...)` when you need to choose explicitly between mocked sets and a real in-memory EF context
- do not assume the mocked-sets helper is provider-neutral yet; today that path still uses the moved Moq-based implementation

Current repo behavior now makes the mode split explicit:

```csharp
var handle = mocker.GetDbContextHandle<ApplicationDbContext>(new DbContextHandleOptions<ApplicationDbContext>
{
    Mode = DbContextTestMode.RealInMemory,
});

var dbContext = handle.Context;
```

### `AddKnownType(...)` vs `AddType(...)`

This distinction matters more in the current v4 release line than it did in previous FastMoq versions.

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
- `CreateInstanceByType<T>(...)`

Current guidance for new code:

```csharp
var component = Mocks.CreateInstanceByType<MyComponent>(
    InstanceCreationFlags.AllowNonPublicConstructorFallback,
    typeof(int),
    typeof(string));
```

Why:

- the implementation now uses a single flags-based override model
- the call site communicates whether constructor fallback is using policy, public-only, or explicitly allowed

If you want to decouple constructor fallback from global policy, use an explicit flag:

```csharp
var component = Mocks.CreateInstance<MyComponent>(InstanceCreationFlags.AllowNonPublicConstructorFallback);
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
Mocks.OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker;

var component = Mocks.CreateInstance<MyComponent>();
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
protected override InstanceCreationFlags ComponentCreationFlags
    => InstanceCreationFlags.ResolveOptionalParametersViaMocker;
```

Why:

- focused component-construction overrides are clearer than an ambient toggle
- constructor creation and invocation now share the same policy model
- `MockOptional` is obsolete and remains available only as a compatibility alias

### Provider-first access

Tests carried forward from previous FastMoq versions often assumed `Moq.Mock` was the only meaningful tracked artifact.

Current guidance:

```csharp
var fastMock = Mocks.GetOrCreateMock<IOrderRepository>();
var moqMock = fastMock.AsMoq();
```

If you want the shortest Moq-specific form in v4, use the provider-package shortcut methods directly on `IFastMock<T>`:

```csharp
Mocks.GetOrCreateMock<IOrderRepository>()
    .Setup(x => x.Load(123))
    .Returns(order);
```

You can still inspect tracked models through `MockModel.NativeMock` or `GetNativeMock(...)` when you truly need the raw provider object.

Why:

- FastMoq is moving toward a provider-neutral core
- provider-specific convenience now belongs in provider packages instead of core
- raw `NativeMock` access is still available, but it is no longer the best primary guidance when a typed provider extension exists

## Expected migration exceptions

Some migrated tests should intentionally stay on raw Moq. That is normal in v4.

For practical fallback patterns when you do not want to stay on Moq, see [Provider Capabilities: Alternatives when a Moq feature is unavailable](../getting-started/provider-capabilities.md#alternatives-when-a-moq-feature-is-unavailable).

### `IMemoryCache` and `ICacheEntry`

Property-setter and cache-entry tests are still a common Moq-only pocket because they often rely on `SetupSet(...)` or `SetupAllProperties()`.

```csharp
var cacheEntry = Mocks.GetMock<ICacheEntry>();
cacheEntry.SetupAllProperties();
cacheEntry.SetupSet(x => x.Value = It.IsAny<object>());

Mocks.GetMock<IMemoryCache>()
    .Setup(x => x.CreateEntry("orders"))
    .Returns(cacheEntry.Object);
```

### `out` / `ref` verification

If the test already depends on `It.Ref<T>.IsAny` or other Moq-specific `out` / `ref` verification forms, keeping raw Moq is usually the clearest move. Prefer `GetOrCreateMock<T>().AsMoq()` when that still reads cleanly, and leave `GetMock<T>()` in place only when preserving the legacy Moq shape avoids churn.

```csharp
Mocks.GetMock<IParser>()
    .Verify(x => x.TryResolve("tenant-a", out It.Ref<object>.IsAny), Times.Once);
```

### Practical rule

- prefer provider-first APIs when they simplify the test
- keep raw Moq when setter-heavy, property-heavy, or `out` / `ref` heavy behavior would otherwise turn the migration into a rewrite
- do not treat these cases as migration failures

## Real migration examples

### Constructor and default setup migration

Before:

```csharp
Mocks.Initialize<IOrderRepository>(mock =>
    mock.Setup(x => x.Load(123)).Returns(order));
```

After:

```csharp
Mocks.GetOrCreateMock<IOrderRepository>()
    .Setup(x => x.Load(123))
    .Returns(order);
```

### Logger verification migration

Before:

```csharp
Mocks.GetMock<ILogger<OrderService>>()
    .VerifyLogger(LogLevel.Information, "Processing started");
```

After:

```csharp
Mocks.VerifyLogged(LogLevel.Information, "Processing started");
```

### Selective verify migration

Before:

```csharp
Mocks.GetMock<IOrderRepository>()
    .Verify(x => x.Save(It.IsAny<Order>()), Times.Once);
```

After:

```csharp
Mocks.Verify<IOrderRepository>(x => x.Save(It.IsAny<Order>()), TimesSpec.Once);
```

### Cache and property-setter edge case

Intentional Moq retention:

```csharp
var cacheEntry = Mocks.GetMock<ICacheEntry>();
cacheEntry.SetupAllProperties();
cacheEntry.SetupSet(x => x.Value = It.IsAny<object>());
```

That is still a valid v4 migration outcome because the test depends on Moq-only setter behavior.

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

Use it when the test reads better as arrange or act or assert phases rather than as one large method body.
Inside `MockerTestBase<TComponent>`, prefer `Scenario` plus the parameterless `With` or `When` or `Then` overloads when `Component` is already available.
Use `WhenThrows<TException>(...)` when the act step is expected to fail but you still want trailing `Then(...)` assertions to run.
Use `ExecuteThrows<TException>()` or `ExecuteThrowsAsync<TException>()` when you want the thrown exception object back for direct inspection.
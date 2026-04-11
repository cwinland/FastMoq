# What's New Since 3.0.0

This page summarizes the release delta between the last public `3.0.0` package and the current v4 code line in this repository.

## 4.0.3

FastMoq.Web now uses `bunit` `2.7.2` for Blazor component tests. The FastMoq Blazor helper layer was updated to keep existing test suites moving with low source churn while matching bUnit 2's renderer, parameter, authorization, and navigation changes.

Consumer impact:

- `MockerBlazorTestBase<T>` and `ComponentState` were updated for bUnit 2's renamed test doubles, unified render APIs, and renderer state shape
- `MockerBlazorTestBase<T>.RenderParameters` now uses `FastMoq.Web.Blazor.Models.RenderParameter` because bUnit 2 no longer exposes the old `ComponentParameter` type to consumers
- `SetElementCheck(...)`, `SetElementSwitch(...)`, and `SetElementText(...)` now accept `IRenderedComponent<IComponent>?` for `startingPoint` because bUnit 2 removed `IRenderedFragment`
- compatibility shims for `TestServiceProvider`, `TestAuthorizationContext`, and `FakeNavigationManager` keep common Blazor test patterns compiling with lower migration churn

Validation run for this change:

- `dotnet build .\FastMoq.Web\FastMoq.Web.csproj -c Debug`
- `dotnet test .\FastMoq.Tests.Web\FastMoq.Tests.Web.csproj -c Debug`
- `dotnet test .\FastMoq.sln -c Debug`
- `powershell -ExecutionPolicy Bypass -File .\scripts\Generate-ApiDocs.ps1`

Migration details are documented in [bUnit and Blazor test migration](../migration/bunit-and-blazor-testing.md).

## Release baseline

- Last public release: `3.0.0`
- Release date: May 12, 2025
- Release baseline commit: `4035d0d`

## Release highlights

The work after `3.0.0` is not a small compatibility patch. It is a broader v4 reshape of FastMoq around provider-neutral internals, explicit package boundaries, and clearer migration paths.

At a high level, the current line adds:

- a provider-first architecture with explicit provider registration and selection
- a new package split for abstractions, database helpers, and provider-specific adapters
- first-party Azure testing helpers for storage-client registration and Azure Functions worker or HTTP-trigger setup
- provider-neutral verification and scenario-building APIs for newer tests
- explicit policy surfaces for constructor fallback, method fallback, built-in known types, and optional-parameter resolution
- expanded repo-native documentation, migration guidance, and executable examples

## Major changes since 3.0.0

### Provider-first architecture

FastMoq no longer assumes Moq everywhere inside core.

Key additions include:

- `IMockingProvider`
- `IMockingProviderCapabilities`
- `IFastMock` and `IFastMock<T>`
- `MockingProviderRegistry`
- `MockCreationOptions`
- a built-in reflection provider plus provider packages for Moq and NSubstitute

This gives FastMoq an explicit provider-selection story instead of treating Moq as an implicit implementation detail.

### New package layout

The package boundary is much clearer than it was in `3.0.0`.

Notable additions and changes:

- `FastMoq.Abstractions` now carries the provider contracts shared by core and provider packages.
- `FastMoq.Provider.Moq` now owns the Moq compatibility provider and Moq-specific convenience extensions.
- `FastMoq.Provider.NSubstitute` adds an optional NSubstitute provider package.
- `FastMoq.AzureFunctions` now owns the Azure Functions worker helpers for `FunctionContext.InstanceServices` plus concrete `HttpRequestData` and `HttpResponseData` builders and body helpers for HTTP-trigger tests.
- `FastMoq.Database` now owns the EF- and DbContext-specific helpers that previously lived in core.
- `FastMoq.Core` stays lighter and focuses on provider-neutral construction, tracking, verification, and built-in known-type handling.

The aggregate `FastMoq` package still keeps the main end-user entry point simple, while direct package consumers can now take only the pieces they need.

### Provider-first mock access and verification

Tracked mocks now expose the active provider object and support both provider-neutral verification and provider-specific setup extensions.

Key surfaces include:

- `GetOrCreateMock<T>()`
- `GetOrCreateMock(Type, ...)`
- `GetNativeMock(...)`
- `MockModel.NativeMock`
- `Verify(...)`, `VerifyNoOtherCalls(...)`, and `VerifyLogged(...)`
- `TimesSpec` with `Once`, `Exactly(count)`, `AtLeast(count)`, `AtMost(count)`, and `Never()`

For migration-heavy suites, `GetMock<T>()` and Moq-shaped compatibility APIs still exist. For newer or actively refactored tests, the preferred direction is tracked provider-first access plus provider-neutral verification.

### Fluent scenario builder

The repo now includes a lightweight scenario builder for workflow-style tests.

Key additions include:

- `Scenario.With(...).When(...).Then(...).Verify(...)`
- `WhenThrows<TException>(...)`
- `ExecuteThrows<TException>()`
- `ExecuteThrowsAsync<TException>()`

This lets tests express arrange-act-assert flows more directly while still routing verification through the provider-neutral model.

### Focused creation and invocation policies

Several older configuration paths have been replaced with explicit flags and policy objects.

Important changes include:

- `InstanceCreationFlags` now controls constructor visibility and fallback behavior directly.
- `InvocationOptions` now carries explicit optional-parameter and non-public-method fallback settings.
- `Mocker.Policy` now exposes defaults for constructor fallback, method fallback, strict mock creation, and built-in type resolution.
- `MockOptional` is now obsolete and kept as a compatibility alias rather than the main configuration path.

This makes test behavior easier to reason about than the older model of implicit coupled defaults.

### Known-type extensibility and built-in policies

Known-type handling is more explicit and more extensible than it was in `3.0.0`.

Notable changes:

- `AddKnownType(...)` allows per-`Mocker` known-type registrations.
- `KnownTypeRegistration` and the centralized `KnownTypeRegistry` make the built-in pipeline explicit.
- `BuiltInTypeResolutionFlags` controls which built-in type enrichments are enabled.

That matters most for framework-heavy types such as `IFileSystem`, `HttpClient`, `DbContext`, and HTTP-context patterns where FastMoq provides more than a plain generic mock.

### Database helpers moved out of core

Entity Framework support is now separated from `FastMoq.Core`.

The new database surface includes:

- `DbContextHandle<TContext>`
- `DbContextHandleOptions<TContext>`
- `DbContextTestMode`
- `DbContextMockerExtensions`

`GetMockDbContext<TContext>()` remains available through the `FastMoq` namespace for low-churn usage, but the implementation and EF-specific dependencies now live in `FastMoq.Database`.

The mocked-sets path still uses the existing Moq-based `DbContextMock<TContext>` internals, while real EF-backed in-memory behavior is now exposed explicitly through `DbContextTestMode.RealInMemory`.

### Compatibility and behavior cleanup

Several compatibility surfaces were kept, but their role is narrower and more explicit.

Important points:

- `Strict` now behaves as a compatibility-oriented alias for fail-on-unconfigured behavior instead of a hidden all-in-one preset switch.
- `UseStrictPreset()` and `UseLenientPreset()` now express the broader behavior profiles directly.
- Moq-specific HTTP helpers such as `SetupHttpMessage(...)` moved out of `FastMoq.Core` and into `FastMoq.Provider.Moq`, while remaining in the `FastMoq.Extensions` namespace for lower source churn.
- `MockerTestBase<TComponent>` gained clearer component-construction and policy hooks.

### Breaking behavior to account for

Two release-facing behavior changes deserve special attention:

- strict-mode semantics are now more explicit and less overloaded than they were in `3.0.0`
- strict `IFileSystem` mocks now stay enriched by the built-in in-memory file system pipeline instead of behaving like a raw empty Moq mock

For detailed migration notes, see [Breaking Changes](../breaking-changes/README.md).

### Documentation, API reference, and executable examples

The repo documentation is substantially broader than it was at the `3.0.0` baseline.

Notable additions include:

- a repo-native testing guide
- a provider-selection guide
- a migration guide focused on the `3.0.0` to v4 move
- executable examples in `FastMoq.TestingExample`
- refreshed DocFX/API output for the expanded surface area

The executable examples now cover:

- order processing with repository, inventory, payment, and logging flows
- customer import using the built-in `MockFileSystem`
- invoice reminders using `Scenario`, `TimesSpec`, `WhenThrows`, and `ExecuteThrows`

For those examples, see [Executable Testing Examples](../samples/testing-examples.md).

## Platform and packaging summary

Compared to `3.0.0`, the current line also updates the supported target frameworks and package organization.

- libraries now target `net8.0`, `net9.0`, and `net10.0`
- older packaging based on per-project `.nuspec` files was removed in favor of SDK-style package metadata in the project files
- solution and test coverage expanded to include provider-specific packages and verification matrices

## Recommended reading order

If you are evaluating the release delta first, use this order:

1. [What's New Since 3.0.0](../whats-new/README.md)
2. [Breaking Changes](../breaking-changes/README.md)
3. [Migration Guide: 3.0.0 To Current Repo](../migration/README.md)
4. [Provider Selection Guide](../getting-started/provider-selection.md)
5. [Executable Testing Examples](../samples/testing-examples.md)

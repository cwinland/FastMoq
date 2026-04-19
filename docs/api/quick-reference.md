# FastMoq Quick Reference

Use this page when you already know the type name and want to jump straight to the API page.

## Most-used starting points

- [Mocker](../../api/FastMoq.Mocker.yml)
- [MockerTestBase&lt;TComponent&gt;](../../api/FastMoq.MockerTestBase-1.yml)
- [ScenarioBuilder&lt;T&gt;](../../api/FastMoq.ScenarioBuilder-1.yml)
- [MockingProviderRegistry](../../api/FastMoq.Providers.MockingProviderRegistry.yml)
- [MockerBlazorTestBase&lt;T&gt;](../../api/FastMoq.Web.Blazor.MockerBlazorTestBase-1.yml)

## Core test entry points

- [Mocker](../../api/FastMoq.Mocker.yml)
- [MockerTestBase&lt;TComponent&gt;](../../api/FastMoq.MockerTestBase-1.yml)
- [ScenarioBuilder&lt;T&gt;](../../api/FastMoq.ScenarioBuilder-1.yml)

## Arrange and creation

- [Mocker](../../api/FastMoq.Mocker.yml)
- [IFastMock&lt;T&gt;](../../api/FastMoq.Providers.IFastMock-1.yml)
- [KnownTypeRegistration](../../api/FastMoq.KnownTypeRegistration.yml)
- [InstanceCreationFlags](../../api/FastMoq.InstanceCreationFlags.yml)
- [InvocationOptions](../../api/FastMoq.InvocationOptions.yml)

Use the [Mocker](../../api/FastMoq.Mocker.yml) page for the tracked and standalone creation APIs: `GetOrCreateMock<T>()`, `CreateFastMock<T>()`, and `CreateStandaloneFastMock<T>()`.

## Provider APIs

- [MockingProviderRegistry](../../api/FastMoq.Providers.MockingProviderRegistry.yml)
- [IMockingProvider](../../api/FastMoq.Providers.IMockingProvider.yml)
- [IMockingProviderCapabilities](../../api/FastMoq.Providers.IMockingProviderCapabilities.yml)
- [TimesSpec](../../api/FastMoq.Providers.TimesSpec.yml)
- [MockCreationOptions](../../api/FastMoq.Providers.MockCreationOptions.yml)

If you are writing your own provider instead of using the bundled ones, start with [IMockingProvider](../../api/FastMoq.Providers.IMockingProvider.yml), then [IMockingProviderCapabilities](../../api/FastMoq.Providers.IMockingProviderCapabilities.yml), then [MockingProviderRegistry](../../api/FastMoq.Providers.MockingProviderRegistry.yml).

Detached provider-first creation and verification also live on [MockingProviderRegistry](../../api/FastMoq.Providers.MockingProviderRegistry.yml): use `CreateMock<T>()`, `Verify(...)`, and `VerifyNoOtherCalls(...)` there when the `IFastMock<T>` handle is not tracked inside a `Mocker`.

Reflection-provider reminder: its verification is best-effort and only compares direct constant arguments with `Equals(...)`; it is not equivalent to richer provider-native matcher semantics.

## Logging and verification

- [TestClassExtensions](../../api/FastMoq.Extensions.TestClassExtensions.yml)
- [TimesSpec](../../api/FastMoq.Providers.TimesSpec.yml)
- [MockBehaviorOptions](../../api/FastMoq.MockBehaviorOptions.yml)
- [MockFeatures](../../api/FastMoq.MockFeatures.yml)

## Extension helpers

- [MockerCreationExtensions](../../api/FastMoq.Extensions.MockerCreationExtensions.yml)
- [TestClassExtensions](../../api/FastMoq.Extensions.TestClassExtensions.yml)
- [MockerHttpExtensions](../../api/FastMoq.Extensions.MockerHttpExtensions.yml)
- [MockerBooleanExtensions](../../api/FastMoq.Extensions.MockerBooleanExtensions.yml)
- [ObjectExtensions](../../api/FastMoq.Extensions.ObjectExtensions.yml)

## Framework helpers

- [FunctionContextTestExtensions](../../api/FastMoq.AzureFunctions.Extensions.FunctionContextTestExtensions.yml)
- [TestWebExtensions](../../api/FastMoq.Web.Extensions.TestWebExtensions.yml)

## Database helpers

- [DbContextMockerExtensions](../../api/FastMoq.DbContextMockerExtensions.yml)
- [DbContextHandle&lt;TContext&gt;](../../api/FastMoq.DbContextHandle-1.yml)
- [DbContextHandleOptions&lt;TContext&gt;](../../api/FastMoq.DbContextHandleOptions-1.yml)
- [DbContextTestMode](../../api/FastMoq.DbContextTestMode.yml)

## Common models and helper types

- [LogEntry](../../api/FastMoq.Models.LogEntry.yml)
- [MockModel&lt;T&gt;](../../api/FastMoq.Models.MockModel-1.yml)
- [InstanceModel&lt;TClass&gt;](../../api/FastMoq.Models.InstanceModel-1.yml)
- [ConstructorHistory](../../api/FastMoq.Models.ConstructorHistory.yml)

## Tracked models and provider escape hatches

- [IFastMock&lt;T&gt;](../../api/FastMoq.Providers.IFastMock-1.yml)
- [MockModel&lt;T&gt;](../../api/FastMoq.Models.MockModel-1.yml)
- [MockingProviderRegistry](../../api/FastMoq.Providers.MockingProviderRegistry.yml)

`MockModel` equality is type-based, not instance-based. Use it to reason about mocked service types, not to decide whether two provider-native doubles are the same object.

## Blazor and web

- [MockerBlazorTestBase&lt;T&gt;](../../api/FastMoq.Web.Blazor.MockerBlazorTestBase-1.yml)
- [IMockerBlazorTestHelpers&lt;T&gt;](../../api/FastMoq.Web.Blazor.Interfaces.IMockerBlazorTestHelpers-1.yml)
- [ComponentState&lt;T&gt;](../../api/FastMoq.Web.Blazor.Models.ComponentState-1.yml)

## Fast ways to find a type

- Use the search box in the top-right and search for the exact type name, such as `Mocker` or `MockerTestBase`.
- Use the site navigation to jump into [Core namespace](../../api/FastMoq.yml) when you know the type is in the main FastMoq namespace.
- Use [API overview](./index.md) when you want package-level entry points before drilling into a type.

## Typical navigation paths

- Starting a new service test: [MockerTestBase&lt;TComponent&gt;](../../api/FastMoq.MockerTestBase-1.yml) then [Mocker](../../api/FastMoq.Mocker.yml)
- Configuring provider behavior: [MockingProviderRegistry](../../api/FastMoq.Providers.MockingProviderRegistry.yml) then [IMockingProvider](../../api/FastMoq.Providers.IMockingProvider.yml)
- Writing a custom provider: [IMockingProvider](../../api/FastMoq.Providers.IMockingProvider.yml) then [IMockingProviderCapabilities](../../api/FastMoq.Providers.IMockingProviderCapabilities.yml) then [MockingProviderRegistry](../../api/FastMoq.Providers.MockingProviderRegistry.yml)
- Verifying interactions: [TimesSpec](../../api/FastMoq.Providers.TimesSpec.yml) then [TestClassExtensions](../../api/FastMoq.Extensions.TestClassExtensions.yml)
- Wiring Azure Functions worker tests: [FunctionContextTestExtensions](../../api/FastMoq.AzureFunctions.Extensions.FunctionContextTestExtensions.yml) then [Mocker](../../api/FastMoq.Mocker.yml)
- Working with Blazor components: [MockerBlazorTestBase&lt;T&gt;](../../api/FastMoq.Web.Blazor.MockerBlazorTestBase-1.yml) then [IMockerBlazorTestHelpers&lt;T&gt;](../../api/FastMoq.Web.Blazor.Interfaces.IMockerBlazorTestHelpers-1.yml)

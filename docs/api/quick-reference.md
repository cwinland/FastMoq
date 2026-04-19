# FastMoq Quick Reference

Use this page when you already know the type name and want to jump straight to the API page.

## Most-used starting points

- [Mocker](https://help.fastmoq.com/api/FastMoq.Mocker.html)
- [MockerTestBase&lt;TComponent&gt;](https://help.fastmoq.com/api/FastMoq.MockerTestBase-1.html)
- [ScenarioBuilder&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.ScenarioBuilder-1.html)
- [MockingProviderRegistry](https://help.fastmoq.com/api/FastMoq.Providers.MockingProviderRegistry.html)
- [MockerBlazorTestBase&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Web.Blazor.MockerBlazorTestBase-1.html)

## Core test entry points

- [Mocker](https://help.fastmoq.com/api/FastMoq.Mocker.html)
- [MockerTestBase&lt;TComponent&gt;](https://help.fastmoq.com/api/FastMoq.MockerTestBase-1.html)
- [ScenarioBuilder&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.ScenarioBuilder-1.html)

## Arrange and creation

- [Mocker](https://help.fastmoq.com/api/FastMoq.Mocker.html)
- [IFastMock&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Providers.IFastMock-1.html)
- [KnownTypeRegistration](https://help.fastmoq.com/api/FastMoq.KnownTypeRegistration.html)
- [InstanceCreationFlags](https://help.fastmoq.com/api/FastMoq.InstanceCreationFlags.html)
- [InvocationOptions](https://help.fastmoq.com/api/FastMoq.InvocationOptions.html)

Use the [Mocker](https://help.fastmoq.com/api/FastMoq.Mocker.html) page for the tracked and standalone creation APIs: `GetOrCreateMock<T>()`, `CreateFastMock<T>()`, and `CreateStandaloneFastMock<T>()`.

## Provider APIs

- [MockingProviderRegistry](https://help.fastmoq.com/api/FastMoq.Providers.MockingProviderRegistry.html)
- [IMockingProvider](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProvider.html)
- [IMockingProviderCapabilities](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProviderCapabilities.html)
- [TimesSpec](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html)
- [MockCreationOptions](https://help.fastmoq.com/api/FastMoq.Providers.MockCreationOptions.html)

If you are writing your own provider instead of using the bundled ones, start with [IMockingProvider](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProvider.html), then [IMockingProviderCapabilities](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProviderCapabilities.html), then [MockingProviderRegistry](https://help.fastmoq.com/api/FastMoq.Providers.MockingProviderRegistry.html).

Detached provider-first creation and verification also live on [MockingProviderRegistry](https://help.fastmoq.com/api/FastMoq.Providers.MockingProviderRegistry.html): use `CreateMock<T>()`, `Verify(...)`, and `VerifyNoOtherCalls(...)` there when the `IFastMock<T>` handle is not tracked inside a `Mocker`.

Reflection-provider reminder: its verification is best-effort and only compares direct constant arguments with `Equals(...)`; it is not equivalent to richer provider-native matcher semantics.

## Logging and verification

- [TestClassExtensions](https://help.fastmoq.com/api/FastMoq.Extensions.TestClassExtensions.html)
- [TimesSpec](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html)
- [MockBehaviorOptions](https://help.fastmoq.com/api/FastMoq.MockBehaviorOptions.html)
- [MockFeatures](https://help.fastmoq.com/api/FastMoq.MockFeatures.html)

## Extension helpers

- [MockerCreationExtensions](https://help.fastmoq.com/api/FastMoq.Extensions.MockerCreationExtensions.html)
- [TestClassExtensions](https://help.fastmoq.com/api/FastMoq.Extensions.TestClassExtensions.html)
- [MockerHttpExtensions](https://help.fastmoq.com/api/FastMoq.Extensions.MockerHttpExtensions.html)
- [MockerBooleanExtensions](https://help.fastmoq.com/api/FastMoq.Extensions.MockerBooleanExtensions.html)
- [ObjectExtensions](https://help.fastmoq.com/api/FastMoq.Extensions.ObjectExtensions.html)

## Framework helpers

- [FunctionContextTestExtensions](https://help.fastmoq.com/api/FastMoq.AzureFunctions.Extensions.FunctionContextTestExtensions.html)
- [TestWebExtensions](https://help.fastmoq.com/api/FastMoq.Web.Extensions.TestWebExtensions.html)

## Database helpers

- [DbContextMockerExtensions](https://help.fastmoq.com/api/FastMoq.DbContextMockerExtensions.html)
- [DbContextHandle&lt;TContext&gt;](https://help.fastmoq.com/api/FastMoq.DbContextHandle-1.html)
- [DbContextHandleOptions&lt;TContext&gt;](https://help.fastmoq.com/api/FastMoq.DbContextHandleOptions-1.html)
- [DbContextTestMode](https://help.fastmoq.com/api/FastMoq.DbContextTestMode.html)

## Common models and helper types

- [LogEntry](https://help.fastmoq.com/api/FastMoq.Models.LogEntry.html)
- [MockModel&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Models.MockModel-1.html)
- [InstanceModel&lt;TClass&gt;](https://help.fastmoq.com/api/FastMoq.Models.InstanceModel-1.html)
- [ConstructorHistory](https://help.fastmoq.com/api/FastMoq.Models.ConstructorHistory.html)

## Tracked models and provider escape hatches

- [IFastMock&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Providers.IFastMock-1.html)
- [MockModel&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Models.MockModel-1.html)
- [MockingProviderRegistry](https://help.fastmoq.com/api/FastMoq.Providers.MockingProviderRegistry.html)

`MockModel` equality is type-based, not instance-based. Use it to reason about mocked service types, not to decide whether two provider-native doubles are the same object.

## Blazor and web

- [MockerBlazorTestBase&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Web.Blazor.MockerBlazorTestBase-1.html)
- [IMockerBlazorTestHelpers&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Web.Blazor.Interfaces.IMockerBlazorTestHelpers-1.html)
- [ComponentState&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Web.Blazor.Models.ComponentState-1.html)

## Fast ways to find a type

- Use the search box in the top-right and search for the exact type name, such as `Mocker` or `MockerTestBase`.
- Use the site navigation to jump into [Core namespace](https://help.fastmoq.com/api/FastMoq.html) when you know the type is in the main FastMoq namespace.
- Use [API overview](./index.md) when you want package-level entry points before drilling into a type.

## Typical navigation paths

- Starting a new service test: [MockerTestBase&lt;TComponent&gt;](https://help.fastmoq.com/api/FastMoq.MockerTestBase-1.html) then [Mocker](https://help.fastmoq.com/api/FastMoq.Mocker.html)
- Configuring provider behavior: [MockingProviderRegistry](https://help.fastmoq.com/api/FastMoq.Providers.MockingProviderRegistry.html) then [IMockingProvider](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProvider.html)
- Writing a custom provider: [IMockingProvider](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProvider.html) then [IMockingProviderCapabilities](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProviderCapabilities.html) then [MockingProviderRegistry](https://help.fastmoq.com/api/FastMoq.Providers.MockingProviderRegistry.html)
- Verifying interactions: [TimesSpec](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html) then [TestClassExtensions](https://help.fastmoq.com/api/FastMoq.Extensions.TestClassExtensions.html)
- Wiring Azure Functions worker tests: [FunctionContextTestExtensions](https://help.fastmoq.com/api/FastMoq.AzureFunctions.Extensions.FunctionContextTestExtensions.html) then [Mocker](https://help.fastmoq.com/api/FastMoq.Mocker.html)
- Working with Blazor components: [MockerBlazorTestBase&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Web.Blazor.MockerBlazorTestBase-1.html) then [IMockerBlazorTestHelpers&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Web.Blazor.Interfaces.IMockerBlazorTestHelpers-1.html)

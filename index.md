# FastMoq Documentation

This site combines generated API reference with repo-backed guidance for the FastMoq `4.3.0` line and the current v4 codebase.

## New to FastMoq

- [Quick reference for common types](docs/api/quick-reference.md)
- [Executable testing examples](docs/samples/testing-examples.md)
- [Provider selection guide](docs/getting-started/provider-selection.md)
- [Getting started guide](docs/getting-started/README.md)
- [Testing guide](docs/getting-started/testing-guide.md)

## Upgrading to v4

- The release delta from `3.0.0` is centered on provider-first architecture, the expanded package split, first-party Azure SDK and Azure Functions helpers, analyzer-by-default aggregate installs, explicit policy surfaces, provider-neutral verification, tracked-versus-standalone mock creation, keyed same-type dependency guidance, and the scenario builder.
- Start with the release summary, then use the breaking-change and migration pages to decide how much compatibility behavior your test suites still need.

- [Migration guide](docs/migration/README.md)
- [What's new since 3.0.0](docs/whats-new/README.md)
- [Breaking changes](docs/breaking-changes/README.md)
- [Provider selection guide](docs/getting-started/provider-selection.md)

## Looking for an API

- [Quick reference for common types](docs/api/quick-reference.md)
- [Core namespace reference](https://help.fastmoq.com/api/FastMoq.html)
- [Provider APIs](https://help.fastmoq.com/api/FastMoq.Providers.html)
- [Extension methods](https://help.fastmoq.com/api/FastMoq.Extensions.html)
- [Blazor and web APIs](https://help.fastmoq.com/api/FastMoq.Web.Blazor.html)
- [Models and helper types](https://help.fastmoq.com/api/FastMoq.Models.html)
- [API overview page](docs/api/index.md)

## Real-world examples

Use the example pages first when you want to understand how FastMoq is applied in tests, then drop into API reference pages when you need the exact shape of a type or member.

- [Cookbook recipes](docs/cookbook/README.md)
- [Executable testing examples](docs/samples/testing-examples.md)
- [Sample applications overview](docs/samples/README.md)
- [TestingExample project README](FastMoq.TestingExample/README.md)

## Blazor and web testing

- [MockerBlazorTestBase&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Web.Blazor.MockerBlazorTestBase-1.html)
- [IMockerBlazorTestHelpers&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Web.Blazor.Interfaces.IMockerBlazorTestHelpers-1.html)
- [TestWebExtensions](https://help.fastmoq.com/api/FastMoq.Web.Extensions.TestWebExtensions.html)

## Quick type lookup

- [Quick reference for common types](docs/api/quick-reference.md)
- [Mocker](https://help.fastmoq.com/api/FastMoq.Mocker.html)
- [MockerTestBase&lt;TComponent&gt;](https://help.fastmoq.com/api/FastMoq.MockerTestBase-1.html)
- [ScenarioBuilder&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.ScenarioBuilder-1.html)
- [MockingProviderRegistry](https://help.fastmoq.com/api/FastMoq.Providers.MockingProviderRegistry.html)

## How to find a type fast

- Use the top-right search box for exact names such as `Mocker`, `MockerTestBase`, or `ScenarioBuilder`.
- Use [Quick reference for common types](docs/api/quick-reference.md) when you want one-click links to the most-used APIs.
- Use the left-side site navigation when you want to browse by topic instead of by type name.

## Package map

### FastMoq

The aggregate package that pulls together the core runtime, shared Azure SDK helpers, Azure Functions helpers, database helpers, web support, provider integrations, and analyzer assets.

- [FastMoq namespace](https://help.fastmoq.com/api/FastMoq.html)
- [Extension methods](https://help.fastmoq.com/api/FastMoq.Extensions.html)
- [Provider APIs](https://help.fastmoq.com/api/FastMoq.Providers.html)
- [Web and Blazor APIs](https://help.fastmoq.com/api/FastMoq.Web.Blazor.html)

### FastMoq.Azure

Shared Azure SDK helpers for pageable builders, credential setup, Azure-oriented configuration or service-provider flows, and common client registration.

- [Getting started package choices](docs/getting-started/README.md#package-choices)
- [Sample applications overview](docs/samples/README.md)
- [What's new since 3.0.0](docs/whats-new/README.md)

### FastMoq.AzureFunctions

Azure Functions worker and HTTP-trigger helpers, including `FunctionContext.InstanceServices`, concrete `HttpRequestData` and `HttpResponseData` builders, and body readers.

- [Azure Functions extensions namespace](https://help.fastmoq.com/api/FastMoq.AzureFunctions.Extensions.html)
- [FunctionContextTestExtensions](https://help.fastmoq.com/api/FastMoq.AzureFunctions.Extensions.FunctionContextTestExtensions.html)
- [Testing guide](docs/getting-started/testing-guide.md)

### FastMoq.Abstractions

Provider contracts shared by the core runtime and provider-specific packages.

- [Provider APIs](https://help.fastmoq.com/api/FastMoq.Providers.html)
- [IMockingProvider](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProvider.html)
- [IMockingProviderCapabilities](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProviderCapabilities.html)

### FastMoq.Core

Core mocking, auto-construction, instance resolution, and provider-neutral verification support.

- [Mocker](https://help.fastmoq.com/api/FastMoq.Mocker.html)
- [MockerTestBase&lt;TComponent&gt;](https://help.fastmoq.com/api/FastMoq.MockerTestBase-1.html)
- [ScenarioBuilder&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.ScenarioBuilder-1.html)
- [MockingProviderRegistry](https://help.fastmoq.com/api/FastMoq.Providers.MockingProviderRegistry.html)
- [TimesSpec](https://help.fastmoq.com/api/FastMoq.Providers.TimesSpec.html)

### FastMoq.Database

EF and DbContext-oriented helpers that stay exposed through the main `FastMoq` namespace.

- [DbContextMockerExtensions](https://help.fastmoq.com/api/FastMoq.DbContextMockerExtensions.html)
- [DbContextHandle&lt;TContext&gt;](https://help.fastmoq.com/api/FastMoq.DbContextHandle-1.html)
- [DbContextHandleOptions&lt;TContext&gt;](https://help.fastmoq.com/api/FastMoq.DbContextHandleOptions-1.html)
- [DbContextTestMode](https://help.fastmoq.com/api/FastMoq.DbContextTestMode.html)

### FastMoq.Web

Blazor and web-oriented testing helpers.

- [MockerBlazorTestBase&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Web.Blazor.MockerBlazorTestBase-1.html)
- [IMockerBlazorTestHelpers&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Web.Blazor.Interfaces.IMockerBlazorTestHelpers-1.html)
- [Blazor namespace](https://help.fastmoq.com/api/FastMoq.Web.Blazor.html)

### FastMoq.Provider.Moq

Moq compatibility provider package for v4 migration and existing Moq-heavy tests.

- [Moq provider types](https://help.fastmoq.com/api/FastMoq.Providers.MoqProvider.html)
- [IFastMock&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Providers.IFastMock-1.html)
- [MockingProviderRegistry](https://help.fastmoq.com/api/FastMoq.Providers.MockingProviderRegistry.html)

### FastMoq.Provider.NSubstitute

Optional NSubstitute provider package for teams standardizing on NSubstitute instead of Moq.

- [NSubstitute provider types](https://help.fastmoq.com/api/FastMoq.Providers.NSubstituteProvider.html)
- [IMockingProvider](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProvider.html)
- [MockingProviderRegistry](https://help.fastmoq.com/api/FastMoq.Providers.MockingProviderRegistry.html)

### FastMoq.Analyzers

Roslyn analyzers and code fixes for provider-first guidance and migration cleanup. These analyzer assets are included by default in the aggregate `FastMoq` package.

- [Getting started package choices](docs/getting-started/README.md#package-choices)
- [Migration guide](docs/migration/README.md)

## Provider-first APIs

FastMoq v4 defaults to the built-in reflection provider. Moq remains available as a compatibility provider, and additional providers can be added explicitly.

Tracked mock access stays on `GetOrCreateMock<T>()`. Use `CreateStandaloneFastMock<T>()` or `MockingProviderRegistry.Default.CreateMock<T>()` when you need a detached extra handle, and use `CreateFastMock<T>()` only when you intentionally want a new tracked registration in the current `Mocker`.

- [IMockingProvider](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProvider.html)
- [IMockingProviderCapabilities](https://help.fastmoq.com/api/FastMoq.Providers.IMockingProviderCapabilities.html)
- [MockCreationOptions](https://help.fastmoq.com/api/FastMoq.Providers.MockCreationOptions.html)
- [Moq provider types](https://help.fastmoq.com/api/FastMoq.Providers.MoqProvider.html)
- [NSubstitute provider types](https://help.fastmoq.com/api/FastMoq.Providers.NSubstituteProvider.html)

## Notes

- Generated API reference from XML comments across the FastMoq projects
- Conceptual example pages published from the repository's sample documentation
- Searchable HTML output suitable for GitHub Pages and the `help.fastmoq.com` host
- A reproducible build path using the pinned local DocFX tool manifest

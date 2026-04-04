# FastMoq Documentation

This site combines generated API reference with real-world example documentation for the current FastMoq v4 codebase.

## New to FastMoq

- [Quick reference for common types](docs/api/quick-reference.md)
- [Executable testing examples](docs/samples/testing-examples.md)
- [Provider selection guide](docs/getting-started/provider-selection.md)
- [Getting started guide](docs/getting-started/README.md)
- [Testing guide](docs/getting-started/testing-guide.md)

## Upgrading to v4

- [Migration guide](docs/migration/README.md)
- [What's new since 3.0.0](docs/whats-new/README.md)
- [Breaking changes](docs/breaking-changes/README.md)
- [Provider selection guide](docs/getting-started/provider-selection.md)

## Looking for an API

- [Quick reference for common types](docs/api/quick-reference.md)
- [Core namespace reference](api/FastMoq.yml)
- [Provider APIs](api/FastMoq.Providers.yml)
- [Extension methods](api/FastMoq.Extensions.yml)
- [Blazor and web APIs](api/FastMoq.Web.Blazor.yml)
- [Models and helper types](api/FastMoq.Models.yml)
- [API overview page](docs/api/index.md)

## Real-world examples

Use the example pages first when you want to understand how FastMoq is applied in tests, then drop into API reference pages when you need the exact shape of a type or member.

- [Executable testing examples](docs/samples/testing-examples.md)
- [Sample applications overview](docs/samples/README.md)
- [TestingExample project README](FastMoq.TestingExample/README.md)

## Blazor and web testing

- [MockerBlazorTestBase&lt;T&gt;](api/FastMoq.Web.Blazor.MockerBlazorTestBase-1.yml)
- [IMockerBlazorTestHelpers&lt;T&gt;](api/FastMoq.Web.Blazor.Interfaces.IMockerBlazorTestHelpers-1.yml)
- [TestWebExtensions](api/FastMoq.Web.Extensions.TestWebExtensions.yml)

## Quick type lookup

- [Quick reference for common types](docs/api/quick-reference.md)
- [Mocker](api/FastMoq.Mocker.yml)
- [MockerTestBase&lt;TComponent&gt;](api/FastMoq.MockerTestBase-1.yml)
- [ScenarioBuilder&lt;T&gt;](api/FastMoq.ScenarioBuilder-1.yml)
- [MockingProviderRegistry](api/FastMoq.Providers.MockingProviderRegistry.yml)

## How to find a type fast

- Use the top-right search box for exact names such as `Mocker`, `MockerTestBase`, or `ScenarioBuilder`.
- Use [Quick reference for common types](docs/api/quick-reference.md) when you want one-click links to the most-used APIs.
- Use the left-side site navigation when you want to browse by topic instead of by type name.

## Package map

### FastMoq

The aggregate package that pulls together the core runtime, database helpers, and web support.

- [FastMoq namespace](api/FastMoq.yml)
- [Extension methods](api/FastMoq.Extensions.yml)
- [Provider APIs](api/FastMoq.Providers.yml)
- [Web and Blazor APIs](api/FastMoq.Web.Blazor.yml)

### FastMoq.Abstractions

Provider contracts shared by the core runtime and provider-specific packages.

- [Provider APIs](api/FastMoq.Providers.yml)
- [IMockingProvider](api/FastMoq.Providers.IMockingProvider.yml)
- [IMockingProviderCapabilities](api/FastMoq.Providers.IMockingProviderCapabilities.yml)

### FastMoq.Core

Core mocking, auto-construction, instance resolution, and provider-neutral verification support.

- [Mocker](api/FastMoq.Mocker.yml)
- [MockerTestBase&lt;TComponent&gt;](api/FastMoq.MockerTestBase-1.yml)
- [ScenarioBuilder&lt;T&gt;](api/FastMoq.ScenarioBuilder-1.yml)
- [MockingProviderRegistry](api/FastMoq.Providers.MockingProviderRegistry.yml)
- [TimesSpec](api/FastMoq.Providers.TimesSpec.yml)

### FastMoq.Database

EF and DbContext-oriented helpers that stay exposed through the main `FastMoq` namespace.

- [DbContextMockerExtensions](api/FastMoq.DbContextMockerExtensions.yml)
- [DbContextHandle&lt;TContext&gt;](api/FastMoq.DbContextHandle-1.yml)
- [DbContextHandleOptions&lt;TContext&gt;](api/FastMoq.DbContextHandleOptions-1.yml)
- [DbContextTestMode](api/FastMoq.DbContextTestMode.yml)

### FastMoq.Web

Blazor and web-oriented testing helpers.

- [MockerBlazorTestBase&lt;T&gt;](api/FastMoq.Web.Blazor.MockerBlazorTestBase-1.yml)
- [IMockerBlazorTestHelpers&lt;T&gt;](api/FastMoq.Web.Blazor.Interfaces.IMockerBlazorTestHelpers-1.yml)
- [Blazor namespace](api/FastMoq.Web.Blazor.yml)

### FastMoq.Provider.Moq

Moq compatibility provider package for v4 migration and existing Moq-heavy tests.

- [Moq provider types](api/FastMoq.Providers.MoqProvider.yml)
- [MockWrapper&lt;T&gt;](api/FastMoq.Core.Providers.MockWrapper-1.yml)
- [MockingProviderRegistry](api/FastMoq.Providers.MockingProviderRegistry.yml)

### FastMoq.Provider.NSubstitute

Optional NSubstitute provider package for teams standardizing on NSubstitute instead of Moq.

- [NSubstitute provider types](api/FastMoq.Providers.NSubstituteProvider.yml)
- [IMockingProvider](api/FastMoq.Providers.IMockingProvider.yml)
- [MockingProviderRegistry](api/FastMoq.Providers.MockingProviderRegistry.yml)

## Provider-first APIs

FastMoq v4 defaults to the built-in reflection provider. Moq remains available as a compatibility provider, and additional providers can be added explicitly.

- [IMockingProvider](api/FastMoq.Providers.IMockingProvider.yml)
- [IMockingProviderCapabilities](api/FastMoq.Providers.IMockingProviderCapabilities.yml)
- [MockCreationOptions](api/FastMoq.Providers.MockCreationOptions.yml)
- [Moq provider types](api/FastMoq.Providers.MoqProvider.yml)
- [NSubstitute provider types](api/FastMoq.Providers.NSubstituteProvider.yml)

## Notes

- Generated API reference from XML comments across the FastMoq projects
- Conceptual example pages published from the repository's sample documentation
- Searchable HTML output suitable for GitHub Pages and the `help.fastmoq.com` host
- A reproducible build path using the pinned local DocFX tool manifest

## Generate locally

Run the clean generation script from the repository root:

```powershell
pwsh ./scripts/Generate-ApiDocs.ps1
```

That script removes the existing `Help` folder before rebuilding so stale files are not kept between runs.

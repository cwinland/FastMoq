# FastMoq Sample Applications

This directory contains the sample documentation and executable examples that currently exist in this repository.

## Available samples in this repository

1. **[E-Commerce Order Processing](./ecommerce-orders/README.md)** - Full sample documentation for an order-processing workflow
2. **[Executable Testing Examples](./testing-examples.md)** - Smaller repo-local service examples backed by the `FastMoq.TestingExample` project

## Repo-local executable examples

If you want smaller service-focused examples that compile and run directly in this repository, start with [Executable Testing Examples](./testing-examples.md).

Those examples are backed by the `FastMoq.TestingExample` project and currently show:

- `MockerTestBase<TComponent>` in realistic service tests
- built-in `IFileSystem` behavior with `MockFileSystem`
- `VerifyLogged(...)` assertions
- fluent `Scenario.With(...).When(...).Then(...).Verify(...)` usage inside `MockerTestBase<TComponent>`
- provider-first verification with `TimesSpec.Once`, `TimesSpec.NeverCalled`, `TimesSpec.Exactly(...)`, `TimesSpec.AtLeast(...)`, and `TimesSpec.AtMost(...)`

## Common patterns demonstrated

- **Azure Service Bus** integration and testing
- **Azure Blob Storage** operations
- **Azure Key Vault** configuration
- **Entity Framework Core** with Azure SQL
- **HttpClient** and external API integration
- **Background Services** and hosted services
- **Blazor Components** testing
- **API Controllers** with complex dependencies
- **Configuration and Options** patterns
- **Logging and Monitoring** integration

## FastMoq Extension Usage in Samples

The sample test projects intentionally showcase FastMoq extension helpers so you can apply them directly:

### HTTP / External API

- `CreateHttpClient()` to quickly register an `HttpClient` and (if needed) an `IHttpClientFactory` with a default response.
- Prefer `WhenHttpRequest(...)` and `WhenHttpRequestJson(...)` for provider-neutral request matching and response setup.
- Use `SetupHttpMessage(...)` only when you intentionally need Moq-specific protected `SendAsync` behavior from the Moq provider package. Keep `using FastMoq.Extensions;`, add `FastMoq.Provider.Moq`, and select the Moq provider for the test assembly when you use that compatibility path.
- Content helpers: `GetStringContent`, `GetContentBytesAsync()`, `GetContentStreamAsync()` for asserting raw payloads.

### Entity Framework Core

- `GetMockDbContext<TContext>()` to obtain a mock context with DbSets auto‑prepared.
- Add a custom variant via `AddType` if you want to pin a specific test-time implementation.

### Logging Verification

- Prefer `Mocks.VerifyLogged(LogLevel.Information, "Message")` for provider-safe logger assertions.
- Use `GetOrCreateMock<ILogger<T>>().AsMoq().VerifyLogger(...)` only when you intentionally need the legacy Moq-specific compatibility behavior, such as minimizing churn in older Moq-shaped tests during the v4 transition. It is not the preferred path for new assertions just because you want more control; prefer `VerifyLogged(...)` unless you specifically need the old Moq-only assertion surface.

### Constructor & Dependency Injection

- `CreateInstance<T>()` / typed overloads pick the correct constructor and auto‑inject mocks.
- `AddType<TAbstraction>(factory)` to pin specific concrete/instance values (e.g., seeded `Uri` or options).

### Azure Service Patterns

Even when not running in Azure, the samples demonstrate how you would substitute Azure types:

- Wrap Azure SDK clients with interfaces in your application layer and mock those interfaces in tests.
- Use consistent naming for senders/processors so verification (e.g., Service Bus send) is easy.

## Azure Function style testing guidance

If you adapt these samples for Azure Functions HTTP triggers, prefer the first-party helpers in `FastMoq.AzureFunctions.Extensions` instead of hand-rolled `MockedHttpRequestData` or `MockedHttpResponseData` utilities when possible:

1. Build the request with `CreateHttpRequestData(...)`: supply method, route values, headers, claims, query-string values, and JSON body.
2. Provide dependency injection values using `AddType(...)`, `AddServiceProvider(...)`, or `AddFunctionContextInstanceServices(...)` when the function resolves framework services.
3. Use `WhenHttpRequest(...)` or `WhenHttpRequestJson(...)` for outbound `HttpClient` calls triggered inside the function.
4. Assert:
   - Outbound calls (verify `SendAsync`)
   - Response status and body (`ReadBodyAsStringAsync(...)` or `ReadBodyAsJsonAsync<T>(...)`)
   - Logs via `VerifyLogged(...)`

Recommended layering:

```csharp
using FastMoq.AzureFunctions.Extensions;

var request = Mocks.CreateHttpRequestData(builder => builder
    .WithMethod("POST")
    .WithUrl("http://test.com/api/Run?mode=sample")
    .WithJsonBody(payload));

var result = await Component.RunAsync(request, CancellationToken.None);
var body = await result.ReadBodyAsStringAsync();
Mocks.VerifyLogged(LogLevel.Information, "Processed event");
```

The Azure Functions helper package now supplies:

- Concrete `HttpRequestData` and `HttpResponseData` builders for HTTP-trigger tests.
- Automatic `FunctionContext.InstanceServices` setup with worker defaults.
- Body readers that rewind request and response streams after assertions.
- The same provider-neutral logger and `HttpClient` helpers used elsewhere in FastMoq.

## Notes about repository scope

Some sample categories mentioned elsewhere in the documentation are future sample directions rather than folders that currently exist in this repository. Use this page and the linked directories above as the source of truth for what is available today.

## Sample enhancement ideas

The current samples are intentionally focused. Consider extending locally with:

- Adding a payment gateway client abstraction and testing retry/backoff logic via provider-neutral request helpers for happy-path routing, then Moq `SetupSequence(...)` only when you intentionally need provider-specific call sequencing.
- Adding blob metadata assertions using a mocked `BlobClient`.
- Introducing an options reload test using `IOptionsMonitor<T>` + updated values via `AddType`.

## Quick Reference: Which Helper to Choose?

| Goal | Helper | Notes |
| ---- | ------ | ----- |
| Fast default HttpClient | `CreateHttpClient()` | Registers handler + factory if missing |
| Custom per‑test HTTP behavior | `WhenHttpRequest()` / `WhenHttpRequestJson()` | Provider-neutral request matching and response setup |
| Mock EF Core context | `GetMockDbContext<T>()` | Auto sets up DbSets; seed data before use |
| Replace concrete dependency | `AddType<T>()` | Pin deterministic instances (e.g., clock) |
| Verify log message | `VerifyLogged(...)` | Provider-safe assertion over captured `ILogger` entries |
| Extract HTTP content | `GetStringContent` | Use for string assertions |

> Tip: Prefer the provider-neutral HTTP helpers first; drop down to Moq-specific setup only for protected-member or sequencing edge cases.

## Getting started

Start with [Executable Testing Examples](./testing-examples.md) if you want the quickest path to real, runnable tests in this repository. Use [E-Commerce Order Processing](./ecommerce-orders/README.md) when you want a larger sample walkthrough.

## Prerequisites

- .NET 8.0 or later
- Azure subscription (for cloud features)
- Docker Desktop (optional, for containerized development)
- Visual Studio 2022 or VS Code

## Quick Start

1. Clone the repository
2. Navigate to a sample directory
3. Follow the sample-specific README
4. Run the tests to see FastMoq in action

```bash
cd docs/samples/ecommerce-orders
dotnet restore
dotnet test
```

For the smaller executable examples instead:

```bash
dotnet test .\FastMoq.TestingExample\FastMoq.TestingExample.csproj
```

## Learning Objectives

After exploring these samples, you'll understand how to:

- Structure tests for complex, real-world applications
- Mock Azure services and external dependencies
- Test asynchronous and background operations
- Verify logging and monitoring behavior
- Handle configuration and secrets in tests
- Test web applications and APIs comprehensively
- Implement integration testing strategies

## Support

If you have questions about the samples or need help adapting them to your specific use case, please:

1. Check the individual sample README files
2. Review the [main documentation](../../index.md)
3. Open an issue on the [FastMoq repository](https://github.com/cwinland/FastMoq/issues)

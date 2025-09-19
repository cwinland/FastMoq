# FastMoq Sample Applications

This directory contains complete sample applications demonstrating FastMoq's capabilities in real-world scenarios, particularly focusing on modern .NET and Azure integration patterns.

## Sample Applications

1. **[E-Commerce Order Processing](./ecommerce-orders/)** - Complete order processing system with Azure integration
2. **[Microservices Communication](./microservices/)** - Service-to-service communication patterns
3. **[Background Processing](./background-services/)** - Queue processing and background jobs
4. **[Blazor Web Application](./blazor-webapp/)** - Full-stack Blazor application with testing

## Common Patterns Demonstrated

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
- `SetupHttpMessage(...)` for per‑test customization (status codes, payloads, sequences).
- Content helpers: `GetStringContent`, `GetContentBytesAsync()`, `GetContentStreamAsync()` for asserting raw payloads.

### Entity Framework Core

- `GetMockDbContext<TContext>()` to obtain a mock context with DbSets auto‑prepared.
- Add the real in‑memory variant via `AddType` if you want a lightweight functional test using SQLite in memory.

### Logging Verification

- Use `Mocks.GetMock<ILogger<T>>() .VerifyLogger(LogLevel.Information, "Message")` instead of direct invocation inspection.

### Constructor & Dependency Injection

- `CreateInstance<T>()` / typed overloads pick the correct constructor and auto‑inject mocks.
- `AddType<TAbstraction>(factory)` to pin specific concrete/instance values (e.g., seeded `DbConnection`, `Uri`, or options).

### Azure Service Patterns

Even when not running in Azure, the samples demonstrate how you would substitute Azure types:

- Wrap Azure SDK clients with interfaces in your application layer and mock those interfaces in tests.
- Use consistent naming for senders/processors so verification (e.g., Service Bus send) is easy.

## Azure Function Style Testing (Guidance)

If you adapt these samples for Azure Functions HTTP triggers, use patterns similar to (conceptually) `MockedHttpRequestData` and `MockedHttpResponseData` helpers (as seen in other internal solution test utilities):

1. Build the request: supply method, route values, headers, and JSON body.
2. Provide dependency injection values using `AddType` (e.g., configuration, services).
3. Use `SetupHttpMessage` for outbound `HttpClient` calls triggered inside the function.
4. Assert:
   - Outbound calls (verify `SendAsync`)
   - Response status & body (deserialize or use content helpers)
   - Logs via `VerifyLogger`

Recommended layering:

```csharp
var request = TestUtils.CreateHttpRequest(jsonBody, queryParams);
var result = await Component.RunAsync(request, CancellationToken.None);
Mocks.GetMock<ILogger<MyFunction>>()
   .VerifyLogger(LogLevel.Information, "Processed event");
```

While FastMoq does not ship Azure Functions request/response shims directly, it complements such utilities by supplying:

- Automatic logger mocks with capture/verification.
- Consistent `HttpClient` mocking for downstream REST calls.
- Simplified DI graph creation so only function inputs need explicit arrangement.

## Sample Enhancement Ideas

The current samples are intentionally minimal. Consider extending locally with:

- Adding a payment gateway client abstraction and testing retry/backoff logic via `SetupHttpMessage` sequences.
- Adding blob metadata assertions using a mocked `BlobClient`.
- Introducing an options reload test using `IOptionsMonitor<T>` + updated values via `AddType`.

## Quick Reference: Which Helper to Choose?

| Goal | Helper | Notes |
|------|--------|-------|
| Fast default HttpClient | `CreateHttpClient()` | Registers handler + factory if missing |
| Custom per‑test response | `SetupHttpMessage()` | Use multiple calls for sequential responses |
| Mock EF Core context | `GetMockDbContext<T>()` | Auto sets up DbSets; seed data before use |
| Replace concrete dependency | `AddType<T>()` | Pin deterministic instances (e.g., clock) |
| Verify log message | `VerifyLogger(...)` | Works on `ILogger` or `ILogger<T>` mocks |
| Extract HTTP content | `GetStringContent` | Use for string assertions |

> Tip: Prefer the extension helpers first; drop down to raw Moq setup only for edge cases.

## Getting Started

Each sample application includes:

- Complete source code
- Comprehensive test suite using FastMoq
- Docker configuration for local development
- README with setup instructions
- Azure deployment templates

Choose a sample based on your use case and follow the individual README files for setup instructions.

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
2. Review the [main documentation](../../README.md)
3. Open an issue on the [FastMoq repository](https://github.com/cwinland/FastMoq/issues)

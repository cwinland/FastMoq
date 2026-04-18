# [FastMoq](http://help.fastmoq.com/)

FastMoq is a .NET testing framework for auto-mocking, dependency injection, and test-focused object creation. In v4, it supports a provider-first architecture with a bundled reflection default and optional Moq compatibility when you need it.

## Start Here

- If you are evaluating the project for the first time, start with `Why FastMoq`, `Packages`, and the documentation links below.
- If you want the shortest path to writing a test, go straight to [Getting Started Guide](./docs/getting-started).
- If you are upgrading from the `3.0.0` line, go straight to [Migration Guide](./docs/migration).

## Release Highlights Since 3.0.0

- provider-first architecture built around `IMockingProvider`, `IFastMock<T>`, and `MockingProviderRegistry`, with the bundled `reflection` default and optional provider-specific integrations
- current package split across `FastMoq`, `FastMoq.Abstractions`, `FastMoq.Azure`, `FastMoq.AzureFunctions`, `FastMoq.Database`, `FastMoq.Web`, `FastMoq.Provider.Moq`, and `FastMoq.Provider.NSubstitute`
- first-party Azure SDK helpers for pageable builders, credential setup, Azure-oriented configuration and service-provider flows, and common client registration
- first-party Azure Functions helpers for typed `FunctionContext.InstanceServices`, concrete `HttpRequestData` and `HttpResponseData` builders, and request or response body readers
- `FastMoq` and `FastMoq.Core` now include the FastMoq analyzer pack by default, while `FastMoq.Analyzers` remains available when you want diagnostics without either runtime package
- provider-neutral verification with `Verify(...)`, `VerifyLogged(...)`, and `TimesSpec`, plus fluent `Scenario.With(...).When(...).Then(...).Verify(...)` flows
- explicit construction, invocation, and known-type policies instead of older coupled option bags, with expanded migration docs, executable examples, and generated API coverage

## Why FastMoq

FastMoq is still intended to remove boilerplate compared with using a mock provider directly.

The value is not only shorter setup calls. The bigger value is that FastMoq keeps the repetitive test harness work out of the test body:

- no separate mock field for every constructor dependency
- no manual `new Mock<T>()` declarations just to build the subject under test
- no long constructor call that must be updated every time the component gains a new dependency
- built-in support for common framework-heavy test types such as `ILogger`, `IFileSystem`, `HttpClient`, and `DbContext`
- tracked mocks plus provider-neutral verification and logging helpers

### FastMoq vs Direct Mock Provider Usage

Using a mock provider directly:

```csharp
public class OrderProcessingServiceTests
{
    [Fact]
    public async Task PlaceOrderAsync_ShouldPersistAndLog_WhenReservationAndPaymentSucceed()
    {
        var inventoryGateway = new Mock<IInventoryGateway>();
        var paymentGateway = new Mock<IPaymentGateway>();
        var orderRepository = new Mock<IOrderRepository>();
        var logger = new Mock<ILogger<OrderProcessingService>>();

        var component = new OrderProcessingService(
            inventoryGateway.Object,
            paymentGateway.Object,
            orderRepository.Object,
            logger.Object);

        inventoryGateway
            .Setup(x => x.ReserveAsync("SKU-RED-CHAIR", 2, CancellationToken.None))
            .ReturnsAsync(true);

        paymentGateway
            .Setup(x => x.ChargeAsync("cust-42", 149.90m, CancellationToken.None))
            .ReturnsAsync("pay_12345");

        var result = await component.PlaceOrderAsync(new OrderRequest
        {
            CustomerId = "cust-42",
            Sku = "SKU-RED-CHAIR",
            Quantity = 2,
            TotalAmount = 149.90m,
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        orderRepository.Verify(x => x.SaveAsync(It.IsAny<OrderRecord>(), CancellationToken.None), Times.Once);
    }
}
```

Using FastMoq:

```csharp
public class OrderProcessingServiceTests : MockerTestBase<OrderProcessingService>
{
    [Fact]
    public async Task PlaceOrderAsync_ShouldPersistAndLog_WhenReservationAndPaymentSucceed()
    {
        Mocks.GetOrCreateMock<IInventoryGateway>()
            .Setup(x => x.ReserveAsync("SKU-RED-CHAIR", 2, CancellationToken.None))
            .ReturnsAsync(true);

        Mocks.GetOrCreateMock<IPaymentGateway>()
            .Setup(x => x.ChargeAsync("cust-42", 149.90m, CancellationToken.None))
            .ReturnsAsync("pay_12345");

        var result = await Component.PlaceOrderAsync(new OrderRequest
        {
            CustomerId = "cust-42",
            Sku = "SKU-RED-CHAIR",
            Quantity = 2,
            TotalAmount = 149.90m,
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        Mocks.Verify<IOrderRepository>(x => x.SaveAsync(It.IsAny<OrderRecord>(), CancellationToken.None), TimesSpec.Once);
        Mocks.VerifyLogged(LogLevel.Information, "Placed order");
    }
}
```

The FastMoq version removes explicit mock declarations, subject construction, and logger-plumbing code while still allowing provider-specific setup when you need it.

## 📚 Documentation

### Quick Links

- **🚀 [Getting Started Guide](./docs/getting-started)** - Your first FastMoq test in 5 minutes
- **🧪 [Testing Guide](./docs/getting-started/testing-guide.md)** - Repo-native guidance for `GetOrCreateMock<T>()`, `AddType(...)`, `DbContext`, `IFileSystem`, and known types
- **🌐 [Web Helper Guidance](./docs/getting-started/testing-guide.md#controller-testing)** - Controller, `HttpContext`, `IHttpContextAccessor`, and claims-principal test setup
- **🔌 [Provider Selection Guide](./docs/getting-started/provider-selection.md)** - How to register, select, and bootstrap providers for a test assembly
- **📋 [Provider Capabilities](./docs/getting-started/provider-capabilities.md)** - What `moq`, `nsubstitute`, and `reflection` support today, with recommended usage patterns
- **👨‍🍳 [Cookbook](./docs/cookbook)** - Real-world patterns and recipes
- **🏗️ [Sample Applications](./docs/samples)** - Complete examples with Azure integration
- **🧪 [Executable Testing Examples](./docs/samples/testing-examples.md)** - Repo-backed service examples using the current FastMoq API direction
- **📊 [Feature Comparison](./docs/feature-parity)** - FastMoq vs Moq/NSubstitute
- **📈 [Performance Benchmarks](./docs/benchmarks)** - Productivity and performance metrics

### Additional Resources

- **📖 [Complete Documentation](./docs)** - All guides and references in one place
- **🗺️ [Roadmap Notes](./docs/roadmap)** - Current provider-first direction and deferred backlog items
- **🆕 [What's New Since 3.0.0](./docs/whats-new)** - Summary of the major post-`3.0.0` architecture, packaging, and API changes
- **⚠️ [Breaking Changes](./docs/breaking-changes)** - Intentional v4 behavior changes relative to the `3.0.0` public release
- **🔄 [Migration Guide](./docs/migration)** - Practical old-to-new guidance from `3.0.0` to the current repo direction
- **❓ [FAQs](./FAQs.md)** - Frequently asked questions and troubleshooting
- **🔗 [API Documentation](https://help.fastmoq.com/)** - Generated HTML API reference

## Features

- NOW BLAZOR SUPPORT in FastMoq and FastMoq.Web.
- Test without declaring Mocks (unless needed).
- Creates objects with chain of automatic injections in objects and their dependencies.
- Creates Mocks and Objects with properties populated.
- Automatically injects and creates components or services.
- Injection: Automatically determines what interfaces need to be injected into the constructor and creates mocks if they do not exist.
  - Generate Mock using specific data.
  - Best guess picks the multiple parameter constructor over the default constructor.
  - Specific mapping allows the tester to create an instance using a specific constructor and specific data.
  - Supports Inject Attributes and multiple constructors.
- Use Mocks without managing fields and properties. Mocks are managed by the Mocker framework. No need to keep track of Mocks. Just use them!
- Create instances of Mocks with non public constructors.
- HttpClient and IFileSystem test helpers
- DbContext support through the optional `FastMoq.Database` package, with the primary calls staying in the `FastMoq` namespace.
- Supports Null method parameter testing.
- **Comprehensive Documentation** - Complete guides, samples, and real-world patterns.
- **Lower Test Boilerplate** - Less fixture code than manual mock declaration plus manual subject construction.

## Packages

- FastMoq - Aggregate package that combines the primary FastMoq runtime, shared Azure SDK helpers, Azure Functions helpers, database helpers, web support, provider integrations, and the FastMoq analyzer pack.
- FastMoq.Analyzers - Standalone Roslyn analyzers and code fixes for provider-first guidance and migration cleanup.
- FastMoq.Abstractions - Shared provider contracts used by core and provider packages.
- FastMoq.Core - Core testing Mocker and provider-first resolution pipeline.
- FastMoq.Azure - Shared Azure SDK testing helpers for credentials, pageable builders, Azure-oriented configuration, and common client registration.
- FastMoq.AzureFunctions - Azure Functions worker and HTTP-trigger helpers for typed FunctionContext.InstanceServices setup, concrete HttpRequestData and HttpResponseData builders, and body readers.
- FastMoq.Database - Entity Framework and DbContext-focused helpers.
- FastMoq.Provider.Moq - Moq compatibility provider and Moq-specific convenience extensions for v4 migration.
- FastMoq.Provider.NSubstitute - Optional NSubstitute provider package.
- FastMoq.Web - Blazor and web support.

In the current v4 layout, `FastMoq.Core` bundles the internal `reflection` provider and the bundled `moq` compatibility provider. The default provider is `reflection`. Additional providers such as `nsubstitute` can be added explicitly.

Web-helper note:

- if you install the aggregate `FastMoq` package, the web helpers are included
- if you install `FastMoq.Core` directly, add `FastMoq.Web` before using `CreateHttpContext(...)`, `CreateControllerContext(...)`, `SetupClaimsPrincipal(...)`, `AddHttpContext(...)`, or `AddHttpContextAccessor(...)`
- for migration-specific guidance, start with [Migration Guide](./docs/migration/README.md#web-test-helpers) and then use [Testing Guide](./docs/getting-started/testing-guide.md#controller-testing) for the day-to-day helper rules
- for the full package-choice overview, use [Getting Started](./docs/getting-started/README.md#package-choices)

Azure Functions helper note:

- if you install the aggregate `FastMoq` package, the Azure Functions helpers are included
- if you install `FastMoq.Core` directly, add `FastMoq.AzureFunctions` and import `FastMoq.AzureFunctions.Extensions` before using `CreateFunctionContextInstanceServices(...)`, `AddFunctionContextInstanceServices(...)`, `CreateHttpRequestData(...)`, or `CreateHttpResponseData(...)`
- the typed `CreateTypedServiceProvider(...)` and `AddServiceProvider(...)` helpers remain in `FastMoq.Core`, while the request and response body readers stay in `FastMoq.AzureFunctions.Extensions`

Azure SDK helper note:

- if you install the aggregate `FastMoq` package, the shared Azure SDK helpers are included
- if you install `FastMoq.Core` directly, add `FastMoq.Azure` before using `PageableBuilder`, `AddTokenCredential(...)`, `CreateAzureServiceProvider(...)`, or the Azure client registration helpers
- the Azure helper namespaces are split by concern under `FastMoq.Azure.Pageable`, `FastMoq.Azure.Credentials`, `FastMoq.Azure.DependencyInjection`, `FastMoq.Azure.Storage`, and `FastMoq.Azure.KeyVault`

Typical split-package install:

```bash
dotnet add package FastMoq.Core
dotnet add package FastMoq.Azure
dotnet add package FastMoq.AzureFunctions
dotnet add package FastMoq.Database
dotnet add package FastMoq.Web
```

`GetMockDbContext<TContext>()` keeps the same main call shape in the `FastMoq` namespace. If you install `FastMoq`, the EF helpers are included. If you install `FastMoq.Core` directly, add `FastMoq.Database` for DbContext support.

`PageableBuilder`, `AddTokenCredential(...)`, `AddDefaultAzureCredential(...)`, `CreateAzureConfiguration(...)`, `CreateAzureServiceProvider(...)`, and the Azure client registration helpers live in the `FastMoq.Azure.*` namespaces.

`CreateFunctionContextInstanceServices(...)`, `AddFunctionContextInstanceServices(...)`, `CreateHttpRequestData(...)`, `CreateHttpResponseData(...)`, `ReadBodyAsStringAsync(...)`, and `ReadBodyAsJsonAsync<T>(...)` live in `FastMoq.AzureFunctions.Extensions`, while the generic `CreateTypedServiceProvider(...)` and `AddServiceProvider(...)` helpers stay in `FastMoq.Extensions`.

`GetMockDbContext<TContext>()` remains the default mocked-sets entry point. For explicit mode selection between mocked DbSets and a real EF in-memory context, use `GetDbContextHandle<TContext>(new DbContextHandleOptions<TContext> { ... })`.

The mocked-sets path is still backed by the existing Moq-based `DbContextMock<TContext>` implementation, while the real-context path is exposed through `DbContextTestMode.RealInMemory`.

If you are upgrading an older suite that still uses `GetMock<T>()`, direct `Mock<T>` access, `VerifyLogger(...)`, or older HTTP setup helpers such as `SetupHttpMessage(...)`, select Moq explicitly for that test assembly. If you are writing new or actively refactoring tests, prefer provider-neutral APIs such as `GetOrCreateMock(...)`, `Verify(...)`, `VerifyLogged(...)`, `WhenHttpRequest(...)`, and `WhenHttpRequestJson(...)`. When you still need provider-specific setup in v4, use the provider-package extensions on `IFastMock<T>` such as `AsMoq()`, `Setup(...)`, or `AsNSubstitute()`.

For the moved HTTP compatibility helpers, the migration point is package selection rather than namespace churn: the Moq compatibility methods remain in the `FastMoq.Extensions` namespace for low-churn source migration, but their implementation now comes from the `FastMoq.Provider.Moq` package instead of `FastMoq.Core`.

Provider selection example:

```csharp
MockingProviderRegistry.Register("moq", MoqMockingProvider.Instance, setAsDefault: true);
var mocker = new Mocker();
```

For a temporary override in a specific async scope, use `MockingProviderRegistry.Push("providerName")`. For detailed setup guidance, see [Provider Selection Guide](./docs/getting-started/provider-selection.md).

## Targets

- .NET 8
- .NET 9
- .NET 10

## Quick Example

```csharp
public class OrderProcessingServiceTests : MockerTestBase<OrderProcessingService>
{
    [Fact]
    public async Task PlaceOrderAsync_ShouldPersistAndLog_WhenReservationAndPaymentSucceed()
    {
        Mocks.GetOrCreateMock<IInventoryGateway>()
            .Setup(x => x.ReserveAsync("SKU-RED-CHAIR", 2, CancellationToken.None))
            .ReturnsAsync(true);

        Mocks.GetOrCreateMock<IPaymentGateway>()
            .Setup(x => x.ChargeAsync("cust-42", 149.90m, CancellationToken.None))
            .ReturnsAsync("pay_12345");

        var result = await Component.PlaceOrderAsync(new OrderRequest
        {
            CustomerId = "cust-42",
            Sku = "SKU-RED-CHAIR",
            Quantity = 2,
            TotalAmount = 149.90m,
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        Mocks.Verify<IOrderRepository>(x => x.SaveAsync(It.IsAny<OrderRecord>(), CancellationToken.None), TimesSpec.Once);
        Mocks.VerifyLogged(LogLevel.Information, "Placed order");
    }
}
```

## Common Entry Points

- `MockerTestBase<TComponent>`: shortest path for service and component tests with automatic construction and dependency injection
- `Mocker`: standalone entry point when you do not want a test base class
- `GetMock<T>()`: lowest-churn compatibility path for older Moq-shaped tests in v4
- `GetOrCreateMock<T>()`: tracked provider-first mock access for the forward v4 and v5 path
- `Verify(...)`, `VerifyNoOtherCalls(...)`, `VerifyLogged(...)`, and `TimesSpec`: provider-neutral verification surface

## Learn More

- [Getting Started Guide](./docs/getting-started) for step-by-step first tests
- [Testing Guide](./docs/getting-started/testing-guide.md) for common patterns such as `IFileSystem`, `DbContext`, `CallMethod(...)`, and constructor-guard testing
- [Executable Testing Examples](./docs/samples/testing-examples.md) for realistic sample flows backed by repository tests
- [Provider Selection Guide](./docs/getting-started/provider-selection.md) for provider bootstrap and selection
- [Provider Capabilities](./docs/getting-started/provider-capabilities.md) for supported-vs-unsupported behavior by provider
- [Migration Guide](./docs/migration) for the v3 to v4 to v5 path
- [Breaking Changes](./docs/breaking-changes/README.md) for behavior changes relative to `3.0.0`
- [API Documentation](https://help.fastmoq.com/) for generated reference docs

## License

[License - MIT](./License)

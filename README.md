# [FastMoq](http://help.fastmoq.com/)

FastMoq is a provider-first .NET testing framework for auto-mocking, dependency-aware construction, and test-focused object creation. In v4, it ships with a bundled `reflection` default and optional Moq or NSubstitute integrations when you need provider-specific arrange syntax.

FastMoq is test-framework agnostic and can be used from xUnit, NUnit, MSTest, or other .NET test frameworks.

## Evaluate FastMoq

If you are comparing FastMoq with direct mock-provider usage or deciding whether it fits your test stack, use this order:

1. Read `Why FastMoq` below for the high-level value proposition.
2. Use [Feature Comparison](https://help.fastmoq.com/docs/feature-parity/README.html) for the detailed FastMoq vs direct-provider story.
3. Use [Provider Capabilities](https://help.fastmoq.com/docs/getting-started/provider-capabilities.html) when you need the exact provider boundary.
4. Use [Benchmarks](https://help.fastmoq.com/docs/benchmarks/README.html) when you want the current measured runtime-overhead comparison.
5. Use [Migration Guide](https://help.fastmoq.com/docs/migration/README.html) and [What's New Since 3.0.0](https://help.fastmoq.com/docs/whats-new/README.html) if you are coming from the `3.0.0` public line.
6. Use [Roadmap Notes](https://help.fastmoq.com/docs/roadmap/README.html) only for future public product direction, not current support claims.

## Choose Your Starting Point

- If you already know you want package-specific guidance, start with [FastMoq Documentation Home](https://help.fastmoq.com/).
- If you want a first test that works under the default `reflection` provider, start with [Getting Started Guide](https://help.fastmoq.com/docs/getting-started/README.html).
- If you want tracked provider-specific `.Setup(...)` syntax, read [Provider Selection Guide](https://help.fastmoq.com/docs/getting-started/provider-selection.html) before copying any Moq-fluent examples.
- Front-door examples use a couple of different assertion styles. Match the assertion library your test project already uses and keep that style consistent within the project.
- Authoring ladder for the current v4 line:
    1. provider-neutral helper first, such as `GetOrCreateMock(...)`, `Verify(...)`, `VerifyNoOtherCalls(...)`, `VerifyLogged(...)`, `WhenHttpRequest(...)`, or `AddType(...)`
    2. tracked `IFastMock<T>` provider extensions such as `Setup(...)`, `SetupGet(...)`, or `AsNSubstitute()` when the selected provider package exposes them
    3. explicit `AsMoq()` or provider-native escape hatches only for the remaining provider-specific gaps

## Why FastMoq

Direct mock-provider tests often spend more effort on declaring mocks, wiring constructors, and repeating framework plumbing than on the behavior under test. FastMoq moves that harness work into a provider-first layer so tests stay focused on the behavior, not the object graph.

### Less harness code, less constructor churn

- **Tracked dependencies instead of mock fields** — `MockerTestBase<TComponent>` maintains a mock registry, so you retrieve what you need with `GetOrCreateMock<T>()` instead of declaring a field for every constructor dependency
- **Automatic component construction** — the subject under test is created and injected for you; no repeated `new MyService(dep1.Object, dep2.Object, ...)` wiring in each test
- **Refactoring-friendly tests** — when a component gains a new constructor dependency, existing tests usually keep working because the extra mock is auto-registered
- **One-call constructor guard coverage** — `TestAllConstructorParameters(...)` replaces a test-per-parameter pattern with one helper-driven null-guard assertion

### Provider-first without provider lock-in

FastMoq ships with a bundled `reflection` provider, so you can start without taking a dependency on a mock library. When you want provider-native arrange syntax, add `FastMoq.Provider.Moq` or `FastMoq.Provider.NSubstitute` and keep the rest of the harness the same. The verification surface stays provider-neutral through `Verify(...)`, `VerifyNoOtherCalls(...)`, `VerifyLogged(...)`, and `TimesSpec`, which keeps tests portable if you switch providers later.

### Built for the parts of tests that usually get noisy

- **Framework-heavy collaborators are pre-wired** — common test types such as `ILogger<T>`, `ILoggerFactory`, `IFileSystem`, and `HttpClient` do not need bespoke setup before you can use them
- **Database and HTTP helpers stay in the same model** — `GetMockDbContext<TContext>()`, `WhenHttpRequest(...)`, and `WhenHttpRequestJson(...)` cover common high-friction setup without dropping out of the FastMoq flow
- **Web, Blazor, Azure, and scenario helpers are available when the test surface expands** — the package set includes support for bUnit-style Blazor tests, MVC and `HttpContext` helpers, Azure SDK and Functions helpers, and fluent scenario coverage through `ScenarioBuilder<T>`

### Works well when a suite needs to stay maintainable

FastMoq pays off most when tests have multiple constructor dependencies, recurring framework abstractions, or a need to keep provider choices flexible over time. It targets .NET 8, 9, and 10, works with the .NET test framework your suite already uses, and includes the analyzer pack by default in `FastMoq` and `FastMoq.Core` so provider-first guidance shows up directly in the IDE.

### FastMoq vs Direct Mock Provider Usage

The example below uses xUnit-style attributes and Moq arrange syntax for illustration. The point of comparison is the FastMoq harness shape, not a requirement to use xUnit or Moq for every test suite.

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

Using FastMoq tracked provider-first mocks with Moq-specific arrange syntax:

`GetOrCreateMock<T>()` is still the provider-first tracked path here. This example uses the Moq provider package only for the arrange-time `Setup(...)` calls, so it requires `FastMoq.Provider.Moq`, `Moq`, and explicit `moq` provider selection for the test assembly.

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

FastMoq removes the need for most explicit mock declarations, subject construction, and logger-plumbing code while still allowing provider-specific setup when you need it.

If you want a copy-paste example that works under the default provider without Moq-specific setup, start with [Getting Started Guide](https://help.fastmoq.com/docs/getting-started/README.html).

## 📚 Documentation

### Quick Links

- **🚀 [Getting Started Guide](https://help.fastmoq.com/docs/getting-started/README.html)** - Your first FastMoq test in 5 minutes
- **🧪 [Testing Guide](https://help.fastmoq.com/docs/getting-started/testing-guide.html)** - Repo-native guidance for `GetOrCreateMock<T>()`, `AddType(...)`, `DbContext`, `IFileSystem`, and known types
- **🔄 [Migration Guide](https://help.fastmoq.com/docs/migration/README.html)** - Practical old-to-new guidance for the `3.0.0` to `4.3.0` transition
- **👨‍🍳 [Cookbook](https://help.fastmoq.com/docs/cookbook/README.html)** - Real-world patterns and recipes with compatibility-only pockets labeled explicitly
- **🔎 [API Reference](https://help.fastmoq.com/)** - Published HTML API reference for namespaces, types, and members
- **🌐 [Web Helper Guidance](https://help.fastmoq.com/docs/getting-started/testing-guide.html#controller-testing)** - Controller, `HttpContext`, `IHttpContextAccessor`, and claims-principal test setup
- **🔌 [Provider Selection Guide](https://help.fastmoq.com/docs/getting-started/provider-selection.html)** - How to register, select, and bootstrap providers for a test assembly
- **📋 [Provider Capabilities](https://help.fastmoq.com/docs/getting-started/provider-capabilities.html)** - What `moq`, `nsubstitute`, and `reflection` support today, with recommended usage patterns
- **🧪 [Executable Testing Examples](https://help.fastmoq.com/docs/samples/testing-examples.html)** - Repo-backed service examples using the current FastMoq API direction
- **🏗️ [Sample Applications](https://help.fastmoq.com/docs/samples/README.html)** - Complete examples with Azure integration
- **📊 [Feature Comparison](https://help.fastmoq.com/docs/feature-parity/README.html)** - FastMoq vs Moq/NSubstitute
- **📈 [Benchmarks](https://help.fastmoq.com/docs/benchmarks/README.html)** - Runnable BenchmarkDotNet suite and latest checked-in results

### Additional Resources

- **📖 [Complete Documentation](https://help.fastmoq.com/)** - All guides and references in one place
- **🗺️ [Roadmap Notes](https://help.fastmoq.com/docs/roadmap/README.html)** - Current provider-first direction and deferred backlog items
- **🆕 [What's New Since 3.0.0](https://help.fastmoq.com/docs/whats-new/README.html)** - Summary of the major post-`3.0.0` architecture, packaging, and API changes
- **⚠️ [Breaking Changes](https://help.fastmoq.com/docs/breaking-changes/README.html)** - Intentional v4 behavior changes relative to the `3.0.0` public release
- **🔄 [Migration Guide](https://help.fastmoq.com/docs/migration/README.html)** - Practical old-to-new guidance from `3.0.0` to the current repo direction
- **❓ [FAQs](./FAQs.md)** - Frequently asked questions and troubleshooting
- **🔗 [API Documentation](https://help.fastmoq.com/)** - Generated HTML API reference

## Features

- Blazor support in FastMoq and FastMoq.Web.
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
- for migration-specific guidance, start with [Migration Guide](https://help.fastmoq.com/docs/migration/README.html#web-test-helpers) and then use [Testing Guide](https://help.fastmoq.com/docs/getting-started/testing-guide.html#controller-testing) for the day-to-day helper rules
- for the full package-choice overview, use [Getting Started](https://help.fastmoq.com/docs/getting-started/README.html#package-choices)

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

The aggregate `FastMoq` package intentionally includes `FastMoq.Database`, which in turn brings EF Core test-helper dependencies such as `Microsoft.EntityFrameworkCore.InMemory`. If your suite already pins relational or provider-specific EF Core packages on a different major version, either align the EF Core major versions across the graph or consume `FastMoq.Core` plus only the helper packages you actually need.

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

For a temporary override in a specific async scope, use `MockingProviderRegistry.Push("providerName")`. For detailed setup guidance, see [Provider Selection Guide](https://help.fastmoq.com/docs/getting-started/provider-selection.html).

## Targets

- .NET 8
- .NET 9
- .NET 10

## Quick Example

If you want a copy-paste example that works under the default provider, use [Getting Started Guide](https://help.fastmoq.com/docs/getting-started/README.html) first.

The snippet below is the optional Moq-fluent path, so it assumes `FastMoq.Provider.Moq` is installed and `moq` is selected for the test assembly.

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
- `GetOrCreateMock<T>()`: tracked provider-first mock access for the forward v4 and v5 path
- `CreateStandaloneFastMock<T>()`: detached provider-first mock handle for manual wiring or an additional same-type double outside the tracked graph
- `CreateFastMock<T>()`: tracked provider-first registration helper when you intentionally want the new mock added immediately; it is not a second independent unkeyed handle
- `Verify(...)`, `VerifyNoOtherCalls(...)`, `VerifyLogged(...)`, and `TimesSpec`: provider-neutral verification surface

## Learn More

- [Getting Started Guide](https://help.fastmoq.com/docs/getting-started/README.html) for step-by-step first tests
- [Testing Guide](https://help.fastmoq.com/docs/getting-started/testing-guide.html) for common patterns such as `IFileSystem`, `DbContext`, `CallMethod(...)`, and constructor-guard testing
- [Executable Testing Examples](https://help.fastmoq.com/docs/samples/testing-examples.html) for realistic sample flows backed by repository tests
- [Provider Selection Guide](https://help.fastmoq.com/docs/getting-started/provider-selection.html) for provider bootstrap and selection
- [Provider Capabilities](https://help.fastmoq.com/docs/getting-started/provider-capabilities.html) for supported-vs-unsupported behavior by provider
- [Migration Guide](https://help.fastmoq.com/docs/migration/README.html) for the v3 to v4 to v5 path
- [Breaking Changes](https://help.fastmoq.com/docs/breaking-changes/README.html) for behavior changes relative to `3.0.0`
- [API Documentation](https://help.fastmoq.com/) for generated reference docs

## License

[License - MIT](./License)

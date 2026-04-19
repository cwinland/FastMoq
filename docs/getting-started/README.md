# Getting Started with FastMoq

Welcome to FastMoq. This guide walks through setup and a first test using the current v4 release line. FastMoq helps you create tests with automatic dependency injection, mock tracking, and test-focused object creation without forcing you to wire every dependency by hand.

## What is FastMoq?

FastMoq is designed to reduce the boilerplate usually required when setting up unit tests. It automatically creates and injects test doubles, lets you override only the parts that matter, and supports both provider-neutral test patterns and provider-specific compatibility for migration scenarios.

### Why teams use FastMoq instead of using a mock provider directly

The main value is still less test harness code.

With a mock provider used directly, the test usually needs to do all of this itself:

- declare each dependency mock
- construct the subject under test manually
- keep constructor wiring in sync as dependencies change
- add extra harness code for framework-heavy types and logging

With FastMoq, the test usually only configures the dependencies that matter for the behavior under test.

Side-by-side example:

This comparison uses the optional Moq-fluent path on the FastMoq side to keep the contrast obvious. The first copy-paste example that works under the default provider appears later under `Your First Test`.

```csharp
// Direct mock-provider usage
var parser = new Mock<ICustomerCsvParser>();
var repository = new Mock<ICustomerRepository>();
var logger = new Mock<ILogger<CustomerImportService>>();
var component = new CustomerImportService(parser.Object, repository.Object, logger.Object);

parser.Setup(x => x.Parse(csv)).Returns(rows);

var importedCount = await component.ImportAsync(filePath, CancellationToken.None);

repository.Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyList<CustomerImportRow>>(), CancellationToken.None), Times.Once);
```

```csharp
// FastMoq with optional Moq-fluent setup
Mocks.GetOrCreateMock<ICustomerCsvParser>()
    .Setup(x => x.Parse(csv))
    .Returns(rows);

var importedCount = await Component.ImportAsync(filePath, CancellationToken.None);

Mocks.Verify<ICustomerRepository>(
    x => x.UpsertAsync(It.IsAny<IReadOnlyList<CustomerImportRow>>(), CancellationToken.None),
    TimesSpec.Once);
```

FastMoq is most valuable when the subject has multiple dependencies, when constructor signatures change frequently, or when your tests repeatedly need built-in framework helpers.

Read the repo README first if you are still deciding whether FastMoq is the right fit. Use this guide once you are ready to install packages and write tests.

For the repo-native testing conventions and framework-specific guidance used by this codebase, see the [FastMoq Testing Guide](./testing-guide.md).

If you want examples that run directly in this repository instead of static snippets only, see [Executable Testing Examples](../samples/testing-examples.md).

If you are updating older FastMoq usage from the last public `3.0.0` release, see [Migration Guide: 3.0.0 To The Current v4 Line](../migration/README.md).

If you need to choose or bootstrap a provider explicitly, see the [Provider Selection Guide](./provider-selection.md).

## Read This Guide In Order

1. Choose the package line you want under `Installation`.
2. Stay on the default provider-neutral path unless you specifically need provider-native arrange syntax such as `.Setup(...)`.
3. Work through `Your First Test` first, because that example runs under the default `reflection` provider without assembly-level Moq registration.
4. Only opt into the Moq-fluent path after reading the provider-selection note below.

### Key Benefits

- **Automatic Mock Creation**: No need to manually declare and setup mocks
- **Dependency Injection**: Automatically resolves and injects dependencies into your components
- **Fluent API**: Clean, readable syntax for test setup and verification
- **Provider Architecture**: Extensible system for custom mock configurations
- **Built-in Helpers**: Common patterns for EF Core, HttpClient, IFileSystem, and more
- **Less Constructor Wiring**: The test stays focused on the behavior under test instead of the dependency graph

## Installation

Install FastMoq using your preferred package manager:

### Optional assertion packages

FastMoq does not require a specific assertion library.

If you want the fluent `.Should()` syntax used in several examples below, install AwesomeAssertions:

```bash
dotnet add package AwesomeAssertions --version 9.4.0
```

If your project uses Shouldly instead, install Shouldly and translate the assertion syntax accordingly:

```bash
dotnet add package Shouldly --version 4.3.0
```

### Package choices

Use this package and namespace quick reference when you are deciding which package line your test project should reference.

| If you want... | Install... | Main namespace(s) to import | Why |
| --- | --- | --- | --- |
| simplest all-in-one experience | `FastMoq` | `FastMoq`, `FastMoq.Extensions`, plus any included helper namespaces such as `FastMoq.Web.Extensions`, `FastMoq.AzureFunctions.Extensions`, and `FastMoq.Azure.*` | Aggregate package that includes the primary runtime, shared Azure SDK helpers, database helpers, web support, Azure Functions helpers, and the FastMoq analyzer pack by default |
| lighter core-only usage | `FastMoq.Core` | `FastMoq`, `FastMoq.Extensions` | Provider-first runtime without the extra EF or web-specific package payloads |
| custom provider or advanced extension authoring | `FastMoq.Abstractions` | `FastMoq.Providers` | Shared provider contracts such as `IMockingProvider`, `IFastMock`, `TimesSpec`, and provider-registration attributes. Most test projects do not need to reference this package directly |
| Azure SDK credentials, pageable builders, or Azure-oriented DI/config helpers while using `FastMoq.Core` | `FastMoq.Azure` | `FastMoq.Azure.Credentials`, `FastMoq.Azure.DependencyInjection`, `FastMoq.Azure.Storage`, `FastMoq.Azure.KeyVault`, `FastMoq.Azure.Pageable` | Adds `PageableBuilder`, token/default-credential helpers, Azure-oriented configuration/service-provider helpers, and common Azure client registration helpers |
| Azure Functions worker helpers while using `FastMoq.Core` | `FastMoq.AzureFunctions` | `FastMoq.AzureFunctions.Extensions`, `FastMoq.AzureFunctions.Http` | Adds `CreateFunctionContextInstanceServices(...)`, `AddFunctionContextInstanceServices(...)`, `CreateHttpRequestData(...)`, `CreateHttpResponseData(...)`, and body readers in `FastMoq.AzureFunctions.Extensions` while keeping the typed `IServiceProvider` helpers in core |
| DbContext and EF-specific helpers while using `FastMoq.Core` | `FastMoq.Database` | `FastMoq` | Adds `GetMockDbContext<TContext>()` and the explicit DbContext handle modes |
| controller, `HttpContext`, `IHttpContextAccessor`, or claims-principal helpers while using `FastMoq.Core` | `FastMoq.Web` | `FastMoq.Web.Extensions`, `FastMoq.Web` | Adds `CreateHttpContext(...)`, `CreateControllerContext(...)`, `SetupClaimsPrincipal(...)`, `AddHttpContext(...)`, and `AddHttpContextAccessor(...)` |
| Moq-specific tracked-mock extension methods during migration | `FastMoq.Provider.Moq` | `FastMoq.Providers.MoqProvider`, `FastMoq.Extensions` | Adds provider-package extension methods such as `AsMoq()`, `Setup(...)`, `SetupGet(...)`, `SetupSequence(...)`, and `Protected()` on `IFastMock<T>` |
| optional NSubstitute-backed provider support | `FastMoq.Provider.NSubstitute` | `FastMoq.Providers.NSubstituteProvider` | Adds the NSubstitute provider package and tracked-mock extensions such as `AsNSubstitute()` |
| analyzer guidance without the core or aggregate runtime package | `FastMoq.Analyzers` | none at runtime | Standalone Roslyn analyzers and code fixes for migration cleanup and provider-first test-authoring guidance |

Use the table above as the quick namespace reference. The more detailed helper-namespace inventory appears below in `Common Using Statements`.

Important package boundaries in the current v4 line:

`FastMoq` already includes the common end-user surface, including shared Azure SDK helpers, web, database, and Azure Functions helpers

- `FastMoq` and `FastMoq.Core` both include the FastMoq analyzer assets by default so most test projects get migration guidance without extra setup
- `FastMoq.Core` keeps the provider-neutral runtime separate from the shared Azure SDK, EF, Azure Functions, and web helper packages when you consume core directly
- `FastMoq.Analyzers` remains useful when you want the diagnostics without taking either the aggregate or core runtime package
- if a core-only test project stays on the legacy Moq-shaped path with `GetMock<T>()`, `VerifyLogger(...)`, `MockModel.Mock`, `SetupSet(...)`, `SetupAllProperties()`, or other Moq-specific compatibility flows, add `FastMoq.Provider.Moq` explicitly, then select `moq` at assembly scope instead of relying on the default `reflection` provider
- `FastMoq.Core` includes the built-in `reflection` provider and the bundled Moq compatibility runtime, but the Moq tracked-mock extension methods such as `Setup(...)` and `Protected()` still belong to the `FastMoq.Provider.Moq` package
- provider-package extension methods still follow the provider-package docs and selection rules described in [Provider Selection and Setup](./provider-selection.md)
- if you are wiring Azure SDK clients, pageable sequences, or token credentials through tests while consuming `FastMoq.Core` directly, add `FastMoq.Azure`
- if you are wiring Azure Functions worker tests through `FunctionContext.InstanceServices` or concrete HTTP-trigger request and response objects, add `FastMoq.AzureFunctions` when you consume `FastMoq.Core` directly
- if you are unsure whether your web tests need another package, see the web-helper notes in [Testing Guide](./testing-guide.md#controller-testing) and the migration-specific notes in [Framework and web helper migration](../migration/framework-and-web-helpers.md#web-test-helpers)
- for the full analyzer catalog and package-aware migration guidance, see [Migration Guide](../migration/README.md#analyzer-catalog)

#### EF Core package topology and version alignment

The aggregate `FastMoq` package intentionally includes `FastMoq.Database`, and that helper package brings EF Core test-helper dependencies such as `Microsoft.EntityFrameworkCore.InMemory`.

That is convenient when you want the umbrella package, but it matters if the same test project already pins relational or provider-specific EF Core packages on another major version.

Use this rule:

- keep `FastMoq` when you want the umbrella package and the EF Core major versions across your graph already align
- prefer `FastMoq.Core` plus only the helper packages you actually need when you do not want the EF-specific helper dependency surface
- if you intentionally combine the aggregate `FastMoq` package with separate EF Core provider packages, align the EF Core major versions across the graph

Mixed-major EF Core graphs often surface as runtime failures that look unrelated to FastMoq at first, such as missing-method, assembly-load, or provider-registration errors.

If your team wants the aggregate runtime package without analyzer diagnostics in a specific test project, you can opt out with:

```xml
<PackageReference Include="FastMoq" Version="4.*" ExcludeAssets="analyzers" />
```

### .NET CLI

```bash
dotnet add package FastMoq
```

If you prefer the split packages instead of the aggregate package:

```bash
dotnet add package FastMoq.Core
dotnet add package FastMoq.Azure
dotnet add package FastMoq.AzureFunctions
dotnet add package FastMoq.Database
dotnet add package FastMoq.Web
dotnet add package FastMoq.Provider.Moq
```

### Package Manager Console

```powershell
Install-Package FastMoq
```

### PackageReference

```xml
<PackageReference Include="FastMoq" Version="4.*" />
```

Split-package example:

```xml
<PackageReference Include="FastMoq.Core" Version="4.*" />
<PackageReference Include="FastMoq.Azure" Version="4.*" />
<PackageReference Include="FastMoq.AzureFunctions" Version="4.*" />
<PackageReference Include="FastMoq.Database" Version="4.*" />
<PackageReference Include="FastMoq.Web" Version="4.*" />
<PackageReference Include="FastMoq.Provider.Moq" Version="4.*" />
```

> Note: this guide targets the current v4 release line. For the release delta relative to the last public `3.0.0` package, see [What's New Since 3.0.0](../whats-new/README.md).
> Note: in the current v4 package line, `GetMockDbContext<TContext>()` keeps the same `FastMoq` namespace call shape, but direct `FastMoq.Core` consumers should add `FastMoq.Database` for EF-specific helpers. Direct shared Azure SDK helper consumers should add `FastMoq.Azure`. Direct Azure Functions helper consumers should add `FastMoq.AzureFunctions`. Direct web-helper consumers should add `FastMoq.Web`.

In the current v4 release line, `FastMoq.Core` bundles the built-in `moq` provider and the internal `reflection` fallback. The default provider is `reflection`. Optional providers such as `nsubstitute` can be added explicitly and then selected by their canonical name once the package is present, or registered manually under a custom alias. You can also register your own provider by implementing `IMockingProvider`; the bundled providers are examples, not the only supported choices.

The DbContext helper path now exposes explicit modes through `GetDbContextHandle<TContext>(...)`. `GetMockDbContext<TContext>()` remains the mocked-sets convenience entry point, `DbContextTestMode.RealInMemory` is the real EF-backed option, and provider-first APIs such as `GetOrCreateMock<TContext>()` and `GetMockModel<TContext>()` return the same tracked context after the helper creates it.

### Common Using Statements

FastMoq itself does not require an assertion library.

For a typical external test project, this is the main FastMoq namespace to import:

```csharp
using FastMoq;
```

Add the other namespaces only when your test code actually uses the corresponding APIs:

```csharp
using FastMoq.Extensions; // Core helper extensions such as VerifyLogged(...), AddServiceProvider(...), and CreateHttpClient(...)
using Microsoft.Extensions.Logging; // ILogger<T> or LogLevel in your test or component
using AwesomeAssertions; // If you use AwesomeAssertions in your assertions
using Shouldly; // If your project uses Shouldly instead
using Xunit; // Or the test framework of your choice
```

`Mocker` and `MockerTestBase<T>` live in `FastMoq`.

`FastMoq.Extensions` is the shared core helper namespace. It is optional and includes helpers such as `VerifyLogged(...)`, `AddServiceProvider(...)`, `AddPropertyState(...)`, `AddPropertySetterCapture(...)`, and `CreateHttpClient(...)`.

If you intentionally want Moq-specific tracked `.Setup(...)`, `SetupSequence(...)`, or `Protected()` syntax, add the provider package and register the provider explicitly before copying those examples:

```bash
dotnet add package FastMoq.Provider.Moq
dotnet add package Moq
```

```csharp
using FastMoq.Providers;

[assembly: FastMoqDefaultProvider("moq")]
```

You can use `[assembly: FastMoqRegisterProvider("moq", typeof(MoqMockingProvider), SetAsDefault = true)]` instead when you want registration and selection in one attribute.

FastMoq does not place every extension API in `FastMoq.Extensions`. The consumer-facing helper namespaces are:

| Namespace | Package | Purpose |
| --- | --- | --- |
| `FastMoq.Extensions` | `FastMoq` or `FastMoq.Core` | Core helper extensions for logger verification, typed `IServiceProvider` setup, property-state helpers, constructor and object helpers, HTTP helpers, and related test utilities. Installing `FastMoq.Provider.Moq` also adds the Moq HTTP compatibility helpers to this same namespace. |
| `FastMoq.Web.Extensions` | `FastMoq.Web` or `FastMoq` | ASP.NET Core web-test helpers such as `HttpContext`, controller-context, and claims-principal setup. |
| `FastMoq.AzureFunctions.Extensions` | `FastMoq.AzureFunctions` or `FastMoq` | Azure Functions worker helpers such as `FunctionContext.InstanceServices`, `HttpRequestData`, `HttpResponseData`, and request/response body helpers. |
| `FastMoq.Azure.Credentials` | `FastMoq.Azure` or `FastMoq` | Azure credential registration helpers for `TokenCredential` and `DefaultAzureCredential`-shaped tests. |
| `FastMoq.Azure.DependencyInjection` | `FastMoq.Azure` or `FastMoq` | Azure-oriented configuration, DI, and typed service-provider helpers. |
| `FastMoq.Azure.Storage` | `FastMoq.Azure` or `FastMoq` | Azure Storage client registration helpers for blob, queue, and table clients. |
| `FastMoq.Azure.KeyVault` | `FastMoq.Azure` or `FastMoq` | Azure Key Vault `SecretClient` registration helpers. |
| `FastMoq.Azure.Pageable` | `FastMoq.Azure` or `FastMoq` | Azure SDK pageable helpers such as `PageableBuilder`. This is helper surface rather than extension-method surface, but consumers still import the namespace directly. |
| `FastMoq.Providers.MoqProvider` | `FastMoq.Provider.Moq` | Moq-specific tracked-mock escape hatches such as `AsMoq()`, `Setup(...)`, `SetupGet(...)`, `SetupSequence(...)`, and `Protected()`. |
| `FastMoq.Providers.NSubstituteProvider` | `FastMoq.Provider.NSubstitute` | NSubstitute-specific tracked-mock escape hatches such as `AsNSubstitute()`, `Received(...)`, and `DidNotReceive()`. |

Azure Functions also exposes non-extension builder helpers in `FastMoq.AzureFunctions.Http` when you want the concrete request and response builders directly.

## Your First Test

Let's create a simple service and write a test for it using FastMoq.

### 1. Create a Service to Test

First, let's create a simple file processing service that depends on the `IFileSystem` interface:

```csharp
// Services/FileProcessorService.cs
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;

public interface IFileProcessorService
{
    Task<string> ProcessFileAsync(string filePath);
    bool ValidateFilePath(string filePath);
}

public class FileProcessorService : IFileProcessorService
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<FileProcessorService> _logger;

    public FileProcessorService(IFileSystem fileSystem, ILogger<FileProcessorService> logger)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ProcessFileAsync(string filePath)
    {
        if (!ValidateFilePath(filePath))
        {
            _logger.LogWarning("Invalid file path: {FilePath}", filePath);
            return string.Empty;
        }

        _logger.LogInformation("Processing file: {FilePath}", filePath);
        
        var content = await _fileSystem.File.ReadAllTextAsync(filePath);
        var processedContent = content.ToUpperInvariant();
        
        _logger.LogInformation("File processed successfully: {FilePath}", filePath);
        return processedContent;
    }

    public bool ValidateFilePath(string filePath)
    {
        return !string.IsNullOrWhiteSpace(filePath) && _fileSystem.File.Exists(filePath);
    }
}
```

### 2. Write Your First FastMoq Test

Start with a provider-neutral example that works under the default `reflection` provider. This path does not require `FastMoq.Provider.Moq`, `Moq`, or assembly-level Moq registration.

This sample uses AwesomeAssertions for the assertion syntax and `VerifyLogged(...)` for log verification, so it also includes those optional namespaces.

```csharp
// Tests/FileProcessorServiceTests.cs
using FastMoq;
using FastMoq.Extensions;
using Microsoft.Extensions.Logging;
using AwesomeAssertions;
using System.IO.Abstractions;
using Xunit;

public class FileProcessorServiceTests : MockerTestBase<FileProcessorService>
{
    [Fact]
    public async Task ProcessFileAsync_ShouldReturnProcessedContent_WhenValidFile()
    {
        // Arrange
        var filePath = @"c:\temp\test.txt";
        var fileSystem = Mocks.GetObject<IFileSystem>();
        fileSystem.File.WriteAllText(filePath, "hello world");

        // Act
        var result = await Component.ProcessFileAsync(filePath);

        // Assert
        result.Should().Be("HELLO WORLD");
        Mocks.VerifyLogged(LogLevel.Information, "File processed successfully", 1);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldReturnEmpty_WhenInvalidFile()
    {
        // Arrange
        var filePath = @"c:\temp\missing.txt";

        // Act
        var result = await Component.ProcessFileAsync(filePath);

        // Assert
        result.Should().BeEmpty();
        Mocks.VerifyLogged(LogLevel.Warning, "Invalid file path", 1);
    }
}
```

### 3. Optional: Moq-fluent setup path

If you prefer tracked `.Setup(...)` syntax, opt into it explicitly first. This is the path that requires `FastMoq.Provider.Moq`, `Moq`, the `FastMoq.Providers.MoqProvider` namespace, and assembly-level provider selection.

```csharp
using FastMoq;
using FastMoq.Extensions;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.Logging;
using Moq;
using AwesomeAssertions;
using System.IO.Abstractions;
using Xunit;

[assembly: FastMoqDefaultProvider("moq")]

public class FileProcessorServiceMoqTests : MockerTestBase<FileProcessorService>
{
    [Fact]
    public async Task ProcessFileAsync_ShouldReturnProcessedContent_WhenValidFile()
    {
        var filePath = "test.txt";

        Mocks.GetOrCreateMock<IFileSystem>()
            .Setup(x => x.File.Exists(filePath))
            .Returns(true);

        Mocks.GetOrCreateMock<IFileSystem>()
            .Setup(x => x.File.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync("hello world");

        var result = await Component.ProcessFileAsync(filePath);

        result.Should().Be("HELLO WORLD");
    }
}
```

## Understanding the FastMoq Architecture

### `MockerTestBase<T>`

`MockerTestBase<T>` is the foundation of FastMoq testing. It automatically:

1. **Creates your component**: The `Component` property contains an instance of your class under test
2. **Manages dependencies**: Constructor parameters are resolved automatically using FastMoq's current type-map, known-type, and auto-mock rules
3. **Provides mock access**: Use `Mocks.GetOrCreateMock<T>()` to configure tracked mock behavior
4. **Handles cleanup**: Mocks are properly disposed after each test

### Key Properties and Methods

| Property/Method | Description |
| --------------- | ----------- |
| `Component` | The instance of your class under test |
| `MockOptional` | Obsolete compatibility alias for `OptionalParameterResolution`. Prefer explicit `OptionalParameterResolution` or `InvocationOptions` in new code. |
| `Mocks` | The `Mocker` instance that manages all mocks |
| `Mocks.GetOrCreateMock<T>()` | Gets the tracked mock handle for interface T |
| `Mocks.GetObject<T>()` | Gets the mocked object instance |

### Automatic Dependency Resolution

FastMoq uses a smart dependency resolver that:

- Creates mocks for interfaces automatically
- Uses sensible defaults for common types (ILogger, IOptions, etc.)
- Supports complex dependency chains
- Handles circular dependencies gracefully

## Advanced Setup Options

### Custom Mock Configuration

The next two snippets use the optional Moq-fluent arrange path. If you want `.Setup(...)`, make sure you already added `FastMoq.Provider.Moq` and selected `moq` for the test assembly as shown above.

If you need to configure mocks before your component is created, use the `SetupMocksAction`:

```csharp
public class OrderServiceTests : MockerTestBase<OrderService>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        // Configure mock behavior before component creation
        mocker.GetOrCreateMock<IPaymentProcessor>()
            .Setup(x => x.ValidateCard(It.IsAny<string>()))
            .Returns(true);
    };

    [Fact]
    public void TestWithPreConfiguredMocks()
    {
        // Your test here - mocks are already configured
    }
}
```

### Constructor Injection Control

You can also configure mock setup through the base constructor:

```csharp
public class OrderServiceTests : MockerTestBase<OrderService>
{
    public OrderServiceTests() : base(ConfigureMocks)
    {
    }

    private static void ConfigureMocks(Mocker mocker)
    {
        mocker.GetOrCreateMock<IOrderRepository>()
            .Setup(x => x.GetOrderAsync(It.IsAny<int>()))
            .ReturnsAsync(new Order());
    }
}
```

When a test needs a specific constructor, prefer the explicit constructor-selection hooks instead of relying on `GetObject<T>()` to land on the right overload.

- For `MockerTestBase<TComponent>`, override `ComponentConstructorParameterTypes`.
- For direct `Mocker` usage, call `CreateInstanceByType<T>(...)`.
- If the chosen constructor depends on `IServiceProvider` or `IServiceScopeFactory`, build and register a typed provider with `AddServiceProvider(...)` instead of extracting only `IServiceScopeFactory` from a manual `BuildServiceProvider()` call.

```csharp
internal sealed class ArchiveInvokerTests : MockerTestBase<ArchiveInvoker>
{
    private readonly MockFileSystem fileSystem = new();

    protected override Action<Mocker>? SetupMocksAction => mocker =>
    {
        mocker.AddType<IFileSystem>(fileSystem, replace: true);
        mocker.AddServiceProvider(services =>
        {
            services.AddLogging();
            services.AddOptions();
            services.AddSingleton<IFileSystem>(fileSystem);
            services.AddSingleton<ArchiveService>();
        }, replace: true);
    };

    protected override Type?[]? ComponentConstructorParameterTypes =>
        new Type?[] { typeof(IFileSystem), typeof(IServiceScopeFactory) };
}
```

See the [Testing Guide](./testing-guide.md#explicit-constructor-selection-in-tests) for the full constructor-selection rules and the [typed `IServiceProvider` helper guidance](./testing-guide.md#typed-iserviceprovider-helpers) for framework-heavy ServiceCollection patterns.

## Common Patterns

### Testing Constructor Parameters

FastMoq provides helpers for testing constructor parameter validation:

```csharp
[Fact]
public void Constructor_ShouldThrow_WhenFileSystemIsNull()
{
    TestConstructorParameters((action, constructorName, parameterName) =>
    {
        action.Should()
            .Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be(parameterName);
    });
}
```

If you want diagnostic output while the helper runs, pass a framework-neutral line writer:

```csharp
TestConstructorParameters((action, constructorName, parameterName) =>
    action.EnsureNullCheckThrown(parameterName, constructorName, message => output.WriteLine(message)));
```

That keeps the FastMoq helper surface framework-neutral while still letting the test project adapt its local runner output.

### Async Method Testing

FastMoq works seamlessly with async methods:

```csharp
[Fact]
public async Task ProcessFileAsync_ShouldReturnProcessedContent()
{
    // Arrange
    var filePath = @"c:\temp\data.txt";
    var fileSystem = Mocks.GetObject<IFileSystem>();
    fileSystem.File.WriteAllText(filePath, "processed data");

    // Act
    var result = await Component.ProcessFileAsync(filePath);

    // Assert
    result.Should().Be("PROCESSED DATA");
}
```

## Next Steps

Now that you understand the basics, explore these advanced topics:

- [Cookbook](../cookbook/README.md) - Common patterns and real-world scenarios
- [Feature Parity](../feature-parity/README.md) - Compare FastMoq with other frameworks
- [Sample Applications](../samples/README.md) - Complete examples with Azure integration

## Best Practices

1. **Use descriptive test names** following the pattern `MethodName_ShouldExpectedBehavior_WhenCondition`
2. **Follow AAA pattern** (Arrange, Act, Assert) for clarity
3. **Keep tests focused** - test one behavior per test method
4. **Use one assertion style consistently** within a given test project
5. **Match the surrounding project** if it already uses fluent `.Should()` assertions or Shouldly-style assertions
6. **Include proper using statements** - Always include `FastMoq.Extensions` for logger verification helpers
7. **Verify important interactions** but avoid over-verification
8. **Group related tests** in the same test class
9. **Use provider-safe logger verification** with `Mocks.VerifyLogged(...)`

### Logger verification guidance

Prefer `Mocks.VerifyLogged(...)` for new code. It is provider-safe because FastMoq captures `ILogger` callbacks through the active `IMockingProvider` and verifies the captured entries in core.

If the test suite needs a first-party registration story for `ILoggerFactory`, `ILogger`, or `ILogger<T>`, use `Mocks.AddLoggerFactory()` for direct FastMoq resolution, or `Mocks.CreateLoggerFactory()` when you want to plug the same callback-backed factory into a typed `IServiceProvider` recipe. When the test also needs to mirror log output to a local sink after `Mocker` already exists, use the sink-aware overloads such as `Mocks.AddLoggerFactory(output.WriteLine, replace: true)` or `Mocks.CreateLoggerFactory((logLevel, eventId, message, exception) => ...)` instead of maintaining a private logger wrapper.

Use `GetOrCreateMock<ILogger<T>>().AsMoq().VerifyLogger(...)` only when you intentionally want the legacy Moq-specific behavior. That API is a compatibility shim and is planned to leave core in v5.

## Troubleshooting

### Common Issues

**Problem**: `System.MethodAccessException` when mocking DbContext
**Solution**: Add `[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]` to your AssemblyInfo.cs

**Problem**: Mock not being created for optional interface parameter
**Solution**: Set `Mocks.OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker` before creating the SUT. `MockOptional = true` is obsolete and only retained as a compatibility alias.

**Problem**: Component constructor throws exception
**Solution**: Use `SetupMocksAction` or base constructor to configure required mocks before component creation

For more troubleshooting tips, see the [FAQ](../../FAQs.md).

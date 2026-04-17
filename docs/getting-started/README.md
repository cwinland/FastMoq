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
// FastMoq
Mocks.GetOrCreateMock<ICustomerCsvParser>()
    .Setup(x => x.Parse(csv))
    .Returns(rows);

var importedCount = await Component.ImportAsync(filePath, CancellationToken.None);

Mocks.Verify<ICustomerRepository>(
    x => x.UpsertAsync(It.IsAny<IReadOnlyList<CustomerImportRow>>(), CancellationToken.None),
    TimesSpec.Once);
```

FastMoq is most valuable when the subject has multiple dependencies, when constructor signatures change frequently, or when your tests repeatedly need built-in framework helpers.

This guide is intentionally the "how to" companion to the repo README. The README is the better first read when you are deciding whether FastMoq is the right fit. This guide is the better first read when you already want to write a test.

For the repo-native testing conventions and framework-specific guidance used by this codebase, see the [FastMoq Testing Guide](./testing-guide.md).

If you want examples that run directly in this repository instead of static snippets only, see [Executable Testing Examples](../samples/testing-examples.md).

If you are updating older FastMoq usage from the last public `3.0.0` release, see [Migration Guide: 3.0.0 To Current Repo](../migration/README.md).

If you need to choose or bootstrap a provider explicitly, see the [Provider Selection Guide](./provider-selection.md).

### Key Benefits

- **Automatic Mock Creation**: No need to manually declare and setup mocks
- **Dependency Injection**: Automatically resolves and injects dependencies into your components
- **Fluent API**: Clean, readable syntax for test setup and verification
- **Provider Architecture**: Extensible system for custom mock configurations
- **Built-in Helpers**: Common patterns for EF Core, HttpClient, IFileSystem, and more
- **Less Constructor Wiring**: The test stays focused on the behavior under test instead of the dependency graph

## Installation

Install FastMoq using your preferred package manager:

### Package choices

Use this table when you are deciding which package line your test project should reference.

| If you want... | Install... | Why |
| --- | --- | --- |
| simplest all-in-one experience | `FastMoq` | Aggregate package that includes the primary runtime, shared Azure SDK helpers, database helpers, web support, Azure Functions helpers, and the FastMoq analyzer pack by default |
| lighter core-only usage | `FastMoq.Core` | Provider-first runtime without the extra EF, web-specific, or analyzer package payloads |
| Azure SDK credentials, pageable builders, or Azure-oriented DI/config helpers while using `FastMoq.Core` | `FastMoq.Azure` | Adds `PageableBuilder`, token/default-credential helpers, Azure-oriented configuration/service-provider helpers, and common Azure client registration helpers |
| Azure Functions worker helpers while using `FastMoq.Core` | `FastMoq.AzureFunctions` | Adds `CreateFunctionContextInstanceServices(...)`, `AddFunctionContextInstanceServices(...)`, `CreateHttpRequestData(...)`, `CreateHttpResponseData(...)`, and body readers in `FastMoq.AzureFunctions.Extensions` while keeping the typed `IServiceProvider` helpers in core |
| DbContext and EF-specific helpers while using `FastMoq.Core` | `FastMoq.Database` | Adds `GetMockDbContext<TContext>()` and the explicit DbContext handle modes |
| controller, `HttpContext`, `IHttpContextAccessor`, or claims-principal helpers while using `FastMoq.Core` | `FastMoq.Web` | Adds `CreateHttpContext(...)`, `CreateControllerContext(...)`, `SetupClaimsPrincipal(...)`, `AddHttpContext(...)`, and `AddHttpContextAccessor(...)` |
| Moq-specific tracked-mock extension methods during migration | `FastMoq.Provider.Moq` | Adds provider-package extension methods such as `AsMoq()`, `Setup(...)`, `SetupGet(...)`, `SetupSequence(...)`, and `Protected()` on `IFastMock<T>` |
| optional NSubstitute-backed provider support | `FastMoq.Provider.NSubstitute` | Adds the NSubstitute provider package and tracked-mock extensions such as `AsNSubstitute()` |
| analyzer guidance without the aggregate runtime package | `FastMoq.Analyzers` | Standalone Roslyn analyzers and code fixes for migration cleanup and provider-first test-authoring guidance |

Important package boundaries in the current v4 line:

`FastMoq` already includes the common end-user surface, including shared Azure SDK helpers, web, database, and Azure Functions helpers

- `FastMoq` also includes the FastMoq analyzer assets by default so most test projects get migration guidance without extra setup
- `FastMoq.Core` stays lighter on purpose, so shared Azure SDK helpers, EF helpers, Azure Functions helpers, and web helpers are separate package decisions when you consume core directly
- `FastMoq.Core` does not include analyzer assets; add `FastMoq.Analyzers` explicitly if you want analyzer guidance in a core-only package graph
- if a core-only test project stays on the legacy Moq-shaped path with `GetMock<T>()`, `VerifyLogger(...)`, `MockModel.Mock`, `SetupSet(...)`, `SetupAllProperties()`, or other Moq-specific compatibility flows, add `FastMoq.Analyzers` and `FastMoq.Provider.Moq` explicitly, then select `moq` at assembly scope instead of relying on the default `reflection` provider
- `FastMoq.Core` includes the built-in `reflection` provider and the bundled Moq compatibility runtime, but the Moq tracked-mock extension methods such as `Setup(...)` and `Protected()` still belong to the `FastMoq.Provider.Moq` package
- provider-package extension methods still follow the provider-package docs and selection rules described in [Provider Selection and Setup](./provider-selection.md)
- if you are wiring Azure SDK clients, pageable sequences, or token credentials through tests while consuming `FastMoq.Core` directly, add `FastMoq.Azure`
- if you are wiring Azure Functions worker tests through `FunctionContext.InstanceServices` or concrete HTTP-trigger request and response objects, add `FastMoq.AzureFunctions` when you consume `FastMoq.Core` directly
- if you are unsure whether your web tests need another package, see the web-helper notes in [Testing Guide](./testing-guide.md#controller-testing) and the migration-specific notes in [Framework and web helper migration](../migration/framework-and-web-helpers.md#web-test-helpers)

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
> Note: in the current repository, `GetMockDbContext<TContext>()` keeps the same `FastMoq` namespace call shape, but direct `FastMoq.Core` consumers should add `FastMoq.Database` for EF-specific helpers. Direct shared Azure SDK helper consumers should add `FastMoq.Azure`. Direct Azure Functions helper consumers should add `FastMoq.AzureFunctions`. Direct web-helper consumers should add `FastMoq.Web`.

In the current v4 transition layout, `FastMoq.Core` bundles the built-in `moq` provider and the internal `reflection` fallback. The default provider is `reflection`. Optional providers such as `nsubstitute` can be added explicitly and then selected by their canonical name once the package is present, or registered manually under a custom alias. You can also register your own provider by implementing `IMockingProvider`; the bundled providers are examples, not the only supported choices.

The DbContext helper path now exposes explicit modes through `GetDbContextHandle<TContext>(...)`. `GetMockDbContext<TContext>()` remains the mocked-sets convenience entry point, `DbContextTestMode.RealInMemory` is the real EF-backed option, and provider-first APIs such as `GetOrCreateMock<TContext>()` and `GetMockModel<TContext>()` return the same tracked context after the helper creates it.

### Required Using Statements

For most FastMoq tests, these using statements are a good starting point:

```csharp
using FastMoq;
using FastMoq.Extensions;
using Microsoft.Extensions.Logging;
using Moq; // Only needed when you intentionally use Moq-specific compatibility APIs.
```

For all FastMoq tests, these are optional and suggested, but they are not required for FastMoq:

```csharp
using Xunit; // Whatever your testing framework is
using FluentAssertions; // Used in examples (free version)
```

`FastMoq.Extensions` is especially useful because it contains logger verification helpers and other testing utilities.

Package-specific helper namespaces:

- Azure Functions worker helpers: `using FastMoq.AzureFunctions.Extensions;`
- Web helpers: `using FastMoq.Web.Extensions;`

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

Now let's create a test using FastMoq's `MockerTestBase<T>`:

```csharp
// Tests/FileProcessorServiceTests.cs
using FastMoq;
using FastMoq.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO.Abstractions;
using Xunit;

public class FileProcessorServiceTests : MockerTestBase<FileProcessorService>
{
    [Fact]
    public async Task ProcessFileAsync_ShouldReturnProcessedContent_WhenValidFile()
    {
        // Arrange
        var filePath = "test.txt";
        var fileContent = "hello world";
        var expectedResult = "HELLO WORLD";

        Mocks.GetOrCreateMock<IFileSystem>()
            .Setup(x => x.File.Exists(filePath))
            .Returns(true);

        Mocks.GetOrCreateMock<IFileSystem>()
            .Setup(x => x.File.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);

        // Act
        var result = await Component.ProcessFileAsync(filePath);

        // Assert
        result.Should().Be(expectedResult);
        
        // Verify interactions
        Mocks.Verify<IFileSystem>(
            x => x.File.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()),
            TimesSpec.Once);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldReturnEmpty_WhenInvalidFile()
    {
        // Arrange
        var filePath = "nonexistent.txt";

        Mocks.GetOrCreateMock<IFileSystem>()
            .Setup(x => x.File.Exists(filePath))
            .Returns(false);

        // Act
        var result = await Component.ProcessFileAsync(filePath);

        // Assert
        result.Should().BeEmpty();
        
        // Verify logging
        Mocks.VerifyLogged(LogLevel.Warning, "not found", 1);
        
        // Verify file was never read
        Mocks.Verify<IFileSystem>(
            x => x.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            TimesSpec.NeverCalled);
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
            .WithMessage($"*{parameterName}*");
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
    var filePath = "data.txt";
    var expectedContent = "PROCESSED DATA";
    
    Mocks.GetOrCreateMock<IFileSystem>()
        .Setup(x => x.File.Exists(filePath))
        .Returns(true);
    
    Mocks.GetOrCreateMock<IFileSystem>()
        .Setup(x => x.File.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()))
        .ReturnsAsync("processed data");

    // Act
    var result = await Component.ProcessFileAsync(filePath);

    // Assert
    result.Should().Be(expectedContent);
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
4. **Use FluentAssertions** for more readable assertions
5. **Include proper using statements** - Always include `FastMoq.Extensions` for logger verification helpers
6. **Verify important interactions** but avoid over-verification
7. **Group related tests** in the same test class
8. **Use provider-safe logger verification** with `Mocks.VerifyLogged(...)`

### Logger verification guidance

Prefer `Mocks.VerifyLogged(...)` for new code. It is provider-safe because FastMoq captures `ILogger` callbacks through the active `IMockingProvider` and verifies the captured entries in core.

If the test suite needs a first-party registration story for `ILoggerFactory`, `ILogger`, or `ILogger<T>`, use `Mocks.AddLoggerFactory()` for direct FastMoq resolution, or `Mocks.CreateLoggerFactory()` when you want to plug the same callback-backed factory into a typed `IServiceProvider` recipe.

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

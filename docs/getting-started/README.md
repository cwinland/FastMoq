# Getting Started with FastMoq

Welcome to FastMoq! This guide will walk you through setting up FastMoq and writing your first test. FastMoq is a powerful extension of the Moq framework that simplifies mocking and provides automatic dependency injection for your .NET tests.

## What is FastMoq?

FastMoq is designed to eliminate the boilerplate code typically required when setting up mocks in unit tests. It automatically creates and injects mock objects, allowing you to focus on writing test logic rather than managing mock setup.

### Key Benefits

- **Automatic Mock Creation**: No need to manually declare and setup mocks
- **Dependency Injection**: Automatically resolves and injects dependencies into your components
- **Fluent API**: Clean, readable syntax for test setup and verification
- **Provider Architecture**: Extensible system for custom mock configurations
- **Built-in Helpers**: Common patterns for EF Core, HttpClient, IFileSystem, and more

## Installation

Install FastMoq using your preferred package manager:

### .NET CLI
```bash
dotnet add package FastMoq
```

### Package Manager Console
```powershell
Install-Package FastMoq
```

### PackageReference
```xml
<PackageReference Include="FastMoq" Version="3.0.0" />
```

### Required Using Statements

For all FastMoq tests, include these using statements:

```csharp
using FastMoq;
using FastMoq.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
```

The `FastMoq.Extensions` namespace is particularly important as it provides logger verification helpers and other utility methods.

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

        Mocks.GetMock<IFileSystem>()
            .Setup(x => x.File.Exists(filePath))
            .Returns(true);

        Mocks.GetMock<IFileSystem>()
            .Setup(x => x.File.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);

        // Act
        var result = await Component.ProcessFileAsync(filePath);

        // Assert
        result.Should().Be(expectedResult);
        
        // Verify interactions
        Mocks.GetMock<IFileSystem>()
            .Verify(x => x.File.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldReturnEmpty_WhenInvalidFile()
    {
        // Arrange
        var filePath = "nonexistent.txt";

        Mocks.GetMock<IFileSystem>()
            .Setup(x => x.File.Exists(filePath))
            .Returns(false);

        // Act
        var result = await Component.ProcessFileAsync(filePath);

        // Assert
        result.Should().BeEmpty();
        
        // Verify logging
        Mocks.GetMock<ILogger<FileProcessorService>>()
            .VerifyLogger(LogLevel.Warning, Times.Once());
        
        // Verify file was never read
        Mocks.GetMock<IFileSystem>()
            .Verify(x => x.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
}
```

## Understanding the FastMoq Architecture

### MockerTestBase<T>

`MockerTestBase<T>` is the foundation of FastMoq testing. It automatically:

1. **Creates your component**: The `Component` property contains an instance of your class under test
2. **Manages dependencies**: All constructor parameters are automatically mocked
3. **Provides mock access**: Use `Mocks.GetMock<T>()` to configure mock behavior
4. **Handles cleanup**: Mocks are properly disposed after each test

### Key Properties and Methods

| Property/Method | Description |
|----------------|-------------|
| `Component` | The instance of your class under test |
| `Mocks` | The `Mocker` instance that manages all mocks |
| `Mocks.GetMock<T>()` | Gets the mock for interface T |
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
        mocker.GetMock<IPaymentProcessor>()
            .Setup(x => x.ValidateCard(It.IsAny<string>()))
            .Returns(true);
        
        // Set global options
        mocker.MockOptional = true; // Mock optional parameters
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
        mocker.GetMock<IOrderRepository>()
            .Setup(x => x.GetOrderAsync(It.IsAny<int>()))
            .ReturnsAsync(new Order());
    }
}
```

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

### Async Method Testing

FastMoq works seamlessly with async methods:

```csharp
[Fact]
public async Task ProcessFileAsync_ShouldReturnProcessedContent()
{
    // Arrange
    var filePath = "data.txt";
    var expectedContent = "PROCESSED DATA";
    
    Mocks.GetMock<IFileSystem>()
        .Setup(x => x.File.Exists(filePath))
        .Returns(true);
    
    Mocks.GetMock<IFileSystem>()
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
8. **Use proper logger verification** with `GetMock<ILogger<T>>().VerifyLogger()` pattern

## Troubleshooting

### Common Issues

**Problem**: `System.MethodAccessException` when mocking DbContext
**Solution**: Add `[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]` to your AssemblyInfo.cs

**Problem**: Mock not being created for interface
**Solution**: Ensure the interface is public and the parameter isn't optional (unless `MockOptional = true`)

**Problem**: Component constructor throws exception
**Solution**: Use `SetupMocksAction` or base constructor to configure required mocks before component creation

For more troubleshooting tips, see the [FAQ](../../FAQs.md).
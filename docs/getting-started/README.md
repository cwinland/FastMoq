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

First, let's create a simple service that depends on an external interface:

```csharp
// Services/OrderService.cs
public interface IPaymentProcessor
{
    Task<bool> ProcessPaymentAsync(decimal amount, string cardNumber);
    bool ValidateCard(string cardNumber);
}

public interface IOrderRepository
{
    Task<Order> GetOrderAsync(int orderId);
    Task SaveOrderAsync(Order order);
}

public class Order
{
    public int Id { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
}

public class OrderService
{
    private readonly IPaymentProcessor _paymentProcessor;
    private readonly IOrderRepository _orderRepository;

    public OrderService(IPaymentProcessor paymentProcessor, IOrderRepository orderRepository)
    {
        _paymentProcessor = paymentProcessor ?? throw new ArgumentNullException(nameof(paymentProcessor));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    public async Task<bool> ProcessOrderAsync(int orderId, string cardNumber)
    {
        var order = await _orderRepository.GetOrderAsync(orderId);
        if (order == null) return false;

        if (!_paymentProcessor.ValidateCard(cardNumber))
            return false;

        var paymentResult = await _paymentProcessor.ProcessPaymentAsync(order.Total, cardNumber);
        
        if (paymentResult)
        {
            order.Status = "Completed";
            await _orderRepository.SaveOrderAsync(order);
        }

        return paymentResult;
    }
}
```

### 2. Write Your First FastMoq Test

Now let's create a test using FastMoq's `MockerTestBase<T>`:

```csharp
// Tests/OrderServiceTests.cs
using FastMoq;
using FastMoq.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class OrderServiceTests : MockerTestBase<OrderService>
{
    [Fact]
    public async Task ProcessOrderAsync_ShouldCompleteOrder_WhenValidOrder()
    {
        // Arrange
        var orderId = 123;
        var cardNumber = "4111111111111111";
        var order = new Order 
        { 
            Id = orderId, 
            Total = 99.99m, 
            Status = "Pending" 
        };

        // Setup mocks using the fluent API
        Mocks.GetMock<IOrderRepository>()
            .Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync(order);

        Mocks.GetMock<IPaymentProcessor>()
            .Setup(x => x.ValidateCard(cardNumber))
            .Returns(true);

        Mocks.GetMock<IPaymentProcessor>()
            .Setup(x => x.ProcessPaymentAsync(order.Total, cardNumber))
            .ReturnsAsync(true);

        // Act
        var result = await Component.ProcessOrderAsync(orderId, cardNumber);

        // Assert
        result.Should().BeTrue();
        order.Status.Should().Be("Completed");
        
        // Verify interactions
        Mocks.GetMock<IOrderRepository>()
            .Verify(x => x.SaveOrderAsync(order), Times.Once);
    }

    [Fact]
    public async Task ProcessOrderAsync_ShouldReturnFalse_WhenInvalidCard()
    {
        // Arrange
        var orderId = 123;
        var cardNumber = "invalid-card";
        var order = new Order { Id = orderId, Total = 99.99m };

        Mocks.GetMock<IOrderRepository>()
            .Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync(order);

        Mocks.GetMock<IPaymentProcessor>()
            .Setup(x => x.ValidateCard(cardNumber))
            .Returns(false); // Invalid card

        // Act
        var result = await Component.ProcessOrderAsync(orderId, cardNumber);

        // Assert
        result.Should().BeFalse();
        order.Status.Should().Be("Pending"); // Status unchanged
        
        // Verify payment was never attempted
        Mocks.GetMock<IPaymentProcessor>()
            .Verify(x => x.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
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
public void Constructor_ShouldThrow_WhenPaymentProcessorIsNull()
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
public async Task GetOrderAsync_ShouldReturnOrder()
{
    // Arrange
    var expectedOrder = new Order { Id = 1 };
    Mocks.GetMock<IOrderRepository>()
        .Setup(x => x.GetOrderAsync(1))
        .ReturnsAsync(expectedOrder);

    // Act
    var result = await Component.GetOrderAsync(1);

    // Assert
    result.Should().Be(expectedOrder);
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
# E-Commerce Order Processing Sample

This sample demonstrates a complete e-commerce order processing system with Azure integration, showcasing FastMoq's capabilities in testing complex, real-world applications.

## Architecture Overview

The sample includes:

- **Order API** - REST API for order management
- **Payment Processing** - Integration with external payment services
- **Inventory Management** - Stock tracking and reservation
- **Azure Service Bus** - Asynchronous order processing
- **Azure Blob Storage** - Receipt and document storage
- **Azure Key Vault** - Secure configuration management
- **Entity Framework Core** - Data persistence with Azure SQL
- **Background Services** - Order fulfillment processing

## Key Features Demonstrated

### Azure Service Bus Integration
- Message publishing and consumption
- Dead letter queue handling
- Message correlation and tracking
- Testing message flows with FastMoq

### Azure Storage Operations
- Blob upload and download
- Container management
- Testing storage operations without actual Azure resources

### Configuration Management
- Azure Key Vault integration
- Environment-specific configuration
- Options pattern implementation
- Testing configuration scenarios

### Complex Dependencies
- Multiple service integrations
- Transactional operations
- Error handling and retries
- Comprehensive logging

## Project Structure

```
ecommerce-orders/
├── src/
│   ├── ECommerce.Orders.Api/          # Web API project
│   ├── ECommerce.Orders.Core/         # Business logic
│   ├── ECommerce.Orders.Infrastructure/ # External integrations
│   └── ECommerce.Orders.Shared/       # Common models and contracts
├── tests/
│   ├── ECommerce.Orders.Api.Tests/    # API integration tests
│   ├── ECommerce.Orders.Core.Tests/   # Unit tests with FastMoq
│   └── ECommerce.Orders.Integration/  # Full integration tests
└── docs/
    ├── api-documentation.md
    └── deployment-guide.md
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Azure subscription (for full deployment)
- Docker Desktop (optional)

### Local Development Setup

1. **Clone and navigate to the sample**
   ```bash
   cd docs/samples/ecommerce-orders
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Configure local settings**
   ```bash
   cp src/ECommerce.Orders.Api/appsettings.Development.template.json src/ECommerce.Orders.Api/appsettings.Development.json
   ```

4. **Run the application**
   ```bash
   dotnet run --project src/ECommerce.Orders.Api
   ```

5. **Run the tests**
   ```bash
   dotnet test
   ```

## FastMoq Testing Patterns Demonstrated

### 1. API Controller Testing with Complex Dependencies

```csharp
public class OrdersControllerTests : MockerTestBase<OrdersController>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        // Setup Azure Service Bus mock
        mocker.GetMock<IServiceBusClient>()
            .Setup(x => x.CreateSender(It.IsAny<string>()))
            .Returns(mocker.GetMock<ServiceBusSender>().Object);

        // Setup Azure Storage mock
        mocker.GetMock<IBlobServiceClient>()
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(mocker.GetMock<BlobContainerClient>().Object);

        // Setup configuration
        var config = new Dictionary<string, string>
        {
            ["ServiceBus:ConnectionString"] = "test-connection-string",
            ["Storage:ConnectionString"] = "test-storage-connection"
        };
        
        mocker.AddType<IConfiguration>(() => 
            new ConfigurationBuilder().AddInMemoryCollection(config).Build());
    };

    [Fact]
    public async Task CreateOrder_WithValidData_ShouldCreateOrderAndPublishMessage()
    {
        // Arrange
        var orderRequest = new CreateOrderRequest
        {
            CustomerId = 123,
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, Quantity = 2, Price = 29.99m }
            }
        };

        Mocks.GetMock<IInventoryService>()
            .Setup(x => x.ReserveStockAsync(It.IsAny<List<StockReservation>>()))
            .ReturnsAsync(true);

        // Act
        var result = await Component.CreateOrderAsync(orderRequest);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var order = createdResult.Value.Should().BeOfType<OrderDto>().Subject;
        
        order.CustomerId.Should().Be(orderRequest.CustomerId);
        order.TotalAmount.Should().Be(59.98m);

        // Verify message was published
        Mocks.GetMock<ServiceBusSender>()
            .Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), 
                Times.Once);

        // Verify stock reservation
        Mocks.GetMock<IInventoryService>()
            .Verify(x => x.ReserveStockAsync(It.IsAny<List<StockReservation>>()), Times.Once);
    }
}
```

### 2. Background Service Testing

```csharp
public class OrderProcessingServiceTests : MockerTestBase<OrderProcessingService>
{
    [Fact]
    public async Task ProcessOrderMessages_ShouldHandleValidMessages()
    {
        // Arrange
        var orderMessage = new OrderCreatedMessage
        {
            OrderId = 123,
            CustomerId = 456,
            TotalAmount = 99.99m
        };

        var cancellationTokenSource = new CancellationTokenSource();

        Mocks.GetMock<ServiceBusProcessor>()
            .Setup(x => x.StartProcessingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Simulate message processing
        var messageArgs = CreateProcessMessageEventArgs(orderMessage);
        
        // Act
        await Component.ProcessMessageAsync(messageArgs);

        // Assert
        Mocks.GetMock<IOrderFulfillmentService>()
            .Verify(x => x.ProcessOrderAsync(orderMessage.OrderId), Times.Once);
            
        Mocks.VerifyLogger<OrderProcessingService>(LogLevel.Information,
            "Successfully processed order {OrderId}", Times.Once());
    }

    [Fact]
    public async Task ProcessOrderMessages_WhenProcessingFails_ShouldDeadLetterMessage()
    {
        // Arrange
        var orderMessage = new OrderCreatedMessage { OrderId = 123 };
        
        Mocks.GetMock<IOrderFulfillmentService>()
            .Setup(x => x.ProcessOrderAsync(It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Processing failed"));

        var messageArgs = CreateProcessMessageEventArgs(orderMessage);

        // Act
        await Component.ProcessMessageAsync(messageArgs);

        // Assert
        // Verify message was dead lettered
        messageArgs.DeadLetterMessageAsync(It.IsAny<string>(), It.IsAny<string>())
            .Should().HaveBeenCalled();
            
        Mocks.VerifyLogger<OrderProcessingService>(LogLevel.Error, Times.Once());
    }
}
```

### 3. Azure Storage Integration Testing

```csharp
public class ReceiptServiceTests : MockerTestBase<ReceiptService>
{
    [Fact]
    public async Task GenerateReceiptAsync_ShouldUploadToAzureStorage()
    {
        // Arrange
        var order = new Order { Id = 123, CustomerId = 456, TotalAmount = 99.99m };
        var receiptPdf = new byte[] { 1, 2, 3, 4, 5 };

        Mocks.GetMock<IPdfGenerator>()
            .Setup(x => x.GenerateReceiptPdfAsync(order))
            .ReturnsAsync(receiptPdf);

        var blobClient = Mocks.GetMock<BlobClient>();
        Mocks.GetMock<BlobContainerClient>()
            .Setup(x => x.GetBlobClient($"receipts/order_{order.Id}_receipt.pdf"))
            .Returns(blobClient.Object);

        blobClient.Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        // Act
        var receiptUrl = await Component.GenerateReceiptAsync(order);

        // Assert
        receiptUrl.Should().NotBeNullOrEmpty();
        receiptUrl.Should().Contain($"order_{order.Id}_receipt.pdf");

        blobClient.Verify(x => x.UploadAsync(
            It.Is<Stream>(s => s.Length == receiptPdf.Length),
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateReceiptAsync_WhenUploadFails_ShouldThrowException()
    {
        // Arrange
        var order = new Order { Id = 123 };
        
        Mocks.GetMock<IPdfGenerator>()
            .Setup(x => x.GenerateReceiptPdfAsync(order))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        var blobClient = Mocks.GetMock<BlobClient>();
        Mocks.GetMock<BlobContainerClient>()
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClient.Object);

        blobClient.Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("Storage error"));

        // Act & Assert
        await Assert.ThrowsAsync<RequestFailedException>(() =>
            Component.GenerateReceiptAsync(order));
    }
}
```

### 4. Entity Framework Core with Azure SQL

```csharp
public class OrderRepositoryTests : MockerTestBase<OrderRepository>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        var dbContextMock = mocker.GetMockDbContext<ECommerceDbContext>();
        mocker.AddType(_ => dbContextMock.Object);
    };

    [Fact]
    public async Task GetOrderWithItemsAsync_ShouldIncludeOrderItems()
    {
        // Arrange
        var orderId = 123;
        var order = new Order
        {
            Id = orderId,
            CustomerId = 456,
            Status = OrderStatus.Pending,
            Items = new List<OrderItem>
            {
                new() { Id = 1, ProductId = 10, Quantity = 2, Price = 25.00m },
                new() { Id = 2, ProductId = 20, Quantity = 1, Price = 49.99m }
            }
        };

        var dbContext = Mocks.GetRequiredObject<ECommerceDbContext>();
        dbContext.Orders.Add(order);
        dbContext.SaveChanges();

        // Act
        var result = await Component.GetOrderWithItemsAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(orderId);
        result.Items.Should().HaveCount(2);
        result.Items.Sum(i => i.Quantity * i.Price).Should().Be(99.99m);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldSetCreatedTimestamp()
    {
        // Arrange
        var order = new Order
        {
            CustomerId = 123,
            Status = OrderStatus.Pending
        };

        // Act
        var result = await Component.CreateOrderAsync(order);

        // Assert
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Id.Should().BeGreaterThan(0);

        var dbContext = Mocks.GetRequiredObject<ECommerceDbContext>();
        dbContext.Orders.Should().Contain(result);
    }
}
```

### 5. Payment Service Integration

```csharp
public class PaymentServiceTests : MockerTestBase<PaymentService>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        // Setup HttpClient for payment gateway API
        var paymentResponse = new PaymentGatewayResponse
        {
            TransactionId = "tx_12345",
            Status = "approved",
            Amount = 99.99m
        };

        mocker.SetupHttpPost("payments/charge")
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(paymentResponse))
            });

        // Setup Key Vault for API keys
        mocker.GetMock<IKeyVaultService>()
            .Setup(x => x.GetSecretAsync("payment-gateway-api-key"))
            .ReturnsAsync("test-api-key-12345");
    };

    [Fact]
    public async Task ProcessPaymentAsync_WithValidCard_ShouldReturnSuccess()
    {
        // Arrange
        var paymentRequest = new PaymentRequest
        {
            Amount = 99.99m,
            Currency = "USD",
            CardNumber = "4111111111111111",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Cvv = "123"
        };

        // Act
        var result = await Component.ProcessPaymentAsync(paymentRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.TransactionId.Should().Be("tx_12345");
        result.Amount.Should().Be(99.99m);

        // Verify API call was made with correct data
        Mocks.VerifyHttpPost("payments/charge", Times.Once());
        
        // Verify Key Vault was accessed
        Mocks.GetMock<IKeyVaultService>()
            .Verify(x => x.GetSecretAsync("payment-gateway-api-key"), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenGatewayReturnsError_ShouldReturnFailure()
    {
        // Arrange
        Mocks.SetupHttpPost("payments/charge")
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    error = "invalid_card",
                    message = "The card number is invalid"
                }))
            });

        var paymentRequest = new PaymentRequest
        {
            Amount = 99.99m,
            CardNumber = "4000000000000002" // Invalid card for testing
        };

        // Act
        var result = await Component.ProcessPaymentAsync(paymentRequest);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("invalid_card");
        result.ErrorMessage.Should().Be("The card number is invalid");
    }
}
```

## Key Testing Benefits Demonstrated

### 1. Reduced Setup Complexity
Traditional approach would require extensive mock setup for each dependency. FastMoq automatically handles:
- Entity Framework DbContext mocking
- HttpClient configuration
- Logger setup
- Configuration binding
- Azure service client mocking

### 2. Real-World Scenario Coverage
The sample demonstrates testing of:
- Complex business workflows
- External service integration
- Asynchronous message processing
- File upload/download operations
- Database transactions
- Error handling and resilience

### 3. Maintainable Test Code
FastMoq enables:
- Consistent test structure across the application
- Easy mock configuration and verification
- Clear separation of test concerns
- Reusable test patterns

## Running the Sample

1. **Prerequisites Setup**
   ```bash
   # Install dependencies
   dotnet restore
   
   # Set up local development database
   dotnet ef database update --project src/ECommerce.Orders.Infrastructure
   ```

2. **Run Unit Tests**
   ```bash
   dotnet test tests/ECommerce.Orders.Core.Tests
   ```

3. **Run Integration Tests**
   ```bash
   dotnet test tests/ECommerce.Orders.Integration
   ```

4. **Run the API**
   ```bash
   dotnet run --project src/ECommerce.Orders.Api
   ```

5. **Test the API**
   ```bash
   curl -X POST https://localhost:5001/api/orders \
     -H "Content-Type: application/json" \
     -d '{
       "customerId": 123,
       "items": [
         {"productId": 1, "quantity": 2, "price": 29.99}
       ]
     }'
   ```

## Deployment

The sample includes Azure deployment templates and GitHub Actions workflows for:
- Azure App Service deployment
- Azure SQL Database setup
- Azure Service Bus configuration
- Azure Storage Account creation
- Azure Key Vault setup

See the [deployment guide](docs/deployment-guide.md) for detailed instructions.

## Learning Outcomes

After exploring this sample, you'll understand:
- How to structure tests for complex applications
- Effective patterns for mocking Azure services
- Testing asynchronous and background operations
- Handling configuration and secrets in tests
- Integration testing strategies
- Performance testing with FastMoq

## Next Steps

- Explore the [Microservices Communication sample](../microservices/) for service-to-service patterns
- Check out the [Blazor Web Application sample](../blazor-webapp/) for frontend testing
- Review the [Background Processing sample](../background-services/) for more queue processing patterns
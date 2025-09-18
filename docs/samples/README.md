# FastMoq Sample Applications

This directory contains complete sample applications demonstrating FastMoq usage in real-world scenarios.

## 🚀 Available Samples

### [Azure Integration Sample](azure-sample/)
A comprehensive .NET application showcasing Azure service integration testing:

- **Azure Storage** - Blob, Table, and Queue services
- **Azure Service Bus** - Message publishing and subscription
- **Azure Key Vault** - Configuration and secrets management
- **Azure Functions** - HTTP and timer-triggered functions
- **Authentication** - Azure AD integration
- **Logging** - Application Insights integration
- **Configuration** - Azure App Configuration

**Technologies:**
- ASP.NET Core Web API
- Entity Framework Core with Azure SQL
- Azure SDK for .NET
- FastMoq testing framework

### [E-Commerce Microservices](ecommerce-microservices/) 
*Coming Soon*

Distributed microservices architecture with:
- Order Management Service
- Inventory Service  
- Payment Service
- Notification Service
- API Gateway

### [Background Processing Sample](background-processing/)
*Coming Soon*

Demonstrates testing of:
- Hosted Services
- Message Queue Processors
- Scheduled Jobs
- Event-driven Architecture

## 📁 Sample Structure

Each sample follows this structure:

```
sample-name/
├── README.md              # Setup and overview
├── src/                   # Source code
│   ├── API/              # Web API project
│   ├── Services/         # Business logic
│   ├── Infrastructure/   # Data access and external services
│   └── Models/           # Shared models
├── tests/                # Test projects
│   ├── UnitTests/        # FastMoq unit tests
│   ├── IntegrationTests/ # Integration tests with TestContainers
│   └── Common/           # Shared test utilities
├── docs/                 # Sample-specific documentation
└── infrastructure/       # IaC and deployment scripts
```

## 🎯 Learning Objectives

Each sample is designed to teach specific FastMoq patterns:

### Azure Integration Sample
- Auto-injection of Azure SDK clients
- Mocking Azure services with limited interfaces
- Testing configuration and options patterns
- Handling Azure authentication in tests
- Testing resilience and retry policies

### E-Commerce Microservices
- Inter-service communication testing
- Event-driven architecture patterns
- Distributed transaction testing
- Service discovery mocking

### Background Processing
- Hosted service testing
- Message queue mocking
- Scheduled job testing
- Event handling patterns

## 🏁 Getting Started

1. **Choose a sample** that matches your scenario
2. **Follow the README** in the sample directory
3. **Run the application** to see it working
4. **Examine the tests** to understand FastMoq patterns
5. **Adapt the patterns** to your own projects

## 💡 Common Patterns Across Samples

### Test Organization
```csharp
namespace YourApp.Tests.Services
{
    public class OrderServiceTests : MockerTestBase<OrderService>
    {
        public class CreateOrderAsync : OrderServiceTests
        {
            [Fact]
            public async Task ShouldSucceed_WhenValidInput() { }
        }
        
        public class ProcessPaymentAsync : OrderServiceTests  
        {
            [Fact]
            public async Task ShouldRetry_WhenTransientFailure() { }
        }
    }
}
```

### Azure Service Mocking
```csharp
public class AzureStorageTests : MockerTestBase<DocumentService>
{
    [Fact]
    public async Task UploadDocument_ShouldSucceed()
    {
        // FastMoq automatically mocks BlobServiceClient
        Mocks.GetMock<BlobServiceClient>()
            .Setup(x => x.GetBlobContainerClient("documents"))
            .Returns(Mocks.GetObject<BlobContainerClient>());
            
        var result = await Component.UploadDocumentAsync(stream, "test.pdf");
        
        result.Should().BeTrue();
    }
}
```

### Configuration Testing
```csharp
[Fact]
public void ShouldUseCorrectSettings()
{
    // FastMoq automatically handles IOptions<T>
    Mocks.GetMock<IOptions<AzureSettings>>()
        .Setup(x => x.Value)
        .Returns(new AzureSettings 
        { 
            StorageConnectionString = "test-connection" 
        });
        
    var result = Component.GetStorageConnection();
    
    result.Should().Be("test-connection");
}
```

---

**Start with:** [Azure Integration Sample](azure-sample/) - Complete Azure-enabled application with comprehensive testing
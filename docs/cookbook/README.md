# FastMoq Cookbook

This cookbook contains practical recipes for common mocking scenarios using FastMoq. Each recipe includes working code examples that you can adapt to your specific needs.

## 📚 Available Recipes

### Web Development
- **[API Controllers](api-controllers.md)** - Testing ASP.NET Core controllers with dependency injection
- **[Middleware Testing](middleware.md)** - Mocking HTTP context and pipeline components
- **[Authentication & Authorization](auth.md)** - Testing identity and claims-based scenarios

### Data Access
- **[Entity Framework Core](ef-core.md)** - DbContext mocking and in-memory testing
- **[Repository Pattern](repository-pattern.md)** - Testing data access layers
- **[Caching Strategies](caching.md)** - Redis, in-memory, and distributed cache testing

### Background Services
- **[Background Services](background-services.md)** - Testing hosted services and worker processes
- **[Message Queues](message-queues.md)** - Azure Service Bus, RabbitMQ patterns
- **[Scheduled Jobs](scheduled-jobs.md)** - Hangfire, Quartz.NET testing

### Azure Integration
- **[Azure Storage](azure-storage.md)** - Blob, Table, and Queue storage mocking
- **[Azure Service Bus](azure-service-bus.md)** - Message publishing and subscription testing
- **[Azure Key Vault](azure-key-vault.md)** - Secrets and configuration management
- **[Azure Functions](azure-functions.md)** - Function app testing patterns

### Configuration & Logging
- **[Configuration Testing](configuration.md)** - IConfiguration and IOptions<T> patterns
- **[Logging Scenarios](logging.md)** - Advanced ILogger<T> testing and verification
- **[Health Checks](health-checks.md)** - Testing application health monitoring

### Advanced Patterns
- **[CQRS & MediatR](cqrs-mediatr.md)** - Command and query handler testing
- **[Event Sourcing](event-sourcing.md)** - Domain events and saga patterns
- **[Microservices](microservices.md)** - Inter-service communication testing

## 🎯 Recipe Categories

### 🟢 Beginner Recipes
Perfect for getting started with FastMoq patterns:
- [API Controllers](api-controllers.md)
- [Configuration Testing](configuration.md)
- [Basic Repository Pattern](repository-pattern.md)

### 🟡 Intermediate Recipes
For developers comfortable with dependency injection:
- [Entity Framework Core](ef-core.md)
- [Background Services](background-services.md)
- [Azure Storage](azure-storage.md)

### 🔴 Advanced Recipes
Complex scenarios requiring deep understanding:
- [CQRS & MediatR](cqrs-mediatr.md)
- [Event Sourcing](event-sourcing.md)
- [Microservices](microservices.md)

## 🏗️ Recipe Template

Each recipe follows this structure:

```markdown
# Recipe Title

## 📋 Scenario
Brief description of what we're testing

## 🎯 Goals
- What the test should verify
- Edge cases to consider
- Performance considerations

## 🔧 Implementation
Step-by-step code examples

## ✅ Verification
How to verify the behavior

## 🚀 Advanced Patterns
Extensions and variations

## 💡 Tips & Tricks
Best practices and common pitfalls
```

## 💡 General Tips

### Test Organization
```csharp
public class ServiceTests : MockerTestBase<MyService>
{
    // Group related tests in nested classes
    public class WhenProcessingOrders : ServiceTests
    {
        [Fact]
        public void ShouldValidateInput() { /* ... */ }
        
        [Fact]
        public void ShouldHandleNullInput() { /* ... */ }
    }
    
    public class WhenSendingNotifications : ServiceTests
    {
        [Fact]
        public void ShouldUseCorrectTemplate() { /* ... */ }
    }
}
```

### Mock Reuse Patterns
```csharp
public abstract class BaseRepositoryTests<T> : MockerTestBase<T> where T : class
{
    protected void SetupSuccessfulSave()
    {
        Mocks.GetMock<IRepository>()
            .Setup(x => x.SaveAsync(It.IsAny<object>()))
            .ReturnsAsync(true);
    }
    
    protected void SetupFailedSave()
    {
        Mocks.GetMock<IRepository>()
            .Setup(x => x.SaveAsync(It.IsAny<object>()))
            .ThrowsAsync(new InvalidOperationException("Save failed"));
    }
}
```

### Common Assertions
```csharp
// Verify method calls
Mocks.GetMock<IService>().Verify(x => x.Method(), Times.Once);

// Verify logging
Mocks.VerifyLogger<MyService>(LogLevel.Error, "Expected message");

// Verify exceptions
var act = () => Component.ProcessInvalidData();
act.Should().Throw<ArgumentException>().WithMessage("Invalid data");

// Verify async operations
var result = await Component.ProcessAsync();
result.Should().NotBeNull();
```

---

Choose a recipe that matches your scenario, or start with the [API Controllers](api-controllers.md) recipe for a comprehensive introduction to FastMoq patterns.
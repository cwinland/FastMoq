# Getting Started with FastMoq

Welcome to FastMoq! This guide will help you set up FastMoq and write your first test in minutes.

## 🎯 What is FastMoq?

FastMoq is an advanced mocking framework for .NET that extends Moq with automatic dependency injection, fluent syntax, and built-in support for modern .NET patterns. It eliminates boilerplate code and simplifies testing of complex dependency graphs.

## 📦 Installation

### Using .NET CLI
```bash
dotnet add package FastMoq
```

### Using Package Manager Console
```powershell
Install-Package FastMoq
```

### Using PackageReference
```xml
<PackageReference Include="FastMoq" Version="3.*" />
```

## 🏗️ Architecture Overview

FastMoq follows a provider-based architecture with two main components:

### Core Components

1. **`Mocker`** - The primary class for auto-injection and mock management
2. **`MockerTestBase<T>`** - Base class for easy test setup with direct access to mocks
3. **Provider System** - Extensible architecture for custom mock creation logic

### Key Concepts

- **Auto-injection**: Automatically resolves and creates mocks for constructor dependencies
- **Component Creation**: Creates instances with fully mocked dependency trees
- **Mock Management**: Centralized mock storage accessible throughout your tests

## 🚀 Your First Test

Let's create a simple service and test it with FastMoq.

### 1. Create a Service to Test

```csharp
public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body);
}

public interface IUserRepository
{
    Task<User> GetUserAsync(int id);
}

public class NotificationService
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailService emailService, 
        IUserRepository userRepository,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<bool> NotifyUserAsync(int userId, string message)
    {
        var user = await _userRepository.GetUserAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return false;
        }

        return await _emailService.SendEmailAsync(user.Email, "Notification", message);
    }
}
```

### 2. Write Your First FastMoq Test

```csharp
using FastMoq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class NotificationServiceTests : MockerTestBase<NotificationService>
{
    [Fact]
    public async Task NotifyUserAsync_ShouldReturnTrue_WhenUserExistsAndEmailSent()
    {
        // Arrange - FastMoq automatically creates and injects all mocks
        var user = new User { Id = 1, Email = "test@example.com" };
        
        Mocks.GetMock<IUserRepository>()
            .Setup(x => x.GetUserAsync(1))
            .ReturnsAsync(user);
            
        Mocks.GetMock<IEmailService>()
            .Setup(x => x.SendEmailAsync("test@example.com", "Notification", "Hello"))
            .ReturnsAsync(true);

        // Act - Component is automatically created with all dependencies injected
        var result = await Component.NotifyUserAsync(1, "Hello");

        // Assert
        result.Should().BeTrue();
        
        // Verify interactions
        Mocks.GetMock<IUserRepository>()
            .Verify(x => x.GetUserAsync(1), Times.Once);
            
        Mocks.GetMock<IEmailService>()
            .Verify(x => x.SendEmailAsync("test@example.com", "Notification", "Hello"), Times.Once);
    }

    [Fact]
    public async Task NotifyUserAsync_ShouldReturnFalse_WhenUserNotFound()
    {
        // Arrange
        Mocks.GetMock<IUserRepository>()
            .Setup(x => x.GetUserAsync(999))
            .ReturnsAsync((User)null);

        // Act
        var result = await Component.NotifyUserAsync(999, "Hello");

        // Assert
        result.Should().BeFalse();
        
        // Verify logging
        Mocks.VerifyLogger<NotificationService>(LogLevel.Warning, "User 999 not found");
        
        // Verify email service was never called
        Mocks.GetMock<IEmailService>()
            .Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), 
                   Times.Never);
    }
}
```

## 🔑 Key Features Demonstrated

### Automatic Dependency Injection
- No need to manually create or pass mocks to constructors
- FastMoq automatically identifies and mocks all dependencies
- `Component` property provides the fully-injected instance under test

### Centralized Mock Management
- Access mocks through `Mocks.GetMock<T>()`
- No need to declare mock fields or properties
- Mocks are automatically created when first accessed

### Built-in Logger Testing
- `Mocks.VerifyLogger<T>()` for easy log verification  
- Automatic `ILogger<T>` injection and setup
- Support for all log levels and message patterns

### Fluent API
- Chain operations for complex test scenarios
- Clear, readable test code
- Reduced boilerplate compared to traditional mocking

## 🔧 Advanced Setup Options

### Custom Mock Configuration

```csharp
public class AdvancedNotificationTests : MockerTestBase<NotificationService>
{
    public AdvancedNotificationTests() : base(ConfigureMocks) { }

    private static void ConfigureMocks(Mocker mocker)
    {
        // Pre-configure mocks with default behaviors
        mocker.GetMock<IEmailService>()
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Configure complex objects
        mocker.AddType<IOptions<EmailSettings>>(() => 
            Options.Create(new EmailSettings { SmtpServer = "localhost" }));
    }
}
```

### Custom Component Creation

```csharp
public class CustomNotificationTests : MockerTestBase<NotificationService>
{
    public CustomNotificationTests() : base(
        ConfigureMocks, 
        CreateCustomComponent, 
        ValidateComponent) { }

    private static void ConfigureMocks(Mocker mocker) { /* setup */ }
    
    private static NotificationService CreateCustomComponent(Mocker mocker)
    {
        // Custom instantiation logic
        return new NotificationService(
            mocker.GetObject<IEmailService>(),
            mocker.GetObject<IUserRepository>(),
            mocker.GetObject<ILogger<NotificationService>>());
    }
    
    private static void ValidateComponent(NotificationService component)
    {
        component.Should().NotBeNull();
        // Additional validation
    }
}
```

## 🎉 What's Next?

Now that you've created your first FastMoq test, explore these advanced features:

- **[Feature Parity Table](feature-parity.md)** - See how FastMoq compares to Moq and NSubstitute
- **[Cookbook Recipes](cookbook/)** - Common patterns for API controllers, EF Core, and more
- **[Azure Sample App](samples/azure-sample/)** - Real-world application with Azure services
- **[Performance Comparisons](performance.md)** - See the productivity benefits

## 🤔 Common Questions

### Why inherit from MockerTestBase<T>?
- Automatic mock lifecycle management
- Built-in component creation and injection
- Access to powerful helper methods
- Consistent test structure across your project

### Can I use FastMoq without the base class?
Yes! You can use `Mocker` directly:

```csharp
[Fact]
public void DirectMockerUsage()
{
    var mocker = new Mocker();
    var component = mocker.CreateInstance<NotificationService>();
    
    mocker.GetMock<IEmailService>()
        .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(true);
    
    // Use component...
}
```

### What about async testing?
FastMoq works seamlessly with async/await patterns. All examples above demonstrate async testing best practices.

---

**Next:** [Feature Parity Table](feature-parity.md) - Compare FastMoq with other mocking frameworks
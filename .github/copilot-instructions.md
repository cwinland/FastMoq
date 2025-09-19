# FastMoq – Copilot Instructions

## 📜 Project Overview
FastMoq is an **extension framework for mocking and auto‑injection** in .NET tests.  
It wraps and extends mocking providers (currently Moq, with planned provider‑agnostic support) to:
- Auto‑create and inject mocks into components under test.
- Support both public and internal/protected types via `MockerTestBase<T>` and non‑generic `MockerTestBase`.
- Provide fluent scenario building (`.With()`, `.When()`, `.Then()`, `.Verify()`).
- Offer verification helpers and structured logging.

## 🛠 Tech Stack
- **Language**: C# (.NET 6, 8, 9 targets)
- **Test frameworks**: xUnit (primary), with Moq as current default provider.
- **Key namespaces**: `FastMoq.Core`, `FastMoq.Web`, `FastMoq.Web.Blazor`
- **Patterns**: Constructor injection, provider abstraction, extension methods.

## 📂 Key Files & Concepts
- `Mocker.cs` – Core mock registry and creation logic.
- `MockerTestBase<T>` – Generic base for public types.
- `MockerTestBase` – Non‑generic base for internal/protected types.
- `MockerConstructionHelper` – Shared constructor resolution logic.
- `TestClassExtensions.cs` – Current verification helpers (e.g., `VerifyLogger`).
- `ScenarioBuilder<T>` – Fluent scenario API (Milestone 2).
- `IMockingProvider` – Provider abstraction (Milestone 1).

## 🧩 Coding Guidelines for Copilot
When generating code:
1. **Provider‑agnostic first**  
   - Use `IMockingProvider` methods for verification and mock creation.
   - Only use Moq APIs inside `MoqProvider` or Moq‑specific test code.
2. **Reuse shared helpers**  
   - For component creation, always call `MockerConstructionHelper.CreateInstance`.
   - For verification, follow the `VerifyLogger` pattern.
3. **Follow naming conventions**  
   - Public API methods: PascalCase.
   - Private fields: `_camelCase`.
   - Test methods: `MethodName_ShouldExpectedBehavior_WhenCondition`.
4. **Keep tests self‑contained**  
   - Use `Component` or `ComponentAs<T>()` from base classes.
   - Prefer `Scenario.With(Component)` for new tests.
5. **Document new APIs**  
   - Add XML doc comments for public methods.
   - Include usage examples in Milestone docs.

## 🚫 Avoid
- Hard‑coding Moq calls in shared/core code.
- Duplicating constructor resolution logic — always centralize in `MockerConstructionHelper`.
- Adding provider‑specific logic to `MockerTestBase` classes.
- Breaking existing public API signatures.

## 📚 Reference Examples
- See `FastMoq.Tests` for usage of `MockerTestBase<T>` and `VerifyLogger`.
- See `FastMoq.Tests.Web` for Blazor component testing patterns.
- See `FastMoq.Tests.Blazor` for UI‑specific injection and verification.

## 🧪 Testing & Validation
- All new features must have unit tests in the appropriate `FastMoq.Tests*` project.
- Run `dotnet test` before committing.
- Ensure tests pass for all target frameworks.

---

# ✅ FastMoq Test Authoring Guidelines

## 📚 Getting Started
Before writing tests, familiarize yourself with:
- `MockerTestBase<T>` - Your primary test base class
- `Mocks` property - Access to the mock registry
- `Component` property - Access to the class under test
- `VerifyLogger` extensions - For logging verification

## 🧪 Core Test Examples

### ✅ Example 1: Basic Test Structure
```csharp
public class CarServiceTests : MockerTestBase<CarService>
{
    [Fact]
    public void StartCar_ShouldReturnTrue_WhenEngineStarts()
    {
        // Arrange
        Mocks.GetMock<IEngine>()
            .Setup(x => x.Start())
            .Returns(true);

        // Act
        var result = Component.StartCar();

        // Assert
        result.Should().BeTrue();
        Mocks.GetMock<IEngine>().Verify(x => x.Start(), Times.Once);
    }
}
```

### ✅ Example 2: Logger Verification
```csharp
[Fact]
public void ProcessData_ShouldLogInformation_WhenSuccessful()
{
    // Arrange
    var data = "test data";

    // Act
    Component.ProcessData(data);

    // Assert
    Mocks.GetMock<ILogger>().VerifyLogger(LogLevel.Information, "Processing data");
}
```

### ✅ Example 3: Method Invocation with Parameters
```csharp
[Fact]
public void SaveEntity_ShouldCallRepository_WithCorrectParameters()
{
    // Arrange
    var entity = new TestEntity { Id = 123 };

    // Act
    Mocks.CallMethod(Component.SaveEntity, entity);

    // Assert
    Mocks.GetMock<IRepository>()
        .Verify(x => x.Save(It.Is<TestEntity>(e => e.Id == 123)), Times.Once);
}
```

### ✅ Example 4: Optional Parameters (Important!)
```csharp
// ✅ CORRECT - Include all parameters explicitly
Mocks.GetMock<IRepository>()
    .Setup(x => x.GetEntityAsync(It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
    .ReturnsAsync(entity);

// ❌ INCORRECT - Missing optional parameters will cause setup failures
Mocks.GetMock<IRepository>()
    .Setup(x => x.GetEntityAsync(It.IsAny<string>(), It.IsAny<string>(), false))
    .ReturnsAsync(entity);
```

## 🧱 Test Structure Best Practices

### Base Class Setup
```csharp
public class MyServiceTests : MockerTestBase<MyService>
{
    // Constructor for setup before component creation
    public MyServiceTests() : base(setupMocks: mocks =>
    {
        // Setup global mocks here
        mocks.GetMock<IGlobalService>().SetupAllProperties();
    })
    { }

    // Override for post-construction setup
    protected override Action<MyService> CreatedComponentAction => component =>
    {
        // Setup component after creation
        component.InitializeSettings();
    };
}
```

## 🎭 Mocking Rules & Patterns

### Core Rules
- **Always use `Mocks.GetMock<T>()`** to retrieve mocks
- **All interface dependencies are auto-mocked** by `MockerTestBase<T>`
- **Use `[Fact]` for xUnit tests** (primary framework)
- **Match method signatures exactly** in `Setup()` and `Verify()` calls
- **Only mock interfaces or virtual members** - not sealed classes

### Mock Access Patterns
```csharp
// Get or create a mock
var mock = Mocks.GetMock<IService>();

// Get mock with setup action
var mock = Mocks.GetMock<IService>(m => m.SetupAllProperties());

// Verify mock was used
Mocks.GetMock<IService>().Verify(x => x.DoSomething(), Times.Once);
```

## 📑 File Organization

### Naming Conventions
- **Test files**: `{ClassUnderTest}Tests.cs`
- **Test methods**: `MethodName_ShouldExpectedBehavior_WhenCondition`
- **Test classes**: Inherit from `MockerTestBase<T>`

### Using Statements
```csharp
using FastMoq;
using FastMoq.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
```

## 🔍 Advanced Verification Patterns

### Logger Verification (Multiple Overloads)
```csharp
// Simple message verification
Mocks.GetMock<ILogger>().VerifyLogger(LogLevel.Information, "message");

// With exception
Mocks.GetMock<ILogger>().VerifyLogger(LogLevel.Error, "error message", exception);

// With specific times
Mocks.GetMock<ILogger>().VerifyLogger(LogLevel.Warning, "warning", 2);

// Generic logger verification
Mocks.GetMock<ILogger<MyService>>().VerifyLogger(LogLevel.Debug, "debug info");
```

### Method Invocation Helpers
```csharp
// Call method with auto-parameter injection
Mocks.CallMethod(Component.ProcessOrder, orderId);

// Call async methods
await Mocks.CallMethod(Component.ProcessOrderAsync, orderId);

// Call with complex parameter matching
Mocks.CallMethod(Component.ValidateData, 
    data: testData, 
    validator: Mocks.GetMock<IValidator>().Object);
```

## 🧪 Test Coverage Guidelines

### What to Test
- **Public methods** - Always test public API
- **Internal methods** - Test critical internal logic when using non-generic `MockerTestBase`
- **Exception scenarios** - Test error handling and edge cases
- **Logging behavior** - Verify important log messages
- **Mock interactions** - Verify dependencies are called correctly

### Test Categories
```csharp
[Fact] // Standard unit test
[Theory] // Parameterized tests
[InlineData(1, "test")]
[InlineData(2, "other")]
public void Method_ShouldBehave_WithDifferentInputs(int id, string name)
{
    // Test implementation
}
```

## 💡 FastMoq-Specific Features

### Auto-Injection
```csharp
// Dependencies are automatically injected
public class OrderServiceTests : MockerTestBase<OrderService>
{
    [Fact]
    public void ProcessOrder_UsesInjectedRepository()
    {
        // IRepository is automatically mocked and injected
        Component.ProcessOrder(123);
        
        Mocks.GetMock<IRepository>()
            .Verify(x => x.GetOrder(123), Times.Once);
    }
}
```

### Resource Helpers
```csharp
// File system mocking
Mocks.AddFileSystemAbstractionMapping();

// Database context mocking  
var dbContextMock = Mocks.GetMockDbContext<TestDbContext>();
```

This ensures your tests follow FastMoq patterns and leverage the framework's auto-injection capabilities effectively.
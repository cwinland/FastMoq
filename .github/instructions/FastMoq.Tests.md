# FastMoq.Tests - Copilot Instructions

## 🧪 Test Writing Patterns

When working in `FastMoq.Tests`, you are creating the **quality assurance backbone** for the entire framework. Follow these established patterns:

### 📋 Test Structure
- **Inherit from `MockerTestBase<T>`**: Use the framework to test the framework
- **AAA Pattern**: Arrange, Act, Assert - keep tests clean and readable
- **One concept per test**: Focus each test method on a single behavior
- **Descriptive names**: `MethodName_ShouldExpectedBehavior_WhenCondition`

### 🎯 Testing Best Practices
```csharp
public class MyComponentTests : MockerTestBase<MyComponent>
{
    [Fact]
    public void GetData_ShouldReturnExpectedValue_WhenServiceReturnsValidData()
    {
        // Arrange
        var expectedData = "test data";
        Mocks.GetMock<IDataService>()
            .Setup(x => x.GetData())
            .Returns(expectedData);

        // Act
        var result = Component.GetData();

        // Assert
        result.Should().Be(expectedData);
        Mocks.GetMock<IDataService>().Verify(x => x.GetData(), Times.Once);
    }
}
```

### 🔍 Verification Patterns
- **Use FluentAssertions**: `.Should().Be()`, `.Should().NotBeNull()`, etc.
- **Verify mock interactions**: Always verify expected calls were made
- **Test edge cases**: Null inputs, empty collections, exception scenarios
- **Use `VerifyLogger` pattern**: For testing logging behavior

### 📦 Test Organization
- **Group related tests**: Use nested classes for logical grouping
- **Test all constructors**: Especially important for `MockerTestBase` variations
- **Cover extension methods**: Test all public extension methods thoroughly
- **Integration tests**: Test component interactions, not just units

### 🎭 Mock Setup Patterns
```csharp
// Simple mock setup
public MyTests() : base(mocks => 
{
    mocks.GetMock<IService>()
        .Setup(x => x.Process(It.IsAny<string>()))
        .Returns("processed");
})

// Complex setup with multiple services
protected override Action<Mocker> SetupMocksAction => mocks =>
{
    mocks.AddType(_ => mocks.GetMockDbContext<TestDbContext>().Object);
    mocks.GetMock<ILogger<MyComponent>>()
        .Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()));
};
```

### 🚫 Testing Anti-Patterns
- **Don't test implementation details**: Focus on behavior, not internal mechanics
- **Avoid over-mocking**: Only mock external dependencies
- **No hardcoded values**: Use meaningful test data
- **Don't ignore async**: Properly test async methods with `.ConfigureAwait(false)`

### 📊 Coverage Goals
- All public APIs must have tests
- Critical paths need multiple test scenarios
- Error conditions should be tested
- Constructor variations need coverage
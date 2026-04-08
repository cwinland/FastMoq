# FastMoq Feature Parity & Comparison

This page is for comparison, not step-by-step setup. Use it to see where FastMoq removes boilerplate compared with using a mock provider directly, then follow the focused guides for implementation details.

## Framework Comparison Overview

| Framework | Auto-Injection | Fluent Setup | DbContext Support | Web Testing | Learning Curve |
| --- | --- | --- | --- | --- | --- |
| **FastMoq** | ✅ Full | ✅ Yes | ✅ Built-in | ✅ Blazor/MVC | 🟢 Easy |
| **Moq** | ❌ Manual | ✅ Yes | 🟡 Manual | 🟡 Manual | 🟡 Medium |
| **NSubstitute** | ❌ Manual | ✅ Yes | 🟡 Manual | 🟡 Manual | 🟢 Easy |
| **FakeItEasy** | ❌ Manual | ✅ Yes | 🟡 Manual | 🟡 Manual | 🟢 Easy |

## Detailed Feature Comparison

### 1. Mock Creation and Management

#### FastMoq Mock Creation

```csharp
public class ServiceTests : MockerTestBase<MyService>
{
    [Fact]
    public void Test_AutomaticMocks()
    {
        Component.SomeMethod();

        Mocks.Verify<IDependency>(x => x.Method(), TimesSpec.Once);
    }
}
```

#### Direct Provider Mock Creation

```csharp
public class ServiceTests
{
    private readonly Mock<IDependency1> _dependency1Mock;
    private readonly Mock<IDependency2> _dependency2Mock;
    private readonly Mock<ILogger<MyService>> _loggerMock;
    private readonly MyService _service;

    public ServiceTests()
    {
        _dependency1Mock = new Mock<IDependency1>();
        _dependency2Mock = new Mock<IDependency2>();
        _loggerMock = new Mock<ILogger<MyService>>();
        _service = new MyService(_dependency1Mock.Object, _dependency2Mock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Test_ManualMocks()
    {
        _service.SomeMethod();
        _dependency1Mock.Verify(x => x.Method(), Times.Once);
    }
}
```

FastMoq removes most constructor wiring and repeated mock declarations from the test class.

### 2. Constructor Parameter Testing

#### FastMoq Constructor Testing

```csharp
[Fact]
public void Constructor_ShouldValidateParameters()
{
    TestAllConstructorParameters((action, constructor, parameter) =>
        action.EnsureNullCheckThrown(parameter, constructor));
}
```

#### Direct Provider Constructor Testing

```csharp
[Fact]
public void Constructor_ShouldThrowWhenDependency1IsNull()
{
    Assert.Throws<ArgumentNullException>(() =>
        new MyService(null, Mock.Of<IDependency2>(), Mock.Of<ILogger<MyService>>()));
}

[Fact]
public void Constructor_ShouldThrowWhenDependency2IsNull()
{
    Assert.Throws<ArgumentNullException>(() =>
        new MyService(Mock.Of<IDependency1>(), null, Mock.Of<ILogger<MyService>>()));
}
```

FastMoq turns repeated guard-clause tests into one helper-driven assertion path.

### 3. DbContext Mocking

#### FastMoq DbContext Testing

```csharp
public class BlogServiceTests : MockerTestBase<BlogService>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        var dbContextMock = mocker.GetMockDbContext<BlogContext>();
        mocker.AddType(_ => dbContextMock.Object);
    };

    [Fact]
    public void GetBlog_ShouldReturnBlog()
    {
        var dbContext = Mocks.GetRequiredObject<BlogContext>();
        dbContext.Blogs.Add(new Blog { Id = 1, Title = "Test" });

        var result = Component.GetBlog(1);

        result.Should().NotBeNull();
    }
}
```

#### Direct Provider DbContext Testing

```csharp
public class BlogServiceTests
{
    private readonly Mock<BlogContext> _contextMock;
    private readonly Mock<DbSet<Blog>> _blogSetMock;
    private readonly BlogService _service;

    public BlogServiceTests()
    {
        var blogs = new List<Blog> { new Blog { Id = 1, Title = "Test" } }.AsQueryable();

        _blogSetMock = new Mock<DbSet<Blog>>();
        _blogSetMock.As<IQueryable<Blog>>().Setup(m => m.Provider).Returns(blogs.Provider);
        _blogSetMock.As<IQueryable<Blog>>().Setup(m => m.Expression).Returns(blogs.Expression);
        _blogSetMock.As<IQueryable<Blog>>().Setup(m => m.ElementType).Returns(blogs.ElementType);
        _blogSetMock.As<IQueryable<Blog>>().Setup(m => m.GetEnumerator()).Returns(blogs.GetEnumerator());

        _contextMock = new Mock<BlogContext>();
        _contextMock.Setup(c => c.Blogs).Returns(_blogSetMock.Object);

        _service = new BlogService(_contextMock.Object);
    }
}
```

FastMoq keeps DbContext setup inside the same mock registry and lifecycle as the rest of the test.

### 4. Web and Blazor Testing

#### FastMoq Blazor Testing

```csharp
public class CounterComponentTests : MockerBlazorTestBase<Counter>
{
    [Fact]
    public void Counter_ShouldIncrement_WhenButtonClicked()
    {
        Component.Find("button").Click();

        Component.Find("p").TextContent.Should().Contain("Current count: 1");
    }
}
```

#### Direct Provider Blazor Testing

```csharp
public class CounterComponentTests : BunitContext
{
    [Fact]
    public void Counter_ShouldIncrement_WhenButtonClicked()
    {
        var component = Render<Counter>();

        component.Find("button").Click();

        component.Find("p").TextContent.Should().Contain("Current count: 1");
    }
}
```

FastMoq adds mock registry support on top of the normal Blazor component test workflow.

### 5. Method Parameter Auto-Injection

`CallMethod(...)` is useful when the method under test takes several collaborators as parameters instead of only constructor dependencies. If you supply the business argument that matters to the test, FastMoq can fill the omitted parameters when they are mocks or other types FastMoq can resolve through its normal creation pipeline.

#### FastMoq Method Invocation

```csharp
[Fact]
public void ProcessData_ShouldReturnData_WhenCollaboratorsAreAutoInjected()
{
    var result = Mocks.CallMethod<string>(Component.ProcessData, "specificValue");

    result.Should().NotBeNull();
}
```

#### Direct Provider Method Invocation

```csharp
[Fact]
public void ProcessData_ShouldReturnData()
{
    var dependency1 = Mock.Of<IDependency1>();
    var dependency2 = Mock.Of<IDependency2>();
    var logger = Mock.Of<ILogger>();

    var result = _service.ProcessData("specificValue", dependency1, dependency2, logger);

    result.Should().NotBeNull();
}
```

FastMoq can fill the rest of the method signature automatically when the omitted parameters are mocks or mockable dependencies, while still letting you override the parameters that matter to the test.

## Code Reduction Snapshot

FastMoq is strongest when you want less test code spent on constructor wiring, dependency registration, and repeated setup. Direct provider usage stays viable, but it usually pushes more object graph assembly into each test class.

### FastMoq Example

```csharp
public class OrderServiceTests : MockerTestBase<OrderService>
{
    [Fact]
    public async Task ProcessOrder_ShouldCompleteOrder()
    {
        Mocks.GetOrCreateMock<IPaymentService>()
            .Setup(x => x.ProcessPayment(It.IsAny<decimal>()))
            .ReturnsAsync(true);

        var result = await Component.ProcessOrderAsync(100m);

        result.Should().BeTrue();
    }
}
```

### Direct Provider Example

```csharp
public class OrderServiceTests
{
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<ILogger<OrderService>> _loggerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _paymentServiceMock = new Mock<IPaymentService>();
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _loggerMock = new Mock<ILogger<OrderService>>();
        _emailServiceMock = new Mock<IEmailService>();

        _orderService = new OrderService(
            _paymentServiceMock.Object,
            _orderRepositoryMock.Object,
            _loggerMock.Object,
            _emailServiceMock.Object);
    }
}
```

The main value is not a special assertion syntax. It is keeping the test focused on behavior instead of mock construction and repeated dependency plumbing.

## FastMoq-Specific Capabilities

- Built-in known-type mappings for common framework abstractions.
- Constructor guard testing helpers.
- `CallMethod(...)` auto-parameter resolution.
- Blazor test base integration.
- `ScenarioBuilder` support that still works across providers because verification stays provider-neutral.

## Where To Go Next

- [Getting Started](../getting-started/README.md)
- [Testing Guide](../getting-started/testing-guide.md)
- [Migration Guide](../migration/README.md)
- [Samples](../samples/README.md)

# FastMoq Feature Parity & Framework Comparison

This page is a comparison aid, not a setup guide. Use it to evaluate where FastMoq changes the authoring model compared with using a mocking framework directly, then follow the focused guides when you are ready to implement a specific test shape.

FastMoq is not trying to erase every provider-specific behavior. Its main value is a provider-first layer for object creation, dependency injection, verification, and framework-heavy test helpers so tests spend less effort on repeated wiring and more effort on behavior.

## How To Read This Page

Legend:

- `✅`: built-in and documented as a first-class path in the framework being compared.
- `🟡`: supported, but with important constraints such as provider dependence, manual setup, or lack of one unified abstraction.
- `❌`: not supported as a built-in capability in the compared scope.

Reading notes:

- For FastMoq, `🟡` often means the capability is still available through the selected provider, but FastMoq does not pretend it is fully portable across every provider.
- For direct providers, `🟡` is reserved for native-framework capabilities that are genuinely present but come with notable constraints.
- The direct-provider columns score the native framework itself, not third-party add-on packages that layer auto-mocking, DI-aware construction, or extra framework helpers on top of that provider.
- FakeItEasy appears here only as an external direct-framework baseline. The current FastMoq repo does not ship a FastMoq provider for FakeItEasy, so this page does not show FastMoq-plus-FakeItEasy implementation examples.

## Framework Comparison Overview

### Construction And Authoring Model

| Capability | FastMoq | Moq | NSubstitute | FakeItEasy |
| --- | --- | --- | --- | --- |
| Multiple provider backends behind one test API | ✅ | ❌ | ❌ | ❌ |
| Explicit provider selection and scoped overrides | ✅ | ❌ | ❌ | ❌ |
| Automatic dependency graph construction | ✅ | ❌ | ❌ | ❌ |
| DI-aware component creation | ✅ | ❌ | ❌ | ❌ |
| Constructor guard helpers | ✅ | ❌ | ❌ | ❌ |
| Method parameter auto-injection | ✅ | ❌ | ❌ | ❌ |
| Native framework setup syntax | 🟡 (1) | ✅ | ✅ | ✅ |
| Portable argument-matching surface | ✅ | ❌ | ❌ | ❌ |

Construction notes:

- `(1)` for FastMoq means the test can still use native provider setup syntax, but that setup remains provider-specific rather than fully portable across every FastMoq provider.

### Verification, Framework Integration, And Tooling

| Capability | FastMoq | Moq | NSubstitute | FakeItEasy |
| --- | --- | --- | --- | --- |
| Portable verification surface across providers | ✅ | ❌ | ❌ | ❌ |
| Native framework verification syntax | 🟡 (2) | ✅ | ✅ | ✅ |
| Async arrangement for `Task`-returning members | ✅ | ✅ | ✅ | ✅ |
| Exception arrangement | 🟡 (3) | ✅ | ✅ | ✅ |
| DbContext-oriented helper layer | ✅ | ❌ | ❌ | ❌ |
| Typed `IServiceProvider` and scope helper layer | ✅ | ❌ | ❌ | ❌ |
| `HttpClient` request helper layer | ✅ | ❌ | ❌ | ❌ |
| ASP.NET Core controller and HttpContext helper layer | ✅ | ❌ | ❌ | ❌ |
| Blazor and bUnit integration helper layer | ✅ | ❌ | ❌ | ❌ |
| Azure SDK credential, pageable, and client-registration helper layer | ✅ | ❌ | ❌ | ❌ |
| Azure Functions worker and HTTP-trigger helper layer | ✅ | ❌ | ❌ | ❌ |
| Analyzer-guided modernization in this repo | ✅ | ❌ | ❌ | ❌ |

Verification and integration notes:

- `(2)` for FastMoq verification syntax means provider-native verification APIs can still be used when a test intentionally stays provider-specific, but FastMoq's primary first-party path is the provider-neutral `Verify(...)` and `VerifyNoOtherCalls(...)` surface, plus `VerifyLogged(...)` when the selected provider supports logger capture.
- `(3)` for FastMoq exception arrangement means the behavior is available, but the arrange step may still rely on the selected provider's native exception setup syntax rather than one shared FastMoq helper.

## What The Matrix Means In Practice

The table is intentionally focused on current validated capability coverage, not on future wish lists.

- FastMoq is strongest where test construction, provider-neutral verification, framework helpers, and migration tooling matter more than using raw provider syntax everywhere.
- Direct providers remain strong when a suite intentionally stays native to one framework and the team is comfortable owning constructor wiring, registration setup, and provider-specific helper code directly in the tests.
- If a workflow becomes possible only after adding ecosystem packages around a provider, that is outside the scope of these direct-framework cells and should be documented as a separate package-stack comparison instead.
- That means rows explicitly described as a helper layer stay `❌` for direct providers unless the provider itself ships that helper surface natively.
- Core helper rows include FastMoq's typed `IServiceProvider` and scope helpers plus the `WhenHttpRequest(...)` and `WhenHttpRequestJson(...)` helper flow for outbound HTTP behavior. In FastMoq, that `HttpClient` helper path stays provider-neutral.
- Azure-specific rows refer to the documented first-party FastMoq packages `FastMoq.Azure` and `FastMoq.AzureFunctions`, including helpers for credentials, pageable builders, Azure-oriented DI, client registration, `FunctionContext.InstanceServices`, and HTTP-trigger request or response objects.
- Some advanced areas remain intentionally provider-dependent today, especially when the semantics are tightly coupled to one provider's model. Those scenarios are better explained in focused notes than flattened into misleading chart cells.

## Detailed Comparisons

### 1. Object Graph Wiring And Everyday Setup

This is the most common FastMoq value proposition: the component is created for you, tracked dependencies live in one registry, and the assert side can stay provider-neutral even when the arrange side uses provider-native syntax.

#### FastMoq Example

```csharp
public class OrderServiceTests : MockerTestBase<OrderService>
{
    [Fact]
    public async Task ProcessOrderAsync_ShouldSaveOrder_WhenPaymentSucceeds()
    {
        Mocks.GetOrCreateMock<IPaymentService>()
            .Setup(x => x.ProcessPayment(100m))
            .ReturnsAsync(true);

        var result = await Component.ProcessOrderAsync(100m);

        result.Should().BeTrue();
        Mocks.Verify<IOrderRepository>(x => x.SaveOrder(100m), TimesSpec.Once);
        Mocks.Verify<IEmailService>(x => x.SendReceipt(100m), TimesSpec.Once);
    }
}
```

#### Direct Moq Example

```csharp
public class OrderServiceTests
{
    private readonly Mock<IPaymentService> _paymentServiceMock = new();
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<ILogger<OrderService>> _loggerMock = new();
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _service = new OrderService(
            _paymentServiceMock.Object,
            _orderRepositoryMock.Object,
            _loggerMock.Object,
            _emailServiceMock.Object);
    }

    [Fact]
    public async Task ProcessOrderAsync_ShouldSaveOrder_WhenPaymentSucceeds()
    {
        _paymentServiceMock
            .Setup(x => x.ProcessPayment(100m))
            .ReturnsAsync(true);

        var result = await _service.ProcessOrderAsync(100m);

        result.Should().BeTrue();
        _orderRepositoryMock.Verify(x => x.SaveOrder(100m), Times.Once);
        _emailServiceMock.Verify(x => x.SendReceipt(100m), Times.Once);
    }
}
```

The test goal is the same in both versions. The difference is where the ceremony lives: direct provider usage spells out every collaborator and subject-construction step, while FastMoq keeps the object graph inside the harness and lets the test focus on the behavior under assertion.

Provider note:

- The FastMoq arrange step above assumes the Moq provider extensions are available.
- When the selected provider is NSubstitute, keep the same component creation and provider-neutral verify calls, then translate the arrange step into native substitute syntax:

```csharp
using var providerScope = MockingProviderRegistry.Push("nsubstitute");

Mocks.GetOrCreateMock<IPaymentService>()
    .AsNSubstitute()
    .ProcessPayment(100m)
    .Returns(Task.FromResult(true));
```

That is the main provider-specific rule on this page: FastMoq can unify the test harness and verify layer, but native arrange syntax still follows the selected provider when the test depends on provider-specific setup APIs.

### 2. Constructor And Guard Testing

FastMoq has first-party helpers for constructor guard coverage. Direct provider usage can still test the same behavior, but it usually turns into one test per parameter or one hand-written helper per constructor shape.

#### FastMoq Guard Helper

```csharp
[Fact]
public void Constructor_ShouldValidateParameters()
{
    TestAllConstructorParameters((action, constructor, parameter) =>
        action.EnsureNullCheckThrown(parameter, constructor));
}
```

#### Direct Provider Guard Tests

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

The direct-provider path is not wrong. It is just repetitive. FastMoq turns repeated guard-clause coverage into one helper-driven test path that follows the same constructor-selection logic as the rest of the harness.

### 3. DbContext Workflows

DbContext testing is where the difference between a helper layer and a direct provider becomes easy to see. FastMoq keeps the DbContext in the same tracked registry as the rest of the test doubles. Direct provider usage usually means building a mock DbContext plus every `DbSet<T>` behavior the query needs.

#### FastMoq DbContext Path

```csharp
public class BlogServiceTests : MockerTestBase<BlogService>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        _ = mocker.GetMockDbContext<BlogContext>();
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

#### Direct Moq DbContext Path

```csharp
[Fact]
public void GetBlog_ShouldReturnBlog()
{
    var blogs = new List<Blog>
    {
        new() { Id = 1, Title = "Test" },
    }.AsQueryable();

    var blogSetMock = new Mock<DbSet<Blog>>();
    blogSetMock.As<IQueryable<Blog>>().Setup(x => x.Provider).Returns(blogs.Provider);
    blogSetMock.As<IQueryable<Blog>>().Setup(x => x.Expression).Returns(blogs.Expression);
    blogSetMock.As<IQueryable<Blog>>().Setup(x => x.ElementType).Returns(blogs.ElementType);
    blogSetMock.As<IQueryable<Blog>>().Setup(x => x.GetEnumerator()).Returns(blogs.GetEnumerator());

    var contextMock = new Mock<BlogContext>();
    contextMock.Setup(x => x.Blogs).Returns(blogSetMock.Object);

    var service = new BlogService(contextMock.Object);

    var result = service.GetBlog(1);

    result.Should().NotBeNull();
}
```

The direct-provider example is complete on purpose because this is one of the places where incomplete comparison snippets are misleading. The point is not that direct providers cannot test `DbContext`. They can. The point is that FastMoq gives that workflow a first-party path inside the same mock registry as the rest of the test.

### 4. Web And Blazor Testing

FastMoq does not replace bUnit or ASP.NET test primitives. It layers tracked mocks and web-focused helpers on top of them.

#### FastMoq Blazor Path

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

#### Direct Provider Blazor Path

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

For a simple component interaction, both paths are straightforward. The FastMoq-specific value shows up when the same test also needs tracked collaborators, typed `IServiceProvider` helpers, `HttpContext`, claims, or other web helpers that would otherwise be stitched together manually around the component test.

### 5. Method Parameter Auto-Injection

`CallMethod(...)` matters when the method under test takes several collaborators as parameters instead of only constructor dependencies. FastMoq can fill omitted mockable parameters while still letting the test override the business argument that matters.

#### FastMoq CallMethod Path

```csharp
[Fact]
public void ProcessData_ShouldReturnData_WhenCollaboratorsAreAutoInjected()
{
    var result = Mocks.CallMethod<string>(Component.ProcessData, "specificValue");

    result.Should().NotBeNull();
}
```

#### Direct Provider Call Path

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

This is not a universal testing need, but when it shows up it is one of the more distinctive FastMoq capabilities because the harness can reuse the same resolution pipeline for method parameters that it already uses for constructor injection.

## What Still Stays Provider-Dependent

Some capabilities are intentionally not flattened into one FastMoq abstraction today.

- Protected-member interception, call-base behavior, and similar class-mocking semantics still depend on the selected provider.
- Ordered-call, sequence-heavy, or event-centric scenarios are often clearer when documented in provider-native terms instead of forcing them into a single portable surface.
- Exception arrangement can still require native provider syntax even when the component creation and verification layers stay on FastMoq.

That is why the matrix uses `🟡` in a few places where a direct provider would say `✅`. The capability may still be available in a FastMoq-based suite, but the suite reaches it through the selected provider rather than through one provider-neutral helper.

For follow-up guidance on these provider-dependent gaps, see [Provider Capabilities](../getting-started/provider-capabilities.md) and the roadmap notes in [Roadmap](../roadmap/README.md).

## Why Teams Still Choose FastMoq

- The harness can create the subject under test and track the dependency graph automatically.
- Verification can stay provider-neutral even when setup remains provider-specific.
- DbContext, Blazor, `HttpContext`, typed `IServiceProvider`, and other framework-heavy scenarios can stay inside the same FastMoq test surface.
- Outbound `HttpClient` behavior can stay on the provider-neutral `WhenHttpRequest(...)` and `WhenHttpRequestJson(...)` helper path instead of dropping directly into provider-specific handler interception for every test.
- Azure SDK and Azure Functions test helpers can stay inside the same FastMoq package family instead of requiring a separate local helper stack.
- Analyzer guidance helps modernize older suites toward current provider-first usage instead of leaving every team to rediscover the same migration decisions.

## Where To Go Next

- [Getting Started](../getting-started/README.md)
- [Provider Selection](../getting-started/provider-selection.md)
- [Provider Capabilities](../getting-started/provider-capabilities.md)
- [Testing Guide](../getting-started/testing-guide.md)
- [Migration Guide](../migration/README.md)
- [Roadmap](../roadmap/README.md)
- [Samples](../samples/README.md)

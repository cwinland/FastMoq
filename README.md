# [FastMoq](http://help.fastmoq.com/)

FastMoq is a .NET testing framework for auto-mocking, dependency injection, and test-focused object creation. In v4, it supports a provider-first architecture with a bundled reflection default and optional Moq compatibility when you need it.

## 📚 Documentation

### Quick Links

- **🚀 [Getting Started Guide](./docs/getting-started)** - Your first FastMoq test in 5 minutes
- **🧪 [Testing Guide](./docs/getting-started/testing-guide.md)** - Repo-native guidance for `GetMock<T>()`, `AddType(...)`, `DbContext`, `IFileSystem`, and known types
- **🔌 [Provider Selection Guide](./docs/getting-started/provider-selection.md)** - How to register, select, and bootstrap providers for a test assembly
- **👨‍🍳 [Cookbook](./docs/cookbook)** - Real-world patterns and recipes
- **🏗️ [Sample Applications](./docs/samples)** - Complete examples with Azure integration
- **🧪 [Executable Testing Examples](./docs/samples/testing-examples.md)** - Repo-backed service examples using the current FastMoq API direction
- **📊 [Feature Comparison](./docs/feature-parity)** - FastMoq vs Moq/NSubstitute
- **📈 [Performance Benchmarks](./docs/benchmarks)** - Productivity and performance metrics

### Additional Resources

- **📖 [Complete Documentation](./docs)** - All guides and references in one place
- **🗺️ [Roadmap Notes](./docs/roadmap)** - Current provider-first direction and deferred backlog items
- **🆕 [What's New Since 3.0.0](./docs/whats-new)** - Summary of the v4 release line relative to the last public `3.0.0` package
- **⚠️ [Breaking Changes](./docs/breaking-changes)** - Intentional v4 behavior changes relative to the `3.0.0` public release
- **🔄 [Migration Guide](./docs/migration)** - Practical old-to-new guidance from `3.0.0` to the current repo direction
- **❓ [FAQs](./FAQs.md)** - Frequently asked questions and troubleshooting
- **🔗 [API Documentation](https://help.fastmoq.com/)** - Generated HTML API reference

## Features

- NOW BLAZOR SUPPORT in FastMoq and FastMoq.Web.
- Test without declaring Mocks (unless needed).
- Creates objects with chain of automatic injections in objects and their dependencies.
- Creates Mocks and Objects with properties populated.
- Automatically injects and creates components or services.
- Injection: Automatically determines what interfaces need to be injected into the constructor and creates mocks if they do not exist.
  - Generate Mock using specific data.
  - Best guess picks the multiple parameter constructor over the default constructor.
  - Specific mapping allows the tester to create an instance using a specific constructor and specific data.
  - Supports Inject Attributes and multiple constructors.
- Use Mocks without managing fields and properties. Mocks are managed by the Mocker framework. No need to keep track of Mocks. Just use them!
- Create instances of Mocks with non public constructors.
- HttpClient and IFileSystem test helpers
- DbContext support through the optional `FastMoq.Database` package, with the primary calls staying in the `FastMoq` namespace.
- Supports Null method parameter testing.
- **Comprehensive Documentation** - Complete guides, samples, and real-world patterns.

## Packages

- FastMoq - Aggregate package that combines FastMoq.Core, FastMoq.Database, and FastMoq.Web.
- FastMoq.Core - Core testing Mocker and provider-first resolution pipeline.
- FastMoq.Database - Entity Framework and DbContext-focused helpers.
- FastMoq.Web - Blazor and Web support.

In the current v4 layout, `FastMoq.Core` bundles the internal `reflection` provider and the bundled `moq` compatibility provider. The default provider is `reflection`. Additional providers such as `nsubstitute` can be added explicitly.

Typical split-package install:

```bash
dotnet add package FastMoq.Core
dotnet add package FastMoq.Database
dotnet add package FastMoq.Web
```

`GetMockDbContext<TContext>()` keeps the same main call shape in the `FastMoq` namespace. If you install `FastMoq`, the EF helpers are included. If you install `FastMoq.Core` directly, add `FastMoq.Database` for DbContext support.

`GetMockDbContext<TContext>()` remains the default mocked-sets entry point. For explicit mode selection between mocked DbSets and a real EF in-memory context, use `GetDbContextHandle<TContext>(new DbContextHandleOptions<TContext> { ... })`.

The mocked-sets path is still backed by the existing Moq-based `DbContextMock<TContext>` implementation, while the real-context path is exposed through `DbContextTestMode.RealInMemory`.

If you are upgrading an older suite that still uses `GetMock<T>()`, direct `Mock<T>` access, or `VerifyLogger(...)`, select Moq explicitly for that test assembly. If you are writing new or actively refactoring tests, prefer provider-neutral APIs such as `GetOrCreateMock(...)`, `Verify(...)`, and `VerifyLogged(...)`.

Provider selection example:

```csharp
MockingProviderRegistry.Register("moq", MoqMockingProvider.Instance, setAsDefault: true);
var mocker = new Mocker();
```

For a temporary override in a specific async scope, use `MockingProviderRegistry.Push("providerName")`. For detailed setup guidance, see [Provider Selection Guide](./docs/getting-started/provider-selection.md).

## Targets

- .NET 9
- .NET 8
- .NET 10

## Most used classes in the FastMoq namespace

```cs
public class Mocker {} // Primary class for auto mock and injection. This can be used standalone from MockerTestBase using Mocks property on the base class.
public abstract class MockerTestBase<TComponent> where TComponent : class {} // Assists in the creation of objects and provides direct access to Mocker.
```

## Most used classes in the FastMoq.Web.Blazor namespace

```cs
public abstract class MockerBlazorTestBase<T> : TestContext, IMockerBlazorTestHelpers<T> where T : ComponentBase // Assists in the creation of Blazor components and provides direct access to Mocker.
```

## Examples

- [API reference landing page](./docs/api/index.md)
- [Executable testing examples in this repo](./docs/samples/testing-examples.md)

### Real-world example: order processing service

```cs
public class OrderProcessingServiceExamples : MockerTestBase<OrderProcessingService>
{
    [Fact]
    public async Task PlaceOrderAsync_ShouldPersistAndLog_WhenReservationAndPaymentSucceed()
    {
        var request = new OrderRequest
        {
            CustomerId = "cust-42",
            Sku = "SKU-RED-CHAIR",
            Quantity = 2,
            TotalAmount = 149.90m,
        };

        Mocks.GetMock<IInventoryGateway>()
            .Setup(x => x.ReserveAsync(request.Sku, request.Quantity, CancellationToken.None))
            .ReturnsAsync(true);

        Mocks.GetMock<IPaymentGateway>()
            .Setup(x => x.ChargeAsync(request.CustomerId, request.TotalAmount, CancellationToken.None))
            .ReturnsAsync("pay_12345");

        var result = await Component.PlaceOrderAsync(request, CancellationToken.None);

        result.Success.Should().BeTrue();
        Mocks.GetMock<IOrderRepository>()
            .Verify(x => x.SaveAsync(It.IsAny<OrderRecord>(), CancellationToken.None), Times.Once);
        Mocks.VerifyLogged(LogLevel.Information, "Placed order", 1);
    }
}
```

`Mocks.VerifyLogged(...)` is the provider-safe logger assertion API. It verifies captured `ILogger` entries through the active provider contract instead of depending on a provider-specific mock surface. `VerifyLogger(...)` remains available only as a Moq compatibility helper during the v4 transition.

### Real-world example: fluent scenario style

```cs
Scenario
    .With(() =>
    {
        Mocks.GetMock<IInvoiceRepository>()
            .Setup(x => x.GetPastDueAsync(now, CancellationToken.None))
            .ReturnsAsync(invoices);
    })
    .When(async () => reminderCount = await Component.SendRemindersAsync(now, CancellationToken.None))
    .Then(() => reminderCount.Should().Be(2))
    .Verify<IInvoiceRepository>(x => x.GetPastDueAsync(now, CancellationToken.None), TimesSpec.Once)
    .Verify<IEmailGateway>(x => x.SendReminderAsync("ap@contoso.test", 125m, CancellationToken.None), TimesSpec.Once)
    .Execute();
```

`TimesSpec` supports `TimesSpec.Once`, `TimesSpec.Exactly(count)`, `TimesSpec.AtLeast(count)`, `TimesSpec.AtMost(count)`, and `TimesSpec.Never()`.

For expected-failure scenarios, use `WhenThrows<TException>(...)` when `Then(...)` assertions should still run, or `ExecuteThrows<TException>()` when the exception object itself is the main assertion target.

For more current repo-backed examples, see [Executable Testing Examples](./docs/samples/testing-examples.md) and [Migration Guide: 3.0.0 To Current Repo](./docs/migration/README.md).

### Auto Injection

Auto injection allows creation of components with parameterized interfaces. If an override for creating the component is not specified, the component will be created will the default Mock Objects.

#### Auto Injection with instance parameters

Additionally, the creation can be overwritten and provided with instances of the parameters. CreateInstance will automatically match the correct constructor to the parameters given to CreateInstance.

```cs
Mocks.CreateInstance(new MockFileSystem()); // CreateInstance matches the parameters and types with the Component constructor.
```

#### Interface Type Map

When multiple classes derive from the same interface, the Interface Type Map can map with class to use for the given injected interface.
The map can also enable mock substitution.

##### Example of two classes inheriting the same interface

```cs
public class TestClassDouble1 : ITestClassDouble {}
public class TestClassDouble2 : ITestClassDouble {}
```

##### Mapping

```AddType``` is a Mocker method like the Dependency Injection method AddSingleton. The Mocker class will track the Mock as a single instance.
```AddType``` must be specified in SetupMocksAction in order to prevent the Mock from creating the default.
This is only needed to inject Mocks when:

- Mock Configuration is needed
- A specific instance is needed for an interface
- Custom parameters or non-public constructors are required.

This code maps the interfaces to the concrete class.

```cs
protected override Action<Mocker> SetupMocksAction => m =>
{
    m.AddType<ILogger, Logger<NullLogger>>();
    m.AddType<IAzResourceService, AzResourceService>();
};
```

The map also accepts parameters to tell it how to create the instance.

```cs
m.AddType<IAzResourceService, AzResourceService>(() => new AzResourceService("test", 456));
```

### DbContext Mocking

If you consume `FastMoq.Core` directly, add `FastMoq.Database` before using the DbContext helpers. If you consume the aggregate `FastMoq` package, the DbContext helpers are already included.

`GetMockDbContext<TContext>()` remains the default pure-mock entry point. For explicit mode selection between mocked DbSets and a real EF in-memory context, use `GetDbContextHandle<TContext>(new DbContextHandleOptions<TContext> { ... })`.

Create Your DbContext. *The DbContext can either have virtual DbSet(s) or an interface. In this example, we use virtual DbSets.*

Suppose you have an ApplicationDbContext with a ```virtual DbSet<Blog>```:

```cs
public class ApplicationDbContext : DbContext
{
    public virtual DbSet<Blog> Blogs { get; set; }
    // ...other DbSet properties

    // This can be public, but we make it internal to hide it for testing only.
    // This is only required if the public constructor does things that we don't want to do.
    internal ApplicationDbContext()
    {
        // In order for an internal to work, you may need to add InternalsVisibleTo attribute in the AssemblyInfo or project to allow the mocker to see the internals.
        // [assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
    }

    // Public constructor used by application.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) => Database.EnsureCreated();

    // ... Initialize and setup
}
```

Suppose you want to test a repository class that uses the ApplicationDbContext:

```cs
public class BlogRepo(ApplicationDbContext dbContext)
{
    public Blog? GetBlogById(int id) => dbContext.Blogs.AsEnumerable().FirstOrDefault(x => x.Id == id);
}
```

#### Sample Unit Test Using DbContext in MockerTestBase

In your unit test, you want to override the SetupMocksAction to create a mock DbContext using Mocker.
Here’s an example using xUnit:

```cs
public class BlogRepoTests : MockerTestBase<BlogRepo>
{
    // This runs actions before BlogRepo is created.
    // This cannot be done in the constructor because the component is created in the base class.
    // An alternative way is to pass the code to the base constructor.
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        var dbContextMock = mocker.GetMockDbContext<ApplicationDbContext>(); // Create DbContextMock
        mocker.AddType(_ => dbContextMock.Object); // Add DbContext to the Type Map.
        // In this example, we don't use a IDbContextFactory, but if we did, it would do this instead of AddType:
        // mocker.GetMock<IDbContextFactory<ApplicationDbContext>>().Setup(x => x.CreateDbContext()).Returns(dbContextMock.Object);
    };

    [Fact]
    public void GetBlog_ShouldReturnBlog_WhenPassedId()
    {
        // Arrange
        const int ID = 1234;
        var blogsTestData = new List<Blog> { new() { Id = ID } };

        // Create a mock DbContext
        var dbContext = Mocks.GetRequiredObject<ApplicationDbContext>();
        dbContext.Blogs.AddRange(blogsTestData); // Can also be dbContext.Set<Blog>().AddRange(blogsTestData)

        // Act
        var result = Component.GetBlogById(ID);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id.Equals(ID));
    }
}
```

*Run Your Tests*: Execute your unit tests, and the mock DbContext will behave like a real DbContext, allowing you to test your BlogRepo logic without hitting the actual database.

#### Sample Unit Test Using DbContext Directly In Test Methods

In your unit test, you want to create a mock DbContext using Mocker.
This can be done directly in the test if the context is either only needed for the method or the component is created manually.
Here’s an example using xUnit:

```cs
public class BtnValidatorTests
{
    [Fact]
    public void BtnValidatorIsValid_ShouldReturnTrue()
    {
        // Arrange
        var id = 1234;
        var blogsTestData = new List<Blog> { new Blog { Id = id } };

        // Create a mock DbContext
        var mocker = new Mocker(); // Create a new mocker only if not already using MockerTestBase; otherwise use Mocks included in the base class.

        var dbContextMock = mocker.GetMockDbContext<ApplicationDbContext>();
        var dbContext = dbContextMock.Object;
        dbContext.Blogs.Add(blogsTestData); // Can also be dbContext.Set<Blog>().Add(blogsTestData)

        var validator = new BtnValidator(dbContext);

        // Act
        var result = validator.IsValid(id);

        // Assert
        Assert.True(result);
    }
}
```

*Run Your Tests*: Execute your unit tests, and the mock DbContext will behave like a real DbContext, allowing you to test your BtnValidator logic without hitting the actual database.

### Null / Parameter checking

Testing constructor parameters is very easy with TestAllConstructorParameters and TestConstructorParameters.

#### Test Methods in MockerTestBase

```TestConstructorParameters``` - Test the constructor that MockerTestBase used to create the Component for the test.
```TestAllConstructorParameters``` - Test all constructors in the component, regardless if the constructor was used to create the component for the test.

#### Test Extension Helper

```EnsureNullCheckThrown``` - Tests the given action, parameter, and constructor parameter to ensure a NullArgumentException is thrown when null.

- ```action``` is the Action to run which must throw the ```ArgumentNullException``` for the specified parameter. This is generally an action provided by ```TestConstructorParameters``` or ```TestAllConstructorParameters```.
- ```parameter``` is the name of the parameter that should throw the ```ArgumentNullException```. The exception must include the name of the parameter which is the default behavior of the ```ArgumentNullException```.
- ```constructor``` (optional) is the constructor being tested. This is the string to display in test output; specifically is multiple constructors are tested.
- ```outputWriter``` (optional) is the ```ITestOutputHelper``` specific to XUnit (```XUnit.Abstractions```)

```cs
action.EnsureNullCheckThrown(parameter, constructor, outputWriter);
```

#### Example: Check all constructor parameters for null and output to the test console

```cs
[Fact]
public void Service_NullArgChecks_AllConstructorsShouldPass() =>
    TestAllConstructorParameters((action, constructor, parameter) =>
        action.EnsureNullCheckThrown(parameter, constructor, outputWriter));
```

#### Example: Check constructor parameters for null exception

```cs
// Check values for null
[Fact]
public void Service_NullArgChecks() => TestConstructorParameters((action, constructorName, parameterName) =>
{
    outputWriter?.WriteLine($"Testing {constructorName}\n - {parameterName}");
    
    action
        .Should()
        .Throw<ArgumentNullException>()
        .WithMessage($"*{parameterName}*");
});
```

#### Example: Check values for specific criteria

```cs
// Check values for specific criteria.
[Fact]
public void Service_NullArgChecks() => TestConstructorParameters((action, constructorName, parameterName) =>
    {
        outputWriter?.WriteLine($"Testing {constructorName}\n - {parameterName}");
    
        action
            .Should()
            .Throw<ArgumentNullException>()
            .WithMessage($"*{parameterName}*");
    },
    info =>
    {
        return info switch
        {
            { ParameterType: { Name: "string" }} => string.Empty,
            { ParameterType: { Name: "int" }} => -1,
            _ => default,
        };
    },
    info =>
    {
        return info switch
        {
            { ParameterType: { Name: "string" }} => "Valid Value",
            { ParameterType: { Name: "int" }} => 22,
            _ => Mocks.GetObject(info.ParameterType),
        };
    }
);
```

#### Example: Test constructors for null and output to a list while using the extension helper

```cs
// Test constructors for null, using built-in extension and log the output.
[Fact]
public void TestAllConstructors_WithExtension()
{
    var messages = new List<string>();
    TestAllConstructorParameters((action, constructor, parameter) => action.EnsureNullCheckThrown(parameter, constructor, messages.Add));

    messages.Should().Contain(new List<string>()
        {
            "Testing .ctor(IFileSystem fileSystem, String field)\n - fileSystem",
            "Passed fileSystem",
            "Testing .ctor(IFileSystem fileSystem, String field)\n - field",
            "Passed field",
        }
    );
}
```

### Call Method with auto injected parameters

When testing method calls on a component, often the method's parameters are mock objects or just need default values. Instead of maintaining the parameter list, methods can be called without specifying specific parameters until required.
FastMoq knows how mocks are already defined and the caller can use those mocks or their own provided mocks, if required.
The helper command, ```CallMethod``` can be used to call any method with or without parameters.

Any method called with ```CallMethod``` can be anywhere such as a component method or a static method. Given the following ```CallTestMethod```, it takes value and reference parameters, many of which can be mocked. If the value cannot be mocked, it can be defaulted.

```cs
 public object?[] CallTestMethod(int num, IFileSystem fileSystem, ITestCollectionOrderer dClass, TestClassMultiple mClass, string name)
 {
     ArgumentNullException.ThrowIfNull(fileSystem);
     ArgumentNullException.ThrowIfNull(dClass);
     ArgumentNullException.ThrowIfNull(mClass);
     ArgumentNullException.ThrowIfNull(num);
     ArgumentNullException.ThrowIfNull(name);

     return
     [
         num, fileSystem, dClass, mClass, name,
     ];
 }

```

In this simple call, the method ```CallTestMethod``` will be called with default values and the current mocks.

```cs
 [Fact]
 public void CallMethod()
 {
     var result = Mocks.CallMethod<object?[]>(Component.CallTestMethod);
     result.Length.Should().Be(5);
     result[0].Should().Be(0);
     result[1].Should().BeOfType<MockFileSystem>().And.NotBeNull();
     result[2].GetType().IsAssignableTo(typeof(ITestCollectionOrderer)).Should().BeTrue();
     result[2].Should().NotBeNull();
     result[3].GetType().IsAssignableTo(typeof(TestClassMultiple)).Should().BeTrue();
     result[3].Should().NotBeNull();
     result[4].Should().Be("");
 }

```

In the previous call, ```CallMethod``` attempts to use mock parameters and then default values. The value for ```num``` was the ```default(int)``` which is 0. The default string value is ```string.Empty```.

To override a value, parameters can be passed to the method. The parameters do not have to have the same count, but they do require the same order. For example, the following code calls ```CallTestMethod``` with num parameter 4 instead of 0. All other parameters are defaulted to their mocks or value default.

```cs

 [Fact]
 public void CallMethod_WithParams()
 {
     var result = Mocks.CallMethod<object?[]>(Component.CallTestMethod, 4);
     result.Length.Should().Be(5);
     result[0].Should().Be(4);
     result[1].Should().BeOfType<MockFileSystem>().And.NotBeNull();
     result[2].GetType().IsAssignableTo(typeof(ITestCollectionOrderer)).Should().BeTrue();
     result[2].Should().NotBeNull();
     result[3].GetType().IsAssignableTo(typeof(TestClassMultiple)).Should().BeTrue();
     result[3].Should().NotBeNull();
     result[4].Should().Be("");
 }
```

In the next call, the first two parameters are overridden only and the other values are the default mock objects/values.

```cs

 [Fact]
 public void CallMethod_WithParams2()
 {
     var result = Mocks.CallMethod<object?[]>(Component.CallTestMethod, 4, Mocks.fileSystem);
     result.Length.Should().Be(5);
     result[0].Should().Be(4);
     result[1].Should().BeOfType<MockFileSystem>().And.NotBeNull();
     result[2].GetType().IsAssignableTo(typeof(ITestCollectionOrderer)).Should().BeTrue();
     result[2].Should().NotBeNull();
     result[3].GetType().IsAssignableTo(typeof(TestClassMultiple)).Should().BeTrue();
     result[3].Should().NotBeNull();
     result[4].Should().Be("");
 }

```

Exceptions can be caught just like the method was called directly. The example below shows the assert looking for an argument null exception.

```cs

 [Fact]
 public void CallMethod_WithException()
 {
     Assert.Throws<ArgumentNullException>(() => Mocks.CallMethod<object?[]>(Component.CallTestMethod, 4, null));
 }
```

## Troubleshooting

### System.MethodAccessException

If the test returns:

- System.MethodAccessException: Attempt by method 'Castle.Proxies.ApplicationDbContextProxy..ctor(Castle.DynamicProxy.IInterceptor[])' to access method

Add the following ```InternalsVisibleTo``` line to the AssemblyInfo file.
```[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]```

## Additional Documentation

[FastMoq API Documentation](https://help.fastmoq.com/)

## Full Change Log

[Full Change Log](https://github.com/cwinland/FastMoq/releases)

## Breaking Change(s)

For current repo-era breaking changes and the older package-line change summary that used to live here, see [Breaking Changes](./docs/breaking-changes/README.md).

## [License - MIT](./License)

[https://help.fastmoq.com/](https://help.fastmoq.com/)

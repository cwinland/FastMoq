# [FastMoq](http://help.fastmoq.com/)

Easy and fast extension of the [Moq](https://github.com/Moq) mocking framework for mocking and auto injection of classes when testing.

## 📚 Documentation

### Quick Links
- **🚀 [Getting Started Guide](./docs/getting-started.md)** - Your first FastMoq test in 5 minutes
- **👨‍🍳 [Cookbook](./docs/cookbook/)** - Real-world patterns and recipes
- **🏗️ [Sample Applications](./docs/samples/)** - Complete examples with Azure integration
- **📊 [Feature Comparison](./docs/feature-parity.md)** - FastMoq vs Moq/NSubstitute
- **📈 [Performance Benchmarks](./docs/performance.md)** - Productivity and performance metrics

### Additional Resources
- **📖 [Complete Documentation](./docs/README.md)** - All guides and references in one place
- **❓ [FAQs](./FAQs.md)** - Frequently asked questions and troubleshooting
- **🔗 [API Documentation](https://cwinland.github.io/FastMoq/Help/html/N-FastMoq.htm)** - Complete API reference

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
- DbContext support - SqlLite and mockable objects.
- Supports Null method parameter testing.
- **Comprehensive Documentation** - Complete guides, samples, and real-world patterns.

## Packages

- FastMoq - Combines FastMoq.Core and FastMoq.Web.
- FastMoq.Core - Original FastMoq testing Mocker.
- FastMoq.Web - New Blazor and Web support.

## Targets

- .NET 9
- .NET 8
- .NET 6

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

- [Examples and documentation of MockerTestBase](http://help.fastmoq.com/Help/html/T-FastMoq.MockerTestBase-1.htm)
- [Examples and documentation of MockerBlazorTestBase](http://help.fastmoq.com/Help/html/T-FastMoq.Web.Blazor.MockerBlazorTestBase-1.htm)

### Basic example of the base class creating the Car class and auto mocking ICarService

```cs
public class CarTest : MockerTestBase<Car> {
     [Fact]
     public void TestCar() {
         Component.Color.Should().Be(Color.Green);
         Component.CarService.Should().NotBeNull();
     }
}

public class Car {
     public Color Color { get; set; } = Color.Green;
     public ICarService CarService { get; }
     public Car(ICarService carService) => CarService = carService;
}

public interface ICarService
{
     Color Color { get; set; }
     ICarService CarService { get; }
     bool StartCar();
}
```

### Example of how to set up for mocks that require specific functionality

```cs
public class CarTest : MockerTestBase<Car> {
     public CarTest() : base(mocks => {
             mocks.Initialize<ICarService>(mock => mock.Setup(x => x.StartCar).Returns(true));
     }
}
```

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

[FastMoq API Documentation](https://cwinland.github.io/FastMoq/Help/html/N-FastMoq.htm)

## Full Change Log

[Full Change Log](https://github.com/cwinland/FastMoq/releases)

## Breaking Change(s)

- 3.0 => .NET 9 Added; Update FindBestMatch; Component Creation; Logging Callbacks and helpers;
- 2.28 => .NET 7 Removed from support.
- 2.25 => Some methods moved to extensions that are no longer in the MockerTestBase or Mocker. Removed extra CreateInstance&lt;T&gt; methods.
- 2.23.200 => Support .NET 8
- 2.23.x    => Removed support for .NET Core 5.
- 2.22.1215 => Removed support for .NET Core 3.1 in FastMoq.Core. Deprecated .NET Core 5 and moved package supporting .NET Core 5.0 from FastMoq to FastMoq.Core.
- 1.22.810 => Removed setters on the MockerTestBase virtual methods: SetupMocksAction, CreateComponentAction, CreatedComponentAction
- 1.22.810 => Update Package Dependencies
- 1.22.728 => Initialize method will reset the mock, if it already exists. This is overridable by settings the reset parameter to false.
- 1.22.604 => Renamed Mocks to Mocker, Renamed TestBase to MockerTestBase.

## [License - MIT](./License)

[http://help.fastmoq.com](http://help.fastmoq.com/)

# [FastMoq](http://help.fastmoq.com/)

Easy and fast extension of the [Moq](https://github.com/Moq) mocking framework for mocking and auto injection of classes when testing.

## API Documentation

[FastMoq API Documentation](https://cwinland.github.io/FastMoq/Help/html/N-FastMoq.htm)

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

## Packages .NET 6.0,7.0,8.0

- FastMoq - Combines FastMoq.Core and FastMoq.Web.
- FastMoq.Core - Original FastMoq testing Mocker.
- FastMoq.Web - New Blazor and Web support.

## Targets

- .NET 7
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

This code maps ITestClassDouble to TestClassDouble1 when testing a component with ITestClassDouble.

```cs
Mocker.AddType<ITestClassDouble, TestClassDouble1>();
```

The map also accepts parameters to tell it how to create the instance.

```cs
Mocks.AddType<ITestClassDouble, TestClassDouble1>(() => new TestClassDouble());
```

### DbContext Mocking

#### Test ContextDb

```cs
public class DbContextTests : MockerTestBase<MyDbContext>
```

#### Use ContextDb in test methods

```cs
    var mockDbContext = Mocks.GetMockDbContext<MyDbContext>();
    var dbContext = mockDbContext.Object;
```

### Null / Parameter checking

Testing constructor parameters is very easy with TestAllConstructorParameters and TestConstructorParameters.

TestConstructorParameters - Test the constructor that MockerTestBase used to create the Component for the test.
TestAllConstructorParameters - Test all constructors in the component, regardless if the constructor was used to create the component for the test.

#### Example: Check values for null

```cs
    // Check values for null
    [Fact]
    public void Service_NullArgChecks() => TestConstructorParameters((action, constructorName, parameterName) =>
    {
        output?.WriteLine($"Testing {constructorName}\n - {parameterName}");
        
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
            output?.WriteLine($"Testing {constructorName}\n - {parameterName}");
        
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

#### Example: Test constructors for null with output

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

## Additional Documentation

[FastMoq API Documentation](https://cwinland.github.io/FastMoq/Help/html/N-FastMoq.htm)

## Full Change Log

[Full Change Log](https://github.com/cwinland/FastMoq/releases)

## Breaking Change
- 2.23.Latest    => Removed support for .NET Core 5.
- 2.22.1215 => Removed support for .NET Core 3.1 in FastMoq.Core. Deprecated .NET Core 5 and moved package supporting .NET Core 5.0 from FastMoq to FastMoq.Core.
- 1.22.810 => Removed setters on the MockerTestBase virtual methods: SetupMocksAction, CreateComponentAction, CreatedComponentAction
- 1.22.810 => Update Package Dependencies
- 1.22.728 => Initialize method will reset the mock, if it already exists. This is overridable by settings the reset parameter to false.
- 1.22.604 => Renamed Mocks to Mocker, Renamed TestBase to MockerTestBase.
- 
- 2.23.200 => Support .NET 8

## [License - MIT](./License)

[http://help.fastmoq.com](http://help.fastmoq.com/)
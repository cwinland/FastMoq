# [FastMoq](http://help.fastmoq.com/)

[http://help.fastmoq.com](http://help.fastmoq.com/)

Easy and fast extension of the [Moq](https://github.com/Moq) mocking framework for mocking and auto injection of classes when testing.

## Features

- Test without declaring Mocks (unless needed).
- Creates objects with chain of automatic injections in objects and their dependencies.
- Automatically injects and creates components or services.
- Injection: Automatically determines what interfaces need to be injected into the constructor and creates mocks if they do not exist.
  - Best guess picks the multiple parameter constructor over the default constructor.
  - Specific mapping allows the tester to create an instance using a specific constructor and specific data.
- Use Mocks without managing fields and properties. Mocks are managed by the Mocker framework. No need to keep track of Mocks. Just use them!
- Create instances of Mocks with non public constructors.

## Targets

- .NET 7
- .NET 6
- .NET 5
- .NET Core 3.1

## Most used classes in the FastMoq namespace

```cs
public class Mocker {} // Primary class for auto mock and injection. This can be used standalone from MockerTestBase using Mocks property on the base class.
public abstract class MockerTestBase<TComponent> where TComponent : class {} // Assists in the creation of objects and provides direct access to Mocker.
```

## Examples

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

## Additional Documentation

[FastMoq API Documentation](https://cwinland.github.io/FastMoq/Help/html/N-FastMoq.htm)

## Breaking Change

- 1.22.810 => Removed setters on the MockerTestBase virtual methods: SetupMocksAction, CreateComponentAction, CreatedComponentAction
- 1.22.810 => Update Package Dependencies
- 1.22.728 => Initialize method will reset the mock, if it already exists. This is overridable by settings the reset parameter to false.
- 1.22.604 => Renamed Mocks to Mocker, Renamed TestBase to MockerTestBase.

## [License - MIT](./License)

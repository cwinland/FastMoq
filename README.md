# FastMoq

Easy and fast extension of [Moq](https://github.com/Moq), mocking framework, for mocking and auto injection of classes.

## Features

- Auto Injection into tested component constructors
  - Best guess picks the multiple parameter constructor over the default constructor.
  - Specific mapping allows the tester to create an instance using a specific constructor and specific data.
- Auto Mocking creation whenever a mock is first used.

## Targets

- .NET Core 3.1
- .NET 5
- .NET 6

## Test Base Constructor Parameters

The following constructor parameters allow customization on the testing classes.

```cs
Action<Mocks> setupMocksAction
Func<TComponent> createComponentAction
Action<TComponent?>? createdComponentAction
```

## Examples

### Example Test Class

Testing this class will auto inject IFileSystem.

```cs
public class TestClassNormal : ITestClassNormal
{
    public event EventHandler TestEvent;
    public IFileSystem FileSystem { get; set; }
    public TestClassNormal() { }
    public TestClassNormal(IFileSystem fileSystem) => FileSystem = fileSystem;
    public void CallTestEvent() => TestEvent?.Invoke(this, EventArgs.Empty);
}
```

### Fast Start Testing

TestClassNormal is created and injects IFileSystem.

```cs
public class TestClassNormalTestsDefaultBase : TestBase<TestClassNormal>
{
    [Fact]
    public void Test1()
    {
        Component.FileSystem.Should().NotBeNull();
        Component.FileSystem.Should().BeOfType<MockFileSystem>();
        Component.FileSystem.File.Should().NotBeNull();
        Component.FileSystem.Directory.Should().NotBeNull();
    }
}
```

### Pre-Test Setup

TestClassNormal is created and injects IFileSystem. SetupMocksAction creates and configures the Mock IFileSystem before the component is created.

```cs
public class TestClassNormalTestsSetupBase : TestBase<TestClassNormal>
{
    public TestClassNormalTestsSetupBase() : base(SetupMocksAction) { }

    private static void SetupMocksAction(Mocks mocks)
    {
        var iFile = new FileSystem().File;
        mocks.Strict = true;

        mocks.Initialize<IFileSystem>(mock => mock.Setup(x => x.File).Returns(iFile));
    }

    [Fact]
    public void Test1()
    {
        Component.FileSystem.Should().NotBeNull();
        Component.FileSystem.Should().NotBeOfType<MockFileSystem>();
        Component.FileSystem.File.Should().NotBeNull();
        Component.FileSystem.Directory.Should().BeNull();
    }
}
```

### Custom Setup, Creation, and Post Create routines

TestClassNormal is created and injects IFileSystem. SetupMocksAction creates and configures the Mock IFileSystem before the component is created. Once created, the CreatedComponentAction subscribes to an event on the component.

```cs
public class TestClassNormalTestsFull : TestBase<TestClassNormal>
{
    private static bool testEventCalled;
    public TestClassNormalTestsFull() : base(SetupMocksAction, CreateComponentAction, CreatedComponentAction) => testEventCalled = false;
    private static void CreatedComponentAction(TestClassNormal? obj) => obj.TestEvent += (_, _) => testEventCalled = true;
    private static TestClassNormal CreateComponentAction(Mocks mocks) => new(mocks.GetObject<IFileSystem>());

    private static void SetupMocksAction(Mocks mocks)
    {
        var mock = new Mock<IFileSystem>();
        var iFile = new FileSystem().File;
        mocks.Strict = true;
        mocks.AddMock(mock, true);
        mocks.Initialize<IFileSystem>(xMock => xMock.Setup(x => x.File).Returns(iFile));
    }

    [Fact]
    public void Test1()
    {
        Component.FileSystem.Should().Be(Mocks.GetMock<IFileSystem>().Object);
        Component.FileSystem.Should().NotBeNull();
        Component.FileSystem.File.Should().NotBeNull();
        Component.FileSystem.Directory.Should().BeNull();
        testEventCalled.Should().BeFalse();
        Component.CallTestEvent();
        testEventCalled.Should().BeTrue();

        Mocks.Initialize<IFileSystem>(mock => mock.Setup(x => x.Directory).Returns(new FileSystem().Directory));
        Component.FileSystem.Directory.Should().NotBeNull();

    }
}
```

### Auto Injection

Auto injection allows creation of components with parameterized interfaces. If an override for creating the component is not specified, the component will be created will the default Mock Objects.

#### Auto Injection with instance parameters

Additionally, the creation can be overwritten and provided with instances of the parameters. CreateInstance will automatically match the correct constructor to the parameters given to CreateInstance.

```cs
private static TestClassNormal CreateComponentAction() => Mocks.CreateInstance(new MockFileSystem()); // CreateInstance matches the parameters and types with the Component constructor.
```

#### Interface Type Map

When multiple classes derive from the same interface, the Interface Type Map can map with class to use for the given injected interface.

##### Example of two classes inheriting the same interface

```cs
public class TestClassDouble1 : ITestClassDouble {}
public class TestClassDouble2 : ITestClassDouble {}
```

##### Mapping

This code maps ITestClassDouble to TestClassDouble1 when testing a component with ITestClassDouble.

```cs
Mocks.AddType<ITestClassDouble, TestClassDouble1>();
```

The map also accepts parameters to tell it how to create the instance.

```cs
Mocks.AddType<ITestClassDouble, TestClassDouble1>(() => new TestClassDouble());
```

## [License - MIT](./License)

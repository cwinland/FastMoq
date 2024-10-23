# FastMoq Frequently Asked Questions (FAQs)

## Table of Contents

### About FastMoq

- [What is FastMoq?](#what-is-fastmoq)
- [How do I install FastMoq?](#how-do-i-install-fastmoq)
- [Can I use FastMoq with existing .NET testing frameworks?](#can-i-use-fastmoq-with-existing-net-testing-frameworks)

### Writing Tests

- [Where do I setup my mocks? Constructor, SetupMocksAction, or test method?](#where-do-i-setup-my-mocks-constructor-setupmocksaction-or-test-method)
- [Why is the constructor parameter null instead of containing a Mock?](#why-is-the-constructor-parameter-null-instead-of-containing-a-mock)
- [The Mocks exist, but they are not setup while running in the constructor?](#the-mocks-exist-but-they-are-not-setup-while-running-in-the-constructor)
- [I receive this error: System.MethodAccessException: Attempt by method 'Castle.Proxies.ApplicationDbContextProxy..ctor(Castle.DynamicProxy.IInterceptor[])' to access method](#i-receive-this-error-systemmethodaccessexception-attempt-by-method-castleproxiesapplicationdbcontextproxyctorcastledynamicproxyiinterceptor-to-access-method)
- [Are there additional ways to debug FastMoq or the Mock itself?](#are-there-additional-ways-to-debug-fastmoq-or-the-mock-itself)
- [What helpers are available in MockerTestBase&lt;T&gt;?](#what-helpers-are-available-in-mockertestbaset)
- [What helpers are available in Mocker?](#what-helpers-are-available-in-mocker)
- [What are some settings Mocker providers to alter the way Mocker works?](#what-are-some-settings-mocker-providers-to-alter-the-way-mocker-works)

### More Information

- [Where can I find more documentation and support for FastMoq?](#where-can-i-find-more-documentation-and-support-for-fastmoq)
- [How do I contribute to FastMoq?](#how-do-i-contribute-to-fastmoq)
- [Who maintains FastMoq?](#who-maintains-fastmoq)
- [How do I report a bug or request a feature?](#how-do-i-report-a-bug-or-request-a-feature)
- [Is FastMoq open source?](#is-fastmoq-open-source)

## What is FastMoq?

FastMoq is a flexible and powerful mocking library designed for .NET developers to simplify the creation and configuration of mock objects in unit tests. It aims to enhance productivity and maintainability in your testing framework. The FastMoq GitHub Repository is located at <https://github.com/cwinland/FastMoq>. The Nuget feed is located at <https://www.nuget.org/packages/FastMoq.Web/>.

## How do I install FastMoq?

You can install FastMoq using the .NET CLI or through the NuGet Package Manager in Visual Studio:

**Using .NET CLI:**

```sh
dotnet add package FastMoq
```

**Using NuGet Package Manager Console:**

```sh
Install-Package FastMoq
```

## Can I use FastMoq with existing .NET testing frameworks?

Absolutely! FastMoq is designed to integrate seamlessly with popular .NET testing frameworks like NUnit, MSTest, and xUnit. Just add FastMoq to your test project and start creating mocks.

## How do I create my mocks?

Think of Mocks as they already exist. In general, you do not need to create Mocks. FastMoq creates and manages all the mocks for you. To get a mock (new or existing), use:

```cs
Mocks.GetMock<TInterface>();
```

```Mocks``` (referenced above) is the name of the property in ```MockerTestBase<T>``` for ```FastMoq.Mocker```.

## Where do I setup my mocks? Constructor, SetupMocksAction, or test method?

Placement of the code that sets up the mock depends on the scope of the mock and where it is needed in the component being tested. Although Mocks are almost always injected into a constructor, they might not be called until later in one of the methods being tested. When FastMoq injects the mock, the test still has a reference to all mocks that are injected into the component being tested.

### FastMoq Locations and Scopes

| **Location**         | **Scope**                                                                                                                | **Can Use Instance Data** | **Pros / Cons**                                                                                                                     |
|----------------------|--------------------------------------------------------------------------------------------------------------------------|---------------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| Test Constructor     | All test methods *(available for all class test methods, but after the test component is created)*         | Yes                       | Used for default setup that either doesn't change or changes for only specific tests. |
| Test Method          | Current test method | Yes                       | Use for test-specific setup. |
| SetupMocksAction**   | Test component constructor, Test class constructor, and all test methods                | Yes                       | Use for class component setup needed for constructor. The setup can change based on a class variable or property. [Example](#option-2-override-setupmocksaction)                 |
| Base Constructor**   | Test component constructor, Test class constructor, and all test methods                | No                        | Use for class component setup; the code does not need to change based on a variable or property of the class. [Example](#option-1-base-class-constructor)

**Note:** The global methods MUST be used if the data is required for the parameters of the component or used in the constructor's code.

## Why is the constructor parameter null instead of containing a Mock?

FastMoq will automatically attempt to create Mocks for Interfaces and objects. Strings and Value types will get default values unless otherwise specified.

### Nullable or Optional Parameters

```c#
public class MyService
{
    public MyService(ISomeInterface someService, ISomeOtherService? someOtherService = null) {}
}
```

If the constructor has an optional or nullable parameter, FastMoq assumes that the parameter should stay null or the default.

In this example, someService will be mocked and someOtherService will be null. In order to mock optional parameters, specify ```c# Mocker.MockOptional = true;```. If the constructor is run when the MockOptional is set to true, then all services should have mocks.

## The Mocks exist, but they are not setup while running in the constructor?

The component under test is created before the constructor in the test class. In order to specify options and Mock setups before creating the component, you'll need to add code to the setup.  There are two methods for adding setup code, the base constructor or the SetupMocksAction override.

### Option 1: Base Class Constructor

This example shows putting mocker setup in the constructor. The code must be static and cannot access non-static members.

```c#
    public class TestMyService : MockerTestBase<MyService> {
        public TestMyService() : base(setupMocksAction: m => { 
            m.MockOptional = true;
            
            m.GetMock<IProfileRepo>().Setup(x => x.GetProfileAsync(It.IsAny<string>()))
                .ReturnsAsync(() => [new ProfileEntity { ProfileType = "test" }]);

            m.Initialize<IConfigRepo>(configMock =>
            {
                configMock.Setup(x => x.GetTypeByIdAsync(id)).ReturnsAsync(() =>
                    new TypeEntity 
                    {
                        // Type code here
                    }
                );
                configMock.Setup(x => x.GetTypesAsync()).ReturnsAsync(
                    () =>
                    [
                        new TypeEntity
                        {
                            // Type code here
                        },
                        new TypeEntity
                        {
                            // Type code here
                        },
                    ]
                );
            });
        })
    }
```

### Option 2: Override SetupMocksAction

This example shows putting mocker setup in the SetupMocksAction property. The code can access static and non-static members.

```c#
protected override Action<Mocker> SetupMocksAction => m =>
{
    m.MockOptional = true;
    
    m.GetMock<IProfileRepo>().Setup(x => x.GetProfileAsync(It.IsAny<string>()))
        .ReturnsAsync(() => [new ProfileEntity { ProfileType = "test" }]);

    m.Initialize<IConfigRepo>(configMock =>
    {
        configMock.Setup(x => x.GetTypeByIdAsync(id)).ReturnsAsync(() =>
            new TypeEntity 
            {
                // Type code here
            }
        );
        configMock.Setup(x => x.GetTypesAsync()).ReturnsAsync(
            () =>
            [
                new TypeEntity
                {
                    // Type code here
                },
                new TypeEntity
                {
                    // Type code here
                },
            ]
        );
    });
};
```

## I receive this error: System.MethodAccessException: Attempt by method 'Castle.Proxies.ApplicationDbContextProxy..ctor(Castle.DynamicProxy.IInterceptor[])' to access method

Add the following InternalsVisibleTo line to the AssemblyInfo file.

```c#
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
```

## Are there additional ways to debug FastMoq or the Mock itself?

Yes, the Mocker class provides properties to track which constructors are called, error messages, etc.

- ```ConstructorHistory``` tracks the constructors that are called.
- ```ExceptionLog``` tracks any exceptions that are intercepted by FastMoq, whether they are ignored or bubbled to the test, they should listed in this log.
- ```mockCollection``` tracks all the existing Mocks that the framework can see.

## What helpers are available in MockerTestBase&lt;T&gt;?

- ```TestAllConstructorParameters```
- ```TestConstructorParameters```
- ```WaitFor```

## What helpers are available in Mocker?

Properties:

- ```fileSystem``` gets an internal ```MockFileSystem``` for IFileSystem injections.
- ```HttpClient``` gets the client used for injections.
- ```DbConnection``` gets the connection used for DbContext injections.

Methods:

- ```AddType``` is dependency injection for tests. Similar to AddSingleton.
- ```GetMock``` gets the Mock used in the tested component.
- ```GetObject``` gets the object of the Mock used in the tested component.
- ```CreateInstance``` creates an instance of a public Type without the need of specifying the injected parameters.
- ```CreateInstanceNonPublic``` creates an instance of a Type without the need of specifying the injected parameters.
- ```GetDbContext``` gets the DbContext used in the tested component.
- ```GetFileSystem```gets the file system used in the tested component.
- ```GetHttpHandlerSetup``` assists in setting up HttpClient calls.
- ```GetMockDbContext``` gets a Mock DbContext.
- ```Initialize``` clears the mock and groups the mock into a callback method for easy setup.
- ```CallMethod``` calls a method and injects mocks and parameters as required.

## What are some settings Mocker providers to alter the way Mocker works?

- ```InnerMockResolution``` indicates that the Mock should attempt to resolve child mocks and injections. Default is True.
- ```MockOptional``` allows mocks to be injected into optional or nullable parameters. Default is False.
- ```Strict``` alters the way that FastMoq uses HttpClient and FileSystems. Strict prevents using the internal versions and pure mocks are used. Default is False.

## Where can I find more documentation and support for FastMoq?

You can find more documentation and support resources on the FastMoq GitHub Repository (<https://github.com/cwinland/FastMoq>). For community support and discussions, check out the issues section or open a new thread.

## How do I contribute to FastMoq?

Contributions are welcome! If you'd like to contribute to FastMoq, please read the Contributing Guide on our GitHub repository. You can submit issues, feature requests, or pull requests (<https://github.com/cwinland/FastMoq/issues>).

## Who maintains FastMoq?

FastMoq is maintained by a community of developers. For more information on the maintainers and contributors, visit the FastMoq GitHub Repository (<https://github.com/cwinland/FastMoq>).

## How do I report a bug or request a feature?

To report a bug or request a feature, please visit the Issues section on our GitHub repository and open a new issue. Provide as much detail as possible to help us understand and address your request (<https://github.com/cwinland/FastMoq/issues>).

## Is FastMoq open source?

Yes, FastMoq is an open-source project. You can view, modify, and distribute the source code under the terms of the MIT license. For more details, refer to the [LICENSE file](https://github.com/cwinland/FastMoq?tab=MIT-1-ov-file#).

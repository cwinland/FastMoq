# FastMoq Frequently Asked Questions (FAQs)

## Table of Contents

### About FastMoq

- [What is FastMoq?](#what-is-fastmoq)
- [How do I install FastMoq?](#how-do-i-install-fastmoq)
- [Can I use FastMoq with existing .NET testing frameworks?](#can-i-use-fastmoq-with-existing-net-testing-frameworks)

### Writing Tests

- [I upgraded to v4 and did not change my tests. Why are `GetMock<T>()` or `VerifyLogger(...)` now failing?](#i-upgraded-to-v4-and-did-not-change-my-tests-why-are-getmockt-or-verifylogger-now-failing)
- [Where do I setup my mocks? Constructor, SetupMocksAction, or test method?](#where-do-i-setup-my-mocks-constructor-setupmocksaction-or-test-method)
- [Why is the constructor parameter null instead of containing a Mock?](#why-is-the-constructor-parameter-null-instead-of-containing-a-mock)
- [The Mocks exist, but they are not setup while running in the constructor?](#the-mocks-exist-but-they-are-not-setup-while-running-in-the-constructor)
- [I receive this error: System.MethodAccessException: Attempt by method 'Castle.Proxies.ApplicationDbContextProxy..ctor(Castle.DynamicProxy.IInterceptor[])' to access method](#i-receive-this-error-systemmethodaccessexception-attempt-by-method-castleproxiesapplicationdbcontextproxyctorcastledynamicproxyiinterceptor-to-access-method)
- [Are there additional ways to debug FastMoq or the Mock itself?](#are-there-additional-ways-to-debug-fastmoq-or-the-mock-itself)
- [What helpers are available in MockerTestBase&lt;T&gt;?](#what-helpers-are-available-in-mockertestbaset)
- [What helpers are available in Mocker?](#what-helpers-are-available-in-mocker)
- [What are some settings on `Mocker` that alter the way FastMoq works?](#what-are-some-settings-on-mocker-that-alter-the-way-fastmoq-works)

### More Information

- [Where can I find more documentation and support for FastMoq?](#where-can-i-find-more-documentation-and-support-for-fastmoq)
- [How do I contribute to FastMoq?](#how-do-i-contribute-to-fastmoq)
- [Who maintains FastMoq?](#who-maintains-fastmoq)
- [How do I report a bug or request a feature?](#how-do-i-report-a-bug-or-request-a-feature)
- [Is FastMoq open source?](#is-fastmoq-open-source)

## What is FastMoq?

FastMoq is a .NET testing framework focused on automatic dependency injection, tracked test doubles, and test-friendly object creation. It helps reduce boilerplate while still letting you drop into compatibility or provider-specific behavior when a test needs it. The GitHub repository is located at <https://github.com/cwinland/FastMoq>. NuGet packages are available at <https://www.nuget.org/packages/FastMoq.Web/>.

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

In many FastMoq tests, tracked mocks already exist by the time you need them. FastMoq creates and manages those test doubles for you. A common compatibility-style entry point is:

```cs
Mocks.GetMock<TInterface>();
```

`Mocks` is the `FastMoq.Mocker` property exposed by `MockerTestBase<T>`.

For new or actively refactored tests in v4, prefer the provider-neutral APIs when possible. `GetMock<T>()` remains available as the Moq compatibility path.

## I upgraded to v4 and did not change my tests. Why are `GetMock<T>()` or `VerifyLogger(...)` now failing?

In v4, `FastMoq.Core` still bundles Moq for compatibility, but the default active provider is now `reflection`.

If an older test suite upgrades packages but keeps the old Moq-shaped APIs without selecting Moq explicitly, the code will usually still compile, but Moq-specific calls can fail at runtime.

Typical symptoms:

- `GetMock<T>()` throws because the active provider does not expose a legacy `Moq.Mock`
- `VerifyLogger(...)` still exists, but it remains a Moq compatibility API rather than a provider-neutral one
- `SetupHttpMessage(...)` is available from the Moq compatibility package, but it also requires the Moq provider to be active for that test assembly
- tests that previously assumed Moq was the implicit default start failing even though no test code was changed

If you want the shortest v4 compatibility fix for an older test assembly, set Moq as the assembly default:

```csharp
using System.Runtime.CompilerServices;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;

namespace MyTests;

public static class TestAssemblyProviderBootstrap
{
    [ModuleInitializer]
    public static void Initialize()
    {
        MockingProviderRegistry.Register("moq", MoqMockingProvider.Instance, setAsDefault: true);
    }
}
```

If you are touching the tests anyway, prefer moving toward the provider-neutral APIs instead:

- use `GetOrCreateMock(...)` when you do not specifically need raw `Moq.Mock`
- use `Mocks.VerifyLogged(...)` instead of `VerifyLogger(...)`
- use `WhenHttpRequest(...)` or `WhenHttpRequestJson(...)` instead of `SetupHttpMessage(...)` unless the test intentionally depends on the Moq compatibility path

See also:

- [Provider Selection Guide](./docs/getting-started/provider-selection.md)
- [Migration Guide](./docs/migration/README.md)

## Where do I setup my mocks? Constructor, SetupMocksAction, or test method?

Placement of the code that sets up the mock depends on the scope of the mock and where it is needed in the component being tested. Although Mocks are almost always injected into a constructor, they might not be called until later in one of the methods being tested. When FastMoq injects the mock, the test still has a reference to all mocks that are injected into the component being tested.

### FastMoq Locations and Scopes

| **Location** | **Scope** | **Can Use Instance Data** | **Pros / Cons** |
| --- | --- | --- | --- |
| Test Constructor | All test methods *(available for all class test methods, but after the test component is created)* | Yes | Used for default setup that either doesn't change or changes for only specific tests. |
| Test Method | Current test method | Yes | Use for test-specific setup. |
| SetupMocksAction** | Test component constructor, test class constructor, and all test methods | Yes | Use for class component setup needed for constructor. The setup can change based on a class variable or property. [Example](#option-2-override-setupmocksaction) |
| Base Constructor** | Test component constructor, test class constructor, and all test methods | No | Use for class component setup; the code does not need to change based on a variable or property of the class. [Example](#option-1-base-class-constructor) |

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

If the constructor has an optional or nullable parameter, FastMoq assumes that the parameter should stay null or the declared default unless you opt into optional-parameter resolution.

In this example, someService will be mocked and someOtherService will be null by default. For new code, prefer explicit optional-parameter resolution on the mocker:

```c#
mocker.OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker;

var service = mocker.CreateInstance<MyService>();
```

`Mocker.MockOptional = true` still works, but it is now obsolete and retained only as a compatibility alias over the same behavior.

## The Mocks exist, but they are not setup while running in the constructor?

The component under test is created before the test-class constructor body finishes. If you need configuration or test-double setup before the component is created, put it in the base constructor callback or the `SetupMocksAction` override.

### Option 1: Base Class Constructor

This example shows setup in the base constructor callback. The code cannot depend on instance state from the test class.
These examples assume the Moq provider extensions are available for the arrange-time `Setup(...)` calls.

```c#
public class TestMyService : MockerTestBase<MyService>
{
    public TestMyService()
        : base(setupMocksAction: mocker =>
        {
            const int typeId = 42;

            mocker.OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker;

            mocker.GetOrCreateMock<IProfileRepo>()
                .Setup(x => x.GetProfileAsync(It.IsAny<string>()))
                .ReturnsAsync([new ProfileEntity { ProfileType = "test" }]);

            var configRepo = mocker.GetOrCreateMock<IConfigRepo>();
            configRepo.Setup(x => x.GetTypeByIdAsync(typeId)).ReturnsAsync(
                new TypeEntity
                {
                    // Type code here
                });
            configRepo.Setup(x => x.GetTypesAsync()).ReturnsAsync(
            [
                new TypeEntity
                {
                    // Type code here
                },
                new TypeEntity
                {
                    // Type code here
                },
            ]);
        })
    {
    }
}
```

### Option 2: Override SetupMocksAction

This example shows setup in the `SetupMocksAction` property. The code can use instance state from the test class.

```c#
protected override Action<Mocker> SetupMocksAction => mocker =>
{
    const int typeId = 42;

    mocker.OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker;

    mocker.GetOrCreateMock<IProfileRepo>()
        .Setup(x => x.GetProfileAsync(It.IsAny<string>()))
        .ReturnsAsync([new ProfileEntity { ProfileType = "test" }]);

    var configRepo = mocker.GetOrCreateMock<IConfigRepo>();
    configRepo.Setup(x => x.GetTypeByIdAsync(typeId)).ReturnsAsync(
        new TypeEntity
        {
            // Type code here
        });
    configRepo.Setup(x => x.GetTypesAsync()).ReturnsAsync(
    [
        new TypeEntity
        {
            // Type code here
        },
        new TypeEntity
        {
            // Type code here
        },
    ]);
};
```

## I receive this error: System.MethodAccessException: Attempt by method 'Castle.Proxies.ApplicationDbContextProxy..ctor(Castle.DynamicProxy.IInterceptor[])' to access method

Add the following InternalsVisibleTo line to the AssemblyInfo file.

```c#
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
```

## Are there additional ways to debug FastMoq or the Mock itself?

Yes. `Mocker` exposes useful state for troubleshooting constructor selection, intercepted exceptions, and tracked mocks.

- ```ConstructorHistory``` tracks the constructors that are called.
- ```ExceptionLog``` tracks exceptions intercepted by FastMoq, whether they were ignored or bubbled to the test.
- ```mockCollection``` tracks all the existing Mocks that the framework can see.

## What helpers are available in MockerTestBase&lt;T&gt;?

- ```TestAllConstructorParameters```
- ```TestConstructorParameters```
- ```WaitFor```

## What helpers are available in Mocker?

Properties:

- ```fileSystem``` gets an internal ```MockFileSystem``` for IFileSystem injections.
- ```HttpClient``` gets the client used for injections.

Methods:

- ```AddType``` is dependency injection for tests. Similar to AddSingleton.
- ```GetMock``` gets the Mock used in the tested component.
- ```GetObject``` gets the object of the Mock used in the tested component.
- ```CreateInstance``` creates an instance of a public Type without the need of specifying the injected parameters.
- ```CreateInstance``` also accepts ```InstanceCreationFlags``` when constructor fallback or optional-parameter behavior should be overridden explicitly.
- ```GetMockDbContext``` gets the mocked DbContext used in the tested component.
- ```GetFileSystem```gets the file system used in the tested component.
- ```GetHttpHandlerSetup``` assists in setting up HttpClient calls.
- ```GetMockDbContext``` gets a Mock DbContext.
- ```Initialize``` clears the mock and groups the mock into a callback method for easy setup.
- ```CallMethod``` calls a method and injects mocks and parameters as required.

## What are some settings on `Mocker` that alter the way FastMoq works?

- `Strict` is a backward-compatible alias for fail-on-unconfigured behavior.
- `Behavior` is the full feature-flag model that controls FastMoq runtime behavior.
- `UseStrictPreset()` applies the predefined strict profile.
- `UseLenientPreset()` applies the predefined lenient profile.

If you only want the old "fail when not configured" behavior, use `Strict = true` or enable `MockFeatures.FailOnUnconfigured` directly. If you want the full strict preset, use `UseStrictPreset()`.

- ```InnerMockResolution``` indicates that the Mock should attempt to resolve child mocks and injections. Default is True.
- ```OptionalParameterResolution``` controls whether optional or nullable parameters use declared defaults or are resolved through FastMoq. Default is `UseDefaultOrNull`.
- ```MockOptional``` is obsolete and remains only as a compatibility alias for `OptionalParameterResolution == ResolveViaMocker`.
- `Strict` is still available as a compatibility switch, but new code should treat it as the narrower fail-on-unconfigured path rather than the full strict profile.

For fuller guidance on these settings, see:

- [Testing Guide](./docs/getting-started/testing-guide.md)
- [Migration Guide](./docs/migration/README.md)

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

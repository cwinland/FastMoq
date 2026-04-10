# FastMoq – Copilot Instructions

## 📜 Project Overview
FastMoq is an **extension framework for mocking and auto‑injection** in .NET tests.  
It wraps and extends mocking providers (currently Moq, with planned provider‑agnostic support) to:
- Auto‑create and inject mocks into components under test.
- Support both public and internal/protected types via `MockerTestBase<T>` and non‑generic `MockerTestBase`.
- Provide fluent scenario building (`.With()`, `.When()`, `.Then()`, `.Verify()`).
- Offer verification helpers and structured logging.

## 🛠 Tech Stack
- **Language**: C# (.NET 6, 8, 9 targets)
- **Test frameworks**: xUnit (primary), with Moq as current default provider.
- **Key namespaces**: `FastMoq.Core`, `FastMoq.Web`, `FastMoq.Web.Blazor`
- **Patterns**: Constructor injection, provider abstraction, extension methods.

## 📂 Key Files & Concepts
- `Mocker.cs` – Core mock registry and creation logic.
- `MockerTestBase<T>` – Generic base for public types.
- `MockerTestBase` – Non‑generic base for internal/protected types.
- `MockerConstructionHelper` – Shared constructor resolution logic.
- `TestClassExtensions.cs` – Current verification helpers (e.g., `VerifyLogger`).
- `ScenarioBuilder<T>` – Fluent scenario API (Milestone 2).
- `IMockingProvider` – Provider abstraction (Milestone 1).

## 🧩 Coding Guidelines for Copilot
When generating code:
1. **Provider‑agnostic first**  
   - Use `IMockingProvider` methods for mock creation, setup, verification, etc.
   - Only use Moq APIs inside `MoqProvider` or Moq‑specific test code.
2. **Reuse shared helpers**  
   - For component creation, always call `MockerConstructionHelper.CreateInstance`.
   - For verification, follow the `VerifyLogger` pattern.
3. **Follow naming conventions**  
   - Public API methods: PascalCase.
   - Private fields: `_camelCase`.
   - Test methods: `MethodName_ShouldExpectedBehavior_WhenCondition`.
4. **Keep tests self‑contained**  
   - Use `Component` or `ComponentAs<T>()` from base classes.
   - Prefer `Scenario.With(Component)` for new tests.
5. **Use proper using statements**  
   - Always add appropriate `using` statements at the top of files instead of fully qualified names.
   - Example: Use `using System.Runtime;` then reference `AmbiguousImplementationException` instead of `System.Runtime.AmbiguousImplementationException`.
   - Keep using statements organized and remove unused ones.
6. **Document new APIs**  
    - Add XML doc comments for all public, protected, and protected internal members you touch, including obsolete compatibility APIs that still appear in the public surface.
    - When doing a documentation cleanup pass, finish by re-checking the touched file for any remaining undocumented public, protected, or protected internal members instead of assuming the patch caught them all.
   - Include usage examples in Milestone docs.
7. **Static Analysis (Sonar) Compliance**  
   - Honor the rules in the "Static Analysis Rules" section below (S121, S122, S6608) when generating or refactoring code.
8. **Keep edits and updates task-bound**
    - Progress updates must stay grounded in the current task, files, and findings. Do not insert speculative filler, unrelated examples, or generic brainstorming text.
    - When a patch is too broad, split it into smaller targeted edits and complete local formatting cleanup before stopping.
    - If you touch a large partial class, preserve the surrounding style and fix any indentation or malformed XML introduced in the edited region before considering the work done.
9. **Keep package and solution topology aligned**
    - `FastMoq\FastMoq.csproj` must directly include every public release package project.
    - When a non-test public release project is added, removed, or renamed, update both `FastMoq.sln` and `FastMoq-Release.sln` in the same change.
    - Test-only or example-only project changes belong in `FastMoq.sln`, but do not require changes to `FastMoq-Release.sln`.

## 🔐 Static Analysis Rules (Sonar)
Apply these consistently in generated code (and prefer refactoring existing code toward them when touched):
- S121: Always use curly braces for control structures (`if/else`, `for`, `foreach`, `while`, `do`, `using`, `lock`, `switch` sections). No single-line implicit blocks.
- S122: One statement per line. Avoid multiple statements separated by semicolons on the same line. Keep declarations and executable statements clearly separated for readability.
- S6608: Prefer modern, clear C# constructs (pattern matching / expression forms) when it improves readability and safety, without sacrificing clarity. Do not apply micro-optimizations that reduce clarity. (If applicability is ambiguous, prefer the most readable form and add a brief comment if deviating.)

Notes:
- Do not introduce breaking changes solely to satisfy these rules. Apply them opportunistically or in newly generated code.
- If a rule conflicts with existing public API shape or widely-used patterns in the repository, prefer backward compatibility and add a TODO comment.

## 🚫 Avoid
- Hard‑coding Moq calls in shared/core code.
- Duplicating constructor resolution logic — always centralize in `MockerConstructionHelper`.
- Adding provider‑specific logic to `MockerTestBase` classes.
- Breaking existing public API signatures.
- Leaving touched public, protected, or protected internal members undocumented because they are obsolete or compatibility-only.
- Posting unrelated status text while working; every progress update should reflect the active task.

## 📚 Reference Examples
- See `FastMoq.Tests` for usage of `MockerTestBase<T>` and `VerifyLogger`.
- See `FastMoq.Tests.Web` for Blazor component testing patterns.
- See `FastMoq.Tests.Blazor` for UI‑specific injection and verification.

## 🧪 Testing & Validation
- All new features must have unit tests in the appropriate `FastMoq.Tests*` project.
- Run `dotnet test` before committing.
- Ensure tests pass for all target frameworks.

---

## 🎯 V2 Refactor Roadmap & Milestones

### ✅ **Objective**
Refactor FastMoq to support a **provider-agnostic mocking architecture**, a **fluent scenario builder**, and **structured logging**, while preserving developer experience and minimizing migration friction from v1 to v2.

### 🧭 **Summary of Changes**
- Introduce a **plugin-based provider model** to support Moq, NSubstitute, FakeItEasy, and custom mocking engines.
- Build a **strongly-typed fluent API** for expressive, chainable test scenarios.
- Add **structured logging and diagnostics** for mock setup, execution, and verification.
- Ensure **backward compatibility** by preserving public interface names or providing migration shims.
- Design for **extensibility** and **session-resilient refactoring**, so work can continue across sessions.

### 🛠️ **Milestone 1: Core Provider Architecture** 
**Goal:** Decouple FastMoq from specific mocking libraries and enable provider swapping.

#### Key Tasks:
- Define `IMockingProvider` interface with methods for mock creation, setup, verification, etc.
- Implement a **reflection-based default provider** (no Moq/NSubstitute dependency).
- Add `MockingProviderRegistry` for global or per-test provider selection.
- Create `MockWrapper<T>` with:
  - Strongly-typed API (`Setup`, `Verify`, etc.)
  - `NativeMock` property for direct access to underlying mock object.
- Support developer-supplied providers via `IMockingProvider`.
- Ensure provider packages use **minimum version dependencies** or reflection-only logic.
- Document provider registration and extensibility patterns.

### 🛠️ **Milestone 2: Fluent Scenario Builder**
**Goal:** Provide a fluent, expressive API for building test scenarios.

#### Key Tasks:
- Design chainable methods: `.With()`, `.When()`, `.Then()`, `.Verify()`, `.LogScenario()`.
- Ensure fluent API routes through `IMockingProvider` abstraction.
- Maintain full IntelliSense via strongly-typed wrappers.
- Support both Arrange/Act/Assert and fluent styles.
- Preserve public method names from v1 where possible.
- Document fluent usage with side-by-side comparisons to Moq.

### 🛠️ **Milestone 3: Expanded Logging & Diagnostics**
**Goal:** Add structured logging for visibility and debugging.

#### Key Tasks:
- Log mock setups, executions, verifications, and outcomes in structured format (JSON or key-value).
- Support dependency graph dumps and call traces.
- Integrate with `ILogger<T>` and test framework outputs.
- Allow per-test or global logging configuration.
- Document logging patterns and integration examples.

### 🔄 **Migration Strategy & Session Continuity**
- **Preserve public interface names** from v1 wherever possible to reduce friction.
- If breaking changes are unavoidable, provide:
  - **Migration guide** with before/after examples.
  - **Converter utilities** or shims to bridge v1 to v2.
- **Encapsulate existing logic** during refactor:
  - Move current implementations into `Legacy` or `V1` namespaces.
  - Store them in a dedicated module or branch for reference.
- **Session continuity**:
  - Save intermediate refactor state in a persistent location (e.g., `RefactorNotes.md`, `RefactorStaging.cs`).
  - Annotate TODOs and partial implementations clearly for pickup in future sessions.
  - When working on milestones, always check for existing work-in-progress files before starting.
  - Use clear naming conventions for milestone-related files (e.g., `Milestone1_IMockingProvider.cs`).

### 🧩 **Refactor Guidelines for Copilot**
When working on v2 refactor tasks:
1. **Check milestone progress** - Look for existing milestone-related files and progress indicators.
2. **Follow provider-first approach** - All new mocking functionality should go through `IMockingProvider`.
3. **Preserve backward compatibility** - Keep existing public APIs working or provide clear migration paths.
4. **Document refactor state** - Update progress files and leave clear notes for continuation.
5. **Test incrementally** - Ensure existing tests continue to pass as refactoring progresses.
6. **Use staging approach** - Create new implementations alongside existing ones before swapping.

### 📂 **Milestone File Mapping**

#### **Milestone 1: Core Provider Architecture**
**Current Files (✅ Existing):**
- `FastMoq.Core\Providers\IMockingProvider.cs` - Main provider interface
- `FastMoq.Core\Providers\IMockingProviderCapabilities.cs` - Provider capability detection
- `FastMoq.Core\Providers\MockingProviderRegistry.cs` - Registry for provider management
- `FastMoq.Core\Providers\MockCreationOptions.cs` - Options for mock creation
- `FastMoq.Core\Providers\TimesSpec.cs` - Provider-agnostic verification times
- `FastMoq.Core\Providers\ProviderBootstrap.cs` - Provider initialization
- `FastMoq.Core\Providers\MoqFastMockFactory.cs` - Moq-specific factory
- `FastMoq.Core\Providers\IMock.cs` - Basic mock interface

**Expected Files (⏳ To Create/Complete):**
- `FastMoq.Core\Providers\MockWrapper.cs` - Strongly-typed wrapper with `Setup`, `Verify`, `NativeMock`
- `FastMoq.Core\Providers\ReflectionProvider.cs` - Default reflection-based provider
- `FastMoq.Core\Providers\MoqProvider.cs` - Full Moq implementation
- `FastMoq.Providers.NSubstitute\NSubstituteProvider.cs` - NSubstitute provider package
- `FastMoq.Providers.FakeItEasy\FakeItEasyProvider.cs` - FakeItEasy provider package

#### **Milestone 2: Fluent Scenario Builder**
**Current Files (✅ Existing):**
- `FastMoq.Core\ScenarioBuilder.cs` - Initial fluent API scaffold

**Expected Files (⏳ To Create/Complete):**
- `FastMoq.Core\ScenarioBuilderExtensions.cs` - Extension methods for fluent API
- `FastMoq.Core\Scenarios\ArrangePhase.cs` - `.With()` implementation
- `FastMoq.Core\Scenarios\ActPhase.cs` - `.When()` implementation  
- `FastMoq.Core\Scenarios\AssertPhase.cs` - `.Then()` implementation
- `FastMoq.Core\Scenarios\VerifyPhase.cs` - `.Verify()` implementation
- `FastMoq.Core\MockerTestBase_Scenarios.cs` - Integration with existing test base

#### **Milestone 3: Expanded Logging & Diagnostics**
**Current Files (✅ Existing):**
- `FastMoq.Core\Extensions\TestClassExtensions.cs` - Contains current `VerifyLogger` methods

**Expected Files (⏳ To Create/Complete):**
- `FastMoq.Core\Logging\MockingLogger.cs` - Structured logging implementation
- `FastMoq.Core\Logging\DiagnosticsCollector.cs` - Dependency graph and call tracing
- `FastMoq.Core\Logging\LoggingConfiguration.cs` - Per-test and global logging config
- `FastMoq.Core\Extensions\LoggingExtensions.cs` - Enhanced logging extensions
- `FastMoq.Core\Diagnostics\CallTrace.cs` - Call tracing and verification logging
- `FastMoq.Core\Diagnostics\DependencyGraphDumper.cs` - Dependency visualization

#### **Shared/Legacy Files:**
**Current Files (✅ Existing):**
- `FastMoq.Core\Mocker.cs` - Core mock registry (to be refactored)
- `FastMoq.Core\MockerTestBase.cs` - Test base classes (to extend)
- `FastMoq.Core\Extensions\MockerCreationExtensions.cs` - Contains `CreateInstance` methods
- `FastMoq.Core\Models\MockModel.cs` - Mock model classes

**Expected Files (⏳ To Create/Complete):**
- `FastMoq.Core\Legacy\V1Mocker.cs` - Preserved v1 implementation  
- `FastMoq.Core\Migration\V1ToV2Converter.cs` - Migration utilities
- `RefactorNotes.md` - Session continuity notes (workspace root)
- `RefactorStaging.cs` - Temporary staging implementations (workspace root)

#### **Test Files:**
**Expected Test Coverage:**
- `FastMoq.Tests\Providers\ProviderTests.cs` - Provider abstraction tests
- `FastMoq.Tests\Scenarios\ScenarioBuilderTests.cs` - Fluent API tests  
- `FastMoq.Tests\Logging\MockingLoggerTests.cs` - Logging functionality tests
- `FastMoq.Tests\Migration\V1ToV2Tests.cs` - Migration compatibility tests

---

# ✅ FastMoq Test Authoring Guidelines

## 📚 Getting Started
Before writing tests, familiarize yourself with:
- `MockerTestBase<T>` - Your primary test base class
- `Mocks` property - Access to the mock registry
- `Component` property - Access to the class under test
- `VerifyLogger` extensions - For logging verification

## 🧪 Core Test Examples

### ✅ Example 1: Basic Test Structure
```csharp
public class CarServiceTests : MockerTestBase<CarService>
{
    [Fact]
    public void StartCar_ShouldReturnTrue_WhenEngineStarts()
    {
        // Arrange
        Mocks.GetMock<IEngine>()
            .Setup(x => x.Start())
            .Returns(true);

        // Act
        var result = Component.StartCar();

        // Assert
        result.Should().BeTrue();
        Mocks.GetMock<IEngine>().Verify(x => x.Start(), Times.Once);
    }
}
```

### ✅ Example 2: Logger Verification
```csharp
[Fact]
public void ProcessData_ShouldLogInformation_WhenSuccessful()
{
    // Arrange
    var data = "test data";

    // Act
    Component.ProcessData(data);

    // Assert
    Mocks.GetMock<ILogger>().VerifyLogger(LogLevel.Information, "Processing data");
}
```

### ✅ Example 3: Method Invocation with Parameters
```csharp
[Fact]
public void SaveEntity_ShouldCallRepository_WithCorrectParameters()
{
    // Arrange
    var entity = new TestEntity { Id = 123 };

    // Act
    Component.SaveEntity(entity);

    // Assert
    Mocks.GetMock<IRepository>()
        .Verify(x => x.Save(It.Is<TestEntity>(e => e.Id == 123)), Times.Once);
}
```

### ✅ Example 4: Optional Parameters (Important!)
```csharp
// ✅ CORRECT - Include all parameters explicitly
Mocks.GetMock<IRepository>()
    .Setup(x => x.GetEntityAsync(It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
    .ReturnsAsync(entity);

// ❌ INCORRECT - Missing optional parameters will cause setup failures
Mocks.GetMock<IRepository>()
    .Setup(x => x.GetEntityAsync(It.IsAny<string>(), It.IsAny<string>(), false))
    .ReturnsAsync(entity);
```

## 🧱 Test Structure Best Practices

### Base Class Setup
```csharp
public class MyServiceTests : MockerTestBase<MyService>
{
    // Constructor for setup before component creation
    public MyServiceTests() : base(setupMocks: mocks =>
    {
        // Setup global mocks here
        mocks.GetMock<IGlobalService>().SetupAllProperties();
    })
    { }

    // Override for post-construction setup
    protected override Action<MyService> CreatedComponentAction => component =>
    {
        // Setup component after creation
        component.InitializeSettings();
    };
}
```

## 🎭 Mocking Rules & Patterns

### Core Rules
- **Always use `Mocks.GetMock<T>()`** to retrieve mocks
- **All interface dependencies are auto-mocked** by `MockerTestBase<T>`
- **Use `[Fact]` for xUnit tests** (primary framework)
- **Match method signatures exactly** in `Setup()` and `Verify()` calls
- **Only mock interfaces or virtual members** - not sealed classes

### Mock Access Patterns
```csharp
// Get or create a mock
var mock = Mocks.GetMock<IService>();

// Get mock with setup action
var mock = Mocks.GetMock<IService>(m => m.SetupAllProperties());

// Verify mock was used
Mocks.GetMock<IService>().Verify(x => x.DoSomething(), Times.Once);
```

## 📑 File Organization

### Naming Conventions
- **Test files**: `{ClassUnderTest}Tests.cs`
- **Test methods**: `MethodName_ShouldExpectedBehavior_WhenCondition`
- **Test classes**: Inherit from `MockerTestBase<T>`

### Using Statements
```csharp
using FastMoq;
using FastMoq.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
```

## 🔍 Advanced Verification Patterns

### Logger Verification (Multiple Overloads)
```csharp
// Simple message verification
Mocks.GetMock<ILogger>().VerifyLogger(LogLevel.Information, "message");

// With exception
Mocks.GetMock<ILogger>().VerifyLogger(LogLevel.Error, "error message", exception);

// With specific times
Mocks.GetMock<ILogger>().VerifyLogger(LogLevel.Warning, "warning", 2);

// Generic logger verification
Mocks.GetMock<ILogger<MyService>>().VerifyLogger(LogLevel.Debug, "debug info");
```

### Method Invocation Helpers
```csharp
// Call method with auto-parameter injection
Mocks.CallMethod(Component.ProcessOrder, orderId);

// Call async methods
await Mocks.CallMethod(Component.ProcessOrderAsync, orderId);

// Call with complex parameter matching
Mocks.CallMethod(Component.ValidateData, 
    data: testData, 
    validator: Mocks.GetMock<IValidator>().Object);
```

## 🧪 Test Coverage Guidelines

### What to Test
- **Public methods** - Always test public API
- **Internal methods** - Test critical internal logic when using non-generic `MockerTestBase`
- **Exception scenarios** - Test error handling and edge cases
- **Logging behavior** - Verify important log messages
- **Mock interactions** - Verify dependencies are called correctly

### Test Categories
```csharp
[Fact] // Standard unit test
[Theory] // Parameterized tests
[InlineData(1, "test")]
[InlineData(2, "other")]
public void Method_ShouldBehave_WithDifferentInputs(int id, string name)
{
    // Test implementation
}
```

## 💡 FastMoq-Specific Features

### Auto-Injection
```csharp
// Dependencies are automatically injected
public class OrderServiceTests : MockerTestBase<OrderService>
{
    [Fact]
    public void ProcessOrder_UsesInjectedRepository()
    {
        // IRepository is automatically mocked and injected
        Component.ProcessOrder(123);
        
        Mocks.GetMock<IRepository>()
            .Verify(x => x.GetOrder(123), Times.Once);
    }
}
```

### Resource Helpers
```csharp
// File system mocking
Mocks.AddFileSystemAbstractionMapping();

// Database context mocking  
var dbContextMock = Mocks.GetMockDbContext<TestDbContext>();
```

This ensures your tests follow FastMoq patterns and leverage the framework's auto-injection capabilities effectively.
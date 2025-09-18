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
   - Use `IMockingProvider` methods for verification and mock creation.
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
5. **Document new APIs**  
   - Add XML doc comments for public methods.
   - Include usage examples in Milestone docs.

## 🚫 Avoid
- Hard‑coding Moq calls in shared/core code.
- Duplicating constructor resolution logic — always centralize in `MockerConstructionHelper`.
- Adding provider‑specific logic to `MockerTestBase` classes.
- Breaking existing public API signatures.

## 📚 Reference Examples
- See `FastMoq.Tests` for usage of `MockerTestBase<T>` and `VerifyLogger`.
- See `FastMoq.Tests.Web` for Blazor component testing patterns.
- See `FastMoq.Tests.Blazor` for UI‑specific injection and verification.

## 🧪 Testing & Validation
- All new features must have unit tests in the appropriate `FastMoq.Tests*` project.
- Run `dotnet test` before committing.
- Ensure tests pass for all target frameworks.
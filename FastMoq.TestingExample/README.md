# FastMoq Testing Examples

This project is the smallest repo-local place to see FastMoq patterns in executable form.

## Real-world examples

- `OrderProcessingServiceExamples` shows a business workflow with repository, inventory, payment, and logger dependencies.
- `CustomerImportServiceExamples` shows the built-in `IFileSystem` support with `MockFileSystem` plus parser and repository collaborators.
- `InvoiceReminderServiceScenarioExamples` shows the fluent `Scenario.With(...).When(...).Then(...).Verify(...)` style.
- `OptionalParameterResolutionExamples` shows the explicit optional-parameter model through `ComponentCreationFlags` and `InvocationOptions`.

## Basic examples

- `ExampleTests.cs` keeps the smaller constructor and `MockerTestBase<T>` samples for quick orientation.

## How to use this project

- Read the service under test first in `RealWorldExampleServices.cs`.
- Open the matching tests in `RealWorldExampleTests.cs` to see FastMoq setup and verification patterns.
- Run `dotnet test .\FastMoq.TestingExample\FastMoq.TestingExample.csproj` after changes to keep the examples trustworthy.

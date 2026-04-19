# FastMoq - Copilot Instructions

## Project Overview
FastMoq is a provider-first .NET testing framework for auto-mocking, dependency injection, and test-focused object creation.
It wraps and extends mocking providers to:
- auto-create and inject dependencies into components under test
- support both public and internal/protected test flows via `MockerTestBase<T>` and `MockerTestBase`
- provide fluent scenario building through `ScenarioBuilder<T>`
- offer provider-neutral verification plus targeted compatibility shims where needed

## Tech Stack
- Language: C# targeting .NET 8, .NET 9, and .NET 10
- Test framework: xUnit
- Provider direction: `reflection` is the default runtime provider; Moq and NSubstitute are opt-in provider paths
- Key namespaces: `FastMoq.Core`, `FastMoq.Web`, `FastMoq.Web.Blazor`, `FastMoq.Providers`

## Key Files and Concepts
- `Mocker.cs` - core tracked mock registry and creation logic
- `MockerTestBase<T>` and `MockerTestBase` - primary test base classes
- `MockerTestBase_Constructors.cs` - component creation flow and constructor-selection controls
- `MockerConstructionHelper` - shared constructor resolution logic
- `ServiceProviderTestExtensions.cs` - typed `IServiceProvider` and scope helpers
- `TestClassExtensions.cs` - logging and compatibility helpers
- `ScenarioBuilder<T>` - fluent scenario API
- `FastMoq.Core/Providers/*` - provider abstraction, selection, and verification primitives

## Canonical Documentation
Use repo-local markdown under `docs/` as the source of truth for the current branch.

Primary references:
- `README.md`
- `docs/getting-started/README.md`
- `docs/getting-started/testing-guide.md`
- `docs/getting-started/provider-selection.md`
- `docs/getting-started/provider-capabilities.md`
- `docs/migration/README.md`
- `docs/migration/api-replacements-and-exceptions.md`
- `docs/cookbook/README.md`
- `docs/samples/README.md`
- `docs/samples/testing-examples.md`
- `docs/roadmap/README.md`

Notes:
- The hosted Help site is generated from the repo docs and can lag the current branch.
- If this file and the repo docs diverge, update the docs or this file instead of adding more duplicated tutorial content here.
- Prefer linking to the canonical docs above over pasting long examples into this file.

## Coding Guidelines for Copilot
When generating or editing code:
1. Provider-first defaults
   - In shared and core code, use provider-agnostic abstractions such as `IMockingProvider`, `IFastMock<T>`, `Verify(...)`, `VerifyLogged(...)`, and `TimesSpec`.
   - For new or touched tests, prefer `GetOrCreateMock<T>()`, `AddType(...)`, `CreateStandaloneFastMock<T>()`, and `CreateFastMock<T>()` based on intent.
   - Treat `GetMock<T>()`, `GetRequiredMock<T>()`, `VerifyLogger(...)`, `Initialize<T>(...)`, `Strict`, and raw `Moq.Mock<T>`-shaped flows as legacy compatibility surfaces, not the default direction.
   - Only use Moq APIs inside `FastMoq.Provider.Moq` or Moq-specific tests.
2. Reuse shared helpers
   - For component creation, always call `MockerConstructionHelper.CreateInstance`.
   - When a test must choose a constructor, prefer `MockerTestBase<TComponent>.ComponentConstructorParameterTypes` first. For direct `Mocker` usage, prefer `CreateInstanceByType(...)`. Use `CreateComponentAction` only when constructor selection cannot be expressed as a signature.
   - When framework code needs a real `IServiceProvider` or `IServiceScopeFactory`, prefer `CreateTypedServiceProvider(...)` or `AddServiceProvider(...)` over mocked `IServiceProvider` shims or manually assembled scope-factory plumbing.
   - For logging verification, prefer `VerifyLogged(...)`. Use `VerifyLogger(...)` only when intentionally preserving compatibility behavior.
3. Follow naming conventions
   - Public API methods: PascalCase.
   - Private fields: `_camelCase`.
   - Test methods: `MethodName_ShouldExpectedBehavior_WhenCondition`.
4. Keep tests self-contained
   - Use `Component` or `ComponentAs<T>()` from the base classes.
   - Prefer `Scenario.With(Component)` for new scenario-based tests.
5. Use proper using statements
   - Add appropriate using statements at the top of files instead of fully qualified names.
   - Keep using statements organized and remove unused ones.
6. Document new or touched APIs
   - Add XML doc comments for all public, protected, and protected internal members you touch, including obsolete compatibility APIs that remain in the public surface.
   - When doing documentation cleanup, re-check the touched file for any remaining undocumented public, protected, or protected internal members before stopping.
7. Static analysis compliance
   - Honor the rules in the Static Analysis Rules section below.
8. Keep edits task-bound
   - Progress updates must stay grounded in the current task, files, and findings.
   - When a patch is too broad, split it into smaller targeted edits and complete local formatting cleanup before stopping.
   - If you touch a large partial class, preserve the surrounding style and fix any malformed XML or indentation introduced in the edited region.
9. Keep package and solution topology aligned
   - `FastMoq/FastMoq.csproj` must directly include every public release package project.
   - When a non-test public release project is added, removed, or renamed, update both `FastMoq.sln` and `FastMoq-Release.sln` in the same change.
   - Test-only or example-only project changes belong in `FastMoq.sln` but do not require changes to `FastMoq-Release.sln`.

## Static Analysis Rules
Apply these consistently in generated or refactored C# code:
- S121: Always use curly braces for control structures.
- S122: One statement per line.
- S6608: Prefer modern, readable C# forms when they improve clarity without sacrificing maintainability.

Notes:
- Do not introduce breaking changes solely to satisfy these rules.
- If a rule conflicts with the existing public API shape or a widely used repository pattern, prefer backward compatibility and leave a short TODO only when necessary.

## Quick Decision Guide
Use these defaults unless the task explicitly targets a compatibility path:
- `GetOrCreateMock<T>()` for the normal tracked dependency in the current `Mocker`
- `CreateStandaloneFastMock<T>()` for a detached extra mock handle or manual wiring outside the tracked graph
- `CreateFastMock<T>()` only when you intentionally want a new tracked registration added immediately
- `AddType(...)` when the cleaner move is to register a fixed fake, stub, factory, or real instance instead of a mock setup chain
- `Verify(...)`, `VerifyNoOtherCalls(...)`, `VerifyLogged(...)`, and `TimesSpec` for provider-neutral verification
- `GetMock<T>()` or `GetRequiredMock<T>()` only when preserving legacy Moq-shaped code is materially cheaper than rewriting the touched test

If a test still needs Moq-specific setup convenience in v4:
- add `FastMoq.Provider.Moq`
- import `FastMoq.Providers.MoqProvider`
- prefer `GetOrCreateMock<T>()` plus the provider-package extensions before expanding raw `GetMock<T>()` usage further

If the constructor under test takes the same interface more than once:
- prefer keyed registrations or separate mock scopes instead of a single unkeyed mock
- use the migration and testing guides above for keyed dependency patterns

## Reference Sources
Use these as examples of current patterns:
- `FastMoq.Tests` for current `MockerTestBase<T>` usage
- `FastMoq.Tests.Web` for web and Blazor-adjacent helpers
- `FastMoq.Tests.Blazor` for UI-specific test flows
- `docs/samples` for repo-backed sample applications and examples

## Testing and Validation
- All behavior changes should include tests in the appropriate `FastMoq.Tests*` project.
- Run targeted `dotnet test` commands before committing code changes.
- When touching docs or DocFX assets that affect published output, rebuild with `scripts/Generate-ApiDocs.ps1`.
- Ensure tests or builds pass for the relevant target frameworks.

## Avoid
- Hard-coding Moq calls in shared/core code
- Duplicating constructor resolution logic
- Adding provider-specific logic to `MockerTestBase` classes
- Breaking existing public API signatures
- Leaving touched public, protected, or protected internal members undocumented because they are obsolete or compatibility-only
- Treating `GetMock<T>()` as the recommended default API
- Using the hosted Help output as the branch source of truth
- Posting unrelated status text while working

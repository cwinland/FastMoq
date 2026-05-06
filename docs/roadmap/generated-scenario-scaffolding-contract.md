# Generated Scenario Scaffolding Contract

This page captures the current design contract for generator-stable scenario and suite scaffolding in FastMoq. It is the canonical repo-local design artifact for [#126](https://github.com/cwinland/FastMoq/issues/126), and the contract described here now has a narrow implementation in the repo.

This page remains primarily a contract and API-design artifact, but the repo now emits a narrow first scenario/scaffold implementation for explicit partial `MockerTestBase<TComponent>` targets. The supported and deferred shapes below describe that current implementation boundary.

For the shared generated-test settings and extensibility contract behind [#162](https://github.com/cwinland/FastMoq/issues/162), see [Generated test settings design](./generated-test-settings.md).

## Current Supported Scope

The current supported [#136](https://github.com/cwinland/FastMoq/issues/136) scaffold slice is narrow:

- generated scenario entry points are emitted inside explicit partial `MockerTestBase<TComponent>` harness targets
- generated suite scaffolding means shared setup composition inside those harnesses through `ConfigureGeneratedMockerPolicy`, `ConfigureGeneratedMocks`, and `AfterGeneratedComponentCreated`
- generated sync, async, and continued-assertion expected-exception entry points build on the existing `ScenarioBuilder<T>` pipeline
- full generated test classes, standalone generated suite types, and consumer-configurable settings-driven scaffold shapes remain deferred

## Purpose And Non-Goals

This slice exists to define the stable runtime contract that later generated scenario scaffolds should target.

The goals are:

- map generated scenario phases to existing FastMoq-owned runtime APIs
- define the first regeneration-safe customization seams for generated scenario output
- keep generated scenario flow provider-first and centered on existing FastMoq surfaces
- preserve the settled ownership boundaries from [#125](https://github.com/cwinland/FastMoq/issues/125), [#122](https://github.com/cwinland/FastMoq/issues/122), and [#162](https://github.com/cwinland/FastMoq/issues/162)

This slice does not:

- implement source generation for scenario or suite scaffolds
- redesign `ScenarioBuilder<T>` into a separate scenario runtime model
- reopen constructor-selection, graph metadata, or harness bootstrap contracts already settled in `#125` and `#122`
- settle the helper-family narrowing matrix for logging, HTTP, Azure, Azure Functions, or typed DI beyond the boundaries needed to keep the scenario contract narrow
- replace the settings, hook-emission-style, or regeneration-policy ownership already documented in `#162`

## Current Runtime Anchors

The first scenario-scaffolding contract should target the existing public FastMoq runtime surface instead of inventing a second scenario abstraction.

Primary anchors:

- `ScenarioBuilder<T>.With(...)` for arrange steps
- `ScenarioBuilder<T>.When(...)` for normal act steps
- `ScenarioBuilder<T>.WhenThrows<TException>(...)` for expected-exception act steps that still allow trailing assertions
- `ScenarioBuilder<T>.Then(...)` for assertion steps
- `ScenarioBuilder<T>.Verify<TMock>(...)` and `VerifyNoOtherCalls<TMock>()` for provider-neutral verification steps
- `ScenarioBuilder<T>.Execute()`, `ExecuteAsync()`, `ExecuteThrows<TException>()`, and `ExecuteThrowsAsync<TException>()` for execution semantics
- `MockerTestBase<TComponent>.Scenario` as the default scenario builder entry point for generated test-base scaffolds
- `MockerTestBase<TComponent>.SetupMocksAction`, `CreateComponentAction`, `CreatedComponentAction`, and `ConfigureMockerPolicy` as the surrounding component and mocker composition seams
- `MockerTestBase<TComponent>.ComponentConstructorParameterTypes`, `CreateComponentConstructionRequest()`, `GetComponentConstructionPlan()`, and `GetComponentHarnessBootstrapDescriptor()` as the settled harness and construction metadata hooks beneath the scenario layer
- `Mocks.VerifyLogged(...)`, `VerifyLoggedOnce(...)`, and `VerifyNotLogged(...)` as the existing provider-neutral logging verification surface when generated scaffolds need log assertions

## First Supported Scaffold Shape

The current supported scaffold shape stays narrow and method-centric:

- generated scenario methods live inside generated partial `MockerTestBase<TComponent>`-based test types
- generated suite scaffolding for [#136](https://github.com/cwinland/FastMoq/issues/136) means suite-level shared setup regions and post-creation hooks inside the same generated partial harness, not standalone scenario or suite container types
- generated scenario methods build on the existing `Scenario` property or `Mocks.Scenario(Component)` flow instead of creating a new standalone scenario-class abstraction
- current generated entry points cover sync, async, and continued-assertion expected-exception flows
- generated scenario methods may consult `GetComponentConstructionPlan()` or `GetComponentHarnessBootstrapDescriptor()` when constructor or bootstrap metadata needs to shape the scaffold, but the scenario contract itself stays above the graph and harness layer

This keeps the first contract aligned with the current runtime model and avoids creating a second compatibility-heavy scenario surface beside `ScenarioBuilder<T>`.

## Phase-To-API Mapping

| Scaffold phase | Primary runtime APIs | Contract stance |
| --- | --- | --- |
| Arrange | `With(...)`, `SetupMocksAction`, `CreatedComponentAction`, `ConfigureMockerPolicy` | Generated scaffolds may use `With(...)` for scenario-local arrangement and existing test-base hooks for component-wide setup. |
| Act | `When(...)` | The normal action path for generated scenario methods. |
| Expected exception | `WhenThrows<TException>(...)`, `ExecuteThrows<TException>()`, `ExecuteThrowsAsync<TException>()` | The contract must preserve both continued-assertion and exception-object assertion flows. |
| Assert | `Then(...)` | Generated assertions should stay inside the normal scenario assert pipeline. |
| Verify | `Verify<TMock>(...)`, `VerifyNoOtherCalls<TMock>()`, `Mocks.VerifyLogged(...)` | Provider-neutral verification stays first-class. Logging stays on the existing mocker helper surface unless a later concrete generator need proves otherwise. |
| Execute | `Execute()`, `ExecuteAsync()` | Sync versus async execution remains a consumer choice shaped by `#162` settings and later scaffold implementation, not by local ad hoc defaults. |

## Regeneration-Safe Customization Model

The scenario contract must match the first extensibility direction already settled in `#162`:

- generated source remains generator-owned and replaceable
- user customization belongs in companion partials or explicit generated hook seams, not in edited generated output
- the first hook-emission model stays constrained to `GeneratedHookEmissionStyle.None` and `GeneratedHookEmissionStyle.CompanionPartialHooks`
- `#126` defines the scenario hook roles that later scaffolds need; `#136` chooses the concrete emitted members that realize those roles while preserving regeneration safety

The current implementation materializes those roles through companion partial members named `ConfigureGeneratedMockerPolicy`, `ConfigureGeneratedMocks`, `AfterGeneratedComponentCreated`, `ArrangeGeneratedScenario`, `ActGeneratedScenario`, `ExpectedExceptionGeneratedScenario<TException>`, `AssertGeneratedScenario`, and `VerifyGeneratedScenario`.

The first minimal hook-role set should stay explicit:

- `Arrange`
- `Act`
- `ExpectedException`
- `Assert`
- `Verify`

These are contract roles, not a second runtime API. The later generated scaffold implementation may materialize them as companion-partial methods, generated delegates, or similarly narrow named seams, but it must not collapse them into one opaque user-editable block.

## Verification And Logging Stance

The first in-band verification path for generated scenarios is provider-neutral FastMoq verification:

- use `ScenarioBuilder<T>.Verify<TMock>(...)` for explicit interaction assertions
- use `ScenarioBuilder<T>.VerifyNoOtherCalls<TMock>()` for verification closure where appropriate
- prefer `Mocks.VerifyLogged(...)` and related helpers for provider-neutral logging assertions instead of adding logging-specific methods to `ScenarioBuilder<T>` in this design slice

That means generated scenarios stay provider-first by default while still allowing hand-written custom hooks to use narrower helper or provider-specific behavior where a consuming suite intentionally opts into that path.

## Async And Expected-Exception Semantics

The current scenario contract already supports mixed sync and async phases. The generated scenario contract should preserve that behavior rather than redefining it.

Key rules:

- arrange, act, and assert phases can each be synchronous or asynchronous through the current `ScenarioBuilder<T>` overload set
- `WhenThrows<TException>(...)` is the expected-exception path when the act phase should fail but trailing assertions should still run
- `ExecuteThrows<TException>()` and `ExecuteThrowsAsync<TException>()` are the path when the exception object itself is the main assertion target
- sync versus async generated test-method syntax belongs to `#162` settings consumption and `#136` scaffold implementation, not to a separate `#126` runtime model
- the current generated scaffold implementation emits `ExecuteGeneratedExpectedExceptionScenarioScaffold<TException>()` and `ExecuteGeneratedExpectedExceptionScenarioScaffoldAsync<TException>()` for the continued-assertion `WhenThrows<TException>(...)` path
- generated wrappers for direct `ExecuteThrows<TException>()` and `ExecuteThrowsAsync<TException>()` exception-object inspection remain deferred; use a hand-written `Scenario` flow when the exception object itself is the primary assertion target

## Composition With Existing Contracts

`#126` depends on lower layers that are already settled and must not reopen them.

- `#125` remains the constructor-selection and public construction-planning contract boundary.
- `#122` remains the graph metadata and harness bootstrap implementation boundary.
- `#162` remains the shared settings, hook-emission-style, and regeneration-policy boundary.

In practice that means:

- generated scenarios may consume harness metadata, but they do not redefine constructor signatures, parameter-source categories, or graph shape
- generated scenarios must consume the shared `#162` settings vocabulary for hook emission, scaffold choice, framework syntax, runner/bootstrap targeting, naming, and regeneration behavior instead of inventing local defaults
- the scenario contract should remain stable even if later scaffold implementations support more than one framework syntax target or runner mode

## Explicit Deferrals

The following work stays outside `#126` even when it is closely related:

- helper-family narrowing and re-triage across logging, HTTP, Azure, Azure Functions, and typed DI stays in [#134](https://github.com/cwinland/FastMoq/issues/134) and is documented in [Generated helper family matrix](./generated-helper-family-matrix.md)
- the first concrete generated scenario and suite scaffolding implementation now lives in [#136](https://github.com/cwinland/FastMoq/issues/136)
- full generated tests from existing services and supported classes stays in [#123](https://github.com/cwinland/FastMoq/issues/123)
- analyzer-guided generation routing and missing-package suggestions stays in [#124](https://github.com/cwinland/FastMoq/issues/124)
- helper-builder generation stays in [#137](https://github.com/cwinland/FastMoq/issues/137)
- standalone generated suite types, settings-driven scaffold variants, and direct exception-object-returning generated scaffold wrappers remain explicit deferred cases from the first `#136` slice

If later discovery shows that one truly missing runtime seam blocks the contract, track that seam narrowly instead of silently widening `#126` into a broad implementation branch.

## Follow-On Boundaries

The immediate sequence after `#162` should stay explicit:

- `#126` defines the stable scenario-scaffolding contract
- `#134` classifies which helper families the scenario implementation may later depend on
- `#136` is the current narrow implementation slice: generated scenario execution plus suite-level shared setup inside explicit partial harness targets
- `#123` and `#124` follow only after the contract and scaffold layers are stable enough to consume instead of re-deriving local defaults
- `#137` remains later and conditional rather than part of the immediate next-step chain

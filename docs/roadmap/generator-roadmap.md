# FastMoq Generator Roadmap And Design

This page captures the current v5 direction for FastMoq code generation. It is the detailed design companion to the main [roadmap summary](./README.md), not a shipped feature guide.

This page is intentionally design-level only. It is appropriate for roadmap and implementation planning ahead of code, but it does not imply current shipped support.

FastMoq does not currently include traditional Roslyn source generators. The current repo ships analyzers and code fixes, but it does not emit new `.g.cs` files for mocks, dependency graphs, or test scaffolding.

## Current Baseline

Confirmed current state:

- `FastMoq.Analyzers` ships analyzers and code fixes for migration, provider-first authoring, package guidance, and helper adoption.
- The repo does not currently contain `ISourceGenerator` or `IIncrementalGenerator` implementations.
- There is no first-party compile-time generation of mocks, DI graphs, scenario scaffolding, or framework-helper builders.

That means code generation in v5 is net-new product surface rather than a minor extension of the current analyzer package.

## Public Issue Crosswalk

The current public backlog for this design is:

- [#120](https://github.com/cwinland/FastMoq/issues/120) `FastMoq.Generators` package shape and MVP contract
- [#121](https://github.com/cwinland/FastMoq/issues/121) runtime prerequisite umbrella for generator-targeted output
- [#146](https://github.com/cwinland/FastMoq/issues/146) analyzer guidance for exact-call provider-first setup helpers
- [#147](https://github.com/cwinland/FastMoq/issues/147) analyzer guidance for shared verification wrappers
- [#125](https://github.com/cwinland/FastMoq/issues/125) graph metadata hooks and constructor-selection primitives
- [#126](https://github.com/cwinland/FastMoq/issues/126) `ScenarioBuilder` scaffolding hooks for generated output
- [#127](https://github.com/cwinland/FastMoq/issues/127) package-detection and target-test-shape rules for generated tests
- [#134](https://github.com/cwinland/FastMoq/issues/134) blocking helper-surface normalization for logging, HTTP, Azure, Azure Functions, and typed DI
- [#122](https://github.com/cwinland/FastMoq/issues/122) compile-time graph and harness MVP
- [#136](https://github.com/cwinland/FastMoq/issues/136) generated scenario and suite scaffolding after the graph and harness MVP
- [#137](https://github.com/cwinland/FastMoq/issues/137) generator-backed framework-helper builders for repeated test patterns
- [#123](https://github.com/cwinland/FastMoq/issues/123) full provider-first test generation from existing services and supported classes
- [#124](https://github.com/cwinland/FastMoq/issues/124) analyzer-guided test generation and missing-package suggestions
- [#138](https://github.com/cwinland/FastMoq/issues/138) later provider-optimized generation evaluation
- [#139](https://github.com/cwinland/FastMoq/issues/139) later narrow compile-time fake or mock generation evaluation

Crosswalk summary:

- `#121` is the runtime-prerequisite umbrella.
- `#132`, `#133`, and `#135` are the narrower v4-style quick wins that are now implemented on the current milestone branch.
- `#146` and `#147` carry the near-term analyzer follow-up for those landed helper surfaces.
- `#120`, `#125`, `#126`, `#127`, and `#134` are the pre-v5 contract and blocking prerequisite slices.
- `#122`, `#136`, `#137`, `#123`, and `#124` are the phased implementation and authoring-flow outcomes once the prerequisites are stable enough.
- `#138` and `#139` are intentionally late evaluation tracks after the main provider-first generator story is already working.

## Product Positioning

The generator story should not be framed as "FastMoq can generate mocks too." That is smaller than the architecture and easier to compare directly against provider-owned dynamic mocking behavior.

The stronger FastMoq position is:

- compile-time provider-first test generation
- compile-time test graph generation
- generated full tests from existing services and other classes
- generated harness and scenario scaffolding
- generated framework-helper builders for repeated integration-style test patterns

That positioning fits what FastMoq already owns today:

- provider-neutral construction and verification surfaces
- DI-aware object creation
- framework-heavy test helpers
- analyzer-guided authoring and migration

## Design Principles

Any v5 generator work should follow these rules:

1. Target stable FastMoq-owned APIs first.
2. Prefer generated graphs, scaffolds, and builders before generated provider-specific mock behavior.
3. Keep provider-specific semantics explicit when they cannot be flattened cleanly.
4. Use generators to remove reflection, graph walking, and boilerplate where FastMoq already has runtime responsibility.
5. Avoid generating new compatibility-heavy wrappers that would keep older Moq-shaped usage alive longer than necessary.
6. Preserve a runtime fallback path when generated assets are not present.

## Biggest Selling Point

The flagship v5 message should be:

FastMoq can generate provider-first test graphs, harness scaffolding, and framework-helper builders at compile time while still letting suites choose Moq, NSubstitute, or reflection-backed runtime behavior where needed.

Why that matters:

- less reflection during test setup
- less runtime graph walking
- less repeated harness boilerplate
- faster creation of correct first-pass tests for existing code that still has no FastMoq coverage
- more deterministic graph construction
- a stronger path for trimming and AOT-sensitive environments where FastMoq-owned generated code can replace reflection-heavy paths

## Planned Generator Workstreams

### 1. Compile-time test graph and harness generation

This is the highest-value first workstream.

Primary outputs:

- generated constructor-chain metadata
- generated dependency ordering
- generated harness bootstrap for selected components under test
- generated tracked-dependency accessors or helpers
- optional generated constructor-selection metadata where FastMoq already owns the runtime policy

Why it goes first:

- it aligns directly with FastMoq's DI-aware object-creation value
- it reduces reflection and graph walking without promising provider-neutral mock semantics that may not exist
- it creates a visible performance and ergonomics story for larger solutions

### 2. Scenario and suite scaffolding

This workstream should build on generated graph metadata rather than replace it.

Primary outputs:

- generated scenario shell types or partials
- generated per-suite setup regions
- generated default verify helpers or scenario hooks
- generated migration-starting points for repeated patterns

Expected value:

- less repetitive suite boilerplate
- easier adoption of `ScenarioBuilder`-style flows
- clearer entry points for new tests in provider-first projects

### 3. Full-test generation from existing services and classes

This workstream should generate complete starting tests for real existing code, not just partial harness fragments.

Primary outputs:

- generated test classes for existing services, handlers, controllers, background services, and other supported component shapes
- generated arrange or act or assert skeletons that map to the current FastMoq package surfaces available in the consuming project
- generated package-aware imports and harness selection based on the installed FastMoq runtime and helper packages
- generated placeholders for unresolved seams when a full test cannot be completed safely

Scope rule:

- the generator should only emit tests that target FastMoq packages and helper surfaces already available in the current project unless the user explicitly accepts an analyzer-guided package addition path

Expected value:

- a large visible adoption win for existing projects that want to bootstrap provider-first tests quickly
- consistent first-pass tests that match FastMoq guidance instead of one-off local patterns
- a stronger story for moving from "no tests yet" to "generated provider-first tests plus manual refinement"

### 4. Framework-helper builders

This workstream should focus on high-friction areas where FastMoq already owns helper APIs.

Likely targets:

- `HttpClient` and request-helper builders
- Azure SDK client and pageable builders
- Azure Functions request or response builders
- logging verification helper builders
- typed `IServiceProvider` and scope bootstrap patterns

Expected value:

- lower friction in framework-heavy tests
- better reuse of existing FastMoq helper surfaces
- less local ad hoc builder code in consuming test suites

### 5. Analyzer-guided test generation and package suggestions

Generator work should be paired with analyzers so the authoring flow can start from real code that is not yet covered.

Primary analyzer behaviors:

- detect existing services or other supported classes that appear to have no neighboring FastMoq-style tests
- suggest generating a provider-first test when the current project already references the needed FastMoq package set
- suggest adding the correct test or helper packages when the target test shape depends on a missing FastMoq surface such as web, database, Azure, or Azure Functions helpers
- avoid firing when the generator would have to guess at unavailable helper packages or unsupported test frameworks

Expected value:

- makes generators discoverable from normal code-authoring flow instead of only from templates or docs
- connects package-aware analyzer guidance with the new code-generation path
- helps teams bootstrap missing tests in a controlled, FastMoq-owned way

### 6. Provider-optimized generation

Provider-specific optimization should be a later step, not the foundation.

Possible outputs:

- Moq-oriented generated setup or bootstrap fast paths
- NSubstitute-oriented generated bootstrap or arrangement helpers
- reflection-provider invocation-map generation where FastMoq fully owns the implementation

This work should only move once the provider-first generator contract is stable.

### 7. Narrow compile-time fake or mock generation

This is the most exploratory workstream and should stay last in priority.

Why it is later:

- direct compile-time mock generation risks overpromising provider-neutral behavior that still depends on provider-owned semantics
- it is easier to defend generated graphs and scaffolds publicly than broad generated-mock claims
- FastMoq's differentiator is broader than fake-object emission alone

If pursued, it should start with the narrowest cases where FastMoq can keep the behavior predictable and provider-first.

## Required Runtime Precondition Work

Generator work should not outpace the runtime API surface it depends on.

Before or alongside generator implementation, FastMoq likely needs:

- expanded provider-first setup helpers for common simple arrangements
- expanded provider-first verification helpers where a shared abstraction is still clear and stable
- stable graph metadata hooks or reusable constructor-selection primitives for generator output
- clearer scenario-builder extension points for generated scaffolding
- generator-friendly helper surfaces for logging, HTTP, Azure, Azure Functions, and DI-heavy setup
- clear package-detection and target-test-shape rules so generated tests do not assume helper packages that are not referenced

Without those runtime targets, generators would be forced to emit provider-native or compatibility-heavy code too early.

### Likely v4 quick wins

Some runtime preparation work was narrow enough to land before v5 without forcing a wider public-contract redesign.

Completed on the current milestone branch:

- expanded provider-first setup helpers for common simple arrangements
- expanded provider-first verification helpers where a shared abstraction is still clear and stable
- small helper-surface cleanups for logging, HTTP, or typed DI setup where the FastMoq-owned runtime surface already exists and only needed a more generator-friendly shape

These landed early because they improve normal authoring even before source generators ship. The remaining near-term follow-up is narrower analyzer guidance in [#146](https://github.com/cwinland/FastMoq/issues/146) and [#147](https://github.com/cwinland/FastMoq/issues/147), plus broader helper normalization in [#134](https://github.com/cwinland/FastMoq/issues/134).

### Likely v5 blocking prerequisites

Some work is more foundational and should be treated as explicit prerequisites for the generator implementation itself.

Blocking areas:

- stable graph metadata hooks or reusable constructor-selection primitives for generator output
- clearer scenario-builder extension points for generated scaffolding
- clear package-detection and target-test-shape rules so generated tests do not assume helper packages that are not referenced
- broader generator-friendly helper normalization across logging, HTTP, Azure, Azure Functions, and DI-heavy setup when generated output needs those shapes to stay consistent across projects

Those pieces are less about convenience and more about preventing the generator from emitting unstable, provider-native, or package-guessing code.

## Package Shape And MVP Contract

Issue [#120](https://github.com/cwinland/FastMoq/issues/120) is the contract slice that fixes the package and MVP boundaries for the generator line.

The current v5 contract for that slice is:

- `FastMoq.Generators` is the dedicated source-generator package.
- `FastMoq.Analyzers` remains a separate analyzer and code-fix package.
- the aggregate `FastMoq` package should include the generator path once the generator line ships, because the aggregate package is the umbrella FastMoq experience
- `FastMoq.Core` should not include the source-generator implementation, because core stays the lighter provider-neutral runtime and split-package base
- any shared generator-facing runtime contract should stay as small as possible; do not move broad generator contracts into `FastMoq.Abstractions` by default
- if a runtime-visible marker or minimal shared contract truly must cross package boundaries, prefer the smallest viable surface in `FastMoq.Abstractions` or a tiny generator companion contract package instead of expanding abstractions broadly up front

That separation keeps the current migration analyzer story intact while allowing generator-specific incremental compilation work to evolve independently.

## Install And Opt-In Model

The install and opt-in story for the first generator slice is:

- installing the aggregate `FastMoq` package should make the generator capability available by default once the generator line ships
- split-package consumers should add `FastMoq.Generators` explicitly when they want generation on top of `FastMoq.Core` and any helper or provider packages they already chose
- package installation enables generation capability, but generation targets should still be explicit rather than blanket automatic for every eligible type in a project
- generator-triggering flow can come from supported markers, declared generation targets, or later analyzer-guided authoring, but the package alone should not imply broad surprise output across an existing suite

Broader project-level or suite-level settings that express preferred generated test direction, scaffold style, or harness shape are later work. They should not be treated as part of the #125 constructor-contract slice or the first #122 graph and harness MVP.

This keeps the aggregate install convenient without making generation feel like unavoidable background behavior.

## Runtime Fallback Contract

The generator line is an accelerator over FastMoq-owned runtime behavior, not a second mandatory execution mode.

For the first generator slice:

- unsupported targets should stay on the supported runtime FastMoq path
- disabled generation paths should stay on the supported runtime FastMoq path
- missing generated assets should stay on the supported runtime FastMoq path
- stale or out-of-date generated assets should not redefine runtime semantics; the supported fallback remains the normal runtime FastMoq path until stricter validation modes are intentionally introduced later

This fallback rule is important because the first public promise is reduced reflection and boilerplate where FastMoq already owns the runtime responsibility, not hard dependence on generated artifacts.

## MVP Boundary For The First Generator Slice

The first supported generator outputs are intentionally narrow.

In scope for the MVP contract:

- generated test graph metadata
- generated harness bootstrap for selected components under test
- the minimum supporting metadata needed for the graph and harness story to target FastMoq-owned runtime APIs cleanly

Out of scope for this contract slice:

- generated scenario or suite scaffolding
- generated full tests from existing services or other supported classes
- generated framework-helper builders for HTTP, Azure, Azure Functions, logging, or typed-DI-heavy setup patterns
- provider-optimized generation layers
- broad compile-time fake or mock generation
- broad AOT or trimming promises beyond the limited reflection-reduction story the first slice can actually prove

That MVP boundary is what issue `#122` is allowed to implement first.

## Current Constructor Contract Direction For #125

Issue [#121](https://github.com/cwinland/FastMoq/issues/121) remains the umbrella tracker for prerequisite status. Issue [#125](https://github.com/cwinland/FastMoq/issues/125) is the active blocking contract slice before the graph and harness MVP in [#122](https://github.com/cwinland/FastMoq/issues/122).

The current proposed public surface for that slice is:

- `InstanceConstructionRequest`
- `Type RequestedType`
- `Type?[]? ConstructorParameterTypes`
- `bool? PublicOnly`
- `OptionalParameterResolutionMode OptionalParameterResolution`
- `ConstructorAmbiguityBehavior ConstructorAmbiguityBehavior`
- `InstanceConstructionPlan`
- `Type RequestedType`
- `Type ResolvedType`
- `bool UsedNonPublicConstructor`
- `bool UsedPreferredConstructorAttribute`
- `bool UsedAmbiguityFallback`
- `IReadOnlyList<InstanceConstructionParameterPlan> Parameters`
- `InstanceConstructionParameterPlan`
- `string Name`
- `Type ParameterType`
- `int Position`
- `bool IsOptional`
- `OptionalParameterResolutionMode OptionalParameterResolution`
- `object? ServiceKey`
- `InstanceConstructionParameterSource Source`

The current proposed first-slice enum members for `InstanceConstructionParameterSource` are:

- `CustomRegistration`
- `KnownType`
- `KeyedService`
- `AutoMock`
- `ConstructedByMocker`
- `OptionalDefault`
- `TypeDefault`

Boundary rules for this slice:

- the public request model captures constructor-selection intent only
- the public resolved plan captures stable constructor-selection output only
- the first slice does not commit to a public executable-plan API such as `CreateInstance(InstanceConstructionPlan plan)`
- the first slice does not add new reflection-heavy contract fields such as raw `ConstructorInfo`, `ParameterInfo`, or executable argument values to the new plan types
- existing public diagnostics and runtime behavior, including current public reflection-metadata resolution paths, remain part of the compatibility boundary and should not be demoted just to make the new contract cleaner

### #125 parity matrix for closure

Closing issue [#125](https://github.com/cwinland/FastMoq/issues/125) should require an explicit parity check for the constructor-selection cases the new contract is expected to describe:

- exact typed constructor signature selection
- explicit parameterless selection when an empty constructor-type list is requested
- public-only selection versus non-public fallback behavior
- optional-parameter behavior for both default-or-null and resolve-via-mocker modes
- ambiguity behavior, including both throw and prefer-parameterless paths
- preferred-constructor selection and invalid multiple-preferred-constructor cases
- keyed or special dependency resolution paths
- stable parameter-source categorization for `CustomRegistration`, `KnownType`, `KeyedService`, `AutoMock`, `ConstructedByMocker`, `OptionalDefault`, and `TypeDefault`

That parity matrix is part of the definition artifact for this slice. It does not require the generator implementation itself to land inside [#125](https://github.com/cwinland/FastMoq/issues/125), but it does require the contract text to make those expected behaviors explicit enough that later implementation issues can target them without reopening constructor-selection semantics.

## Suggested v5 Delivery Phases

### Phase 0: contract and package design

Define:

- package boundaries
- generator opt-in model
- runtime fallback behavior
- attribute or marker strategy
- baseline benchmark scenarios

Phase 0 ends once issue `#120` has fixed the package set, install and opt-in model, runtime fallback behavior, MVP boundary, and non-goals strongly enough that later implementation issues can cite those decisions directly instead of reopening them.

### Phase 1: graph and harness MVP

Ship:

- generated test graph metadata
- generated harness bootstrap for selected components
- initial benchmark coverage showing reduced reflection and setup overhead

### Phase 2: scenario and scaffold generation

Ship:

- generated suite or scenario scaffolds
- generated migration or new-test starting points
- analyzer guidance that can offer "move to generated scaffold" suggestions where appropriate

### Phase 3: full-test generation from existing code

Ship:

- generated full tests for supported existing services and other supported component shapes
- package-aware harness selection based on the FastMoq surfaces already referenced by the project
- analyzer suggestions that can offer "generate provider-first test" for supported untested code

### Phase 4: framework-helper builders

Ship:

- targeted builders for repeated HTTP, Azure, Azure Functions, logging, and DI-heavy setup patterns

### Phase 5: package-aware analyzer follow-up

Ship:

- analyzer guidance for supported untested code where the current project is missing the FastMoq package surface needed for the desired generated test shape
- suggestions that point to the correct package additions before generation is offered

### Phase 6: provider-optimized and narrower generated-fake work

Evaluate:

- provider-specific optimization layers
- narrow compile-time fake or mock generation where FastMoq can keep the semantics predictable

## Risks And Constraints

The largest design risks are:

- promising provider-neutral generated behavior where providers still differ materially
- coupling generator output too tightly to unstable runtime APIs
- turning source generation into a second compatibility surface instead of a provider-first accelerator
- overcommitting to AOT claims before the generated path is proven end to end
- generating low-value placeholder tests that look complete but do not map cleanly to the packages and helper surfaces actually installed in the project

Because of that, the safest public promise is generated provider-first graphs and scaffolding first, broader generated-mock claims later only if the model still holds.

## Explicit Non-Goals For The #120 Slice

Issue `#120` should not promise:

- provider-optimized generation semantics
- broad compile-time fake or mock generation
- blanket automatic generation for every eligible type as soon as a package is installed
- a requirement that generated assets must exist for FastMoq runtime behavior to work
- wider scenario, full-test, or helper-builder support before their dedicated prerequisite issues land

Those items remain follow-on work so the first contract stays aligned with the current runtime surface and does not outrun the prerequisite map.

## Recommended Issue Breakdown

The current doc plan now maps to these issue slices:

1. Define generator package shape, runtime contract, and MVP scope.
2. Coordinate the runtime prerequisite map across likely v4 quick wins and v5 blocking work.
3. Expand provider-first setup helpers for common simple arrangements.
4. Expand provider-first verification helpers where a shared abstraction is still clear and stable.
5. Tighten the existing logging, HTTP, and typed DI helper surfaces that are small enough to land as v4 quick wins.
6. Define graph metadata hooks and constructor-selection contracts for generator-targeted output.
7. Define `ScenarioBuilder` scaffolding hooks and regeneration-safe extension points for generated output.
8. Define package-detection and target-test-shape rules for package-aware generation.
9. Normalize the broader blocking helper surfaces for logging, HTTP, Azure, Azure Functions, and typed DI-heavy setup.
10. Implement compile-time test graph and harness generation MVP.
11. Implement generated scenario and suite scaffolding after the graph and harness MVP.
12. Implement generator-backed framework-helper builders for repeated test patterns.
13. Add full-test generation for supported existing services and other supported classes.
14. Add analyzer guidance for untested code plus package-aware suggestions before test generation.
15. Evaluate provider-optimized generation after the provider-first contract is stable.
16. Evaluate narrow compile-time fake or mock generation only for predictable provider-first cases.

Those issue slices keep design, runtime prerequisites, MVP implementation, and analyzer or scaffolding follow-up separate enough to plan and sequence clearly without hiding important prerequisite work inside umbrella wording.

# FastMoq Roadmap

This page summarizes the current public FastMoq direction for upcoming releases. It stays focused on future work, known major-version cleanup, and areas still under evaluation.

Only a small part of the release split is fixed right now. The main decided v5 items are the planned core packaging cleanup, obsolete-surface removal, and code-generation work. Other work may land in additional v4 releases or in a later major version depending on stability, package boundaries, and documentation readiness.

For current package behavior, provider availability, and already shipped features, use the getting-started, provider-selection, and what's-new docs instead of this page.

This page tracks public FastMoq product direction only. Internal maintainer work such as local test-stack migration, internal runner experiments, or other repository-only operational tasks is intentionally excluded even when that work still matters inside this repo.

## Near-term direction

### Provider-neutral release hardening

Near-term work will continue to sharpen the provider boundary so shared FastMoq behavior stays portable and provider-specific behavior stays explicit.

Active focus areas include:

- Further reduce reliance on Moq-shaped compatibility paths in shared core flows.
- Keep provider-native access available without requiring every provider to emulate Moq.
- Expand provider-first setup and verification methods where a shared FastMoq surface can stay clear and useful without hiding real provider differences.
- Improve packaging and provider-selection guidance for the current provider lineup, aggregate-package behavior, and optional provider packages.
- Tighten migration guidance for compatibility surfaces that remain in v4 but are not intended to define the long-term API shape.

### Analyzer expansion

The remaining non-web analyzer work stays on the near-term roadmap where the guidance is stable enough to stay low-noise and useful in everyday test authoring.

Likely analyzer work includes:

- Distinguishing between tests that should use `GetOrCreateMock<T>()` and tests that should intentionally replace resolution with `AddType(...)` or `AddKnownType(...)`.
- Guiding suites away from one-object-for-all-types service-provider shims once the typed `IServiceProvider` helper is available.
- Adding keyed-service diagnostics when same-type constructor dependencies should not collapse into one unkeyed test double.

### Test helpers and migration support

Helper and migration work remains active where common testing scenarios still need a first-party answer rather than documentation alone.

Current follow-up areas include:

- Analyzer follow-up for the newly landed provider-first setup and verification helper surfaces so supported rewrites can be suggested precisely without widening the shared runtime contract too far.
- Additional provider-neutral guidance for framework-heavy suites that combine typed `IServiceProvider` helpers with keyed services or framework-owned service graphs beyond the current scope-aware helper path.
- Azure Functions follow-up for analyzer guidance and broader tracked-property provider adoption beyond the shipped `FunctionContext.InstanceServices`, `CreateHttpRequestData(...)`, `CreateHttpResponseData(...)`, and replay-safe orchestration logging support.
- Focused migration guidance and examples for compatibility-only APIs that remain temporary rather than long-term patterns.

Current public issue anchors in this bucket include [#41](https://github.com/cwinland/FastMoq/issues/41) for reusable diagnostics snapshots, [#145](https://github.com/cwinland/FastMoq/issues/145) for broader structured runtime diagnostics, [#148](https://github.com/cwinland/FastMoq/issues/148) for replay-safe orchestration analyzer guidance, and [#150](https://github.com/cwinland/FastMoq/issues/150) for the remaining tracked-property provider-contract adoption work.

Examples, migration docs, and analyzer guidance are more likely to move first than broad new helper surfaces when the release split is still being evaluated.

### Documentation and examples

Documentation work remains part of the near-term plan because several provider-dependent and helper-heavy scenarios still need stronger release-facing guidance.

Likely documentation work includes:

- More provider-native examples beyond the current capability matrix.
- Expanded feature-parity guidance for provider-dependent sequence, call-order, event, and advanced setup scenarios where the current shared FastMoq surface intentionally stops short of a single abstraction.
- Focused migration notes for older Moq-heavy suites.
- Keyed-service guidance tied to new diagnostics and helper APIs.
- Examples for typed service-provider flows and broader Azure testing helpers, including HTTP-trigger request or response setup.
- Additional DbContext examples as those surfaces continue to harden.

## Later v5 work or timing still open

### Web and UI expansion

Some web-focused items remain intentionally outside the committed v4 scope because their timing depends on how narrow and stable the public web surface should be.

Potential follow-up areas include:

- Additional ASP.NET integration helpers beyond the current `HttpContext` and `HttpClient` patterns.
- MVC, minimal API, and non-Blazor convenience layers.
- More public web abstractions where they can be added without hard-coding framework assumptions too early.
- Richer provider-specific convenience layers once the shared provider contract is stable enough to support them cleanly.

### Blazor migration analyzers

Targeted analyzer guidance for older `FastMoq.Web` helper patterns is still planned, but its release target remains open.

Planned follow-up includes:

- Flagging older parameter setup patterns that should move toward `RenderParameter`.
- Flagging legacy nested-component targeting assumptions when the current rendered-component path is clearer.
- Pointing older helper usage toward the current `MockerBlazorTestBase<T>` guidance when that recommendation can be made precisely.

## Planned v5 Cleanup

### Core packaging and provider defaults

The current v5 direction is to keep `FastMoq.Core` provider-neutral and move bundled provider availability to the aggregate package instead of core.

Current public issue anchor in this bucket is [#140](https://github.com/cwinland/FastMoq/issues/140).

Planned follow-up includes:

- Removing the current built-in Moq provider path from `FastMoq.Core`.
- Keeping provider implementations such as Moq and NSubstitute available through the aggregate `FastMoq` package.
- Keeping selective-install workflows available through explicit provider packages.
- Tightening provider-selection guidance so package composition stays predictable as the core or aggregate boundaries change.

### Provider expansion

Provider expansion remains part of the current v5 direction once the provider boundary and package model are stable enough to support another first-party provider cleanly.

Planned follow-up includes:

- evaluating a first-party FakeItEasy provider as a distinct provider-expansion track rather than folding it into the generator backlog
- keeping parity docs explicit that FakeItEasy is currently a direct-framework comparison baseline, not a shipped FastMoq provider
- tracking the FakeItEasy provider work through [#128](https://github.com/cwinland/FastMoq/issues/128) instead of leaving it only in roadmap prose

### Obsolete and compatibility surface cleanup

A future major-version cleanup will continue reducing Moq-oriented compatibility members that remain only to ease migration.

The goal is a smaller, clearer public surface where provider-first APIs are the default path and compatibility shims are no longer carrying day-to-day guidance.

Current public issue anchor in this bucket is [#141](https://github.com/cwinland/FastMoq/issues/141).

### Code generation and scaffolding

FastMoq now contains a narrow first-party source-generator slice for explicit harness targets, but the broader code-generation work is still part of the current v5 direction.

The main value is not "generated mocks" in isolation. The stronger FastMoq-specific opportunity is compile-time provider-first test generation: generated test graphs, harness scaffolding, and framework-helper builders that reduce reflection, reduce boilerplate, and stay aligned with FastMoq-owned APIs.

Current public issue crosswalk:

- foundation and package-shape work: [#120](https://github.com/cwinland/FastMoq/issues/120), [#121](https://github.com/cwinland/FastMoq/issues/121), [#125](https://github.com/cwinland/FastMoq/issues/125), [#126](https://github.com/cwinland/FastMoq/issues/126) for the stable `ScenarioBuilder` scaffolding contract, [#127](https://github.com/cwinland/FastMoq/issues/127), and [#134](https://github.com/cwinland/FastMoq/issues/134)
- near-term helper preparation that can improve v4 authoring before generators ship: [#132](https://github.com/cwinland/FastMoq/issues/132), [#133](https://github.com/cwinland/FastMoq/issues/133), and [#135](https://github.com/cwinland/FastMoq/issues/135)
- completed first implementation-facing MVP: [#122](https://github.com/cwinland/FastMoq/issues/122)
- current shared generated-test settings and test-platform contract gate for later authoring flows: [#162](https://github.com/cwinland/FastMoq/issues/162)
- later implementation-facing outcomes after the shared `#162` contract settles: [#136](https://github.com/cwinland/FastMoq/issues/136) for scenario and suite scaffolding, [#123](https://github.com/cwinland/FastMoq/issues/123), [#137](https://github.com/cwinland/FastMoq/issues/137), and [#124](https://github.com/cwinland/FastMoq/issues/124)
- later evaluation tracks after the provider-first generator story is stable: [#138](https://github.com/cwinland/FastMoq/issues/138) and [#139](https://github.com/cwinland/FastMoq/issues/139)

Planned direction stays phased:

1. Compile-time test graph and harness generation.
2. Shared generated-test settings and test-platform contract.
3. Scenario and suite scaffolding.
4. Full generated tests from existing supported classes.
5. Framework-helper builders for repeated helper-heavy test patterns.
6. Analyzer-guided generation flow and package suggestions.
7. Provider-optimized or narrower generated-fake evaluation only after the shared contract is stable.

Current package and MVP contract direction for [#120](https://github.com/cwinland/FastMoq/issues/120):

- `FastMoq.Generators` is the dedicated source-generator package
- `FastMoq.Analyzers` remains separate from the source-generator implementation
- the aggregate `FastMoq` package should include the generator path once it ships, while `FastMoq.Core` stays the lighter provider-neutral runtime and does not include the source-generator implementation
- generator capability is install-enabled but target-explicit, not blanket automatic for every eligible type in a project
- unsupported, disabled, missing, or stale generated paths fall back to the supported FastMoq runtime path
- the first implementation-facing MVP is generated graph metadata and harness bootstrap only; scenario scaffolding, full generated tests, helper builders, provider-optimized generation, and compile-time fake generation remain later work
- broader project-level or suite-level generated-test preference settings and test-platform targets are intentionally later than the first MVP and now track through [#162](https://github.com/cwinland/FastMoq/issues/162)

Current constructor-contract direction for [#125](https://github.com/cwinland/FastMoq/issues/125):

- [#121](https://github.com/cwinland/FastMoq/issues/121) remains the umbrella tracker; [#125](https://github.com/cwinland/FastMoq/issues/125) is complete, and the public constructor-planning contract is now the settled runtime boundary that the first real [#122](https://github.com/cwinland/FastMoq/issues/122) generator output should target
- the proposed public contract is `InstanceConstructionRequest`, `InstanceConstructionPlan`, `InstanceConstructionParameterPlan`, and `InstanceConstructionParameterSource`, with `Mocker.CreateConstructionPlan(InstanceConstructionRequest request)` as the preferred entry point
- the first graph and harness MVP should keep that `Mocker` surface request-only; the first harness-side consumer can sit on `MockerTestBase<TComponent>` instead of adding a companion generic overload on `Mocker`
- the proposed first-slice parameter-source enum members are `CustomRegistration`, `KnownType`, `KeyedService`, `AutoMock`, `OptionalDefault`, and `TypeDefault`; `ConstructedByMocker` is deferred until the runtime model exposes a distinct recursive-construction parameter category
- the new contract stays narrow while existing public diagnostics, models, creation APIs, and current public reflection-metadata resolution behavior remain part of the compatibility boundary

Current implementation status for [#127](https://github.com/cwinland/FastMoq/issues/127):

- done: `FastMoq.Analyzers` now exposes a shared package-layout and target-test-shape matrix for aggregate, core-only, and split helper-package layouts
- done: supported generated test shapes are explicit for core, web, Blazor, database, Azure, and Azure Functions targets based on the referenced FastMoq package set
- done: `MissingHelperPackageAnalyzer` now consumes the same package matrix instead of ad hoc helper-package checks
- done: analyzer tests now cover the core-only, split-helper, and aggregate package layouts that later generator and analyzer flows need to respect
- next: continue [#122](https://github.com/cwinland/FastMoq/issues/122) with the first real source-generator output against the settled planning, graph, harness-bootstrap, and package-shape runtime contracts

Current implementation status for [#122](https://github.com/cwinland/FastMoq/issues/122):

- done: an internal construction-graph metadata model now layers over `Mocker.CreateConstructionPlan(...)` without widening the public planning API
- done: `MockerTestBase<TComponent>` has the first graph-facing harness consumer through `GetComponentConstructionGraph()`
- done: an internal harness-bootstrap descriptor now projects `ComponentCreationFlags`, ordered constructor-signature hooks, and explicit-request-override detection on top of the current graph metadata
- done: focused runtime coverage now proves both the root-node mapping and the first harness-bootstrap descriptor paths
- done: the repo now contains a dedicated `FastMoq.Generators` package and the first incremental generator path for explicit partial `MockerTestBase<TComponent>` targets, emitting constructor-signature metadata and harness bootstrap against the settled runtime contract
- done: generator-driver coverage now proves both the single-constructor path and the explicit-signature path compile cleanly while ambiguous multi-constructor targets stay on the normal runtime fallback path
- done: representative generated consuming scenarios now compile against real generated harness output, and parity tests now prove the generated path matches the supported runtime harness path for the same component shapes
- done: measured setup-path evidence is now recorded in [generated harness setup benchmark results](../benchmarks/results/generated-harness-setup-net8.md), where the generated bootstrap-descriptor path holds a slight edge over the runtime fallback path with effectively identical allocations on the richer single-constructor benchmark
- done: the current branch now completes the planned `#122` MVP slice; later generated-test settings and framework or runner targeting move to [#162](https://github.com/cwinland/FastMoq/issues/162) while later scaffolding and full-test generation stay in [#136](https://github.com/cwinland/FastMoq/issues/136), [#123](https://github.com/cwinland/FastMoq/issues/123), and [#124](https://github.com/cwinland/FastMoq/issues/124)
- next: settle the shared generated-test settings and test-platform model in [#162](https://github.com/cwinland/FastMoq/issues/162) before widening into framework-specific scaffolds, full generated tests, or analyzer entry points

For the current detailed direction, design constraints, and fuller generator issue mapping, see [Generator roadmap and design](./generator-roadmap.md).
For the shared generated-test settings contract behind [#162](https://github.com/cwinland/FastMoq/issues/162), see [Generated test settings design](./generated-test-settings.md).

### `MockOptional` retirement

FastMoq will continue moving optional-parameter guidance toward explicit controls such as `Mocker.OptionalParameterResolution`, `InvocationOptions`, and focused `MockerTestBase<TComponent>` construction overrides.

Current public issue anchor in this bucket is [#142](https://github.com/cwinland/FastMoq/issues/142).

Planned follow-up includes:

- Rewriting any remaining compatibility-only `MockOptional` examples.
- Exposing the explicit options model more directly where targeted helper APIs would benefit from it.
- Removing the `MockOptional` compatibility alias in `v5` once the migration path is complete.

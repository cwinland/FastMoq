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

Code generation remains part of the current v5 direction.

The main value is not "generated mocks" in isolation. The stronger FastMoq-specific opportunity is compile-time provider-first test generation: generated test graphs, harness scaffolding, and framework-helper builders that reduce reflection, reduce boilerplate, and stay aligned with FastMoq-owned APIs.

Current direction stays phased:

1. Compile-time test graph and harness generation.
2. Shared generated-test settings and test-platform contracts.
3. Stable scenario-scaffolding contracts and helper-boundary narrowing.
4. Scenario and suite scaffolding.
5. Broaden full generated tests and analyzer-guided generation beyond the current explicit-harness xUnit smoke-test slice.
6. Framework-helper builders for repeated helper-heavy test patterns when they justify a separate layer.
7. Provider-optimized or narrower generated-fake evaluation only after the shared contract is stable.

For the current detailed direction, implementation status, scope boundaries, and fuller generator issue mapping, see [Generator roadmap and design](./generator-roadmap.md).
For the shared generated-test settings contract behind [#162](https://github.com/cwinland/FastMoq/issues/162), see [Generated test settings design](./generated-test-settings.md).
For the scenario-scaffolding contract behind [#126](https://github.com/cwinland/FastMoq/issues/126), see [Generated scenario scaffolding contract](./generated-scenario-scaffolding-contract.md).
For the current helper-boundary contract behind [#134](https://github.com/cwinland/FastMoq/issues/134), see [Generated helper family matrix](./generated-helper-family-matrix.md).

### `MockOptional` retirement

FastMoq will continue moving optional-parameter guidance toward explicit controls such as `Mocker.OptionalParameterResolution`, `InvocationOptions`, and focused `MockerTestBase<TComponent>` construction overrides.

Current public issue anchor in this bucket is [#142](https://github.com/cwinland/FastMoq/issues/142).

Planned follow-up includes:

- Rewriting any remaining compatibility-only `MockOptional` examples.
- Exposing the explicit options model more directly where targeted helper APIs would benefit from it.
- Removing the `MockOptional` compatibility alias in `v5` once the migration path is complete.

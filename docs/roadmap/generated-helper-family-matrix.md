# Generated Helper Family Matrix

This page captures the current helper-family narrowing contract for generator-facing FastMoq helper surfaces. It is the canonical repo-local design artifact for [#134](https://github.com/cwinland/FastMoq/issues/134), not a shipped feature guide.

This page is intentionally docs-only. The repo does emit core scenario and suite scaffolding, but it does not yet emit helper-heavy or package-specific scaffold variants, and this page does not itself implement helper normalization.

For the shared generated-test settings contract behind [#162](https://github.com/cwinland/FastMoq/issues/162), see [Generated test settings design](./generated-test-settings.md).
For the scenario-scaffolding contract behind [#126](https://github.com/cwinland/FastMoq/issues/126), see [Generated scenario scaffolding contract](./generated-scenario-scaffolding-contract.md).

## Purpose And Non-Goals

This slice exists to answer one narrow question for later generated scenario or suite scaffolds: which current FastMoq helper-family entry points may the first scaffold implementation target safely, and under what package or provider conditions?

The goals are:

- classify the current helper-family matrix for the first consumer, [#136](https://github.com/cwinland/FastMoq/issues/136)
- identify which helper paths are generator-stable as-is
- identify which helper paths are usable only with explicit package or provider bounds
- identify which helper paths stay deferred from the first generated scenario or suite scaffold path
- split any truly missing normalization work into later follow-on issues instead of absorbing it into this docs pass

This slice does not:

- implement helper normalization in runtime code
- redesign the settled `#126` scenario contract or the settled `#162` settings contract
- reopen package-detection and target-shape logic already owned by [#127](https://github.com/cwinland/FastMoq/issues/127)
- define helper-builder generation strategy for [#137](https://github.com/cwinland/FastMoq/issues/137)
- define full generated-test behavior for [#123](https://github.com/cwinland/FastMoq/issues/123) or analyzer routing behavior for [#124](https://github.com/cwinland/FastMoq/issues/124)
- widen into broader observability, tracing, or diagnostics architecture work that belongs elsewhere

## How #136 Uses This Matrix

The first generated scenario or suite scaffold implementation should use this matrix as a hard boundary.

- the current supported `#136` scaffold slice stays inside core provider-neutral scenario and suite hooks and does not yet auto-emit helper-family-specific setup from this matrix
- `#136` may target helper families classified as `generator-stable as-is` directly.
- `#136` may target helper families classified as `generator-stable with explicit bounds` only when the documented package or provider conditions are satisfied.
- `#136` should not target helper families classified as `deferred from first scenario scaffolds`.
- If later generated output needs behavior outside the current stable helper entry points, that work should move to a later follow-on issue instead of quietly widening `#134`.

## Classification Model

Each helper family in this matrix is classified into one of four buckets.

| Classification | Meaning for later generator consumers |
| --- | --- |
| `generator-stable as-is` | The current public helper surface is explicit enough for the first generated scenario or suite scaffolds to target directly. |
| `generator-stable with explicit bounds` | The helper surface is usable for generation only under documented package, provider, or scenario-shape limits. |
| `deferred from first scenario scaffolds` | The helper surface exists, but the first scaffold implementation should not rely on it. |
| `requires follow-on implementation issue` | Later generated output may need this family, but the current public surface is not yet stable enough to target without new public-shape work. |

## Helper-Family Matrix

| Helper family | Primary entry points | Classification | First-slice guidance | Ownership notes |
| --- | --- | --- | --- | --- |
| Logging verification | `VerifyLogged(...)`, `VerifyLoggedOnce(...)`, `VerifyNotLogged(...)` | `generator-stable as-is` | Generated scaffolds may use the current provider-neutral logging verification helpers directly. | Reuses the shared verification line from `#133` and analyzer follow-up in `#147`. Broader observability remains outside this issue. |
| Logger-factory composition | `CreateLoggerFactory(...)`, `AddLoggerFactory(...)`, `AddCapturedLoggerFactory(...)` | `generator-stable as-is` | Generated scaffolds may use the current capture-backed logger-factory helpers directly for logging setup. | Reuses existing logging capture and verification surfaces rather than introducing a second abstraction. |
| Typed `IServiceProvider` and `IServiceScope` composition | `CreateTypedServiceProvider(...)`, `CreateTypedServiceScope(...)`, `AddTypedServiceProvider(...)`, `AddTypedServiceScope(...)` | `generator-stable as-is` | Generated scaffolds may rely on the typed provider and scope helpers when a real DI composition path is cleaner than direct registrations. | Reuses the narrow typed-DI quick-win line already stabilized before this slice. |
| ASP.NET Core principal and `HttpContext` helpers | `SetupClaimsPrincipal(...)`, `CreateHttpContext(...)`, `AddHttpContext(...)`, `AddHttpContextAccessor(...)`, `CreateControllerContext(...)` | `generator-stable with explicit bounds` | Generated scaffolds may use these helpers when `FastMoq.Web` is referenced and the generated target actually needs web or controller context composition. | Keep this row scoped to web-context composition, not to broader UI interaction helpers. |
| Azure configuration and typed provider convenience | `CreateAzureConfiguration(...)`, `AddAzureConfiguration(...)`, `CreateAzureServiceProvider(...)`, `AddAzureServiceProvider(...)` | `generator-stable with explicit bounds` | Generated scaffolds may use Azure configuration and typed-provider helpers when `FastMoq.Azure` is referenced. Prefer the Azure typed-provider convenience path over ad hoc lower-level composition where possible. | Package-aware targeting still belongs to `#127`. |
| Azure client, storage, and pageable helpers | `AddAzureClient(...)`, storage client registration helpers, `CreatePageable(...)`, `CreateAsyncPageable(...)` | `generator-stable with explicit bounds` | Generated scaffolds may use the current stable client and pageable helpers for first-pass scenarios, but should not assume keyed or multi-client normalization beyond what the current public surface already expresses. | Later keyed or broader client-shaping needs should split to a later follow-on issue instead of widening this slice. |
| Azure Functions HTTP and `FunctionContext` basics | `CreateHttpRequestData(...)`, `CreateHttpResponseData(...)`, `ReadBodyAsStringAsync(...)`, `ReadBodyAsJsonAsync<T>(...)`, `AddFunctionContextInvocationId(...)`, `CreateFunctionContextInstanceServices()` | `generator-stable with explicit bounds` | Generated scaffolds may use these helpers for HTTP-trigger and basic worker-context scenarios when `FastMoq.AzureFunctions` is referenced. | Reuses the narrower landed Azure Functions helper work, including `#107`. |
| Durable replay-safe logger creation via concrete orchestration context | `Mocker.AddTaskOrchestrationReplaySafeLogging(...)` before resolving `TaskOrchestrationContext` | `generator-stable with explicit bounds` | Generated scaffolds may use the concrete replay-safe logger helper path when the scenario only needs replay-safe logger creation and the helper is registered before resolution. | Keep the documented bounds explicit: logger creation is supported, broader orchestration activity behavior is not. |
| Durable replay-safe logger creation via tracked orchestration mock | `IFastMock.AddTaskOrchestrationReplaySafeLogging(...)` | `deferred from first scenario scaffolds` | The first scaffold implementation should not rely on the tracked orchestration-mock path. It is provider-constrained and does not support replay-state suppression on the tracked path. | Later expansion should align with the provider-contract follow-up already noted around tracked-property configuration support. |
| Blazor component interaction helpers | `MockerBlazorTestBase<TComponent>`, `IMockerBlazorTestHelpers<TComponent>` and related UI interaction helpers | `deferred from first scenario scaffolds` | The first helper matrix should not treat UI-component interaction helpers as required for the initial generated scenario or suite scaffold path. | Keep this for later web or UI expansion rather than mixing it into the first `#136` assumption set. |
| Broader observability, tracing, and diagnostics architecture | Structured trace or diagnostics architecture beyond current logging capture and verification | `requires follow-on implementation issue` | Do not make the first generated scaffold depend on a broader observability stack than the current repo-owned logging capture and verification helpers already provide. | Broader structured diagnostics or tracing remains outside this issue. |

## Package And Provider Constraints

This matrix does not replace package-aware or provider-aware capability checks.

- [#127](https://github.com/cwinland/FastMoq/issues/127) remains the authoritative package-detection and target-shape gate.
- Package-specific helper families may only be emitted when the referenced FastMoq package set makes that helper family valid for the current project.
- The Durable tracked-mock replay-safe logger path remains provider-constrained and should not be treated as provider-neutral scaffolding guidance.
- The concrete replay-safe orchestration helper remains intentionally narrow: it supports logger creation and replay-state assertions, not full orchestration activity, timer, event, or sub-orchestrator behavior.

## Dependency Boundaries

`#134` exists to narrow helper-family assumptions, not to re-own adjacent work that already landed or already has a narrower issue.

- [#132](https://github.com/cwinland/FastMoq/issues/132) owns the shared setup-helper expansion line for fixed returns, async returns, callbacks, and exception helpers.
- [#133](https://github.com/cwinland/FastMoq/issues/133) owns the shared verification-helper expansion line, including the current logging verification story.
- [#135](https://github.com/cwinland/FastMoq/issues/135) owns the earlier quick-win cleanup for logging, HTTP, and typed-DI entry points that this matrix now reuses rather than redesigns.
- [#107](https://github.com/cwinland/FastMoq/issues/107) owns the landed Azure Functions InvocationId and replay-safe logger guidance that this matrix now classifies for generator-facing use.
- [#145](https://github.com/cwinland/FastMoq/issues/145) or later observability work owns broader structured diagnostics or tracing concerns outside the current helper capture or verification surface.
- [#137](https://github.com/cwinland/FastMoq/issues/137) remains later helper-builder generation and should not be designed inside this narrowing pass.

## Split Criteria For Later Follow-On Work

`#134` should stop and point to later follow-on work when a generator need exceeds the current stable helper surface.

Open or route to a later issue when one of these becomes necessary:

- a helper family needs a new public entry point instead of relying on the current repo-owned helper surface
- the desired generated flow depends on keyed or multi-client semantics that the current helper API does not make explicit
- the desired generated flow depends on provider-native or protected-member behaviors that are not part of the shared provider-first helper guidance
- the desired generated flow depends on Azure Functions orchestration behavior beyond replay-safe logger creation
- the desired generated flow depends on Blazor UI interaction helpers rather than simple web-context composition
- the desired generated flow depends on broader structured observability or diagnostics architecture rather than the current logging capture helpers

## Follow-On Boundaries

The immediate follow-on order after this docs pass should remain explicit:

- `#134` ends once the helper-family matrix, package or provider bounds, and split criteria are documented and mirrored into the roadmap.
- the first `#136` implementation now emits generated scenario and suite scaffolding inside explicit partial harness targets while staying within the core contract and these matrix bounds.
- `#123` and `#124` should consume the same helper assumptions later instead of inventing local defaults or re-triaging helper families inside their own slices.
- `#137` remains later and conditional rather than part of the immediate `#126 -> #134 -> #136 -> #123/#124` chain.

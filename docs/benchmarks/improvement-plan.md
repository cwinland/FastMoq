# FastMoq Benchmark Improvement Plan

This plan is based on the current benchmark suite and the code paths exercised by the slowest scenarios.

## Current evidence

Measured on the current branch after the latest optimization pass:

- Full tracked service flow with the Moq provider: `942.7 us` versus `330.2 us` for direct Moq in the simple scenario.
- Larger tracked dependency graph with the Moq provider: `1.884 ms` versus `865.0 us` for direct Moq.
- Simple invocation-only flow after one-time setup: `1.278 us` versus `1.251 us` for direct Moq at `InvocationCount=1`, with identical allocation.
- Complex invocation-only flow after one-time setup: `3.631 us` versus `3.667 us` for direct Moq at `InvocationCount=1`, with identical allocation.
- Detached same-type doubles: `154.7 us` versus `102.2 us` for direct Moq.
- Lightweight provider-matrix interaction flow: `52.96 us` to `65.82 us` across `reflection`, `moq`, and `nsubstitute`.
- Tracked lookup microbench: `GetOrCreateLastTrackedMock` improved from about `194 ns` to about `23 ns`.
- Tracked creation microbench: `CreateServiceWithTrackedDependencies` improved from about `477 us` / `534 KB` to about `439 us` / `355 KB`.

The detached-handle, provider-matrix, and new invocation-only numbers are all much closer to the direct-provider baselines than the full tracked-service numbers are.

## Working hypothesis

The main remaining overhead is now more clearly in the tracked mock lifecycle and tracked object-creation path, not in post-setup invocation or provider-neutral verification alone.

The new invocation-only benchmarks show that once a service graph is already built, FastMoq and direct Moq are effectively tied in both runtime and allocation for the measured scenarios.

The current likely hotspots are:

1. `SetupFastMock(...)` still eagerly materializes and configures tracked mocks.
2. Full tracked-service creation still repeats dependency resolution work inside the same construction wave.
3. `AddProperties(...)` and object construction still re-enter `GetObject(...)` repeatedly when building larger graphs.
4. Provider-specific arrange costs inside the initial setup path may still dominate parts of the Moq-backed end-to-end flows.

## Completed in this round

1. Replaced unkeyed tracked mock list scans with a private type index while preserving `mockCollection` ordering for existing tests.
2. Cached inferred `InstanceModel` resolution and interface-to-concrete fallback results.
3. Cached injection field and property discovery.
4. Added a no-data fast path for `AddProperties(...)` and cached writable property lists for the common auto-population path.
5. Added invocation-only benchmarks that build services once and measure repeated business calls separately from setup.

These changes were validated against the main `FastMoq.Tests` project across `net8.0`, `net9.0`, and `net10.0` and then re-measured with the benchmark suite. A constructor metadata cache was tried and then removed because its microbenchmark win did not hold in the full tracked-service benchmarks.

## Priority order

### 1. Reduce duplicate object-resolution work during component creation

Evidence:

- `CreateServiceWithTrackedDependencies` is still much more expensive than isolated tracked mock creation.
- Larger service graphs repeatedly re-enter `GetObject(...)` and parameter resolution for the same construction wave.

Planned change:

- Add a scoped per-construction cache for already-resolved constructor parameter values.
- Avoid repeated `GetTypeModel(...)` and `GetObject(...)` churn for dependencies already resolved earlier in the same activation.

Expected effect:

- Narrow the remaining complex graph regression without changing resolution rules.

### 2. Make `SetupFastMock(...)` cheaper without changing behavior

Evidence:

- The provider-matrix benchmark is still relatively cheap, but the simple and complex Moq-backed service flows spend far more time in tracked setup and activation.
- Logger-specific and known-type post-processing still impose non-trivial cost in the service benchmarks.

Planned change:

- Separate the unavoidable parts of tracked mock setup from the optional post-processing layers.
- Add narrower benchmarks for `SetupFastMock(...)` itself so later changes can prove whether logger setup, nested property setup, or known-type post-processing is dominating.

Expected effect:

- Better targeting for the next optimization round and fewer speculative runtime changes.

### 3. Add more diagnostic microbenchmarks before the next optimization wave lands

Remaining benchmark additions after this optimization pass:

- `SetupFastMock(...)` only
- `CreateInstance<T>()` with pre-registered dependencies
- `Verify<T>(...)` only

Reason:

- The new invocation-only benchmarks already separate post-setup business-call cost from construction cost. These remaining additions will isolate the setup path further so each later performance change can prove where the win came from.

## Recommended order of implementation

1. Dictionary-backed unkeyed tracked mock index.
2. Injectable-member and type-model caches.
3. `AddProperties(...)` no-data fast path.
4. Per-construction resolution cache.
5. Narrow `SetupFastMock(...)` diagnostics and targeted reductions.
6. Re-run the benchmark suite after each step and keep the checked-in results current, including the invocation-only guardrails.

## Success criteria

Near-term targets after the completed work in this round:

- Reduce the simple tracked Moq benchmark below `800 us`.
- Reduce the complex tracked Moq benchmark below `1.5 ms`.
- Keep detached-handle and provider-matrix paths at or below their current overhead bands.
- Preserve the current invocation-only parity band with direct Moq.

If those gains land, the next pass can decide whether further work should focus on Moq-specific setup overhead, logging setup, or a more specialized tracked graph bootstrap path.
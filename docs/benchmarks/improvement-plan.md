# FastMoq Benchmark Improvement Plan

This plan is based on the current benchmark suite and the code paths exercised by the slowest scenarios.

## Current evidence

Measured on the current branch after the latest optimization pass:

- Full tracked service flow with the Moq provider: `917.0 us` versus `313.6 us` for direct Moq in the simple scenario.
- Larger tracked dependency graph with the Moq provider: `1.949 ms` versus `872.6 us` for direct Moq.
- Simple invocation-only flow after one-time setup: `1.372 us` versus `1.386 us` for direct Moq at `InvocationCount=1`, with identical allocation. The `InvocationCount=100` short-run result widened to `154.710 us` versus `130.189 us`, which is why these numbers still need to be interpreted conservatively.
- Complex invocation-only flow after one-time setup: `3.763 us` versus `3.709 us` for direct Moq at `InvocationCount=1`, with effectively identical allocation.
- Detached same-type doubles: `167.5 us` versus `115.5 us` for direct Moq.
- Lightweight provider-matrix interaction flow: `39.19 us` to `65.49 us` across `reflection`, `moq`, and `nsubstitute`.
- Tracked lookup microbench: `GetOrCreateLastTrackedMock` improved from about `194 ns` to about `23 ns`.
- Tracked creation microbench: `CreateTrackedLoggerMock` improved from about `198 us` to about `128 us`, and `CreateServiceWithTrackedDependencies` remains down from about `477 us` / `534 KB` to about `434 us` / `360 KB`.
- `SetupFastMock(...)` microbench: plain interface create+setup is `30.10 us` versus `25.35 us` for create-only, while logger create+setup is still `97.00 us` versus `25.51 us` for create-only.

The detached-handle, provider-matrix, invocation-only, and new setup-only numbers are all much closer to the direct-provider baselines than the full tracked-service numbers are.

## Working hypothesis

The main remaining overhead is now more clearly in the tracked mock lifecycle and tracked object-creation path, not in post-setup invocation or provider-neutral verification alone.

The invocation-only benchmarks show that once a service graph is already built, FastMoq and direct Moq stay in the same runtime and allocation band for the measured scenarios, even though short-run noise can still move the simple `InvocationCount=100` slice around.

The current likely hotspots are:

1. `SetupFastMock(...)` still eagerly materializes and configures tracked mocks, and the new microbench shows that logger-specific compatibility work is the dominant setup outlier.
2. Full tracked-service creation still repeats dependency resolution work inside the same construction wave.
3. `AddProperties(...)` and object construction still re-enter `GetObject(...)` repeatedly when building larger graphs.
4. Provider-specific arrange costs inside the initial setup path may still dominate parts of the Moq-backed end-to-end flows.

## Completed in this round

1. Replaced unkeyed tracked mock list scans with a private type index while preserving `mockCollection` ordering for existing tests.
2. Cached inferred `InstanceModel` resolution and interface-to-concrete fallback results.
3. Cached injection field and property discovery.
4. Added a no-data fast path for `AddProperties(...)` and cached writable property lists for the common auto-population path.
5. Added invocation-only benchmarks that build services once and measure repeated business calls separately from setup.
6. Added `SetupFastMockBenchmarks` to isolate tracked setup from raw provider mock creation.
7. Reduced Moq logger setup reflection by caching the generic setup dispatcher and using a fast path for wrapper-to-native `Mock` access.
8. Skipped interface injection-member scanning inside `SetupFastMock(...)`, which keeps plain interface setup close to raw provider creation.

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
- The new `SetupFastMock(...)` benchmark shows plain interface setup near raw provider creation while logger setup remains the dominant outlier.

Planned change:

- Separate the unavoidable parts of tracked mock setup from the optional post-processing layers.
- Use the existing `SetupFastMock(...)` benchmark to test narrower reductions in logger compatibility, nested property setup, and known-type post-processing.

Expected effect:

- Better targeting for the next optimization round and fewer speculative runtime changes.

### 3. Add more diagnostic microbenchmarks before the next optimization wave lands

Remaining benchmark additions after this optimization pass:

- `CreateInstance<T>()` with pre-registered dependencies
- `Verify<T>(...)` only

Reason:

- The new invocation-only and setup-only benchmarks already separate post-setup business-call cost from construction cost. These remaining additions will isolate activation and verification further so each later performance change can prove where the win came from.

## Recommended order of implementation

1. Dictionary-backed unkeyed tracked mock index.
2. Injectable-member and type-model caches.
3. `AddProperties(...)` no-data fast path.
4. `SetupFastMock(...)` diagnostics and targeted reductions.
5. Per-construction resolution cache.
6. Activation-only and verification-only microbenchmarks.
7. Re-run the benchmark suite after each step and keep the checked-in results current, including the invocation-only and setup-only guardrails.

## Success criteria

Near-term targets after the completed work in this round:

- Reduce the simple tracked Moq benchmark below `800 us`.
- Reduce the complex tracked Moq benchmark below `1.5 ms`.
- Keep detached-handle and provider-matrix paths at or below their current overhead bands.
- Preserve the current invocation-only parity band with direct Moq.

If those gains land, the next pass can decide whether further work should focus on Moq-specific setup overhead, logging setup, or a more specialized tracked graph bootstrap path.

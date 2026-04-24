# FastMoq Benchmark Improvement Plan

This plan is based on the current benchmark suite and the latest diagnostics used for internal performance work.

## Current evidence

Measured on the current branch after the latest benchmark refresh:

- Simple invocation-only workflow: FastMoq and direct Moq are effectively tied across `InvocationCount=10`, `50`, and `100`, with identical allocations.
- Complex invocation-only workflow: FastMoq and direct Moq are effectively tied across `InvocationCount=10`, `50`, and `100`, with identical allocations.
- `SetupFastMock(...)` diagnostics: plain interface create plus setup is `28.04 us` versus `25.29 us` for raw create-only, file system create plus setup is `34.28 us` versus `24.49 us`, and logger create plus setup is still `96.34 us` versus `25.04 us`.
- Tracked creation diagnostics: tracked interface mock creation is `47.25 us`, tracked logger mock creation is `121.97 us`, and tracked service creation with dependencies is `432.74 us`.

## Working hypothesis

The public runtime question is largely answered for the measured workflows: once setup is out of the measurement, FastMoq and direct Moq are effectively tied.

The main remaining overhead is in tracked setup and activation, not in repeated business execution.

The current likely hotspots are:

1. `SetupFastMock(...)` still does materially more work for logger mocks than for plain interfaces.
2. Full tracked-service creation still repeats dependency resolution and activation work inside the same construction wave.
3. `AddProperties(...)` and object construction still re-enter `GetObject(...)` repeatedly when building larger graphs.
4. Provider-specific arrange costs inside the initial setup path may still dominate parts of the Moq-backed creation flow.

## Completed in this round

1. Replaced unkeyed tracked mock list scans with a private type index while preserving `mockCollection` ordering for existing tests.
2. Cached inferred `InstanceModel` resolution and interface-to-concrete fallback results.
3. Cached injection field and property discovery.
4. Added a no-data fast path for `AddProperties(...)` and cached writable property lists for the common auto-population path.
5. Added invocation-only benchmarks that build services once and measure repeated business calls separately from setup.
6. Added `SetupFastMockBenchmarks` to isolate tracked setup from raw provider mock creation.
7. Reduced Moq logger setup reflection by caching the generic setup dispatcher and using a fast path for wrapper-to-native `Mock` access.
8. Skipped interface injection-member scanning inside `SetupFastMock(...)`, which keeps plain interface setup close to raw provider creation.
9. Removed embedded benchmark jobs from the benchmark classes so command-line job selection is authoritative and longer runs do not stack with a baked-in short run.
10. Updated the invocation-only benchmark parameters to `10`, `50`, and `100` so the published workflow slice emphasizes more representative repeated-execution counts.

These changes were validated against the main `FastMoq.Tests` project across `net8.0`, `net9.0`, and `net10.0` and then re-measured with the current benchmark slices.

## Priority order

### 1. Reduce duplicate object-resolution work during component creation

Evidence:

- `CreateServiceWithTrackedDependencies` is still much more expensive than isolated tracked mock creation.
- Larger service graphs repeatedly re-enter `GetObject(...)` and parameter resolution for the same construction wave.

Planned change:

- Add a scoped per-construction cache for already-resolved constructor parameter values.
- Avoid repeated `GetTypeModel(...)` and `GetObject(...)` churn for dependencies already resolved earlier in the same activation.

Expected effect:

- Reduce tracked activation cost without changing resolution rules.

### 2. Make `SetupFastMock(...)` cheaper without changing behavior

Evidence:

- Plain interface setup is now close to raw provider creation.
- Logger setup remains the dominant tracked-setup outlier in the diagnostics.

Planned change:

- Separate unavoidable tracked mock setup from optional post-processing layers.
- Continue using `SetupFastMockBenchmarks` to measure changes in logger compatibility, nested property setup, and known-type post-processing directly.

Expected effect:

- Better targeting for the next optimization round and fewer speculative runtime changes.

### 3. Add activation-only and verification-only diagnostics

Remaining benchmark additions after this optimization pass:

- `CreateInstance<T>()` with pre-registered dependencies
- `Verify<T>(...)` only

Reason:

- The invocation-only and setup-only benchmarks now separate steady-state runtime from setup. These remaining additions would isolate activation and verification further so each later performance change can prove where the win came from.

## Recommended order of implementation

1. Keep public comparisons on the invocation-only workflow slice.
2. Keep setup and tracked-creation diagnostics as internal guardrails.
3. `SetupFastMock(...)` targeted reductions.
4. Per-construction resolution cache.
5. Activation-only and verification-only microbenchmarks.
6. Re-run the selected benchmark slices after each step and keep the checked-in results current.

## Success criteria

Near-term targets after the completed work in this round:

- Preserve the current invocation-only parity band with direct Moq.
- Reduce logger setup overhead further without regressing plain interface setup.
- Reduce tracked service creation cost below the current `432.74 us` / `360.09 KB` band.

If those gains land, the next pass can decide whether further work should focus on Moq-specific logger setup, activation reuse, or a more specialized tracked graph bootstrap path.

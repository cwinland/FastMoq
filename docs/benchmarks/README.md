# FastMoq Benchmarks

This repository includes a runnable BenchmarkDotNet suite in `FastMoq.Benchmarks`.

The current suite measures raw direct-provider versus FastMoq provider-first overhead for a few representative flows. It does not try to quantify readability, migration cost, or the value of higher-level helpers outside the measured execution path.

## What the suite measures

- `SimpleServiceBenchmarks`: direct Moq versus FastMoq tracked `GetOrCreateMock<T>()` plus `CreateInstance<T>()` for a small service graph
- `ComplexServiceBenchmarks`: direct Moq versus FastMoq tracked creation for a larger workflow with options, logging, and more collaborators
- `StandaloneHandleBenchmarks`: direct Moq versus `CreateStandaloneFastMock<T>()` for detached same-type doubles
- `ProviderMatrixInteractionBenchmarks`: the same lightweight FastMoq tracked interaction flow under `moq`, `nsubstitute`, and `reflection`
- `TrackedLookupBenchmarks`: repeated lookup of already tracked mocks to isolate storage-path overhead
- `TrackedCreationBenchmarks`: tracked mock and tracked service creation to isolate setup and activation overhead
- `SetupFastMockBenchmarks`: provider mock creation versus provider mock creation plus `SetupFastMock(...)` to isolate tracked setup cost
- `SimpleInvocationOnlyBenchmarks`: repeated business invocations against a prebuilt simple service to isolate steady-state post-setup cost
- `ComplexInvocationOnlyBenchmarks`: repeated business invocations against a prebuilt complex service graph to isolate steady-state post-setup cost

## Run the benchmarks

```powershell
dotnet run -c Release --project .\FastMoq.Benchmarks\FastMoq.Benchmarks.csproj -- --filter "*"
```

BenchmarkDotNet writes local artifacts to `BenchmarkDotNet.Artifacts/results/`.

## Latest checked-in results

The latest checked-in short-run summary is in [results/latest-short-run-net8.md](./results/latest-short-run-net8.md).

Headline results from the latest local full-suite run on this branch:

| Scenario | Fastest result | Slowest result | Key takeaway |
| --- | --- | --- | --- |
| Simple tracked service flow | Direct Moq: 313.6 us, 115.54 KB | FastMoq provider-first: 917.0 us, 467.46 KB | The simple full-service path improved again, but it is still materially slower than direct Moq |
| Complex tracked dependency graph | Direct Moq: 872.6 us, 285.98 KB | FastMoq provider-first: 1.949 ms, 912.42 KB | The larger tracked graph remains the main end-to-end gap |
| Invocation-only steady-state flow | Direct Moq: 1.386 us to 367.539 us | FastMoq provider-first: 1.372 us to 372.993 us | Once setup is removed, the simple and complex runtime paths stay in the same allocation band and usually within short-run noise of each other |
| SetupFastMock microbench | Plain interface create+setup: 30.10 us, 50.53 KB | Logger create+setup: 97.00 us, 75.46 KB | Logger compatibility and setup still dominate the tracked setup outlier, while plain interface setup is now close to raw provider creation |
| Detached same-type doubles | Direct Moq: 115.5 us, 44.76 KB | FastMoq standalone handles: 167.5 us, 121.11 KB | Detached-handle overhead remains smaller than the tracked service-creation gap |
| FastMoq provider matrix | Reflection: 39.19 us, 34.43 KB | NSubstitute: 65.49 us, 48.41 KB | The lightweight interaction-only path is much cheaper than full tracked service creation, though short-run variance is still visible |
| Tracked lookup microbench | Contains: 4.989 ns | GetOrCreate tracked mock: 22.859 ns | Indexed unkeyed lookup keeps tracked retrieval overhead low |
| Tracked creation microbench | Tracked interface mock: 48.67 us, 82.04 KB | Tracked service with dependencies: 433.76 us, 359.58 KB | Tracked logger creation improved sharply, but full service activation still dominates the remaining cost |

## What the current numbers suggest

- FastMoq is still slower than direct Moq in the measured full-service benchmarks.
- The invocation-only benchmarks still show that once the service graph is already built, FastMoq and direct Moq stay in the same runtime and allocation band for the measured simple and complex flows.
- The new `SetupFastMockBenchmarks` make the next hotspot clearer: logger setup remains the dominant tracked-setup outlier, while plain interface setup is now only a small increment above raw provider creation.
- The provider-matrix interaction benchmark is much cheaper than the full tracked-service benchmarks, which keeps pointing to tracked setup and object construction rather than provider-neutral verification alone.
- The detached-handle benchmark is still much closer to direct Moq than the full tracked-service flows are.
- This branch already improved tracked lookup and tracked creation overhead with an indexed unkeyed mock store, type-model and injection-member caches, cached property metadata, cheaper Moq logger setup dispatch, and a setup fast path that skips injection-member scans for interface mocks.
- A constructor metadata cache was tested and then removed because the tracked creation microbench improved while the full tracked-service benchmarks did not show a reliable end-to-end benefit.
- The remaining optimization work is now more clearly centered on tracked setup and construction behavior rather than on post-setup invocation, detached-handle creation, or provider-neutral verification alone.

## Improvement plan

The current optimization plan is tracked in [improvement-plan.md](./improvement-plan.md).

That plan records which performance changes were completed in this round and what the next safe behavior-preserving passes should target.

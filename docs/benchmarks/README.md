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
| Simple tracked service flow | Direct Moq: 330.2 us, 115.54 KB | FastMoq provider-first: 942.7 us, 464 KB | Still slower than direct Moq, and this short-run simple benchmark remains noisy enough that small deltas should not drive design decisions alone |
| Complex tracked dependency graph | Direct Moq: 865.0 us, 285.63 KB | FastMoq provider-first: 1.884 ms, 904.74 KB | This is still the main end-to-end gap for widely used tracked service creation |
| Invocation-only steady-state flow | Direct Moq: 1.251 us to 376.1 us | FastMoq provider-first: 1.278 us to 346.8 us | Once setup is removed, FastMoq and direct Moq are effectively tied with the same allocations in both simple and complex scenarios |
| Detached same-type doubles | Direct Moq: 102.2 us, 44.76 KB | FastMoq standalone handles: 154.7 us, 119.85 KB | Detached-handle overhead remains smaller than the tracked service-creation gap |
| FastMoq provider matrix | Reflection: 52.96 us, 34.56 KB | NSubstitute: 65.82 us, 48.54 KB | The lightweight interaction-only path is much cheaper than full tracked service creation, but short-run variance is visible here too |
| Tracked lookup microbench | Contains: 7.011 ns | GetOrCreate tracked mock: 22.656 ns | Indexed unkeyed lookup removed most of the tracked retrieval overhead |
| Tracked creation microbench | Tracked interface mock: 52.09 us, 82.99 KB | Tracked service with dependencies: 438.63 us, 355.4 KB | Tracked creation is still materially better than the original baseline, but constructor metadata caching was reverted because that microbench win did not hold in the full tracked-service benchmarks |

## What the current numbers suggest

- FastMoq is still slower than direct Moq in the measured full-service benchmarks.
- The new invocation-only benchmarks show that once the service graph is already built, FastMoq and direct Moq are effectively tied in both runtime and allocations for the measured simple and complex flows.
- The provider-matrix interaction benchmark is much cheaper than the full tracked-service benchmarks, which suggests the main cost is still in tracked setup and object construction rather than provider-neutral verification alone.
- The detached-handle benchmark is still much closer to direct Moq than the full tracked-service flows are.
- This branch already improved tracked lookup and tracked creation overhead with an indexed unkeyed mock store, type-model and injection-member caches, and cached property metadata.
- A constructor metadata cache was tested and then removed because the tracked creation microbench improved while the full tracked-service benchmarks did not show a reliable end-to-end benefit.
- The remaining optimization work is now more clearly centered on tracked setup and construction behavior rather than on post-setup invocation, detached-handle creation, or provider-neutral verification alone.

## Improvement plan

The current optimization plan is tracked in [improvement-plan.md](./improvement-plan.md).

That plan records which performance changes were completed in this round and what the next safe behavior-preserving passes should target.
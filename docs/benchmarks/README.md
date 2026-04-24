# FastMoq Benchmarks

This repository includes a runnable BenchmarkDotNet suite in `FastMoq.Benchmarks`.

The suite is used for two different purposes:

- fast local diagnostics while tuning changes
- publishable slices that separate steady-state runtime from setup and creation cost

The benchmark classes no longer embed a fixed BenchmarkDotNet job. Choose the job explicitly from the command line so `short`, `medium`, and any custom settings do not stack together.

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

Fast local full-suite pass:

```powershell
dotnet run -c Release --project .\FastMoq.Benchmarks\FastMoq.Benchmarks.csproj -- -j short --filter "*"
```

Publishable runtime slice:

```powershell
dotnet run -c Release --project .\FastMoq.Benchmarks\FastMoq.Benchmarks.csproj -- -j medium --filter "*SimpleInvocationOnlyBenchmarks*" "*ComplexInvocationOnlyBenchmarks*"
```

Publishable setup and creation slice:

```powershell
dotnet run -c Release --project .\FastMoq.Benchmarks\FastMoq.Benchmarks.csproj -- -j medium --filter "*SetupFastMockBenchmarks*" "*TrackedCreationBenchmarks*"
```

BenchmarkDotNet writes local artifacts to `BenchmarkDotNet.Artifacts/results/`.

## Latest checked-in results

The latest checked-in publish set is in [results/latest-medium-run-selected-net8.md](./results/latest-medium-run-selected-net8.md).

Headline results from the latest checked-in medium runs on this branch:

| Scenario | Direct Moq | FastMoq | Key takeaway |
| --- | --- | --- | --- |
| Simple invocation-only runtime | `1.261 us` to `133.876 us`, `1.16 KB` to `115.7 KB` | `1.272 us` to `128.809 us`, same allocations | Once setup is removed, the simple runtime path is effectively tied and slightly favors FastMoq at `InvocationCount=100` |
| Complex invocation-only runtime | `3.901 us` to `370.498 us`, `2.68 KB` to `268.07 KB` | `3.659 us` to `365.893 us`, same allocations | The larger steady-state workflow is also effectively tied once construction is out of the measurement |
| SetupFastMock diagnostics | raw creation `24.49 us` to `25.29 us` | create plus setup `28.04 us` to `96.34 us` | Plain interface setup is close to raw creation, while logger setup remains the main tracked-setup outlier |
| Tracked creation diagnostics | interface `47.25 us` | logger `121.97 us`, service `432.74 us` | Runtime parity is no longer the main issue in these measured slices; setup and activation are the remaining optimization targets |

## What the current numbers suggest

- The publishable story should focus on invocation-only results, because they answer the runtime question directly without mixing in one-time setup cost.
- In the current medium runs, FastMoq and direct Moq are effectively tied in both simple and complex steady-state flows, with identical allocations across the measured invocation counts.
- Setup is still slower than raw provider creation, especially for loggers, but that is now a diagnostic result rather than the headline runtime claim.
- `SetupFastMockBenchmarks` and `TrackedCreationBenchmarks` are the right slices to keep using for optimization work because they isolate the remaining overhead without hiding it.
- Full service-construction benchmarks are still useful during development, but they are not the clearest published summary because they combine construction, setup, and business execution into one number.

## Improvement plan

The current optimization plan is tracked in [improvement-plan.md](./improvement-plan.md).

That plan records which performance changes were completed in this round and which setup and activation slices are still the best next targets.

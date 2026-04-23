# Latest Short-Run Benchmark Results

These results were generated from the current branch on 2026-04-23 with:

```powershell
dotnet run -c Release --project .\FastMoq.Benchmarks\FastMoq.Benchmarks.csproj -- --filter "*"
```

Environment:

- BenchmarkDotNet v0.13.12
- Windows 11 (10.0.26200.8246)
- 13th Gen Intel Core i9-13900K
- .NET SDK 10.0.300-preview.0.26177.108
- Host runtime .NET 8.0.26
- Job `ShortRun-.NET 8.0` with `LaunchCount=1`, `WarmupCount=3`, `IterationCount=3`

These numbers are local branch measurements, not a guarantee of the same timings on another machine or under a longer benchmark job.

## Simple tracked service flow

This scenario compares direct Moq wiring against FastMoq tracked `GetOrCreateMock<T>()` plus `CreateInstance<T>()` for a small service graph.

| Method | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| DirectMoq | 313.6 us | 78.05 us | 4.28 us | 1.00 | 0.00 | 1 | 5.8594 | 5.3711 | 115.54 KB | 1.00 |
| FastMoqProviderFirst | 917.0 us | 1,503.41 us | 82.41 us | 2.92 | 0.25 | 2 | 25.3906 | 23.4375 | 467.46 KB | 4.05 |

## Complex tracked dependency graph

This scenario compares direct Moq wiring against FastMoq tracked creation for a larger workflow with options, logging, and more collaborators.

| Method | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| DirectMoq | 872.6 us | 182.4 us | 10.00 us | 1.00 | 0.00 | 1 | 13.6719 | 11.7188 | 285.98 KB | 1.00 |
| FastMoqProviderFirst | 1,949.3 us | 2,112.4 us | 115.79 us | 2.24 | 0.16 | 2 | 46.8750 | 42.9688 | 912.42 KB | 3.19 |

## Simple invocation-only flow

This scenario builds the service once in global setup and then measures repeated business invocations only.

| Method | InvocationCount | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| FastMoqInvokeOnly | 1 | 1.372 us | 1.1308 us | 0.0620 us | 0.99 | 0.06 | 1 | 0.0610 | 0.0591 | 1.16 KB | 1.00 |
| DirectMoqInvokeOnly | 1 | 1.386 us | 0.5618 us | 0.0308 us | 1.00 | 0.00 | 2 | 0.0610 | 0.0591 | 1.16 KB | 1.00 |
|                     |                 |            |             |            |       |         |      |        |        |           |             |
| FastMoqInvokeOnly | 10 | 13.290 us | 11.5404 us | 0.6326 us | 0.95 | 0.06 | 1 | 0.6104 | 0.5798 | 11.63 KB | 1.00 |
| DirectMoqInvokeOnly | 10 | 14.048 us | 14.1103 us | 0.7734 us | 1.00 | 0.00 | 2 | 0.6104 | 0.5798 | 11.63 KB | 1.00 |
|                     |                 |            |             |            |       |         |      |        |        |           |             |
| DirectMoqInvokeOnly | 100 | 130.189 us | 20.3551 us | 1.1157 us | 1.00 | 0.00 | 1 | 6.1035 | 5.8594 | 115.7 KB | 1.00 |
| FastMoqInvokeOnly | 100 | 154.710 us | 604.9917 us | 33.1616 us | 1.19 | 0.25 | 2 | 6.1035 | 5.8594 | 115.7 KB | 1.00 |

## Complex invocation-only flow

This scenario builds the larger workflow once in global setup and then measures repeated business invocations only.

| Method | InvocationCount | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| DirectMoqInvokeOnly | 1 | 3.709 us | 0.6478 us | 0.0355 us | 1.00 | 0.00 | 1 | 0.1450 | 0.1373 | 2.68 KB | 1.00 |
| FastMoqInvokeOnly | 1 | 3.763 us | 0.9572 us | 0.0525 us | 1.01 | 0.01 | 2 | 0.1450 | 0.1373 | 2.68 KB | 1.00 |
|                     |                 |            |            |           |       |         |      |         |         |           |             |
| FastMoqInvokeOnly | 10 | 37.309 us | 2.4034 us | 0.1317 us | 1.00 | 0.02 | 1 | 1.4038 | 1.3428 | 26.87 KB | 1.00 |
| DirectMoqInvokeOnly | 10 | 37.347 us | 12.8885 us | 0.7065 us | 1.00 | 0.00 | 1 | 1.4038 | 1.3428 | 26.87 KB | 1.00 |
|                     |                 |            |            |           |       |         |      |         |         |           |             |
| DirectMoqInvokeOnly | 100 | 367.539 us | 72.6601 us | 3.9827 us | 1.00 | 0.00 | 1 | 14.1602 | 13.6719 | 268.06 KB | 1.00 |
| FastMoqInvokeOnly | 100 | 372.993 us | 55.8883 us | 3.0634 us | 1.01 | 0.01 | 2 | 14.1602 | 13.6719 | 268.07 KB | 1.00 |

## Detached same-type doubles

This scenario compares two raw `Mock<T>` instances against `CreateStandaloneFastMock<T>()` for manual wiring of two same-type collaborators.

| Method | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| DirectMoq | 115.5 us | 108.5 us | 5.95 us | 1.00 | 0.00 | 1 | 2.1973 | 1.9531 | 44.76 KB | 1.00 |
| FastMoqStandaloneHandles | 167.5 us | 180.6 us | 9.90 us | 1.45 | 0.12 | 2 | 6.3477 | 5.8594 | 121.11 KB | 2.71 |

## FastMoq provider matrix

This scenario compares the same lightweight tracked interaction flow across the built-in FastMoq providers.

| Method | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| FastMoqReflectionProvider | 39.19 us | 14.888 us | 0.816 us | 0.68 | 0.02 | 1 | 1.8311 | 1.7090 | 34.43 KB | 0.35 |
| FastMoqMoqProvider | 57.64 us | 7.221 us | 0.396 us | 1.00 | 0.00 | 2 | 5.2490 | 0.2441 | 97.31 KB | 1.00 |
| FastMoqNSubstituteProvider | 65.49 us | 42.735 us | 2.342 us | 1.14 | 0.03 | 3 | 2.4414 | 0.9766 | 48.41 KB | 0.50 |

## Tracked microbenchmarks after optimization

These targeted microbenchmarks isolate the slices optimized in this round.

### Tracked lookup

| Method | Mean | Error | StdDev | Ratio | Rank | Gen0 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ContainsLastTrackedMock | 4.989 ns | 1.329 ns | 0.0729 ns | 1.00 | 1 | - | - | NA |
| GetNativeLastTrackedMock | 15.565 ns | 2.825 ns | 0.1548 ns | 3.12 | 2 | - | - | NA |
| GetOrCreateLastTrackedMock | 22.859 ns | 7.842 ns | 0.4299 ns | 4.58 | 3 | 0.0021 | 40 B | NA |

Compared with the earlier baseline on this branch, `GetOrCreateLastTrackedMock` dropped from about `194 ns` to about `23 ns`, and `GetNativeLastTrackedMock` dropped from about `190 ns` to about `16 ns`.

### Tracked creation

| Method | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| CreateTrackedInterfaceMock | 48.67 us | 17.84 us | 0.978 us | 1.00 | 0.00 | 1 | 4.3945 | 0.1221 | 82.04 KB | 1.00 |
| CreateTrackedLoggerMock | 127.89 us | 22.08 us | 1.210 us | 2.63 | 0.03 | 2 | 5.6152 | 5.3711 | 107.01 KB | 1.30 |
| CreateServiceWithTrackedDependencies | 433.76 us | 47.44 us | 2.600 us | 8.92 | 0.23 | 3 | 19.5313 | 18.5547 | 359.58 KB | 4.38 |

Compared with the earlier baseline on this branch, `CreateTrackedLoggerMock` dropped from about `198 us` to about `128 us`, and `CreateServiceWithTrackedDependencies` remains down from about `477 us` / `534 KB` to about `434 us` / `360 KB`.

## SetupFastMock microbenchmarks

These targeted setup microbenchmarks isolate provider mock creation from tracked `SetupFastMock(...)` work.

| Method | Scenario | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| CreateOnly | FileSystem | 25.07 us | 6.363 us | 0.349 us | 1.00 | 0.00 | 1 | 2.5024 | 0.0610 | 46.37 KB | 1.00 |
| CreateAndSetup | FileSystem | 73.01 us | 216.074 us | 11.844 us | 2.91 | 0.48 | 2 | 3.4180 | 0.1221 | 64.13 KB | 1.38 |
| CreateOnly | Logger | 25.51 us | 6.501 us | 0.356 us | 1.00 | 0.00 | 1 | 2.5024 | 0.0610 | 46.63 KB | 1.00 |
| CreateAndSetup | Logger | 97.00 us | 34.041 us | 1.866 us | 3.80 | 0.10 | 2 | 3.9063 | 3.6621 | 75.46 KB | 1.62 |
| CreateOnly | PlainInterface | 25.35 us | 4.329 us | 0.237 us | 1.00 | 0.00 | 1 | 2.5024 | 0.0610 | 46.38 KB | 1.00 |
| CreateAndSetup | PlainInterface | 30.10 us | 23.509 us | 1.289 us | 1.19 | 0.06 | 2 | 2.7466 | 0.0610 | 50.53 KB | 1.09 |

The new setup-only slice shows that plain interface setup is now close to raw provider creation, while logger-specific compatibility and post-processing still dominate the setup outlier.

## Interpretation

- In the current Moq-backed full-service benchmarks, direct Moq is still faster and allocates less memory than the FastMoq provider-first path.
- In the invocation-only benchmarks, FastMoq and direct Moq remain in the same runtime and allocation band once setup is removed from the measurement, although the short-run simple `InvocationCount=100` slice is noisier than the rest.
- The detached-handle benchmark remains much closer to direct Moq than the tracked-service benchmarks are, so detached mock creation is probably not the main source of the larger regression.
- The provider-matrix benchmark shows that the lightweight tracked interaction-only path is still relatively cheap across `reflection`, `moq`, and `nsubstitute`, but short-run variance is visible.
- The new `SetupFastMock(...)` benchmark shows that logger setup is the dominant tracked-setup outlier, while plain interface setup is now close to raw provider creation.
- This optimization round materially improved the tracked lookup path, cut tracked logger creation sharply, and still improved the tracked service-creation microbench versus the original baseline.
- A constructor metadata cache was tried and then reverted because its microbenchmark win did not show a reliable end-to-end benefit in the full tracked-service benchmarks.
- The remaining larger gap is therefore still in the full tracked-service path, so the next safe optimization wave should focus on per-construction resolution reuse and `SetupFastMock(...)` work rather than on detached handles, post-setup invocation, or provider-neutral verification.

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
| DirectMoq | 330.2 us | 362.1 us | 19.85 us | 1.00 | 0.00 | 1 | 5.8594 | 5.3711 | 115.54 KB | 1.00 |
| FastMoqProviderFirst | 942.7 us | 1,136.7 us | 62.30 us | 2.85 | 0.03 | 2 | 23.4375 | 21.4844 | 464 KB | 4.02 |

## Complex tracked dependency graph

This scenario compares direct Moq wiring against FastMoq tracked creation for a larger workflow with options, logging, and more collaborators.

| Method | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| DirectMoq | 865.0 us | 70.36 us | 3.86 us | 1.00 | 0.00 | 1 | 13.6719 | 11.7188 | 285.63 KB | 1.00 |
| FastMoqProviderFirst | 1,884.3 us | 2,650.28 us | 145.27 us | 2.18 | 0.16 | 2 | 46.8750 | 42.9688 | 904.74 KB | 3.17 |

## Simple invocation-only flow

This scenario builds the service once in global setup and then measures repeated business invocations only.

| Method | InvocationCount | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| DirectMoqInvokeOnly | 1 | 1.251 us | 0.1511 us | 0.0083 us | 1.00 | 0.00 | 1 | 0.0610 | 0.0591 | 1.16 KB | 1.00 |
| FastMoqInvokeOnly | 1 | 1.278 us | 0.4409 us | 0.0242 us | 1.02 | 0.01 | 2 | 0.0610 | 0.0591 | 1.16 KB | 1.00 |
| FastMoqInvokeOnly | 10 | 12.600 us | 6.0714 us | 0.3328 us | 0.98 | 0.01 | 1 | 0.6104 | 0.5798 | 11.63 KB | 1.00 |
| DirectMoqInvokeOnly | 10 | 12.912 us | 7.5857 us | 0.4158 us | 1.00 | 0.00 | 2 | 0.6256 | 0.6104 | 11.63 KB | 1.00 |
| DirectMoqInvokeOnly | 100 | 125.522 us | 92.6904 us | 5.0807 us | 1.00 | 0.00 | 1 | 6.1035 | 5.8594 | 115.7 KB | 1.00 |
| FastMoqInvokeOnly | 100 | 128.402 us | 102.3522 us | 5.6103 us | 1.02 | 0.04 | 2 | 6.1035 | 5.8594 | 115.7 KB | 1.00 |

## Complex invocation-only flow

This scenario builds the larger workflow once in global setup and then measures repeated business invocations only.

| Method | InvocationCount | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| FastMoqInvokeOnly | 1 | 3.631 us | 1.6081 us | 0.0881 us | 0.99 | 0.03 | 1 | 0.1450 | 0.1373 | 2.68 KB | 1.00 |
| DirectMoqInvokeOnly | 1 | 3.667 us | 0.2490 us | 0.0136 us | 1.00 | 0.00 | 1 | 0.1450 | 0.1373 | 2.68 KB | 1.00 |
| FastMoqInvokeOnly | 10 | 35.647 us | 2.5910 us | 0.1420 us | 0.93 | 0.01 | 1 | 1.4038 | 1.3428 | 26.87 KB | 1.00 |
| DirectMoqInvokeOnly | 10 | 38.315 us | 8.8338 us | 0.4842 us | 1.00 | 0.00 | 2 | 1.4038 | 1.3428 | 26.87 KB | 1.00 |
| FastMoqInvokeOnly | 100 | 346.808 us | 192.4714 us | 10.5500 us | 0.92 | 0.01 | 1 | 14.1602 | 13.6719 | 268.06 KB | 1.00 |
| DirectMoqInvokeOnly | 100 | 376.098 us | 236.9309 us | 12.9870 us | 1.00 | 0.00 | 2 | 14.1602 | 13.6719 | 268.06 KB | 1.00 |

## Detached same-type doubles

This scenario compares two raw `Mock<T>` instances against `CreateStandaloneFastMock<T>()` for manual wiring of two same-type collaborators.

| Method | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| DirectMoq | 102.2 us | 23.33 us | 1.28 us | 1.00 | 0.00 | 1 | 2.1973 | 1.9531 | 44.76 KB | 1.00 |
| FastMoqStandaloneHandles | 154.7 us | 270.13 us | 14.81 us | 1.51 | 0.13 | 2 | 6.3477 | 5.8594 | 119.85 KB | 2.68 |

## FastMoq provider matrix

This scenario compares the same lightweight tracked interaction flow across the built-in FastMoq providers.

| Method | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| FastMoqReflectionProvider | 52.96 us | 214.450 us | 11.755 us | 0.96 | 0.21 | 1 | 1.8311 | 1.7090 | 34.56 KB | 0.36 |
| FastMoqMoqProvider | 55.08 us | 2.695 us | 0.148 us | 1.00 | 0.00 | 2 | 5.1270 | 0.2441 | 96.27 KB | 1.00 |
| FastMoqNSubstituteProvider | 65.82 us | 76.219 us | 4.178 us | 1.19 | 0.07 | 3 | 2.4414 | 0.9766 | 48.54 KB | 0.50 |

## Tracked microbenchmarks after optimization

These targeted microbenchmarks isolate the slices optimized in this round.

### Tracked lookup

| Method | Mean | Error | StdDev | Ratio | Rank | Gen0 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| ContainsLastTrackedMock | 7.011 ns | 0.3118 ns | 0.0171 ns | 1.00 | 1 | - | - | NA |
| GetNativeLastTrackedMock | 14.788 ns | 1.1644 ns | 0.0638 ns | 2.11 | 2 | - | - | NA |
| GetOrCreateLastTrackedMock | 22.656 ns | 1.2114 ns | 0.0664 ns | 3.23 | 3 | 0.0021 | 40 B | NA |

Compared with the earlier baseline on this branch, `GetOrCreateLastTrackedMock` dropped from about `194 ns` to about `23 ns`, and `GetNativeLastTrackedMock` dropped from about `190 ns` to about `15 ns`.

### Tracked creation

| Method | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| CreateTrackedInterfaceMock | 52.09 us | 40.77 us | 2.235 us | 1.00 | 0.00 | 1 | 4.3945 | 0.1221 | 82.99 KB | 1.00 |
| CreateTrackedLoggerMock | 198.02 us | 588.23 us | 32.243 us | 3.82 | 0.78 | 2 | 5.3711 | 4.8828 | 106.42 KB | 1.28 |
| CreateServiceWithTrackedDependencies | 438.63 us | 134.83 us | 7.390 us | 8.43 | 0.48 | 3 | 18.5547 | 17.5781 | 355.4 KB | 4.28 |

Compared with the earlier baseline on this branch, `CreateTrackedInterfaceMock` moved from about `49 us` to about `52 us`, while `CreateServiceWithTrackedDependencies` still dropped allocations from about `534 KB` to about `355 KB` and improved runtime from about `477 us` to about `439 us`.

## Interpretation

- In the current Moq-backed full-service benchmarks, direct Moq is still faster and allocates less memory than the FastMoq provider-first path.
- In the new invocation-only benchmarks, FastMoq and direct Moq are effectively tied once setup is removed from the measurement, and both simple and complex flows allocate the same amount.
- The detached-handle benchmark remains much closer to direct Moq than the tracked-service benchmarks are, so detached mock creation is probably not the main source of the larger regression.
- The provider-matrix benchmark shows that the lightweight tracked interaction-only path is still relatively cheap across `reflection`, `moq`, and `nsubstitute`, but short-run variance is visible.
- This optimization round materially improved the tracked lookup path and still improved the tracked service-creation microbench versus the original baseline.
- A constructor metadata cache was tried and then reverted because its microbenchmark win did not show a reliable end-to-end benefit in the full tracked-service benchmarks.
- The remaining larger gap is therefore still in the full tracked-service path, so the next safe optimization wave should focus on per-construction resolution reuse and `SetupFastMock(...)` work rather than on detached handles, post-setup invocation, or provider-neutral verification.
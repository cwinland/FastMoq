# Latest Medium-Run Selected Results

These results were generated from the current branch on 2026-04-24 with the publishable medium-run slices below.

Runtime slice:

```powershell
dotnet run -c Release --project .\FastMoq.Benchmarks\FastMoq.Benchmarks.csproj -- -j medium --filter "*SimpleInvocationOnlyBenchmarks*" "*ComplexInvocationOnlyBenchmarks*"
```

Setup and creation slice:

```powershell
dotnet run -c Release --project .\FastMoq.Benchmarks\FastMoq.Benchmarks.csproj -- -j medium --filter "*SetupFastMockBenchmarks*" "*TrackedCreationBenchmarks*"
```

Environment:

- BenchmarkDotNet v0.13.12
- Windows 11 (10.0.26200.8246)
- 13th Gen Intel Core i9-13900K
- .NET SDK 10.0.300-preview.0.26177.108
- Host runtime .NET 8.0.26
- Job `MediumRun` with `LaunchCount=2`, `WarmupCount=10`, `IterationCount=15`

These numbers are local branch measurements, not a guarantee of the same timings on another machine.

## Why these are the published slices

These results intentionally separate two questions:

- How close is FastMoq to direct Moq once a service graph already exists?
- Which setup and activation paths still cost more and should guide optimization work?

That keeps the runtime claim narrow and defensible while still surfacing the remaining setup overhead honestly.

## Simple invocation-only flow

This scenario builds the service once in global setup and then measures repeated business invocations only.

| Method | InvocationCount | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| DirectMoqInvokeOnly | 1 | 1.261 us | 0.0350 us | 0.0524 us | 1.00 | 0.00 | 1 | 0.0610 | 0.0591 | 1.16 KB | 1.00 |
| FastMoqInvokeOnly | 1 | 1.272 us | 0.0373 us | 0.0558 us | 1.01 | 0.05 | 1 | 0.0610 | 0.0591 | 1.16 KB | 1.00 |
| DirectMoqInvokeOnly | 10 | 13.201 us | 0.5176 us | 0.7423 us | 1.00 | 0.00 | 1 | 0.6104 | 0.5798 | 11.63 KB | 1.00 |
| FastMoqInvokeOnly | 10 | 13.578 us | 0.4031 us | 0.5518 us | 1.04 | 0.07 | 2 | 0.6104 | 0.5798 | 11.63 KB | 1.00 |
| FastMoqInvokeOnly | 100 | 128.809 us | 2.0787 us | 2.8453 us | 0.97 | 0.06 | 1 | 6.1035 | 5.8594 | 115.7 KB | 1.00 |
| DirectMoqInvokeOnly | 100 | 133.876 us | 5.4199 us | 7.7731 us | 1.00 | 0.00 | 2 | 6.1035 | 5.8594 | 115.7 KB | 1.00 |

## Complex invocation-only flow

This scenario builds the larger workflow once in global setup and then measures repeated business invocations only.

| Method | InvocationCount | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| FastMoqInvokeOnly | 1 | 3.659 us | 0.0887 us | 0.1300 us | 0.94 | 0.06 | 1 | 0.1450 | 0.1373 | 2.68 KB | 1.00 |
| DirectMoqInvokeOnly | 1 | 3.901 us | 0.1722 us | 0.2524 us | 1.00 | 0.00 | 2 | 0.1450 | 0.1373 | 2.68 KB | 1.00 |
| FastMoqInvokeOnly | 10 | 36.860 us | 1.0988 us | 1.5759 us | 0.98 | 0.05 | 1 | 1.4038 | 1.3428 | 26.87 KB | 1.00 |
| DirectMoqInvokeOnly | 10 | 37.634 us | 0.8850 us | 1.2972 us | 1.00 | 0.00 | 1 | 1.4038 | 1.3428 | 26.87 KB | 1.00 |
| FastMoqInvokeOnly | 100 | 365.893 us | 9.7922 us | 14.6565 us | 0.99 | 0.07 | 1 | 14.1602 | 13.6719 | 268.07 KB | 1.00 |
| DirectMoqInvokeOnly | 100 | 370.498 us | 16.0618 us | 24.0406 us | 1.00 | 0.00 | 1 | 14.1602 | 13.6719 | 268.07 KB | 1.00 |

## SetupFastMock diagnostics

These targeted setup benchmarks isolate provider mock creation from tracked `SetupFastMock(...)` work.

| Method | Scenario | Mean | Error | StdDev | Median | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| CreateOnly | FileSystem | 24.49 us | 0.151 us | 0.217 us | 24.47 us | 1.00 | 0.00 | 1 | 2.5024 | 0.0610 | 46.37 KB | 1.00 |
| CreateAndSetup | FileSystem | 34.28 us | 0.761 us | 1.091 us | 33.96 us | 1.40 | 0.05 | 2 | 3.4180 | 0.1221 | 64.02 KB | 1.38 |
| CreateOnly | Logger | 25.04 us | 0.248 us | 0.331 us | 24.92 us | 1.00 | 0.00 | 1 | 2.5024 | 0.0610 | 46.63 KB | 1.00 |
| CreateAndSetup | Logger | 96.34 us | 0.554 us | 0.829 us | 96.43 us | 3.85 | 0.05 | 2 | 3.9063 | 3.6621 | 75.68 KB | 1.62 |
| CreateOnly | PlainInterface | 25.29 us | 0.377 us | 0.552 us | 25.04 us | 1.00 | 0.00 | 1 | 2.5024 | 0.0610 | 46.38 KB | 1.00 |
| CreateAndSetup | PlainInterface | 28.04 us | 0.720 us | 1.009 us | 27.38 us | 1.11 | 0.05 | 2 | 2.7466 | 0.0610 | 50.53 KB | 1.09 |

## Tracked creation diagnostics

These benchmarks isolate tracked mock and tracked service creation without the surrounding end-to-end flow.

| Method | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| CreateTrackedInterfaceMock | 47.25 us | 0.415 us | 0.608 us | 1.00 | 0.00 | 1 | 4.3945 | 0.1221 | 82.04 KB | 1.00 |
| CreateTrackedLoggerMock | 121.97 us | 2.600 us | 3.729 us | 2.58 | 0.08 | 2 | 5.6152 | 5.3711 | 107.08 KB | 1.31 |
| CreateServiceWithTrackedDependencies | 432.74 us | 5.969 us | 8.170 us | 9.16 | 0.23 | 3 | 19.5313 | 18.5547 | 360.09 KB | 4.39 |

## Interpretation

- In these medium runs, FastMoq and direct Moq are effectively tied in the measured simple and complex invocation-only flows, with identical allocations at each invocation count.
- The main publishable claim is therefore about steady-state runtime after setup, not about one-time setup cost.
- Setup remains slower than raw provider creation, especially for loggers, and those results should be treated as optimization guidance rather than hidden or overstated.
- `SetupFastMockBenchmarks` and `TrackedCreationBenchmarks` now provide the clearest signals for the next performance work because they isolate the remaining setup and activation overhead directly.

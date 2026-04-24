# Latest Results

These results were generated from the current branch on 2026-04-24 with the published workflow comparison below.

```powershell
dotnet run -c Release --project .\FastMoq.Benchmarks\FastMoq.Benchmarks.csproj -- -j medium --filter "*SimpleInvocationOnlyBenchmarks*" "*ComplexInvocationOnlyBenchmarks*"
```

Environment:

- BenchmarkDotNet v0.13.12
- Windows 11 (10.0.26200.8246)
- 13th Gen Intel Core i9-13900K
- .NET SDK 10.0.300-preview.0.26177.108
- Host runtime .NET 8.0.26
- Job selected by the command above

These numbers are local branch measurements, not a guarantee of the same timings on another machine.

## Why this is the published comparison

This document publishes the invocation-only workflow comparison: how close is FastMoq to direct Moq once setup is already complete?

## Simple invocation-only workflow

This scenario builds the service once in global setup and then measures repeated business invocations only.

| Method | InvocationCount | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| FastMoqInvokeOnly | 10 | 15.41 us | 0.507 us | 0.694 us | 0.95 | 0.08 | 1 | 0.6104 | 0.5798 | 11.63 KB | 1.00 |
| DirectMoqInvokeOnly | 10 | 16.56 us | 1.174 us | 1.721 us | 1.00 | 0.00 | 2 | 0.6104 | 0.5798 | 11.63 KB | 1.00 |
| FastMoqInvokeOnly | 50 | 74.44 us | 3.681 us | 5.279 us | 0.94 | 0.10 | 1 | 3.0518 | 2.9297 | 57.88 KB | 1.00 |
| DirectMoqInvokeOnly | 50 | 80.12 us | 4.682 us | 6.863 us | 1.00 | 0.00 | 2 | 3.0518 | 2.9297 | 57.88 KB | 1.00 |
| DirectMoqInvokeOnly | 100 | 147.80 us | 7.536 us | 10.808 us | 1.00 | 0.00 | 1 | 6.1035 | 5.8594 | 115.7 KB | 1.00 |
| FastMoqInvokeOnly | 100 | 147.97 us | 4.888 us | 7.011 us | 1.01 | 0.09 | 1 | 6.1035 | 5.8594 | 115.7 KB | 1.00 |

## Complex invocation-only workflow

This scenario builds the larger workflow once in global setup and then measures repeated business invocations only.

| Method | InvocationCount | Mean | Error | StdDev | Ratio | RatioSD | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| DirectMoqInvokeOnly | 10 | 42.70 us | 2.787 us | 3.997 us | 1.00 | 0.00 | 1 | 1.4038 | 1.3428 | 26.87 KB | 1.00 |
| FastMoqInvokeOnly | 10 | 43.43 us | 1.643 us | 2.459 us | 1.02 | 0.09 | 1 | 1.4038 | 1.3428 | 26.87 KB | 1.00 |
| FastMoqInvokeOnly | 50 | 187.04 us | 3.026 us | 4.340 us | 0.91 | 0.05 | 1 | 7.0801 | 6.8359 | 134.07 KB | 1.00 |
| DirectMoqInvokeOnly | 50 | 205.35 us | 9.735 us | 13.962 us | 1.00 | 0.00 | 2 | 7.0801 | 6.8359 | 134.07 KB | 1.00 |
| FastMoqInvokeOnly | 100 | 404.97 us | 7.537 us | 10.316 us | 1.00 | 0.05 | 1 | 14.1602 | 13.6719 | 268.07 KB | 1.00 |
| DirectMoqInvokeOnly | 100 | 407.69 us | 12.048 us | 17.279 us | 1.00 | 0.00 | 1 | 14.1602 | 13.6719 | 268.07 KB | 1.00 |

## Interpretation

- In these workflow comparisons, FastMoq and direct Moq are effectively tied across the measured invocation counts.
- Allocations are identical across the published simple and complex workflow slices.

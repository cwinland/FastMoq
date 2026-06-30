# Generated Harness Setup Results

These results were generated from the current branch on 2026-05-06 with the published generator-facing comparison below.

```powershell
dotnet run -c Release --project .\FastMoq.Benchmarks\FastMoq.Benchmarks.csproj -- -j short --filter "*GeneratedHarnessSetupBenchmarks*"
```

Environment:

- BenchmarkDotNet v0.13.12
- Windows 11 (10.0.26200.8246)
- 13th Gen Intel Core i9-13900K
- .NET SDK 10.0.300-preview.0.26177.108
- Host runtime .NET 8.0.26
- Job selected by the command above

These numbers are local branch measurements, not a guarantee of the same timings on another machine.

## Why this comparison exists

This document records the first generator-facing setup-path measurement for `#122`: a richer single-public-constructor target where the benchmark projects the harness bootstrap descriptor and construction graph through either the normal runtime fallback path or the source-generated constructor-metadata path.

## Generated harness bootstrap projection

| Method | Mean | Error | StdDev | Ratio | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| GeneratedHarnessBootstrapDescriptor | 232.7 us | 47.78 us | 2.62 us | 0.98 | 1 | 20.0195 | 2.4414 | 375.91 KB | 0.99 |
| RuntimeFallbackBootstrapDescriptor | 238.1 us | 81.33 us | 4.46 us | 1.00 | 2 | 20.5078 | 2.9297 | 380.57 KB | 1.00 |

## Interpretation

- On this richer graph/bootstrap comparison, the generated harness path still holds a slight edge over the runtime fallback path.
- Allocations remain close, with the generated harness path now measuring slightly lower than the runtime fallback path.
- The short-run confidence interval is intentionally lightweight, so treat this as recorded branch evidence for the first MVP slice rather than a blanket promise for every component shape.

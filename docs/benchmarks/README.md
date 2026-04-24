# FastMoq Benchmarks

This repository includes a runnable BenchmarkDotNet suite in `FastMoq.Benchmarks`.

The published comparison in this folder focuses on a narrow question: once a test service graph already exists, how close is FastMoq to direct Moq during repeated business execution?

## Run the published comparison

Published workflow comparison:

```powershell
dotnet run -c Release --project .\FastMoq.Benchmarks\FastMoq.Benchmarks.csproj -- -j medium --filter "*SimpleInvocationOnlyBenchmarks*" "*ComplexInvocationOnlyBenchmarks*"
```

BenchmarkDotNet writes local artifacts to `BenchmarkDotNet.Artifacts/results/`.

## Latest checked-in results

The latest checked-in results are in [results/latest-results-net8.md](./results/latest-results-net8.md).

The latest checked-in workflow comparison shows:

| Workflow | Takeaway |
| --- | --- |
| Simple invocation-only workflow | FastMoq and direct Moq are effectively tied across `10`, `50`, and `100` repeated invocations, with identical allocations throughout. |
| Complex invocation-only workflow | FastMoq and direct Moq are effectively tied across the measured invocation counts, again with identical allocations. |

## What the published results show

- Once setup is removed from the measurement, FastMoq and direct Moq are effectively tied in these workflow comparisons.
- The published comparison stays centered on invocation-only workflow execution.

# FastMoq Benchmarks

This repository includes a runnable BenchmarkDotNet suite in `FastMoq.Benchmarks`.

The published comparison in this folder focuses on a narrow question: once a test service graph already exists, how close is FastMoq to direct Moq during repeated business execution?

That comparison is intentionally separate from one-time setup and activation diagnostics. Those engineering benchmarks are still useful for improving FastMoq, but they are not the right headline for public runtime comparisons.

## What the suite measures

The benchmark suite contains several slices, including:

- full service-construction benchmarks
- detached handle benchmarks
- provider matrix benchmarks
- setup and creation diagnostics
- invocation-only workflow benchmarks

For public comparison, the invocation-only workflow benchmarks are the primary reference because they isolate steady-state execution after setup.

## Run the benchmarks

Quick local full-suite pass:

```powershell
dotnet run -c Release --project .\FastMoq.Benchmarks\FastMoq.Benchmarks.csproj -- -j short --filter "*"
```

Published workflow comparison:

```powershell
dotnet run -c Release --project .\FastMoq.Benchmarks\FastMoq.Benchmarks.csproj -- -j medium --filter "*SimpleInvocationOnlyBenchmarks*" "*ComplexInvocationOnlyBenchmarks*"
```

BenchmarkDotNet writes local artifacts to `BenchmarkDotNet.Artifacts/results/`.

## Latest checked-in results

The latest checked-in results are in [results/latest-results-net8.md](./results/latest-results-net8.md).

Headline takeaways from the latest checked-in workflow comparison:

| Workflow | Takeaway |
| --- | --- |
| Simple invocation-only workflow | FastMoq and direct Moq are effectively tied across `10`, `50`, and `100` repeated invocations, with identical allocations throughout. |
| Complex invocation-only workflow | FastMoq and direct Moq are effectively tied across the measured invocation counts, again with identical allocations. |

## What the current numbers suggest

- Once setup is removed from the measurement, FastMoq and direct Moq are effectively tied in these workflow comparisons.
- The public comparison story should stay centered on invocation-only workflow execution, because that answers the steady-state runtime question directly.
- Setup and tracked-creation diagnostics remain useful internally because they isolate the remaining overhead directly and help guide optimization work.

## Internal performance work

If you are tuning FastMoq itself, the internal optimization notes remain in [improvement-plan.md](./improvement-plan.md).

# FastMoq Benchmarks

This repository includes a runnable BenchmarkDotNet suite in `FastMoq.Benchmarks`.

The published comparison in this folder focuses on a narrow question: once a test service graph already exists, how close is FastMoq to direct Moq during repeated business execution?

The current branch also records a second narrow generator-facing question: for a richer single-constructor target, how does source-generated graph/bootstrap projection compare to the normal runtime fallback path?

## Run the published comparison

Published workflow comparison:

```powershell
dotnet run -c Release --project .\FastMoq.Benchmarks\FastMoq.Benchmarks.csproj -- -j medium --filter "*SimpleInvocationOnlyBenchmarks*" "*ComplexInvocationOnlyBenchmarks*"
```

BenchmarkDotNet writes local artifacts to `BenchmarkDotNet.Artifacts/results/`.

Generated harness comparison:

```powershell
dotnet run -c Release --project .\FastMoq.Benchmarks\FastMoq.Benchmarks.csproj -- -j short --filter "*GeneratedHarnessSetupBenchmarks*"
```

## Latest checked-in results

The latest checked-in invocation-only results are in [results/latest-results-net8.md](./results/latest-results-net8.md).

The latest checked-in generated-harness setup results are in [results/generated-harness-setup-net8.md](./results/generated-harness-setup-net8.md).

The latest checked-in workflow comparison shows:

| Workflow | Takeaway |
| --- | --- |
| Simple invocation-only workflow | FastMoq and direct Moq are effectively tied across `10`, `50`, and `100` repeated invocations, with identical allocations throughout. |
| Complex invocation-only workflow | FastMoq and direct Moq are effectively tied across the measured invocation counts, again with identical allocations. |
| Generated graph/bootstrap projection | On the richer single-constructor benchmark used for `#122`, the generated harness path holds a slight edge over the runtime fallback path with effectively identical allocations. |

## What the published results show

- Once setup is removed from the measurement, FastMoq and direct Moq are effectively tied in these workflow comparisons.
- The published comparison stays centered on invocation-only workflow execution.
- The generated harness comparison stays centered on graph/bootstrap planning for the first explicit `MockerTestBase<TComponent>` generator slice rather than full generated tests.

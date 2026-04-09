# FastMoq API Reference

This API reference is generated from XML comments in the source projects.

## Example-first entry points

- [Executable testing examples](../samples/testing-examples.md)
- [Sample applications overview](../samples/README.md)
- [Provider selection guide](../getting-started/provider-selection.md)
- [Testing guide](../getting-started/testing-guide.md)
- [Quick reference for common types](./quick-reference.md) when you want the shortest path to provider APIs such as `IMockingProvider` and `MockingProviderRegistry`

## Quick type lookup

- [Quick reference for common types](./quick-reference.md)
- [Mocker](../../api/FastMoq.Mocker.yml)
- [MockerTestBase&lt;TComponent&gt;](../../api/FastMoq.MockerTestBase-1.yml)
- [ScenarioBuilder&lt;T&gt;](../../api/FastMoq.ScenarioBuilder-1.yml)
- [MockingProviderRegistry](../../api/FastMoq.Providers.MockingProviderRegistry.yml)
- [FunctionContextTestExtensions](../../api/FastMoq.AzureFunctions.Extensions.FunctionContextTestExtensions.yml)
- [TestWebExtensions](../../api/FastMoq.Web.Extensions.TestWebExtensions.yml)

If you are integrating a non-bundled mocking library, start from [Quick reference for common types](./quick-reference.md) and follow the provider path through `IMockingProvider`, `IMockingProviderCapabilities`, and `MockingProviderRegistry`.

## Notes

- The generated site output is written to the Help folder.
- The generation script clears the existing Help folder before building so stale files are not carried forward.
- The metadata step is pinned to net8.0 for consistent CI generation.
- DocFX is pinned through `.config/dotnet-tools.json` so local and CI builds use the same version.
- The published release version is only stamped into the footer during the GitHub release publish flow.

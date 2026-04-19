# FastMoq API Reference

The published API reference is generated from XML comments in the source projects.

## Example-first entry points

- [Executable testing examples](../samples/testing-examples.md)
- [Sample applications overview](../samples/README.md)
- [Provider selection guide](../getting-started/provider-selection.md)
- [Testing guide](../getting-started/testing-guide.md)
- [Quick reference for common types](./quick-reference.md) when you want the shortest path to provider APIs such as `IMockingProvider` and `MockingProviderRegistry`

## Quick type lookup

- [Quick reference for common types](./quick-reference.md)
- [Mocker](https://help.fastmoq.com/api/FastMoq.Mocker.html)
- [MockerTestBase&lt;TComponent&gt;](https://help.fastmoq.com/api/FastMoq.MockerTestBase-1.html)
- [ScenarioBuilder&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.ScenarioBuilder-1.html)
- [IFastMock&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Providers.IFastMock-1.html)
- [MockModel&lt;T&gt;](https://help.fastmoq.com/api/FastMoq.Models.MockModel-1.html)
- [MockingProviderRegistry](https://help.fastmoq.com/api/FastMoq.Providers.MockingProviderRegistry.html)
- [FunctionContextTestExtensions](https://help.fastmoq.com/api/FastMoq.AzureFunctions.Extensions.FunctionContextTestExtensions.html)
- [TestWebExtensions](https://help.fastmoq.com/api/FastMoq.Web.Extensions.TestWebExtensions.html)

If you are integrating a non-bundled mocking library, start from [Quick reference for common types](./quick-reference.md) and follow the provider path through `IMockingProvider`, `IMockingProviderCapabilities`, and `MockingProviderRegistry`.

Lifecycle note: tracked creation helpers such as `GetOrCreateMock<T>()` and `CreateFastMock<T>()`, plus detached creation through `CreateStandaloneFastMock<T>()`, are methods on [Mocker](https://help.fastmoq.com/api/FastMoq.Mocker.html) rather than separate API types.

## Notes

- The generated site output is written to the Help folder.
- The generation script clears the existing Help folder before building so stale files are not carried forward.
- The metadata step is pinned to net8.0 for consistent CI generation.
- DocFX is pinned through `.config/dotnet-tools.json` so local and CI builds use the same version.
- The published release version is only stamped into the footer during the GitHub release publish flow.

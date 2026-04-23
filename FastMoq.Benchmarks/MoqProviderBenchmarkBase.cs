using BenchmarkDotNet.Attributes;
using FastMoq.Providers;

namespace FastMoq.Benchmarks;

/// <summary>
/// Shared BenchmarkDotNet lifecycle hooks for scenarios that pin FastMoq to the Moq provider.
/// </summary>
public abstract class MoqProviderBenchmarkBase
{
    private IDisposable? _providerScope;

    /// <summary>
    /// Selects the Moq provider for the benchmark run so provider-first setup extensions are available.
    /// </summary>
    [GlobalSetup]
    public void ActivateMoqProvider()
    {
        _providerScope = MockingProviderRegistry.Push("moq");
    }

    /// <summary>
    /// Releases the benchmark-scoped provider selection.
    /// </summary>
    [GlobalCleanup]
    public void ReleaseMoqProvider()
    {
        _providerScope?.Dispose();
        _providerScope = null;
    }
}
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using FastMoq.Extensions;
using FastMoq.Providers;

namespace FastMoq.Benchmarks;

/// <summary>
/// Measures repeated lookup cost for already tracked mocks so storage-path changes can be evaluated independently.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TrackedLookupBenchmarks
{
    private IDisposable? _providerScope;
    private Mocker? _mocker;

    /// <summary>
    /// Selects the Moq provider and pre-populates a small tracked mock graph so lookups hit the last registered type.
    /// </summary>
    [GlobalSetup]
    public void SetupTrackedMocks()
    {
        _providerScope = MockingProviderRegistry.Push("moq");
        _mocker = new Mocker();

        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker01>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker02>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker03>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker04>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker05>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker06>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker07>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker08>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker09>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker10>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker11>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker12>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker13>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker14>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker15>>();
        _ = _mocker.GetOrCreateMock<ILookupProbe<LookupMarker16>>();
    }

    /// <summary>
    /// Disposes the tracked benchmark mocker after the run completes.
    /// </summary>
    [GlobalCleanup]
    public void DisposeTrackedMocks()
    {
        _mocker?.Dispose();
        _mocker = null;
        _providerScope?.Dispose();
        _providerScope = null;
    }

    /// <summary>
    /// Measures the public contains path for a late-entry tracked mock.
    /// </summary>
    [Benchmark(Baseline = true)]
    public bool ContainsLastTrackedMock()
    {
        return _mocker!.Contains(typeof(ILookupProbe<LookupMarker16>));
    }

    /// <summary>
    /// Measures repeated provider-first retrieval for an already tracked mock.
    /// </summary>
    [Benchmark]
    public object GetOrCreateLastTrackedMock()
    {
        return _mocker!.GetOrCreateMock<ILookupProbe<LookupMarker16>>();
    }

    /// <summary>
    /// Measures retrieval of the provider-native mock for an already tracked type.
    /// </summary>
    [Benchmark]
    public object GetNativeLastTrackedMock()
    {
        return _mocker!.GetNativeMock<ILookupProbe<LookupMarker16>>();
    }
}
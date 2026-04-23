using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using FastMoq.Providers;
using Microsoft.Extensions.Logging;

namespace FastMoq.Benchmarks;

/// <summary>
/// Measures tracked-mock and tracked-instance creation without the surrounding end-to-end benchmark flow.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[ShortRunJob(RuntimeMoniker.Net80)]
public class TrackedCreationBenchmarks
{
    private IDisposable? _providerScope;

    /// <summary>
    /// Selects the Moq provider for the tracked-creation benchmark run.
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

    /// <summary>
    /// Measures creation of a single tracked interface mock.
    /// </summary>
    [Benchmark(Baseline = true)]
    public bool CreateTrackedInterfaceMock()
    {
        using var mocker = new Mocker();
        return mocker.GetOrCreateMock<IUserDirectory>().Instance is not null;
    }

    /// <summary>
    /// Measures creation of a tracked logger mock, including logger-specific setup.
    /// </summary>
    [Benchmark]
    public bool CreateTrackedLoggerMock()
    {
        using var mocker = new Mocker();
        return mocker.GetOrCreateMock<ILogger<UserRegistrationService>>().Instance is not null;
    }

    /// <summary>
    /// Measures component creation after the tracked dependencies have already been registered.
    /// </summary>
    [Benchmark]
    public object CreateServiceWithTrackedDependencies()
    {
        using var mocker = new Mocker();

        _ = mocker.GetOrCreateMock<IUserDirectory>();
        _ = mocker.GetOrCreateMock<IWelcomeMessageGateway>();
        _ = mocker.GetOrCreateMock<IRegistrationAuditSink>();

        return mocker.CreateInstance<UserRegistrationService>()
            ?? throw new InvalidOperationException("FastMoq did not create UserRegistrationService for the tracked-creation benchmark.");
    }
}
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using FastMoq.Providers;
using System.IO.Abstractions;

namespace FastMoq.Benchmarks;

/// <summary>
/// Measures the incremental overhead of <see cref="Mocker.SetupFastMock(Type, IFastMock)"/> on top of raw provider mock creation.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SetupFastMockBenchmarks
{
    private IDisposable? _providerScope;
    private Type _mockedType = typeof(IUserDirectory);
    private MockCreationOptions _creationOptions = new(CallBase: false);

    /// <summary>
    /// Selects which tracked mock shape to benchmark.
    /// </summary>
    [Params("PlainInterface", "Logger", "FileSystem")]
    public string Scenario { get; set; } = "PlainInterface";

    /// <summary>
    /// Selects the Moq provider and configures the active benchmark scenario.
    /// </summary>
    [GlobalSetup]
    public void SetupScenario()
    {
        _providerScope = MockingProviderRegistry.Push("moq");
        _mockedType = Scenario switch
        {
            "PlainInterface" => typeof(IUserDirectory),
            "Logger" => typeof(Microsoft.Extensions.Logging.ILogger<UserRegistrationService>),
            "FileSystem" => typeof(IFileSystem),
            _ => throw new InvalidOperationException($"Unsupported setup benchmark scenario '{Scenario}'."),
        };

        _creationOptions = new MockCreationOptions(CallBase: !_mockedType.IsInterface);
    }

    /// <summary>
    /// Releases the benchmark-scoped provider selection.
    /// </summary>
    [GlobalCleanup]
    public void CleanupScenario()
    {
        _providerScope?.Dispose();
        _providerScope = null;
    }

    /// <summary>
    /// Measures raw provider mock creation on the same fresh-<see cref="Mocker"/> baseline used by the setup benchmark.
    /// </summary>
    [Benchmark(Baseline = true)]
    public IFastMock CreateOnly()
    {
        using var mocker = new Mocker();
        return MockingProviderRegistry.Default.CreateMock(_mockedType, _creationOptions);
    }

    /// <summary>
    /// Measures provider mock creation plus the tracked <see cref="Mocker.SetupFastMock(Type, IFastMock)"/> pass.
    /// </summary>
    [Benchmark]
    public IFastMock CreateAndSetup()
    {
        using var mocker = new Mocker();
        var fastMock = MockingProviderRegistry.Default.CreateMock(_mockedType, _creationOptions);
        mocker.SetupFastMock(_mockedType, fastMock);
        return fastMock;
    }
}
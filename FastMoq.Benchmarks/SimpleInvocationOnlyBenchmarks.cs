using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FastMoq.Benchmarks;

/// <summary>
/// Measures the steady-state cost of invoking an already constructed simple service.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[ShortRunJob(RuntimeMoniker.Net80)]
public class SimpleInvocationOnlyBenchmarks
{
    private static readonly RegistrationCommand Command = new("ada@example.test", "Ada Lovelace");

    private IDisposable? _providerScope;
    private Mocker? _fastMoqMocker;
    private UserRegistrationService? _directService;
    private UserRegistrationService? _fastMoqService;

    /// <summary>
    /// Number of business invocations performed per benchmark operation.
    /// </summary>
    [Params(1, 10, 100)]
    public int InvocationCount { get; set; }

    /// <summary>
    /// Builds the direct Moq and FastMoq-backed services once so only invocation cost is measured.
    /// </summary>
    [GlobalSetup]
    public void SetupServices()
    {
        _providerScope = MockingProviderRegistry.Push("moq");
        _directService = CreateDirectService();
        _fastMoqService = CreateFastMoqService(out var mocker);
        _fastMoqMocker = mocker;
    }

    /// <summary>
    /// Disposes the shared FastMoq state after the benchmark run.
    /// </summary>
    [GlobalCleanup]
    public void CleanupServices()
    {
        _fastMoqMocker?.Dispose();
        _fastMoqMocker = null;
        _providerScope?.Dispose();
        _providerScope = null;
    }

    /// <summary>
    /// Measures repeated business invocations on a prebuilt direct Moq service.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<int> DirectMoqInvokeOnly()
    {
        var successes = 0;
        for (var i = 0; i < InvocationCount; i++)
        {
            if (await _directService!.RegisterAsync(Command, CancellationToken.None).ConfigureAwait(false))
            {
                successes++;
            }
        }

        return successes;
    }

    /// <summary>
    /// Measures repeated business invocations on a prebuilt FastMoq-backed service.
    /// </summary>
    [Benchmark]
    public async Task<int> FastMoqInvokeOnly()
    {
        var successes = 0;
        for (var i = 0; i < InvocationCount; i++)
        {
            if (await _fastMoqService!.RegisterAsync(Command, CancellationToken.None).ConfigureAwait(false))
            {
                successes++;
            }
        }

        return successes;
    }

    private static UserRegistrationService CreateDirectService()
    {
        var userDirectory = new Mock<IUserDirectory>();
        var welcomeGateway = new Mock<IWelcomeMessageGateway>();
        var auditSink = new Mock<IRegistrationAuditSink>();

        userDirectory
            .Setup(x => x.SaveAsync(Command, CancellationToken.None))
            .Returns(Task.CompletedTask);
        welcomeGateway
            .Setup(x => x.SendWelcomeAsync(Command.EmailAddress, CancellationToken.None))
            .Returns(Task.CompletedTask);
        auditSink
            .Setup(x => x.WriteAsync(Command.EmailAddress, CancellationToken.None))
            .Returns(Task.CompletedTask);

        return new UserRegistrationService(
            userDirectory.Object,
            welcomeGateway.Object,
            auditSink.Object,
            NullLogger<UserRegistrationService>.Instance);
    }

    private static UserRegistrationService CreateFastMoqService(out Mocker mocker)
    {
        mocker = new Mocker()
            .AddType<Microsoft.Extensions.Logging.ILogger<UserRegistrationService>>(NullLogger<UserRegistrationService>.Instance);

        mocker.GetOrCreateMock<IUserDirectory>()
            .Setup(x => x.SaveAsync(Command, CancellationToken.None))
            .Returns(Task.CompletedTask);
        mocker.GetOrCreateMock<IWelcomeMessageGateway>()
            .Setup(x => x.SendWelcomeAsync(Command.EmailAddress, CancellationToken.None))
            .Returns(Task.CompletedTask);
        mocker.GetOrCreateMock<IRegistrationAuditSink>()
            .Setup(x => x.WriteAsync(Command.EmailAddress, CancellationToken.None))
            .Returns(Task.CompletedTask);

        return mocker.CreateInstance<UserRegistrationService>()
            ?? throw new InvalidOperationException("FastMoq did not create UserRegistrationService for the invocation-only benchmark.");
    }
}
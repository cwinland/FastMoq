using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.Logging;
using Moq;

namespace FastMoq.Benchmarks;

/// <summary>
/// Benchmarks a simple tracked test flow using direct Moq wiring versus FastMoq provider-first tracked mocks.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SimpleServiceBenchmarks : MoqProviderBenchmarkBase
{
    private static readonly RegistrationCommand Command = new("ada@example.test", "Ada Lovelace");

    /// <summary>
    /// Measures direct Moq setup, subject construction, execution, and verification for a simple service.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<bool> DirectMoq()
    {
        var userDirectory = new Mock<IUserDirectory>();
        var welcomeGateway = new Mock<IWelcomeMessageGateway>();
        var auditSink = new Mock<IRegistrationAuditSink>();
        var logger = new Mock<ILogger<UserRegistrationService>>();

        userDirectory
            .Setup(x => x.SaveAsync(Command, CancellationToken.None))
            .Returns(Task.CompletedTask);
        welcomeGateway
            .Setup(x => x.SendWelcomeAsync(Command.EmailAddress, CancellationToken.None))
            .Returns(Task.CompletedTask);
        auditSink
            .Setup(x => x.WriteAsync(Command.EmailAddress, CancellationToken.None))
            .Returns(Task.CompletedTask);

        var service = new UserRegistrationService(userDirectory.Object, welcomeGateway.Object, auditSink.Object, logger.Object);
        var registered = await service.RegisterAsync(Command, CancellationToken.None).ConfigureAwait(false);

        userDirectory.Verify(x => x.SaveAsync(Command, CancellationToken.None), Times.Once);
        welcomeGateway.Verify(x => x.SendWelcomeAsync(Command.EmailAddress, CancellationToken.None), Times.Once);
        auditSink.Verify(x => x.WriteAsync(Command.EmailAddress, CancellationToken.None), Times.Once);
        return registered;
    }

    /// <summary>
    /// Measures the equivalent flow using tracked provider-first FastMoq mocks under the Moq provider.
    /// </summary>
    [Benchmark]
    public async Task<bool> FastMoqProviderFirst()
    {
        using var mocker = new Mocker();

        mocker.GetOrCreateMock<IUserDirectory>()
            .Setup(x => x.SaveAsync(Command, CancellationToken.None))
            .Returns(Task.CompletedTask);
        mocker.GetOrCreateMock<IWelcomeMessageGateway>()
            .Setup(x => x.SendWelcomeAsync(Command.EmailAddress, CancellationToken.None))
            .Returns(Task.CompletedTask);
        mocker.GetOrCreateMock<IRegistrationAuditSink>()
            .Setup(x => x.WriteAsync(Command.EmailAddress, CancellationToken.None))
            .Returns(Task.CompletedTask);

        var service = mocker.CreateInstance<UserRegistrationService>()
            ?? throw new InvalidOperationException("FastMoq did not create UserRegistrationService for the benchmark.");
        var registered = await service.RegisterAsync(Command, CancellationToken.None).ConfigureAwait(false);

        mocker.Verify<IUserDirectory>(x => x.SaveAsync(Command, CancellationToken.None), TimesSpec.Once);
        mocker.Verify<IWelcomeMessageGateway>(x => x.SendWelcomeAsync(Command.EmailAddress, CancellationToken.None), TimesSpec.Once);
        mocker.Verify<IRegistrationAuditSink>(x => x.WriteAsync(Command.EmailAddress, CancellationToken.None), TimesSpec.Once);
        return registered;
    }
}
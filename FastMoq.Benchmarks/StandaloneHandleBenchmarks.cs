using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Moq;

namespace FastMoq.Benchmarks;

/// <summary>
/// Benchmarks the detached same-type double path that uses <see cref="Mocker.CreateStandaloneFastMock{T}(FastMoq.Providers.MockCreationOptions?)"/>.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[ShortRunJob(RuntimeMoniker.Net80)]
public class StandaloneHandleBenchmarks : MoqProviderBenchmarkBase
{
    /// <summary>
    /// Measures manual construction of two independent same-type Moq doubles plus verification.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task DirectMoq()
    {
        var primary = new Mock<IAuditChannel>();
        var secondary = new Mock<IAuditChannel>();

        primary
            .Setup(x => x.WriteAsync("order-42:primary", CancellationToken.None))
            .Returns(Task.CompletedTask);
        secondary
            .Setup(x => x.WriteAsync("order-42:secondary", CancellationToken.None))
            .Returns(Task.CompletedTask);

        var forwarder = new DualAuditForwarder(primary.Object, secondary.Object);
        await forwarder.DispatchAsync("order-42", CancellationToken.None).ConfigureAwait(false);

        primary.Verify(x => x.WriteAsync("order-42:primary", CancellationToken.None), Times.Once);
        secondary.Verify(x => x.WriteAsync("order-42:secondary", CancellationToken.None), Times.Once);
    }

    /// <summary>
    /// Measures detached FastMoq handle creation for the same same-type manual-wiring scenario.
    /// </summary>
    [Benchmark]
    public async Task FastMoqStandaloneHandles()
    {
        using var mocker = new Mocker();

        var primary = mocker.CreateStandaloneFastMock<IAuditChannel>();
        var secondary = mocker.CreateStandaloneFastMock<IAuditChannel>();

        primary.AsMoq()
            .Setup(x => x.WriteAsync("order-42:primary", CancellationToken.None))
            .Returns(Task.CompletedTask);
        secondary.AsMoq()
            .Setup(x => x.WriteAsync("order-42:secondary", CancellationToken.None))
            .Returns(Task.CompletedTask);

        var forwarder = new DualAuditForwarder(primary.Instance, secondary.Instance);
        await forwarder.DispatchAsync("order-42", CancellationToken.None).ConfigureAwait(false);

        MockingProviderRegistry.Default.Verify(primary, x => x.WriteAsync("order-42:primary", CancellationToken.None), TimesSpec.Once);
        MockingProviderRegistry.Default.Verify(secondary, x => x.WriteAsync("order-42:secondary", CancellationToken.None), TimesSpec.Once);
    }
}
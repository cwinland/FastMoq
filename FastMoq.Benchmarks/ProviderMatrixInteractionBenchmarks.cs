using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using FastMoq.Providers;

namespace FastMoq.Benchmarks;

/// <summary>
/// Compares the same provider-first tracked interaction flow across the built-in FastMoq providers.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ProviderMatrixInteractionBenchmarks
{
    /// <summary>
    /// Measures the provider-first tracked interaction flow under the Moq provider.
    /// </summary>
    [Benchmark(Baseline = true)]
    public bool FastMoqMoqProvider()
    {
        return RunWithProvider("moq");
    }

    /// <summary>
    /// Measures the provider-first tracked interaction flow under the NSubstitute provider.
    /// </summary>
    [Benchmark]
    public bool FastMoqNSubstituteProvider()
    {
        return RunWithProvider("nsubstitute");
    }

    /// <summary>
    /// Measures the provider-first tracked interaction flow under the built-in reflection provider.
    /// </summary>
    [Benchmark]
    public bool FastMoqReflectionProvider()
    {
        return RunWithProvider("reflection");
    }

    private static bool RunWithProvider(string providerName)
    {
        using var providerScope = MockingProviderRegistry.Push(providerName);
        using var mocker = new Mocker();

        var service = mocker.CreateInstance<ProviderMatrixInteractionService>()
            ?? throw new InvalidOperationException($"FastMoq did not create ProviderMatrixInteractionService for provider '{providerName}'.");

        var result = service.Run();

        mocker.Verify<IProviderMatrixPrimarySink>(x => x.Publish("alpha"), TimesSpec.Once);
        mocker.Verify<IProviderMatrixSecondarySink>(x => x.Publish("beta"), TimesSpec.Once);
        mocker.VerifyNoOtherCalls<IProviderMatrixPrimarySink>();
        mocker.VerifyNoOtherCalls<IProviderMatrixSecondarySink>();

        return result;
    }
}
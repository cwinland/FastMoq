using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using FastMoq.Generators;

namespace FastMoq.Benchmarks;

/// <summary>
/// Measures fresh harness setup for the generated single-constructor path versus the normal runtime fallback path.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public partial class GeneratedHarnessSetupBenchmarks
{
    /// <summary>
    /// Measures creating a fresh runtime-only harness and projecting the graph/bootstrap descriptor through constructor discovery.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int RuntimeFallbackBootstrapDescriptor()
    {
        using var harness = new RuntimeSingleConstructorHarness();
        return harness.GetBenchmarkedNodeCount();
    }

    /// <summary>
    /// Measures creating a fresh source-generated harness and projecting the same graph/bootstrap descriptor through generated constructor metadata.
    /// </summary>
    [Benchmark]
    public int GeneratedHarnessBootstrapDescriptor()
    {
        using var harness = new GeneratedSingleConstructorHarness();
        return harness.GetBenchmarkedNodeCount();
    }

    internal sealed class SingleConstructorTarget
    {
        public SingleConstructorTarget(
            IAlphaDependency alphaDependency,
            IBetaDependency betaDependency,
            IGammaDependency gammaDependency,
            IDeltaDependency deltaDependency,
            IEpsilonDependency epsilonDependency,
            IZetaDependency zetaDependency,
            IEtaDependency etaDependency,
            IThetaDependency thetaDependency,
            IServiceProvider serviceProvider,
            string value,
            int retryCount)
        {
            AlphaDependency = alphaDependency;
            BetaDependency = betaDependency;
            GammaDependency = gammaDependency;
            DeltaDependency = deltaDependency;
            EpsilonDependency = epsilonDependency;
            ZetaDependency = zetaDependency;
            EtaDependency = etaDependency;
            ThetaDependency = thetaDependency;
            ServiceProviderWasResolved = serviceProvider is not null;
            ValueWasNull = value is null;
            RetryCount = retryCount;
        }

        public IAlphaDependency AlphaDependency { get; }

        public IBetaDependency BetaDependency { get; }

        public IGammaDependency GammaDependency { get; }

        public IDeltaDependency DeltaDependency { get; }

        public IEpsilonDependency EpsilonDependency { get; }

        public IZetaDependency ZetaDependency { get; }

        public IEtaDependency EtaDependency { get; }

        public IThetaDependency ThetaDependency { get; }

        public bool ServiceProviderWasResolved { get; }

        public bool ValueWasNull { get; }

        public int RetryCount { get; }
    }

    internal interface IAlphaDependency;

    internal interface IBetaDependency;

    internal interface IGammaDependency;

    internal interface IDeltaDependency;

    internal interface IEpsilonDependency;

    internal interface IZetaDependency;

    internal interface IEtaDependency;

    internal interface IThetaDependency;

    [FastMoqGeneratedTestTarget(typeof(SingleConstructorTarget))]
    internal partial class GeneratedSingleConstructorHarness : MockerTestBase<SingleConstructorTarget>
    {
        public int GetBenchmarkedNodeCount() => GetComponentHarnessBootstrapDescriptor().Graph.Nodes.Count;
    }

    internal sealed class RuntimeSingleConstructorHarness : MockerTestBase<SingleConstructorTarget>
    {
        public int GetBenchmarkedNodeCount() => GetComponentHarnessBootstrapDescriptor().Graph.Nodes.Count;
    }
}
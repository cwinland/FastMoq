using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Xunit;

[assembly: FastMoqRegisterProvider("moq", typeof(MoqMockingProvider), SetAsDefault = true)]
[assembly: CollectionBehavior(DisableTestParallelization = true)]
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;

[assembly: FastMoqRegisterProvider("moq", typeof(MoqMockingProvider), SetAsDefault = true)]
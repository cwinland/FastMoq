using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using FastMoq.Providers.NSubstituteProvider;

[assembly: FastMoqRegisterProvider("moq", typeof(MoqMockingProvider), SetAsDefault = true)]
[assembly: FastMoqRegisterProvider("nsubstitute", typeof(NSubstituteMockingProvider))]
using FastMoq.Providers;
using FastMoq.Providers.NSubstituteProvider;

[assembly: FastMoqRegisterProvider("nsubstitute", typeof(NSubstituteMockingProvider))]

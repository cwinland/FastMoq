using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using System.Runtime.CompilerServices;

namespace FastMoq.Tests
{
    public static class TestAssemblyProviderBootstrap
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            MockingProviderRegistry.Register("moq", MoqMockingProvider.Instance, setAsDefault: true);
        }
    }
}
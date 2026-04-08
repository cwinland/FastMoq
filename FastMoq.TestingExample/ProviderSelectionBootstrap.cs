using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using System.Runtime.CompilerServices;

namespace FastMoq.TestingExample
{
    public static class ProviderSelectionBootstrap
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            MockingProviderRegistry.Register("moq", MoqMockingProvider.Instance, setAsDefault: true);
        }
    }
}
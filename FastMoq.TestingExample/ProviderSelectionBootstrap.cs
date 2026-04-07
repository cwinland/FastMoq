using System.Runtime.CompilerServices;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;

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
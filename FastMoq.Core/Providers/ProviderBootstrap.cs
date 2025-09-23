using System.Runtime.CompilerServices;
using FastMoq.Core.Providers.MoqProvider;
using FastMoq.Core.Providers.Reflection;
using FastMoq.Providers;

[assembly: InternalsVisibleTo("FastMoq.Tests")]

namespace FastMoq.Core.Providers
{
    internal static class ProviderBootstrap
    {
        private static int _initialized;
        static ProviderBootstrap() => Ensure();

        public static void Ensure()
        {
            if (Interlocked.Exchange(ref _initialized, 1) == 1) return;
            // Register reflection provider first (non-default) then Moq provider as default.
            MockingProviderRegistry.Register("reflection", new ReflectionMockingProvider(), setAsDefault: false);
            IMockingProvider provider = new MoqMockingProvider();
            MockingProviderRegistry.Register("moq", provider, setAsDefault: true);
        }
    }
}

using FastMoq.Providers;
using System.Runtime.CompilerServices;

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
            MockingProviderRegistry.EnsureDiscoveredProvidersRegistered();
        }
    }
}

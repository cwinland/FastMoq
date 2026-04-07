using System.Runtime.CompilerServices;
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
            // Force the registry type initializer to run so built-in providers are registered once.
            _ = MockingProviderRegistry.RegisteredProviderNames;
        }
    }
}

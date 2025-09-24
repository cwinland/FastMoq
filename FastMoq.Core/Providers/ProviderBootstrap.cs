using System.Threading;
using System.Runtime.CompilerServices;
using FastMoq.Providers.MoqProvider; // updated namespace
using FastMoq.Providers.ReflectionProvider; // updated namespace
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
            // Register providers (registry static ctor also registers these; this is a safety net for early access scenarios)
            MockingProviderRegistry.Register("reflection", ReflectionMockingProvider.Instance, setAsDefault: false);
            MockingProviderRegistry.Register("moq", MoqMockingProvider.Instance, setAsDefault: true);
        }
    }
}

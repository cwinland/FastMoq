using System;
using System.Collections.Concurrent;
using System.Threading;
using FastMoq.Providers.MoqProvider;
using FastMoq.Core.Providers.NSubstituteProvider; // added
using FastMoq.Providers.ReflectionProvider; // added reflection provider

namespace FastMoq.Providers
{
    /// <summary>
    /// Global registry for available mocking providers with support for scoped overrides.
    /// </summary>
    public static class MockingProviderRegistry
    {
        private static readonly ConcurrentDictionary<string, IMockingProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
        private static IMockingProvider? _default;
        private static readonly AsyncLocal<IMockingProvider?> _current = new();

        static MockingProviderRegistry()
        {
            // Auto-register Moq provider as default if consumer did not register any provider yet.
            Register("moq", MoqMockingProvider.Instance, setAsDefault: true);
            // Register NSubstitute (not default) for validation / optional use.
            Register("nsubstitute", NSubstituteMockingProvider.Instance, setAsDefault: false);
            // Register reflection fallback provider (lowest priority, not default)
            Register("reflection", ReflectionMockingProvider.Instance, setAsDefault: false);
        }

        public static void Register(string name, IMockingProvider provider, bool setAsDefault = false)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(provider);
            _providers[name] = provider;
            if (setAsDefault || _default is null)
            {
                _default = provider;
            }
        }

        public static bool TryGet(string name, out IMockingProvider provider) => _providers.TryGetValue(name, out provider!);

        public static IMockingProvider Default => _current.Value ?? _default ?? throw new InvalidOperationException("No mocking provider registered. Register one via MockingProviderRegistry.Register().");

        /// <summary>
        /// Push a provider for the current async context returning a disposable that restores the previous value.
        /// </summary>
        public static IDisposable Push(IMockingProvider provider)
        {
            var prior = _current.Value;
            _current.Value = provider;
            return new PopDisposable(prior);
        }

        public static void Clear()
        {
            _providers.Clear();
            _default = null;
            _current.Value = null;
        }

        private sealed class PopDisposable : IDisposable
        {
            private readonly IMockingProvider? _prior;
            private bool _disposed;
            public PopDisposable(IMockingProvider? prior) => _prior = prior;
            public void Dispose()
            {
                if (_disposed) return;
                _current.Value = _prior;
                _disposed = true;
            }
        }
    }
}

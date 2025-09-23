using System;
using System.Collections.Concurrent;
using System.Threading;

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

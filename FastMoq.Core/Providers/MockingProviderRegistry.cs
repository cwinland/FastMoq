using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FastMoq.Providers.MoqProvider;
using FastMoq.Providers.ReflectionProvider;

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
            // Auto-register reflection as the provider-neutral default.
            Register("reflection", ReflectionMockingProvider.Instance, setAsDefault: true);
            // Moq remains bundled in v4 for compatibility, but is no longer the default.
            Register("moq", MoqMockingProvider.Instance, setAsDefault: false);
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

        public static IReadOnlyCollection<string> RegisteredProviderNames => _providers.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

        public static IMockingProvider Default => _current.Value ?? _default ?? throw new InvalidOperationException("No mocking provider registered. Register one via MockingProviderRegistry.Register().");

        public static IFastMock WrapLegacy(object legacyMock, Type mockedType)
        {
            ArgumentNullException.ThrowIfNull(legacyMock);
            ArgumentNullException.ThrowIfNull(mockedType);

            var defaultProvider = _current.Value ?? _default;
            if (defaultProvider != null)
            {
                var wrappedByDefault = defaultProvider.TryWrapLegacy(legacyMock, mockedType);
                if (wrappedByDefault != null)
                {
                    return wrappedByDefault;
                }
            }

            foreach (var provider in _providers.Values.Distinct())
            {
                if (ReferenceEquals(provider, defaultProvider))
                {
                    continue;
                }

                var wrapped = provider.TryWrapLegacy(legacyMock, mockedType);
                if (wrapped != null)
                {
                    return wrapped;
                }
            }

            throw new NotSupportedException($"No registered mocking provider can wrap legacy instance '{legacyMock.GetType().FullName}' for mocked type '{mockedType.FullName}'.");
        }

        public static void SetDefault(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (!TryGet(name, out var provider))
            {
                throw new InvalidOperationException($"Unable to find provider '{name}'. Registered providers: {string.Join(", ", RegisteredProviderNames)}.");
            }

            _default = provider;
        }

        /// <summary>
        /// Push a provider for the current async context returning a disposable that restores the previous value.
        /// </summary>
        public static IDisposable Push(IMockingProvider provider)
        {
            var prior = _current.Value;
            _current.Value = provider;
            return new PopDisposable(prior);
        }

        public static IDisposable Push(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (!TryGet(name, out var provider))
            {
                throw new InvalidOperationException($"Unable to find provider '{name}'. Registered providers: {string.Join(", ", RegisteredProviderNames)}.");
            }

            return Push(provider);
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

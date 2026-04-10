using FastMoq.Providers.MoqProvider;
using FastMoq.Providers.ReflectionProvider;
using System.Collections.Concurrent;
using System.Reflection;

namespace FastMoq.Providers
{
    /// <summary>
    /// Global registry for available mocking providers with support for scoped overrides.
    /// </summary>
    public static class MockingProviderRegistry
    {
        private const string NSubstituteProviderAssemblyName = "FastMoq.Provider.NSubstitute";
        private const string NSubstituteProviderTypeName = "FastMoq.Providers.NSubstituteProvider.NSubstituteMockingProvider";

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

        /// <summary>
        /// Registers a mocking provider under the supplied name.
        /// </summary>
        /// <param name="name">The provider name used to look up the registration later.</param>
        /// <param name="provider">The provider implementation to register.</param>
        /// <param name="setAsDefault">When <see langword="true"/>, also makes the provider the global default provider.</param>
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

        /// <summary>
        /// Attempts to resolve a registered provider by name.
        /// </summary>
        /// <param name="name">The provider name to look up.</param>
        /// <param name="provider">When this method returns <see langword="true"/>, contains the resolved provider.</param>
        /// <returns><see langword="true"/> when the provider was found or loaded; otherwise, <see langword="false"/>.</returns>
        public static bool TryGet(string name, out IMockingProvider provider)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (_providers.TryGetValue(name, out provider!))
            {
                return true;
            }

            return TryEnsureOptionalProviderRegistered(name) && _providers.TryGetValue(name, out provider!);
        }

        /// <summary>
        /// Gets the registered provider names ordered alphabetically.
        /// </summary>
        public static IReadOnlyCollection<string> RegisteredProviderNames => _providers.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

        /// <summary>
        /// Gets the effective provider for the current async flow.
        /// A scoped provider pushed with <see cref="Push(IMockingProvider)"/> or <see cref="Push(string)"/> takes precedence over the global default.
        /// </summary>
        public static IMockingProvider Default => _current.Value ?? _default ?? throw new InvalidOperationException("No mocking provider registered. Register one via MockingProviderRegistry.Register().");

        /// <summary>
        /// Wraps a legacy provider-specific mock object in the provider-agnostic <see cref="IFastMock"/> abstraction.
        /// </summary>
        /// <param name="legacyMock">The legacy provider-specific mock object to wrap.</param>
        /// <param name="mockedType">The mocked type represented by <paramref name="legacyMock"/>.</param>
        /// <returns>A provider-agnostic wrapper for the legacy mock object.</returns>
        /// <exception cref="NotSupportedException">Thrown when no registered provider can adapt the supplied legacy mock.</exception>
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

        /// <summary>
        /// Sets the global default provider by name.
        /// </summary>
        /// <param name="name">The name of the provider to make the global default.</param>
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
        /// Pushes a provider for the current async context and returns a disposable that restores the previous provider when disposed.
        /// </summary>
        /// <param name="provider">The provider to make active for the current async flow.</param>
        /// <returns>A disposable scope that restores the prior provider when disposed.</returns>
        public static IDisposable Push(IMockingProvider provider)
        {
            var prior = _current.Value;
            _current.Value = provider;
            return new PopDisposable(prior);
        }

        /// <summary>
        /// Pushes a named provider for the current async context and returns a disposable that restores the previous provider when disposed.
        /// </summary>
        /// <param name="name">The registered provider name to activate for the current async flow.</param>
        /// <returns>A disposable scope that restores the prior provider when disposed.</returns>
        public static IDisposable Push(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (!TryGet(name, out var provider))
            {
                throw new InvalidOperationException($"Unable to find provider '{name}'. Registered providers: {string.Join(", ", RegisteredProviderNames)}.");
            }

            return Push(provider);
        }

        /// <summary>
        /// Clears all provider registrations and resets both the global and scoped provider selections.
        /// </summary>
        public static void Clear()
        {
            _providers.Clear();
            _default = null;
            _current.Value = null;
        }

        internal static void ApplyAssemblyProviderRegistrations(IEnumerable<Assembly> assemblies)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            var registrations = assemblies
                .SelectMany(GetAssemblyProviderRegistrations)
                .GroupBy(registration => registration.ProviderName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var group in registrations)
            {
                var distinctProviderTypes = group
                    .Select(registration => registration.ProviderType.AssemblyQualifiedName ?? registration.ProviderType.FullName ?? registration.ProviderType.Name)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                if (distinctProviderTypes.Length > 1)
                {
                    throw new InvalidOperationException($"Multiple FastMoq provider registrations were declared for '{group.Key}': {string.Join(", ", distinctProviderTypes.OrderBy(name => name, StringComparer.Ordinal))}. Declare only one provider type per provider name.");
                }

                var registration = group.First();
                Register(registration.ProviderName, CreateProviderInstance(registration.ProviderName, registration.ProviderType), setAsDefault: false);
            }
        }

        internal static void ApplyAssemblyDefaultProviders(IEnumerable<Assembly> assemblies)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            var declaredProviders = assemblies
                .SelectMany(GetAssemblyDefaultProviderNames)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (declaredProviders.Length == 0)
            {
                return;
            }

            if (declaredProviders.Length > 1)
            {
                throw new InvalidOperationException($"Multiple FastMoq default providers were declared across loaded assemblies: {string.Join(", ", declaredProviders.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))}. Declare only one assembly default provider per process.");
            }

            SetDefault(declaredProviders[0]);
        }

        internal static bool TryGetAssemblyDefaultProviderName(Assembly assembly, out string providerName)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            providerName = GetAssemblyDefaultProviderNames(assembly).FirstOrDefault() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(providerName);
        }

        private static IEnumerable<string> GetAssemblyDefaultProviderNames(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            var attribute = assembly.GetCustomAttributes<FastMoqDefaultProviderAttribute>().FirstOrDefault();
            if (attribute is not null && !string.IsNullOrWhiteSpace(attribute.ProviderName))
            {
                yield return attribute.ProviderName;
            }

            foreach (var registration in GetAssemblyProviderRegistrations(assembly))
            {
                if (registration.SetAsDefault)
                {
                    yield return registration.ProviderName;
                }
            }
        }

        private static IEnumerable<AssemblyProviderRegistration> GetAssemblyProviderRegistrations(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            foreach (var attribute in assembly.GetCustomAttributes<FastMoqRegisterProviderAttribute>())
            {
                if (string.IsNullOrWhiteSpace(attribute.ProviderName))
                {
                    continue;
                }

                yield return new AssemblyProviderRegistration(attribute.ProviderName, attribute.ProviderType, attribute.SetAsDefault);
            }
        }

        private static IMockingProvider CreateProviderInstance(string providerName, Type providerType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
            ArgumentNullException.ThrowIfNull(providerType);

            if (!typeof(IMockingProvider).IsAssignableFrom(providerType))
            {
                throw new InvalidOperationException($"FastMoq provider registration '{providerName}' references '{providerType.FullName}', which does not implement IMockingProvider.");
            }

            const BindingFlags STATIC_FLAGS = BindingFlags.Public | BindingFlags.Static;
            if (providerType.GetField("Instance", STATIC_FLAGS)?.GetValue(null) is IMockingProvider fieldProvider)
            {
                return fieldProvider;
            }

            if (providerType.GetProperty("Instance", STATIC_FLAGS)?.GetValue(null) is IMockingProvider propertyProvider)
            {
                return propertyProvider;
            }

            if (providerType.GetConstructor(Type.EmptyTypes) is not null &&
                Activator.CreateInstance(providerType) is IMockingProvider createdProvider)
            {
                return createdProvider;
            }

            throw new InvalidOperationException($"FastMoq provider registration '{providerName}' could not create '{providerType.FullName}'. Expose a public static Instance or a public parameterless constructor.");
        }

        private static bool TryEnsureOptionalProviderRegistered(string providerName)
        {
            if (!string.Equals(providerName, "nsubstitute", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Assembly? providerAssembly;
            try
            {
                providerAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, NSubstituteProviderAssemblyName, StringComparison.OrdinalIgnoreCase))
                    ?? Assembly.Load(new AssemblyName(NSubstituteProviderAssemblyName));
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (FileLoadException)
            {
                return false;
            }
            catch (BadImageFormatException)
            {
                return false;
            }

            var providerType = providerAssembly.GetType(NSubstituteProviderTypeName, throwOnError: false, ignoreCase: false);
            if (providerType is null || !typeof(IMockingProvider).IsAssignableFrom(providerType))
            {
                return false;
            }

            const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.Static;
            if (providerType.GetField("Instance", FLAGS)?.GetValue(null) is not IMockingProvider provider)
            {
                provider = providerType.GetProperty("Instance", FLAGS)?.GetValue(null) as IMockingProvider
                    ?? throw new InvalidOperationException($"Optional provider '{providerName}' does not expose a public static Instance of type IMockingProvider.");
            }

            Register(providerName, provider, setAsDefault: false);
            return true;
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

        private sealed record AssemblyProviderRegistration(string ProviderName, Type ProviderType, bool SetAsDefault);
    }
}

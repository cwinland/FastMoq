using FastMoq.Providers.MoqProvider;
using FastMoq.Providers.ReflectionProvider;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Providers
{
    /// <summary>
    /// Global registry for available mocking providers with support for scoped overrides.
    /// </summary>
    public static class MockingProviderRegistry
    {
        private static readonly ConcurrentDictionary<string, IMockingProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, MockingProviderRegistrationInfo> _providerRegistrations = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, string> _discoveryWarnings = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _discoveryLock = new();
        private static IMockingProvider? _default;
        private static readonly AsyncLocal<IMockingProvider?> _current = new();
        private static DefaultProviderSource _defaultSource;
        private static int _assemblyChangeVersion;
        private static int _lastCompletedDiscoveryVersion = -1;
        private static int _discoveryExecutionCount;
        private static int _discoveryMode = (int)MockingProviderDiscoveryMode.Automatic;
        private static bool _isDiscoveringProviders;

        static MockingProviderRegistry()
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            InitializeBuiltInProviders();
        }

        /// <summary>
        /// Registers a mocking provider under the supplied name.
        /// </summary>
        /// <param name="name">The provider name used to look up the registration later.</param>
        /// <param name="provider">The provider implementation to register.</param>
        /// <param name="setAsDefault">When <see langword="true"/>, also makes the provider the global default provider.</param>
        public static void Register(string name, IMockingProvider provider, bool setAsDefault = false)
        {
            RegisterCore(name, provider, setAsDefault, MockingProviderRegistrationSource.RuntimeRegistration);
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

            if (_isDiscoveringProviders)
            {
                provider = null!;
                return false;
            }

            EnsureDiscoveredProvidersRegistered();
            return _providers.TryGetValue(name, out provider!);
        }

        /// <summary>
        /// Gets the registered provider names ordered alphabetically.
        /// </summary>
        public static IReadOnlyCollection<string> RegisteredProviderNames => _providers.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

        /// <summary>
        /// Gets metadata for the currently registered providers ordered alphabetically by registration name.
        /// </summary>
        public static IReadOnlyCollection<MockingProviderRegistrationInfo> RegisteredProviders => _providerRegistrations.Values.OrderBy(info => info.Name, StringComparer.OrdinalIgnoreCase).ToArray();

        /// <summary>
        /// Gets discovery warnings collected while scanning assemblies for convention-based provider registrations.
        /// </summary>
        public static IReadOnlyCollection<string> DiscoveryWarnings => _discoveryWarnings.Values.OrderBy(message => message, StringComparer.OrdinalIgnoreCase).ToArray();

        /// <summary>
        /// Gets or sets how automatic provider discovery should behave for the current process.
        /// Set this before resolving providers when a test process must avoid loading referenced assemblies or disable convention-based discovery.
        /// </summary>
        public static MockingProviderDiscoveryMode DiscoveryMode
        {
            get => (MockingProviderDiscoveryMode)Volatile.Read(ref _discoveryMode);
            set
            {
                if (!Enum.IsDefined(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                var prior = Interlocked.Exchange(ref _discoveryMode, (int)value);
                if (prior == (int)value)
                {
                    return;
                }

                _lastCompletedDiscoveryVersion = -1;
                _discoveryWarnings.Clear();
            }
        }

        /// <summary>
        /// Gets the effective provider for the current async flow.
        /// A scoped provider pushed with <see cref="Push(IMockingProvider)"/> or <see cref="Push(string)"/> takes precedence over the global default.
        /// </summary>
        public static IMockingProvider Default
        {
            get
            {
                if (_current.Value is IMockingProvider currentProvider)
                {
                    return currentProvider;
                }

                if (_defaultSource is DefaultProviderSource.None or DefaultProviderSource.ReflectionFallback)
                {
                    EnsureDiscoveredProvidersRegistered();
                }

                return _default ?? throw new InvalidOperationException("No mocking provider registered. Register one via MockingProviderRegistry.Register().");
            }
        }

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

            if (_defaultSource is DefaultProviderSource.None or DefaultProviderSource.ReflectionFallback)
            {
                EnsureDiscoveredProvidersRegistered();
            }

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

        internal static IMockingProvider ResolveProvider(IFastMock mock)
        {
            ArgumentNullException.ThrowIfNull(mock);

            return mock is IProviderBoundFastMock providerBoundFastMock
                ? providerBoundFastMock.Provider
                : Default;
        }

        /// <summary>
        /// Sets the global default provider by name.
        /// </summary>
        /// <param name="name">The name of the provider to make the global default.</param>
        public static void SetDefault(string name)
        {
            SetDefaultProvider(name, DefaultProviderSource.ExplicitRuntime);
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
            _providerRegistrations.Clear();
            _discoveryWarnings.Clear();
            _default = null;
            _defaultSource = DefaultProviderSource.None;
            _lastCompletedDiscoveryVersion = -1;
            _current.Value = null;
        }

        /// <summary>
        /// Verifies that the specified invocation occurred exactly once on a detached provider-first mock handle.
        /// </summary>
        public static void VerifyCalledOnce<T>(IFastMock<T> mock, Expression<Action<T>> expression) where T : class
        {
            ArgumentNullException.ThrowIfNull(mock);
            ArgumentNullException.ThrowIfNull(expression);

            ResolveProvider(mock).Verify(mock, expression, TimesSpec.Once);
        }

        /// <summary>
        /// Verifies that the specified invocation occurred exactly the supplied number of times on a detached provider-first mock handle.
        /// </summary>
        public static void VerifyCalledExactly<T>(IFastMock<T> mock, Expression<Action<T>> expression, int times) where T : class
        {
            ArgumentNullException.ThrowIfNull(mock);
            ArgumentNullException.ThrowIfNull(expression);

            ResolveProvider(mock).Verify(mock, expression, TimesSpec.Exactly(times));
        }

        /// <summary>
        /// Verifies that the specified invocation occurred at least the supplied number of times on a detached provider-first mock handle.
        /// </summary>
        public static void VerifyCalledAtLeast<T>(IFastMock<T> mock, Expression<Action<T>> expression, int times) where T : class
        {
            ArgumentNullException.ThrowIfNull(mock);
            ArgumentNullException.ThrowIfNull(expression);

            ResolveProvider(mock).Verify(mock, expression, TimesSpec.AtLeast(times));
        }

        /// <summary>
        /// Verifies that the specified invocation occurred at most the supplied number of times on a detached provider-first mock handle.
        /// </summary>
        public static void VerifyCalledAtMost<T>(IFastMock<T> mock, Expression<Action<T>> expression, int times) where T : class
        {
            ArgumentNullException.ThrowIfNull(mock);
            ArgumentNullException.ThrowIfNull(expression);

            ResolveProvider(mock).Verify(mock, expression, TimesSpec.AtMost(times));
        }

        /// <summary>
        /// Verifies calls to the named method on a detached provider-first mock handle while treating every argument as a wildcard matcher.
        /// </summary>
        public static void VerifyAnyArgs<T>(IFastMock<T> mock, string methodName, TimesSpec? times = null, params Type[] parameterTypes) where T : class
        {
            ArgumentNullException.ThrowIfNull(mock);

            ResolveProvider(mock).Verify(mock, VerificationExpressionBuilder.BuildAnyArgsExpression<T>(methodName, parameterTypes), times);
        }

        /// <summary>
        /// Verifies calls to the selected method on a detached provider-first mock handle while treating every argument as a wildcard matcher.
        /// </summary>
        public static void VerifyAnyArgs<T, TDelegate>(IFastMock<T> mock, Func<T, TDelegate> methodSelector, TimesSpec? times = null)
            where T : class
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(mock);
            ArgumentNullException.ThrowIfNull(methodSelector);

            var method = VerificationExpressionBuilder.GetSelectedMethod(mock.Instance, methodSelector);
            ResolveProvider(mock).Verify(mock, VerificationExpressionBuilder.BuildAnyArgsExpression<T>(method), times);
        }

        /// <summary>
        /// Verifies that the specified invocation never occurred on a detached provider-first mock handle.
        /// </summary>
        public static void VerifyNotCalled<T>(IFastMock<T> mock, Expression<Action<T>> expression) where T : class
        {
            ArgumentNullException.ThrowIfNull(mock);
            ArgumentNullException.ThrowIfNull(expression);

            ResolveProvider(mock).Verify(mock, expression, TimesSpec.Never());
        }

        /// <summary>
        /// Verifies that the named method was called exactly once on a detached provider-first mock handle while treating every argument as a wildcard matcher.
        /// </summary>
        public static void VerifyCalledOnceAnyArgs<T>(IFastMock<T> mock, string methodName, params Type[] parameterTypes) where T : class
        {
            VerifyAnyArgs(mock, methodName, TimesSpec.Once, parameterTypes);
        }

        /// <summary>
        /// Verifies that the named method was called exactly the supplied number of times on a detached provider-first mock handle while treating every argument as a wildcard matcher.
        /// </summary>
        public static void VerifyCalledExactlyAnyArgs<T>(IFastMock<T> mock, string methodName, int times, params Type[] parameterTypes) where T : class
        {
            VerifyAnyArgs(mock, methodName, TimesSpec.Exactly(times), parameterTypes);
        }

        /// <summary>
        /// Verifies that the named method was called at least the supplied number of times on a detached provider-first mock handle while treating every argument as a wildcard matcher.
        /// </summary>
        public static void VerifyCalledAtLeastAnyArgs<T>(IFastMock<T> mock, string methodName, int times, params Type[] parameterTypes) where T : class
        {
            VerifyAnyArgs(mock, methodName, TimesSpec.AtLeast(times), parameterTypes);
        }

        /// <summary>
        /// Verifies that the named method was called at most the supplied number of times on a detached provider-first mock handle while treating every argument as a wildcard matcher.
        /// </summary>
        public static void VerifyCalledAtMostAnyArgs<T>(IFastMock<T> mock, string methodName, int times, params Type[] parameterTypes) where T : class
        {
            VerifyAnyArgs(mock, methodName, TimesSpec.AtMost(times), parameterTypes);
        }

        /// <summary>
        /// Verifies that the selected method was called exactly once on a detached provider-first mock handle while treating every argument as a wildcard matcher.
        /// </summary>
        public static void VerifyCalledOnceAnyArgs<T, TDelegate>(IFastMock<T> mock, Func<T, TDelegate> methodSelector)
            where T : class
            where TDelegate : Delegate
        {
            VerifyAnyArgs(mock, methodSelector, TimesSpec.Once);
        }

        /// <summary>
        /// Verifies that the selected method was called exactly the supplied number of times on a detached provider-first mock handle while treating every argument as a wildcard matcher.
        /// </summary>
        public static void VerifyCalledExactlyAnyArgs<T, TDelegate>(IFastMock<T> mock, Func<T, TDelegate> methodSelector, int times)
            where T : class
            where TDelegate : Delegate
        {
            VerifyAnyArgs(mock, methodSelector, TimesSpec.Exactly(times));
        }

        /// <summary>
        /// Verifies that the selected method was called at least the supplied number of times on a detached provider-first mock handle while treating every argument as a wildcard matcher.
        /// </summary>
        public static void VerifyCalledAtLeastAnyArgs<T, TDelegate>(IFastMock<T> mock, Func<T, TDelegate> methodSelector, int times)
            where T : class
            where TDelegate : Delegate
        {
            VerifyAnyArgs(mock, methodSelector, TimesSpec.AtLeast(times));
        }

        /// <summary>
        /// Verifies that the selected method was called at most the supplied number of times on a detached provider-first mock handle while treating every argument as a wildcard matcher.
        /// </summary>
        public static void VerifyCalledAtMostAnyArgs<T, TDelegate>(IFastMock<T> mock, Func<T, TDelegate> methodSelector, int times)
            where T : class
            where TDelegate : Delegate
        {
            VerifyAnyArgs(mock, methodSelector, TimesSpec.AtMost(times));
        }

        /// <summary>
        /// Verifies that the named method was never called on a detached provider-first mock handle while treating every argument as a wildcard matcher.
        /// </summary>
        public static void VerifyNotCalledAnyArgs<T>(IFastMock<T> mock, string methodName, params Type[] parameterTypes) where T : class
        {
            VerifyAnyArgs(mock, methodName, TimesSpec.Never(), parameterTypes);
        }

        /// <summary>
        /// Verifies that the selected method was never called on a detached provider-first mock handle while treating every argument as a wildcard matcher.
        /// </summary>
        public static void VerifyNotCalledAnyArgs<T, TDelegate>(IFastMock<T> mock, Func<T, TDelegate> methodSelector)
            where T : class
            where TDelegate : Delegate
        {
            VerifyAnyArgs(mock, methodSelector, TimesSpec.Never());
        }

        /// <summary>
        /// Verifies that no other invocations were recorded on a detached provider-first mock handle.
        /// </summary>
        public static void VerifyNoOtherCalls(IFastMock mock)
        {
            ArgumentNullException.ThrowIfNull(mock);

            ResolveProvider(mock).VerifyNoOtherCalls(mock);
        }

        internal static void InitializeBuiltInProviders(bool includeMoqProvider = true)
        {
            Clear();
            RegisterCore("reflection", ReflectionMockingProvider.Instance, setAsDefault: false, MockingProviderRegistrationSource.BuiltIn);

            if (includeMoqProvider)
            {
                RegisterCore("moq", MoqMockingProvider.Instance, setAsDefault: false, MockingProviderRegistrationSource.BuiltIn);
            }

            SetDefaultProviderCore(ReflectionMockingProvider.Instance, DefaultProviderSource.ReflectionFallback);
        }

        internal static void ApplyAssemblyProviderRegistrations(IEnumerable<Assembly> assemblies)
        {
            ApplyAssemblyProviderRegistrations(assemblies, includeConventionDiscovery: true);
        }

        internal static void ApplyAssemblyProviderRegistrations(IEnumerable<Assembly> assemblies, bool includeConventionDiscovery)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            var assemblyArray = assemblies.ToArray();
            var explicitRegistrationGroups = assemblyArray
                .SelectMany(GetAssemblyProviderRegistrations)
                .GroupBy(registration => registration.ProviderName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var group in explicitRegistrationGroups)
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
                if (_providers.TryGetValue(registration.ProviderName, out var existingProvider))
                {
                    if (existingProvider.GetType() != registration.ProviderType)
                    {
                        RecordDiscoveryWarning(
                            registration.ProviderName,
                            $"Skipped automatic registration for provider name '{registration.ProviderName}' because it is already registered to '{GetProviderTypeDisplay(existingProvider.GetType())}', while assembly registration found '{GetProviderTypeDisplay(registration.ProviderType)}'. Register the intended provider explicitly with a stable alias.");
                    }

                    continue;
                }

                RegisterCore(
                    registration.ProviderName,
                    CreateProviderInstance(registration.ProviderName, registration.ProviderType),
                    setAsDefault: false,
                    MockingProviderRegistrationSource.AssemblyAttribute);
            }

            if (!includeConventionDiscovery)
            {
                return;
            }

            var explicitlyRegisteredProviderTypes = explicitRegistrationGroups
                .Select(static group => group.First().ProviderType)
                .ToHashSet();

            var conventionRegistrationGroups = assemblyArray
                .SelectMany(assembly => GetConventionProviderRegistrations(assembly, explicitlyRegisteredProviderTypes))
                .GroupBy(registration => registration.ProviderName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var group in conventionRegistrationGroups)
            {
                var distinctProviderTypes = group
                    .Select(static registration => registration.ProviderType)
                    .Distinct()
                    .ToArray();

                if (distinctProviderTypes.Length > 1)
                {
                    RecordDiscoveryWarning(
                        group.Key,
                        $"Skipped automatic registration for provider name '{group.Key}' because multiple discoverable IMockingProvider types matched that fallback name: {string.Join(", ", distinctProviderTypes.Select(GetProviderTypeDisplay).OrderBy(name => name, StringComparer.Ordinal))}. Register the intended provider explicitly with a stable alias.");
                    continue;
                }

                var registration = group.First();

                if (_providers.TryGetValue(registration.ProviderName, out var existingProvider))
                {
                    if (existingProvider.GetType() != registration.ProviderType)
                    {
                        RecordDiscoveryWarning(
                            registration.ProviderName,
                            $"Skipped automatic registration for provider name '{registration.ProviderName}' because it is already registered to '{GetProviderTypeDisplay(existingProvider.GetType())}', while convention discovery found '{GetProviderTypeDisplay(registration.ProviderType)}'. Register the intended provider explicitly with a stable alias.");
                    }

                    continue;
                }

                RegisterCore(
                    registration.ProviderName,
                    CreateProviderInstance(registration.ProviderName, registration.ProviderType),
                    setAsDefault: false,
                    MockingProviderRegistrationSource.ConventionDiscovery);
            }
        }

        internal static bool ApplyAssemblyDefaultProviders(IEnumerable<Assembly> assemblies)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            var declaredProviders = assemblies
                .SelectMany(GetAssemblyDefaultProviderNames)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (declaredProviders.Length == 0)
            {
                return false;
            }

            if (declaredProviders.Length > 1)
            {
                throw new InvalidOperationException($"Multiple FastMoq default providers were declared across loaded assemblies: {string.Join(", ", declaredProviders.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))}. Declare only one assembly default provider per process.");
            }

            SetDefaultProvider(declaredProviders[0], DefaultProviderSource.AssemblyDefault);
            return true;
        }

        internal static void ApplyImplicitDefaultProvider()
        {
            if (_defaultSource is DefaultProviderSource.ExplicitRegistration or DefaultProviderSource.ExplicitRuntime or DefaultProviderSource.AssemblyDefault)
            {
                return;
            }

            var candidates = _providers
                .Select(entry => entry.Value)
                .Where(static provider => provider is not ReflectionMockingProvider)
                .GroupBy(static provider => provider.GetType())
                .Select(static group => group.First())
                .ToArray();

            if (candidates.Length == 1)
            {
                SetDefaultProviderCore(candidates[0], DefaultProviderSource.ImplicitSelection);
                return;
            }

            var reflectionProvider = _providers.Values.FirstOrDefault(static provider => provider is ReflectionMockingProvider);
            if (reflectionProvider is not null)
            {
                SetDefaultProviderCore(reflectionProvider, DefaultProviderSource.ReflectionFallback);
            }
        }

        internal static void EnsureDiscoveredProvidersRegistered()
        {
            if (_isDiscoveringProviders)
            {
                return;
            }

            var assemblyChangeVersion = Volatile.Read(ref _assemblyChangeVersion);
            if (_lastCompletedDiscoveryVersion == assemblyChangeVersion)
            {
                return;
            }

            lock (_discoveryLock)
            {
                assemblyChangeVersion = Volatile.Read(ref _assemblyChangeVersion);
                if (_isDiscoveringProviders || _lastCompletedDiscoveryVersion == assemblyChangeVersion)
                {
                    return;
                }

                _isDiscoveringProviders = true;
                try
                {
                    Interlocked.Increment(ref _discoveryExecutionCount);
                    var discoveryMode = DiscoveryMode;
                    var assemblies = LoadAssembliesForProviderDiscovery(discoveryMode);
                    ApplyAssemblyProviderRegistrations(assemblies, includeConventionDiscovery: discoveryMode != MockingProviderDiscoveryMode.ExplicitOnly);
                    var explicitDefaultApplied = ApplyAssemblyDefaultProviders(assemblies);
                    if (!explicitDefaultApplied)
                    {
                        ApplyImplicitDefaultProvider();
                    }

                    _lastCompletedDiscoveryVersion = Volatile.Read(ref _assemblyChangeVersion);
                }
                finally
                {
                    _isDiscoveringProviders = false;
                }
            }
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

        private static IEnumerable<AssemblyProviderRegistration> GetConventionProviderRegistrations(
            Assembly assembly,
            ISet<Type> explicitlyRegisteredProviderTypes)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(explicitlyRegisteredProviderTypes);

            foreach (var providerType in GetLoadableTypes(assembly))
            {
                if (!IsConventionDiscoverableProviderType(providerType) ||
                    explicitlyRegisteredProviderTypes.Contains(providerType) ||
                    _providers.Values.Any(provider => provider.GetType() == providerType))
                {
                    continue;
                }

                var providerName = providerType.FullName;
                if (string.IsNullOrWhiteSpace(providerName))
                {
                    continue;
                }

                yield return new AssemblyProviderRegistration(providerName, providerType, SetAsDefault: false);
            }
        }

        private static bool IsConventionDiscoverableProviderType(Type providerType)
        {
            ArgumentNullException.ThrowIfNull(providerType);

            if (!typeof(IMockingProvider).IsAssignableFrom(providerType) ||
                providerType.IsAbstract ||
                providerType.IsInterface ||
                providerType.ContainsGenericParameters ||
                !providerType.IsVisible)
            {
                return false;
            }

            return HasProviderFactory(providerType);
        }

        private static bool HasProviderFactory(Type providerType)
        {
            ArgumentNullException.ThrowIfNull(providerType);

            const BindingFlags STATIC_FLAGS = BindingFlags.Public | BindingFlags.Static;
            return providerType.GetField("Instance", STATIC_FLAGS)?.FieldType is Type fieldType && typeof(IMockingProvider).IsAssignableFrom(fieldType) ||
                providerType.GetProperty("Instance", STATIC_FLAGS)?.PropertyType is Type propertyType && typeof(IMockingProvider).IsAssignableFrom(propertyType) ||
                providerType.GetConstructor(Type.EmptyTypes) is not null;
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

        private static void RegisterCore(string name, IMockingProvider provider, bool setAsDefault, MockingProviderRegistrationSource source)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(provider);

            _providers[name] = provider;
            _providerRegistrations[name] = new MockingProviderRegistrationInfo(name, provider.GetType(), source);
            _discoveryWarnings.TryRemove(name, out _);

            if (setAsDefault)
            {
                SetDefaultProviderCore(provider, DefaultProviderSource.ExplicitRegistration);
                return;
            }

            if (_default is null)
            {
                _default = provider;
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(static type => type is not null)!;
            }
        }

        private static void RecordDiscoveryWarning(string providerName, string message)
        {
            ArgumentNullException.ThrowIfNull(providerName);
            ArgumentNullException.ThrowIfNull(message);

            _discoveryWarnings[providerName] = message;
        }

        private static string GetProviderTypeDisplay(Type providerType)
        {
            ArgumentNullException.ThrowIfNull(providerType);

            return providerType.AssemblyQualifiedName ?? providerType.FullName ?? providerType.Name;
        }

        private static Assembly[] LoadAssembliesForProviderDiscovery(MockingProviderDiscoveryMode discoveryMode)
        {
            var assemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                AddAssembly(assemblies, assembly);
            }

            if (discoveryMode != MockingProviderDiscoveryMode.Automatic)
            {
                return assemblies.Values.ToArray();
            }

            var referencedAssemblies = assemblies.Values
                .SelectMany(GetReferencedAssembliesSafe)
                .Where(reference => !ContainsAssembly(assemblies.Values, reference))
                .GroupBy(reference => reference.FullName ?? reference.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();

            foreach (var reference in referencedAssemblies)
            {
                if (!ShouldAttemptDiscoveryLoad(reference))
                {
                    continue;
                }

                var loadedAssembly = TryLoadAssembly(reference);
                if (loadedAssembly is null || loadedAssembly.IsDynamic)
                {
                    continue;
                }

                AddAssembly(assemblies, loadedAssembly);
            }

            return assemblies.Values.ToArray();
        }

        private static void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs eventArgs)
        {
            if (eventArgs.LoadedAssembly.IsDynamic)
            {
                return;
            }

            Interlocked.Increment(ref _assemblyChangeVersion);
        }

        private static void SetDefaultProvider(string name, DefaultProviderSource source)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (!_providers.TryGetValue(name, out var provider) && !TryGet(name, out provider!))
            {
                throw new InvalidOperationException($"Unable to find provider '{name}'. Registered providers: {string.Join(", ", RegisteredProviderNames)}.");
            }

            SetDefaultProviderCore(provider, source);
        }

        private static void SetDefaultProviderCore(IMockingProvider provider, DefaultProviderSource source)
        {
            ArgumentNullException.ThrowIfNull(provider);

            _default = provider;
            _defaultSource = source;
        }

        private static void AddAssembly(IDictionary<string, Assembly> assemblies, Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assemblies);
            ArgumentNullException.ThrowIfNull(assembly);

            var key = assembly.FullName ?? assembly.GetName().Name;
            if (!string.IsNullOrWhiteSpace(key))
            {
                assemblies[key] = assembly;
            }
        }

        private static bool ContainsAssembly(IEnumerable<Assembly> assemblies, AssemblyName reference)
        {
            ArgumentNullException.ThrowIfNull(assemblies);
            ArgumentNullException.ThrowIfNull(reference);

            return assemblies.Any(assembly => AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), reference));
        }

        private static IEnumerable<AssemblyName> GetReferencedAssembliesSafe(Assembly assembly)
        {
            try
            {
                return assembly.GetReferencedAssemblies();
            }
            catch
            {
                return Array.Empty<AssemblyName>();
            }
        }

        private static bool ShouldAttemptDiscoveryLoad(AssemblyName reference)
        {
            ArgumentNullException.ThrowIfNull(reference);

            var simpleName = reference.Name;
            if (string.IsNullOrWhiteSpace(simpleName))
            {
                return false;
            }

            return !simpleName.Equals("System", StringComparison.OrdinalIgnoreCase)
                && !simpleName.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
                && !simpleName.Equals("Microsoft", StringComparison.OrdinalIgnoreCase)
                && !simpleName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase);
        }

        private static Assembly? TryLoadAssembly(AssemblyName reference)
        {
            ArgumentNullException.ThrowIfNull(reference);

            try
            {
                return AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(assembly => !assembly.IsDynamic && AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), reference))
                    ?? Assembly.Load(reference);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (FileLoadException)
            {
                return null;
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        private sealed class PopDisposable : IDisposable
        {
            private readonly IMockingProvider? _prior;
            private bool _disposed;
            public PopDisposable(IMockingProvider? prior) => _prior = prior;
            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _current.Value = _prior;
                _disposed = true;
            }
        }

        private sealed record AssemblyProviderRegistration(string ProviderName, Type ProviderType, bool SetAsDefault);

        private enum DefaultProviderSource
        {
            None,
            ReflectionFallback,
            ImplicitSelection,
            ExplicitRegistration,
            AssemblyDefault,
            ExplicitRuntime,
        }
    }
}

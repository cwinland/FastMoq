using Microsoft.Extensions.DependencyInjection;

namespace FastMoq.Extensions
{
    /// <summary>
    /// Provides typed service-provider helpers for framework-heavy test setup.
    /// </summary>
    public static class ServiceProviderTestExtensions
    {
        /// <summary>
        /// Creates a typed <see cref="IServiceProvider" /> from the supplied service registrations.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="configureServices">Optional service registrations to apply before the provider is built.</param>
        /// <param name="includeMockerFallback">True to fall back to the current <see cref="Mocker" /> for unregistered class and interface resolutions.</param>
        /// <returns>A real service provider that resolves services by requested type.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var serviceProvider = Mocks.CreateTypedServiceProvider(services =>
        /// {
        ///     services.AddSingleton(new Uri("https://fastmoq.dev"));
        ///     services.AddOptions();
        /// });
        /// ]]></code>
        /// </example>
        public static IServiceProvider CreateTypedServiceProvider(this Mocker mocker, Action<IServiceCollection>? configureServices = null, bool includeMockerFallback = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            var services = new ServiceCollection();
            configureServices?.Invoke(services);
            var serviceProvider = services.BuildServiceProvider();
            if (!includeMockerFallback)
            {
                return serviceProvider;
            }

            return new MockerBackedServiceProvider(mocker, serviceProvider);
        }

        /// <summary>
        /// Creates a typed <see cref="IServiceScope" /> from the supplied service registrations.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="configureServices">Optional service registrations to apply before the scope is built.</param>
        /// <param name="includeMockerFallback">True to fall back to the current <see cref="Mocker" /> for unregistered class and interface resolutions.</param>
        /// <returns>A real service scope whose <see cref="IServiceScope.ServiceProvider" /> resolves services by requested type.</returns>
        public static IServiceScope CreateTypedServiceScope(this Mocker mocker, Action<IServiceCollection>? configureServices = null, bool includeMockerFallback = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            var serviceProvider = mocker.CreateTypedServiceProvider(configureServices, includeMockerFallback);
            if (serviceProvider.GetService(typeof(IServiceScopeFactory)) is not IServiceScopeFactory scopeFactory)
            {
                DisposeCreatedRegistration(serviceProvider);
                throw new InvalidOperationException("The typed service provider did not expose an IServiceScopeFactory.");
            }

            try
            {
                return new OwnedServiceScope(scopeFactory.CreateScope(), serviceProvider);
            }
            catch
            {
                DisposeCreatedRegistration(serviceProvider);
                throw;
            }
        }

        /// <summary>
        /// Registers a typed <see cref="IServiceProvider" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="serviceProvider">The provider to register.</param>
        /// <param name="replace">True to replace an existing registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddServiceProvider(this Mocker mocker, IServiceProvider serviceProvider, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(serviceProvider);

            mocker.AddType<IServiceProvider>(serviceProvider, replace);

            if (serviceProvider.GetService(typeof(IServiceScopeFactory)) is IServiceScopeFactory scopeFactory)
            {
                mocker.AddType<IServiceScopeFactory>(scopeFactory, replace);
            }

            if (serviceProvider.GetService(typeof(IServiceProviderIsService)) is IServiceProviderIsService serviceProviderIsService)
            {
                mocker.AddType<IServiceProviderIsService>(serviceProviderIsService, replace);
            }

            return mocker;
        }

        /// <summary>
        /// Registers a typed <see cref="IServiceScope" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="serviceScope">The scope to register.</param>
        /// <param name="replace">True to replace an existing registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddServiceScope(this Mocker mocker, IServiceScope serviceScope, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(serviceScope);

            mocker.AddType<IServiceScope>(serviceScope, replace);
            return mocker.AddServiceProvider(serviceScope.ServiceProvider, replace);
        }

        /// <summary>
        /// Builds and registers a typed <see cref="IServiceProvider" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="configureServices">Optional service registrations to apply before the provider is built.</param>
        /// <param name="replace">True to replace an existing registration.</param>
        /// <param name="includeMockerFallback">True to fall back to the current <see cref="Mocker" /> for unregistered class and interface resolutions.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddServiceProvider(this Mocker mocker, Action<IServiceCollection>? configureServices, bool replace = false, bool includeMockerFallback = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            var serviceProvider = mocker.CreateTypedServiceProvider(configureServices, includeMockerFallback);
            try
            {
                mocker.AddServiceProvider(serviceProvider, replace);
                mocker.TrackOwnedRegistration(serviceProvider);
                return mocker;
            }
            catch
            {
                DisposeCreatedRegistration(serviceProvider);
                throw;
            }
        }

        /// <summary>
        /// Builds and registers a typed <see cref="IServiceScope" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="configureServices">Optional service registrations to apply before the scope is built.</param>
        /// <param name="replace">True to replace an existing registration.</param>
        /// <param name="includeMockerFallback">True to fall back to the current <see cref="Mocker" /> for unregistered class and interface resolutions.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddServiceScope(this Mocker mocker, Action<IServiceCollection>? configureServices, bool replace = false, bool includeMockerFallback = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            var serviceScope = mocker.CreateTypedServiceScope(configureServices, includeMockerFallback);
            try
            {
                mocker.AddServiceScope(serviceScope, replace);
                mocker.TrackOwnedRegistration(serviceScope);
                return mocker;
            }
            catch
            {
                DisposeCreatedRegistration(serviceScope);
                throw;
            }
        }

        private static void DisposeCreatedRegistration(object createdRegistration)
        {
            ArgumentNullException.ThrowIfNull(createdRegistration);

            if (createdRegistration is IDisposable disposable)
            {
                disposable.Dispose();
                return;
            }

            if (createdRegistration is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
    }

    internal sealed class OwnedServiceScope : IServiceScope, IAsyncDisposable
    {
        private readonly object _owner;
        private readonly IServiceScope _scope;

        public OwnedServiceScope(IServiceScope scope, object owner)
        {
            ArgumentNullException.ThrowIfNull(scope);
            ArgumentNullException.ThrowIfNull(owner);

            _scope = scope;
            _owner = owner;
        }

        public IServiceProvider ServiceProvider => _scope.ServiceProvider;

        public void Dispose()
        {
            try
            {
                _scope.Dispose();
            }
            finally
            {
                if (_owner is IDisposable ownedDisposable)
                {
                    ownedDisposable.Dispose();
                }
                else if (_owner is IAsyncDisposable ownedAsyncDisposable)
                {
                    ownedAsyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_scope is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    _scope.Dispose();
                }
            }
            finally
            {
                if (_owner is IAsyncDisposable ownedAsyncDisposable)
                {
                    await ownedAsyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else if (_owner is IDisposable ownedDisposable)
                {
                    ownedDisposable.Dispose();
                }
            }
        }
    }

    internal sealed class MockerBackedServiceProvider : IServiceProvider, IServiceScopeFactory, IServiceProviderIsService, IDisposable, IAsyncDisposable
    {
        private readonly IServiceProviderIsService? _innerServiceProviderIsService;
        private readonly Mocker _mocker;
        private readonly ServiceProvider _serviceProvider;

        public MockerBackedServiceProvider(Mocker mocker, ServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(serviceProvider);

            _mocker = mocker;
            _serviceProvider = serviceProvider;
            _innerServiceProviderIsService = serviceProvider.GetService<IServiceProviderIsService>();
        }

        public object? GetService(Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            if (serviceType == typeof(IServiceProvider))
            {
                return this;
            }

            if (serviceType == typeof(IServiceScopeFactory))
            {
                return this;
            }

            if (serviceType == typeof(IServiceProviderIsService))
            {
                return this;
            }

            var resolved = _serviceProvider.GetService(serviceType);
            if (resolved is not null)
            {
                return resolved;
            }

            return CanResolveFromMocker(serviceType)
                ? _mocker.GetObject(serviceType)
                : null;
        }

        public IServiceScope CreateScope()
        {
            var innerScope = _serviceProvider.CreateScope();
            return new MockerBackedServiceScope(_mocker, innerScope, this);
        }

        public bool IsService(Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            if (IsBuiltInService(serviceType))
            {
                return true;
            }

            if (_innerServiceProviderIsService?.IsService(serviceType) == true)
            {
                return true;
            }

            return _mocker.Contains(serviceType) || _mocker.HasTypeRegistration(serviceType) || CanResolveFromMocker(serviceType);
        }

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return _serviceProvider.DisposeAsync();
        }

        private static bool CanResolveFromMocker(Type serviceType)
        {
            return serviceType.IsInterface || !serviceType.IsSealed && serviceType.IsClass;
        }

        private static bool IsBuiltInService(Type serviceType)
        {
            return serviceType == typeof(IServiceProvider) ||
                   serviceType == typeof(IServiceScopeFactory) ||
                   serviceType == typeof(IServiceProviderIsService);
        }
    }

    internal sealed class MockerBackedServiceScope : IServiceScope, IAsyncDisposable
    {
        private readonly IServiceScope _innerScope;

        public MockerBackedServiceScope(Mocker mocker, IServiceScope innerScope, IServiceScopeFactory scopeFactory)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(innerScope);
            ArgumentNullException.ThrowIfNull(scopeFactory);

            _innerScope = innerScope;
            ServiceProvider = new MockerBackedScopedServiceProvider(mocker, innerScope.ServiceProvider, scopeFactory);
        }

        public IServiceProvider ServiceProvider { get; }

        public void Dispose()
        {
            _innerScope.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            if (_innerScope is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }

            _innerScope.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    internal sealed class MockerBackedScopedServiceProvider : IServiceProvider, IServiceProviderIsService
    {
        private readonly IServiceProviderIsService? _innerServiceProviderIsService;
        private readonly Mocker _mocker;
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScopeFactory _scopeFactory;

        public MockerBackedScopedServiceProvider(Mocker mocker, IServiceProvider serviceProvider, IServiceScopeFactory scopeFactory)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(scopeFactory);

            _mocker = mocker;
            _serviceProvider = serviceProvider;
            _scopeFactory = scopeFactory;
            _innerServiceProviderIsService = serviceProvider.GetService(typeof(IServiceProviderIsService)) as IServiceProviderIsService;
        }

        public object? GetService(Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            if (serviceType == typeof(IServiceProvider))
            {
                return this;
            }

            if (serviceType == typeof(IServiceScopeFactory))
            {
                return _scopeFactory;
            }

            if (serviceType == typeof(IServiceProviderIsService))
            {
                return this;
            }

            var resolved = _serviceProvider.GetService(serviceType);
            if (resolved is not null)
            {
                return resolved;
            }

            return CanResolveFromMocker(serviceType)
                ? _mocker.GetObject(serviceType)
                : null;
        }

        public bool IsService(Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            if (serviceType == typeof(IServiceProvider) ||
                serviceType == typeof(IServiceScopeFactory) ||
                serviceType == typeof(IServiceProviderIsService))
            {
                return true;
            }

            if (_innerServiceProviderIsService?.IsService(serviceType) == true)
            {
                return true;
            }

            return _mocker.Contains(serviceType) || _mocker.HasTypeRegistration(serviceType) || CanResolveFromMocker(serviceType);
        }

        private static bool CanResolveFromMocker(Type serviceType)
        {
            return serviceType.IsInterface || !serviceType.IsSealed && serviceType.IsClass;
        }
    }
}
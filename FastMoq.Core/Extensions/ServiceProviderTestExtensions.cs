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
        public static IServiceProvider CreateTypedServiceProvider(this Mocker mocker, Action<IServiceCollection>? configureServices = null)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            var services = new ServiceCollection();
            configureServices?.Invoke(services);
            return services.BuildServiceProvider();
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
        /// Builds and registers a typed <see cref="IServiceProvider" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="configureServices">Optional service registrations to apply before the provider is built.</param>
        /// <param name="replace">True to replace an existing registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddServiceProvider(this Mocker mocker, Action<IServiceCollection>? configureServices, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            return mocker.AddServiceProvider(mocker.CreateTypedServiceProvider(configureServices), replace);
        }
    }
}
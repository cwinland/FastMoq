using FastMoq.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FastMoq.Azure.DependencyInjection
{
    /// <summary>
    /// Provides Azure-oriented configuration, client registration, and typed service-provider helpers.
    /// </summary>
    public static class AzureDependencyInjectionTestExtensions
    {
        /// <summary>
        /// Creates an in-memory Azure test configuration.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="initialValues">Optional key-value pairs to seed into the configuration.</param>
        /// <returns>An in-memory configuration root.</returns>
        public static IConfigurationRoot CreateAzureConfiguration(this Mocker mocker, IEnumerable<KeyValuePair<string, string?>>? initialValues = null)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            var builder = new ConfigurationBuilder();
            if (initialValues is not null)
            {
                builder.AddInMemoryCollection(initialValues);
            }

            return builder.Build();
        }

        /// <summary>
        /// Registers Azure-oriented configuration types for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="configuration">The configuration to register.</param>
        /// <param name="replace">True to replace existing registrations.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddAzureConfiguration(this Mocker mocker, IConfiguration configuration, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(configuration);

            var configurationRoot = configuration as IConfigurationRoot;

            mocker.AddType<IConfiguration>(configuration, replace);
            if (configurationRoot is not null)
            {
                mocker.AddType<IConfigurationRoot>(configurationRoot, replace);
            }

            return mocker;
        }

        /// <summary>
        /// Builds and registers an in-memory Azure test configuration for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="initialValues">The configuration values to seed into the configuration.</param>
        /// <param name="replace">True to replace existing registrations.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddAzureConfiguration(this Mocker mocker, IEnumerable<KeyValuePair<string, string?>> initialValues, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(initialValues);

            return mocker.AddAzureConfiguration(mocker.CreateAzureConfiguration(initialValues), replace);
        }

        /// <summary>
        /// Creates a typed <see cref="IServiceProvider" /> with common Azure testing defaults.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="configureServices">Optional service registrations to apply after the Azure defaults.</param>
        /// <param name="configurationValues">Optional configuration values to register as an in-memory configuration root.</param>
        /// <returns>A real service provider suitable for Azure-focused tests.</returns>
        public static IServiceProvider CreateAzureServiceProvider(this Mocker mocker, Action<IServiceCollection>? configureServices = null, IEnumerable<KeyValuePair<string, string?>>? configurationValues = null)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            var configuration = mocker.CreateAzureConfiguration(configurationValues);

            return mocker.CreateTypedServiceProvider(services =>
            {
                services.AddLogging();
                services.AddOptions();
                services.AddSingleton<IConfiguration>(configuration);
                services.AddSingleton<IConfigurationRoot>(configuration);
                configureServices?.Invoke(services);
            });
        }

        /// <summary>
        /// Registers a typed Azure-oriented <see cref="IServiceProvider" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="serviceProvider">The service provider to register.</param>
        /// <param name="replace">True to replace existing registrations.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddAzureServiceProvider(this Mocker mocker, IServiceProvider serviceProvider, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(serviceProvider);

            var configuration = serviceProvider.GetService(typeof(IConfiguration)) as IConfiguration;
            var configurationRoot = serviceProvider.GetService(typeof(IConfigurationRoot)) as IConfigurationRoot ?? configuration as IConfigurationRoot;

            mocker.AddServiceProvider(serviceProvider, replace);
            if (configuration is not null)
            {
                mocker.AddType<IConfiguration>(configuration, replace);
            }

            if (configurationRoot is not null)
            {
                mocker.AddType<IConfigurationRoot>(configurationRoot, replace);
            }

            return mocker;
        }

        /// <summary>
        /// Builds and registers a typed Azure-oriented <see cref="IServiceProvider" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="configureServices">Optional service registrations to apply after the Azure defaults.</param>
        /// <param name="configurationValues">Optional configuration values to register as an in-memory configuration root.</param>
        /// <param name="replace">True to replace existing registrations.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddAzureServiceProvider(this Mocker mocker, Action<IServiceCollection>? configureServices = null, IEnumerable<KeyValuePair<string, string?>>? configurationValues = null, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            return mocker.AddAzureServiceProvider(mocker.CreateAzureServiceProvider(configureServices, configurationValues), replace);
        }

        /// <summary>
        /// Registers an Azure client instance as a singleton service.
        /// </summary>
        /// <typeparam name="TClient">The client type to register.</typeparam>
        /// <param name="services">The service collection to update.</param>
        /// <param name="client">The client instance to register.</param>
        /// <returns>The current <see cref="IServiceCollection" /> instance.</returns>
        public static IServiceCollection AddAzureClient<TClient>(this IServiceCollection services, TClient client)
            where TClient : class
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(client);

            services.AddSingleton(client);
            return services;
        }

        /// <summary>
        /// Registers an Azure client instance for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <typeparam name="TClient">The client type to register.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="client">The client instance to register.</param>
        /// <param name="replace">True to replace an existing registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddAzureClient<TClient>(this Mocker mocker, TClient client, bool replace = false)
            where TClient : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(client);

            return mocker.AddType<TClient>(client, replace);
        }
    }
}
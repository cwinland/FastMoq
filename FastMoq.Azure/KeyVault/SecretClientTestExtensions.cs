using Azure.Security.KeyVault.Secrets;
using FastMoq.Azure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace FastMoq.Azure.KeyVault
{
    /// <summary>
    /// Provides <see cref="SecretClient" /> registration helpers for tests.
    /// </summary>
    public static class SecretClientTestExtensions
    {
        /// <summary>
        /// Registers a <see cref="SecretClient" /> singleton in the supplied service collection.
        /// </summary>
        /// <param name="services">The service collection to update.</param>
        /// <param name="secretClient">The client instance to register.</param>
        /// <returns>The current <see cref="IServiceCollection" /> instance.</returns>
        public static IServiceCollection AddSecretClient(this IServiceCollection services, SecretClient secretClient)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(secretClient);

            return services.AddAzureClient(secretClient);
        }

        /// <summary>
        /// Registers a <see cref="SecretClient" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="secretClient">The client instance to register.</param>
        /// <param name="replace">True to replace an existing registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddSecretClient(this Mocker mocker, SecretClient secretClient, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(secretClient);

            return mocker.AddAzureClient(secretClient, replace);
        }
    }
}
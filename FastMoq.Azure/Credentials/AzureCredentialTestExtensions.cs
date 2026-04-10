using Azure.Core;
using Azure.Identity;

namespace FastMoq.Azure.Credentials
{
    /// <summary>
    /// Provides Azure credential registration helpers for <see cref="Mocker" />.
    /// </summary>
    public static class AzureCredentialTestExtensions
    {
        /// <summary>
        /// Registers a <see cref="TokenCredential" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="credential">The credential to register.</param>
        /// <param name="replace">True to replace an existing registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddTokenCredential(this Mocker mocker, TokenCredential credential, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(credential);

            return mocker.AddType<TokenCredential>(credential, replace);
        }

        /// <summary>
        /// Registers a fixed-token <see cref="TokenCredential" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="token">The bearer token to return for every request.</param>
        /// <param name="expiresOn">The token expiry to return. When omitted, the token expires one hour from construction.</param>
        /// <param name="replace">True to replace an existing registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddTokenCredential(this Mocker mocker, string token, DateTimeOffset? expiresOn = null, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            return mocker.AddTokenCredential(new TestTokenCredential(token, expiresOn), replace);
        }

        /// <summary>
        /// Registers a concrete <see cref="DefaultAzureCredential" /> and exposes it as both <see cref="DefaultAzureCredential" /> and <see cref="TokenCredential" />.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="credential">The credential to register.</param>
        /// <param name="replace">True to replace existing registrations.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddDefaultAzureCredential(this Mocker mocker, DefaultAzureCredential credential, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(credential);

            mocker.AddType<DefaultAzureCredential>(credential, replace);
            mocker.AddType<TokenCredential>(credential, replace);
            return mocker;
        }

        /// <summary>
        /// Registers a fixed-token <see cref="DefaultAzureCredential" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="token">The bearer token to return for every request.</param>
        /// <param name="expiresOn">The token expiry to return. When omitted, the token expires one hour from construction.</param>
        /// <param name="replace">True to replace existing registrations.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddDefaultAzureCredential(this Mocker mocker, string token, DateTimeOffset? expiresOn = null, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            return mocker.AddDefaultAzureCredential(new TestDefaultAzureCredential(token, expiresOn), replace);
        }

    }
}
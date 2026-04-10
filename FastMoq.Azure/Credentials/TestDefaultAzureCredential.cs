using Azure.Core;
using Azure.Identity;

namespace FastMoq.Azure.Credentials
{
    /// <summary>
    /// Represents a concrete <see cref="DefaultAzureCredential" /> that returns a fixed access token for tests.
    /// </summary>
    public sealed class TestDefaultAzureCredential : DefaultAzureCredential
    {
        private readonly AccessToken _accessToken;

        /// <summary>
        /// Initializes a new <see cref="TestDefaultAzureCredential" /> instance.
        /// </summary>
        /// <param name="token">The bearer token to return for every request.</param>
        /// <param name="expiresOn">The token expiry to return. When omitted, the token expires one hour from construction.</param>
        public TestDefaultAzureCredential(string token, DateTimeOffset? expiresOn = null)
            : base(false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            _accessToken = new AccessToken(token, expiresOn ?? DateTimeOffset.UtcNow.AddHours(1));
        }

        /// <inheritdoc />
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(requestContext);

            return _accessToken;
        }

        /// <inheritdoc />
        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(requestContext);

            return ValueTask.FromResult(_accessToken);
        }
    }
}
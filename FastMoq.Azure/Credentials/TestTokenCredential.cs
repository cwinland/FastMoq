using Azure.Core;

namespace FastMoq.Azure.Credentials
{
    /// <summary>
    /// Represents a token credential that returns a fixed access token for every request.
    /// </summary>
    public sealed class TestTokenCredential : TokenCredential
    {
        private readonly AccessToken _accessToken;

        /// <summary>
        /// Initializes a new <see cref="TestTokenCredential" /> instance.
        /// </summary>
        /// <param name="token">The bearer token to return for every request.</param>
        /// <param name="expiresOn">The token expiry to return. When omitted, the token expires one hour from construction.</param>
        public TestTokenCredential(string token, DateTimeOffset? expiresOn = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            _accessToken = new AccessToken(token, expiresOn ?? DateTimeOffset.UtcNow.AddHours(1));
        }

        /// <inheritdoc />
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return _accessToken;
        }

        /// <inheritdoc />
        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(_accessToken);
        }
    }
}
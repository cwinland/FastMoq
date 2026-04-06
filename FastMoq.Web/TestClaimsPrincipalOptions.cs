using System.Security.Claims;

namespace FastMoq.Web
{
    /// <summary>
    /// Configures the synthetic <see cref="ClaimsPrincipal"/> created by FastMoq web helpers.
    /// </summary>
    public sealed class TestClaimsPrincipalOptions
    {
        /// <summary>
        /// Gets or sets the authentication type used by the generated identity.
        /// </summary>
        public string AuthenticationType { get; set; } = "TestAuth";

        /// <summary>
        /// Gets or sets the value used for <see cref="ClaimTypes.Name"/>.
        /// </summary>
        public string? Name { get; set; } = "Test User";

        /// <summary>
        /// Gets or sets the value used for display-name style claims.
        /// </summary>
        public string? DisplayName { get; set; } = "Test User";

        /// <summary>
        /// Gets or sets the value used for email-style claims.
        /// </summary>
        public string? Email { get; set; } = "test.user@microsoft.com";

        /// <summary>
        /// Gets or sets the value used for preferred username and UPN-style claims.
        /// </summary>
        public string? PreferredUserName { get; set; } = "test.user@microsoft.com";

        /// <summary>
        /// Gets or sets the value used for object identifier style claims.
        /// </summary>
        public string? ObjectId { get; set; } = "11111111-1111-1111-1111-111111111111";

        /// <summary>
        /// Gets or sets the value used for tenant identifier style claims.
        /// </summary>
        public string? TenantId { get; set; } = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

        /// <summary>
        /// Gets or sets a value indicating whether the built-in compatibility claims should be added.
        /// </summary>
        public bool IncludeDefaultIdentityClaims { get; set; } = true;

        /// <summary>
        /// Gets the additional claims appended after the built-in compatibility claims.
        /// </summary>
        public IList<Claim> AdditionalClaims { get; } = [];
    }
}
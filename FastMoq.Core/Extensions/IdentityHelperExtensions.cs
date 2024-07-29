using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Security.Claims;

namespace FastMoq.Extensions
{
    /// <summary>
    /// Class IdentityHelper.
    /// </summary>
    public static class IdentityHelperExtensions
    {
        /// <summary>
        /// Sets the user.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="principal">The principal.</param>
        public static void SetUser(this HttpContext context, ClaimsPrincipal principal)
        {
            context.User = principal;
        }

        /// <summary>
        /// Sets the user.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="identity">The principal.</param>
        public static void SetUser(this HttpContext context, ClaimsIdentity identity)
        {
            context.User = new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Creates a claim.
        /// </summary>
        /// <param name="type">The type of claim. Must be from <see cref="ClaimTypes"/>.</param>
        /// <param name="value">Value of the claim.</param>
        /// <param name="properties">Claim Properties.</param>
        /// <param name="allowCustomType">Indicates if type is validated. If custom type is allowed, then the type string is not validated.</param>
        public static Claim CreateClaim(string type, string value, Dictionary<string, string>? properties = null, bool allowCustomType = false)
        {
            if (!allowCustomType && !IsValidClaimType(type))
            {
                throw new ArgumentException("Invalid claim type", nameof(type));
            }

            var claim = new Claim(type, value);

            foreach (var property in properties ?? new())
            {
                claim.Properties.Add(property.Key, property.Value);
            }

            return claim;
        }

        /// <summary>
        /// Creates the principal.
        /// </summary>
        /// <param name="claims">The claims.</param>
        /// <param name="authenticationType">Type of the authentication.</param>
        /// <returns>ClaimsPrincipal.</returns>
        public static ClaimsPrincipal CreatePrincipal(IEnumerable<Claim> claims, string? authenticationType = null) =>
            new(new ClaimsIdentity(claims, authenticationType));

        /// <summary>
        /// Determines whether given type is included as a <see cref="ClaimTypes" /> constant. Custom types may be valid, but this method will return false.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if type in <see cref="ClaimTypes" />; otherwise, <c>false</c>.</returns>
        public static bool IsValidClaimType(string type)
        {
            var claimTypes = typeof(ClaimTypes).GetFields(BindingFlags.Public | BindingFlags.Static)
                                               .Where(f => f.FieldType == typeof(string))
                                               .Select(f => (string?) f.GetValue(null))
                                               .Where(f => f is not null);

            return claimTypes.Contains(type);
        }
    }
}
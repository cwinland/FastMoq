using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;

namespace FastMoq.Web.Extensions
{
    /// <summary>
    ///     Class TestWebExtensions.
    /// </summary>
    public static class TestWebExtensions
    {
        private const string PreferredUserNameClaimType = "preferred_username";
        private const string DisplayNameClaimType = "name";
        private const string ObjectIdentifierClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string TenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";

        /// <summary>
        /// Creates an authenticated <see cref="ClaimsPrincipal"/> with compatibility-focused default claims.
        /// </summary>
        public static ClaimsPrincipal SetupClaimsPrincipal(this Mocker _, params string[] roleNames)
        {
            return _.SetupClaimsPrincipal(new TestClaimsPrincipalOptions(), roleNames);
        }

        /// <summary>
        /// Creates an authenticated <see cref="ClaimsPrincipal"/> by combining custom claims with compatibility defaults.
        /// Existing custom claim types are preserved.
        /// </summary>
        public static ClaimsPrincipal SetupClaimsPrincipal(this Mocker _, IEnumerable<Claim> claims)
        {
            return _.SetupClaimsPrincipal(claims, new TestClaimsPrincipalOptions());
        }

        /// <summary>
        /// Creates an authenticated <see cref="ClaimsPrincipal"/> by combining custom claims with compatibility defaults.
        /// Existing custom claim types are preserved.
        /// </summary>
        public static ClaimsPrincipal SetupClaimsPrincipal(this Mocker _, IEnumerable<Claim> claims, TestClaimsPrincipalOptions principalOptions)
        {
            ArgumentNullException.ThrowIfNull(_);
            ArgumentNullException.ThrowIfNull(claims);
            ArgumentNullException.ThrowIfNull(principalOptions);

            var mergedClaims = claims.ToList();
            if (principalOptions.IncludeDefaultIdentityClaims)
            {
                AddClaimIfMissing(mergedClaims, ClaimTypes.Name, principalOptions.Name);
                AddClaimIfMissing(mergedClaims, DisplayNameClaimType, principalOptions.DisplayName ?? principalOptions.Name);
                AddClaimIfMissing(mergedClaims, ClaimTypes.Email, principalOptions.Email);
                AddClaimIfMissing(mergedClaims, PreferredUserNameClaimType, principalOptions.PreferredUserName ?? principalOptions.Email);
                AddClaimIfMissing(mergedClaims, ClaimTypes.Upn, principalOptions.PreferredUserName ?? principalOptions.Email);
                AddClaimIfMissing(mergedClaims, ClaimTypes.NameIdentifier, principalOptions.ObjectId ?? principalOptions.Email);
                AddClaimIfMissing(mergedClaims, ObjectIdentifierClaimType, principalOptions.ObjectId);
                AddClaimIfMissing(mergedClaims, TenantIdClaimType, principalOptions.TenantId);
            }

            foreach (var claim in principalOptions.AdditionalClaims)
            {
                mergedClaims.Add(claim);
            }

            var identity = new ClaimsIdentity(mergedClaims, principalOptions.AuthenticationType, ClaimTypes.Name, ClaimTypes.Role);
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Creates an authenticated <see cref="ClaimsPrincipal"/> using the provided identity options and roles.
        /// </summary>
        public static ClaimsPrincipal SetupClaimsPrincipal(this Mocker _, TestClaimsPrincipalOptions principalOptions, params string[] roleNames)
        {
            ArgumentNullException.ThrowIfNull(_);
            ArgumentNullException.ThrowIfNull(principalOptions);

            var claims = roleNames
                .Select(roleName => new Claim(ClaimTypes.Role, roleName))
                .ToList();
            return _.SetupClaimsPrincipal(claims, principalOptions);
        }

        /// <summary>
        /// Creates a <see cref="ControllerContext"/> with an authenticated test user.
        /// </summary>
        public static ControllerContext CreateControllerContext(this Mocker mocker, params string[] roleNames)
        {
            return mocker.CreateControllerContext(new TestClaimsPrincipalOptions(), roleNames);
        }

        /// <summary>
        /// Creates a <see cref="ControllerContext"/> with a principal composed from the supplied claims and compatibility defaults.
        /// </summary>
        public static ControllerContext CreateControllerContext(this Mocker mocker, IEnumerable<Claim> claims)
        {
            return mocker.CreateControllerContext(mocker.SetupClaimsPrincipal(claims));
        }

        /// <summary>
        /// Creates a <see cref="ControllerContext"/> with a principal composed from the supplied claims and compatibility defaults.
        /// </summary>
        public static ControllerContext CreateControllerContext(this Mocker mocker, IEnumerable<Claim> claims, TestClaimsPrincipalOptions principalOptions)
        {
            return mocker.CreateControllerContext(mocker.SetupClaimsPrincipal(claims, principalOptions));
        }

        /// <summary>
        /// Creates a <see cref="ControllerContext"/> from an existing principal.
        /// </summary>
        public static ControllerContext CreateControllerContext(this Mocker mocker, ClaimsPrincipal principal)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(principal);

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal,
                },
            };
        }

        /// <summary>
        /// Creates a <see cref="ControllerContext"/> with an authenticated test user using the provided identity options.
        /// </summary>
        public static ControllerContext CreateControllerContext(this Mocker mocker, TestClaimsPrincipalOptions principalOptions, params string[] roleNames)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = mocker.SetupClaimsPrincipal(principalOptions, roleNames),
                },
            };
        }

        /// <summary>
        /// Creates a <see cref="ControllerContext"/> from an existing <see cref="HttpContext"/>.
        /// </summary>
        public static ControllerContext CreateControllerContext(this Mocker mocker, HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(httpContext);

            return new ControllerContext
            {
                HttpContext = httpContext,
            };
        }

        /// <summary>
        /// Creates an <see cref="HttpContext"/> with an authenticated test user.
        /// </summary>
        public static HttpContext CreateHttpContext(this Mocker mocker, params string[] roleNames)
        {
            return mocker.CreateHttpContext(new TestClaimsPrincipalOptions(), roleNames);
        }

        /// <summary>
        /// Creates an <see cref="HttpContext"/> with a principal composed from the supplied claims and compatibility defaults.
        /// </summary>
        public static HttpContext CreateHttpContext(this Mocker mocker, IEnumerable<Claim> claims)
        {
            return mocker.CreateHttpContext(mocker.SetupClaimsPrincipal(claims));
        }

        /// <summary>
        /// Creates an <see cref="HttpContext"/> with a principal composed from the supplied claims and compatibility defaults.
        /// </summary>
        public static HttpContext CreateHttpContext(this Mocker mocker, IEnumerable<Claim> claims, TestClaimsPrincipalOptions principalOptions)
        {
            return mocker.CreateHttpContext(mocker.SetupClaimsPrincipal(claims, principalOptions));
        }

        /// <summary>
        /// Creates an <see cref="HttpContext"/> from an existing principal.
        /// </summary>
        public static HttpContext CreateHttpContext(this Mocker mocker, ClaimsPrincipal principal)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(principal);

            return new DefaultHttpContext
            {
                User = principal,
            };
        }

        /// <summary>
        /// Creates an <see cref="HttpContext"/> with an authenticated test user using the provided identity options.
        /// </summary>
        public static HttpContext CreateHttpContext(this Mocker mocker, TestClaimsPrincipalOptions principalOptions, params string[] roleNames)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            return new DefaultHttpContext
            {
                User = mocker.SetupClaimsPrincipal(principalOptions, roleNames),
            };
        }

        /// <summary>
        /// Registers an <see cref="HttpContext"/> for controller, middleware, or accessor-based tests.
        /// </summary>
        public static Mocker AddHttpContext(this Mocker mocker, HttpContext? httpContext = null, bool replace = false, params string[] roleNames)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            return mocker.AddType(_ => httpContext ?? mocker.CreateHttpContext(roleNames), replace);
        }

        /// <summary>
        /// Registers an <see cref="IHttpContextAccessor"/> backed by the provided or generated <see cref="HttpContext"/>.
        /// </summary>
        public static Mocker AddHttpContextAccessor(this Mocker mocker, HttpContext? httpContext = null, bool replace = false, params string[] roleNames)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            var resolvedHttpContext = httpContext ?? mocker.CreateHttpContext(roleNames);
            var accessor = new HttpContextAccessor
            {
                HttpContext = resolvedHttpContext,
            };

            mocker.AddType<HttpContext>(_ => resolvedHttpContext, replace);
            mocker.AddType<HttpContextAccessor>(_ => accessor, replace);
            return mocker.AddType<IHttpContextAccessor, HttpContextAccessor>(_ => accessor, replace);
        }

        /// <summary>
        /// Registers a <see cref="RequestDelegate"/> for middleware tests.
        /// </summary>
        public static Mocker AddRequestDelegate(this Mocker mocker, RequestDelegate? next = null, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            return mocker.AddType(next ?? (_ => Task.CompletedTask), replace);
        }

        /// <summary>
        /// Sets or replaces a request header value on an <see cref="HttpContext"/>.
        /// </summary>
        public static HttpContext SetRequestHeader(this HttpContext httpContext, string name, params string[] values)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            httpContext.Request.Headers[name] = new StringValues(values);
            return httpContext;
        }

        /// <summary>
        /// Sets multiple request header values on an <see cref="HttpContext"/>.
        /// </summary>
        public static HttpContext SetRequestHeaders(this HttpContext httpContext, IEnumerable<KeyValuePair<string, string[]>> headers)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            ArgumentNullException.ThrowIfNull(headers);

            foreach (var header in headers)
            {
                httpContext.SetRequestHeader(header.Key, header.Value);
            }

            return httpContext;
        }

        /// <summary>
        /// Sets the raw query string on an <see cref="HttpContext"/>.
        /// </summary>
        public static HttpContext SetQueryString(this HttpContext httpContext, string? queryString)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            if (string.IsNullOrEmpty(queryString))
            {
                httpContext.Request.QueryString = QueryString.Empty;
                return httpContext;
            }

            httpContext.Request.QueryString = queryString.StartsWith("?", StringComparison.Ordinal)
                ? new QueryString(queryString)
                : new QueryString($"?{queryString}");

            return httpContext;
        }

        /// <summary>
        /// Sets the request query collection and synchronized raw query string on an <see cref="HttpContext"/>.
        /// </summary>
        public static HttpContext SetQueryParameters(this HttpContext httpContext, IEnumerable<KeyValuePair<string, string?>> parameters)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            ArgumentNullException.ThrowIfNull(parameters);

            var items = parameters
                .Select(parameter => new KeyValuePair<string, StringValues>(parameter.Key, new StringValues(parameter.Value)))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

            httpContext.Request.Query = new QueryCollection(items);
            httpContext.Request.QueryString = QueryString.Create(
                items.SelectMany(pair => pair.Value, (pair, value) => new KeyValuePair<string, string?>(pair.Key, value)));

            return httpContext;
        }

        /// <summary>
        /// Sets or replaces a single query parameter on an <see cref="HttpContext"/>.
        /// </summary>
        public static HttpContext SetQueryParameter(this HttpContext httpContext, string name, string? value)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var current = httpContext.Request.Query.ToDictionary(
                pair => pair.Key,
                pair => (string?) pair.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);
            current[name] = value;
            return httpContext.SetQueryParameters(current);
        }

        /// <summary>
        ///     Gets the content of the object result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result">The result.</param>
        /// <returns>T.</returns>
        public static T GetObjectResultContent<T>(this ActionResult<T> result)
        {
            if (result.Result is ObjectResult { Value: T value })
            {
                return value;
            }

            return result.Value!;
        }

        /// <summary>
        /// Gets the content of an <see cref="IActionResult"/> as a typed object result payload.
        /// </summary>
        public static T GetObjectResultContent<T>(this IActionResult result)
        {
            var objectResult = result.GetObjectResult();
            if (objectResult.Value is T value)
            {
                return value;
            }

            throw new InvalidOperationException($"Expected {typeof(T).Name} content but received {objectResult.Value?.GetType().Name ?? "null"}.");
        }

        /// <summary>
        /// Gets an <see cref="ObjectResult"/> from an <see cref="IActionResult"/>.
        /// </summary>
        public static ObjectResult GetObjectResult(this IActionResult result)
        {
            return result as ObjectResult
                ?? throw new InvalidOperationException($"Expected {nameof(ObjectResult)} but received {result.GetType().Name}.");
        }

        /// <summary>
        /// Gets an <see cref="OkObjectResult"/> from an <see cref="IActionResult"/>.
        /// </summary>
        public static OkObjectResult GetOkObjectResult(this IActionResult result)
        {
            return result as OkObjectResult
                ?? throw new InvalidOperationException($"Expected {nameof(OkObjectResult)} but received {result.GetType().Name}.");
        }

        /// <summary>
        /// Gets a <see cref="BadRequestObjectResult"/> from an <see cref="IActionResult"/>.
        /// </summary>
        public static BadRequestObjectResult GetBadRequestObjectResult(this IActionResult result)
        {
            return result as BadRequestObjectResult
                ?? throw new InvalidOperationException($"Expected {nameof(BadRequestObjectResult)} but received {result.GetType().Name}.");
        }

        /// <summary>
        /// Gets a <see cref="ConflictObjectResult"/> from an <see cref="IActionResult"/>.
        /// </summary>
        public static ConflictObjectResult GetConflictObjectResult(this IActionResult result)
        {
            return result as ConflictObjectResult
                ?? throw new InvalidOperationException($"Expected {nameof(ConflictObjectResult)} but received {result.GetType().Name}.");
        }

        /// <summary>
        /// Gets a <see cref="NoContentResult"/> from an <see cref="IActionResult"/>.
        /// </summary>
        public static NoContentResult GetNoContentResult(this IActionResult result)
        {
            return result as NoContentResult
                ?? throw new InvalidOperationException($"Expected {nameof(NoContentResult)} but received {result.GetType().Name}.");
        }

        private static void AddClaimIfValue(ICollection<Claim> claims, string claimType, string? value)
        {
            ArgumentNullException.ThrowIfNull(claims);
            ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(claimType, value));
            }
        }

        private static void AddClaimIfMissing(ICollection<Claim> claims, string claimType, string? value)
        {
            ArgumentNullException.ThrowIfNull(claims);
            ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

            if (claims.Any(claim => string.Equals(claim.Type, claimType, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            AddClaimIfValue(claims, claimType, value);
        }
    }
}
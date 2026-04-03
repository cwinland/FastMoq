using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace FastMoq.Web.Extensions
{
    /// <summary>
    ///     Class TestWebExtensions.
    /// </summary>
    public static class TestWebExtensions
    {
        /// <summary>
        /// Creates an authenticated <see cref="ClaimsPrincipal"/> with role claims and a default name claim.
        /// </summary>
        public static ClaimsPrincipal SetupClaimsPrincipal(this Mocker _, params string[] roleNames)
        {
            var claims = roleNames
                .Select(roleName => new Claim(ClaimTypes.Role, roleName))
                .Append(new Claim(ClaimTypes.Name, "Test User"))
                .ToList();

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Creates a <see cref="ControllerContext"/> with an authenticated test user.
        /// </summary>
        public static ControllerContext CreateControllerContext(this Mocker mocker, params string[] roleNames)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = mocker.SetupClaimsPrincipal(roleNames),
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
            ArgumentNullException.ThrowIfNull(mocker);

            return new DefaultHttpContext
            {
                User = mocker.SetupClaimsPrincipal(roleNames),
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
                pair => (string?)pair.Value.ToString(),
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
    }
}
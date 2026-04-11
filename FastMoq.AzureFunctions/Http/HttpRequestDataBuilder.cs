using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Collections.Specialized;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace FastMoq.AzureFunctions.Http
{
    /// <summary>
    /// Builds concrete <see cref="HttpRequestData" /> instances for Azure Functions HTTP-trigger tests.
    /// </summary>
    public sealed class HttpRequestDataBuilder
    {
        private readonly FunctionContext _functionContext;
        private readonly List<IHttpCookie> _cookies = [];
        private readonly Dictionary<string, List<string>> _headers = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ClaimsIdentity> _identities = [];
        private readonly NameValueCollection _queryParameters = new();
        private Stream _body = new MemoryStream();
        private string _method = "POST";
        private Uri _url = new("https://localhost/");

        /// <summary>
        /// Initializes a new <see cref="HttpRequestDataBuilder" /> instance.
        /// </summary>
        /// <param name="functionContext">The function context to associate with the request.</param>
        public HttpRequestDataBuilder(FunctionContext functionContext)
        {
            ArgumentNullException.ThrowIfNull(functionContext);

            _functionContext = functionContext;
        }

        /// <summary>
        /// Sets the HTTP method.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <returns>The current <see cref="HttpRequestDataBuilder" /> instance.</returns>
        public HttpRequestDataBuilder WithMethod(string method)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(method);

            _method = method;
            return this;
        }

        /// <summary>
        /// Sets the request URL.
        /// </summary>
        /// <param name="url">The absolute request URL.</param>
        /// <returns>The current <see cref="HttpRequestDataBuilder" /> instance.</returns>
        public HttpRequestDataBuilder WithUrl(string url)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(url);

            _url = new Uri(url, UriKind.Absolute);
            return this;
        }

        /// <summary>
        /// Sets the request URL.
        /// </summary>
        /// <param name="url">The absolute request URL.</param>
        /// <returns>The current <see cref="HttpRequestDataBuilder" /> instance.</returns>
        public HttpRequestDataBuilder WithUrl(Uri url)
        {
            ArgumentNullException.ThrowIfNull(url);

            _url = url;
            return this;
        }

        /// <summary>
        /// Adds a request header.
        /// </summary>
        /// <param name="name">The header name.</param>
        /// <param name="value">The header value.</param>
        /// <returns>The current <see cref="HttpRequestDataBuilder" /> instance.</returns>
        public HttpRequestDataBuilder WithHeader(string name, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(value);

            AddHeader(name, value);
            return this;
        }

        /// <summary>
        /// Adds a query-string parameter.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <returns>The current <see cref="HttpRequestDataBuilder" /> instance.</returns>
        public HttpRequestDataBuilder WithQueryParameter(string name, string? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _queryParameters.Add(name, value ?? string.Empty);
            return this;
        }

        /// <summary>
        /// Sets the request body from an existing stream.
        /// </summary>
        /// <param name="body">The body stream.</param>
        /// <param name="contentType">An optional content-type header value.</param>
        /// <returns>The current <see cref="HttpRequestDataBuilder" /> instance.</returns>
        public HttpRequestDataBuilder WithBody(Stream body, string? contentType = null)
        {
            ArgumentNullException.ThrowIfNull(body);

            _body = body;
            ResetStreamPosition(_body);

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                SetHeader("Content-Type", contentType);
            }

            return this;
        }

        /// <summary>
        /// Sets the request body from a string payload.
        /// </summary>
        /// <param name="body">The body payload.</param>
        /// <param name="encoding">The text encoding. Defaults to UTF-8.</param>
        /// <param name="contentType">An optional content-type header value.</param>
        /// <returns>The current <see cref="HttpRequestDataBuilder" /> instance.</returns>
        public HttpRequestDataBuilder WithBody(string body, Encoding? encoding = null, string? contentType = null)
        {
            ArgumentNullException.ThrowIfNull(body);

            var resolvedEncoding = encoding ?? Encoding.UTF8;
            _body = new MemoryStream(resolvedEncoding.GetBytes(body));

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                SetHeader("Content-Type", contentType);
            }

            return this;
        }

        /// <summary>
        /// Sets the request body from a JSON payload.
        /// </summary>
        /// <typeparam name="TValue">The payload type.</typeparam>
        /// <param name="value">The payload to serialize.</param>
        /// <param name="jsonSerializerOptions">Optional serializer options.</param>
        /// <returns>The current <see cref="HttpRequestDataBuilder" /> instance.</returns>
        public HttpRequestDataBuilder WithJsonBody<TValue>(TValue value, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            _body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(value, jsonSerializerOptions));
            SetHeader("Content-Type", "application/json; charset=utf-8");
            return this;
        }

        /// <summary>
        /// Adds a request cookie.
        /// </summary>
        /// <param name="name">The cookie name.</param>
        /// <param name="value">The cookie value.</param>
        /// <param name="path">An optional cookie path.</param>
        /// <param name="domain">An optional cookie domain.</param>
        /// <returns>The current <see cref="HttpRequestDataBuilder" /> instance.</returns>
        public HttpRequestDataBuilder WithCookie(string name, string value, string? path = null, string? domain = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(value);

            _cookies.Add(new HttpResponseDataBuilder.TestHttpCookie(name, value, domain: domain, path: path));
            return this;
        }

        /// <summary>
        /// Adds a claims identity to the request.
        /// </summary>
        /// <param name="identity">The identity to add.</param>
        /// <returns>The current <see cref="HttpRequestDataBuilder" /> instance.</returns>
        public HttpRequestDataBuilder WithIdentity(ClaimsIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            _identities.Add(identity);
            return this;
        }

        /// <summary>
        /// Adds all identities from the supplied principal to the request.
        /// </summary>
        /// <param name="principal">The principal whose identities should be added.</param>
        /// <returns>The current <see cref="HttpRequestDataBuilder" /> instance.</returns>
        public HttpRequestDataBuilder WithClaimsPrincipal(ClaimsPrincipal principal)
        {
            ArgumentNullException.ThrowIfNull(principal);

            foreach (var identity in principal.Identities)
            {
                _identities.Add(identity);
            }

            return this;
        }

        /// <summary>
        /// Builds the configured <see cref="HttpRequestData" /> instance.
        /// </summary>
        /// <returns>A concrete <see cref="HttpRequestData" /> suitable for tests.</returns>
        public HttpRequestData Build()
        {
            var query = ParseQueryParameters(_url);
            MergeQueryParameters(query, _queryParameters);

            var finalUrl = BuildUrl(_url, query);
            if (!_headers.ContainsKey("Host"))
            {
                SetHeader("Host", finalUrl.Authority);
            }

            ResetStreamPosition(_body);
            return new TestHttpRequestData(
                _functionContext,
                _body,
                CreateHeaders(),
                _cookies.AsReadOnly(),
                _identities.AsReadOnly(),
                finalUrl,
                _method,
                query);
        }

        private static Uri BuildUrl(Uri baseUrl, NameValueCollection query)
        {
            var builder = new UriBuilder(baseUrl)
            {
                Query = BuildQueryString(query),
            };

            return builder.Uri;
        }

        private static string BuildQueryString(NameValueCollection query)
        {
            var segments = new List<string>();
            foreach (var key in query.AllKeys)
            {
                if (key is null)
                {
                    continue;
                }

                var values = query.GetValues(key);
                if (values is null || values.Length == 0)
                {
                    segments.Add(Uri.EscapeDataString(key));
                    continue;
                }

                foreach (var value in values)
                {
                    segments.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value ?? string.Empty)}");
                }
            }

            return string.Join("&", segments);
        }

        private static void MergeQueryParameters(NameValueCollection target, NameValueCollection source)
        {
            foreach (var key in source.AllKeys)
            {
                if (key is null)
                {
                    continue;
                }

                var values = source.GetValues(key);
                if (values is null)
                {
                    continue;
                }

                foreach (var value in values)
                {
                    target.Add(key, value);
                }
            }
        }

        private static NameValueCollection ParseQueryParameters(Uri url)
        {
            var query = new NameValueCollection();
            var rawQuery = url.Query;
            if (string.IsNullOrWhiteSpace(rawQuery))
            {
                return query;
            }

            foreach (var segment in rawQuery.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var separatorIndex = segment.IndexOf('=');
                var encodedName = separatorIndex >= 0 ? segment[..separatorIndex] : segment;
                var encodedValue = separatorIndex >= 0 ? segment[(separatorIndex + 1)..] : string.Empty;
                query.Add(WebUtility.UrlDecode(encodedName), WebUtility.UrlDecode(encodedValue));
            }

            return query;
        }

        private static void ResetStreamPosition(Stream stream)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
        }

        private void AddHeader(string name, string value)
        {
            if (!_headers.TryGetValue(name, out var values))
            {
                values = [];
                _headers[name] = values;
            }

            values.Add(value);
        }

        private HttpHeadersCollection CreateHeaders()
        {
            var headers = new HttpHeadersCollection();
            foreach (var pair in _headers)
            {
                foreach (var value in pair.Value)
                {
                    headers.Add(pair.Key, value);
                }
            }

            return headers;
        }

        private void SetHeader(string name, string value)
        {
            _headers[name] = [value];
        }

        internal sealed class TestHttpRequestData : HttpRequestData
        {
            public TestHttpRequestData(
                FunctionContext functionContext,
                Stream body,
                HttpHeadersCollection headers,
                IReadOnlyCollection<IHttpCookie> cookies,
                IEnumerable<ClaimsIdentity> identities,
                Uri url,
                string method,
                NameValueCollection query)
                : base(functionContext)
            {
                Body = body;
                Headers = headers;
                Cookies = cookies;
                Identities = identities;
                Url = url;
                Method = method;
                Query = query;
            }

            public override Stream Body { get; }

            public override IReadOnlyCollection<IHttpCookie> Cookies { get; }

            public override HttpHeadersCollection Headers { get; }

            public override IEnumerable<ClaimsIdentity> Identities { get; }

            public override string Method { get; }

            public override NameValueCollection Query { get; }

            public override Uri Url { get; }

            public override HttpResponseData CreateResponse()
            {
                return new HttpResponseDataBuilder(FunctionContext).Build();
            }
        }
    }
}
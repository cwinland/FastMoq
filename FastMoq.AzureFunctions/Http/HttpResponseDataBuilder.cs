using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;

namespace FastMoq.AzureFunctions.Http
{
    /// <summary>
    /// Builds concrete <see cref="HttpResponseData" /> instances for Azure Functions HTTP-trigger tests.
    /// </summary>
    public sealed class HttpResponseDataBuilder
    {
        private readonly List<IHttpCookie> _cookies = [];
        private readonly FunctionContext _functionContext;
        private readonly Dictionary<string, List<string>> _headers = new(StringComparer.OrdinalIgnoreCase);
        private Stream _body = new MemoryStream();
        private HttpStatusCode _statusCode = HttpStatusCode.OK;

        /// <summary>
        /// Initializes a new <see cref="HttpResponseDataBuilder" /> instance.
        /// </summary>
        /// <param name="functionContext">The function context to associate with the response.</param>
        public HttpResponseDataBuilder(FunctionContext functionContext)
        {
            ArgumentNullException.ThrowIfNull(functionContext);

            _functionContext = functionContext;
        }

        /// <summary>
        /// Sets the response status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <returns>The current <see cref="HttpResponseDataBuilder" /> instance.</returns>
        public HttpResponseDataBuilder WithStatusCode(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
            return this;
        }

        /// <summary>
        /// Adds a response header.
        /// </summary>
        /// <param name="name">The header name.</param>
        /// <param name="value">The header value.</param>
        /// <returns>The current <see cref="HttpResponseDataBuilder" /> instance.</returns>
        public HttpResponseDataBuilder WithHeader(string name, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(value);

            AddHeader(name, value);
            return this;
        }

        /// <summary>
        /// Sets the response body from an existing stream.
        /// </summary>
        /// <param name="body">The body stream.</param>
        /// <param name="contentType">An optional content-type header value.</param>
        /// <returns>The current <see cref="HttpResponseDataBuilder" /> instance.</returns>
        public HttpResponseDataBuilder WithBody(Stream body, string? contentType = null)
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
        /// Sets the response body from a string payload.
        /// </summary>
        /// <param name="body">The body payload.</param>
        /// <param name="encoding">The text encoding. Defaults to UTF-8.</param>
        /// <param name="contentType">An optional content-type header value.</param>
        /// <returns>The current <see cref="HttpResponseDataBuilder" /> instance.</returns>
        public HttpResponseDataBuilder WithBody(string body, Encoding? encoding = null, string? contentType = null)
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
        /// Sets the response body from a JSON payload.
        /// </summary>
        /// <typeparam name="TValue">The payload type.</typeparam>
        /// <param name="value">The payload to serialize.</param>
        /// <param name="jsonSerializerOptions">Optional serializer options.</param>
        /// <returns>The current <see cref="HttpResponseDataBuilder" /> instance.</returns>
        public HttpResponseDataBuilder WithJsonBody<TValue>(TValue value, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            _body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(value, jsonSerializerOptions));
            SetHeader("Content-Type", "application/json; charset=utf-8");
            return this;
        }

        /// <summary>
        /// Adds a response cookie.
        /// </summary>
        /// <param name="name">The cookie name.</param>
        /// <param name="value">The cookie value.</param>
        /// <param name="path">An optional cookie path.</param>
        /// <param name="domain">An optional cookie domain.</param>
        /// <param name="httpOnly">An optional HttpOnly flag.</param>
        /// <param name="secure">An optional Secure flag.</param>
        /// <returns>The current <see cref="HttpResponseDataBuilder" /> instance.</returns>
        public HttpResponseDataBuilder WithCookie(string name, string value, string? path = null, string? domain = null, bool? httpOnly = null, bool? secure = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(value);

            _cookies.Add(new TestHttpCookie(name, value, domain: domain, httpOnly: httpOnly, path: path, secure: secure));
            return this;
        }

        /// <summary>
        /// Builds the configured <see cref="HttpResponseData" /> instance.
        /// </summary>
        /// <returns>A concrete <see cref="HttpResponseData" /> suitable for tests.</returns>
        public HttpResponseData Build()
        {
            ResetStreamPosition(_body);
            return new TestHttpResponseData(
                _functionContext,
                _statusCode,
                CreateHeaders(),
                _body,
                new TestHttpCookies(_cookies));
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

        internal sealed class TestHttpResponseData : HttpResponseData
        {
            public TestHttpResponseData(FunctionContext functionContext, HttpStatusCode statusCode, HttpHeadersCollection headers, Stream body, HttpCookies cookies)
                : base(functionContext)
            {
                StatusCode = statusCode;
                Headers = headers;
                Body = body;
                Cookies = cookies;
            }

            public override Stream Body { get; set; }

            public override HttpCookies Cookies { get; }

            public override HttpHeadersCollection Headers { get; set; }

            public override HttpStatusCode StatusCode { get; set; }
        }

        internal sealed class TestHttpCookies : HttpCookies
        {
            private readonly List<IHttpCookie> _cookies;

            public TestHttpCookies(IEnumerable<IHttpCookie>? cookies = null)
            {
                _cookies = cookies?.ToList() ?? [];
            }

            public override void Append(string name, string value)
            {
                _cookies.Add(new TestHttpCookie(name, value));
            }

            public override void Append(IHttpCookie cookie)
            {
                ArgumentNullException.ThrowIfNull(cookie);

                _cookies.Add(cookie);
            }

            public override IHttpCookie CreateNew()
            {
                return new TestHttpCookie(string.Empty, string.Empty);
            }
        }

        internal sealed class TestHttpCookie : IHttpCookie
        {
            public TestHttpCookie(
                string name,
                string value,
                string? domain = null,
                DateTimeOffset? expires = null,
                bool? httpOnly = null,
                double? maxAge = null,
                string? path = null,
                SameSite sameSite = default,
                bool? secure = null)
            {
                Name = name;
                Value = value;
                Domain = domain ?? string.Empty;
                Expires = expires;
                HttpOnly = httpOnly;
                MaxAge = maxAge;
                Path = path ?? string.Empty;
                SameSite = sameSite;
                Secure = secure;
            }

            public string Domain { get; }

            public DateTimeOffset? Expires { get; }

            public bool? HttpOnly { get; }

            public double? MaxAge { get; }

            public string Name { get; }

            public string Path { get; }

            public SameSite SameSite { get; }

            public bool? Secure { get; }

            public string Value { get; }
        }
    }
}
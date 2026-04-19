using System.Net;
using System.Text;

namespace FastMoq.Extensions
{
    /// <summary>
    ///     Mocker Http Extensions Class.
    /// </summary>
    public static class MockerHttpExtensions
    {
        /// <summary>
        ///     Creates an HTTP client backed by FastMoq-managed HTTP dependencies.
        /// </summary>
        /// <remarks>
        /// This entry point remains supported after v5.
        /// New tests should prefer the provider-neutral request helpers in this class for HTTP behavior.
        /// Advanced protected-member setups remain available from the Moq provider package for migration scenarios.
        /// When the current <see cref="Mocker" /> is still using the built-in HTTP compatibility registrations,
        /// repeated calls update that built-in handler and <see cref="IHttpClientFactory" /> path.
        /// If the test replaces <see cref="IHttpClientFactory" /> explicitly, it owns <c>CreateClient(...)</c> behavior.
        /// </remarks>
        public static HttpClient CreateHttpClient(this Mocker mocker, string clientName = "FastMoqHttpClient", string baseAddress = "http://localhost",
            HttpStatusCode statusCode = HttpStatusCode.OK, string stringContent = "[{'id':1}]")
        {
            return CreateHttpClientCore(mocker, clientName, baseAddress, statusCode, stringContent);
        }

        /// <summary>
        ///     Configures a provider-neutral HTTP response for requests that match the supplied predicate.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="requestPredicate">Predicate used to match an outgoing request.</param>
        /// <param name="responseFactory">Factory used to create the HTTP response for a matching request.</param>
        public static void WhenHttpRequest(this Mocker mocker, Func<HttpRequestMessage, bool> requestPredicate, Func<HttpResponseMessage> responseFactory)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(requestPredicate);
            ArgumentNullException.ThrowIfNull(responseFactory);

            GetOrCreateProviderNeutralHandler(mocker).AddRoute(requestPredicate, responseFactory);
        }

        /// <summary>
        ///     Configures a provider-neutral HTTP response for a specific method and request URI or path.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="method">The expected HTTP method.</param>
        /// <param name="requestUriOrPath">The absolute URI or relative path to match.</param>
        /// <param name="responseFactory">Factory used to create the HTTP response for a matching request.</param>
        public static void WhenHttpRequest(this Mocker mocker, HttpMethod method, string requestUriOrPath, Func<HttpResponseMessage> responseFactory)
        {
            ArgumentNullException.ThrowIfNull(method);
            ArgumentException.ThrowIfNullOrWhiteSpace(requestUriOrPath);
            ArgumentNullException.ThrowIfNull(responseFactory);

            var predicate = CreateRequestMatcher(method, requestUriOrPath);
            mocker.WhenHttpRequest(predicate, responseFactory);
        }

        /// <summary>
        ///     Configures a provider-neutral JSON response for a specific method and request URI or path.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="method">The expected HTTP method.</param>
        /// <param name="requestUriOrPath">The absolute URI or relative path to match.</param>
        /// <param name="json">The JSON payload returned to matching requests.</param>
        /// <param name="statusCode">The response status code.</param>
        public static void WhenHttpRequestJson(this Mocker mocker, HttpMethod method, string requestUriOrPath, string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(method);
            ArgumentException.ThrowIfNullOrWhiteSpace(requestUriOrPath);
            ArgumentNullException.ThrowIfNull(json);

            mocker.WhenHttpRequest(method, requestUriOrPath, () => CreateJsonResponse(statusCode, json));
        }

        internal static HttpClient CreateHttpClientCore(this Mocker mocker, string clientName = "FastMoqHttpClient", string baseAddress = "http://localhost",
            HttpStatusCode statusCode = HttpStatusCode.OK, string stringContent = "[{'id':1}]")
        {
            var baseUri = new Uri(baseAddress);

            EnsureProviderNeutralHttpDependencies(mocker, baseUri, statusCode, stringContent);
            return mocker.GetObject<IHttpClientFactory>()?.CreateClient(clientName)
                ?? throw new ApplicationException("Unable to create IHttpClientFactory.");
        }

        /// <summary>
        ///     Creates the HTTP client internal.
        /// </summary>
        /// <param name="mocker">Mocker object</param>
        /// <param name="baseUri">The base URI.</param>
        /// <returns>System.Net.Http.HttpClient.</returns>
        /// <exception cref="System.ApplicationException">Unable to create HttpMessageHandler.</exception>
        internal static HttpClient CreateHttpClientInternal(this Mocker mocker, Uri baseUri) =>
            new(GetPreferredHttpMessageHandler(mocker) ?? throw new ApplicationException("Unable to create HttpMessageHandler."))
            {
                BaseAddress = baseUri,
            };

        /// <summary>
        ///     Gets the content bytes.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>byte[].</returns>
        public static async Task<byte[]> GetContentBytesAsync(this HttpContent content) =>
            content is ByteArrayContent data ? await data.ReadAsByteArrayAsync() : [];

        /// <summary>
        ///     Gets the content stream.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>System.IO.Stream.</returns>
        public static async Task<Stream> GetContentStreamAsync(this HttpContent content) =>
            content is ByteArrayContent data ? await data.ReadAsStreamAsync() : Stream.Null;

        private static Func<HttpRequestMessage, bool> CreateRequestMatcher(HttpMethod method, string requestUriOrPath)
        {
            if (Uri.TryCreate(requestUriOrPath, UriKind.Absolute, out var absoluteUri))
            {
                return request => request.Method == method && request.RequestUri == absoluteUri;
            }

            var normalizedPath = NormalizeRequestPath(requestUriOrPath);
            return request => request.Method == method && NormalizeRequestPath(request.RequestUri?.PathAndQuery) == normalizedPath;
        }

        private static string NormalizeRequestPath(string? requestUriOrPath)
        {
            if (string.IsNullOrWhiteSpace(requestUriOrPath))
            {
                return "/";
            }

            return requestUriOrPath.StartsWith("/", StringComparison.Ordinal)
                ? requestUriOrPath
                : $"/{requestUriOrPath}";
        }

        private static HttpMessageHandler? GetPreferredHttpMessageHandler(Mocker mocker)
        {
            if (mocker.Contains<HttpMessageHandler>())
            {
                return mocker.GetMockModel(typeof(HttpMessageHandler)).Instance as HttpMessageHandler;
            }

            return mocker.GetObject<HttpMessageHandler>();
        }

        private static void EnsureProviderNeutralHttpDependencies(Mocker mocker, Uri baseUri, HttpStatusCode statusCode, string stringContent)
        {
            if (!mocker.Contains<HttpMessageHandler>())
            {
                if (mocker.typeMap.TryGetValue(typeof(HttpMessageHandler), out _) &&
                    mocker.GetObject<HttpMessageHandler>() is ConfigurableHttpMessageHandler existingHandler)
                {
                    existingHandler.UpdateDefault(() => CreateResponse(statusCode, stringContent));
                }
                else if (!mocker.typeMap.ContainsKey(typeof(HttpMessageHandler)))
                {
                    var handler = new ConfigurableHttpMessageHandler(() => CreateResponse(statusCode, stringContent));
                    mocker.AddType<HttpMessageHandler>(_ => handler, replace: true);
                }
            }

            if (!mocker.Contains<IHttpClientFactory>())
            {
                if (mocker.typeMap.TryGetValue(typeof(IHttpClientFactory), out _) &&
                    mocker.GetObject<IHttpClientFactory>() is ConfigurableHttpClientFactory existingFactory)
                {
                    existingFactory.UpdateClientFactory(() => mocker.CreateHttpClientInternal(baseUri));
                }
                else if (!mocker.typeMap.ContainsKey(typeof(IHttpClientFactory)))
                {
                    var factory = new ConfigurableHttpClientFactory(() => mocker.CreateHttpClientInternal(baseUri));
                    mocker.AddType<IHttpClientFactory>(factory, replace: true);
                }
            }
        }

        internal static void RemoveBuiltInHttpClientFactoryRegistration(Mocker mocker)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            if (mocker.typeMap.TryGetValue(typeof(IHttpClientFactory), out _) &&
                mocker.GetObject<IHttpClientFactory>() is ConfigurableHttpClientFactory)
            {
                mocker.typeMap.Remove(typeof(IHttpClientFactory));
            }
        }

        private static void SetProviderNeutralHandlerResponse(Mocker mocker, Func<HttpResponseMessage> messageFunc)
        {
            GetOrCreateProviderNeutralHandler(mocker).UpdateDefault(messageFunc);
        }

        private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string stringContent)
        {
            return new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(stringContent),
            };
        }

        private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json)
        {
            return new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }

        private static ConfigurableHttpMessageHandler GetOrCreateProviderNeutralHandler(Mocker mocker)
        {
            if (mocker.Contains<HttpMessageHandler>())
            {
                throw new NotSupportedException("Provider-neutral HTTP helpers cannot be used when a tracked HttpMessageHandler mock is already in use.");
            }

            if (mocker.typeMap.TryGetValue(typeof(HttpMessageHandler), out _)
                && mocker.GetObject<HttpMessageHandler>() is ConfigurableHttpMessageHandler existingHandler)
            {
                return existingHandler;
            }

            var handler = new ConfigurableHttpMessageHandler(() => CreateResponse(HttpStatusCode.OK, "[{'id':1}]"));
            mocker.AddType<HttpMessageHandler>(_ => handler, replace: true);
            return handler;
        }

        private sealed class ConfigurableHttpClientFactory : IHttpClientFactory
        {
            private Func<HttpClient> clientFactory;

            public ConfigurableHttpClientFactory(Func<HttpClient> clientFactory)
            {
                this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            }

            internal void UpdateClientFactory(Func<HttpClient> clientFactory)
            {
                this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            }

            public HttpClient CreateClient(string name) => clientFactory();
        }

        private sealed class ConfigurableHttpMessageHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
        {
            private readonly List<HttpRouteRegistration> _registrations = [];
            private Func<HttpResponseMessage> _responseFactory = responseFactory;

            internal void UpdateDefault(Func<HttpResponseMessage> responseFactory)
            {
                _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
            }

            internal void AddRoute(Func<HttpRequestMessage, bool> requestPredicate, Func<HttpResponseMessage> responseFactory)
            {
                ArgumentNullException.ThrowIfNull(requestPredicate);
                ArgumentNullException.ThrowIfNull(responseFactory);

                _registrations.Add(new HttpRouteRegistration(requestPredicate, responseFactory));
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                for (var i = _registrations.Count - 1; i >= 0; i--)
                {
                    var registration = _registrations[i];
                    if (registration.RequestPredicate(request))
                    {
                        return Task.FromResult(registration.ResponseFactory());
                    }
                }

                return Task.FromResult(_responseFactory());
            }
        }

        private sealed record HttpRouteRegistration(Func<HttpRequestMessage, bool> RequestPredicate, Func<HttpResponseMessage> ResponseFactory);
    }
}

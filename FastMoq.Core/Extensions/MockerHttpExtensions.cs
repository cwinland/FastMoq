using Moq;
using Moq.Protected;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using FastMoq.Providers;

namespace FastMoq.Extensions
{
    /// <summary>
    ///     Mocker Http Extensions Class.
    /// </summary>
    public static class MockerHttpExtensions
    {
        /// <summary>
        ///     Creates the HTTP client using a mock IHttpClientFactory.
        /// </summary>
        public static HttpClient CreateHttpClient(this Mocker mocker, string clientName = "FastMoqHttpClient", string baseAddress = "http://localhost",
            HttpStatusCode statusCode = HttpStatusCode.OK, string stringContent = "[{'id':1}]")
        {
            var setupHttpFactory = false;
            var baseUri = new Uri(baseAddress);

            if (!UsesLegacyProtectedHttpMocks())
            {
                EnsureProviderNeutralHttpDependencies(mocker, baseUri, statusCode, stringContent);
                return mocker.GetObject<IHttpClientFactory>()?.CreateClient(clientName)
                    ?? throw new ApplicationException("Unable to create IHttpClientFactory.");
            }

            // Ensure handler mock exists (idempotent due to CreateMock changes / GetMock usage)
            if (!mocker.Contains<HttpMessageHandler>())
            {
                mocker.GetMock<HttpMessageHandler>(); // create or fetch
                mocker.SetupHttpMessage(() => new HttpResponseMessage
                    {
                        StatusCode = statusCode,
                        Content = new StringContent(stringContent),
                    }
                );
            }

            if (!mocker.Contains<IHttpClientFactory>())
            {
                setupHttpFactory = true;
                mocker.GetMock<IHttpClientFactory>().Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => mocker.CreateHttpClientInternal(baseUri));
            }

            return setupHttpFactory
                ? mocker.GetObject<IHttpClientFactory>()?.CreateClient(clientName) ?? throw new ApplicationException("Unable to create IHttpClientFactory.")
                : mocker.CreateHttpClientInternal(baseUri);
        }

        /// <summary>
        ///     Creates the HTTP client internal.
        /// </summary>
        /// <param name="mocker">Mocker object</param>
        /// <param name="baseUri">The base URI.</param>
        /// <returns>System.Net.Http.HttpClient.</returns>
        /// <exception cref="System.ApplicationException">Unable to create HttpMessageHandler.</exception>
        internal static HttpClient CreateHttpClientInternal(this Mocker mocker, Uri baseUri) =>
            new(mocker.GetObject<HttpMessageHandler>() ?? throw new ApplicationException("Unable to create HttpMessageHandler."))
            {
                BaseAddress = baseUri,
            };

        /// <summary>
        ///     Setups the HTTP message.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="messageFunc">The message function.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void SetupHttpMessage(this Mocker mocker, Func<HttpResponseMessage> messageFunc, Expression? request = null, Expression? cancellationToken = null)
        {
            if (!UsesLegacyProtectedHttpMocks())
            {
                SetProviderNeutralHandlerResponse(mocker, messageFunc);
                return;
            }

            request ??= ItExpr.IsAny<HttpRequestMessage>();
            cancellationToken ??= ItExpr.IsAny<CancellationToken>();
            mocker.GetMock<HttpMessageHandler>(); // ensure exists
            mocker.SetupMessageProtectedAsync<HttpMessageHandler, HttpResponseMessage>("SendAsync", messageFunc, request, cancellationToken);
        }

        /// <summary>
        ///     Setups the message.
        /// </summary>
        /// <typeparam name="TMock">The type of the mock.</typeparam>
        /// <typeparam name="TReturn">The type of the return value.</typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="messageFunc">The message function.</param>
        public static void SetupMessage<TMock, TReturn>(this Mocker mocker, Expression<Func<TMock, TReturn>> expression, Func<TReturn> messageFunc)
            where TMock : class =>
            mocker.GetMock<TMock>()
                .Setup(expression)?
                .Returns(messageFunc)?.Verifiable();

        /// <summary>
        ///     Setups the message asynchronous.
        /// </summary>
        /// <typeparam name="TMock">The type of the mock.</typeparam>
        /// <typeparam name="TReturn">The type of the return value.</typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="messageFunc">The message function.</param>
        /// <exception cref="System.IO.InvalidDataException">Unable to setup '{typeof(TMock)}'.</exception>
        public static void SetupMessageAsync<TMock, TReturn>(this Mocker mocker, Expression<Func<TMock, Task<TReturn>>> expression, Func<TReturn> messageFunc)
            where TMock : class =>
            (mocker.GetMock<TMock>()
                 .Setup(expression) ??
             throw new InvalidDataException($"Unable to setup '{typeof(TMock)}'."))
            .ReturnsAsync(messageFunc)?.Verifiable();

        /// <summary>
        ///     Setups the message protected.
        /// </summary>
        /// <typeparam name="TMock">The type of the mock.</typeparam>
        /// <typeparam name="TReturn">The type of the return value.</typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="methodOrPropertyName">Name of the method or property.</param>
        /// <param name="messageFunc">The message function.</param>
        /// <param name="args">The arguments.</param>
        public static void SetupMessageProtected<TMock, TReturn>(this Mocker mocker, string methodOrPropertyName, Func<TReturn> messageFunc, params object?[]? args)
            where TMock : class =>
            mocker.GetMock<TMock>().Protected()
                ?.Setup<TReturn>(methodOrPropertyName, args ?? [])
                ?.Returns(messageFunc)?.Verifiable();

        /// <summary>
        ///     Setups the message protected asynchronous.
        /// </summary>
        /// <typeparam name="TMock">The type of the mock.</typeparam>
        /// <typeparam name="TReturn">The type of the return value.</typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="methodOrPropertyName">Name of the method or property.</param>
        /// <param name="messageFunc">The message function.</param>
        /// <param name="args">The arguments.</param>
        public static void SetupMessageProtectedAsync<TMock, TReturn>(this Mocker mocker, string methodOrPropertyName, Func<TReturn> messageFunc, params object?[]? args)
            where TMock : class =>
            mocker.GetMock<TMock>().Protected()
                ?.Setup<Task<TReturn>>(methodOrPropertyName, args ?? [])
                ?.ReturnsAsync(messageFunc)?.Verifiable();

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

        private static bool UsesLegacyProtectedHttpMocks()
        {
            return MockingProviderRegistry.Default.Capabilities.SupportsProtectedMembers;
        }

        private static void EnsureProviderNeutralHttpDependencies(Mocker mocker, Uri baseUri, HttpStatusCode statusCode, string stringContent)
        {
            if (!mocker.Contains<HttpMessageHandler>() && !mocker.typeMap.ContainsKey(typeof(HttpMessageHandler)))
            {
                var handler = new ConfigurableHttpMessageHandler(() => CreateResponse(statusCode, stringContent));
                mocker.AddType<HttpMessageHandler>(_ => handler, replace: true);
            }

            if (!mocker.Contains<IHttpClientFactory>() && !mocker.typeMap.ContainsKey(typeof(IHttpClientFactory)))
            {
                var factory = new ConfigurableHttpClientFactory(() => mocker.CreateHttpClientInternal(baseUri));
                mocker.AddType<IHttpClientFactory>(factory, replace: true);
            }
        }

        private static void SetProviderNeutralHandlerResponse(Mocker mocker, Func<HttpResponseMessage> messageFunc)
        {
            if (mocker.Contains<HttpMessageHandler>())
            {
                throw new NotSupportedException("SetupHttpMessage requires Moq protected-member support when a tracked HttpMessageHandler mock is already in use.");
            }

            if (mocker.typeMap.TryGetValue(typeof(HttpMessageHandler), out _)
                && mocker.GetObject<HttpMessageHandler>() is ConfigurableHttpMessageHandler existingHandler)
            {
                existingHandler.Update(messageFunc);
                return;
            }

            var handler = new ConfigurableHttpMessageHandler(messageFunc);
            mocker.AddType<HttpMessageHandler>(_ => handler, replace: true);
        }

        private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string stringContent)
        {
            return new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(stringContent),
            };
        }

        private sealed class ConfigurableHttpClientFactory(Func<HttpClient> clientFactory) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => clientFactory();
        }

        private sealed class ConfigurableHttpMessageHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
        {
            private Func<HttpResponseMessage> _responseFactory = responseFactory;

            internal void Update(Func<HttpResponseMessage> responseFactory)
            {
                _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_responseFactory());
            }
        }
    }
}

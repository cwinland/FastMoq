using Moq;
using Moq.Protected;
using System.Linq.Expressions;
using System.Net;

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
        /// <param name="mocker">The mocker.</param>
        /// <param name="clientName">Name of the client.</param>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="stringContent">Content of the string.</param>
        /// <returns>HttpClient object.</returns>
        /// <exception cref="System.ApplicationException">Unable to create IHttpClientFactory.</exception>
        public static HttpClient CreateHttpClient(this Mocker mocker, string clientName = "FastMoqHttpClient", string baseAddress = "http://localhost",
            HttpStatusCode statusCode = HttpStatusCode.OK, string stringContent = "[{'id':1}]")
        {
            var setupHttpFactory = false;

            var baseUri = new Uri(baseAddress);

            if (!mocker.Contains<HttpMessageHandler>())
            {
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
            request ??= ItExpr.IsAny<HttpRequestMessage>();
            cancellationToken ??= ItExpr.IsAny<CancellationToken>();

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
                ?.Setup<TReturn>(methodOrPropertyName, args ?? Array.Empty<object>())
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
                ?.Setup<Task<TReturn>>(methodOrPropertyName, args ?? Array.Empty<object>())
                ?.ReturnsAsync(messageFunc)?.Verifiable();

        /// <summary>
        ///     Gets the content bytes.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>byte[].</returns>
        public static async Task<byte[]> GetContentBytesAsync(this HttpContent content) =>
            content is ByteArrayContent data ? await data.ReadAsByteArrayAsync() : Array.Empty<byte>();

        /// <summary>
        ///     Gets the content stream.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>System.IO.Stream.</returns>
        public static async Task<Stream> GetContentStreamAsync (this HttpContent content) =>
            content is ByteArrayContent data ? await data.ReadAsStreamAsync() : Stream.Null;
    }
}

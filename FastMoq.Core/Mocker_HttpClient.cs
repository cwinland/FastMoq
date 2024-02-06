using Moq;
using System.Net;

namespace FastMoq
{
    /// <inheritdoc cref="Mocker" />
    public partial class Mocker
    {
        #region Fields

        /// <summary>
        ///     The setup HTTP factory
        /// </summary>
        private bool setupHttpFactory;

        #endregion

        #region Properties

        /// <summary>
        ///     The virtual mock http client that is used by mocker unless overridden with the <see cref="Strict" /> property.
        /// </summary>
        /// <value>The HTTP client.</value>
        public HttpClient HttpClient { get; }

        #endregion

        /// <summary>
        ///     Creates the HTTP client using a mock IHttpClientFactory.
        /// </summary>
        /// <param name="clientName">Name of the client.</param>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="stringContent">Content of the string.</param>
        /// <returns>HttpClient object.</returns>
        public HttpClient CreateHttpClient(string clientName = "FastMoqHttpClient", string baseAddress = "http://localhost",
            HttpStatusCode statusCode = HttpStatusCode.OK, string stringContent = "[{'id':1}]")
        {
            var baseUri = new Uri(baseAddress);

            if (!Contains<HttpMessageHandler>())
            {
                SetupHttpMessage(() => new HttpResponseMessage
                    {
                        StatusCode = statusCode,
                        Content = new StringContent(stringContent),
                    }
                );
            }

            if (!Contains<IHttpClientFactory>())
            {
                setupHttpFactory = true;
                GetMock<IHttpClientFactory>().Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => CreateHttpClientInternal(baseUri));
            }

            return setupHttpFactory
                ? GetObject<IHttpClientFactory>()?.CreateClient(clientName) ?? throw new ApplicationException("Unable to create IHttpClientFactory.")
                : CreateHttpClientInternal(baseUri);
        }

        /// <summary>
        ///     Creates the HTTP client internal.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <returns>System.Net.Http.HttpClient.</returns>
        internal HttpClient CreateHttpClientInternal(Uri baseUri) =>
            new(GetObject<HttpMessageHandler>() ?? throw new ApplicationException("Unable to create HttpMessageHandler."))
            {
                BaseAddress = baseUri,
            };
    }
}

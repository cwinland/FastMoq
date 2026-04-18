using FastMoq.Extensions;
using FastMoq.AzureFunctions.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text;
using System.Text.Json;

namespace FastMoq.AzureFunctions.Extensions
{
    /// <summary>
    /// Provides Azure Functions HTTP-trigger helpers for constructing request and response data in tests.
    /// </summary>
    public static class HttpTriggerTestExtensions
    {
        /// <summary>
        /// Creates a concrete <see cref="HttpRequestData" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="configureRequest">Optional request-builder configuration.</param>
        /// <returns>A concrete <see cref="HttpRequestData" /> suitable for Azure Functions trigger tests.</returns>
        public static HttpRequestData CreateHttpRequestData(this Mocker mocker, Action<HttpRequestDataBuilder>? configureRequest = null)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            return GetOrCreateConfiguredFunctionContext(mocker).CreateHttpRequestData(configureRequest);
        }

        /// <summary>
        /// Creates a concrete <see cref="HttpRequestData" /> for the supplied <see cref="FunctionContext" />.
        /// </summary>
        /// <param name="functionContext">The function context to associate with the request.</param>
        /// <param name="configureRequest">Optional request-builder configuration.</param>
        /// <returns>A concrete <see cref="HttpRequestData" /> suitable for Azure Functions trigger tests.</returns>
        public static HttpRequestData CreateHttpRequestData(this FunctionContext functionContext, Action<HttpRequestDataBuilder>? configureRequest = null)
        {
            ArgumentNullException.ThrowIfNull(functionContext);

            var builder = new HttpRequestDataBuilder(functionContext);
            configureRequest?.Invoke(builder);
            return builder.Build();
        }

        /// <summary>
        /// Creates a concrete <see cref="HttpResponseData" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="configureResponse">Optional response-builder configuration.</param>
        /// <returns>A concrete <see cref="HttpResponseData" /> suitable for Azure Functions trigger tests.</returns>
        public static HttpResponseData CreateHttpResponseData(this Mocker mocker, Action<HttpResponseDataBuilder>? configureResponse = null)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            return GetOrCreateConfiguredFunctionContext(mocker).CreateHttpResponseData(configureResponse);
        }

        /// <summary>
        /// Creates a concrete <see cref="HttpResponseData" /> for the supplied <see cref="FunctionContext" />.
        /// </summary>
        /// <param name="functionContext">The function context to associate with the response.</param>
        /// <param name="configureResponse">Optional response-builder configuration.</param>
        /// <returns>A concrete <see cref="HttpResponseData" /> suitable for Azure Functions trigger tests.</returns>
        public static HttpResponseData CreateHttpResponseData(this FunctionContext functionContext, Action<HttpResponseDataBuilder>? configureResponse = null)
        {
            ArgumentNullException.ThrowIfNull(functionContext);

            var builder = new HttpResponseDataBuilder(functionContext);
            configureResponse?.Invoke(builder);
            return builder.Build();
        }

        /// <summary>
        /// Reads the current request body as a string and rewinds the stream when possible.
        /// </summary>
        /// <param name="request">The current request.</param>
        /// <param name="encoding">The text encoding. Defaults to UTF-8.</param>
        /// <returns>The body text.</returns>
        public static Task<string> ReadBodyAsStringAsync(this HttpRequestData request, Encoding? encoding = null)
        {
            ArgumentNullException.ThrowIfNull(request);

            return ReadStreamAsStringAsync(request.Body, encoding);
        }

        /// <summary>
        /// Reads the current request body as JSON and rewinds the stream when possible.
        /// </summary>
        /// <typeparam name="TValue">The expected JSON type.</typeparam>
        /// <param name="request">The current request.</param>
        /// <param name="jsonSerializerOptions">Optional serializer options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized body value.</returns>
        public static Task<TValue?> ReadBodyAsJsonAsync<TValue>(this HttpRequestData request, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return ReadStreamAsJsonAsync<TValue>(request.Body, jsonSerializerOptions, cancellationToken);
        }

        /// <summary>
        /// Reads the current response body as a string and rewinds the stream when possible.
        /// </summary>
        /// <param name="response">The current response.</param>
        /// <param name="encoding">The text encoding. Defaults to UTF-8.</param>
        /// <returns>The body text.</returns>
        public static Task<string> ReadBodyAsStringAsync(this HttpResponseData response, Encoding? encoding = null)
        {
            ArgumentNullException.ThrowIfNull(response);

            return ReadStreamAsStringAsync(response.Body, encoding);
        }

        /// <summary>
        /// Reads the current response body as JSON and rewinds the stream when possible.
        /// </summary>
        /// <typeparam name="TValue">The expected JSON type.</typeparam>
        /// <param name="response">The current response.</param>
        /// <param name="jsonSerializerOptions">Optional serializer options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized body value.</returns>
        public static Task<TValue?> ReadBodyAsJsonAsync<TValue>(this HttpResponseData response, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(response);

            return ReadStreamAsJsonAsync<TValue>(response.Body, jsonSerializerOptions, cancellationToken);
        }

        private static FunctionContext GetOrCreateConfiguredFunctionContext(Mocker mocker)
        {
            var hadTrackedFunctionContext = mocker.Contains(typeof(FunctionContext));
            var hasFunctionContextTypeRegistration = mocker.HasTypeRegistration(typeof(FunctionContext));
            var hasKnownFunctionContextRegistration = mocker.KnownTypeRegistrations.Any(registration => registration.ServiceType == typeof(FunctionContext));
            if (hadTrackedFunctionContext || hasFunctionContextTypeRegistration || hasKnownFunctionContextRegistration)
            {
                var existingFunctionContext = mocker.GetObject<FunctionContext>();
                if (existingFunctionContext?.InstanceServices is not null)
                {
                    return existingFunctionContext;
                }

                var existingProvider = mocker.HasTypeRegistration(typeof(IServiceProvider))
                    ? mocker.GetRequiredObject<IServiceProvider>()
                    : mocker.CreateFunctionContextInstanceServices();

                var configuredExistingFunctionContext = TryAssignFunctionContextInstanceServices(existingFunctionContext, existingProvider);
                // GetObject<FunctionContext>() can materialize a tracked mock from a known-type registration,
                // so re-check the tracked shape after resolution before configuring mock-specific behavior.
                var hasTrackedFunctionContextAfterResolution = mocker.Contains(typeof(FunctionContext));
                if (hasTrackedFunctionContextAfterResolution)
                {
                    mocker.GetOrCreateMock<FunctionContext>().AddFunctionContextInstanceServices(existingProvider);
                }

                if (configuredExistingFunctionContext && existingFunctionContext is not null)
                {
                    return existingFunctionContext;
                }

                return mocker.GetRequiredObject<FunctionContext>();
            }

            if (mocker.HasTypeRegistration(typeof(IServiceProvider)))
            {
                mocker.AddFunctionContextInstanceServices(mocker.GetRequiredObject<IServiceProvider>(), replace: true);
                return mocker.GetRequiredObject<FunctionContext>();
            }

            mocker.AddFunctionContextInstanceServices();
            return mocker.GetRequiredObject<FunctionContext>();
        }

        private static async Task<TValue?> ReadStreamAsJsonAsync<TValue>(Stream stream, JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken)
        {
            ResetStreamPosition(stream);
            var value = await JsonSerializer.DeserializeAsync<TValue>(stream, jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
            ResetStreamPosition(stream);
            return value;
        }

        private static async Task<string> ReadStreamAsStringAsync(Stream stream, Encoding? encoding)
        {
            ResetStreamPosition(stream);

            using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var value = await reader.ReadToEndAsync().ConfigureAwait(false);

            ResetStreamPosition(stream);
            return value;
        }

        private static void ResetStreamPosition(Stream stream)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
        }

        private static bool TryAssignFunctionContextInstanceServices(FunctionContext? functionContext, IServiceProvider instanceServices)
        {
            if (functionContext is null)
            {
                return false;
            }

            try
            {
                functionContext.InstanceServices = instanceServices;
                return functionContext.InstanceServices is not null;
            }
            catch
            {
                return false;
            }
        }
    }
}
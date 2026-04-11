using Azure;
using Azure.Core;

namespace FastMoq.Azure.Pageable
{
    /// <summary>
    /// Represents a lightweight Azure SDK <see cref="Response" /> implementation for test helpers.
    /// </summary>
    public sealed class AzureTestResponse : Response
    {
        private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _headers;
        private readonly int _statusCode;
        private readonly string _reasonPhrase;
        private readonly bool _isError;
        private string _clientRequestId;
        private Stream? _contentStream;

        /// <summary>
        /// Initializes a new <see cref="AzureTestResponse" /> instance.
        /// </summary>
        /// <param name="status">The HTTP status code to expose.</param>
        /// <param name="reasonPhrase">The reason phrase to expose.</param>
        /// <param name="headers">Optional headers to expose through <see cref="Response.Headers" />.</param>
        /// <param name="contentStream">An optional content stream.</param>
        /// <param name="clientRequestId">An optional client request identifier.</param>
        /// <param name="isError">True to mark the response as an error response.</param>
        public AzureTestResponse(
            int status = 200,
            string reasonPhrase = "OK",
            IEnumerable<HttpHeader>? headers = null,
            Stream? contentStream = null,
            string? clientRequestId = null,
            bool isError = false)
        {
            ArgumentNullException.ThrowIfNull(reasonPhrase);

            _statusCode = status;
            _reasonPhrase = reasonPhrase;
            _headers = CreateHeaderLookup(headers);
            _contentStream = contentStream;
            _clientRequestId = string.IsNullOrWhiteSpace(clientRequestId)
                ? Guid.NewGuid().ToString("D")
                : clientRequestId;
            _isError = isError;
        }

        /// <inheritdoc />
        public override int Status => _statusCode;

        /// <inheritdoc />
        public override string ReasonPhrase => _reasonPhrase;

        /// <inheritdoc />
        public override Stream? ContentStream
        {
            get => _contentStream;
            set => _contentStream = value;
        }

        /// <inheritdoc />
        public override string ClientRequestId
        {
            get => _clientRequestId;
            set => _clientRequestId = value;
        }

        /// <inheritdoc />
        public override bool IsError => _isError;

        /// <inheritdoc />
        public override void Dispose()
        {
            _contentStream?.Dispose();
            _contentStream = null;
        }

        /// <inheritdoc />
        protected override bool TryGetHeader(string name, out string value)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (_headers.TryGetValue(name, out var values))
            {
                value = string.Join(",", values);
                return true;
            }

            value = default!;
            return false;
        }

        /// <inheritdoc />
        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (_headers.TryGetValue(name, out var storedValues))
            {
                values = storedValues;
                return true;
            }

            values = default!;
            return false;
        }

        /// <inheritdoc />
        protected override bool ContainsHeader(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            return _headers.ContainsKey(name);
        }

        /// <inheritdoc />
        protected override IEnumerable<HttpHeader> EnumerateHeaders()
        {
            foreach (var header in _headers)
            {
                foreach (var value in header.Value)
                {
                    yield return new HttpHeader(header.Key, value);
                }
            }
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> CreateHeaderLookup(IEnumerable<HttpHeader>? headers)
        {
            if (headers is null)
            {
                return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            }

            var headerLookup = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                if (!headerLookup.TryGetValue(header.Name, out var values))
                {
                    values = new List<string>();
                    headerLookup[header.Name] = values;
                }

                values.Add(header.Value);
            }

            return headerLookup.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>) pair.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
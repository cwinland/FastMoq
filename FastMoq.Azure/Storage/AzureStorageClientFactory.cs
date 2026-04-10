using Azure.Core;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;

namespace FastMoq.Azure.Storage
{
    /// <summary>
    /// Creates Azure Storage SDK clients from a shared connection string or service URI for tests.
    /// </summary>
    public sealed class AzureStorageClientFactory
    {
        private readonly string _connectionValue;
        private readonly StorageConnectionMode _connectionMode;
        private readonly TokenCredential? _credential;
        private readonly BlobClientOptions _blobClientOptions;
        private readonly QueueClientOptions _queueClientOptions;
        private readonly TableClientOptions _tableClientOptions;

        private AzureStorageClientFactory(
            string connectionValue,
            StorageConnectionMode connectionMode,
            TokenCredential? credential,
            BlobClientOptions? blobClientOptions,
            QueueClientOptions? queueClientOptions,
            TableClientOptions? tableClientOptions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionValue);

            _connectionValue = connectionValue;
            _connectionMode = connectionMode;
            _credential = credential;
            _blobClientOptions = blobClientOptions ?? new BlobClientOptions();
            _queueClientOptions = queueClientOptions ?? new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64,
            };
            _tableClientOptions = tableClientOptions ?? new TableClientOptions();
        }

        /// <summary>
        /// Gets the original connection string or service URI used to create the factory.
        /// </summary>
        public string ConnectionValue => _connectionValue;

        /// <summary>
        /// Gets a value indicating whether the factory was created from a connection string.
        /// </summary>
        public bool UsesConnectionString => _connectionMode == StorageConnectionMode.ConnectionString;

        /// <summary>
        /// Gets a value indicating whether the factory can create blob clients.
        /// </summary>
        public bool SupportsBlobClients => UsesConnectionString || _connectionMode == StorageConnectionMode.BlobServiceUri;

        /// <summary>
        /// Gets a value indicating whether the factory can create queue clients.
        /// </summary>
        public bool SupportsQueueClients => UsesConnectionString || _connectionMode == StorageConnectionMode.QueueServiceUri;

        /// <summary>
        /// Gets a value indicating whether the factory can create table clients.
        /// </summary>
        public bool SupportsTableClients => UsesConnectionString || _connectionMode == StorageConnectionMode.TableServiceUri;

        /// <summary>
        /// Creates a factory backed by an Azure Storage connection string.
        /// </summary>
        /// <param name="connectionString">The Azure Storage connection string.</param>
        /// <param name="blobClientOptions">Optional blob client options.</param>
        /// <param name="queueClientOptions">Optional queue client options.</param>
        /// <param name="tableClientOptions">Optional table client options.</param>
        /// <returns>A storage client factory backed by a connection string.</returns>
        public static AzureStorageClientFactory FromConnectionString(
            string connectionString,
            BlobClientOptions? blobClientOptions = null,
            QueueClientOptions? queueClientOptions = null,
            TableClientOptions? tableClientOptions = null)
        {
            return new AzureStorageClientFactory(
                connectionString,
                StorageConnectionMode.ConnectionString,
                credential: null,
                blobClientOptions,
                queueClientOptions,
                tableClientOptions);
        }

        /// <summary>
        /// Creates a factory backed by a blob service URI.
        /// </summary>
        /// <param name="blobServiceUri">The blob service URI.</param>
        /// <param name="credential">The credential used to create blob clients.</param>
        /// <param name="blobClientOptions">Optional blob client options.</param>
        /// <returns>A storage client factory backed by a blob service URI.</returns>
        public static AzureStorageClientFactory FromBlobServiceUri(string blobServiceUri, TokenCredential credential, BlobClientOptions? blobClientOptions = null)
        {
            ArgumentNullException.ThrowIfNull(credential);

            return new AzureStorageClientFactory(
                blobServiceUri,
                StorageConnectionMode.BlobServiceUri,
                credential,
                blobClientOptions,
                queueClientOptions: null,
                tableClientOptions: null);
        }

        /// <summary>
        /// Creates a factory backed by a queue service URI.
        /// </summary>
        /// <param name="queueServiceUri">The queue service URI.</param>
        /// <param name="credential">The credential used to create queue clients.</param>
        /// <param name="queueClientOptions">Optional queue client options.</param>
        /// <returns>A storage client factory backed by a queue service URI.</returns>
        public static AzureStorageClientFactory FromQueueServiceUri(string queueServiceUri, TokenCredential credential, QueueClientOptions? queueClientOptions = null)
        {
            ArgumentNullException.ThrowIfNull(credential);

            return new AzureStorageClientFactory(
                queueServiceUri,
                StorageConnectionMode.QueueServiceUri,
                credential,
                blobClientOptions: null,
                queueClientOptions,
                tableClientOptions: null);
        }

        /// <summary>
        /// Creates a factory backed by a table service URI.
        /// </summary>
        /// <param name="tableServiceUri">The table service URI.</param>
        /// <param name="credential">The credential used to create table clients.</param>
        /// <param name="tableClientOptions">Optional table client options.</param>
        /// <returns>A storage client factory backed by a table service URI.</returns>
        public static AzureStorageClientFactory FromTableServiceUri(string tableServiceUri, TokenCredential credential, TableClientOptions? tableClientOptions = null)
        {
            ArgumentNullException.ThrowIfNull(credential);

            return new AzureStorageClientFactory(
                tableServiceUri,
                StorageConnectionMode.TableServiceUri,
                credential,
                blobClientOptions: null,
                queueClientOptions: null,
                tableClientOptions);
        }

        /// <summary>
        /// Creates a <see cref="BlobContainerClient" />.
        /// </summary>
        /// <param name="containerName">The blob container name.</param>
        /// <returns>A blob container client.</returns>
        public BlobContainerClient CreateBlobContainerClient(string containerName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
            EnsureBlobSupport();

            if (UsesConnectionString)
            {
                return new BlobContainerClient(_connectionValue, containerName, _blobClientOptions);
            }

            return new BlobContainerClient(CreateServiceUri(containerName), _credential!, _blobClientOptions);
        }

        /// <summary>
        /// Creates a <see cref="BlobClient" />.
        /// </summary>
        /// <param name="containerName">The blob container name.</param>
        /// <param name="blobName">The blob name.</param>
        /// <returns>A blob client.</returns>
        public BlobClient CreateBlobClient(string containerName, string blobName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
            ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
            EnsureBlobSupport();

            if (UsesConnectionString)
            {
                return new BlobClient(_connectionValue, containerName, blobName, _blobClientOptions);
            }

            return new BlobClient(CreateServiceUri(containerName, blobName), _credential!, _blobClientOptions);
        }

        /// <summary>
        /// Creates a <see cref="QueueClient" />.
        /// </summary>
        /// <param name="queueName">The queue name.</param>
        /// <returns>A queue client.</returns>
        public QueueClient CreateQueueClient(string queueName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
            EnsureQueueSupport();

            if (UsesConnectionString)
            {
                return new QueueClient(_connectionValue, queueName, _queueClientOptions);
            }

            return new QueueClient(CreateServiceUri(queueName), _credential!, _queueClientOptions);
        }

        /// <summary>
        /// Creates a <see cref="TableClient" />.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <returns>A table client.</returns>
        public TableClient CreateTableClient(string tableName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            EnsureTableSupport();

            if (UsesConnectionString)
            {
                return new TableClient(_connectionValue, tableName, _tableClientOptions);
            }

            return new TableClient(new Uri(_connectionValue, UriKind.Absolute), tableName, _credential!, _tableClientOptions);
        }

        private Uri CreateServiceUri(params string[] segments)
        {
            var baseUri = _connectionValue.EndsWith("/", StringComparison.Ordinal)
                ? new Uri(_connectionValue, UriKind.Absolute)
                : new Uri(_connectionValue + "/", UriKind.Absolute);
            var relativePath = string.Join("/", segments.Select(segment => segment.Trim('/')));
            return new Uri(baseUri, relativePath);
        }

        private void EnsureBlobSupport()
        {
            if (!SupportsBlobClients)
            {
                throw new InvalidOperationException("This storage factory was not created for blob clients. Use FromBlobServiceUri(...) or FromConnectionString(...).");
            }
        }

        private void EnsureQueueSupport()
        {
            if (!SupportsQueueClients)
            {
                throw new InvalidOperationException("This storage factory was not created for queue clients. Use FromQueueServiceUri(...) or FromConnectionString(...).");
            }
        }

        private void EnsureTableSupport()
        {
            if (!SupportsTableClients)
            {
                throw new InvalidOperationException("This storage factory was not created for table clients. Use FromTableServiceUri(...) or FromConnectionString(...).");
            }
        }

        private enum StorageConnectionMode
        {
            ConnectionString,
            BlobServiceUri,
            QueueServiceUri,
            TableServiceUri,
        }
    }
}
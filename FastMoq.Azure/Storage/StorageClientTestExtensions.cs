using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using FastMoq.Azure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace FastMoq.Azure.Storage
{
    /// <summary>
    /// Provides common Azure Storage client registration helpers for tests.
    /// </summary>
    public static class StorageClientTestExtensions
    {
        /// <summary>
        /// Registers a <see cref="TableClient" /> singleton in the supplied service collection.
        /// </summary>
        /// <param name="services">The service collection to update.</param>
        /// <param name="tableClient">The client instance to register.</param>
        /// <returns>The current <see cref="IServiceCollection" /> instance.</returns>
        public static IServiceCollection AddTableClient(this IServiceCollection services, TableClient tableClient)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(tableClient);

            return services.AddAzureClient(tableClient);
        }

        /// <summary>
        /// Registers a <see cref="TableClient" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="tableClient">The client instance to register.</param>
        /// <param name="replace">True to replace an existing registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddTableClient(this Mocker mocker, TableClient tableClient, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(tableClient);

            return mocker.AddAzureClient(tableClient, replace);
        }

        /// <summary>
        /// Registers a <see cref="BlobContainerClient" /> singleton in the supplied service collection.
        /// </summary>
        /// <param name="services">The service collection to update.</param>
        /// <param name="blobContainerClient">The client instance to register.</param>
        /// <returns>The current <see cref="IServiceCollection" /> instance.</returns>
        public static IServiceCollection AddBlobContainerClient(this IServiceCollection services, BlobContainerClient blobContainerClient)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(blobContainerClient);

            return services.AddAzureClient(blobContainerClient);
        }

        /// <summary>
        /// Registers a <see cref="BlobContainerClient" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="blobContainerClient">The client instance to register.</param>
        /// <param name="replace">True to replace an existing registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddBlobContainerClient(this Mocker mocker, BlobContainerClient blobContainerClient, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(blobContainerClient);

            return mocker.AddAzureClient(blobContainerClient, replace);
        }

        /// <summary>
        /// Registers a <see cref="BlobClient" /> singleton in the supplied service collection.
        /// </summary>
        /// <param name="services">The service collection to update.</param>
        /// <param name="blobClient">The client instance to register.</param>
        /// <returns>The current <see cref="IServiceCollection" /> instance.</returns>
        public static IServiceCollection AddBlobClient(this IServiceCollection services, BlobClient blobClient)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(blobClient);

            return services.AddAzureClient(blobClient);
        }

        /// <summary>
        /// Registers a <see cref="BlobClient" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="blobClient">The client instance to register.</param>
        /// <param name="replace">True to replace an existing registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddBlobClient(this Mocker mocker, BlobClient blobClient, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(blobClient);

            return mocker.AddAzureClient(blobClient, replace);
        }

        /// <summary>
        /// Registers a <see cref="QueueClient" /> singleton in the supplied service collection.
        /// </summary>
        /// <param name="services">The service collection to update.</param>
        /// <param name="queueClient">The client instance to register.</param>
        /// <returns>The current <see cref="IServiceCollection" /> instance.</returns>
        public static IServiceCollection AddQueueClient(this IServiceCollection services, QueueClient queueClient)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(queueClient);

            return services.AddAzureClient(queueClient);
        }

        /// <summary>
        /// Registers a <see cref="QueueClient" /> for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="queueClient">The client instance to register.</param>
        /// <param name="replace">True to replace an existing registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddQueueClient(this Mocker mocker, QueueClient queueClient, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(queueClient);

            return mocker.AddAzureClient(queueClient, replace);
        }
    }
}
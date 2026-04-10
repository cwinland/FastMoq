using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using FastMoq;
using FastMoq.Azure.Credentials;
using FastMoq.Azure.DependencyInjection;
using FastMoq.Azure.KeyVault;
using FastMoq.Azure.Pageable;
using FastMoq.Azure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FastMoq.Tests.Azure
{
    public class AzureHelperTests
    {
        [Fact]
        public async Task CreateAsyncPageable_ShouldYieldAllValuesAndPreserveResponses()
        {
            var response = new AzureTestResponse(
                status: 206,
                headers:
                [
                    new HttpHeader("x-ms-request-id", "req-123"),
                ]);
            var pageable = PageableBuilder.CreateAsyncPageable(new[] { "one", "two", "three" }, pageSize: 2, response);

            var values = new List<string>();
            await foreach (var value in pageable)
            {
                values.Add(value);
            }

            var pages = new List<Page<string>>();
            await foreach (var page in pageable.AsPages())
            {
                pages.Add(page);
            }

            values.Should().Equal("one", "two", "three");
            pages.Should().HaveCount(2);
            pages[0].ContinuationToken.Should().Be("2");
            pages[1].ContinuationToken.Should().BeNull();
            pages[0].GetRawResponse().Status.Should().Be(206);
            pages[0].GetRawResponse().Headers.TryGetValue("x-ms-request-id", out var requestId).Should().BeTrue();
            requestId.Should().Be("req-123");
        }

        [Fact]
        public void CreatePageable_ShouldSupportExplicitPages()
        {
            var firstPage = PageableBuilder.CreatePage(new[] { 1, 2 }, continuationToken: "2");
            var secondPage = PageableBuilder.CreatePage(new[] { 3 });

            var pageable = PageableBuilder.CreatePageable(new[] { firstPage, secondPage });

            pageable.Should().Equal(1, 2, 3);
            pageable.AsPages().Select(page => page.ContinuationToken).Should().Equal("2", null);
        }

        [Fact]
        public void AddTokenCredential_ShouldRegisterFixedCredential()
        {
            var mocker = new Mocker();

            mocker.AddTokenCredential("token-value");

            var credential = mocker.GetRequiredObject<TokenCredential>();
            var accessToken = credential.GetToken(new TokenRequestContext(new[] { "https://storage.azure.com/.default" }), CancellationToken.None);

            accessToken.Token.Should().Be("token-value");
        }

        [Fact]
        public void AddDefaultAzureCredential_ShouldExposeConcreteAndAbstractCredentialRegistrations()
        {
            var mocker = new Mocker();

            mocker.AddDefaultAzureCredential("default-token");

            var defaultCredential = mocker.GetRequiredObject<DefaultAzureCredential>();
            var tokenCredential = mocker.GetRequiredObject<TokenCredential>();
            var accessToken = defaultCredential.GetToken(new TokenRequestContext(new[] { "scope" }), CancellationToken.None);

            tokenCredential.Should().BeSameAs(defaultCredential);
            accessToken.Token.Should().Be("default-token");
        }

        [Fact]
        public void CreateAzureServiceProvider_ShouldRegisterConfigurationLoggingAndClients()
        {
            var mocker = new Mocker();
            var endpoint = new Uri("https://fastmoq-test.vault.azure.net/");
            var secretClient = new SecretClient(endpoint, new TestTokenCredential("secret-token"));

            var provider = mocker.CreateAzureServiceProvider(
                services => services.AddSecretClient(secretClient),
                new Dictionary<string, string?>
                {
                    ["Azure:KeyVault:Endpoint"] = endpoint.ToString(),
                });

            provider.GetRequiredService<ILoggerFactory>().Should().NotBeNull();
            provider.GetRequiredService<IConfiguration>()["Azure:KeyVault:Endpoint"].Should().Be(endpoint.ToString());
            provider.GetRequiredService<IConfigurationRoot>().Should().NotBeNull();
            provider.GetRequiredService<SecretClient>().Should().BeSameAs(secretClient);
        }

        [Fact]
        public void AddAzureServiceProvider_ShouldRegisterProviderAndConfigurationOnMocker()
        {
            var mocker = new Mocker();
            var provider = mocker.CreateAzureServiceProvider(
                configurationValues: new Dictionary<string, string?>
                {
                    ["Azure:Storage:QueueName"] = "jobs",
                });

            mocker.AddAzureServiceProvider(provider);

            mocker.GetRequiredObject<IServiceProvider>().Should().BeSameAs(provider);
            mocker.GetRequiredObject<IConfiguration>()["Azure:Storage:QueueName"].Should().Be("jobs");
            mocker.GetRequiredObject<IConfigurationRoot>().Should().NotBeNull();
        }

        [Fact]
        public void ClientRegistrationHelpers_ShouldRegisterKeyVaultAndStorageClients()
        {
            var mocker = new Mocker();
            var secretClient = new SecretClient(new Uri("https://fastmoq-test.vault.azure.net/"), new TestTokenCredential("secret-token"));
            var blobContainerClient = new BlobContainerClient(new Uri("https://fastmoqstorage.blob.core.windows.net/orders"));
            var services = new ServiceCollection();

            mocker.AddSecretClient(secretClient);
            mocker.AddBlobContainerClient(blobContainerClient);
            services.AddSecretClient(secretClient);
            services.AddBlobContainerClient(blobContainerClient);

            var provider = services.BuildServiceProvider();

            mocker.GetRequiredObject<SecretClient>().Should().BeSameAs(secretClient);
            mocker.GetRequiredObject<BlobContainerClient>().Should().BeSameAs(blobContainerClient);
            provider.GetRequiredService<SecretClient>().Should().BeSameAs(secretClient);
            provider.GetRequiredService<BlobContainerClient>().Should().BeSameAs(blobContainerClient);
        }

        [Fact]
        public void AzureStorageClientFactory_ShouldCreateClientsFromConnectionString()
        {
            var factory = AzureStorageClientFactory.FromConnectionString("UseDevelopmentStorage=true");

            var blobContainerClient = factory.CreateBlobContainerClient("orders");
            var blobClient = factory.CreateBlobClient("orders", "payloads/order.json");
            var queueClient = factory.CreateQueueClient("jobs");
            var tableClient = factory.CreateTableClient("status");

            factory.UsesConnectionString.Should().BeTrue();
            factory.SupportsBlobClients.Should().BeTrue();
            factory.SupportsQueueClients.Should().BeTrue();
            factory.SupportsTableClients.Should().BeTrue();
            blobContainerClient.Uri.AbsoluteUri.Should().Contain("/orders");
            blobClient.Uri.AbsoluteUri.Should().Contain("/orders/payloads/order.json");
            queueClient.Uri.AbsoluteUri.Should().Contain("/jobs");
            tableClient.Uri.AbsoluteUri.Should().Contain("/status");
        }

        [Fact]
        public void AzureStorageClientFactory_ShouldCreateClientsFromServiceUris()
        {
            var credential = new TestTokenCredential("storage-token");
            var blobFactory = AzureStorageClientFactory.FromBlobServiceUri("https://fastmoqstorage.blob.core.windows.net", credential);
            var queueFactory = AzureStorageClientFactory.FromQueueServiceUri("https://fastmoqstorage.queue.core.windows.net", credential);
            var tableFactory = AzureStorageClientFactory.FromTableServiceUri("https://fastmoqstorage.table.core.windows.net", credential);

            blobFactory.CreateBlobContainerClient("orders").Uri.Should().Be(new Uri("https://fastmoqstorage.blob.core.windows.net/orders"));
            blobFactory.CreateBlobClient("orders", "payload.json").Uri.Should().Be(new Uri("https://fastmoqstorage.blob.core.windows.net/orders/payload.json"));
            queueFactory.CreateQueueClient("jobs").Uri.Should().Be(new Uri("https://fastmoqstorage.queue.core.windows.net/jobs"));
            tableFactory.CreateTableClient("status").Uri.Should().Be(new Uri("https://fastmoqstorage.table.core.windows.net/status"));

            Action invalidQueueCreate = () => blobFactory.CreateQueueClient("jobs");
            invalidQueueCreate.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void AzureStorageClientFactoryRegistrationHelpers_ShouldRegisterFactoryAndDerivedClients()
        {
            var mocker = new Mocker();
            var factory = AzureStorageClientFactory.FromConnectionString("UseDevelopmentStorage=true");
            var services = new ServiceCollection();

            mocker.AddAzureStorageClientFactory(factory);
            mocker.AddBlobContainerClient(factory, "orders");
            mocker.AddQueueClient(factory, "jobs");
            mocker.AddTableClient(factory, "status");

            services.AddAzureStorageClientFactory(factory);
            services.AddBlobContainerClient(factory, "orders");
            services.AddQueueClient(factory, "jobs");
            services.AddTableClient(factory, "status");

            var provider = services.BuildServiceProvider();

            mocker.GetRequiredObject<AzureStorageClientFactory>().Should().BeSameAs(factory);
            mocker.GetRequiredObject<BlobContainerClient>().Uri.AbsoluteUri.Should().Contain("/orders");
            mocker.GetRequiredObject<QueueClient>().Uri.AbsoluteUri.Should().Contain("/jobs");
            mocker.GetRequiredObject<TableClient>().Uri.AbsoluteUri.Should().Contain("/status");

            provider.GetRequiredService<AzureStorageClientFactory>().Should().BeSameAs(factory);
            provider.GetRequiredService<BlobContainerClient>().Uri.AbsoluteUri.Should().Contain("/orders");
            provider.GetRequiredService<QueueClient>().Uri.AbsoluteUri.Should().Contain("/jobs");
            provider.GetRequiredService<TableClient>().Uri.AbsoluteUri.Should().Contain("/status");
        }
    }
}
using System;
using Azure.Core.Serialization;
using FastMoq.AzureFunctions.Extensions;
using FastMoq.Extensions;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using FastMoq.Providers.NSubstituteProvider;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FastMoq.Tests
{
    public class ServiceProviderHelperTests
    {
        [Fact]
        public void CreateTypedServiceProvider_ShouldResolveRegisteredServicesByType()
        {
            var mocker = new Mocker();
            var expectedUri = new Uri("https://fastmoq.dev/");

            var provider = mocker.CreateTypedServiceProvider(services =>
            {
                services.AddSingleton(expectedUri);
                services.AddOptions();
            });

            provider.GetService(typeof(Uri)).Should().BeSameAs(expectedUri);
        }

        [Fact]
        public void AddServiceProvider_ShouldRegisterTypedProviderAndScopeFactory()
        {
            var mocker = new Mocker();
            var provider = mocker.CreateTypedServiceProvider(services => services.AddOptions());

            mocker.AddServiceProvider(provider);

            mocker.GetObject<IServiceProvider>().Should().BeSameAs(provider);
            mocker.GetObject<IServiceScopeFactory>().Should().NotBeNull();
            mocker.GetObject<IServiceProviderIsService>().Should().NotBeNull();
        }

        [Fact]
        public void CreateFunctionContextInstanceServices_ShouldIncludeWorkerDefaults()
        {
            var mocker = new Mocker();
            var expectedUri = new Uri("https://functions.fastmoq/");

            var provider = mocker.CreateFunctionContextInstanceServices(services => services.AddSingleton(expectedUri));
            var workerOptions = provider.GetService(typeof(WorkerOptions));
            var workerOptionsAccessor = provider.GetService(typeof(IOptions<WorkerOptions>));
            var serializer = provider.GetService(typeof(ObjectSerializer));

            provider.GetService(typeof(ILoggerFactory)).Should().NotBeNull();
            provider.GetService(typeof(IOptions<WorkerOptions>)).Should().NotBeNull();
            workerOptions.Should().BeSameAs(((IOptions<WorkerOptions>) workerOptionsAccessor!).Value);
            serializer.Should().BeSameAs(((WorkerOptions) workerOptions!).Serializer);
            serializer.Should().BeOfType<JsonObjectSerializer>();
            provider.GetService(typeof(Uri)).Should().BeSameAs(expectedUri);
        }

        [Fact]
        public void CreateFunctionContextInstanceServices_ShouldAllowWorkerDefaultsToBeCustomized()
        {
            var mocker = new Mocker();
            var expectedSerializer = new JsonObjectSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

            var provider = mocker.CreateFunctionContextInstanceServices(services =>
            {
                services.Configure<WorkerOptions>(options => options.Serializer = expectedSerializer);
            });

            provider.GetService(typeof(WorkerOptions)).Should().BeSameAs(((IOptions<WorkerOptions>) provider.GetService(typeof(IOptions<WorkerOptions>))!).Value);
            provider.GetService(typeof(ObjectSerializer)).Should().BeSameAs(expectedSerializer);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public void AddFunctionContextInstanceServices_ShouldConfigureFunctionContextInstanceServices(string providerName)
        {
            using var providerScope = PushProviderScope(providerName);
            var mocker = new Mocker();
            var expectedUri = new Uri("https://functions.fastmoq/");

            mocker.AddFunctionContextInstanceServices(services => services.AddSingleton(expectedUri), replace: true);

            var functionContext = mocker.GetObject<FunctionContext>();

            functionContext.Should().NotBeNull();
            functionContext!.InstanceServices.Should().NotBeNull();
            functionContext.InstanceServices.GetService(typeof(Uri)).Should().BeSameAs(expectedUri);
            functionContext.InstanceServices.GetService(typeof(IOptions<WorkerOptions>)).Should().NotBeNull();
            functionContext.InstanceServices.GetService(typeof(WorkerOptions)).Should().NotBeNull();
            functionContext.InstanceServices.GetService(typeof(ObjectSerializer)).Should().NotBeNull();
        }

        [Fact]
        public void AddFunctionContextInstanceServices_ShouldUpdateExistingTrackedFunctionContext()
        {
            using var providerScope = PushProviderScope("moq");
            var mocker = new Mocker();
            var existing = mocker.GetOrCreateMock<FunctionContext>();
            var provider = mocker.CreateFunctionContextInstanceServices(services => services.AddLogging());

            mocker.AddFunctionContextInstanceServices(provider, replace: true);

            existing.Instance.InstanceServices.Should().BeSameAs(provider);
        }

        private static IDisposable PushProviderScope(string providerName)
        {
            EnsureProviderRegistered(providerName);
            return MockingProviderRegistry.Push(providerName);
        }

        private static void EnsureProviderRegistered(string providerName)
        {
            if (MockingProviderRegistry.TryGet(providerName, out _))
            {
                return;
            }

            if (string.Equals(providerName, "moq", StringComparison.OrdinalIgnoreCase))
            {
                MockingProviderRegistry.Register("moq", MoqMockingProvider.Instance, setAsDefault: false);
                return;
            }

            if (string.Equals(providerName, "nsubstitute", StringComparison.OrdinalIgnoreCase))
            {
                MockingProviderRegistry.Register("nsubstitute", NSubstituteMockingProvider.Instance, setAsDefault: false);
                return;
            }

            throw new InvalidOperationException($"Unknown provider '{providerName}'.");
        }
    }
}
using System;
using FastMoq.Extensions;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using FastMoq.Providers.NSubstituteProvider;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

            provider.GetService(typeof(ILoggerFactory)).Should().NotBeNull();
            provider.GetService(typeof(IOptions<WorkerOptions>)).Should().NotBeNull();
            provider.GetService(typeof(Uri)).Should().BeSameAs(expectedUri);
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
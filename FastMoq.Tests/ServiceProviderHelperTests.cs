using System;
using Azure.Core.Serialization;
using FastMoq.AzureFunctions.Extensions;
using FastMoq.Extensions;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using FastMoq.Providers.NSubstituteProvider;
using FastMoq.Providers.ReflectionProvider;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

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
        public void SetupOptions_ShouldRegisterConcreteOptionsValue()
        {
            var mocker = new Mocker();
            var expected = new SampleOptions
            {
                Name = "alpha",
                RetryCount = 2,
            };

            mocker.SetupOptions(expected);

            var options = mocker.GetObject<IOptions<SampleOptions>>();

            options.Should().NotBeNull();
            options!.Value.Should().BeSameAs(expected);
        }

        [Fact]
        public void SetupOptions_ShouldCreateAndReplaceOptionsValueFromFactory()
        {
            var mocker = new Mocker();

            mocker.SetupOptions(new SampleOptions
            {
                Name = "before",
                RetryCount = 1,
            });

            mocker.SetupOptions(() => new SampleOptions
            {
                Name = "after",
                RetryCount = 5,
            }, replace: true);

            var options = mocker.GetObject<IOptions<SampleOptions>>();

            options.Should().NotBeNull();
            options!.Value.Name.Should().Be("after");
            options.Value.RetryCount.Should().Be(5);
        }

        [Fact]
        public void SetupOptions_ShouldCreateDefaultOptionsValue()
        {
            var mocker = new Mocker();

            mocker.SetupOptions<SampleOptions>();

            var options = mocker.GetObject<IOptions<SampleOptions>>();

            options.Should().NotBeNull();
            options!.Value.Should().NotBeNull();
            options.Value.Name.Should().BeNull();
            options.Value.RetryCount.Should().Be(0);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public void AddLoggerFactory_ShouldRegisterLoggerFactoryAndTypedLoggers(string providerName)
        {
            using var providerScope = PushProviderScope(providerName);
            var mocker = new Mocker();

            mocker.AddLoggerFactory(replace: true);

            var loggerFactory = mocker.GetObject<ILoggerFactory>();
            var logger = mocker.GetObject<ILogger<ServiceProviderHelperTests>>();

            loggerFactory.Should().NotBeNull();
            logger.Should().NotBeNull();

            loggerFactory!.CreateLogger("fastmoq.tests").LogInformation("factory logger");
            logger!.LogWarning("typed logger");

            mocker.VerifyLogged(LogLevel.Information, "factory logger");
            mocker.VerifyLogged(LogLevel.Warning, "typed logger");
        }

        [Fact]
        public void CreateLoggerFactory_ShouldSupportTypedServiceProviderComposition()
        {
            using var providerScope = PushProviderScope("reflection");
            var mocker = new Mocker();
            var loggerFactory = mocker.CreateLoggerFactory();
            var provider = mocker.CreateTypedServiceProvider(services =>
            {
                services.AddSingleton(loggerFactory);
                services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            });

            mocker.AddServiceProvider(provider, replace: true);

            var resolvedProvider = mocker.GetObject<IServiceProvider>();
            resolvedProvider.Should().NotBeNull();

            var serviceProvider = resolvedProvider!;
            var resolvedFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var typedLogger = serviceProvider.GetRequiredService<ILogger<ServiceProviderHelperTests>>();
            var factoryLogger = resolvedFactory.CreateLogger("service-provider");

            factoryLogger.LogInformation("service provider factory");
            typedLogger.LogError("service provider typed logger");

            mocker.VerifyLogged(LogLevel.Information, "service provider factory");
            mocker.VerifyLogged(LogLevel.Error, "service provider typed logger");
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

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public async Task CreateHttpRequestData_ShouldCreateConfiguredRequestAndDefaultResponse(string providerName)
        {
            using var providerScope = PushProviderScope(providerName);
            var mocker = new Mocker();
            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "chris")], "TestAuth"));

            var request = mocker.CreateHttpRequestData(builder => builder
                .WithMethod("PUT")
                .WithUrl("https://fastmoq.dev/widgets?existing=1")
                .WithHeader("x-fastmoq", "true")
                .WithCookie("session", "abc")
                .WithQueryParameter("mode", "test")
                .WithClaimsPrincipal(principal)
                .WithJsonBody(new TriggerPayload
                {
                    Name = "alpha",
                    Count = 2,
                }));

            request.Method.Should().Be("PUT");
            request.Url.Query.Should().Contain("existing=1");
            request.Url.Query.Should().Contain("mode=test");
            request.Query["existing"].Should().Be("1");
            request.Query["mode"].Should().Be("test");
            request.Headers.TryGetValues("Host", out var hostValues).Should().BeTrue();
            hostValues.Should().ContainSingle().Which.Should().Be("fastmoq.dev");
            request.Headers.TryGetValues("Content-Type", out var contentTypes).Should().BeTrue();
            contentTypes.Should().ContainSingle().Which.Should().StartWith("application/json");
            request.Identities.Should().ContainSingle().Which.Name.Should().Be("chris");
            request.Cookies.Should().ContainSingle(cookie => cookie.Name == "session" && cookie.Value == "abc");

            var payload = await request.ReadBodyAsJsonAsync<TriggerPayload>();
            payload.Should().BeEquivalentTo(new TriggerPayload
            {
                Name = "alpha",
                Count = 2,
            });

            var bodyText = await request.ReadBodyAsStringAsync();
            bodyText.Should().Contain("alpha");

            var response = request.CreateResponse();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.FunctionContext.Should().BeSameAs(request.FunctionContext);
            response.Headers.Should().NotBeNull();
            response.Body.Should().NotBeNull();
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public void CreateHttpRequestData_ShouldRejectRelativeUriInstances(string providerName)
        {
            using var providerScope = PushProviderScope(providerName);
            var mocker = new Mocker();

            Action action = () => mocker.CreateHttpRequestData(builder => builder.WithUrl(new Uri("widgets", UriKind.Relative)));

            action.Should().Throw<ArgumentException>()
                .WithParameterName("url")
                .WithMessage("*absolute*");
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public async Task CreateHttpResponseData_ShouldSerializeJsonHeadersAndReadableBody(string providerName)
        {
            using var providerScope = PushProviderScope(providerName);
            var mocker = new Mocker();

            var response = mocker.CreateHttpResponseData(builder => builder
                .WithStatusCode(HttpStatusCode.Accepted)
                .WithHeader("x-correlation-id", "123")
                .WithCookie("session", "abc")
                .WithJsonBody(new TriggerPayload
                {
                    Name = "beta",
                    Count = 7,
                }));

            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            response.Headers.TryGetValues("x-correlation-id", out var correlationIds).Should().BeTrue();
            correlationIds.Should().ContainSingle().Which.Should().Be("123");
            response.FunctionContext.InstanceServices.Should().NotBeNull();

            var payload = await response.ReadBodyAsJsonAsync<TriggerPayload>();
            payload.Should().BeEquivalentTo(new TriggerPayload
            {
                Name = "beta",
                Count = 7,
            });

            var bodyText = await response.ReadBodyAsStringAsync();
            bodyText.Should().Contain("beta");

            response.Cookies.Should().NotBeNull();
            response.Cookies.Append("later", "cookie");
        }

        [Fact]
        public void CreateHttpRequestData_ShouldReuseExistingFunctionContextInstanceServices()
        {
            using var providerScope = PushProviderScope("moq");
            var mocker = new Mocker();
            var expectedUri = new Uri("https://functions.fastmoq/");

            mocker.AddFunctionContextInstanceServices(services => services.AddSingleton(expectedUri), replace: true);

            var request = mocker.CreateHttpRequestData();

            request.FunctionContext.InstanceServices.GetService(typeof(Uri)).Should().BeSameAs(expectedUri);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public void CreateHttpRequestData_ShouldReuseExistingServiceProviderRegistration(string providerName)
        {
            using var providerScope = PushProviderScope(providerName);
            var mocker = new Mocker();
            var expectedUri = new Uri("https://services.fastmoq/");
            var provider = mocker.CreateTypedServiceProvider(services =>
            {
                services.AddSingleton(expectedUri);
                services.AddOptions();
            });

            mocker.AddServiceProvider(provider, replace: true);

            var request = mocker.CreateHttpRequestData();

            request.FunctionContext.InstanceServices.Should().BeSameAs(provider);
            request.FunctionContext.InstanceServices.GetService(typeof(Uri)).Should().BeSameAs(expectedUri);
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

            if (string.Equals(providerName, "reflection", StringComparison.OrdinalIgnoreCase))
            {
                MockingProviderRegistry.Register("reflection", ReflectionMockingProvider.Instance, setAsDefault: false);
                return;
            }

            throw new InvalidOperationException($"Unknown provider '{providerName}'.");
        }

        private sealed class TriggerPayload
        {
            public int Count { get; set; }

            public string? Name { get; set; }
        }

        private sealed class SampleOptions
        {
            public string? Name { get; set; }

            public int RetryCount { get; set; }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Immutable;
using System.Collections.Generic;
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
using System.Threading;
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
        public void CreateTypedServiceProvider_ShouldFallbackToMockerResolution_WhenEnabled()
        {
            var mocker = new Mocker();
            mocker.SetupOptions(new SampleOptions
            {
                Name = "fallback",
                RetryCount = 7,
            });

            var provider = mocker.CreateTypedServiceProvider(includeMockerFallback: true);
            var options = provider.GetService(typeof(IOptions<SampleOptions>)) as IOptions<SampleOptions>;

            options.Should().NotBeNull();
            options!.Value.Name.Should().Be("fallback");
            options.Value.RetryCount.Should().Be(7);
        }

        [Fact]
        public void CreateTypedServiceProvider_ShouldResolveExplicitSealedRegistrations_WhenFallbackIsEnabled()
        {
            var mocker = new Mocker();
            var expectedUri = new Uri("https://fallback.fastmoq/");
            mocker.AddType(expectedUri, replace: true);

            var provider = mocker.CreateTypedServiceProvider(includeMockerFallback: true);

            provider.GetService(typeof(Uri)).Should().BeSameAs(expectedUri);
        }

        [Fact]
        public void CreateTypedServiceProvider_ShouldReturnNull_ForUnknownValueTypeServices_WhenFallbackIsEnabled()
        {
            var mocker = new Mocker();
            var provider = mocker.CreateTypedServiceProvider(includeMockerFallback: true);

            provider.GetService(typeof(int)).Should().BeNull();
        }

        [Fact]
        public void CreateTypedServiceScope_ShouldResolveScopedServicesByType()
        {
            var mocker = new Mocker();

            using var scope = mocker.CreateTypedServiceScope(services => services.AddScoped<ScopedProbe>());

            var first = scope.ServiceProvider.GetRequiredService<ScopedProbe>();
            var second = scope.ServiceProvider.GetRequiredService<ScopedProbe>();

            first.Should().BeSameAs(second);
        }

        [Fact]
        public void CreateTypedServiceScope_ShouldFallbackToMockerResolution_WhenEnabled()
        {
            var mocker = new Mocker();
            mocker.SetupOptions(new SampleOptions
            {
                Name = "scoped-fallback",
                RetryCount = 3,
            });

            using var scope = mocker.CreateTypedServiceScope(includeMockerFallback: true);

            var options = scope.ServiceProvider.GetRequiredService<IOptions<SampleOptions>>();

            options.Value.Name.Should().Be("scoped-fallback");
            options.Value.RetryCount.Should().Be(3);
        }

        [Fact]
        public void CreateTypedServiceScope_ShouldDisposeOwnedRootProvider_WhenScopeIsDisposed()
        {
            var mocker = new Mocker();
            DisposableProbe? probe;

            var scope = mocker.CreateTypedServiceScope(services => services.AddSingleton<DisposableProbe>());
            probe = scope.ServiceProvider.GetRequiredService<DisposableProbe>();

            scope.Dispose();

            probe.IsDisposed.Should().BeTrue();
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
        public void AddServiceScope_ShouldRegisterTypedScopeAndScopeOwnedProvider()
        {
            var mocker = new Mocker();
            using var scope = mocker.CreateTypedServiceScope(services => services.AddScoped<ScopedProbe>());

            mocker.AddServiceScope(scope);

            mocker.GetObject<IServiceScope>().Should().BeSameAs(scope);
            mocker.GetObject<IServiceProvider>().Should().BeSameAs(scope.ServiceProvider);
            mocker.GetObject<IServiceProvider>()!.GetRequiredService<ScopedProbe>().Should().NotBeNull();
        }

        [Fact]
        public void AddServiceScope_ShouldRegisterProviderBackedScopeAndFixedScopeFactory()
        {
            var mocker = new Mocker();
            var provider = mocker.CreateTypedServiceProvider(services => services.AddScoped<ScopedProbe>());

            mocker.AddServiceScope(provider, replace: true);

            var scope = mocker.GetObject<IServiceScope>();
            var scopeFactory = mocker.GetObject<IServiceScopeFactory>();

            scope.Should().NotBeNull();
            scope!.ServiceProvider.Should().BeSameAs(provider);
            scopeFactory.Should().NotBeNull();
            scopeFactory!.CreateScope().Should().BeSameAs(scope);
            mocker.GetObject<IServiceProvider>().Should().BeSameAs(provider);
        }

        [Fact]
        public void AddServiceProvider_ShouldDisposeOwnedProvider_WhenMockerIsDisposed()
        {
            DisposableProbe? probe;

            using (var mocker = new Mocker())
            {
                mocker.AddServiceProvider(services => services.AddSingleton<DisposableProbe>(), replace: true);
                probe = mocker.GetObject<IServiceProvider>()!.GetRequiredService<DisposableProbe>();
            }

            probe!.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void AddServiceScope_ShouldDisposeOwnedScope_WhenMockerIsDisposed()
        {
            DisposableProbe? probe;

            using (var mocker = new Mocker())
            {
                mocker.AddServiceScope(services => services.AddSingleton<DisposableProbe>(), replace: true);
                probe = mocker.GetObject<IServiceProvider>()!.GetRequiredService<DisposableProbe>();
            }

            probe!.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public async Task AddServiceProvider_ShouldDisposeOwnedProvider_WhenMockerIsDisposedAsync()
        {
            DisposableProbe? probe;

            var mocker = new Mocker();
            mocker.AddServiceProvider(services => services.AddSingleton<DisposableProbe>(), replace: true);
            probe = mocker.GetObject<IServiceProvider>()!.GetRequiredService<DisposableProbe>();

            await mocker.DisposeAsync();

            probe!.IsDisposed.Should().BeTrue();
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
        public void SetupOptions_ShouldEvaluateFactoryPerResolution()
        {
            var mocker = new Mocker();
            var nextRetryCount = 1;

            mocker.SetupOptions(() => new SampleOptions
            {
                RetryCount = nextRetryCount++,
            });

            var first = mocker.GetObject<IOptions<SampleOptions>>();
            var second = mocker.GetObject<IOptions<SampleOptions>>();

            first.Should().NotBeNull();
            second.Should().NotBeNull();
            first!.Value.RetryCount.Should().Be(1);
            second!.Value.RetryCount.Should().Be(2);
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

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public void AddLoggerFactory_WithSink_ShouldMirrorLogsAndPreserveVerification(string providerName)
        {
            using var providerScope = PushProviderScope(providerName);
            var mirroredEntries = new List<string>();
            var constructorEntries = new List<string>();
            var mocker = new Mocker((_, _, message, _) => constructorEntries.Add(message));

            mocker.AddLoggerFactory((logLevel, eventId, message, exception) =>
            {
                mirroredEntries.Add($"{logLevel}:{eventId.Id}:{message}:{exception?.Message}");
            }, replace: true);

            var loggerFactory = mocker.GetObject<ILoggerFactory>();
            var logger = mocker.GetObject<ILogger<ServiceProviderHelperTests>>();

            loggerFactory.Should().NotBeNull();
            logger.Should().NotBeNull();

            loggerFactory!.CreateLogger("fastmoq.sink").LogInformation("factory sink logger");
            logger!.LogError(12, new InvalidOperationException("sink boom"), "typed sink logger");

            mirroredEntries.Should().HaveCount(2);
            mirroredEntries[0].Should().Contain("Information:0:factory sink logger");
            mirroredEntries[1].Should().Contain("Error:12:typed sink logger:sink boom");
            constructorEntries.Should().Contain("factory sink logger");
            constructorEntries.Should().Contain("typed sink logger");

            mocker.VerifyLogged(LogLevel.Information, "factory sink logger");
            mocker.VerifyLogged(LogLevel.Error, "typed sink logger", new InvalidOperationException("sink boom"), 12, TimesSpec.Once);
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
        public void CreateLoggerFactory_WithLineWriter_ShouldMirrorFormattedOutputForTypedServiceProvider()
        {
            using var providerScope = PushProviderScope("reflection");
            var lines = new List<string>();
            var mocker = new Mocker();
            var loggerFactory = mocker.CreateLoggerFactory(lines.Add);
            var provider = mocker.CreateTypedServiceProvider(services =>
            {
                services.AddSingleton(loggerFactory);
                services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            });

            mocker.AddServiceProvider(provider, replace: true);

            var serviceProvider = mocker.GetObject<IServiceProvider>();
            serviceProvider.Should().NotBeNull();

            var resolvedFactory = serviceProvider!.GetRequiredService<ILoggerFactory>();
            var typedLogger = serviceProvider.GetRequiredService<ILogger<ServiceProviderHelperTests>>();

            resolvedFactory.CreateLogger("line-writer").LogInformation("line sink factory");
            typedLogger.LogError(12, new InvalidOperationException("line sink boom"), "line sink typed");

            lines.Should().HaveCount(2);
            lines[0].Should().Contain("[I]");
            lines[0].Should().Contain("line sink factory");
            lines[1].Should().Contain("[E]");
            lines[1].Should().Contain("12");
            lines[1].Should().Contain("line sink typed");
            lines[1].Should().Contain("line sink boom");

            mocker.VerifyLogged(LogLevel.Information, "line sink factory");
            mocker.VerifyLogged(LogLevel.Error, "line sink typed", new InvalidOperationException("line sink boom"), 12, TimesSpec.Once);
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
        public void AddFunctionContextInvocationId_ShouldConfigureTrackedFunctionContext(string providerName)
        {
            using var providerScope = PushProviderScope(providerName);
            var mocker = new Mocker();

            mocker.AddFunctionContextInvocationId("inv-123", replace: true);

            var functionContext = mocker.GetObject<FunctionContext>();

            functionContext.Should().NotBeNull();
            functionContext!.InvocationId.Should().Be("inv-123");
        }

        [Fact]
        public void AddFunctionContextInvocationId_ShouldUpdateExistingTrackedFunctionContext()
        {
            using var providerScope = PushProviderScope("moq");
            var mocker = new Mocker();
            var existing = mocker.GetOrCreateMock<FunctionContext>();

            mocker.AddFunctionContextInvocationId("inv-456", replace: true);

            existing.Instance.InvocationId.Should().Be("inv-456");
        }

        [Theory]
        [InlineData("moq", true)]
        [InlineData("moq", false)]
        [InlineData("nsubstitute", true)]
        [InlineData("nsubstitute", false)]
        public void CreateHttpRequestData_ShouldPreserveComposedFunctionContextHelpers(string providerName, bool registerInvocationIdFirst)
        {
            using var providerScope = PushProviderScope(providerName);
            var mocker = new Mocker();
            var expectedUri = new Uri("https://composed.fastmoq/");
            var provider = mocker.CreateFunctionContextInstanceServices(services => services.AddSingleton(expectedUri));

            if (registerInvocationIdFirst)
            {
                mocker.AddFunctionContextInvocationId("inv-composed", replace: true);
                mocker.AddFunctionContextInstanceServices(provider, replace: true);
            }
            else
            {
                mocker.AddFunctionContextInstanceServices(provider, replace: true);
                mocker.AddFunctionContextInvocationId("inv-composed", replace: true);
            }

            var request = mocker.CreateHttpRequestData();

            request.FunctionContext.InvocationId.Should().Be("inv-composed");
            request.FunctionContext.InstanceServices.Should().BeSameAs(provider);
            request.FunctionContext.InstanceServices.GetService(typeof(Uri)).Should().BeSameAs(expectedUri);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public void AddFunctionContextInvocationId_OnTrackedMock_ShouldConfigureOnlyTheFunctionContextMock(string providerName)
        {
            using var providerScope = PushProviderScope(providerName);
            var mocker = new Mocker();
            var context = mocker.GetOrCreateMock<FunctionContext>();

            context.AddFunctionContextInvocationId("inv-789");

            context.Instance.InvocationId.Should().Be("inv-789");
        }

        [Fact]
        public void CreateHttpRequestData_ShouldReuseConfiguredFunctionContextInvocationId()
        {
            using var providerScope = PushProviderScope("moq");
            var mocker = new Mocker();

            mocker.AddFunctionContextInvocationId("inv-request", replace: true);

            var request = mocker.CreateHttpRequestData();

            request.FunctionContext.InvocationId.Should().Be("inv-request");
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public void AddFunctionContextInstanceServices_OnTrackedMock_ShouldNotReplaceGlobalServiceProvider(string providerName)
        {
            using var providerScope = PushProviderScope(providerName);
            var mocker = new Mocker();
            var globalProvider = mocker.CreateTypedServiceProvider(services => services.AddLogging());
            var functionProvider = mocker.CreateFunctionContextInstanceServices(services => services.AddOptions());
            var context = mocker.GetOrCreateMock<FunctionContext>();

            mocker.AddServiceProvider(globalProvider, replace: true);

            context.AddFunctionContextInstanceServices(functionProvider);

            context.Instance.InstanceServices.Should().BeSameAs(functionProvider);
            mocker.GetObject<IServiceProvider>().Should().BeSameAs(globalProvider);
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

        [Fact]
        public void CreateHttpRequestData_ShouldConfigureKnownTypeFunctionContextInstanceServices()
        {
            var mocker = new Mocker();
            var expectedUri = new Uri("https://known-context.fastmoq/");
            var provider = mocker.CreateTypedServiceProvider(services => services.AddSingleton(expectedUri));
            var functionContext = new TestFunctionContext();

            mocker.AddServiceProvider(provider, replace: true);
            mocker.AddKnownType<FunctionContext>(
                directInstanceFactory: (_, _) => functionContext,
                replace: true);

            var request = mocker.CreateHttpRequestData();

            request.FunctionContext.Should().BeSameAs(functionContext);
            request.FunctionContext.InstanceServices.Should().BeSameAs(provider);
            request.FunctionContext.InstanceServices.GetService(typeof(Uri)).Should().BeSameAs(expectedUri);
        }

        [Fact]
        public void CreateHttpRequestData_ShouldConfigureTypeRegisteredFunctionContextInstanceServices()
        {
            var mocker = new Mocker();
            var expectedUri = new Uri("https://type-context.fastmoq/");
            var provider = mocker.CreateTypedServiceProvider(services => services.AddSingleton(expectedUri));
            var functionContext = new TestFunctionContext();

            mocker.AddServiceProvider(provider, replace: true);
            mocker.AddType<FunctionContext>(functionContext, replace: true);

            var request = mocker.CreateHttpRequestData();

            request.FunctionContext.Should().BeSameAs(functionContext);
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

        private sealed class TestFunctionContext : FunctionContext
        {
            private IServiceProvider? _instanceServices;

            public override string InvocationId { get; } = Guid.NewGuid().ToString("N");

            public override string FunctionId { get; } = "test-function";

            public override TraceContext TraceContext { get; } = new TestTraceContext();

            public override BindingContext BindingContext { get; } = new TestBindingContext();

            public override RetryContext RetryContext { get; } = new TestRetryContext();

            public override IServiceProvider InstanceServices
            {
                get => _instanceServices!;
                set => _instanceServices = value;
            }

            public override FunctionDefinition FunctionDefinition { get; } = new TestFunctionDefinition();

            public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

            public override IInvocationFeatures Features { get; } = new TestInvocationFeatures();

            public override CancellationToken CancellationToken { get; } = CancellationToken.None;
        }

        private sealed class TestTraceContext : TraceContext
        {
            public override string TraceParent { get; } = string.Empty;

            public override string TraceState { get; } = string.Empty;
        }

        private sealed class TestBindingContext : BindingContext
        {
            public override IReadOnlyDictionary<string, object?> BindingData { get; } = new Dictionary<string, object?>();
        }

        private sealed class TestRetryContext : RetryContext
        {
            public override int RetryCount { get; } = 0;

            public override int MaxRetryCount { get; } = 0;
        }

        private sealed class TestFunctionDefinition : FunctionDefinition
        {
            public override ImmutableArray<FunctionParameter> Parameters { get; } = ImmutableArray<FunctionParameter>.Empty;

            public override string PathToAssembly { get; } = string.Empty;

            public override string EntryPoint { get; } = string.Empty;

            public override string Id { get; } = "test-definition";

            public override string Name { get; } = "TestFunction";

            public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; } = ImmutableDictionary<string, BindingMetadata>.Empty;

            public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; } = ImmutableDictionary<string, BindingMetadata>.Empty;
        }

        private sealed class TestInvocationFeatures : IInvocationFeatures
        {
            private readonly Dictionary<Type, object> _features = new Dictionary<Type, object>();

            public T Get<T>()
            {
                return _features.TryGetValue(typeof(T), out var feature)
                    ? (T) feature!
                    : default!;
            }

            public void Set<T>(T instance)
            {
                _features[typeof(T)] = instance!;
            }

            public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
            {
                return _features.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class ScopedProbe
        {
            public Guid Id { get; } = Guid.NewGuid();
        }

        private sealed class DisposableProbe : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        private sealed class SampleOptions
        {
            public string? Name { get; set; }

            public int RetryCount { get; set; }
        }
    }
}
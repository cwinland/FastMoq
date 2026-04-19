using FastMoq.Extensions;
using FastMoq.Providers.MoqProvider;
using FastMoq.Tests.TestClasses;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS8604 // Possible null reference argument for parameter.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'.
#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable.
#pragma warning disable CS8974 // Converting method group to non-delegate type
#pragma warning disable CS0472 // The result of the expression is always 'value1' since a value of type 'value2' is never equal to 'null' of type 'value3'.

namespace FastMoq.Tests
{
    public class HttpTests : MockerTestBase<HttpTestClass>
    {
        [Fact]
        public void Component_ShouldInjectBuiltInHttpClientAndUri_WhenNoExplicitRegistrationExists()
        {
            Component.Should().NotBeNull();
            Component.http.Should().NotBeNull();
            Component.http.Should().BeSameAs(Mocks.HttpClient);
            Component.uri.Should().BeSameAs(Mocks.Uri);
        }

        [Fact]
        public void GetObject_Uri_ShouldPreferExplicitRegistration_WhenProvided()
        {
            Mocks.AddType<Uri, Uri>(_ => new Uri("http://localhost"));
            Mocks.GetObject<Uri>().ToString().Should().Be("http://localhost/");

            // Adding same type will throw an error.
            new Action(() => Mocks.AddType<Uri, Uri>(_ => new Uri("http://localhost2/test"))).Should().Throw<ArgumentException>();

            // Adding same type with replace = true, will replace.
            Mocks.AddType<Uri, Uri>(_ => new Uri("http://localhost2/test/"), true);
            Mocks.GetObject<Uri>().ToString().Should().Be("http://localhost2/test/");
        }

        [Fact]
        public void GetObject_HttpClient_ShouldPreferExplicitRegistration_OverBuiltIn()
        {
            using var expected = new HttpClient
            {
                BaseAddress = new Uri("http://custom.fastmoq/")
            };

            Mocks.AddType(expected, replace: true);

            Mocks.GetObject<HttpClient>().Should().BeSameAs(expected);
        }

        [Fact]
        public void GetObject_HttpClient_ShouldPreferTrackedMock_OverBuiltIn()
        {
            var trackedMock = Mocks.GetOrCreateMock<HttpClient>();
            trackedMock.Instance.BaseAddress = new Uri("http://tracked.fastmoq/");

            var httpClient = Mocks.GetObject<HttpClient>();

            httpClient.Should().BeSameAs(trackedMock.Instance);
        }

        [Fact]
        public void GetObject_HttpClient_ShouldKeepBuiltIn_WhenOnlyFailOnUnconfiguredIsEnabled()
        {
            Mocks.Behavior.Enabled |= MockFeatures.FailOnUnconfigured;

            var httpClient = Mocks.GetObject<HttpClient>();

            httpClient.Should().BeSameAs(Mocks.HttpClient);
        }

        [Fact]
        public void GetObject_HttpClient_ShouldNotUseBuiltIn_WhenStrictCompatibilityDefaultsAreEnabled()
        {
            Mocks.Behavior.Enabled |= MockFeatures.FailOnUnconfigured;
            Mocks.Policy.EnabledBuiltInTypeResolutions = BuiltInTypeResolutionFlags.StrictCompatibilityDefaults;

            var httpClient = Mocks.GetObject<HttpClient>();

            httpClient.Should().NotBeNull();
            httpClient.Should().NotBeSameAs(Mocks.HttpClient);
            httpClient!.BaseAddress.Should().BeNull();
        }

        [Fact]
        public void GetObject_HttpClient_ShouldAllowExplicitBuiltInOverride_WhenStrictCompatibilityDefaultsAreEnabled()
        {
            Mocks.Behavior.Enabled |= MockFeatures.FailOnUnconfigured;
            Mocks.Policy.EnabledBuiltInTypeResolutions = BuiltInTypeResolutionFlags.StrictCompatibilityDefaults;
            Mocks.Policy.EnabledBuiltInTypeResolutions |= BuiltInTypeResolutionFlags.HttpClient;

            var httpClient = Mocks.GetObject<HttpClient>();

            httpClient.Should().BeSameAs(Mocks.HttpClient);
        }

        [Fact]
        public void GetObject_Uri_ShouldAllowExplicitBuiltInOverride_WhenStrictCompatibilityDefaultsAreEnabled()
        {
            Mocks.Behavior.Enabled |= MockFeatures.FailOnUnconfigured;
            Mocks.Policy.EnabledBuiltInTypeResolutions = BuiltInTypeResolutionFlags.StrictCompatibilityDefaults;
            Mocks.Policy.EnabledBuiltInTypeResolutions |= BuiltInTypeResolutionFlags.Uri;

            var uri = Mocks.GetObject<Uri>();

            uri.Should().BeSameAs(Mocks.Uri);
        }

        [Fact]
        public void GetObject_Uri_ShouldUseResolvedHttpClientBaseAddress_WhenNoExplicitRegistrationExists()
        {
            var uri = Mocks.GetObject<Uri>();
            var uri2 = Mocks.GetObject<Uri>();

            uri.Should().BeSameAs(Mocks.Uri);
            uri2.Should().BeSameAs(uri);
        }

        [Fact]
        public async Task BuiltInHttpClient_ShouldUseMockedHandler_WhenNoExplicitRegistrationExists()
        {
            // Execute Http request.
            var result = await Component.http.GetAsync(new Uri("api/test", UriKind.Relative));

            // Test Results
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await Mocks.GetStringContent(result.Content);
            content.Should().Be("[{'id':1}]");
        }

        [Fact]
        public async Task WhenHttpRequestJson_ShouldOverrideBuiltInHandlerResponse()
        {
            Mocks.WhenHttpRequestJson(HttpMethod.Get, "/api/test", "[{'id':55, 'value':'33'}]", HttpStatusCode.Accepted);

            // Execute Http request.
            var result = await Component.http.GetAsync(new Uri("api/test", UriKind.Relative));

            // Test Results
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.Accepted);

            var content = await Mocks.GetStringContent(result.Content);
            content.Should().Be("[{'id':55, 'value':'33'}]");
        }

        [Fact]
        public async Task SetupHttpMessage_ShouldRemainAvailable_ForAdvancedMoqCompatibility()
        {
            Mocks.SetupHttpMessage(
                () => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Accepted,
                    Content = new StringContent("[{'id':99}]"),
                },
                ItExpr.Is<HttpRequestMessage>(request => request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/compat")
            );

            using var client = Mocks.CreateHttpClient();
            client.BaseAddress = new Uri("http://compat.fastmoq/");

            var matched = await client.GetAsync(new Uri("api/compat", UriKind.Relative));

            matched.StatusCode.Should().Be(HttpStatusCode.Accepted);
            (await Mocks.GetStringContent(matched.Content)).Should().Be("[{'id':99}]");
        }

        [Fact]
        public async Task WhenHttpRequest_ShouldMatchMethodAndPath_AndFallbackToDefaultResponse()
        {
            Mocks.WhenHttpRequest(HttpMethod.Get, "/api/orders/42", () =>
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Accepted,
                    Content = new StringContent("[{'id':42}]"),
                });

            var matched = await Component.http.GetAsync(new Uri("api/orders/42", UriKind.Relative));
            var unmatched = await Component.http.GetAsync(new Uri("api/orders/7", UriKind.Relative));

            matched.StatusCode.Should().Be(HttpStatusCode.Accepted);
            (await Mocks.GetStringContent(matched.Content)).Should().Be("[{'id':42}]");

            unmatched.StatusCode.Should().Be(HttpStatusCode.OK);
            (await Mocks.GetStringContent(unmatched.Content)).Should().Be("[{'id':1}]");
        }

        [Fact]
        public async Task CreateHttpClient_ShouldUpdateBuiltInHttpClientFactoryConfiguration()
        {
            using var client = Mocks.CreateHttpClient(
                clientName: "WeatherApiClient",
                baseAddress: "https://api.fastmoq.test/",
                statusCode: HttpStatusCode.Accepted,
                stringContent: "{\"temperature\":25}");

            client.BaseAddress.Should().Be(new Uri("https://api.fastmoq.test/"));

            var factory = Mocks.GetObject<IHttpClientFactory>();
            using var factoryClient = factory!.CreateClient("any-name");

            factoryClient.BaseAddress.Should().Be(new Uri("https://api.fastmoq.test/"));

            var response = await factoryClient.GetAsync("weather");

            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            (await Mocks.GetStringContent(response.Content)).Should().Be("{\"temperature\":25}");
        }

        [Fact]
        public void GetObject_IHttpClientFactory_ShouldPreferTrackedMock_OverBuiltInCompatibilityFactory()
        {
            using var expected = new HttpClient
            {
                BaseAddress = new Uri("http://tracked-factory.fastmoq/")
            };

            var factory = Mocks.GetOrCreateMock<IHttpClientFactory>();
            factory.AsMoq().Setup(x => x.CreateClient("tracked")).Returns(expected);

            var resolvedFactory = Mocks.GetObject<IHttpClientFactory>();
            using var client = Mocks.CreateHttpClient(clientName: "tracked");

            resolvedFactory.Should().NotBeNull();
            client.Should().BeSameAs(expected);
            resolvedFactory!.CreateClient("tracked").Should().BeSameAs(expected);
        }

        [Fact]
        public async Task GetOrCreateMock_KeyedIHttpClientFactory_ShouldNotReplaceUnkeyedBuiltInCompatibilityFactory()
        {
            _ = Mocks.CreateHttpClient(
                clientName: "WeatherApiClient",
                baseAddress: "https://keyed.fastmoq.test/",
                statusCode: HttpStatusCode.Accepted,
                stringContent: "{\"temperature\":31}");

            var keyedFactory = Mocks.GetOrCreateMock<IHttpClientFactory>(new MockRequestOptions
            {
                ServiceKey = "keyed"
            });

            var resolvedFactory = Mocks.GetObject<IHttpClientFactory>();
            using var builtInClient = resolvedFactory!.CreateClient("any-name");
            var response = await builtInClient.GetAsync("weather");

            resolvedFactory.Should().NotBeNull();
            resolvedFactory.Should().NotBeSameAs(keyedFactory.Instance);
            builtInClient.BaseAddress.Should().Be(new Uri("https://keyed.fastmoq.test/"));
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            (await Mocks.GetStringContent(response.Content)).Should().Be("{\"temperature\":31}");
        }

        [Fact]
        public async Task WhenHttpRequestJson_ShouldReturnJsonContent_WithApplicationJsonMediaType()
        {
            Mocks.WhenHttpRequestJson(HttpMethod.Post, "/api/orders", "{\"status\":\"created\"}", HttpStatusCode.Created);

            var result = await Component.http.PostAsync(new Uri("api/orders", UriKind.Relative), new StringContent("{}"));

            result.StatusCode.Should().Be(HttpStatusCode.Created);
            result.Content.Headers.ContentType.Should().NotBeNull();
            result.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            (await Mocks.GetStringContent(result.Content)).Should().Be("{\"status\":\"created\"}");
        }

        [Fact]
        public async Task TrackedHttpMessageHandlerMock_ShouldDriveHttpClientRequests()
        {
            // Add mocks / setup
            var handler = Mocks.GetOrCreateMock<HttpMessageHandler>();

            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("[{'id':2, 'value':'2'}]"),
                    }
                ).Verifiable();

            using var client = Mocks.CreateHttpClient();
            client.BaseAddress = new Uri("http://help.fastmoq.com/");

            // Execute Http request.
            var result = await client.GetAsync(new Uri("api/test2", UriKind.Relative));

            // Test Results
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await Mocks.GetStringContent(result.Content);
            content.Should().Be("[{'id':2, 'value':'2'}]");

            handler.Protected().Verify("SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == new Uri("http://help.fastmoq.com/api/test2")),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}

using FastMoq.Tests.TestClasses;
using FluentAssertions;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FastMoq.Tests
{
    public class HttpTests : MockerTestBase<HttpTestClass>
    {
        [Fact]
        public void Create()
        {
            // Check Component
            Component.Should().NotBeNull();
            Component.http.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateWithBuiltInHttpClient()
        {
            // Execute Http request.
            var result = await Component.http.GetAsync(new Uri("api/test", UriKind.Relative));

            // Test Results
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await Mocks.GetStringContent(result.Content);
            content.Should().Be("[{'id':1, 'value':'1'}]");

            var handler = Mocks.GetMock<HttpMessageHandler>();
            handler.Protected().Verify("SendAsync", Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == new Uri("http://localhost/api/test")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateWithMocks()
        {
            // Add mocks / setup
            var handler = Mocks.GetMock<HttpMessageHandler>();
            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("[{'id':2, 'value':'2'}]")
                }).Verifiable();

            Component.http.BaseAddress = new Uri("http://help.fastmoq.com/");

            // Execute Http request.
            var result = await Component.http.GetAsync(new Uri("api/test2", UriKind.Relative));

            // Test Results
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await Mocks.GetStringContent(result.Content);
            content.Should().Be("[{'id':2, 'value':'2'}]");

            handler.Protected().Verify("SendAsync", Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == new Uri("http://help.fastmoq.com/api/test2")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateWithCustomHttpClient()
        {
            Mocks.SetupHttpMessage(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Accepted,
                    Content = new StringContent("[{'id':55, 'value':'33'}]")
                }
            );

            // Execute Http request.
            var result = await Component.http.GetAsync(new Uri("api/test", UriKind.Relative));

            // Test Results
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.Accepted);

            var content = await Mocks.GetStringContent(result.Content);
            content.Should().Be("[{'id':55, 'value':'33'}]");

            var handler = Mocks.GetMock<HttpMessageHandler>();
            handler.Protected().Verify("SendAsync", Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == new Uri("http://localhost/api/test")),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}
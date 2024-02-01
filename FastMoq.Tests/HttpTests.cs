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
        public void Create()
        {
            // Check Component
            Component.Should().NotBeNull();
            Component.http.Should().NotBeNull();
        }

        [Fact]
        public void CreateUri()
        {
            Mocks.AddType<Uri, Uri>(_ => new Uri("http://localhost"));
            var m = Mocks.GetObject<Uri>().ToString().Should().Be("http://localhost/");

            // Adding same type will throw an error.
            new Action(() => Mocks.AddType<Uri, Uri>(_ => new Uri("http://localhost2/test"))).Should().Throw<ArgumentException>();

            // Adding same type with replace = true, will replace.
            Mocks.AddType<Uri, Uri>(_ => new Uri("http://localhost2/test/"), true);
            Mocks.GetObject<Uri>().ToString().Should().Be("http://localhost2/test/");

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
            content.Should().Be("[{'id':1}]");

            Mock<HttpMessageHandler> handler = Mocks.GetMock<HttpMessageHandler>();

            handler.Protected().Verify("SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == new Uri("http://localhost/api/test")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task CreateWithCustomHttpClient()
        {
            Mocks.SetupHttpMessage(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Accepted,
                    Content = new StringContent("[{'id':55, 'value':'33'}]"),
                }
            );

            // Execute Http request.
            var result = await Component.http.GetAsync(new Uri("api/test", UriKind.Relative));

            // Test Results
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.Accepted);

            var content = await Mocks.GetStringContent(result.Content);
            content.Should().Be("[{'id':55, 'value':'33'}]");

            Mock<HttpMessageHandler> handler = Mocks.GetMock<HttpMessageHandler>();

            handler.Protected().Verify("SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == new Uri("http://localhost/api/test")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task CreateWithMocks()
        {
            // Add mocks / setup
            Mock<HttpMessageHandler> handler = Mocks.GetMock<HttpMessageHandler>();

            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("[{'id':2, 'value':'2'}]"),
                    }
                ).Verifiable();

            Component.http.BaseAddress = new Uri("http://help.fastmoq.com/");

            // Execute Http request.
            var result = await Component.http.GetAsync(new Uri("api/test2", UriKind.Relative));

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

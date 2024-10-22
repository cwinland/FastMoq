using FastMoq.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace FastMoq.Tests
{
    public class MockerCreationExtensionsTests
    {
        private Mocker Mocks { get; } = new Mocker();

        [Fact]
        public void CreateMock_IActionContextAccessor_ShouldCreateMock()
        {
            var o = Mocks.CreateMockInternal<IActionContextAccessor>();
            o.Should().NotBeNull();
            o.Should().BeOfType<Mock<IActionContextAccessor>>();
            o.Object.Should().NotBeNull();

            var p = Mocks.CreateMockInternal<IActionInvokerFactory>();
            p.Should().NotBeNull();
            p.Should().BeOfType<Mock<IActionInvokerFactory>>();
            p.Object.Should().NotBeNull();

            var q = Mocks.CreateMockInternal<IFormFile>();
            q.Should().NotBeNull();
            q.Should().BeOfType<Mock<IFormFile>>();
            q.Object.Should().NotBeNull();

            var r = Mocks.CreateMockInternal<HttpContext>();
            r.Should().NotBeNull();
            r.Should().BeOfType<Mock<HttpContext>>();
            r.Object.Should().NotBeNull();
            var rObj = r.Object;
            rObj.Session.Should().NotBeNull();
            rObj.Items.Should().NotBeNull();
            rObj.User.Should().NotBeNull();
            rObj.Response.Should().BeNull();
        }

        [Fact]
        public void SetupMockPropertyByPropertyInfo()
        {
            var mock = Mocks.GetMock<IFormFile>();
            mock.SetupMockProperty(typeof(IFormFile).GetProperty("Headers"), new HeaderDictionary());
        }

        [Fact]
        public void SetupMockPropertyByName()
        {
            var mock = Mocks.GetMock<IFormFile>();
            mock.SetupMockProperty("Headers", new HeaderDictionary());
        }

        [Fact]
        public void SetupMockPropertyByExpression()
        {
            var mock = Mocks.GetMock<IFormFile>();
            mock.SetupMockProperty(x=>x.Headers, new HeaderDictionary());
        }
    }
}

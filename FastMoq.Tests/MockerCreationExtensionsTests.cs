using FastMoq.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

#pragma warning disable CS8604 // Possible null reference argument for parameter.

namespace FastMoq.Tests
{
    public class MockerCreationExtensionsTests
    {
        private Mocker Mocks { get; } = new Mocker();

        [Fact]
        public void CreateMockInternal_ShouldCreateSupportedFrameworkMocks()
        {
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
        public void CreateMockInternal_ShouldUseDefaultStrictMockCreationPolicy()
        {
            Mocks.Policy.DefaultStrictMockCreation = true;

            var mock = Mocks.CreateMockInternal<IFormFile>();

            mock.Behavior.Should().Be(MockBehavior.Strict);
        }

        [Fact]
        public void SetupMockProperty_ShouldAssignValue_WhenUsingPropertyInfo()
        {
            var mock = Mocks.GetMock<IFormFile>();
            mock.SetupMockProperty(typeof(IFormFile).GetProperty("Headers"), new HeaderDictionary());
        }

        [Fact]
        public void SetupMockProperty_ShouldAssignValue_WhenUsingPropertyName()
        {
            var mock = Mocks.GetMock<IFormFile>();
            mock.SetupMockProperty("Headers", new HeaderDictionary());
        }

        [Fact]
        public void SetupMockProperty_ShouldAssignValue_WhenUsingPropertyExpression()
        {
            var mock = Mocks.GetMock<IFormFile>();
            mock.SetupMockProperty(x => x.Headers, new HeaderDictionary());
        }
    }
}

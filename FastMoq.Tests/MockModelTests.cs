using FluentAssertions;
using Moq;
using System;
using System.IO.Abstractions;
using Xunit;

namespace FastMoq.Tests
{
    public class MockModelTests : TestBase<MockModel>
    {
        public MockModelTests() : base(mocks => new MockModel(typeof(IFileSystem), new Mock<IFileSystem>()))
        {
        }

        [Fact]
        public void Create()
        {
            Component.Should().NotBeNull();
            Component.Type.Should().Be(typeof(IFileSystem));
            Component.Mock.Should().BeOfType(typeof(Mock<IFileSystem>));
        }

        [Fact]
        public void CreateNullType()
        {
            Action a = () => new MockModel(null, new Mock<IFileSystem>());
            a.Should().Throw<ArgumentNullException>();

            Action b = () => new MockModel(typeof(IFileSystem), null);
            b.Should().Throw<ArgumentNullException>();
        }
    }
}

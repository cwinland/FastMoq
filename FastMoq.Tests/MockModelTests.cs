using FluentAssertions;
using Moq;
using System;
using System.IO.Abstractions;
using Xunit;

#pragma warning disable CS8602
#pragma warning disable CS8625

namespace FastMoq.Tests
{
    public class MockModelTests : MockerTestBase<MockModel>
    {
        public MockModelTests() : base(mocks => new MockModel(typeof(IFileSystem), new Mock<IFileSystem>())) { }

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
            Action a = () => _ = new MockModel(null, new Mock<IFileSystem>());
            a.Should().Throw<ArgumentNullException>();

            Action b = () => _ = new MockModel(typeof(IFileSystem), null);
            b.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Create2()
        {
            Component = new MockModel<IFileSystem>(new Mock<IFileSystem>());
            Component.Should().NotBeNull();
            Component.Type.Should().Be(typeof(IFileSystem));
            Component.Mock.Should().BeOfType(typeof(Mock<IFileSystem>));
        }

        [Fact]
        public void Create3()
        {
            var mockModel = new MockModel<IFileSystem>(new Mock<IFileSystem>());
            Component = new MockModel<IFileSystem>(mockModel);
            Component.Should().NotBeNull();
            Component.Type.Should().Be(typeof(IFileSystem));
            Component.Mock.Should().BeOfType(typeof(Mock<IFileSystem>));
            mockModel.Mock.Should().BeOfType(typeof(Mock<IFileSystem>));
            var mockModel2 = new MockModel<IFileSystem>(new Mock<IFileSystem>());
            mockModel2.Mock = mockModel.Mock;
            mockModel2.Mock.Should().BeEquivalentTo(mockModel.Mock);
            mockModel2.Should().BeEquivalentTo(mockModel);
        }
    }
}
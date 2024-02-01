using System;
using System.IO.Abstractions;
using FastMoq.Models;

#pragma warning disable CS8604 // Possible null reference argument for parameter.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'.
#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable.
#pragma warning disable CS8974 // Converting method group to non-delegate type
#pragma warning disable CS0472 // The result of the expression is always 'value1' since a value of type 'value2' is never equal to 'null' of type 'value3'.

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
            var mockModel2 = new MockModel<IFileSystem>(new Mock<IFileSystem>())
            {
                Mock = mockModel.Mock,
            };

            mockModel2.Mock.Should().BeEquivalentTo(mockModel.Mock);
            mockModel2.Should().BeEquivalentTo(mockModel);
        }
    }
}
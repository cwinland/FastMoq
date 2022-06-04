using FluentAssertions;
using System;
using System.IO.Abstractions;
using Xunit;
#pragma warning disable CS8602
#pragma warning disable CS8625

namespace FastMoq.Tests
{
    public class InstanceModelTests : MockerTestBase<InstanceModel<IFileSystem>>
    {
        public InstanceModelTests() : base(mocks => new InstanceModel<IFileSystem>(mocks1 => new FileSystem()))
        {
        }

        [Fact]
        public void Create()
        {
            Component.Should().NotBeNull();
            Component.InstanceType.Should().Be(typeof(IFileSystem));
            Component.CreateFunc.Should().NotBeNull();
        }

        [Fact]
        public void CreateNullType()
        {
            Action a = () => new InstanceModel(null) { CreateFunc = mocks1 => new FileSystem()};
            a.Should().Throw<ArgumentNullException>();

            var im = new InstanceModel<IFileSystem>(null);
            im.InstanceType.Should().Be(typeof(IFileSystem));
            im.CreateFunc.Should().BeNull();
        }
    }
}

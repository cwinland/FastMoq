using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using Xunit;

#pragma warning disable CS8602
#pragma warning disable CS8625

namespace FastMoq.Tests
{
    public class InstanceModelTests : MockerTestBase<InstanceModel<IFileSystem>>
    {
        public InstanceModelTests() : base(_ => new InstanceModel<IFileSystem>(_ => new FileSystem())) { }

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
            Action a = () => _ = new InstanceModel(null) { CreateFunc = _ => new FileSystem() };
            a.Should().Throw<ArgumentNullException>();

            var im = new InstanceModel<IFileSystem>(null);
            im.InstanceType.Should().Be(typeof(IFileSystem));
            im.CreateFunc.Should().BeNull();
        }

        [Fact]
        public void CreateInstance()
        {
            var obj = new InstanceModel(typeof(IFileSystem), mocker => new FileSystem(), new List<object>());
            obj.Should().NotBeNull();
            obj.CreateFunc.Should().NotBeNull();
            obj.Arguments.Should().HaveCount(0);

            new Action(() => new InstanceModel(typeof(IFileSystem), mocker => new FileSystem(), null)).Should().Throw<ArgumentNullException>();
        }
    }
}
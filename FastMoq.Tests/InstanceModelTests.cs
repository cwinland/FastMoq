using System;
using System.Collections.Generic;
using System.IO.Abstractions;

#pragma warning disable CS8604 // Possible null reference argument for parameter.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'.
#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable.
#pragma warning disable CS8974 // Converting method group to non-delegate type
#pragma warning disable CS0472 // The result of the expression is always 'value1' since a value of type 'value2' is never equal to 'null' of type 'value3'.

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
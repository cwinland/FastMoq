using FluentAssertions;
using Moq;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

#pragma warning disable CS8604
#pragma warning disable CS8602

namespace FastMoq.TestingExample
{
    public class TestClassNormalTestsDefaultBase : MockerTestBase<TestClassNormal>
    {
        [Fact]
        public void Test1()
        {
            Component.FileSystem.Should().NotBeNull();
            Component.FileSystem.Should().BeOfType<MockFileSystem>();
            Component.FileSystem.File.Should().NotBeNull();
            Component.FileSystem.Directory.Should().NotBeNull();
        }
    }

    public class TestClassNormalTestsSetupBase : MockerTestBase<TestClassNormal>
    {
        public TestClassNormalTestsSetupBase() : base(SetupMocks) { }

        [Fact]
        public void TestStrict()
        {
            Component.FileSystem.Should().NotBeNull();
            Component.FileSystem.Should().NotBeOfType<MockFileSystem>();
            Component.FileSystem.File.Should().NotBeNull();
            Component.FileSystem.Directory.Should().BeNull();
        }

        private static void SetupMocks(Mocker mocks)
        {
            var iFile = new FileSystem().File;
            mocks.Strict = true; // Indicates to use an empty mock instead of predefined IFileSystem (FileSystemMock).
            mocks.Initialize<IFileSystem>(mock => mock.Setup(x => x.File).Returns(iFile));
        }
    }

    public class TestClassNormalTestsFull : MockerTestBase<TestClassNormal>
    {
        #region Fields

        private static bool testEventCalled;

        #endregion

        public TestClassNormalTestsFull() : base(SetupMocks, CreateComponent, CreatedComponent) =>
            testEventCalled = false;

        [Fact]
        public void Test1()
        {
            Component.FileSystem.Should().Be(Mocks.GetMock<IFileSystem>().Object);
            Component.FileSystem.Should().NotBeNull();
            Component.FileSystem.File.Should().NotBeNull();
            Component.FileSystem.Directory.Should().BeNull();
            testEventCalled.Should().BeFalse();
            Component.CallTestEvent();
            testEventCalled.Should().BeTrue();

            Mocks.Initialize<IFileSystem>(mock => mock.Setup(x => x.Directory).Returns(new FileSystem().Directory));
            Component.FileSystem.Directory.Should().NotBeNull();
        }

        private static TestClassNormal CreateComponent(Mocker mocks) => new(mocks.GetObject<IFileSystem>());
        private static void CreatedComponent(TestClassNormal? obj) => obj.TestEvent += (_, _) => testEventCalled = true;

        private static void SetupMocks(Mocker mocks)
        {
            var mock = new Mock<IFileSystem>();
            var iFile = new FileSystem().File;
            mocks.Strict = true; // Indicates to use an empty mock instead of predefined IFileSystem (FileSystemMock).
            mocks.AddMock(mock, true);
            mocks.Initialize<IFileSystem>(xMock => xMock.Setup(x => x.File).Returns(iFile));
        }
    }
}
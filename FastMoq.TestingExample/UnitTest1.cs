using FluentAssertions;
using Moq;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace FastMoq.TestingExample
{
    public class TestClassNormalTestsDefaultBase : TestBase<TestClassNormal>
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

    public class TestClassNormalTestsSetupBase : TestBase<TestClassNormal>
    {
        public TestClassNormalTestsSetupBase() : base(SetupMocksAction)
        {

        }

        private static void SetupMocksAction(Mocks mocks)
        {
            var iFile = new FileSystem().File;
            mocks.Strict = true;

            mocks.Initialize<IFileSystem>(mock =>
                {
                    mock.SetupAllProperties();
                    mock.Setup(x => x.File).Returns(iFile);
                }
            );
        }

        [Fact]
        public void Test1()
        {
            Component.FileSystem.Should().NotBeNull();
            Component.FileSystem.Should().NotBeOfType<MockFileSystem>();
            Component.FileSystem.File.Should().NotBeNull();
            Component.FileSystem.Directory.Should().BeNull();
        }
    }

    public class TestClassNormalTestsFull : TestBase<TestClassNormal>
    {
        private static Mock<IFileSystem> mock = new Mock<IFileSystem>();
        private static bool testEventCalled;

        public TestClassNormalTestsFull() : base(SetupMocksAction, CreateComponentAction, CreatedComponentAction)
        {
            testEventCalled = false;
        }

        private static void CreatedComponentAction(TestClassNormal? obj)
        {
            obj.TestEvent += (sender, args) => testEventCalled = true;
        }

        private static TestClassNormal? CreateComponentAction()
        {
            return new TestClassNormal(mock.Object);
        }

        private static void SetupMocksAction(Mocks mocks)
        {
            var iFile = new FileSystem().File;
            mocks.Strict = true;
            mocks.AddMock(mock, true);

            mocks.Initialize<IFileSystem>(mock =>
                {
                    mock.SetupAllProperties();
                    mock.Setup(x => x.File).Returns(iFile);
                }
            );
        }

        [Fact]
        public void Test1()
        {
            Component.FileSystem.Should().Be(mock.Object);
            Component.FileSystem.Should().NotBeNull();
            Component.FileSystem.File.Should().NotBeNull();
            Component.FileSystem.Directory.Should().BeNull();
            testEventCalled.Should().BeFalse();
            Component.CallTestEvent();
            testEventCalled.Should().BeTrue();

            Mocks.Initialize<IFileSystem>(mock1 => mock.Setup(x => x.Directory).Returns(new FileSystem().Directory));
            Component.FileSystem.Directory.Should().NotBeNull();

        }
    }
}
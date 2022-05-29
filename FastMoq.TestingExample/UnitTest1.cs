using FluentAssertions;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Xunit;
#pragma warning disable CS8604
#pragma warning disable CS8602

namespace FastMoq.TestingExample
{
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
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

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
    public class TestClassNormalTestsSetupBase : TestBase<TestClassNormal>
    {
        public TestClassNormalTestsSetupBase() : base(SetupMocksAction) { }

        private static void SetupMocksAction(Mocks mocks)
        {
            var iFile = new FileSystem().File;
            mocks.Strict = true;

            mocks.Initialize<IFileSystem>(mock => mock.Setup(x => x.File).Returns(iFile));
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

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
    public class TestClassNormalTestsFull : TestBase<TestClassNormal>
    {
        private static bool testEventCalled;
        public TestClassNormalTestsFull() : base(SetupMocksAction, CreateComponentAction, CreatedComponentAction) => testEventCalled = false;
        private static void CreatedComponentAction(TestClassNormal? obj) => obj.TestEvent += (_, _) => testEventCalled = true;
        private static TestClassNormal CreateComponentAction(Mocks mocks) => new(mocks.GetObject<IFileSystem>());

        private static void SetupMocksAction(Mocks mocks)
        {
            var mock = new Mock<IFileSystem>();
            var iFile = new FileSystem().File;
            mocks.Strict = true;
            mocks.AddMock(mock, true);
            mocks.Initialize<IFileSystem>(xMock => xMock.Setup(x => x.File).Returns(iFile));
        }

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
    }
}
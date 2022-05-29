using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography;
using Xunit;

#pragma warning disable CS8602
#pragma warning disable CS8625

namespace FastMoq.Tests
{
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
    public class MocksTests : TestBase<Mocks>
    {
        public MocksTests() : base(SetupAction, CreateAction, CreatedAction)
        {
        }

        [Fact]
        public void Contains_ShouldWork()
        {
            Mocks.Contains<IDirectory>().Should().BeTrue();
            Mocks.Contains<IFileInfo>().Should().BeTrue();
            Mocks.Contains<IFile>().Should().BeFalse();

            Mocks.Contains(typeof(IDirectory)).Should().BeTrue();
            Mocks.Contains(typeof(IFileInfo)).Should().BeTrue();
            Mocks.Contains(typeof(IFile)).Should().BeFalse();
        }

        [Fact]
        public void CreateMock_ShouldWork()
        {
            Mocks.CreateMock<IDirectoryInfo>().Should().NotBeNull();
            Mocks.Contains<IDirectoryInfo>().Should().BeTrue();
        }

        [Fact]
        public void CreateMock_ValueType_ShouldFail()
        {
            Action a = () => Mocks.CreateMock(typeof(int));
            a.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateMock_ShouldWorkAsParam()
        {
            Mocks.CreateMock(typeof(IDirectoryInfo)).Should().NotBeNull();
            Mocks.Contains(typeof(IDirectoryInfo)).Should().BeTrue();
        }

        [Fact]
        public void CreateMockDuplicate_ShouldThrowArgumentException()
        {
            Mocks.CreateMock<IDirectoryInfo>();

            Action a = () => Mocks.CreateMock(typeof(IDirectoryInfo));
            a.Should().Throw<ArgumentException>();

            Action b = () => Mocks.CreateMock<IDirectoryInfo>();
            b.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateExact_WithMultiClass()
        {
            Mocks.CreateInstance<TestClassMany>(args: new object[] { 4 }).Should().NotBeNull();
            Mocks.CreateInstance<TestClassMany>(args: new object[] { "str" }).Should().NotBeNull();
            Mocks.CreateInstance<TestClassMany>(args: new object[] {4, "str"}).Should().NotBeNull();
            Action a = () => Mocks.CreateInstance<TestClassMany>(args: new object[] {"4", "str"}).Should().NotBeNull();
            a.Should().Throw<NotImplementedException>();
        }

        [Fact]
        public void CreateBest() => Mocks.CreateInstance<TestClassNormal>().Should().NotBeNull();

        [Fact]
        public void CreateBest_Should_ThrowAmbiguous()
        {
            Action a = () => Mocks.CreateInstance<TestClassMany>();
            a.Should().Throw<AmbiguousImplementationException>();
        }

        [Fact]
        public void CreateFromInterface_ManyMatches_ShouldThrow()
        {
            Action a = () => Mocks.CreateInstance<IFile>().Should().NotBeNull();
            a.Should().Throw<AmbiguousImplementationException>();
        }

        [Fact]
        public void CreateFromInterface_BestGuess() => Mocks.CreateInstance<ITestClassNormal>().Should().NotBeNull();

        [Fact]
        public void FileSystem_ShouldBeValid()
        {
            Mocks.fileSystem.Should().NotBeNull();
            Mocks.fileSystem.Should().BeOfType(typeof(MockFileSystem));
            Mocks.fileSystem.File.Should().NotBeNull();
            Mocks.fileSystem.Directory.Should().NotBeNull();
            Mocks.fileSystem.FileInfo.Should().NotBeNull();
            Mocks.fileSystem.DirectoryInfo.Should().NotBeNull();
            Mocks.fileSystem.Path.Should().NotBeNull();
            Mocks.fileSystem.DriveInfo.Should().NotBeNull();
            Mocks.fileSystem.FileStream.Should().NotBeNull();
            Mocks.fileSystem.FileSystem.Should().NotBeNull();
            Mocks.fileSystem.FileSystem.GetType().IsAssignableTo(typeof(IFileSystem)).Should().BeTrue();
            Mocks.GetObject<IFileSystem>().Should().Be(Mocks.fileSystem.FileSystem);
            Mocks.Strict = true;
            Mocks.GetObject<IFileSystem>().Should().NotBe(Mocks.fileSystem.FileSystem);
        }

        [Fact]
        public void GetRequiredMock()
        {
            Mocks.GetRequiredMock<IDirectory>().Should().NotBeNull();
            Mocks.GetRequiredMock<IFileInfo>().Should().NotBeNull();
            Action a = () => Mocks.GetRequiredMock<IFile>();
            a.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetRequiredMockByTypeVariable()
        {
            Mocks.GetRequiredMock(typeof(IDirectory)).Should().NotBeNull();
            Mocks.GetRequiredMock(typeof(IFileInfo)).Should().NotBeNull();
            Action a = () => Mocks.GetRequiredMock(typeof(IFile));
            a.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetTypeFromInterfaceWithNonInterface()
        {
            var type = Mocks.GetTypeFromInterface<TestClassNormal>();
            type.InstanceType.Should().Be<TestClassNormal>();
        }

        [Fact]
        public void GetRequiredMock_Null()
        {
            Action a = () => Mocks.GetRequiredMock(null);
            a.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetRequiredMock_ValueType()
        {
            Action a = () => Mocks.GetRequiredMock(typeof(int));
            a.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetMock()
        {
            Mocks.Contains<IFileInfo>().Should().BeTrue();
            Mocks.Contains<IDirectoryInfo>().Should().BeFalse();
            Mocks.GetMock(typeof(IFileInfo)).Should().BeOfType<Mock<IFileInfo>>();
            Mocks.GetMock<IFileInfo>().Should().BeOfType<Mock<IFileInfo>>();
            Mocks.GetMock(typeof(IDirectoryInfo)).Should().BeOfType<Mock<IDirectoryInfo>>();
            Mocks.GetMock<IDirectoryInfo>().Should().BeOfType<Mock<IDirectoryInfo>>();
        }

        [Fact]
        public void GetObject()
        {
            var a = Mocks.GetMock<IFileInfo>().Object;
            a.Should().Be(Mocks.GetObject<IFileInfo>());
            Mocks.GetMock<IFileInfo>().CallBase.Should().BeFalse();

            var b = Mocks.GetObject<IDirectoryInfo>();
            b.Should().Be(Mocks.GetMock<IDirectoryInfo>().Object);
            Mocks.GetMock<IDirectoryInfo>().CallBase.Should().BeFalse();

            var c = Mocks.GetObject<ITestClassNormal>();
            c.Should().Be(Mocks.GetMock<ITestClassNormal>().Object);
            Mocks.GetMock<ITestClassNormal>().CallBase.Should().BeFalse();

            var d = Mocks.GetObject<TestClassNormal>();
            d.Should().Be(Mocks.GetMock<TestClassNormal>().Object);
            Mocks.GetMock<TestClassNormal>().CallBase.Should().BeTrue();

            var e = Mocks.GetObject<ITestClassMany>();
            e.Should().Be(Mocks.GetMock<ITestClassMany>().Object);
            Mocks.GetMock<ITestClassMany>().CallBase.Should().BeFalse();

            var f = Mocks.GetObject<TestClassMany>();
            f.Should().Be(Mocks.GetMock<TestClassMany>().Object);
            Mocks.GetMock<TestClassMany>().CallBase.Should().BeTrue();

        }

        [Fact]
        public void GetList()
        {
            var count = 0;
            var numbers = Mocks.GetList(3, () => count++);
            numbers.Should().BeEquivalentTo(new List<int> {0, 1, 2});

            count = 0;
            var strings = Mocks.GetList(3, () => (count++).ToString());
            strings.Should().BeEquivalentTo(new List<string> { "0", "1", "2"});

            count = 0;
            var test = Mocks.GetList(3, () => new TestClassMany(count++));
            test[0].value.Should().Be(0);
            test[1].value.Should().Be(1);
            test[2].value.Should().Be(2);
        }

        [Fact]
        public void RemoveMock()
        {
            var mock = new Mock<IFileSystemInfo>();
            Mocks.AddMock(mock, false);
            Mocks.Contains<IFileSystemInfo>().Should().BeTrue();
            Mocks.RemoveMock(mock).Should().BeTrue();
            Mocks.Contains<IFileSystemInfo>().Should().BeFalse();
            Mocks.RemoveMock(mock).Should().BeFalse();

        }

        [Fact]
        public void AddMock()
        {
            var mock = new Mock<IFileSystemInfo>
            {
                Name = "First"
            };

            var mockResult = Mocks.AddMock(mock, false);
            mockResult.Should().Be(mock);
            mockResult.Name.Should().Be("First");
        }

        [Fact]
        public void AddMockDuplicateWithoutOverwrite_ShouldThrowArgumentException()
        {
            var mock = new Mock<IFileSystemInfo>
            {
                Name = "First"
            };

            Mocks.AddMock(mock, false).Should().Be(mock);

            Action a = () => Mocks.AddMock(mock, false);
            a.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddMockDuplicateWithOverwrite_ShouldSucceed()
        {
            var mock = new Mock<IFileSystemInfo>
            {
                Name = "First"
            };

            var mock1 = Mocks.AddMock(mock, false);
            mock1.Should().Be(mock);
            mock1.Name.Should().Be("First");

            mock.Name = "test";
            var mock2 = Mocks.AddMock(mock, true);
            mock2.Should().Be(mock);
            mock2.Name.Should().Be("test");

        }

        [Fact]
        public void AddMockWithNull_ShouldThrow()
        {
            Action a = () => Mocks.AddMock(null, typeof(IFileSystem), false);
            a.Should().Throw<ArgumentNullException>();

            Action b = () => Mocks.AddMock(new Mock<IFileSystem>(), null, false);
            b.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddTypeBothSameClass()
        {
            Action a = () => Mocks.AddType<TestClassDouble1, TestClassDouble1>();
            a.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddTypeNotInterface()
        {
            Action a = () => Mocks.AddType<TestClassDouble2, TestClassDouble1>();
            a.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_WithMapTest1()
        {
            Action a = () => Mocks.CreateInstance<ITestClassDouble>();
            a.Should().Throw<AmbiguousImplementationException>();

            Mocks.AddType<ITestClassDouble, TestClassDouble1>();
            var o = Mocks.CreateInstance<ITestClassDouble>();
            o.Should().BeOfType<TestClassDouble1>();
        }

        [Fact]
        public void Create_WithMapTest2()
        {
            Action a = () => Mocks.CreateInstance<ITestClassDouble>();
            a.Should().Throw<AmbiguousImplementationException>();

            Mocks.AddType<ITestClassDouble, TestClassDouble2>();
            var o2 = Mocks.CreateInstance<ITestClassDouble>();
            o2.Should().BeOfType<TestClassDouble2>();
            o2.Value.Should().Be(0);

            Action b = () => Mocks.AddType<ITestClassDouble, TestClassDouble1>();
            b.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_WithMapInstance()
        {
            // Create Random number.
            var number = (double)RandomNumberGenerator.GetInt32(1, 100);

            // Add Mock Mapping, demonstrating that the number doesn't get used until CreateInstance is called.
            Mocks.AddType<ITestClassDouble, TestClassDouble2>(_ => new TestClassDouble2
                { Value = number });

            // Saving original number
            var number2 = number;

            // Changing number used by CreateInstance
            number = 0.5;

            // Get number from CreateInstance
            var value = Mocks.CreateInstance<ITestClassDouble>().Value;

            // Demonstrate that the latest number is used and not the original number.
            value.Should().Be(number);
            value.Should().NotBe(number2);
        }

        [Fact]
        public void Create_WithMapTest_CannotAddDuplicateMap()
        {
            Action a = () => Mocks.CreateInstance<ITestClassDouble>();
            a.Should().Throw<AmbiguousImplementationException>();

            Mocks.AddType<ITestClassDouble, TestClassDouble1>();
            Action b = () => Mocks.AddType<ITestClassDouble, TestClassDouble2>();
            b.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void FindConstructorByArgs()
        {
            CheckConstructorByArgs(Mocks.GetObject<IFileSystem>());
            CheckConstructorByArgs(new FileSystem());
            CheckConstructorByArgs(new MockFileSystem());
            CheckConstructorByArgs(new Mock<IFileSystem>().Object);
        }

        [Fact]
        public void FindConstructorByBest()
        {
            CheckBestConstructor(Mocks.GetObject<IFileSystem>());
            CheckBestConstructor(new FileSystem());
            CheckBestConstructor(new MockFileSystem());
            CheckBestConstructor(new Mock<IFileSystem>().Object);
            CheckBestConstructor(new Mock<IFile>().Object, false);
        }

        private void CheckConstructorByArgs(object data, bool expected = true)
        {
            var constructor = Mocks.FindConstructor(typeof(TestClassNormal), data);
            var isValid = Mocks.IsValidConstructor(constructor.Key, data);
            isValid.Should().Be(expected);
        }

        private void CheckBestConstructor(object data, bool expected = true)
        {
            var constructor = Mocks.FindConstructor(true, typeof(TestClassNormal));
            var isValid = Mocks.IsValidConstructor(constructor.Key, data);
            isValid.Should().Be(expected);
        }

        [Fact]
        public void IsValidConstructor()
        {
            var constructor = Mocks.FindConstructor(typeof(TestClassNormal), Mocks.GetObject<IFileSystem>());
            var isValid = Mocks.IsValidConstructor(constructor.Key, Mocks.GetObject<IFileSystem>());
            isValid.Should().BeTrue();

            isValid = Mocks.IsValidConstructor(constructor.Key, Mocks.GetObject<IFileSystem>(), 12);
            isValid.Should().BeFalse();

            isValid = Mocks.IsValidConstructor(constructor.Key, 12);
            isValid.Should().BeFalse();
        }

        private static Mocks CreateAction(Mocks mocks) => new();

        private static void CreatedAction(Mocks? component) => component.Should().NotBeNull();

        private static void SetupAction(Mocks mocks)
        {
            mocks.Initialize<IDirectory>(mock => mock.SetupAllProperties());
            mocks.Initialize<IFileInfo>(mock => mock.SetupAllProperties());
        }
    }
}

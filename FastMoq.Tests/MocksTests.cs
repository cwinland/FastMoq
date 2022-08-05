using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime;
using System.Security.Cryptography;
using Xunit;

#pragma warning disable CS8604
#pragma warning disable CS8602
#pragma warning disable CS8625

namespace FastMoq.Tests
{
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
    public class MocksTests : MockerTestBase<Mocker>
    {
        public MocksTests() : base(SetupAction, CreateAction, CreatedAction) { }

        [Fact]
        public void Mocker_CreateWithEmptyMap()
        {
            var test = new Mocker(new Dictionary<Type, InstanceModel>());
            test.typeMap.Should().BeEmpty();
        }

        [Fact]
        public void Mocker_CreateWithMap()
        {
            var map = new Dictionary<Type, InstanceModel>()
            {
                { typeof(IFileSystem), new InstanceModel<IFileSystem>() },
                { typeof(IFile), new InstanceModel<IFile>(_ => new MockFileSystem().File) }
            };

            var test = new Mocker(map);
            test.typeMap.Should().BeEquivalentTo(map);
        }

        [Fact]
        public void GetMockModelIndexOf_ShouldFindIfAuto()
        {
            _ = Component.GetMock<IFile>();

            // Should not find it, because it doesn't exist.
            Action a = () => Component.GetMockModelIndexOf(typeof(IFileSystem), false);
            a.Should().Throw<NotImplementedException>();

            // Should find it because it is auto created.
            Component.GetMockModelIndexOf(typeof(IFileSystem)).Should().Be(1);

            // Should find it because it was created in previous step.
            Component.GetMockModelIndexOf(typeof(IFileSystem), false).Should().Be(1);

            Component.GetMockModelIndexOf(typeof(IFile), false).Should().Be(0);
        }

        [Fact]
        public void TestMethodInvoke()
        {
            var o = Mocks.CreateInstance<ITestClassOne>();
            var x = Mocks.InvokeMethod(o, "TestVoid", true);
            var y = Mocks.InvokeMethod<ITestClassOne>(null, "TestStaticObject");
            var z = Mocks.InvokeMethod(o, "TestInt", true);
            x.Should().BeNull();
            y.Should().NotBeNull();
            y.Should().BeOfType<MockFileSystem>();
        }

        [Fact]
        public void MockParameters()
        {
            var o = Mocks.GetObject<TestClassDouble1>();
            o.Value = 33;
            o.Value.Should().Be(33);
            Mocks.GetObject<TestClassDouble1>().Value.Should().Be(33);
        }

        [Fact]
        public void AddMock()
        {
            var mock = new Mock<IFileSystemInfo>
            {
                Name = "First"
            };

            var mockResult = Mocks.AddMock(mock, false);
            var mockModel = Mocks.GetMockModel<IFileSystemInfo>();
            mockResult.Mock.Should().Be(mockModel.Mock);
            mockModel.Mock.Name.Should().Be("First");
        }

        [Fact]
        public void AddMockDuplicateWithoutOverwrite_ShouldThrowArgumentException()
        {
            var mock = new Mock<IFileSystemInfo>
            {
                Name = "First"
            };

            Mocks.AddMock(mock, false).Mock.Should().Be(mock);

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

            var mock1 = Mocks.AddMock(mock, false).Mock;
            mock1.Should().Be(mock);
            mock1.Name.Should().Be("First");

            mock.Name = "test";
            var mock2 = Mocks.AddMock(mock, true).Mock;
            mock2.Should().Be(mock);
            mock2.Name.Should().Be("test");
        }

        [Fact]
        public void AddMockWithNull_ShouldThrow()
        {
            Action a = () => Mocks.AddMock(null, typeof(IFileSystem));
            a.Should().Throw<ArgumentNullException>();

            Action b = () => Mocks.AddMock(new Mock<IFileSystem>(), null);
            b.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddTypeBothSameClass()
        {
            var a = () => Mocks.AddType<TestClassDouble1, TestClassDouble1>();
            a.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddTypeNotInterface()
        {
            var a = () => Mocks.AddType<TestClassDouble2, TestClassDouble1>();
            a.Should().Throw<ArgumentException>();
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
        public void Create_WithMapInstance()
        {
            // Create Random number.
            var number = (double) RandomNumberGenerator.GetInt32(1, 100);

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
            var b = () => Mocks.AddType<ITestClassDouble, TestClassDouble2>();
            b.Should().Throw<ArgumentException>();
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

            var b = () => Mocks.AddType<ITestClassDouble, TestClassDouble1>();
            b.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_WithNulls()
        {
            Mocks.CreateInstance<TestClassMany>(true, 4, null);
            Mocks.CreateInstance<TestClassNormal>(true, null);
            Action a = () => Mocks.CreateInstance<TestClassMany>(true, null, "str");
            a.Should().Throw<NotImplementedException>();
        }

        [Fact]
        public void CreateBest()
        {
            Mocks.CreateInstance<TestClassOne>().Should().NotBeNull();

            Mocks.CreateInstance<TestClassNormal>().Should().NotBeNull();
            Mocks.CreateInstance<IFileSystem>(true).Should().NotBeNull();
        }

        [Fact]
        public void CreateBest_Should_ThrowAmbiguous()
        {
            Action a = () => Mocks.CreateInstance<TestClassMany>();
            a.Should().Throw<AmbiguousImplementationException>();

            Action m = () => Mocks.CreateInstanceNonPublic<TestClassOne>().Should().NotBeNull();
            m.Should().Throw<AmbiguousImplementationException>();

            Action b = () => Mocks.CreateInstance<IFileSystem>(false).Should().NotBeNull();
            b.Should().Throw<AmbiguousImplementationException>();
        }

        [Fact]
        public void CreateExact_WithMultiClass()
        {
            Mocks.CreateInstance<TestClassMany>(4).Should().NotBeNull();
            Mocks.CreateInstance<TestClassMany>("str").Should().NotBeNull();
            Mocks.CreateInstance<TestClassMany>(true, 4, "str").Should().NotBeNull();
            Action a = () => Mocks.CreateInstance<TestClassMany>("4", "str").Should().NotBeNull();
            a.Should().Throw<NotImplementedException>();
            IFile file = new FileWrapper(new FileSystem());
            Mocks.CreateInstanceNonPublic<TestClassOne>(file).Should().NotBeNull();
            Mocks.CreateInstanceNonPublic<TestClassOne>(new FileSystem()).Should().NotBeNull();
            Action b = () => Mocks.CreateInstanceNonPublic<TestClassOne>("4", "str").Should().NotBeNull();
            b.Should().Throw<NotImplementedException>();

        }

        [Fact]
        public void CreateFromInterface_BestGuess() => Mocks.CreateInstance<ITestClassNormal>().Should().NotBeNull();

        [Fact]
        public void CreateFromInterface_ManyMatches_ShouldThrow()
        {
            Action a = () => Mocks.CreateInstance<IFile>().Should().NotBeNull();
            a.Should().Throw<AmbiguousImplementationException>();
        }

        [Fact]
        public void CreateMock_ShouldWork()
        {
            Mocks.CreateMock<IDirectoryInfo>().Should().NotBeNull();
            Mocks.Contains<IDirectoryInfo>().Should().BeTrue();
        }

        [Fact]
        public void CreateMock_ShouldWorkAsParam()
        {
            Mocks.CreateMock(typeof(IDirectoryInfo)).Should().NotBeNull();
            Mocks.Contains(typeof(IDirectoryInfo)).Should().BeTrue();
        }

        [Fact]
        public void CreateMock_ValueType_ShouldFail()
        {
            Action a = () => Mocks.CreateMock(typeof(int));
            a.Should().Throw<ArgumentException>();
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
            typeof(IFileSystem).IsAssignableFrom(Mocks.fileSystem.FileSystem.GetType()).Should().BeTrue();
            Mocks.GetObject<IFileSystem>().Should().Be(Mocks.fileSystem.FileSystem);
            Mocks.Strict = true;
            Mocks.GetObject<IFileSystem>().Should().NotBe(Mocks.fileSystem.FileSystem);
        }

        [Fact]
        public void FindConstructor_Exact()
        {
            var m = Mocks.FindConstructor(typeof(TestClassMany), false, 4, "");
            m.Should().NotBeNull();
        }

        [Fact]
        public void FindConstructor_Missing_ShouldThrow()
        {
            Action a = () => Mocks.FindConstructor(typeof(TestClassMany), false, Mocks.GetObject<IFileSystem>());
            a.Should().Throw<NotImplementedException>();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FindConstructorByArgs(bool nonPublic)
        {
            CheckConstructorByArgs(Mocks.GetObject<IFileSystem>(), true, nonPublic);
            CheckConstructorByArgs(new FileSystem(), true, nonPublic);
            CheckConstructorByArgs(new MockFileSystem(), true, nonPublic);
            CheckConstructorByArgs(new Mock<IFileSystem>().Object, true, nonPublic);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FindConstructorByBest(bool nonPublic)
        {
            CheckBestConstructor(Mocks.GetObject<IFileSystem>(), true, nonPublic);
            CheckBestConstructor(new FileSystem(), true, nonPublic);
            CheckBestConstructor(new MockFileSystem(), true, nonPublic);
            CheckBestConstructor(new Mock<IFileSystem>().Object, true, nonPublic);
            CheckBestConstructor(new Mock<IFile>().Object, false, nonPublic);
        }

        [Fact]
        public void GetList()
        {
            var count = 0;
            var numbers = Mocker.GetList(3, () => count++);
            numbers.Should().BeEquivalentTo(new List<int> { 0, 1, 2 });

            count = 0;
            var strings = Mocker.GetList(3, () => (count++).ToString());
            strings.Should().BeEquivalentTo(new List<string> { "0", "1", "2" });

            count = 0;
            var test = Mocker.GetList(3, () => new TestClassMany(count++));
            test[0].value.Should().Be(0);
            test[1].value.Should().Be(1);
            test[2].value.Should().Be(2);
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
        public void GetRequiredMock()
        {
            Mocks.GetRequiredMock<IDirectory>().Should().NotBeNull();
            Mocks.GetRequiredMock<IFileInfo>().Should().NotBeNull();
            Action a = () => Mocks.GetRequiredMock<IFile>();
            a.Should().Throw<InvalidOperationException>();
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
        public void CreateInstanceShouldCreateByType()
        {
            var test = Component.CreateInstance<ITestClassMultiple, IFileSystem, IFile>(new Dictionary<Type, object?>()
            {
                { typeof(IFileSystem), null }
            });

            test.Fs.Should().BeNull();
            test.F.Should().NotBeNull();

            var test2 = Component.CreateInstance<ITestClassMultiple, IFileSystem, IFile>(new Dictionary<Type, object?>()
            {
                { typeof(IFile), null }
            });

            test2.F.Should().BeNull();
            test2.Fs.Should().NotBeNull();
        }

        [Fact]
        public void GetObjectWithArgs()
        {
            var args = Component.GetArgData<ITestClassMultiple>();
            var test = Component.GetObject<ITestClassMultiple>(args);
            test.Fs.Should().NotBeNull();
            test.F.Should().NotBeNull();

            args[0] = null;
            var test2 = Component.GetObject<ITestClassMultiple>(args);
            test2.Fs.Should().BeNull();
            test2.F.Should().NotBeNull();
        }

        [Fact]
        public void IsValidConstructor()
        {
            var constructor = Mocks.FindConstructor(typeof(TestClassNormal), false, Mocks.GetObject<IFileSystem>());
            var isValid = Mocker.IsValidConstructor(constructor.ConstructorInfo, Mocks.GetObject<IFileSystem>());
            isValid.Should().BeTrue();

            isValid = Mocker.IsValidConstructor(constructor.ConstructorInfo, Mocks.GetObject<IFileSystem>(), 12);
            isValid.Should().BeFalse();

            isValid = Mocker.IsValidConstructor(constructor.ConstructorInfo, 12);
            isValid.Should().BeFalse();
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

        private void CheckBestConstructor(object data, bool expected, bool nonPublic)
        {
            var constructor = Mocks.FindConstructor(true, typeof(TestClassNormal), nonPublic);
            var isValid = Mocker.IsValidConstructor(constructor.ConstructorInfo, data);
            isValid.Should().Be(expected);
        }

        private void CheckConstructorByArgs(object data, bool expected, bool nonPublic)
        {
            var constructor = Mocks.FindConstructor(typeof(TestClassNormal), nonPublic, data);
            var isValid = Mocker.IsValidConstructor(constructor.ConstructorInfo, data);
            isValid.Should().Be(expected);
        }

        private static Mocker CreateAction(Mocker mocks) => new();

        private static void CreatedAction(Mocker? component) => component.Should().NotBeNull();

        private static void SetupAction(Mocker mocks)
        {
            mocks.Initialize<IDirectory>(mock => mock.SetupAllProperties());
            mocks.Initialize<IFileInfo>(mock => mock.SetupAllProperties());
        }
    }
}
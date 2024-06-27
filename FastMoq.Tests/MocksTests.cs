using FastMoq.Extensions;
using FastMoq.Models;
using FastMoq.Tests.TestBase;
using FastMoq.Tests.TestClasses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;

#pragma warning disable CS8604 // Possible null reference argument for parameter.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'.
#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable.
#pragma warning disable CS8974 // Converting method group to non-delegate type
#pragma warning disable CS0472 // The result of the expression is always 'value1' since a value of type 'value2' is never equal to 'null' of type 'value3'.

namespace FastMoq.Tests
{
    public class MocksTests : MockerTestBase<Mocker>
    {
        public MocksTests() : base(SetupAction, CreateAction, CreatedAction)
        {
            Component.AddFileSystemAbstractionMapping();
        }

        [Fact]
        public void AddInjections()
        {
            // Null Path
            object? obj = null;
            Component.AddInjections(obj).Should().Be(null);

            // Create class without injected property.
            var c = new TestClassOne(Mocks.GetObject<IFile>());

            // Check property is null
            c.FileSystem.Should().BeNull();

            // AddInjections sets property with InjectAttribute.
            Component.AddInjections(c).FileSystem.Should().NotBeNull();
        }

        [Fact]
        public void AddMock()
        {
            var mock = new Mock<IFileSystemInfo>
            {
                Name = "First",
            };

            MockModel<IFileSystemInfo> mockResult = Mocks.AddMock(mock, false);
            MockModel<IFileSystemInfo> mockModel = Mocks.GetMockModel<IFileSystemInfo>();
            mockResult.Mock.Should().Be(mockModel.Mock);
            mockModel.Mock.Name.Should().Be("First");
        }

        [Fact]
        public void AddMockDuplicateWithoutOverwrite_ShouldThrowArgumentException()
        {
            var mock = new Mock<IFileSystemInfo>
            {
                Name = "First",
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
                Name = "First",
            };

            Mock<IFileSystemInfo> mock1 = Mocks.AddMock(mock, false).Mock;
            mock1.Should().Be(mock);
            mock1.Name.Should().Be("First");

            mock.Name = "test";
            Mock<IFileSystemInfo> mock2 = Mocks.AddMock(mock, true).Mock;
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
            var number = (double)RandomNumberGenerator.GetInt32(1, 100);

            // Add Mock Mapping, demonstrating that the number doesn't get used until CreateInstance is called.
            Mocks.AddType<ITestClassDouble, TestClassDouble2>(_ => new TestClassDouble2
            { Value = number }
            );

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
        public void Create_WithMapTest1b()
        {
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
        public void CreateBest()
        {
            Mocks.CreateInstance<TestClassOne>().Should().NotBeNull();

            Mocks.CreateInstance<TestClassNormal>().Should().NotBeNull();
            Mocks.CreateInstance<IFileSystem>(true).Should().NotBeNull();
        }

        [Fact]
        public void CreateBest_Should_ThrowAmbiguous()
        {
            // Ambiguous constructors.
            new Action(() => Mocks.CreateInstance<TestClassMany>()).Should().Throw<AmbiguousImplementationException>();
            new Action(() => Mocks.CreateInstanceNonPublic<TestClassOne>().Should().NotBeNull()).Should().Throw<AmbiguousImplementationException>();

            Mocks.Strict = true;
            // No Constructor.
            new Action(() => Mocks.CreateInstance<IFileSystem>(false).Should().NotBeNull()).Should().Throw<NotImplementedException>();

            // Valid Constructor.
            new Action(() => Mocks.CreateInstance<IFileSystem>(true).Should().NotBeNull()).Should().NotThrow();
        }

        [Fact]
        public void CreateClassWithInjectParameters()
        {
            Mocks.MockOptional = true;
            var m = Mocks.CreateInstance<TestClassParameters>();
            m.Should().NotBeNull();
            m.anotherFileSystem.Should().NotBeNull();
            m.anotherFileSystem2.Should().NotBeNull();
            m.anotherFileSystem3.Should().NotBeNull();
            m.invalidInjection.Should().Be(0);
            m.invalidInjection2.Should().BeNull();
            m.invalidInjection3.Should().BeEmpty();
            m.invalidInjection4.Should().BeEmpty();
            m.fileSystem.Should().NotBeNull();
            m.logger.Should().NotBeNull();

            Mocks.CreateInstance<ITestClassOne>().Should().NotBeNull();
            (Mocks.CreateInstance<ITestClassOne>() as TestClassOne).FileSystem.Should().NotBeNull();
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
        public void CreateExact_ByTypeWithMultiClass()
        {
            Mocks.CreateInstanceByType<TestClassMany>(new Type[] { typeof(int) }).Should().NotBeNull();
            Mocks.CreateInstanceByType<TestClassMany>(new Type[] { typeof(string) }).Should().NotBeNull();
            Mocks.CreateInstanceByType<TestClassMany>(new Type[] { typeof(int), typeof(string) }).Should().NotBeNull();
            Action a = () => Mocks.CreateInstance<TestClassMany>(new Type[] { typeof(string), typeof(string) }).Should().NotBeNull();
            a.Should().Throw<NotImplementedException>();
        }

        [Fact]
        public void CreateFromInterface_BestGuess() => Mocks.CreateInstance<ITestClassNormal>().Should().NotBeNull();

        [Fact]
        public void CreateFromInterface_ManyMatches_ShouldThrow_Ambigous()
        {
            new Action(() => Mocks.CreateInstance<ITestClassDouble>().Should().NotBeNull()).Should().Throw<AmbiguousImplementationException>();
            Mocks.AddType<ITestClassDouble, TestClassDouble1>();
            new Action(() => Mocks.CreateInstance<ITestClassDouble>().Should().NotBeNull()).Should().NotThrow<AmbiguousImplementationException>();
        }

        [Fact]
        public void CreateInstance_ShouldNotBeNull()
        {
            Mocks.CreateInstance<ITestClassOne>().Should().NotBeNull();
            Mocks.CreateInstance<TestClassDouble1>().Should().NotBeNull();
            Mocks.CreateInstance<TestClassDouble2>().Should().NotBeNull();
            Mocks.CreateInstance<TestClassParameters>().Should().NotBeNull();
        }

        [Fact]
        public void CreateInstanceShouldCreateByType()
        {
            var test = Component.CreateInstance<ITestClassMultiple, IFileSystem, IFile>(new Dictionary<Type, object?>
                {
                    {typeof(IFileSystem), null},
                }
            );

            test.Fs.Should().BeNull();
            test.F.Should().NotBeNull();

            var test2 = Component.CreateInstance<ITestClassMultiple, IFileSystem, IFile>(new Dictionary<Type, object?>
                {
                    {typeof(IFile), null},
                }
            );

            test2.F.Should().BeNull();
            test2.Fs.Should().NotBeNull();
        }

        [Fact]
        public void CreateInterfaceWithInjectParameters()
        {
            Mocks.CreateInstance<ITestClassOne>().Should().NotBeNull();
            (Mocks.CreateInstance<ITestClassOne>() as TestClassOne).FileSystem.Should().NotBeNull();
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
        public void CreateMockMappedCreateFuncObjectWithInjectParameters()
        {
            Mocks.AddType<ITestClassOne, TestClassOne>(x => x.CreateInstance<TestClassOne>());
            Mocks.GetObject<ITestClassOne>().Should().NotBeNull();
            Mocks.GetObject<ITestClassOne>().FileSystem.Should().NotBeNull();
        }

        [Fact]
        public void CreateMockMappedObjectWithInjectParameters()
        {
            Mocks.AddType<ITestClassOne, TestClassOne>();
            Mocks.GetObject<ITestClassOne>().Should().NotBeNull();
            Mocks.GetObject<ITestClassOne>().FileSystem.Should().NotBeNull();
        }

        [Fact]
        public void CreateMockObjectWithInjectParameters()
        {
            Mocks.GetObject<ITestClassOne>().Should().NotBeNull();
            Mocks.GetObject<ITestClassOne>().FileSystem.Should().NotBeNull();
        }

        [Fact]
        public void CreateMockWithInjectParameters()
        {
            Mocks.AddType<ITestClassOne, TestClassOne>();
            Mocks.GetMock<ITestClassOne>().Object.FileSystem.Should().NotBeNull();
            Mocks.GetMock<TestClassOne>().Object.FileSystem.Should().NotBeNull();
        }

        [Fact]
        public void AddTypeT_ShouldBe_AddTypeTT()
        {
            Mocks.AddType<TestClassOne>(_ => Mocks.CreateInstance<TestClassOne>());
            var t = GetTypeMapOf<TestClassOne>().Value.CreateFunc.Invoke(null);
            Mocks.typeMap.Clear();
            Mocks.AddFileSystemAbstractionMapping();
            Mocks.AddType<TestClassOne, TestClassOne>(_ => Mocks.CreateInstance<TestClassOne>());
            var t2 = GetTypeMapOf<TestClassOne>().Value.CreateFunc.Invoke(null);
            t.Should().BeEquivalentTo(t2);
        }

        [Fact]
        public void AddTypeT_ShouldBe_AddType()
        {
            Mocks.AddType<TestClassOne>(_ => Mocks.CreateInstance<TestClassOne>());
            var t = GetTypeMapOf<TestClassOne>().Value.CreateFunc.Invoke(null);
            var o = Mocks.CreateInstance<TestClassOne>();
            o.Should().BeEquivalentTo(t);
            Mocks.typeMap.Clear();
            Mocks.AddFileSystemAbstractionMapping(); 
            Mocks.AddType(typeof(TestClassOne), typeof(TestClassOne), _ => Mocks.CreateInstance<TestClassOne>());
            var t2 = GetTypeMapOf<TestClassOne>().Value.CreateFunc.Invoke(null);
            t.Should().BeEquivalentTo(t2);
            o = Mocks.CreateInstance<TestClassOne>();
            o.Should().BeEquivalentTo(t2);
        }

        private KeyValuePair<Type, IInstanceModel> GetTypeMapOf<T>() => Mocks.typeMap.First(x => x.Value.Type.FullName == typeof(T).FullName);

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
            List<int> numbers = Mocker.GetList(3, () => count++);
            numbers.Should().BeEquivalentTo(new List<int> { 0, 1, 2 });

            count = 0;
            List<string> strings = Mocker.GetList(3, () => (count++).ToString());
            strings.Should().BeEquivalentTo(new List<string> { "0", "1", "2" });

            count = 0;
            List<TestClassMany> test = Mocker.GetList(3, () => new TestClassMany(count++));
            test[0].value.Should().Be(0);
            test[1].value.Should().Be(1);
            test[2].value.Should().Be(2);
        }

        [Fact]
        public void GetList_ShouldInitAfterCreate()
        {
            List<TestClassMany> testInit = Mocker.GetList(3, i => new TestClassMany(i), (i, many) => many.value = i * 2);
            testInit[0].value.Should().Be(0);
            testInit[1].value.Should().Be(2);
            testInit[2].value.Should().Be(4);
        }

        [Fact]
        public void GetListParameter()
        {
            List<int> numbers = Mocker.GetList(3, i => i);
            numbers.Should().BeEquivalentTo(new List<int> { 0, 1, 2 });

            List<string> strings = Mocker.GetList(3, i => i.ToString());
            strings.Should().BeEquivalentTo(new List<string> { "0", "1", "2" });

            List<TestClassMany> test = Mocker.GetList(3, i => new TestClassMany(i));
            test[0].value.Should().Be(0);
            test[1].value.Should().Be(1);
            test[2].value.Should().Be(2);
        }

        [Fact]
        public void GetMethodArgData()
        {
            var type = typeof(Thread);
            var instance = Thread.CurrentThread;

            var types = new List<Type>
                {typeof(int)};

            var methodInfo = type.GetMethod("Sleep", types.ToArray());
            var argData = Mocks.GetMethodArgData(methodInfo);
            argData.Should().Contain(0);

            types = new List<Type>
                {typeof(TimeSpan)};

            methodInfo = type.GetMethod("Sleep", types.ToArray());
            argData = Mocks.GetMethodArgData(methodInfo);
            argData.First().Should().BeOfType<TimeSpan>();

            types = new List<Type>
                {typeof(string), typeof(Type[])};

            methodInfo = type.GetType().GetMethod("GetMethod", types.ToArray());
            argData = Mocks.GetMethodArgData(methodInfo);
            CheckTypes(argData, types);

            types = new List<Type>
            {
                typeof(string), typeof(BindingFlags), typeof(Binder), typeof(CallingConventions), typeof(Type[]),
                typeof(ParameterModifier[]),
            };

            methodInfo = type.GetType().GetMethod("GetMethod", types.ToArray());
            argData = Mocks.GetMethodArgData(methodInfo);
            CheckTypes(argData, types);
        }

        [Fact]
        public void GetMethodArgData_Null_ShouldThrow() => new Action(() => Mocks.GetMethodArgData(null)).Should().Throw<ArgumentNullException>();

        [Fact]
        public void GetMethodDefaultData_Null_ShouldThrow() =>
            new Action(() => Component.GetMethodDefaultData(null)).Should().Throw<ArgumentNullException>();

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
        public void GetMockAction()
        {
            var mock = Mocks.GetMock<IFileInfo>(mock =>
            {
                mock.Should().NotBeNull();
                mock.Object.FileSystem.Should().BeNull();
                mock.SetupGet(x => x.FileSystem).Returns(Mocks.fileSystem);
            });
            mock.Should().NotBeNull();
            mock.Object.FileSystem.Should().NotBeNull();
        }

        [Fact]
        public void GetMockAction_WithGetFileSystem()
        {
            Mocks.GetFileSystem(fs =>
                {
                    fs.Should().BeOfType<MockFileSystem>();
                    fs.File.Should().BeOfType<MockFile>();
                }
            );

            if (Mocks.GetFileSystem() is not IFileSystem)
            {
                throw new InvalidCastException("Expected GetFileSystem() to be IFileSystem");
            }
        }

        [Fact]
        public void GetMockInstance()
        {
            Mock<ITestClassMany> mock = Component.CreateMockInstance<ITestClassMany>();
            mock.Setup(x => x.Value).Returns(1);
            var mock1Object = mock.Object;

            Mock<ITestClassMany> mock2 = Component.CreateMockInstance<ITestClassMany>();
            mock2.Setup(x => x.Value).Returns(2);
            var mock2Object = mock2.Object;

            mock1Object.Value.Should().NotBe(mock2Object.Value);
        }

        [Fact]
        public void GetMockModelIndexOf_ShouldFindIfAuto()
        {
            _ = Component.GetMock<IFile>();
            var mockCount = Component.mockCollection.Count;

            // Should not find it, because it doesn't exist.
            Action a = () => Component.GetMockModelIndexOf(typeof(IFileSystem), false);
            a.Should().Throw<NotImplementedException>();

            // Should find it because it is auto created.
            Component.GetMockModelIndexOf(typeof(IFileSystem)).Should().Be(mockCount);

            // Should find it because it was created in previous step.
            Component.GetMockModelIndexOf(typeof(IFileSystem), false).Should().Be(mockCount);

            Component.GetMockModelIndexOf(typeof(IFile), false).Should().Be(mockCount - 1);
        }

        [Fact]
        public void GetMockValueTest()
        {
            Mock<ITestClassMany> mock = Component.GetMock<ITestClassMany>();
            mock.Setup(x => x.Value).Returns(1);
            var mock1Object = mock.Object;

            Mock<ITestClassMany> mock2 = Component.GetMock<ITestClassMany>();
            mock2.Setup(x => x.Value).Returns(2);
            var mock2Object = mock2.Object;

            mock1Object.Value.Should().Be(mock2Object.Value);
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
        public void GetObject_InitAction()
        {
            var obj = Component.GetObject<TestClass>(t => t.field2 = 3);
            obj.field2.Should().Be(3);
            Component.AddType<ITestClassDouble, TestClassDouble2>();
            var obj2 = Component.GetObject<ITestClassDouble>(t => t.Value = 333.333);
            obj2.Value.Should().Be(333.333);
        }

        [Fact]
        public void GetObject_InitAction_ShouldThrowWithoutMap()
        {
            var obj = Component.GetObject<TestClass>(t => t.field2 = 3);
            obj.field2.Should().Be(3);
            new Action(() => Component.GetObject<ITestClassDouble>(t => t.Value = 333.333)).Should().Throw<AmbiguousImplementationException>();
        }

        [Fact]
        public void GetObject_ShouldThrow()
        {
            new Action(() => Component.GetObject(null)).Should().Throw<ArgumentNullException>();

            var info = Mocks.GetObject<ParameterInfo>();
            new Action(() => Component.GetObject(info)).Should().Throw<Exception>();
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

            args = Component.GetArgData<TestClassParameters>();
            Component.GetObject<TestClassParameters>(args);
            Component.CreateInstance<TestClassParameters>();
            Component.CreateInstance<TestClassParameters>(args);
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
        public void IsValidConstructor()
        {
            var constructor = Mocks.FindConstructor(typeof(TestClassNormal), false, Mocks.GetObject<IFileSystem>());
            var isValid = typeof(IFileSystem).IsValidConstructor(constructor.ConstructorInfo, Mocks.GetObject<IFileSystem>());
            isValid.Should().BeTrue();

            isValid = typeof(IFileSystem).IsValidConstructor(constructor.ConstructorInfo, Mocks.GetObject<IFileSystem>(), 12);
            isValid.Should().BeFalse();

            isValid = typeof(IFileSystem).IsValidConstructor(constructor.ConstructorInfo, 12);
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Mocker_AddMapClass_NotADuplicate()
        {
            Component.typeMap.Clear();
            new Action(() => Component.AddType<IFileSystem, FileSystem>())
                .Should().NotThrow();
        }

        [Fact]
        public void Mocker_AddMapClass_Duplicate()
        {
            new Action(() => Component.AddType<IFileSystem, FileSystem>())
                .Should().Throw<Exception>();
        }

        [Fact]
        public void Mocker_AddMapClass_OverwriteDuplicate()
        {
            new Action(() => Component.AddType<IFileSystem, FileSystem>(replace: true))
                .Should().NotThrow();
        }

        [Fact]
        public void Mocker_AddMapClassIncompatibleInterface_ShouldThrow() => new Action(() => Component.AddType<IFileInfo, FileSystem>())
            .Should().ThrowExactly<ArgumentException>($"{nameof(FileSystem)} is not assignable to {nameof(IFileInfo)}.");

        [Fact]
        public void Mocker_AddMapInterfaceAsClass_ShouldThrow() => new Action(() => Component.AddType<IFileInfo, IFile>())
            .Should().ThrowExactly<ArgumentException>($"{nameof(IFile)} cannot be an interface.");

        [Fact]
        public void Mocker_CreateMockInstance_InnerMockResolution_False_ShouldThrow()
        {
            Component.InnerMockResolution = false;
            new Action(() => Component.CreateMockInstance<TestClassMultiple>()).Should().NotThrow<ArgumentException>();
        }

        [Fact]
        public void Mocker_CreateMockInstance_InnerMockResolution_True_ShouldNotThrow()
        {
            Component.InnerMockResolution = true;
            new Action(() => Component.CreateMockInstance<TestClassMultiple>()).Should().NotThrow<ArgumentException>();
        }

        [Fact]
        public void Mocker_CreateMockInstanceNull_ShouldThrow() =>
            new Action(() => Component.CreateMockInstance(null)).Should().Throw<ArgumentException>();

        [Fact]
        public void Mocker_CreateWithEmptyMap()
        {
            var test = new Mocker(new Dictionary<Type, IInstanceModel>());
            test.typeMap.Should().BeEmpty();
        }

        [Fact]
        public void Mocker_CreateWithMap()
        {
            var map = new Dictionary<Type, IInstanceModel>
            {
                {typeof(IFileSystem), new InstanceModel<IFileSystem>()},
                {typeof(IFile), new InstanceModel<IFile>(_ => new MockFileSystem().File)},
            };

            var test = new Mocker(map);
            test.typeMap.Should().BeEquivalentTo(map);
        }

        [Fact]
        public void MockParameters()
        {
            var o = Mocks.GetObject<TestClassDouble1>();
            o.Value = 33;
            o.Value.Should().Be(33);
            Mocks.GetObject<TestClassDouble1>().Value.Should().Be(33);
            Mocks.GetObject<TestClassDouble1>().Value = 44;
            Mocks.GetObject<TestClassDouble1>().Value.Should().Be(44);
            Mocks.GetMock<TestClassDouble1>().Object.Value.Should().Be(44);
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
        public void TestMethodInvoke()
        {
            Mocks.InvokeMethod(Mocks.CreateInstance<ITestClassOne>(), "TestVoid", true).Should().BeNull();
            Mocks.InvokeMethod(Mocks.CreateInstance<ITestClassOne>(), "TestVoid").Should().BeNull();
            Mocks.InvokeMethod<ITestClassOne>(null, "TestStaticObject").Should().BeOfType<MockFileSystem>();
            Mocks.InvokeMethod<ITestClassOne>("TestStaticObject").Should().BeOfType<MockFileSystem>();
            Mocks.InvokeMethod(Mocks.CreateInstance<TestClassOne>(), "TestInt", true).Should().Be(0);
            Mocks.InvokeMethod(Mocks.CreateInstance<ITestClassOne>(), "TestInt", true).Should().Be(0);
            Mocks.InvokeMethod(Mocks.CreateInstance<ITestClassOne>(), "TestInt", true, 2).Should().Be(2);
            Mocks.InvokeMethod(Mocks.CreateInstance<TestClassOne>(), "TestInt", true, 2).Should().Be(2);

            Mocks.Strict = true;
            Action a = () => Mocks.InvokeMethod(Mocks.CreateInstance<ITestClassOne>(), "TestVoid");
            a.Should().Throw<ArgumentOutOfRangeException>().WithMessage("Specified argument was out of the range of valid values.*");
        }

        [Fact]
        public void TestCreateInstanceWithMap()
        {
            Mocks.CreateInstance<ITestClassOne>().Should().NotBeNull();
            new Action(() => Mocks.CreateInstance<ITestClassDouble>()).Should().Throw<AmbiguousImplementationException>();
            Mocks.AddType<ITestClassDouble, TestClassDouble1>();
            Mocks.CreateInstance<ITestClassDouble>().Should().NotBeNull();
        }

        [Fact]
        public void TestCreateEmptyUri()
        {
            var uri = new UriBuilder().Uri;

            uri.Should().BeEquivalentTo(typeof(Uri).GetDefaultValue());
        }

        [Fact]
        public void TestCreateInstanceWithMap2()
        {
            // Create instance via arguments in create instance.
            var instance1 = Mocks.CreateInstance<ITestClassMany>(1, "test");
            instance1.Should().NotBeNull();

            // Add type map with arguments.
            Mocks.AddType<ITestClassMany, TestClassMany>(args: new object?[] { 1, "test" });
            var instance2 = Mocks.CreateInstance<ITestClassMany>();
            instance2.Should().NotBeNull();

            // instances should be equivalent.
            instance1.Should().BeEquivalentTo(instance2);

            // Creating instance with parameter overrides type map.
            Mocks.CreateInstance<ITestClassMany>(1).Should().NotBeEquivalentTo(instance1);
        }

        internal static int CallTestMethodInt(int num, IFileSystem fileSystem, ITestCollectionOrderer dClass, TestClassMultiple mClass, string name)
        {
            ArgumentNullException.ThrowIfNull(fileSystem);
            ArgumentNullException.ThrowIfNull(dClass);
            ArgumentNullException.ThrowIfNull(mClass);
            ArgumentNullException.ThrowIfNull(num);
            ArgumentNullException.ThrowIfNull(name);

            return num;
        }

        internal static object?[] CallTestMethod(int num, IFileSystem fileSystem, ITestCollectionOrderer dClass, TestClassMultiple mClass, string name)
        {
            ArgumentNullException.ThrowIfNull(fileSystem);
            ArgumentNullException.ThrowIfNull(dClass);
            ArgumentNullException.ThrowIfNull(mClass);
            ArgumentNullException.ThrowIfNull(num);
            ArgumentNullException.ThrowIfNull(name);

            return
            [
                num, fileSystem, dClass, mClass, name,
            ];
        }

        internal void CallTestMethodVoid(int num, IFileSystem fileSystem)
        {
            ArgumentNullException.ThrowIfNull(fileSystem);

            if (num == 5)
            {
                throw new InvalidDataException();
            }
        }

        [Fact]
        public void CallMethod_Success_WhenVoid()
        {
            Mocks.CallMethod<object>(CallTestMethodVoid);
            Mocks.CallMethod(() => CallTestMethodVoid);
        }

        [Fact]
        public void CallMethod_Error_WhenNull()
        {
            Assert.Throws<ArgumentNullException>(() => Mocks.CallMethod<object>(CallTestMethodVoid, 0, null));
            Assert.Throws<ArgumentNullException>(() => Mocks.CallMethod(CallTestMethodVoid, 0, null));
        }

        [Fact]
        public void CallMethod_Error_When5()
        {
            Assert.Throws<InvalidDataException>(() => Mocks.CallMethod<object>(CallTestMethodVoid, 5));
            Assert.Throws<InvalidDataException>(() => Mocks.CallMethod(CallTestMethodVoid, 5));
        }

        [Fact]
        public void CallMethod()
        {
            var result = Mocks.CallMethod<object?[]>(CallTestMethod);
            result.Length.Should().Be(5);
            result[0].Should().Be(0);
            result[1].Should().BeOfType<MockFileSystem>().And.NotBeNull();
            result[2].GetType().IsAssignableTo(typeof(ITestCollectionOrderer)).Should().BeTrue();
            result[2].Should().NotBeNull();
            result[3].GetType().IsAssignableTo(typeof(TestClassMultiple)).Should().BeTrue();
            result[3].Should().NotBeNull();
            result[4].Should().Be("");
        }

        [Fact]
        public void CallMethod_WithParams()
        {
            var result = Mocks.CallMethod<object?[]>(CallTestMethod, 4);
            result.Length.Should().Be(5);
            result[0].Should().Be(4);
            result[1].Should().BeOfType<MockFileSystem>().And.NotBeNull();
            result[2].GetType().IsAssignableTo(typeof(ITestCollectionOrderer)).Should().BeTrue();
            result[2].Should().NotBeNull();
            result[3].GetType().IsAssignableTo(typeof(TestClassMultiple)).Should().BeTrue();
            result[3].Should().NotBeNull();
            result[4].Should().Be("");
        }

        [Fact]
        public void CallMethod_WithParams2()
        {
            var result = Mocks.CallMethod<object?[]>(CallTestMethod, 4, Mocks.fileSystem);
            result.Length.Should().Be(5);
            result[0].Should().Be(4);
            result[1].Should().BeOfType<MockFileSystem>().And.NotBeNull();
            result[2].GetType().IsAssignableTo(typeof(ITestCollectionOrderer)).Should().BeTrue();
            result[2].Should().NotBeNull();
            result[3].GetType().IsAssignableTo(typeof(TestClassMultiple)).Should().BeTrue();
            result[3].Should().NotBeNull();
            result[4].Should().Be("");
        }

        [Fact]
        public void CallMethod_WithParamsReturnInt()
        {
            var result = Mocks.CallMethod<int>(CallTestMethodInt, 4, Mocks.fileSystem);
            result.Should().Be(4);
        }

        [Fact]
        public void CallMethod_WithParamsReturnInt2()
        {
            var result = Mocks.CallMethod<int>(CallTestMethodInt, 7);
            result.Should().Be(7);
        }

        [Fact]
        public void CallMethod_WithNoParamsReturnInt()
        {
            var result = Mocks.CallMethod<int>(CallTestMethodInt);
            result.Should().Be(0);
        }

        [Fact]
        public void CallMethod_WithException()
        {
            Assert.Throws<ArgumentNullException>(() => Mocks.CallMethod<object?[]>(CallTestMethod, 4, null));
        }

        [Fact]
        public void VerifyLogger_ShouldPass_WhenMatches()
        {
            var mLogger = new Mock<ILogger>();
            mLogger.VerifyLogger(LogLevel.Information, "test", 0);

            mLogger.Object.LogInformation("test");
            mLogger.VerifyLogger(LogLevel.Information, "test");

            mLogger.Object.LogInformation("test");
            mLogger.VerifyLogger(LogLevel.Information, "test", 2);
            mLogger.VerifyLogger(LogLevel.Information, "test", null, null, 2);

            mLogger.Invocations.Clear();
            mLogger.Object.LogError(1, new AmbiguousImplementationException("Test Exception"), "test message");
            mLogger.VerifyLogger(LogLevel.Error, "test", new AmbiguousImplementationException("Test Exception"), 1);
            mLogger.VerifyLogger<Exception>(LogLevel.Error, "test", new AmbiguousImplementationException("Test Exception"), 1);
        }

        [Fact]
        public void VerifyLogger_ShouldPass_WhenMatchesILoggerSubtype()
        {
            var mLogger = new Mock<ILogger<NullLogger>>();
            mLogger.VerifyLogger(LogLevel.Information, "test", 0);

            mLogger.Object.LogInformation("test");
            mLogger.VerifyLogger(LogLevel.Information, "test");

            mLogger.Object.LogInformation("test");
            mLogger.VerifyLogger(LogLevel.Information, "test", 2);
            mLogger.VerifyLogger(LogLevel.Information, "test", null, null, 2);

            mLogger.Invocations.Clear();
            mLogger.Object.LogError(1, new AmbiguousImplementationException("Test Exception"), "test message");
            mLogger.VerifyLogger(LogLevel.Error, "test", new AmbiguousImplementationException("Test Exception"), 1);
            mLogger.VerifyLogger<Exception, NullLogger>(LogLevel.Error, "test", new AmbiguousImplementationException("Test Exception"), 1);
        }

        [Fact]
        public void VerifyLogger_ShouldThrow_WhenNotMatches()
        {
            var mLogger = new Mock<ILogger>();
            mLogger.VerifyLogger(LogLevel.Information, "test", 0);

            mLogger.Object.LogInformation("test");
            Assert.Throws<MockException>(() => mLogger.VerifyLogger(LogLevel.Information, "test2")); // Wrong Message.

            mLogger.Object.LogInformation("test");
            Assert.Throws<MockException>(() => mLogger.VerifyLogger(LogLevel.Information, "test")); // Wrong number of times.

            mLogger.Invocations.Clear();
            mLogger.Object.LogError(1, new AmbiguousImplementationException("Test Exception"), "test message");
            Assert.Throws<MockException>(() => mLogger.VerifyLogger(LogLevel.Error, "test", new AmbiguousImplementationException("Test Exception"), 0)); // Wrong eventId.
        }

        private static void LogException(Exception ex, ILogger log, string customMessage = "", [CallerMemberName] string caller = "")
        {
            log.LogError("[{caller}] - {customMessage}{errorMessage}", caller, $"{customMessage} ", ex.Message);
        }

        [Fact]
        public void TestLogException_CallsLogError()
        {
            var ex = new Exception();

            Mocks.AddType<ILogger, Logger<NullLogger>>();
            Mocks.CallMethod<object>(LogException, ex);
        }

        private void CheckBestConstructor(object data, bool expected, bool nonPublic)
        {
            var constructor = Mocks.FindConstructor(true, typeof(TestClassNormal), nonPublic);
            var isValid = typeof(IFileSystem).IsValidConstructor(constructor.ConstructorInfo, data);
            isValid.Should().Be(expected);
        }

        private void CheckConstructorByArgs(object data, bool expected, bool nonPublic)
        {
            var constructor = Mocks.FindConstructor(typeof(TestClassNormal), nonPublic, data);
            var isValid = typeof(IFileSystem).IsValidConstructor(constructor.ConstructorInfo, data);
            isValid.Should().Be(expected);
        }

        private static void CheckTypes(IReadOnlyList<object?> argData, List<Type> types)
        {
            for (var i = 0; i < argData.Count; i++)
            {
                types[i].IsInstanceOfType(argData[i]).Should().BeTrue();
            }
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

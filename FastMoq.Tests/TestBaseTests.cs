using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FastMoq.Extensions;
using FastMoq.Models;
using FluentAssertions.Equivalency;
using Xunit.Abstractions;
using Xunit.Sdk;

#pragma warning disable CS8604 // Possible null reference argument for parameter.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'.
#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable.
#pragma warning disable CS8974 // Converting method group to non-delegate type
#pragma warning disable CS0472 // The result of the expression is always 'value1' since a value of type 'value2' is never equal to 'null' of type 'value3'.

namespace FastMoq.Tests
{
    public class TestBaseTests : MockerTestBase<TestClass>
    {
        [Fact]
        public void CustomMocksTest()
        {
            var mock = new Mock<IFileSystem>();
            var mock2 = new Mock<IFile>();
            var mockModel = new MockModel<IFileSystem>(mock);
            var mockModel2 = new MockModel<IFile>(mock2);

            CustomMocks = new List<MockModel>
                {mockModel, mockModel2};

            var count = Mocks.mockCollection.Count;
            CreateComponent();
            Mocks.GetMockModelIndexOf(typeof(IFile), false).Should().Be(count + 1);
            Mocks.GetMockModelIndexOf(typeof(IFileSystem), false).Should().Be(count);
        }

        [Fact]
        public void GetMember_MustBeMemberName()
        {
            Component.GetMemberName(p => p.field2).Should().Be(Component.GetMember(p => p.field2).Name);
            Component.GetMemberName(p => p.field3).Should().Be(Component.GetMember(p => p.field3).Name);
            Component.GetMemberName(p => p.property4).Should().Be(Component.GetMember(p => p.property4).Name);

            Component.GetMember(p => p.field2).Name.Should().Be(nameof(TestClass.field2));
            Component.GetMember(p => p.field3).Name.Should().Be(nameof(TestClass.field3));
            Component.GetMember(p => p.property4).Name.Should().Be(nameof(TestClass.property4));
        }

        [Theory]
        [InlineData("sfield", 123)]
        [InlineData("field", 123)]
        [InlineData("field2", 111)]
        [InlineData("field3", 222)]
        public void GetPrivateFieldValue(string name, object expectedValue)
        {
            var member = Component.GetField(name);
            member.Should().NotBeNull();
            var value = Component.GetFieldValue(name);
            value.Should().NotBeNull();
            value.Should().Be(expectedValue);
        }

        [Theory]
        [InlineData("method", "test")]
        [InlineData("method2", "test2")]
        public void GetPrivateMethodValue(string name, object expectedValue)
        {
            var member = Component.GetMethod(name);
            member.Should().NotBeNull();
            var value = Component.GetMethodValue(name);
            value.Should().NotBeNull();
            value.Should().Be(expectedValue);
        }

        [Theory]
        [InlineData("sproperty", 456)]
        [InlineData("property", 456)]
        [InlineData("property2", 789)]
        [InlineData("property3", 789)]
        [InlineData("property4", 789)]
        public void GetPrivatePropertyValue(string name, object expectedValue)
        {
            var member = Component.GetProperty(name);
            member.Should().NotBeNull();
            var value = Component.GetPropertyValue(name);
            value.Should().NotBeNull();
            value.Should().Be(expectedValue);
        }

        [Theory]
        [InlineData("field", 333)]
        [InlineData("field2", 333)]
        [InlineData("field3", 333)]
        public void SetFieldValue(string name, object value)
        {
            var value1 = Component.GetFieldValue(name);
            Component.SetFieldValue(name, value);
            Component.GetFieldValue(name).Should().Be(value);
        }

        [Theory]
        [InlineData("property", 333, false)]
        [InlineData("property2", 333, true)]
        [InlineData("property3", 333, true)]
        [InlineData("property4", 333, true)]
        public void SetPropertyValue(string name, object value, bool getOnly)
        {
            var value1 = Component.GetPropertyValue(name);
            var a = () => Component.SetPropertyValue(name, value);

            if (getOnly)
            {
                a.Should()?.Throw<Exception>();
            }
            else
            {
                Component.SetPropertyValue(name, value);
                Component.GetPropertyValue(name).Should().Be(value);
            }
        }

        [Fact]
        public void TestMethod_ShouldThrow_WhenNull()
        {
            List<object?>? dataResult = null;

            TestMethodParametersAsync(x => x.TestMethod,
                (func, paramName, data, info) =>
                {
                    func.Should()?.ThrowAsync<ArgumentNullException>(paramName);
                    dataResult = data;
                },
                1,
                "2",
                new TestClass()
            );

            dataResult.Should().HaveCount(3);
        }

        [Fact]
        public void TestMethod2_ShouldThrow()
        {
            List<object?>? dataResult = null;

            TestMethodParametersAsync(x => x.TestMethod2,
                (func, paramName, data, info) =>
                {
                    func.Should()?.ThrowAsync<ArgumentNullException>(paramName);
                    dataResult = data;
                },
                TestClass.TestEnum.test2,
                "2"
            );

            dataResult.Should().HaveCount(2);
        }

        [Fact]
        public void TestMethod2Async_ShouldThrow()
        {
            List<object?>? dataResult = null;

            TestMethodParametersAsync(x => x.TestMethod2Async,
                (func, paramName, data, info) =>
                {
                    func.Should().ThrowAsync<ArgumentNullException>(paramName);
                    dataResult = data;
                },
                1,
                "2",
                new TestClass()
            );

            dataResult.Should().HaveCount(3);
        }

        [Fact]
        public void TestMethodAsync_MethodInfo_Null_ShouldThrow()
        {
            var obj = new TestBaseTestClass();
            new Action(() => obj.TestMethodParameters(null, (func, s, arg3, info) => { })).Should().Throw<ArgumentNullException>();
            new Action(() => obj.TestMethodParameters(obj.GetMethod("TestMethodParameters"), null)).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TestMethodAsync_MethodInfo_ShouldPass()
        {
            List<object?>? dataResult = null;
            var methodInfo = Component.GetType().GetMethod("TestMethodAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            TestMethodParametersAsync(methodInfo,
                (func, paramName, data, info) =>
                {
                    func.Should().ThrowAsync<ArgumentNullException>(paramName);
                    dataResult = data;
                },
                1,
                "2",
                new TestClass()
            );

            dataResult.Should().HaveCount(3);
        }

        [Fact]
        public void TestMethodAsync_ShouldThrow()
        {
            List<object?>? dataResult = null;

            TestMethodParametersAsync(x => x.TestMethodAsync,
                (func, paramName, data, info) =>
                {
                    func.Should().ThrowAsync<ArgumentNullException>(paramName);
                    dataResult = data;
                },
                1,
                "2",
                new TestClass()
            );

            dataResult.Should().HaveCount(3);
        }

        [Fact]
        public void WaitForTest()
        {
            var result1 = false;
            var result2 = false;

            var task1 = new Task(() =>
                {
                    Thread.Sleep(1000);
                    result1 = true;
                }
            );

            var task2 = new Task(() =>
                {
                    Thread.Sleep(500);
                    result2 = true;
                }
            );

            task1.Start();
            task2.Start();
            WaitFor(() => result1 && result2, TimeSpan.FromSeconds(2));
            result1.Should().BeTrue();
            result2.Should().BeTrue();
            task1.Dispose();
            task2.Dispose();
        }
    }

    public class ConstructorTestClass
    {
        public ConstructorTestClass()
        {

        }

        public ConstructorTestClass(IFileSystem fileSystem, string field)
        {
            ArgumentNullException.ThrowIfNull(fileSystem);
            ArgumentNullException.ThrowIfNull(field);
        }
    }

    public class TestBaseConstructorTestClass : MockerTestBase<ConstructorTestClass>
    {
        private readonly ITestOutputHelper output;
        public TestBaseConstructorTestClass(ITestOutputHelper output) => this.output = output;
        [Fact]
        public void TestConstructor()
        {
            TestConstructorParameters((action, c, p) =>
            {
                output?.WriteLine($"{c} - {p}");
                action
                    .Should()
                    .Throw<ArgumentNullException>()
                    .WithMessage($"*{p}*");
            });
        }

        [Fact]
        public void TestAllConstructors()
        {
            TestAllConstructorParameters((action, c, p) =>
            {
                output?.WriteLine($"{c} - {p}");
                action
                    .Should()
                    .Throw<ArgumentNullException>()
                    .WithMessage($"*{p}*");
            });
        }

        [Fact]
        public void TestConstructorInfo()
        {
            var constructors = typeof(ConstructorTestClass).GetConstructors();
            foreach (var constructorInfo in constructors)
            {
                TestConstructorParameters(constructorInfo, (action, c, p) =>
                {
                    output?.WriteLine($"{c} - {p}");
                    action
                        .Should()
                        .Throw<ArgumentNullException>()
                        .WithMessage($"*{p}*");
                });
            }
        }

        [Fact]
        public void TestConstructorInfo_Values()
        {
            var constructors = typeof(ConstructorTestClass).GetConstructors();
            foreach (var constructorInfo in constructors)
            {
                TestConstructorParameters(constructorInfo, (action, c, p) =>
                {
                    output?.WriteLine($"{c} - {p}");
                    action
                        .Should()
                        .Throw<ArgumentNullException>()
                        .WithMessage($"*{p}*");
                },
                    info => null,
                    info =>
                    {
                        return info switch
                        {
                            _ when info.ParameterType.Name == nameof(IFileSystem) => new FileSystem(),
                            _ => null,
                        };
                    }
                );
            }
        }
    }

    public class TestBaseTestClass : MockerTestBase<TestClass>
    {
        public void TestMethodParameters(MethodInfo methodInfo, Action<Func<Task>?, string?, List<object?>?, ParameterInfo> resultAction, params object?[]? args) =>
            TestMethodParametersAsync(methodInfo, resultAction, args);
    }

    public class TestClass
    {
        #region Fields

        private static readonly int sField = 123;
        public object field2 = 111;
        public int field3 = 222;

        private int field = sField;

        #endregion

        #region Properties

        public object property4 => property3;
        private object property { get; set; } = sProperty;
        private object property2 { get; } = 789;
        private int property3 => int.Parse(property2.ToString());

        private static int sProperty { get; } = 456;

        #endregion

        internal void TestMethod(int i, string s, TestClass c)
        {
            if (i == null)
            {
                throw new ArgumentNullException(nameof(i));
            }

            if (string.IsNullOrEmpty(s))
            {
                throw new ArgumentNullException(nameof(s));
            }

            if (c == null)
            {
                throw new ArgumentNullException(nameof(c));
            }
        }

        internal void TestMethod2(TestEnum testEnum, string s)
        {
            if (s == null)
            {
                throw new InvalidOperationException(nameof(s));
            }
        }

        internal async Task TestMethod2Async(int i, string s, TestClass c) => await TestMethodAsync(i, s, c);

        internal async Task TestMethodAsync(int? i, string s, TestClass c)
        {
            if (i == null)
            {
                throw new ArgumentNullException(nameof(i));
            }

            if (string.IsNullOrEmpty(s))
            {
                throw new ArgumentNullException(nameof(s));
            }

            if (c == null)
            {
                throw new ArgumentNullException(nameof(c));
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        private object method() => "test";
        private string method2() => "test2";

        internal enum TestEnum
        {
            test1,
            test2,
            test3,
            test4,
            test5,
            test6
        }
    }
}

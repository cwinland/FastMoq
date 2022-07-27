using FluentAssertions;
using System;
using Xunit;

#pragma warning disable CS8604
#pragma warning disable CS8602

namespace FastMoq.Tests
{
    public class TestBaseTests : MockerTestBase<TestClass>
    {
        [Fact]
        public void GetMember()
        {
            Component.GetMember(p => p.field2).Name.Should().Be("field2");
            Component.GetMember(p => p.field3).Name.Should().Be("field3");
            Component.GetMember(p => p.property4).Name.Should().Be("property4");
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
                a.Should().Throw<Exception>();
            }
            else
            {
                Component.SetPropertyValue(name, value);
                Component.GetPropertyValue(name).Should().Be(value);
            }
        }
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

        private static int sProperty { get; } = 456;
        private object property { get; set; } = sProperty;
        private object property2 { get; } = 789;
        private int property3 => int.Parse(property2.ToString());
        public object property4 => property3;

        #endregion

        private object method() => "test";
        private string method2() => "test2";
    }
}
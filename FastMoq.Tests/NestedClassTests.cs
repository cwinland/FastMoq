using FastMoq.Tests.TestClasses;

#pragma warning disable CS8604 // Possible null reference argument for parameter.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'.
#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable.
#pragma warning disable CS8974 // Converting method group to non-delegate type
#pragma warning disable CS0472 // The result of the expression is always 'value1' since a value of type 'value2' is never equal to 'null' of type 'value3'.

namespace FastMoq.Tests
{
    public class NestedClassTests
    {
        #region Fields

        private readonly Mocker mocks = new();

        #endregion

        [Fact]
        public void GetTypeFromInterface()
        {
            typeof(INestedTestClassBase).IsAssignableFrom(typeof(INestedTestClass)).Should().BeTrue();
            typeof(INestedTestClassBase).IsAssignableFrom(typeof(NestedTestClass)).Should().BeTrue();

            typeof(INestedTestClass).IsAssignableFrom(typeof(INestedTestClass)).Should().BeTrue();
            typeof(INestedTestClass).IsAssignableFrom(typeof(NestedTestClass)).Should().BeTrue();

            var r = mocks.GetTypeFromInterface<INestedTestClassBase>();
            var s = mocks.GetTypeFromInterface<INestedTestClass>();

            r.InstanceType.Should().Be(typeof(NestedTestClassBase));
            s.InstanceType.Should().Be(typeof(NestedTestClass));

            typeof(INestedTestClassBase).IsAssignableFrom(s.InstanceType).Should().BeTrue();
            typeof(INestedTestClassBase).IsAssignableFrom(r.InstanceType).Should().BeTrue();

            typeof(INestedTestClass).IsAssignableFrom(s.InstanceType).Should().BeTrue();
            typeof(INestedTestClass).IsAssignableFrom(r.InstanceType).Should().BeFalse();
        }
    }
}
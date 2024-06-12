using FastMoq.Extensions;
using FastMoq.Models;
using FastMoq.Tests.TestClasses;
using Xunit.Abstractions;

namespace FastMoq.Tests
{
    public class TestNormalTests(ITestOutputHelper outputWriter) : MockerTestBase<TestClassNormal>
    {
        [Fact]
        public void CanCreate()
        {
            Component.Should().NotBeNull();
            var value = (ConstructorModel)Mocks.ConstructorHistory[0].Value[0];
            value.ParameterList.Should().HaveCount(2); // Used constructor with 2 parameters instead of empty constructor.
        }

        // Check values for null
        [Fact]
        public void Service_NullArgChecks_AllConstructorsShouldPass() =>
            TestAllConstructorParameters((action, constructor, parameter) => action.EnsureNullCheckThrown(parameter, constructor, outputWriter));

        [Fact]
        public void Service_NullArgChecks_CurrentConstructorShouldPass() =>
            TestConstructorParameters((action, constructor, parameter) => action.EnsureNullCheckThrown(parameter, constructor, outputWriter));
    }
}

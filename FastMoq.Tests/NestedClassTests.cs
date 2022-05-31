using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace FastMoq.Tests
{
    public class NestedClassTests
    {
        private Mocks mocks;

        public NestedClassTests()
        {
            mocks = new Mocks();
        }

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

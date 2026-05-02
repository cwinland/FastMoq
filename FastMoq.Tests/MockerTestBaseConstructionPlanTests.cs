using FastMoq.Models;
using System;
using System.IO.Abstractions;
using AwesomeAssertionExtensions = AwesomeAssertions.AssertionExtensions;
using AwesomeEnumAssertionExtensions = AwesomeAssertions.EnumAssertionsExtensions;

namespace FastMoq.Tests
{
    public sealed class MockerTestBaseConstructionPlanTests
    {
        [Fact]
        public void GetComponentConstructionPlan_ShouldUseComponentConstructorParameterTypesHook()
        {
            var harness = new ConstructorTypesHarness();

            var plan = harness.DescribeComponentConstruction();

            AwesomeAssertionExtensions.Should(plan.RequestedType).Be(typeof(ConstructorSelectionTarget));
            AwesomeAssertionExtensions.Should(plan.ResolvedType).Be(typeof(ConstructorSelectionTarget));
            AwesomeAssertionExtensions.Should(plan.Parameters).HaveCount(2);
            AwesomeAssertionExtensions.Should(plan.Parameters[0].ParameterType).Be(typeof(IFileSystem));
            AwesomeAssertionExtensions.Should(plan.Parameters[1].ParameterType).Be(typeof(string));
        }

        [Fact]
        public void GetComponentConstructionPlan_ShouldUseComponentCreationFlags()
        {
            var harness = new NonPublicConstructorHarness();

            var plan = harness.DescribeComponentConstruction();

            AwesomeAssertionExtensions.Should(plan.UsedNonPublicConstructor).BeTrue();
            AwesomeAssertionExtensions.Should(plan.Parameters).HaveCount(1);
            AwesomeEnumAssertionExtensions.Should(plan.Parameters[0].Source).Be(InstanceConstructionParameterSource.AutoMock);
        }

        [Fact]
        public void GetComponentConstructionPlan_ShouldAllowExplicitRequestOverride_WhenCreateComponentActionDiverges()
        {
            var harness = new CustomRequestHarness();

            var plan = harness.DescribeComponentConstruction();

            AwesomeAssertionExtensions.Should(plan.Parameters).BeEmpty();
        }

        private sealed class ConstructorTypesHarness : MockerTestBase<ConstructorSelectionTarget>
        {
            protected override Type?[]? ComponentConstructorParameterTypes => [typeof(IFileSystem), typeof(string)];

            public InstanceConstructionPlan DescribeComponentConstruction() => GetComponentConstructionPlan();
        }

        private sealed class NonPublicConstructorHarness : MockerTestBase<NonPublicConstructorTarget>
        {
            protected override InstanceCreationFlags ComponentCreationFlags => InstanceCreationFlags.AllowNonPublicConstructorFallback;

            public InstanceConstructionPlan DescribeComponentConstruction() => GetComponentConstructionPlan();
        }

        private sealed class CustomRequestHarness : MockerTestBase<ManualConstructionTarget>
        {
            protected override Func<Mocker, ManualConstructionTarget> CreateComponentAction => _ => new ManualConstructionTarget();

            protected override InstanceConstructionRequest CreateComponentConstructionRequest() => new(typeof(ManualConstructionTarget))
            {
                ConstructorParameterTypes = [],
            };

            public InstanceConstructionPlan DescribeComponentConstruction() => GetComponentConstructionPlan();
        }

        private sealed class ConstructorSelectionTarget
        {
            public ConstructorSelectionTarget()
            {
            }

            public ConstructorSelectionTarget(IFileSystem fileSystem, string value)
            {
            }
        }

        public interface IDependency;

        private sealed class NonPublicConstructorTarget
        {
            private NonPublicConstructorTarget(IDependency dependency)
            {
            }
        }

        private sealed class ManualConstructionTarget
        {
            public ManualConstructionTarget()
            {
            }

            public ManualConstructionTarget(IDependency dependency)
            {
            }
        }
    }
}
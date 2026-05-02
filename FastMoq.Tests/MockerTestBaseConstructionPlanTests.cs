using FastMoq.Models;
using System;
using System.IO.Abstractions;

namespace FastMoq.Tests
{
    public sealed class MockerTestBaseConstructionPlanTests
    {
        [Fact]
        public void GetComponentConstructionPlan_ShouldUseComponentConstructorParameterTypesHook()
        {
            var harness = new ConstructorTypesHarness();

            var plan = harness.DescribeComponentConstruction();

            plan.RequestedType.Should().Be(typeof(ConstructorSelectionTarget));
            plan.ResolvedType.Should().Be(typeof(ConstructorSelectionTarget));
            plan.Parameters.Should().HaveCount(2);
            plan.Parameters[0].ParameterType.Should().Be(typeof(IFileSystem));
            plan.Parameters[1].ParameterType.Should().Be(typeof(string));
        }

        [Fact]
        public void GetComponentConstructionPlan_ShouldUseComponentCreationFlags()
        {
            var harness = new NonPublicConstructorHarness();

            var plan = harness.DescribeComponentConstruction();

            plan.UsedNonPublicConstructor.Should().BeTrue();
            plan.Parameters.Should().HaveCount(1);
            plan.Parameters[0].Source.Should().Be(InstanceConstructionParameterSource.AutoMock);
        }

        [Fact]
        public void GetComponentConstructionPlan_ShouldAllowExplicitRequestOverride_WhenCreateComponentActionDiverges()
        {
            var harness = new CustomRequestHarness();

            var plan = harness.DescribeComponentConstruction();

            plan.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void GetComponentHarnessBootstrapDescriptor_ShouldDescribeDefaultHookBootstrap()
        {
            var harness = new ConstructorTypesHarness();

            var descriptor = harness.DescribeComponentBootstrap();

            descriptor.RequiresExplicitConstructionRequestOverride.Should().BeFalse();
            descriptor.ComponentCreationFlags.Should().Be(InstanceCreationFlags.None);
            descriptor.ComponentConstructorParameterTypes.Should().NotBeNull();
            descriptor.ComponentConstructorParameterTypes!.Should().Equal([typeof(IFileSystem), typeof(string)]);
            descriptor.Graph.Root.Plan.Should().NotBeNull();
            descriptor.Graph.Root.Plan!.Parameters.Should().HaveCount(2);
        }

        [Fact]
        public void GetComponentHarnessBootstrapDescriptor_ShouldPreserveComponentCreationFlags()
        {
            var harness = new NonPublicConstructorHarness();

            var descriptor = harness.DescribeComponentBootstrap();

            descriptor.RequiresExplicitConstructionRequestOverride.Should().BeFalse();
            descriptor.ComponentCreationFlags.Should().Be(InstanceCreationFlags.AllowNonPublicConstructorFallback);
            descriptor.ComponentConstructorParameterTypes.Should().BeNull();
            descriptor.Graph.Root.Plan.Should().NotBeNull();
            descriptor.Graph.Root.Plan!.UsedNonPublicConstructor.Should().BeTrue();
        }

        [Fact]
        public void GetComponentHarnessBootstrapDescriptor_ShouldRequireExplicitRequestOverride_WhenHooksDoNotMatchRequest()
        {
            var harness = new CustomRequestHarness();

            var descriptor = harness.DescribeComponentBootstrap();

            descriptor.RequiresExplicitConstructionRequestOverride.Should().BeTrue();
            descriptor.ComponentCreationFlags.Should().Be(InstanceCreationFlags.None);
            descriptor.ComponentConstructorParameterTypes.Should().BeNull();
            descriptor.Graph.Request.ConstructorParameterTypes.Should().NotBeNull();
            descriptor.Graph.Request.ConstructorParameterTypes.Should().BeEmpty();
            descriptor.Graph.Root.Plan.Should().NotBeNull();
            descriptor.Graph.Root.Plan!.Parameters.Should().BeEmpty();
        }

        private sealed class ConstructorTypesHarness : MockerTestBase<ConstructorSelectionTarget>
        {
            protected override Type?[]? ComponentConstructorParameterTypes => [typeof(IFileSystem), typeof(string)];

            public InstanceConstructionPlan DescribeComponentConstruction() => GetComponentConstructionPlan();

            public ComponentHarnessBootstrapDescriptor DescribeComponentBootstrap() => GetComponentHarnessBootstrapDescriptor();
        }

        private sealed class NonPublicConstructorHarness : MockerTestBase<NonPublicConstructorTarget>
        {
            protected override InstanceCreationFlags ComponentCreationFlags => InstanceCreationFlags.AllowNonPublicConstructorFallback;

            public InstanceConstructionPlan DescribeComponentConstruction() => GetComponentConstructionPlan();

            public ComponentHarnessBootstrapDescriptor DescribeComponentBootstrap() => GetComponentHarnessBootstrapDescriptor();
        }

        private sealed class CustomRequestHarness : MockerTestBase<ManualConstructionTarget>
        {
            protected override Func<Mocker, ManualConstructionTarget> CreateComponentAction => _ => new ManualConstructionTarget();

            protected override InstanceConstructionRequest CreateComponentConstructionRequest() => new(typeof(ManualConstructionTarget))
            {
                ConstructorParameterTypes = [],
            };

            public InstanceConstructionPlan DescribeComponentConstruction() => GetComponentConstructionPlan();

            public ComponentHarnessBootstrapDescriptor DescribeComponentBootstrap() => GetComponentHarnessBootstrapDescriptor();
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
using FastMoq.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO.Abstractions;
using System.Linq;

namespace FastMoq.Tests
{
    public sealed class InstanceConstructionGraphTests
    {
        [Fact]
        public void CreateConstructionGraph_ShouldCreateRootAndOrderedDependencyNodes_FromConstructionPlan()
        {
            var mocker = new Mocker();

            var graph = mocker.CreateConstructionGraph(new InstanceConstructionRequest(typeof(TargetWithGraphDependencies)));

            graph.Request.RequestedType.Should().Be(typeof(TargetWithGraphDependencies));
            graph.Root.Kind.Should().Be(InstanceConstructionGraphNodeKind.Root);
            graph.Root.Plan.Should().NotBeNull();
            graph.Root.Plan!.ResolvedType.Should().Be(typeof(TargetWithGraphDependencies));
            graph.Nodes.Should().HaveCount(4);
            graph.Edges.Should().HaveCount(3);
            graph.Edges.Select(edge => edge.ParameterName).Should().Equal(["fileSystem", "dependency", "name"]);
            graph.Edges.Select(edge => edge.ParameterPosition).Should().Equal([0, 1, 2]);

            var dependencyNodes = graph.Nodes.Skip(1).ToArray();
            dependencyNodes[0].Kind.Should().Be(InstanceConstructionGraphNodeKind.Dependency);
            dependencyNodes[0].Parameter.Should().NotBeNull();
            dependencyNodes[0].NodeType.Should().Be(typeof(IFileSystem));
            dependencyNodes[0].Parameter!.Source.Should().Be(InstanceConstructionParameterSource.KnownType);
            dependencyNodes[1].NodeType.Should().Be(typeof(IDependency));
            dependencyNodes[1].Parameter!.Source.Should().Be(InstanceConstructionParameterSource.KeyedService);
            dependencyNodes[1].Parameter!.ServiceKey.Should().Be("primary");
            dependencyNodes[2].NodeType.Should().Be(typeof(string));
            dependencyNodes[2].Parameter!.Source.Should().Be(InstanceConstructionParameterSource.OptionalDefault);
        }

        [Fact]
        public void GetComponentConstructionGraph_ShouldMapHarnessHooksThroughGraphMetadata()
        {
            using var harness = new ConstructorTypesGraphHarness();

            var graph = harness.GetComponentConstructionGraph();

            graph.Request.ConstructorParameterTypes.Should().NotBeNull();
            graph.Request.ConstructorParameterTypes!.Should().Equal([typeof(IFileSystem), typeof(string)]);
            graph.Root.Plan.Should().NotBeNull();
            graph.Root.Plan!.Parameters.Should().HaveCount(2);
            graph.Edges.Select(edge => edge.ParameterName).Should().Equal(["fileSystem", "value"]);
            graph.Nodes.Skip(1).Select(node => node.NodeType).Should().Equal([typeof(IFileSystem), typeof(string)]);
        }

        private sealed class ConstructorTypesGraphHarness : MockerTestBase<ConstructorSelectionTarget>
        {
            protected override Type?[]? ComponentConstructorParameterTypes => [typeof(IFileSystem), typeof(string)];
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

        private sealed class TargetWithGraphDependencies
        {
            public TargetWithGraphDependencies(IFileSystem fileSystem, [FromKeyedServices("primary")] IDependency dependency, string name = "default")
            {
            }
        }
    }
}
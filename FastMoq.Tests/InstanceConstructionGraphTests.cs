using FastMoq.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO.Abstractions;
using System.Linq;
using AwesomeAssertionExtensions = AwesomeAssertions.AssertionExtensions;
using AwesomeEnumAssertionExtensions = AwesomeAssertions.EnumAssertionsExtensions;

namespace FastMoq.Tests
{
    public sealed class InstanceConstructionGraphTests
    {
        [Fact]
        public void CreateConstructionGraph_ShouldCreateRootAndOrderedDependencyNodes_FromConstructionPlan()
        {
            var mocker = new Mocker();

            var graph = mocker.CreateConstructionGraph(new InstanceConstructionRequest(typeof(TargetWithGraphDependencies)));

            AwesomeAssertionExtensions.Should(graph.Request.RequestedType).Be(typeof(TargetWithGraphDependencies));
            AwesomeEnumAssertionExtensions.Should(graph.Root.Kind).Be(InstanceConstructionGraphNodeKind.Root);
            AwesomeAssertionExtensions.Should(graph.Root.Plan).NotBeNull();
            AwesomeAssertionExtensions.Should(graph.Root.Plan!.ResolvedType).Be(typeof(TargetWithGraphDependencies));
            AwesomeAssertionExtensions.Should(graph.Nodes).HaveCount(4);
            AwesomeAssertionExtensions.Should(graph.Edges).HaveCount(3);
            AwesomeAssertionExtensions.Should(graph.Edges.Select(edge => edge.ParameterName)).Equal(["fileSystem", "dependency", "name"]);
            AwesomeAssertionExtensions.Should(graph.Edges.Select(edge => edge.ParameterPosition)).Equal([0, 1, 2]);

            var dependencyNodes = graph.Nodes.Skip(1).ToArray();
            AwesomeEnumAssertionExtensions.Should(dependencyNodes[0].Kind).Be(InstanceConstructionGraphNodeKind.Dependency);
            AwesomeAssertionExtensions.Should(dependencyNodes[0].Parameter).NotBeNull();
            AwesomeAssertionExtensions.Should(dependencyNodes[0].NodeType).Be(typeof(IFileSystem));
            AwesomeEnumAssertionExtensions.Should(dependencyNodes[0].Parameter!.Source).Be(InstanceConstructionParameterSource.KnownType);
            AwesomeAssertionExtensions.Should(dependencyNodes[1].NodeType).Be(typeof(IDependency));
            AwesomeEnumAssertionExtensions.Should(dependencyNodes[1].Parameter!.Source).Be(InstanceConstructionParameterSource.KeyedService);
            AwesomeAssertionExtensions.Should(dependencyNodes[1].Parameter!.ServiceKey).Be("primary");
            AwesomeAssertionExtensions.Should(dependencyNodes[2].NodeType).Be(typeof(string));
            AwesomeEnumAssertionExtensions.Should(dependencyNodes[2].Parameter!.Source).Be(InstanceConstructionParameterSource.OptionalDefault);
        }

        [Fact]
        public void GetComponentConstructionGraph_ShouldMapHarnessHooksThroughGraphMetadata()
        {
            var harness = new ConstructorTypesGraphHarness();

            var graph = harness.GetComponentConstructionGraph();

            AwesomeAssertionExtensions.Should(graph.Request.ConstructorParameterTypes).NotBeNull();
            AwesomeAssertionExtensions.Should(graph.Request.ConstructorParameterTypes!).Equal([typeof(IFileSystem), typeof(string)]);
            AwesomeAssertionExtensions.Should(graph.Root.Plan).NotBeNull();
            AwesomeAssertionExtensions.Should(graph.Root.Plan!.Parameters).HaveCount(2);
            AwesomeAssertionExtensions.Should(graph.Edges.Select(edge => edge.ParameterName)).Equal(["fileSystem", "value"]);
            AwesomeAssertionExtensions.Should(graph.Nodes.Skip(1).Select(node => node.NodeType)).Equal([typeof(IFileSystem), typeof(string)]);
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
namespace FastMoq.Models
{
    internal sealed class InstanceConstructionGraph
    {
        public InstanceConstructionGraph(
            InstanceConstructionRequest request,
            InstanceConstructionGraphNode root,
            IEnumerable<InstanceConstructionGraphNode> nodes,
            IEnumerable<InstanceConstructionGraphEdge> edges)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Nodes = [.. nodes ?? throw new ArgumentNullException(nameof(nodes))];
            Edges = [.. edges ?? throw new ArgumentNullException(nameof(edges))];
        }

        public InstanceConstructionRequest Request { get; }

        public InstanceConstructionGraphNode Root { get; }

        public IReadOnlyList<InstanceConstructionGraphNode> Nodes { get; }

        public IReadOnlyList<InstanceConstructionGraphEdge> Edges { get; }
    }

    internal sealed class InstanceConstructionGraphNode
    {
        public InstanceConstructionGraphNode(
            int id,
            Type nodeType,
            InstanceConstructionGraphNodeKind kind,
            InstanceConstructionPlan? plan = null,
            InstanceConstructionParameterPlan? parameter = null)
        {
            if (kind == InstanceConstructionGraphNodeKind.Root && plan == null)
            {
                throw new ArgumentException("Root graph nodes require a construction plan.", nameof(plan));
            }

            if (kind == InstanceConstructionGraphNodeKind.Dependency && parameter == null)
            {
                throw new ArgumentException("Dependency graph nodes require parameter metadata.", nameof(parameter));
            }

            Id = id;
            NodeType = nodeType ?? throw new ArgumentNullException(nameof(nodeType));
            Kind = kind;
            Plan = plan;
            Parameter = parameter;
        }

        public int Id { get; }

        public Type NodeType { get; }

        public InstanceConstructionGraphNodeKind Kind { get; }

        public InstanceConstructionPlan? Plan { get; }

        public InstanceConstructionParameterPlan? Parameter { get; }
    }

    internal sealed class InstanceConstructionGraphEdge
    {
        public InstanceConstructionGraphEdge(int fromNodeId, int toNodeId, int parameterPosition, string parameterName)
        {
            FromNodeId = fromNodeId;
            ToNodeId = toNodeId;
            ParameterPosition = parameterPosition;
            ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
        }

        public int FromNodeId { get; }

        public int ToNodeId { get; }

        public int ParameterPosition { get; }

        public string ParameterName { get; }
    }

    internal enum InstanceConstructionGraphNodeKind
    {
        Root = 0,
        Dependency = 1,
    }
}
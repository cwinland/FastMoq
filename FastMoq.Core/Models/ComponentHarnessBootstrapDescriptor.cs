namespace FastMoq.Models
{
    internal sealed class ComponentHarnessBootstrapDescriptor
    {
        public ComponentHarnessBootstrapDescriptor(
            InstanceConstructionGraph graph,
            InstanceCreationFlags componentCreationFlags,
            IEnumerable<Type?>? componentConstructorParameterTypes,
            bool requiresExplicitConstructionRequestOverride)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            ComponentCreationFlags = componentCreationFlags;
            ComponentConstructorParameterTypes = componentConstructorParameterTypes == null
                ? null
                : [.. componentConstructorParameterTypes];
            RequiresExplicitConstructionRequestOverride = requiresExplicitConstructionRequestOverride;
        }

        public InstanceConstructionGraph Graph { get; }

        public InstanceCreationFlags ComponentCreationFlags { get; }

        public IReadOnlyList<Type?>? ComponentConstructorParameterTypes { get; }

        public bool RequiresExplicitConstructionRequestOverride { get; }
    }
}

namespace FastMoq.Models
{
    /// <summary>
    /// Describes constructor-selection intent for a type that should be planned through FastMoq's runtime constructor-resolution pipeline.
    /// </summary>
    public sealed class InstanceConstructionRequest
    {
        /// <summary>
        /// Initializes a new request for the supplied requested type.
        /// </summary>
        /// <param name="requestedType">The requested service or concrete type whose constructor path should be planned.</param>
        public InstanceConstructionRequest(Type requestedType)
        {
            RequestedType = requestedType ?? throw new ArgumentNullException(nameof(requestedType));
        }

        /// <summary>
        /// Gets the requested service or concrete type whose constructor path should be planned.
        /// </summary>
        public Type RequestedType { get; }

        /// <summary>
        /// Gets or sets the exact constructor parameter types to match.
        /// Set this to <see langword="null" /> to use FastMoq's preferred-constructor selection rules.
        /// Set this to an empty array to request the parameterless constructor explicitly.
        /// </summary>
        public Type?[]? ConstructorParameterTypes { get; init; }

        /// <summary>
        /// Gets or sets whether constructor selection should stay on public constructors only.
        /// Set this to <see langword="null" /> to use the current <see cref="MockerPolicyOptions.DefaultFallbackToNonPublicConstructors" /> policy.
        /// </summary>
        public bool? PublicOnly { get; init; }

        /// <summary>
        /// Gets or sets how optional parameters should be resolved when FastMoq needs to supply values automatically.
        /// </summary>
        public OptionalParameterResolutionMode OptionalParameterResolution { get; init; } = OptionalParameterResolutionMode.UseDefaultOrNull;

        /// <summary>
        /// Gets or sets how constructor ambiguity should be handled when multiple equally viable constructors remain.
        /// </summary>
        public ConstructorAmbiguityBehavior ConstructorAmbiguityBehavior { get; init; } = ConstructorAmbiguityBehavior.Throw;
    }
}
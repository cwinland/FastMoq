namespace FastMoq.Models
{
    /// <summary>
    /// Describes the constructor-selection outcome FastMoq resolved for a requested type.
    /// </summary>
    public sealed class InstanceConstructionPlan
    {
        /// <summary>
        /// Initializes a new resolved constructor plan.
        /// </summary>
        /// <param name="requestedType">The originally requested type.</param>
        /// <param name="resolvedType">The concrete type whose constructor was selected.</param>
        /// <param name="usedNonPublicConstructor">True when the selected constructor is non-public.</param>
        /// <param name="usedPreferredConstructorAttribute">True when constructor selection was driven by <see cref="PreferredConstructorAttribute" />.</param>
        /// <param name="usedAmbiguityFallback">True when ambiguity resolution fell back to a parameterless constructor.</param>
        /// <param name="parameters">The selected constructor parameters in declaration order.</param>
        public InstanceConstructionPlan(
            Type requestedType,
            Type resolvedType,
            bool usedNonPublicConstructor,
            bool usedPreferredConstructorAttribute,
            bool usedAmbiguityFallback,
            IEnumerable<InstanceConstructionParameterPlan> parameters)
        {
            RequestedType = requestedType ?? throw new ArgumentNullException(nameof(requestedType));
            ResolvedType = resolvedType ?? throw new ArgumentNullException(nameof(resolvedType));
            UsedNonPublicConstructor = usedNonPublicConstructor;
            UsedPreferredConstructorAttribute = usedPreferredConstructorAttribute;
            UsedAmbiguityFallback = usedAmbiguityFallback;
            Parameters = [.. parameters ?? throw new ArgumentNullException(nameof(parameters))];
        }

        /// <summary>
        /// Gets the originally requested service or concrete type.
        /// </summary>
        public Type RequestedType { get; }

        /// <summary>
        /// Gets the concrete type whose constructor was selected.
        /// </summary>
        public Type ResolvedType { get; }

        /// <summary>
        /// Gets a value indicating whether the selected constructor is non-public.
        /// </summary>
        public bool UsedNonPublicConstructor { get; }

        /// <summary>
        /// Gets a value indicating whether constructor selection used <see cref="PreferredConstructorAttribute" />.
        /// </summary>
        public bool UsedPreferredConstructorAttribute { get; }

        /// <summary>
        /// Gets a value indicating whether ambiguity resolution fell back to a parameterless constructor.
        /// </summary>
        public bool UsedAmbiguityFallback { get; }

        /// <summary>
        /// Gets the selected constructor parameters in declaration order.
        /// </summary>
        public IReadOnlyList<InstanceConstructionParameterPlan> Parameters { get; }
    }

    /// <summary>
    /// Describes one selected constructor parameter in an <see cref="InstanceConstructionPlan" />.
    /// </summary>
    public sealed class InstanceConstructionParameterPlan
    {
        /// <summary>
        /// Initializes a new constructor-parameter plan entry.
        /// </summary>
        /// <param name="name">The constructor parameter name.</param>
        /// <param name="parameterType">The constructor parameter type.</param>
        /// <param name="position">The zero-based parameter position.</param>
        /// <param name="isOptional">True when the constructor parameter is optional.</param>
        /// <param name="optionalParameterResolution">The optional-parameter policy used for this parameter.</param>
        /// <param name="serviceKey">The DI-style service key when the parameter is keyed; otherwise <see langword="null" />.</param>
        /// <param name="source">The resolved source category FastMoq would use for the parameter.</param>
        public InstanceConstructionParameterPlan(
            string name,
            Type parameterType,
            int position,
            bool isOptional,
            OptionalParameterResolutionMode optionalParameterResolution,
            object? serviceKey,
            InstanceConstructionParameterSource source)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ParameterType = parameterType ?? throw new ArgumentNullException(nameof(parameterType));
            Position = position;
            IsOptional = isOptional;
            OptionalParameterResolution = optionalParameterResolution;
            ServiceKey = serviceKey;
            Source = source;
        }

        /// <summary>
        /// Gets the constructor parameter name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the constructor parameter type.
        /// </summary>
        public Type ParameterType { get; }

        /// <summary>
        /// Gets the zero-based constructor parameter position.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// Gets a value indicating whether the constructor parameter is optional.
        /// </summary>
        public bool IsOptional { get; }

        /// <summary>
        /// Gets the optional-parameter policy FastMoq would apply for this parameter.
        /// </summary>
        public OptionalParameterResolutionMode OptionalParameterResolution { get; }

        /// <summary>
        /// Gets the DI-style service key when the parameter is keyed.
        /// </summary>
        public object? ServiceKey { get; }

        /// <summary>
        /// Gets the resolved source category FastMoq would use for this parameter.
        /// </summary>
        public InstanceConstructionParameterSource Source { get; }
    }

    /// <summary>
    /// Describes the source category FastMoq would use for a selected constructor parameter.
    /// </summary>
    public enum InstanceConstructionParameterSource
    {
        /// <summary>
        /// The parameter resolves through a custom registration that provides its own instance or factory.
        /// </summary>
        CustomRegistration = 0,

        /// <summary>
        /// The parameter resolves through a built-in or custom known-type registration.
        /// </summary>
        KnownType = 1,

        /// <summary>
        /// The parameter resolves through keyed-service metadata.
        /// </summary>
        KeyedService = 2,

        /// <summary>
        /// The parameter resolves through FastMoq's tracked-mock creation path.
        /// </summary>
        AutoMock = 3,

        /// <summary>
        /// The parameter uses its declared optional default value or null.
        /// </summary>
        OptionalDefault = 4,

        /// <summary>
        /// The parameter falls back to the type default because FastMoq does not create a richer value.
        /// </summary>
        TypeDefault = 5,
    }
}
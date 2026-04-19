namespace FastMoq.Providers
{
    /// <summary>
    /// Describes a provider registration known to <see cref="MockingProviderRegistry"/>.
    /// </summary>
    public sealed class MockingProviderRegistrationInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockingProviderRegistrationInfo"/> class.
        /// </summary>
        /// <param name="name">The provider name used for registry lookup.</param>
        /// <param name="providerType">The concrete provider type registered under <paramref name="name"/>.</param>
        /// <param name="source">The registration path that added the provider.</param>
        public MockingProviderRegistrationInfo(string name, Type providerType, MockingProviderRegistrationSource source)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(providerType);

            Name = name;
            ProviderType = providerType;
            Source = source;
        }

        /// <summary>
        /// Gets the provider name used for registry lookup.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the concrete provider type registered under <see cref="Name"/>.
        /// </summary>
        public Type ProviderType { get; }

        /// <summary>
        /// Gets how the provider registration entered the registry.
        /// </summary>
        public MockingProviderRegistrationSource Source { get; }
    }

    /// <summary>
    /// Identifies how a provider registration entered <see cref="MockingProviderRegistry"/>.
    /// </summary>
    public enum MockingProviderRegistrationSource
    {
        /// <summary>
        /// The provider was registered as part of FastMoq's built-in registry initialization.
        /// </summary>
        BuiltIn,

        /// <summary>
        /// The provider was registered explicitly at runtime through <see cref="MockingProviderRegistry.Register(string, IMockingProvider, bool)"/>.
        /// </summary>
        RuntimeRegistration,

        /// <summary>
        /// The provider was registered from assembly metadata such as <see cref="FastMoqRegisterProviderAttribute"/>.
        /// </summary>
        AssemblyAttribute,

        /// <summary>
        /// The provider was registered through convention-based discovery.
        /// </summary>
        ConventionDiscovery,
    }
}
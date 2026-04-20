namespace FastMoq.Providers
{
    /// <summary>
    /// Controls how <see cref="MockingProviderRegistry"/> performs automatic provider discovery.
    /// </summary>
    public enum MockingProviderDiscoveryMode
    {
        /// <summary>
        /// Discovers providers from assemblies already loaded into the current process and from eligible direct references.
        /// This is the default mode.
        /// </summary>
        Automatic,

        /// <summary>
        /// Discovers providers only from assemblies that are already loaded into the current process.
        /// No additional assemblies are loaded during discovery.
        /// </summary>
        LoadedAssembliesOnly,

        /// <summary>
        /// Honors only built-in registrations, runtime registrations, and explicit assembly metadata.
        /// Convention-based IMockingProvider type discovery is disabled in this mode.
        /// </summary>
        ExplicitOnly,
    }
}

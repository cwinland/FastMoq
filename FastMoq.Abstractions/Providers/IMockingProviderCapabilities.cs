namespace FastMoq.Providers
{
    /// <summary>
    /// Describes optional features a concrete mocking provider supports so the framework can adapt behavior.
    /// </summary>
    public interface IMockingProviderCapabilities
    {
        /// <summary>
        /// Gets a value indicating whether the provider can call through to base implementations.
        /// </summary>
        bool SupportsCallBase { get; }

        /// <summary>
        /// Gets a value indicating whether the provider can automatically back settable properties.
        /// </summary>
        bool SupportsSetupAllProperties { get; }

        /// <summary>
        /// Gets a value indicating whether the provider can configure or verify protected members.
        /// </summary>
        bool SupportsProtectedMembers { get; }

        /// <summary>
        /// Gets a value indicating whether the provider can track performed invocations for verification.
        /// </summary>
        bool SupportsInvocationTracking { get; }

        /// <summary>
        /// Gets a value indicating whether the provider can capture logger invocations through framework helpers.
        /// </summary>
        bool SupportsLoggerCapture { get; }
    }
}
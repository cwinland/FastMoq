using System;

namespace FastMoq.Providers
{
    /// <summary>
    /// Describes optional features a concrete mocking provider supports so the framework can adapt behavior.
    /// </summary>
    public interface IMockingProviderCapabilities
    {
        bool SupportsCallBase { get; }
        bool SupportsStrict { get; }
        bool SupportsSetupAllProperties { get; }
        bool SupportsProtectedMembers { get; }
        bool SupportsInvocationTracking { get; }
    }
}

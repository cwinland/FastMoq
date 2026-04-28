namespace FastMoq.Providers
{
    /// <summary>
    /// Exposes the concrete provider instance that created the current <see cref="IFastMock" /> wrapper.
    /// Optional provider-extension interfaces can use this to opt into advanced behaviors without forcing every provider through the same surface.
    /// </summary>
    public interface IProviderBoundFastMock : IFastMock
    {
        /// <summary>
        /// Gets the provider that created and owns the current tracked mock wrapper.
        /// </summary>
        IMockingProvider Provider { get; }
    }
}
namespace FastMoq.Providers
{
    /// <summary>
    /// Non generic abstraction for a created mock instance.
    /// Prefer <see cref="Instance" /> and <see cref="Reset" /> for provider-first flows, and treat <see cref="NativeMock" /> as the provider-native escape hatch.
    /// </summary>
    public interface IFastMock
    {
        /// <summary>
        /// Gets the type being mocked.
        /// </summary>
        Type MockedType { get; }

        /// <summary>
        /// Gets the usable mocked instance.
        /// In provider-first migrations, this is the tracked replacement for older <c>GetMock&lt;T&gt;().Object</c> access.
        /// </summary>
        object Instance { get; }

        /// <summary>
        /// Gets the provider-specific underlying mock object.
        /// Prefer <see cref="Instance" /> and provider-first helpers first, and use this only when the test intentionally needs a provider-specific API surface.
        /// </summary>
        object NativeMock { get; }

        /// <summary>
        /// Resets or clears tracked mock state through the active provider.
        /// Use this provider-first path instead of calling provider-native reset APIs directly when the test can stay provider neutral.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Generic variant for convenience strongly typing <see cref="IFastMock.Instance" />.
    /// </summary>
    public interface IFastMock<T> : IFastMock where T : class
    {
        /// <summary>
        /// Gets the usable mocked instance with its concrete type.
        /// This is the strongly typed provider-first replacement for older tracked-mock <c>.Object</c> usage.
        /// </summary>
        new T Instance { get; }
    }
}
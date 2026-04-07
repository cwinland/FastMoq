namespace FastMoq.Providers
{
    /// <summary>
    /// Non generic abstraction for a created mock instance. Named IFastMock to avoid collision with provider library IMock types.
    /// </summary>
    public interface IFastMock
    {
        /// <summary>
        /// Gets the type being mocked.
        /// </summary>
        Type MockedType { get; }

        /// <summary>
        /// Gets the usable mocked instance.
        /// </summary>
        object Instance { get; }

        /// <summary>
        /// Gets the provider-specific underlying mock object.
        /// </summary>
        object NativeMock { get; }

        /// <summary>
        /// Clears configured state and recorded invocations on the mock.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Generic variant for convenience strongly typing Instance.
    /// </summary>
    public interface IFastMock<T> : IFastMock where T : class
    {
        /// <summary>
        /// Gets the usable mocked instance with its concrete type.
        /// </summary>
        new T Instance { get; }
    }
}
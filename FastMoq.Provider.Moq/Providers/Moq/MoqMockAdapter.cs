namespace FastMoq.Providers.MoqProvider
{
    /// <summary>
    /// Wraps a typed Moq mock in the provider-neutral FastMoq abstraction.
    /// </summary>
    public sealed class MoqMockAdapter<T> : global::FastMoq.Providers.IFastMock<T> where T : class
    {
        /// <summary>
        /// Initializes a new adapter for the supplied Moq mock.
        /// </summary>
        public MoqMockAdapter(global::Moq.Mock<T> inner)
        {
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>
        /// Gets the wrapped Moq mock.
        /// </summary>
        public global::Moq.Mock<T> Inner { get; }

        /// <summary>
        /// Gets the type being mocked.
        /// </summary>
        public Type MockedType => typeof(T);

        /// <summary>
        /// Gets the mocked instance.
        /// </summary>
        public T Instance => Inner.Object;

        /// <summary>
        /// Gets the provider-specific underlying mock object.
        /// </summary>
        public object NativeMock => Inner;
        object global::FastMoq.Providers.IFastMock.Instance => Instance!;

        /// <summary>
        /// Resets mock state when supported by the provider wrapper.
        /// </summary>
        public void Reset()
        {
        }
    }

    /// <summary>
    /// Wraps a non-generic Moq mock in the provider-neutral FastMoq abstraction.
    /// </summary>
    public sealed class MoqMockAdapter : global::FastMoq.Providers.IFastMock
    {
        /// <summary>
        /// Initializes a new adapter for the supplied Moq mock.
        /// </summary>
        public MoqMockAdapter(global::Moq.Mock inner)
        {
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>
        /// Gets the wrapped Moq mock.
        /// </summary>
        public global::Moq.Mock Inner { get; }

        /// <summary>
        /// Gets the runtime type being mocked.
        /// </summary>
        public Type MockedType => Inner.Object.GetType();

        /// <summary>
        /// Gets the mocked instance.
        /// </summary>
        public object Instance => Inner.Object;

        /// <summary>
        /// Gets the provider-specific underlying mock object.
        /// </summary>
        public object NativeMock => Inner;

        /// <summary>
        /// Resets mock state when supported by the provider wrapper.
        /// </summary>
        public void Reset()
        {
        }
    }
}
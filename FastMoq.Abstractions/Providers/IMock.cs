namespace FastMoq.Providers
{
    /// <summary>
    /// Non generic abstraction for a created mock instance. Named IFastMock to avoid collision with provider library IMock types.
    /// </summary>
    public interface IFastMock
    {
        Type MockedType { get; }
        object Instance { get; }
        object NativeMock { get; }
        void Reset();
    }

    /// <summary>
    /// Generic variant for convenience strongly typing Instance.
    /// </summary>
    public interface IFastMock<T> : IFastMock where T : class
    {
        new T Instance { get; }
    }
}
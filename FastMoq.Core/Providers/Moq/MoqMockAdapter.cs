using System;

namespace FastMoq.Core.Providers.MoqProvider
{
    internal sealed class MoqMockAdapter<T> : global::FastMoq.Providers.IFastMock<T> where T : class
    {
        public global::Moq.Mock<T> Inner { get; }
        public MoqMockAdapter(global::Moq.Mock<T> inner) => Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        public Type MockedType => typeof(T);
        public T Instance => Inner.Object;
        object global::FastMoq.Providers.IFastMock.Instance => Instance!;
        // Moq version in use does not expose a Reset API. Intentionally a no-op.
        public void Reset() { /* no-op */ }
    }

    internal sealed class MoqMockAdapter : global::FastMoq.Providers.IFastMock
    {
        public global::Moq.Mock Inner { get; }
        public MoqMockAdapter(global::Moq.Mock inner) => Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        public Type MockedType => Inner.Object.GetType();
        public object Instance => Inner.Object;
        // Moq version in use does not expose a Reset API. Intentionally a no-op.
        public void Reset() { /* no-op */ }
    }
}

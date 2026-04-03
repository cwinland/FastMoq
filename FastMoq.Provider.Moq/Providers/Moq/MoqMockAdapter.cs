using System;

namespace FastMoq.Providers.MoqProvider
{
    public sealed class MoqMockAdapter<T> : global::FastMoq.Providers.IFastMock<T> where T : class
    {
        public MoqMockAdapter(global::Moq.Mock<T> inner)
        {
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public global::Moq.Mock<T> Inner { get; }
        public Type MockedType => typeof(T);
        public T Instance => Inner.Object;
        public object NativeMock => Inner;
        object global::FastMoq.Providers.IFastMock.Instance => Instance!;

        public void Reset()
        {
        }
    }

    public sealed class MoqMockAdapter : global::FastMoq.Providers.IFastMock
    {
        public MoqMockAdapter(global::Moq.Mock inner)
        {
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public global::Moq.Mock Inner { get; }
        public Type MockedType => Inner.Object.GetType();
        public object Instance => Inner.Object;
        public object NativeMock => Inner;

        public void Reset()
        {
        }
    }
}
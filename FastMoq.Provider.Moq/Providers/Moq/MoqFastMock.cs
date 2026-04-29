namespace FastMoq.Providers.MoqProvider
{
    internal class MoqFastMock : IProviderBoundFastMock
    {
        protected readonly Mock _mock;

        public MoqFastMock(Mock mock)
        {
            _mock = mock ?? throw new ArgumentNullException(nameof(mock));
            MockedType = mock.GetType().GetGenericArguments()[0];
        }

        public Type MockedType { get; }
        public object Instance => _mock.Object;
        public object NativeMock => _mock;
        public IMockingProvider Provider => MoqMockingProvider.Instance;
        internal Mock InnerMock => _mock;

        public void Reset() => _mock.Reset();
    }

    internal sealed class MoqFastMockGeneric<T> : MoqFastMock, IFastMock<T> where T : class
    {
        private readonly Mock<T> _typed;

        public MoqFastMockGeneric(Mock<T> mock) : base(mock)
        {
            _typed = mock;
        }

        public new T Instance => _typed.Object;
        T IFastMock<T>.Instance => _typed.Object;
    }
}
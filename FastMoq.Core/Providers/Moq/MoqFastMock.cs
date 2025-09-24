using System;
using Moq;
using FastMoq.Providers;

namespace FastMoq.Providers.MoqProvider
{
    internal class MoqFastMock : IFastMock
    {
        public Type MockedType { get; }
        public object Instance => _mock.Object;
        protected readonly Mock _mock;
        internal Mock InnerMock => _mock;
        public MoqFastMock(Mock mock)
        {
            _mock = mock ?? throw new ArgumentNullException(nameof(mock));
            MockedType = mock.GetType().GetGenericArguments()[0];
        }
        public void Reset() => _mock.Reset();
    }

    internal sealed class MoqFastMockGeneric<T> : MoqFastMock, IFastMock<T> where T : class
    {
        private readonly Mock<T> _typed;
        public new T Instance => _typed.Object;
        public MoqFastMockGeneric(Mock<T> mock) : base(mock) { _typed = mock; }
        T IFastMock<T>.Instance => _typed.Object; // explicit to satisfy interface & hide new keyword warning
    }
}

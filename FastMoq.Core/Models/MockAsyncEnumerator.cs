namespace FastMoq.Models
{
    class MockAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public T Current => _inner.Current;

        public MockAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }
        public ValueTask DisposeAsync()
        {
            _inner.Dispose();

            return new ValueTask();
        }
    }
}
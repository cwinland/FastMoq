namespace FastMoq.Models
{
    public class MockAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> inner;

        public MockAsyncEnumerator(IEnumerator<T> inner) => this.inner = inner;

        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return new ValueTask();
        }

        public T Current => inner.Current;
        public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());
    }
}
namespace FastMoq.Models
{
    /// <summary>
    /// Adapts a synchronous enumerator to <see cref="IAsyncEnumerator{T}"/> for EF Core async query testing.
    /// </summary>
    /// <typeparam name="T">The element type returned by the enumerator.</typeparam>
    public class MockAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> inner;

        /// <summary>
        /// Initializes the async enumerator wrapper.
        /// </summary>
        /// <param name="inner">The underlying synchronous enumerator.</param>
        public MockAsyncEnumerator(IEnumerator<T> inner) => this.inner = inner;

        /// <summary>
        /// Disposes the underlying synchronous enumerator.
        /// </summary>
        /// <returns>A completed <see cref="ValueTask"/>.</returns>
        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return new ValueTask();
        }

        /// <summary>
        /// Gets the current element in the underlying enumeration.
        /// </summary>
        public T Current => inner.Current;

        /// <summary>
        /// Advances the underlying enumerator to the next element.
        /// </summary>
        /// <returns>A <see cref="ValueTask{TResult}"/> whose result indicates whether the enumerator advanced successfully.</returns>
        public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());
    }
}
namespace FastMoq.Models
{
    /// <exclude />
    public class MockAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        #region Fields

        /// <summary>
        ///     The inner
        /// </summary>
        private readonly IEnumerator<T> inner;

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockAsyncEnumerator{T}" /> class.
        /// </summary>
        /// <param name="inner">The inner.</param>
        public MockAsyncEnumerator(IEnumerator<T> inner) => this.inner = inner;

        #region IAsyncDisposable

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            inner.Dispose();

            return new ValueTask();
        }

        #endregion

        #region IAsyncEnumerator<T>

        /// <inheritdoc />
        public T Current => inner.Current;

        /// <inheritdoc />
        public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());

        #endregion
    }
}

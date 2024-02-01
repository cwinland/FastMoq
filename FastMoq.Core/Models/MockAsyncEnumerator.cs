namespace FastMoq.Models
{
    /// <summary>
    ///     Class MockAsyncEnumerator.
    ///     Implements the <see cref="System.Collections.Generic.IAsyncEnumerator{T}" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <inheritdoc />
    /// <seealso cref="System.Collections.Generic.IAsyncEnumerator{T}" />
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

        /// <summary>
        ///     Advances the enumerator asynchronously to the next element of the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Threading.Tasks.ValueTask`1" /> that will complete with a result of
        ///     <see langword="true" /> if the enumerator was successfully advanced to the next element, or
        ///     <see langword="false" /> if the enumerator has passed the end of the collection.
        /// </returns>
        /// <inheritdoc />
        public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());

        #endregion
    }
}

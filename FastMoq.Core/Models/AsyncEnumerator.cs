namespace FastMoq.Models
{
    /// <summary>
    ///     Class AsyncEnumerator.
    ///     Implements the <see cref="System.Collections.Generic.IAsyncEnumerator{T}" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <inheritdoc />
    /// <seealso cref="System.Collections.Generic.IAsyncEnumerator{T}" />
    public class AsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        #region Fields

        private readonly IEnumerator<T> enumerator;

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="AsyncEnumerator{T}" /> class.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public AsyncEnumerator(IEnumerator<T> enumerator) =>
            this.enumerator = enumerator ?? throw new ArgumentNullException();

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        ///     Moves the next.
        /// </summary>
        /// <param name="cancellationToken">
        ///     The cancellation token that can be used by other objects or threads to receive notice
        ///     of cancellation.
        /// </param>
        /// <returns>Moves the next.</returns>
        public Task<bool> MoveNext(CancellationToken cancellationToken) =>
            Task.FromResult(enumerator.MoveNext());

        #region IAsyncDisposable

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        ///     asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public ValueTask DisposeAsync() => throw new NotImplementedException();

        #endregion

        #region IAsyncEnumerator<T>

        /// <summary>
        ///     Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <value>The current.</value>
        public T Current => enumerator.Current;

        /// <summary>
        ///     Advances the enumerator asynchronously to the next element of the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Threading.Tasks.ValueTask`1" /> that will complete with a result of
        ///     <see langword="true" /> if the enumerator was successfully advanced to the next element, or
        ///     <see langword="false" /> if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public ValueTask<bool> MoveNextAsync() => throw new NotImplementedException();

        #endregion
    }
}

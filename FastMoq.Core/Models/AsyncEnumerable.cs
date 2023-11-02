using System.Linq.Expressions;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class AsyncEnumerable.
    ///     Implements the <see cref="System.Linq.EnumerableQuery{T}" />
    ///     Implements the <see cref="System.Collections.Generic.IAsyncEnumerable{T}" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <inheritDoc cref="System.Linq.EnumerableQuery{T}" />
    /// <inheritDoc cref="System.Collections.Generic.IAsyncEnumerable{T}" />
    /// <seealso cref="System.Linq.EnumerableQuery{T}" />
    /// <seealso cref="System.Collections.Generic.IAsyncEnumerable{T}" />
    public class AsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AsyncEnumerable{T}" /> class.
        /// </summary>
        /// <param name="expression">An expression tree to associate with the new instance.</param>
        public AsyncEnumerable(Expression expression)
            : base(expression) { }

        /// <summary>
        ///     Gets the enumerator.
        /// </summary>
        /// <returns>IAsyncEnumerator&lt;T&gt; of the enumerator.</returns>
        public IAsyncEnumerator<T> GetEnumerator() =>
            new AsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        #region IAsyncEnumerable<T>

        /// <summary>
        ///     Returns an enumerator that iterates asynchronously through the collection.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A <see cref="T:System.Threading.CancellationToken" /> that may be used to cancel the
        ///     asynchronous iteration.
        /// </param>
        /// <returns>An enumerator that can be used to iterate asynchronously through the collection.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        #endregion
    }
}

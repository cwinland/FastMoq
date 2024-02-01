using System.Linq.Expressions;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class MockAsyncEnumerable.
    ///     Implements the <see cref="System.Linq.EnumerableQuery{T}" />
    ///     Implements the <see cref="System.Collections.Generic.IAsyncEnumerable{T}" />
    ///     Implements the <see cref="System.Linq.IQueryable{T}" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <inheritdoc cref="System.Linq.EnumerableQuery{T}" />
    /// <inheritdoc cref="System.Collections.Generic.IAsyncEnumerable{T}" />
    /// <inheritdoc cref="System.Linq.IQueryable{T}" />
    /// <seealso cref="System.Linq.EnumerableQuery{T}" />
    /// <seealso cref="System.Collections.Generic.IAsyncEnumerable{T}" />
    /// <seealso cref="System.Linq.IQueryable{T}" />
    public class MockAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        /// <inheritdoc />
        public MockAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }

        /// <inheritdoc />
        public MockAsyncEnumerable(Expression expression) : base(expression) { }

        #region IAsyncEnumerable<T>

        /// <inheritdoc />
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new MockAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        #endregion

        #region IQueryable

        /// <inheritdoc />
        IQueryProvider IQueryable.Provider => new MockAsyncQueryProvider<T>(this);

        #endregion
    }
}

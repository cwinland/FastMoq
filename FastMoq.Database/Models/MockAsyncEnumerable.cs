using System.Linq.Expressions;

namespace FastMoq.Models
{
    /// <summary>
    /// Wraps an enumerable or expression tree so EF Core async query APIs can enumerate it through <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type exposed by the queryable sequence.</typeparam>
    public class MockAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        /// <summary>
        /// Initializes the async enumerable from an existing in-memory sequence.
        /// </summary>
        /// <param name="enumerable">The sequence to expose through async query APIs.</param>
        public MockAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }

        /// <summary>
        /// Initializes the async enumerable from an expression tree.
        /// </summary>
        /// <param name="expression">The expression tree that defines the query.</param>
        public MockAsyncEnumerable(Expression expression) : base(expression) { }

        /// <summary>
        /// Returns an async enumerator over the wrapped sequence.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that is ignored by this in-memory test implementation.</param>
        /// <returns>An async enumerator over the wrapped sequence.</returns>
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new MockAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IQueryProvider IQueryable.Provider => new MockAsyncQueryProvider<T>(this);
    }
}
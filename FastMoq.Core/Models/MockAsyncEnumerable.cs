using System.Linq.Expressions;

namespace FastMoq.Models
{
    /// <exclude />
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

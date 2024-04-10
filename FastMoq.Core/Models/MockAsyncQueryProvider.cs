using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class MockAsyncQueryProvider.
    ///     Implements the <see cref="IAsyncQueryProvider" /></summary>
    /// <typeparam name="TEntity">The type of the t entity.</typeparam>
    /// <inheritdoc />
    /// <seealso cref="IAsyncQueryProvider" />
    internal class MockAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider inner;

        internal MockAsyncQueryProvider(IQueryProvider inner)
        {
            this.inner = inner;
        }

        /// <inheritdoc />
        public IQueryable CreateQuery(Expression expression)
        {
            return new MockAsyncEnumerable<TEntity>(expression);
        }

        /// <inheritdoc />
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new MockAsyncEnumerable<TElement>(expression);
        }

        /// <inheritdoc />
        public object? Execute(Expression expression)
        {
            return inner.Execute(expression);
        }

        /// <inheritdoc />
        public TResult Execute<TResult>(Expression expression)
        {
            return inner.Execute<TResult>(expression);
        }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethod(name: nameof(IQueryProvider.Execute), genericParameterCount: 1, [typeof(Expression)])
                .MakeGenericMethod(expectedResultType)
                .Invoke(this, [expression]);

            return (TResult) typeof(Task)
                .GetMethod(nameof(Task.FromResult))
                ?.MakeGenericMethod(expectedResultType)
                .Invoke(null, [executionResult]);
        }
    }
}
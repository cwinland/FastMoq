using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class DbAsyncQueryProvider.
    ///     Implements the <see cref="IAsyncQueryProvider" />
    /// </summary>
    /// <typeparam name="TEntity">The type of the t entity.</typeparam>
    /// <inheritdoc />
    /// <seealso cref="IAsyncQueryProvider" />
    public class DbAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        #region Fields

        private readonly IQueryProvider _inner;

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbAsyncQueryProvider{TEntity}" /> class.
        /// </summary>
        /// <param name="inner">The inner.</param>
        public DbAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

        /// <summary>
        ///     Executes the asynchronous.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="cancellationToken">
        ///     The cancellation token that can be used by other objects or threads to receive notice
        ///     of cancellation.
        /// </param>
        /// <returns>Executes the asynchronous.</returns>
        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken) => Task.FromResult(Execute(expression));

        /// <summary>
        ///     Executes the asynchronous.
        /// </summary>
        /// <typeparam name="TResult">The type of the t result.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <param name="cancellationToken">
        ///     The cancellation token that can be used by other objects or threads to receive notice
        ///     of cancellation.
        /// </param>
        /// <returns>Executes the asynchronous.</returns>
        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) =>
            Task.FromResult(Execute<TResult>(expression));

        #region IAsyncQueryProvider

        TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        #endregion

        #region IQueryProvider

        /// <summary>
        ///     Creates the query.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>Creates the query.</returns>
        public IQueryable CreateQuery(Expression expression)
        {
            if (expression is MethodCallExpression methodCallExpression)
            {
                var resultType = methodCallExpression.Method.ReturnType;
                var genericElement = resultType.GetGenericArguments()[0];
                var queryType = typeof(AsyncEnumerable<>).MakeGenericType(genericElement);
                return (IQueryable) Activator.CreateInstance(queryType, expression);
            }

            return new AsyncEnumerable<TEntity>(expression);
        }

        /// <summary>
        ///     Creates the query.
        /// </summary>
        /// <typeparam name="TElement">The type of the t element.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns>Creates the query.</returns>
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new AsyncEnumerable<TElement>(expression);

        /// <summary>
        ///     Executes the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>Executes.</returns>
        public object Execute(Expression expression) => _inner.Execute(expression);

        /// <summary>
        ///     Executes the specified expression.
        /// </summary>
        /// <typeparam name="TResult">The type of the t result.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns>Executes.</returns>
        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

        #endregion
    }
}

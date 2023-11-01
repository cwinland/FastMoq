using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Moq.Language.Flow;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Models
{
    public class FastMoqNonVoidSetupPhrase<T>
    {
        private readonly object setupPhrase;
        public FastMoqNonVoidSetupPhrase(object setupPhrase)
        {
            this.setupPhrase = setupPhrase;
        }


        public IReturnsResult<T> Returns(Delegate value, Type resultType)
        {
            var findType = typeof(Func<>);
            var genericFindType = findType.MakeGenericType(resultType);
            genericFindType = typeof(Delegate);

            var returnsMethod = setupPhrase.GetType().GetMethod("Returns", BindingFlags.Public | BindingFlags.Instance, new Type[] { genericFindType });

            var returnObj = returnsMethod.Invoke(setupPhrase, new object[] { value });
            return returnObj as IReturnsResult<T>;
        }

    }

    public class AsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>
    {
        public AsyncEnumerable(Expression expression)
            : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public IAsyncEnumerator<T> GetEnumerator() =>
            new AsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    public class AsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> enumerator;

        public AsyncEnumerator(IEnumerator<T> enumerator) =>
            this.enumerator = enumerator ?? throw new ArgumentNullException();

        public T Current => enumerator.Current;

        public void Dispose() { }

        public ValueTask DisposeAsync() => throw new NotImplementedException();

        public Task<bool> MoveNext(CancellationToken cancellationToken) =>
            Task.FromResult(enumerator.MoveNext());

        public ValueTask<bool> MoveNextAsync() => throw new NotImplementedException();
    }

    public class DbAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public DbAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

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

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new AsyncEnumerable<TElement>(expression);

        public object Execute(Expression expression) => _inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken) => Task.FromResult(Execute(expression));

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) =>
            Task.FromResult(Execute<TResult>(expression));

        TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    
    public class MockDbContextOptions<T> : Mock<DbContextOptions<T>> where T : DbContext
    {

    }
    public class DbSetMock<TEntity> : Mock<DbSet<TEntity>>
        where TEntity : class
    {
        private List<TEntity> _store = new();

        public DbSetMock() : this(null) { }
        public DbSetMock(IList<TEntity>? initialData)
        {
            if (initialData != null)
            {
                _store.AddRange(initialData);
            }

            var data = _store.AsQueryable();
            As<IQueryable<TEntity>>().Setup(x => x.Provider).Returns(data.Provider);
            As<IQueryable<TEntity>>().Setup(x => x.Expression).Returns(data.Expression);
            As<IQueryable<TEntity>>().Setup(x => x.ElementType).Returns(data.ElementType);
            As<IQueryable<TEntity>>().Setup(x => x.GetEnumerator()).Returns(() => data.GetEnumerator());
            As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(() => data.GetEnumerator());

            Setup(x => x.Add(It.IsAny<TEntity>())).Callback<TEntity>(_store.Add);
            Setup(x => x.AddRange(It.IsAny<IEnumerable<TEntity>>())).Callback<IEnumerable<TEntity>>(_store.AddRange);
            Setup(x => x.Remove(It.IsAny<TEntity>())).Callback<TEntity>(x => _store.Remove(x));

            Setup(x => x.RemoveRange(It.IsAny<IEnumerable<TEntity>>()))
                .Callback<IEnumerable<TEntity>>(x => x.ToList().ForEach(y => _store.Remove(y)));

            Setup(x => x.Find(It.IsAny<object[]>())).Returns<object[]>(x => _store.Find(y => y.Equals(x)));
        }
    }
}
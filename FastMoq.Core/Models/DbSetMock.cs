using Microsoft.EntityFrameworkCore;
using Moq;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class DbSetMock.
    ///     Implements the <see cref="Mock{T}" />
    /// </summary>
    /// <typeparam name="TEntity">The type of the t entity.</typeparam>
    /// <inheritdoc />
    /// <seealso cref="Mock{T}" />
    public class DbSetMock<TEntity> : Mock<DbSet<TEntity>> where TEntity : class
    {
        #region Fields

        private readonly List<TEntity> store = new();

        #endregion

        #region Properties

        private Mock<IAsyncEnumerable<TEntity>> AsyncMock =>
            As<IAsyncEnumerable<TEntity>>() ?? throw new InvalidOperationException("Unable to get Async Enumerable.");

        private Mock<IQueryable<TEntity>> QueryableMock =>
            As<IQueryable<TEntity>>() ?? throw new InvalidOperationException("Unable to get IQueryable.");

        #endregion

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.Models.DbSetMock`1" /> class.
        /// </summary>
        public DbSetMock() : this(new List<TEntity>()) { }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.Models.DbSetMock`1" /> class.
        /// </summary>
        /// <param name="initialData">The initial data.</param>
        public DbSetMock(IList<TEntity> initialData)
        {
            if (initialData != null)
            {
                store.AddRange(initialData);
            }

            var data = store.AsQueryable();

            QueryableMock.Setup(x => x.Provider).Returns(() => data.Provider);
            QueryableMock.Setup(x => x.Expression).Returns(() => data.Expression);
            QueryableMock.Setup(x => x.ElementType).Returns(() => data.ElementType);
            QueryableMock.Setup(x => x.GetEnumerator()).Returns(data.GetEnumerator);

            AsyncMock.Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new MockAsyncEnumerator<TEntity>(data.GetEnumerator()));

        }

        public virtual void SetupListMethods()
        {
            var data = store.AsQueryable();

            Setup(x => x.AsQueryable()).Returns(data);
            Setup(x => x.Add(It.IsAny<TEntity>())).Callback<TEntity>(store.Add);
            Setup(x => x.AddRange(It.IsAny<IEnumerable<TEntity>>())).Callback<IEnumerable<TEntity>>(store.AddRange);
            Setup(x => x.Remove(It.IsAny<TEntity>())).Callback<TEntity>(x => store.Remove(x));

            Setup(x => x.RemoveRange(It.IsAny<IEnumerable<TEntity>>()))
                .Callback<IEnumerable<TEntity>>(x => x.ToList().ForEach(y => store.Remove(y)));

            Setup(x => x.Find(It.IsAny<object[]>())).Returns<object[]>(x => store.Find(y => y.Equals(x)));
        }

        public virtual void SetupAsyncListMethods()
        {
            var data = store.AsQueryable();

            Setup(x => x.AsAsyncEnumerable()).Returns(() => new MockAsyncEnumerable<TEntity>(data));
            Setup(x => x.AddAsync(It.IsAny<TEntity>(), It.IsAny<CancellationToken>())).Callback<TEntity, CancellationToken>((e, c) => store.Add(e));
            Setup(m => m.FindAsync(It.IsAny<object[]>())).Returns(new ValueTask<TEntity>(data.FirstOrDefault()));
            Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(new ValueTask<TEntity>(data.FirstOrDefault()));

            Setup(m => m.AddRangeAsync(It.IsAny<IEnumerable<TEntity>>(), It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<TEntity> source, CancellationToken token) => store.AddRange(source))
                .Returns(Task.CompletedTask);

            Setup(m => m.AddRangeAsync(It.IsAny<TEntity[]>()))
                .Callback((TEntity[] source) => store.AddRange(source))
                .Returns(Task.CompletedTask);
        }
    }
}

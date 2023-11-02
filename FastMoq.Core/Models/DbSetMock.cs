using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class DbSetMock.
    ///     Implements the <see cref="Mock{T}" />
    /// </summary>
    /// <typeparam name="TEntity">The type of the t entity.</typeparam>
    /// <inheritdoc />
    /// <seealso cref="Mock{DbSet{TEntity}}" />
    public class DbSetMock<TEntity> : Mock<DbSet<TEntity>>
        where TEntity : class
    {
        #region Fields

        private readonly List<TEntity> _store = new();

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbSetMock{TEntity}" /> class.
        /// </summary>
        public DbSetMock() : this(null) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbSetMock{TEntity}" /> class.
        /// </summary>
        /// <param name="initialData">The initial data.</param>
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

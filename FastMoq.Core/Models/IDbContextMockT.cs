using Microsoft.EntityFrameworkCore;

namespace FastMoq.Models
{
    /// <inheritdoc />
    /// <summary>
    ///     Interface IDbContextMock
    ///     Implements the <see cref="T:FastMoq.Models.IDbContextMock" />
    /// </summary>
    /// <typeparam name="TEntity">The type of the t entity.</typeparam>
    /// <seealso cref="T:FastMoq.Models.IDbContextMock" />
    public interface IDbContextMock<TEntity> : IDbContextMock where TEntity : DbContext
    {
        /// <summary>
        ///     Create Mock Setup for the database sets that are marked as virtual.
        /// </summary>
        /// <param name="mocks">The <see cref="Mocker" /> from test context.</param>
        /// <returns>DbContext Mock.</returns>
        DbContextMock<TEntity> SetupDbSets(Mocker mocks);
    }
}

namespace FastMoq.Models
{
    /// <summary>
    /// Describes a strongly typed FastMoq DbContext mock wrapper.
    /// </summary>
    /// <typeparam name="TEntity">The concrete DbContext type being mocked.</typeparam>
    public interface IDbContextMock<TEntity> : IDbContextMock where TEntity : DbContext
    {
        /// <summary>
        /// Configures all DbSet properties and related helper methods for the current DbContext mock.
        /// </summary>
        /// <param name="mocks">The owning mocker used to resolve nested set dependencies.</param>
        /// <returns>The current DbContext mock instance for fluent chaining.</returns>
        DbContextMock<TEntity> SetupDbSets(Mocker mocks);
    }
}
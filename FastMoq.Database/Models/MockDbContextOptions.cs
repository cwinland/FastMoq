namespace FastMoq.Models
{
    /// <summary>
    /// Legacy Moq wrapper for <see cref="DbContextOptions{TContext}"/> used by older DbContext mock creation paths.
    /// </summary>
    /// <typeparam name="T">The DbContext type associated with the options object.</typeparam>
    public class MockDbContextOptions<T> : Mock<DbContextOptions<T>> where T : DbContext
    {
    }
}
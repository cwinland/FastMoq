namespace FastMoq.Models
{
    /// <summary>
    /// Describes the setup operations used to prepare DbSet mocks for synchronous and asynchronous query execution.
    /// </summary>
    public interface IDbSetMock
    {
        /// <summary>
        /// Configures the asynchronous enumeration members required for EF Core async query support.
        /// </summary>
        void SetupAsyncListMethods();

        /// <summary>
        /// Configures the synchronous list and query members on the DbSet mock.
        /// </summary>
        void SetupListMethods();

        /// <summary>
        /// Runs the full default setup pipeline for the DbSet mock.
        /// </summary>
        void SetupMockMethods();
    }
}
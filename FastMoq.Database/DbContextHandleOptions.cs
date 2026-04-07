using Microsoft.EntityFrameworkCore;

namespace FastMoq
{
    /// <summary>
    /// Options for creating DbContext test handles.
    /// </summary>
    public sealed class DbContextHandleOptions<TContext> where TContext : DbContext
    {
        /// <summary>
        /// Gets or sets the DbContext provisioning mode.
        /// </summary>
        public DbContextTestMode Mode { get; set; } = DbContextTestMode.MockedSets;

        /// <summary>
        /// Gets or sets the database name used for the real in-memory mode.
        /// </summary>
        public string? DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets an optional factory for constructing a real DbContext instance.
        /// </summary>
        public Func<DbContextOptions<TContext>, TContext>? RealContextFactory { get; set; }
    }
}
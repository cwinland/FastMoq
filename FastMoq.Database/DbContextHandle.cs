using FastMoq.Models;
using Microsoft.EntityFrameworkCore;

namespace FastMoq
{
    /// <summary>
    /// Represents a DbContext test double created by FastMoq.
    /// </summary>
    public sealed class DbContextHandle<TContext> where TContext : DbContext
    {
        internal DbContextHandle(DbContextTestMode mode, TContext context, DbContextMock<TContext>? mock)
        {
            Mode = mode;
            Context = context;
            Mock = mock;
        }

        /// <summary>
        /// Gets the provisioning mode used for this handle.
        /// </summary>
        public DbContextTestMode Mode { get; }

        /// <summary>
        /// Gets the resolved DbContext instance.
        /// </summary>
        public TContext Context { get; }

        /// <summary>
        /// Gets the DbContext mock when the handle is in mocked-set mode.
        /// </summary>
        public DbContextMock<TContext>? Mock { get; }

        /// <summary>
        /// Gets a value indicating whether this handle is backed by a DbContext mock.
        /// </summary>
        public bool IsMocked => Mock != null;
    }
}
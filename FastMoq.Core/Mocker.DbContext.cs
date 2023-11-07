using FastMoq.Extensions;
using FastMoq.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq
{
    /// <summary>
    ///     Class Mocker.
    /// </summary>
    public partial class Mocker
    {
        /// <summary>
        ///     Gets the database context.
        /// </summary>
        /// <typeparam name="TContext">The type of the t context.</typeparam>
        /// <param name="options">The options.</param>
        /// <param name="connection">The connection.</param>
        /// <returns>TContext.</returns>
        public TContext GetDbContext<TContext>(DbContextOptions<TContext>? options = null, DbConnection? connection = null)
            where TContext : DbContext =>
            GetDbContext(contextOptions =>
                {
                    AddType(_ => contextOptions, true);
                    AddType<TContext>(_ => CreateInstance<TContext>(), true);
                    return CreateInstance<TContext>() ?? throw new InvalidOperationException("Unable to create DbContext.");
                },
                options,
                connection
            );

        /// <summary>
        ///     Gets the database context.
        /// </summary>
        /// <typeparam name="TContext">The type of the t context.</typeparam>
        /// <param name="newObjectFunc">The new object function.</param>
        /// <returns>TContext.</returns>
        public TContext GetDbContext<TContext>(Func<DbContextOptions, TContext> newObjectFunc) where TContext : DbContext
        {
            DbConnection = new SqliteConnection("DataSource=:memory:");
            DbConnection.Open();
            var dbContextOptions = new DbContextOptionsBuilder<TContext>()
                .UseSqlite(DbConnection)
                .Options;

            var context = newObjectFunc(dbContextOptions);
            context.Database.EnsureCreated();
            context.SaveChanges();

            return context;
        }

        /// <summary>
        ///     Gets the database context using a SqlLite DB or provided options and DbConnection.
        /// </summary>
        /// <typeparam name="TContext">The type of the t context.</typeparam>
        /// <param name="newObjectFunc">The new object function.</param>
        /// <param name="options">The options.</param>
        /// <param name="connection">The connection.</param>
        /// <returns>TContext.</returns>
        public TContext GetDbContext<TContext>(Func<DbContextOptions<TContext>, TContext> newObjectFunc, DbContextOptions<TContext>? options,
            DbConnection? connection) where TContext : DbContext
        {
            DbConnection = connection ?? new SqliteConnection("DataSource=:memory:");
            DbConnection.Open();

            var dbContextOptions = options ??
                                   new DbContextOptionsBuilder<TContext>()
                                       .UseSqlite(DbConnection)
                                       .Options;

            var context = newObjectFunc(dbContextOptions);
            context.Database.EnsureCreated();
            context.SaveChanges();

            return context;
        }

        /// <summary>
        ///     Gets the mock database context.
        /// </summary>
        /// <param name="contextType">Type of the context.</param>
        /// <returns>Mock of the mock database context.</returns>
        /// <exception cref="System.InvalidOperationException">Unable to get MockDb. Try GetDbContext to use internal database.</exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="MissingMethodException">GetMockDbContext</exception>
        public Mock GetMockDbContext(Type contextType)
        {
            return contextType.CallGenericMethod(this) as Mock ??
                   throw new InvalidOperationException("Unable to get MockDb. Try GetDbContext to use internal database.");
        }

        /// <summary>
        ///     Gets the mock database context.
        /// </summary>
        /// <typeparam name="TDbContext">The type of the t database context.</typeparam>
        /// <returns>Mock&lt;TDbContext&gt; of the mock database context.</returns>
        public DbContextMock<TDbContext> GetMockDbContext<TDbContext>() where TDbContext : DbContext
        {
            if (Contains<TDbContext>())
            {
                return (DbContextMock<TDbContext>) GetMock<TDbContext>();
            }

            // Add DbContextOptions wrapper to mock DbContextOptions.
            if (!Contains<DbContextOptions<TDbContext>>())
            {
                AddMock(new MockDbContextOptions<TDbContext>(), false);
            }

            var mock = (DbContextMock<TDbContext>) GetProtectedMock(typeof(TDbContext));

            return mock.SetupDbSets(this);
        }
    }
}

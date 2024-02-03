using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FastMoq.Tests
{
    public class BlogRepoTests : MockerTestBase<BlogRepo>
    {
        protected override Action<Mocker> SetupMocksAction => mocker =>
        {
            var dbContextMock = mocker.GetMockDbContext<ApplicationDbContext>();
            mocker.AddType(_ => dbContextMock.Object);
        };

        [Fact]
        public void GetBlog_ShouldReturnBlog_WhenPassedId()
        {
            // Arrange
            const int ID = 1234;
            var blogsTestData = new List<Blog> { new() { Id = ID } };

            // Create a mock DbContext
            var dbContext = Mocks.GetRequiredObject<ApplicationDbContext>();
            dbContext.Blogs.AddRange(blogsTestData); // Can also be dbContext.Set<Blog>().AddRange(blogsTestData)

            // Act
            var result = Component.GetBlogById(ID);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id.Equals(ID));
        }
    }

    public class BlogRepo(ApplicationDbContext dbContext)
    {
        public Blog? GetBlogById(int id) => dbContext.Blogs.AsEnumerable().FirstOrDefault(x => x.Id == id);
    }

    public class ApplicationDbContext : DbContext
    {
        public virtual DbSet<Blog> Blogs { get; set; }
        // ...other DbSet properties

        // Internal for testing because the public constructor cannot be used. Otherwise, we need an interface for this object.
        // In order for an internal to work, you may need to add InternalsVisibleTo attribute in the AssemblyInfo or project to allow the mocker to see the internals.
        // [assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
        internal ApplicationDbContext()
        {
        }

        // Public constructor used by application.
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) => Database.EnsureCreated();
    }

    public class Blog
    {
        public int Id { get; set; }
    }
}

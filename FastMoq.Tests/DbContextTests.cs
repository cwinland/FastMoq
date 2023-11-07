using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastMoq.Tests
{
    public class DbContextTests : MockerTestBase<MyDbContext>
    {
        [Fact]
        public void GetDbContext_WithOptions()
        {
            var mockDbContext = Mocks.GetDbContext(options => new MyDbContext((DbContextOptions<MyDbContext>)options));
            mockDbContext.Certificates.Should().HaveCount(0);
            mockDbContext.MyDbSetData.Should().HaveCount(0);
            mockDbContext.MyDbSetData2.Should().HaveCount(0);
        }

        [Fact]
        public void GetDbContext_WithoutOptions()
        {
            var mockDbContext = Mocks.GetDbContext<MyDbContext>();
            mockDbContext.Certificates.Should().HaveCount(0);
            mockDbContext.MyDbSetData.Should().HaveCount(0);
            mockDbContext.MyDbSetData2.Should().HaveCount(0);
        }

        [Fact]
        public void GetMockDbContext_SameAs_Component()
        {
            var mockDbContext = Mocks.GetMockDbContext(typeof(MyDbContext));
            mockDbContext.Object.Should().BeSameAs(Component);

            Mocks.GetMock<MyDbContext>().Object.Should().BeSameAs(Component);

            mockDbContext.Should().BeSameAs(Mocks.GetMock<MyDbContext>());
        }

        [Fact]
        public void TestComponent_SetAdd()
        {
            Component.MyDbUpdateMethod();
            Component.Set<MockDataModel>().Add(new() { Name="test1" }); // This brings back the second one because it was set twice and last one wins.
            Component.Set<MockDataModel>("MyDbSetData2").Add(new() { Name="Test2" });
            Component.MyDbSetData2.Add(new () { Name="Test3" });
            Component.MyDbSetData2.Should().HaveCount(3);
        }

        [Fact]
        public void TestDbContext()
        {
            var mockDbContext = Mocks.GetMockDbContext<MyDbContext>();

            mockDbContext.Object.MyDbUpdateMethod();
            mockDbContext.Object.Set<MockDataModel>().Add(new() { Name="test1" });  // This brings back the second one because it was set twice and last one wins.
            mockDbContext.Object.Set<MockDataModel>("MyDbSetData2").Add(new() { Name="Test2" });
            mockDbContext.Object.MyDbSetData2.Add(new () { Name="Test3" });
            mockDbContext.Object.MyDbSetData2.Should().HaveCount(3);
        }

        [Fact]
        public void TestCustomProp()
        {
            Component.CustomProp.Should().BeFalse();
            Component.MyDbUpdateMethod();
            Component.CustomProp.Should().BeTrue();
        }

        [Fact]
        public void TestSetByName()
        {
            Component.Set<MockDataModel>("MyDbSetData").Should().BeSameAs(Component.MyDbSetData);
            Component.Set<MockDataModel>("MyDbSetData2").Should().BeSameAs(Component.MyDbSetData2);

            Component.MyDbSetData.Add(new MockDataModel());
            Component.Set<MockDataModel>("MyDbSetData").Should().HaveCount(1);
            Component.Set<MockDataModel>("MyDbSetData2").Should().HaveCount(0);
        }

        [Fact]
        public async Task TestAsync_AddAsync()
        {
            var dbSet = Component.Set<MockDataModel>("MyDbSetData");

            dbSet.Should().HaveCount(0);
            await dbSet.AddAsync(new MockDataModel(), CancellationToken.None);
            dbSet.Should().HaveCount(1);
        }

        [Fact]
        public async Task TestAsync_GetAllToList()
        {
            var dbSet = Component.Set<MockDataModel>("MyDbSetData");
            dbSet.Add(new MockDataModel());
            dbSet.Should().HaveCount(1);

            var test = await GetAll<MockDataModel>(Component);
            test.Should().HaveCount(1);
        }

        [Fact]
        public async Task TestAsync_GetAllToListAsync()
        {
            var dbSet = Component.Set<MockDataModel>("MyDbSetData");
            dbSet.Add(new MockDataModel());
            dbSet.Should().HaveCount(1);

            var test2 = await GetAll2<MockDataModel>(Component, CancellationToken.None);
            test2.Should().HaveCount(1);
        }

        private Task<List<TEntity>> GetAll<TEntity>(MyDbContext dbContext) where TEntity : class
        {
            var query = dbContext.Set<TEntity>("MyDbSetData").AsQueryable();
            return Task.FromResult(query.ToList());
        }

        private async Task<List<TEntity>> GetAll2<TEntity>(MyDbContext dbContext, CancellationToken token) where TEntity : class
        {
            List<TEntity> list = new();

            var query = dbContext.Set<TEntity>("MyDbSetData").AsAsyncEnumerable();
            await foreach (var element in query.WithCancellation(token))
            {
                list.Add(element);
            }

            return list;
        }
    }

    public class MockDataModel2
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class MockDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class MyDbContext : DbContext
    {
        public bool CustomProp { get; set; }
        public virtual DbSet<MockDataModel> MyDbSetData { get; set; } // Dataset 1
        public virtual DbSet<MockDataModel> MyDbSetData2 { get; set; } // Dataset 2
        public DbSet<MockDataModel2> Certificates { get; set; } // Not Mockable and should be ignored.

        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        public bool MyDbUpdateMethod() => CustomProp = true;
        protected MyDbContext()
        {
        }
    }
}

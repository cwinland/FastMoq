using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace FastMoq.Tests
{
    public class DbContextTests : MockerTestBase<MyDbContext>
    {
        #region Overrides of MockerTestBase<MyDbContext>

        /// <inheritdoc />
        protected override Func<Mocker, MyDbContext?> CreateComponentAction => mocker => mocker.GetMockDbContext<MyDbContext>().Object;

        #endregion

        [Fact]
        public void TestDbContext()
        {
            var mockDbContext = Mocks.GetMockDbContext<MyDbContext>();
            Component.MyDbUpdateMethod();
            var test = Mocks.mockCollection.ToList();
            Component.Set<MockDataModel>().Add(new());
            Component.Set<MockDataModel>("MyDbSetData").Add(new());
            Component.MyDbSetData.Add(new MockDataModel());
            Component.MyDbSetData.Should().HaveCount(3);
        }
    }
    
    public class MockDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class MyDbContext : DbContext
    {
        public virtual DbSet<MockDataModel> MyDbSetData { get; set; }
        public virtual DbSet<X509Certificate2> X509Certificates { get; set; }
        protected MyDbContext() {}
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

        public bool MyDbUpdateMethod() => true;
    }
}

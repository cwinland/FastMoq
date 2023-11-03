using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace FastMoq.Tests
{
    public class DbContextTests : MockerTestBase<MyDbContext>
    {
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
            Component.Set<MockDataModel>().Add(new());
            Component.Set<MockDataModel>("MyDbSetData").Add(new());
            Component.MyDbSetData.Add(new MockDataModel());
            Component.MyDbSetData.Should().HaveCount(3);
        }

        [Fact]
        public void TestDbContext()
        {
            var mockDbContext = Mocks.GetMockDbContext<MyDbContext>();

            mockDbContext.Object.MyDbUpdateMethod();
            mockDbContext.Object.Set<MockDataModel>().Add(new());
            mockDbContext.Object.Set<MockDataModel>("MyDbSetData").Add(new());
            mockDbContext.Object.MyDbSetData.Add(new MockDataModel());
            mockDbContext.Object.MyDbSetData.Should().HaveCount(3);
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
        public DbSet<X509Certificate2> X509Certificates { get; set; } // Not Mockable and should be ignored.

        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

        public bool MyDbUpdateMethod() => CustomProp = true;
    }
}

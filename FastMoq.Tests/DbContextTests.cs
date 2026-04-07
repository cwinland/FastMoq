using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastMoq.Tests
{
    public class DbContextTests : MockerTestBase<MyDbContext>
    {
        [Fact]
        public void GetObject_DbContext_ShouldReturnBuiltInMockObject_WhenNoProvidedRegistrationExists()
        {
            var dbContext = Mocks.GetObject<MyDbContext>();

            dbContext.Should().NotBeNull();
            Mocks.Contains<MyDbContext>().Should().BeTrue();
            Mocks.GetMockDbContext<MyDbContext>().Object.Should().BeSameAs(dbContext);
        }

        [Fact]
        public void GetObject_DbContext_ShouldReturnTrackedMockObject_WhenTrackedMockAlreadyExists()
        {
            var trackedMock = Mocks.GetMockDbContext<MyDbContext>();

            var dbContext = Mocks.GetObject<MyDbContext>();

            dbContext.Should().BeSameAs(trackedMock.Object);
        }

        [Fact]
        public void GetDbContextHandle_ShouldDefaultToMockedSetMode()
        {
            var handle = Mocks.GetDbContextHandle<MyDbContext>();

            handle.Mode.Should().Be(DbContextTestMode.MockedSets);
            handle.Mock.Should().NotBeNull();
            handle.Context.Should().BeSameAs(handle.Mock!.Object);

            var secondHandle = Mocks.GetDbContextHandle<MyDbContext>();
            secondHandle.Should().BeSameAs(handle);
        }

        [Fact]
        public void GetDbContextHandle_ShouldCreateRealInMemoryContext_WhenRequested()
        {
            var mocker = new Mocker();

            var handle = mocker.GetDbContextHandle<RealModeDbContext>(new DbContextHandleOptions<RealModeDbContext>
            {
                Mode = DbContextTestMode.RealInMemory,
            });

            handle.Mode.Should().Be(DbContextTestMode.RealInMemory);
            handle.Mock.Should().BeNull();
            handle.Context.Items.Add(new RealModeEntity { Id = 5 });
            handle.Context.SaveChanges();

            var resolved = mocker.GetObject<RealModeDbContext>();
            resolved.Should().BeSameAs(handle.Context);
            resolved!.Items.Should().ContainSingle(x => x.Id == 5);

            var secondHandle = mocker.GetDbContextHandle<RealModeDbContext>(new DbContextHandleOptions<RealModeDbContext>
            {
                Mode = DbContextTestMode.RealInMemory,
            });

            secondHandle.Should().BeSameAs(handle);
        }

        [Fact]
        public void GetDbContextHandle_ShouldThrow_WhenDifferentModeAlreadyTracked()
        {
            var mocker = new Mocker();

            mocker.GetDbContextHandle<RealModeDbContext>(new DbContextHandleOptions<RealModeDbContext>
            {
                Mode = DbContextTestMode.RealInMemory,
            });

            var action = () => mocker.GetDbContextHandle<RealModeDbContext>();

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*already tracked in RealInMemory mode*");
        }

        [Fact]
        public void GetObject_DbContext_ShouldRemainBuiltInManagedInstance_WhenStrictCompatibilityDefaultsAreEnabled()
        {
            Mocks.Behavior.Enabled |= MockFeatures.FailOnUnconfigured;
            Mocks.Policy.EnabledBuiltInTypeResolutions = BuiltInTypeResolutionFlags.StrictCompatibilityDefaults;

            var dbContext = Mocks.GetObject<MyDbContext>();

            dbContext.Should().NotBeNull();
            Mocks.Contains<MyDbContext>().Should().BeTrue();
            Mocks.GetMockDbContext<MyDbContext>().Object.Should().BeSameAs(dbContext);
        }

        [Fact]
        public void GetObject_DbContext_ShouldAllowDisablingBuiltInManagedResolution_Explicitly()
        {
            Mocks.Policy.EnabledBuiltInTypeResolutions &= ~BuiltInTypeResolutionFlags.DbContext;

            var dbContext = Mocks.GetObject<MyDbContext>();

            dbContext.Should().NotBeNull();
            Mocks.Contains<MyDbContext>().Should().BeTrue();
            Mocks.GetMock<MyDbContext>().Object.Should().BeSameAs(dbContext);
        }

        [Fact]
        public void GetMockDbContext_ShouldRemainUsable_WhenDefaultStrictMockCreationIsEnabled()
        {
            using var fixture = new StrictMockCreationDbContextFixture();

            var mockDbContext = fixture.TestMocks.GetMockDbContext<MyDbContext>();

            mockDbContext.Behavior.Should().Be(MockBehavior.Default);
        }

        [Fact]
        public void GetObject_DbContext_ShouldReturnCustomManagedInstance_BeforeTrackedAndBuiltIn()
        {
            var expected = new CustomManagedDbContext(
                new DbContextOptionsBuilder<CustomManagedDbContext>()
                    .UseInMemoryDatabase($"FastMoq_{nameof(CustomManagedDbContext)}_{System.Guid.NewGuid():N}")
                    .Options);

            Mocks.AddKnownType<DbContext>(
                managedInstanceFactory: (_, requestedType) =>
                    requestedType == typeof(CustomManagedDbContext) ? expected : null,
                includeDerivedTypes: true);

            var trackedMock = Mocks.GetMockDbContext<CustomManagedDbContext>();
            trackedMock.Should().NotBeNull();

            var dbContext = Mocks.GetObject<CustomManagedDbContext>();

            dbContext.Should().BeSameAs(expected);
            dbContext.Should().NotBeSameAs(trackedMock.Object);
        }

        [Fact]
        public void GetMockDbContext_ShouldReturnSameInstanceAsComponent()
        {
            var mockDbContext = Mocks.GetMockDbContext(typeof(MyDbContext));
            mockDbContext.Object.Should().BeSameAs(Component);

            Mocks.GetMock<MyDbContext>().Object.Should().BeSameAs(Component);

            mockDbContext.Should().BeSameAs(Mocks.GetMock<MyDbContext>());
        }

        [Fact]
        public void Component_DbSetOperations_ShouldTrackAddedEntitiesAcrossNamedSets()
        {
            Component.MyDbUpdateMethod();
            Component.Set<MockDataModel>().Add(new() { Name="test1" }); // This brings back the second one because it was set twice and last one wins.
            Component.Set<MockDataModel>("MyDbSetData2").Add(new() { Name="Test2" });
            Component.MyDbSetData2.Add(new () { Name="Test3" });
            Component.MyDbSetData2.Should().HaveCount(3);
        }

        [Fact]
        public void GetMockDbContext_ShouldSupportDbSetOperationsOnTrackedMock()
        {
            var mockDbContext = Mocks.GetMockDbContext<MyDbContext>();

            mockDbContext.Object.MyDbUpdateMethod();
            mockDbContext.Object.Set<MockDataModel>().Add(new() { Name="test1" });  // This brings back the second one because it was set twice and last one wins.
            mockDbContext.Object.Set<MockDataModel>("MyDbSetData2").Add(new() { Name="Test2" });
            mockDbContext.Object.MyDbSetData2.Add(new () { Name="Test3" });
            mockDbContext.Object.MyDbSetData2.Should().HaveCount(3);
        }

        [Fact]
        public void Component_ShouldPersistCustomPropertyChangesOnDbContextMock()
        {
            Component.CustomProp.Should().BeFalse();
            Component.MyDbUpdateMethod();
            Component.CustomProp.Should().BeTrue();
        }

        [Fact]
        public void SetByName_ShouldResolveConfiguredDbSetProperties()
        {
            Component.Set<MockDataModel>("MyDbSetData").Should().BeSameAs(Component.MyDbSetData);
            Component.Set<MockDataModel>("MyDbSetData2").Should().BeSameAs(Component.MyDbSetData2);

            Component.MyDbSetData.Add(new MockDataModel());
            Component.Set<MockDataModel>("MyDbSetData").Should().HaveCount(1);
            Component.Set<MockDataModel>("MyDbSetData2").Should().HaveCount(0);
        }

        [Fact]
        public async Task DbSet_AddAsync_ShouldAppendEntityToTrackedSet()
        {
            var dbSet = Component.Set<MockDataModel>("MyDbSetData");

            dbSet.Should().HaveCount(0);
            await dbSet.AddAsync(new MockDataModel(), CancellationToken.None);
            dbSet.Should().HaveCount(1);
        }

        [Fact]
        public async Task QueryableDbSet_ShouldEnumerateToList()
        {
            var dbSet = Component.Set<MockDataModel>("MyDbSetData");
            dbSet.Add(new MockDataModel());
            dbSet.Should().HaveCount(1);

            var test = await GetAll<MockDataModel>(Component);
            test.Should().HaveCount(1);
        }

        [Fact]
        public async Task AsyncEnumerableDbSet_ShouldEnumerateToListAsync()
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

    public class CustomManagedDbContext : DbContext
    {
        public CustomManagedDbContext(DbContextOptions<CustomManagedDbContext> options) : base(options)
        {
        }

        protected CustomManagedDbContext()
        {
        }
    }

    public class RealModeDbContext : DbContext
    {
        public RealModeDbContext(DbContextOptions<RealModeDbContext> options) : base(options)
        {
        }

        public DbSet<RealModeEntity> Items { get; set; }
    }

    public class RealModeEntity
    {
        public int Id { get; set; }
    }

    internal sealed class StrictMockCreationDbContextFixture : MockerTestBase<MyDbContext>
    {
        public Mocker TestMocks => Mocks;

        protected override Action<MockerPolicyOptions>? ConfigureMockerPolicy => policy =>
        {
            policy.DefaultStrictMockCreation = true;
        };
    }
}

using System;
using System.IO.Abstractions;
using System.Net.Http;
using FastMoq.Providers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FastMoq.Tests
{
    public class ProviderTests
    {
        public static TheoryData<string> ProviderNames => new()
        {
            "moq",
            "nsubstitute",
            "reflection",
        };

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void GetOrCreateMock_ShouldUseSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();

            var first = mocker.GetOrCreateMock<IProviderDependency>();
            var second = mocker.GetOrCreateMock<IProviderDependency>();

            second.Should().BeSameAs(first);
            first.Instance.Should().NotBeNull();
            first.MockedType.Should().Be(typeof(IProviderDependency));

            if (providerName == "moq")
            {
                first.NativeMock.Should().BeOfType<Mock<IProviderDependency>>();
            }
            else
            {
                first.NativeMock.Should().BeSameAs(first.Instance);
            }
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void Verify_ShouldWork_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<IProviderDependency>();

            dependency.Instance.Run("alpha");

            mocker.Verify<IProviderDependency>(x => x.Run("alpha"), TimesSpec.Once);
            mocker.VerifyNoOtherCalls<IProviderDependency>();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void CreateInstance_ShouldInjectTrackedMock_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<IProviderDependency>();

            var instance = mocker.CreateInstance<ProviderConsumer>();

            instance.Should().NotBeNull();
            instance!.Dependency.Should().BeSameAs(dependency.Instance);
        }

        [Theory]
        [InlineData("moq", false)]
        [InlineData("nsubstitute", true)]
        [InlineData("reflection", true)]
        public void GetMock_ShouldRemainMoqOnlyCompatibilitySurface(string providerName, bool shouldThrow)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();

            Action action = () => mocker.GetMock<IProviderDependency>();

            if (shouldThrow)
            {
                action.Should().Throw<NotSupportedException>();
                return;
            }

            action.Should().NotThrow();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void GetOrCreateMock_WithServiceKey_ShouldReturnSameTrackedMockPerKey(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();

            var alphaOptions = new MockRequestOptions { ServiceKey = "alpha" };
            var betaOptions = new MockRequestOptions { ServiceKey = "beta" };

            var first = mocker.GetOrCreateMock<IProviderDependency>(alphaOptions);
            var second = mocker.GetOrCreateMock<IProviderDependency>(alphaOptions);
            var other = mocker.GetOrCreateMock<IProviderDependency>(betaOptions);

            second.Should().BeSameAs(first);
            other.Should().NotBeSameAs(first);
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void CreateInstance_ShouldResolveKeyedDependencies_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var keyedDependency = mocker.GetOrCreateMock<IProviderDependency>(new MockRequestOptions
            {
                ServiceKey = "dep",
            });
            var primaryUri = new Uri("http://primary.fastmoq/");

            mocker.AddKeyedType<Uri>("primary", _ => primaryUri);

            var instance = mocker.CreateInstance<KeyedProviderConsumer>();

            instance.Should().NotBeNull();
            instance!.PrimaryUri.Should().BeSameAs(primaryUri);
            instance.Dependency.Should().BeSameAs(keyedDependency.Instance);
            instance.DefaultHttpClient.Should().BeSameAs(mocker.HttpClient);
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void GetObject_ShouldPreferKnownTypeOverride_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var expected = new Uri("http://override.fastmoq/");

            mocker.AddKnownType<Uri>(directInstanceFactory: (_, _) => expected, replace: true);

            var resolved = mocker.GetObject<Uri>();

            resolved.Should().BeSameAs(expected);
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void GetObject_IFileSystem_ShouldReturnBuiltInInstance_WhenNoTrackedMockExists(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();

            var resolved = mocker.GetObject<IFileSystem>();

            resolved.Should().NotBeNull();
            resolved.Should().BeSameAs(mocker.fileSystem);
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void GetObject_IFileSystem_ShouldPreferTrackedMock_WhenTrackedProviderMockExists(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var tracked = mocker.GetOrCreateMock<IFileSystem>();

            var resolved = mocker.GetObject<IFileSystem>();

            resolved.Should().BeSameAs(tracked.Instance);
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void GetObject_IFileSystem_ShouldPreferCustomRegistration_OverTrackedMockAndBuiltIn(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var custom = new System.IO.Abstractions.TestingHelpers.MockFileSystem();

            mocker.GetOrCreateMock<IFileSystem>();
            mocker.AddKnownType<IFileSystem>(directInstanceFactory: (_, _) => custom, replace: true);

            var resolved = mocker.GetObject<IFileSystem>();

            resolved.Should().BeSameAs(custom);
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void GetObject_DbContext_ShouldReturnTrackedBuiltInDbContext_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();

            var resolved = mocker.GetObject<ProviderDbContext>();
            var dbContextMock = mocker.GetMockDbContext<ProviderDbContext>();

            resolved.Should().NotBeNull();
            resolved.Should().BeSameAs(dbContextMock.Object);
            mocker.Contains<ProviderDbContext>().Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void GetObject_DbContext_ShouldPreferCustomManagedKnownType_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var expected = new ProviderDbContext(
                new DbContextOptionsBuilder<ProviderDbContext>()
                    .UseInMemoryDatabase($"ProviderKnown_{providerName}_{Guid.NewGuid():N}")
                    .Options);

            mocker.AddKnownType<DbContext>(
                managedInstanceFactory: (_, requestedType) => requestedType == typeof(ProviderDbContext) ? expected : null,
                includeDerivedTypes: true);

            var trackedMock = mocker.GetMockDbContext<ProviderDbContext>();
            var resolved = mocker.GetObject<ProviderDbContext>();

            trackedMock.Should().NotBeNull();
            resolved.Should().BeSameAs(expected);
            resolved.Should().NotBeSameAs(trackedMock.Object);
        }

        private static IDisposable PushProvider(string providerName)
        {
            if (!MockingProviderRegistry.TryGet(providerName, out var provider))
            {
                throw new InvalidOperationException($"Unable to find provider '{providerName}'.");
            }

            return MockingProviderRegistry.Push(provider);
        }

        public interface IProviderDependency
        {
            void Run(string value);
        }

        public class ProviderConsumer(IProviderDependency dependency)
        {
            public IProviderDependency Dependency { get; } = dependency;
        }

        public class KeyedProviderConsumer(
            [FromKeyedServices("primary")] Uri primaryUri,
            [FromKeyedServices("dep")] IProviderDependency dependency,
            HttpClient defaultHttpClient)
        {
            public Uri PrimaryUri { get; } = primaryUri;
            public IProviderDependency Dependency { get; } = dependency;
            public HttpClient DefaultHttpClient { get; } = defaultHttpClient;
        }

        public class ProviderDbContext(DbContextOptions<ProviderDbContext> options) : DbContext(options)
        {
            public virtual DbSet<ProviderEntity> Entities { get; set; }
        }

        public class ProviderEntity
        {
            public int Id { get; set; }
        }
    }
}
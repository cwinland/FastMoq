using System;
using System.IO.Abstractions;
using System.Linq.Expressions;
using System.Net.Http;
using FastMoq.Extensions;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using FastMoq.Providers.NSubstituteProvider;
using FastMoq.Providers.ReflectionProvider;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FastMoq.Tests
{
    public class ProviderTests
    {
        static ProviderTests()
        {
            MockingProviderRegistry.Register("nsubstitute", NSubstituteMockingProvider.Instance, setAsDefault: false);
        }

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

        [Fact]
        public void GetOrCreateMock_Instance_ShouldBeUsable_WithReflectionProvider()
        {
            using var providerScope = PushProvider("reflection");
            var mocker = new Mocker();

            var dependency = mocker.GetOrCreateMock<IProviderDependency>();
            var consumer = new ProviderConsumer(dependency.Instance);

            consumer.Dependency.Run("alpha");

            mocker.Verify<IProviderDependency>(x => x.Run("alpha"), TimesSpec.Once);
        }

        [Theory]
        [InlineData("moq", true)]
        [InlineData("nsubstitute", true)]
        [InlineData("reflection", false)]
        public void LoggerCaptureCapability_ShouldMatchProviderBehavior(string providerName, bool supportsLoggerCapture)
        {
            using var providerScope = PushProvider(providerName);

            MockingProviderRegistry.Default.Capabilities.SupportsLoggerCapture.Should().Be(supportsLoggerCapture);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public void VerifyLogged_ShouldWork_ForProvidersThatSupportLoggerCapture(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var logger = mocker.GetObject<ILogger<NullLogger>>();

            logger.Should().NotBeNull();
            logger!.LogInformation("provider info");
            logger.LogError(12, new InvalidOperationException("provider boom"), "provider error");

            mocker.VerifyLogged(LogLevel.Information, "provider info");
            mocker.VerifyLogged(LogLevel.Information, "provider info", TimesSpec.AtLeast(1));
            mocker.VerifyLogged(LogLevel.Error, "provider error", new InvalidOperationException("provider boom"), 12, TimesSpec.Once);
        }

        [Fact]
        public void VerifyLogged_ShouldFailFast_ForProviderWithoutLoggerCapture()
        {
            using var providerScope = PushProvider("reflection");
            var mocker = new Mocker();

            var action = () => mocker.VerifyLogged(LogLevel.Information, "provider info", 1);

            action.Should().Throw<NotSupportedException>()
                .WithMessage("*ReflectionMockingProvider*");
        }

        [Fact]
        public void FrameworkCalls_ShouldSkipUnsupportedProviderCapabilities()
        {
            using var providerScope = PushProvider("reflection");
            var mocker = new Mocker();

            var dependency = mocker.GetObject<IProviderDependency>();
            var consumer = mocker.CreateInstance<ProviderConsumer>();

            dependency.Should().NotBeNull();
            consumer.Should().NotBeNull();
            consumer!.Dependency.Should().BeSameAs(dependency);
        }

        [Fact]
        public void DirectUnsupportedProviderCalls_ShouldThrow_ForNSubstitute()
        {
            var provider = NSubstituteMockingProvider.Instance;
            var mock = provider.CreateMock<ILogger>();

            provider.Capabilities.SupportsSetupAllProperties.Should().BeFalse();
            provider.Capabilities.SupportsCallBase.Should().BeFalse();

            Action configureProperties = () => provider.ConfigureProperties(mock);
            Action setCallBase = () => provider.SetCallBase(mock, true);

            configureProperties.Should().Throw<NotSupportedException>();
            setCallBase.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void DirectUnsupportedProviderCalls_ShouldThrow_ForReflection()
        {
            var provider = ReflectionMockingProvider.Instance;
            var mock = provider.CreateMock<ILogger>();

            provider.Capabilities.SupportsSetupAllProperties.Should().BeFalse();
            provider.Capabilities.SupportsLoggerCapture.Should().BeFalse();

            Action configureProperties = () => provider.ConfigureProperties(mock);
            Action configureLogger = () => provider.ConfigureLogger(mock, (_, _, _, _) => { });

            configureProperties.Should().Throw<NotSupportedException>();
            configureLogger.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void DirectSupportedProviderCalls_ShouldSucceed_ForMoq()
        {
            var provider = MoqMockingProvider.Instance;
            var mock = provider.CreateMock<ILogger>();

            Action configureProperties = () => provider.ConfigureProperties(mock);
            Action configureLogger = () => provider.ConfigureLogger(mock, (_, _, _, _) => { });
            Action setCallBase = () => provider.SetCallBase(mock, false);

            configureProperties.Should().NotThrow();
            configureLogger.Should().NotThrow();
            setCallBase.Should().NotThrow();
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public void Push_ShouldResolveRegisteredProviderByName(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);

            MockingProviderRegistry.Default.Should().BeSameAs(GetProvider(providerName));
        }

        [Fact]
        public void RegisteredProviderNames_ShouldIncludeBuiltInProviders()
        {
            MockingProviderRegistry.RegisteredProviderNames.Should().Contain(["moq", "reflection", "nsubstitute"]);
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
        public void CreateInstance_ShouldFallbackToUnkeyedTrackedMock_WhenKeyedDependencyIsNotRegistered(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var unkeyedDependency = mocker.GetOrCreateMock<IProviderDependency>();

            var instance = mocker.CreateInstance<KeyedProviderFallbackConsumer>();

            instance.Should().NotBeNull();
            instance!.Dependency.Should().BeSameAs(unkeyedDependency.Instance);
        }

        [Fact]
        public void BuildExpression_ShouldUseMoqWildcardMatcher_WhenMoqIsActive()
        {
            using var providerScope = PushProvider("moq");
            var mocker = new Mocker();
            var dependency = mocker.GetMock<IExpressionConsumer>();

            dependency
                .Setup(x => x.Match(Mocker.BuildExpression<string>()))
                .Returns(true);

            var matched = dependency.Object.Match(value => value == "alpha");

            matched.Should().BeTrue();
        }

        [Theory]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public void BuildExpression_ShouldReturnProviderSafePredicate_ForNonMoqProviders(string providerName)
        {
            using var providerScope = PushProvider(providerName);

            var expression = Mocker.BuildExpression<string>();

            expression.Should().NotBeNull();
            expression.Compile().Invoke("alpha").Should().BeTrue();
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
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public void GetOrCreateMock_IFileSystem_ShouldUseBuiltInManagedInstance_ForProvidersWithoutPropertySetup(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();

            var tracked = mocker.GetOrCreateMock<IFileSystem>();

            tracked.Instance.Should().BeSameAs(mocker.fileSystem);
            tracked.NativeMock.Should().BeSameAs(mocker.fileSystem);
        }

        [Theory]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public void GetObject_IHttpContextAccessor_ShouldUseConcreteBuiltInAccessor_ForProvidersWithoutPropertySetup(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();

            var accessor = mocker.GetObject<IHttpContextAccessor>();

            accessor.Should().NotBeNull();
            accessor.Should().BeOfType<HttpContextAccessor>();
            accessor!.HttpContext.Should().NotBeNull();
            accessor.HttpContext.Should().BeSameAs(mocker.GetObject<HttpContext>());
        }

        [Fact]
        public void CreateHttpClient_ShouldUseTrackedHandlerInstance_WhenNSubstituteProviderIsActive()
        {
            using var providerScope = PushProvider("nsubstitute");
            var mocker = new Mocker();
            _ = mocker.GetOrCreateMock<HttpMessageHandler>();

            using var client = mocker.CreateHttpClient();

            client.Should().NotBeNull();
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
            return MockingProviderRegistry.Push(providerName);
        }

        private static IMockingProvider GetProvider(string providerName)
        {
            if (!MockingProviderRegistry.TryGet(providerName, out var provider))
            {
                throw new InvalidOperationException($"Unable to find provider '{providerName}'.");
            }

            return provider;
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

        public class KeyedProviderFallbackConsumer([FromKeyedServices("dep")] IProviderDependency dependency)
        {
            public IProviderDependency Dependency { get; } = dependency;
        }

        public interface IExpressionConsumer
        {
            bool Match(Expression<Func<string, bool>> predicate);
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
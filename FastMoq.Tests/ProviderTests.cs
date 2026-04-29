using FastMoq.Extensions;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using FastMoq.Providers.NSubstituteProvider;
using FastMoq.Providers.ReflectionProvider;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;

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

        [Fact]
        public void GetOrCreateMock_WithConstructorArgs_ShouldCreateConcreteMockThroughSupportedProviderFirstPath()
        {
            using var providerScope = PushProvider("moq");
            var mocker = new Mocker();
            var endpoint = new Uri("https://fastmoq.test/providers/orders");
            const string queueName = "orders";

            var typed = mocker.GetOrCreateMockWithConstructorArgs<ProviderConstructedDependency>(endpoint, queueName);
            var untyped = mocker.GetOrCreateMockWithConstructorArgs(typeof(ProviderConstructedDependency), endpoint, queueName);

            untyped.Should().BeSameAs(typed);
            typed.Instance.Endpoint.Should().Be(endpoint);
            typed.Instance.QueueName.Should().Be(queueName);
        }

        [Fact]
        public void ExplicitMoqProviderReference_ShouldNotEmitTransitionWarningToStandardError()
        {
            using var providerScope = PushProvider("moq");
            var mocker = new Mocker();
            var originalError = Console.Error;
            using var errorWriter = new StringWriter();

            try
            {
                Console.SetError(errorWriter);

                _ = mocker.GetOrCreateMock<IProviderDependency>();
            }
            finally
            {
                Console.SetError(originalError);
            }

            errorWriter.ToString().Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void TryGetTrackedMock_ShouldReturnFalse_WhenTrackedMockDoesNotExist(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();

            var found = mocker.TryGetTrackedMock<IProviderDependency>(out var trackedMock);

            found.Should().BeFalse();
            trackedMock.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void GetRequiredTrackedMock_ShouldReturnTrackedMock_WhenTrackedMockExists(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var trackedMock = mocker.GetOrCreateMock<IProviderDependency>();

            var required = mocker.GetRequiredTrackedMock<IProviderDependency>();

            required.Should().BeSameAs(trackedMock);
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void GetRequiredTrackedMock_ShouldThrowHelpfulMessage_WhenTrackedMockDoesNotExist(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();

            Action action = () => _ = mocker.GetRequiredTrackedMock<IProviderDependency>();

            var exception = action.Should().Throw<InvalidOperationException>().Which;
            exception.Message.Should().Contain("No tracked mock exists for type IProviderDependency");
            exception.Message.Should().Contain("GetOrCreateMock<IProviderDependency>()");
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void GetRequiredTrackedMock_WithServiceKey_ShouldReturnTrackedMock_WhenTrackedMockExists(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var trackedMock = mocker.GetOrCreateMock<IProviderDependency>(new MockRequestOptions
            {
                ServiceKey = "alpha",
            });

            var required = mocker.GetRequiredTrackedMock(typeof(IProviderDependency), "alpha");

            required.Should().BeSameAs(trackedMock);
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
        public void VerifyCalledOnce_AndVerifyNotCalled_ShouldWork_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<IProviderDependency>();

            dependency.Instance.Run("alpha");

            mocker.VerifyCalledOnce<IProviderDependency>(x => x.Run("alpha"));
            mocker.VerifyNotCalled<IProviderDependency>(x => x.Run("beta"));
            mocker.VerifyNoOtherCalls<IProviderDependency>();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void VerifyCalledExactly_AtLeast_AndAtMost_ShouldWork_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<IProviderDependency>();

            dependency.Instance.Run("alpha");
            dependency.Instance.Run("alpha");
            dependency.Instance.Run("beta");

            mocker.VerifyCalledExactly<IProviderDependency>(service => service.Run("alpha"), 2);
            mocker.VerifyCalledAtLeast<IProviderDependency>(service => service.Run(FastArg.Any<string>()), 3);
            mocker.VerifyCalledAtMost<IProviderDependency>(service => service.Run("beta"), 1);
            mocker.VerifyNoOtherCalls<IProviderDependency>();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void Verify_ShouldSupportFastArgAnyMatcher_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<IProviderDependency>();

            dependency.Instance.Run("alpha");
            dependency.Instance.Run("beta");

            mocker.Verify<IProviderDependency>(x => x.Run(FastArg.Any<string>()), TimesSpec.Exactly(2));
            mocker.VerifyNoOtherCalls<IProviderDependency>();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void Verify_ShouldSupportFastArgPredicateMatcher_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<IProviderDependency>();

            dependency.Instance.Run("alpha");
            dependency.Instance.Run("beta");

            mocker.Verify<IProviderDependency>(x => x.Run(FastArg.Is<string>(value => value.StartsWith("al"))), TimesSpec.Once);
            mocker.Verify<IProviderDependency>(x => x.Run("beta"), TimesSpec.Once);
            mocker.VerifyNoOtherCalls<IProviderDependency>();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void Verify_ShouldSupportFastArgNullAndNotNullMatchers_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<INullableProviderDependency>();

            dependency.Instance.Run(null);
            dependency.Instance.Run("alpha");

            mocker.Verify<INullableProviderDependency>(x => x.Run(FastArg.IsNull<string?>()), TimesSpec.Once);
            mocker.Verify<INullableProviderDependency>(x => x.Run(FastArg.IsNotNull<string?>()), TimesSpec.Once);
            mocker.VerifyNoOtherCalls<INullableProviderDependency>();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void Verify_ShouldSupportFastArgAnyExpressionMatcher_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<IExpressionActionConsumer>();

            dependency.Instance.Observe(value => value == "alpha");

            mocker.Verify<IExpressionActionConsumer>(x => x.Observe(FastArg.AnyExpression<string>()), TimesSpec.Once);
            mocker.VerifyNoOtherCalls<IExpressionActionConsumer>();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void VerifyAnyArgs_ShouldSupportLongMethodSignatures_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<IProviderDeploymentDependency>();

            dependency.Instance.CreateDeployment(
                "alpha",
                new Dictionary<string, int> { ["region"] = 1 },
                new MemoryStream(),
                new MemoryStream(),
                true);

            mocker.VerifyAnyArgs<IProviderDeploymentDependency, Action<string, Dictionary<string, int>, Stream, Stream, bool>>(service => service.CreateDeployment, TimesSpec.Once);
            mocker.VerifyNoOtherCalls<IProviderDeploymentDependency>();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void VerifyAnyArgs_ShouldSupportNonVoidMethods_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<IProviderResultDependency>();

            _ = dependency.Instance.Lookup("alpha");

            mocker.VerifyAnyArgs<IProviderResultDependency>(nameof(IProviderResultDependency.Lookup), TimesSpec.Once, typeof(string));
            mocker.VerifyNoOtherCalls<IProviderResultDependency>();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void DetachedVerifyAnyArgs_ShouldSupportDelegateSelector_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.CreateStandaloneFastMock<IProviderDeploymentDependency>();

            dependency.Instance.CreateDeployment(
                "alpha",
                new Dictionary<string, int> { ["region"] = 1 },
                new MemoryStream(),
                new MemoryStream(),
                true);

            MockingProviderRegistry.VerifyAnyArgs<IProviderDeploymentDependency, Action<string, Dictionary<string, int>, Stream, Stream, bool>>(
                dependency,
                target => target.CreateDeployment,
                TimesSpec.Once);
            MockingProviderRegistry.VerifyNoOtherCalls(dependency);
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void DetachedVerifyAnyArgs_ShouldSupportOverloadSelection_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.CreateStandaloneFastMock<IProviderOverloadedDeploymentDependency>();

            dependency.Instance.CreateDeployment(
                "alpha",
                new Dictionary<string, int> { ["region"] = 1 },
                new MemoryStream(),
                new MemoryStream(),
                true);

            MockingProviderRegistry.VerifyAnyArgs(
                dependency,
                nameof(IProviderOverloadedDeploymentDependency.CreateDeployment),
                TimesSpec.Once,
                typeof(string),
                typeof(Dictionary<string, int>),
                typeof(Stream),
                typeof(Stream),
                typeof(bool));
            MockingProviderRegistry.VerifyNoOtherCalls(dependency);
        }

        [Fact]
        public void VerifyAnyArgs_ShouldThrowHelpfulMessage_WhenMethodNameIsAmbiguous()
        {
            var mocker = new Mocker();

            var action = () => mocker.VerifyAnyArgs<IProviderOverloadedDeploymentDependency>(nameof(IProviderOverloadedDeploymentDependency.CreateDeployment), TimesSpec.Once);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*is overloaded*parameter types*");
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void CreateStandaloneFastMock_ShouldCreateIndependentUntrackedHandles(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();

            var first = mocker.CreateStandaloneFastMock<IProviderDependency>();
            var second = mocker.CreateStandaloneFastMock<IProviderDependency>();
            var consumer = new DualDependencyConsumer(first.Instance, second.Instance);

            consumer.Primary.Run("alpha");
            consumer.Secondary.Run("beta");

            first.Should().NotBeSameAs(second);
            mocker.TryGetTrackedMock<IProviderDependency>(out var tracked).Should().BeFalse();
            tracked.Should().BeNull();

            if (providerName == "moq")
            {
                first.NativeMock.Should().BeOfType<Mock<IProviderDependency>>();
                second.NativeMock.Should().BeOfType<Mock<IProviderDependency>>();
            }
            else
            {
                first.NativeMock.Should().BeSameAs(first.Instance);
                second.NativeMock.Should().BeSameAs(second.Instance);
            }

            MockingProviderRegistry.Default.Verify(first, x => x.Run("alpha"), TimesSpec.Once);
            MockingProviderRegistry.Default.Verify(second, x => x.Run("beta"), TimesSpec.Once);
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void DetachedVerifyCalledOnce_AndVerifyNotCalled_ShouldWork_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();

            var dependency = mocker.CreateStandaloneFastMock<IProviderDependency>();
            dependency.Instance.Run("alpha");

            MockingProviderRegistry.VerifyCalledOnce(dependency, x => x.Run("alpha"));
            MockingProviderRegistry.VerifyNotCalled(dependency, x => x.Run("beta"));
            MockingProviderRegistry.VerifyNoOtherCalls(dependency);
        }

        [Fact]
        public void VerifyWrappers_ShouldUseProviderBoundMockProvider_WhenDefaultProviderChanges()
        {
            using var providerScope = PushProvider("moq");
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<IProviderDependency>();
            var detached = mocker.CreateStandaloneFastMock<IProviderDependency>();

            dependency.Instance.Run("alpha");
            detached.Instance.Run("beta");

            using var switchedProvider = PushProvider("reflection");

            Action trackedVerify = () => mocker.VerifyCalledOnce<IProviderDependency>(service => service.Run("alpha"));
            Action detachedVerify = () => MockingProviderRegistry.VerifyCalledOnce(detached, service => service.Run("beta"));

            trackedVerify.Should().NotThrow();
            detachedVerify.Should().NotThrow();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void DetachedVerifyCalledExactlyAnyArgs_AtLeastAnyArgs_AndAtMostAnyArgs_ShouldWork_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.CreateStandaloneFastMock<IProviderDeploymentDependency>();

            dependency.Instance.CreateDeployment(
                "alpha",
                new Dictionary<string, int> { ["region"] = 1 },
                new MemoryStream(),
                new MemoryStream(),
                true);
            dependency.Instance.CreateDeployment(
                "beta",
                new Dictionary<string, int> { ["region"] = 2 },
                new MemoryStream(),
                new MemoryStream(),
                false);

            MockingProviderRegistry.VerifyCalledExactlyAnyArgs<IProviderDeploymentDependency, Action<string, Dictionary<string, int>, Stream, Stream, bool>>(
                dependency,
                service => service.CreateDeployment,
                2);
            MockingProviderRegistry.VerifyCalledAtLeastAnyArgs<IProviderDeploymentDependency, Action<string, Dictionary<string, int>, Stream, Stream, bool>>(
                dependency,
                service => service.CreateDeployment,
                2);
            MockingProviderRegistry.VerifyCalledAtMostAnyArgs<IProviderDeploymentDependency, Action<string, Dictionary<string, int>, Stream, Stream, bool>>(
                dependency,
                service => service.CreateDeployment,
                2);
            MockingProviderRegistry.VerifyNoOtherCalls(dependency);
        }

        [Fact]
        public void CreateStandaloneFastMock_WithConstructorArgs_ShouldHonorSelectedProviderWithoutTracking()
        {
            using var providerScope = PushProvider("moq");
            var mocker = new Mocker();
            var endpoint = new Uri("https://fastmoq.test/providers/standalone");
            const string queueName = "standalone";

            var first = mocker.CreateStandaloneFastMock<ProviderConstructedDependency>(new MockCreationOptions(
                ConstructorArgs: [endpoint, queueName]));
            var second = mocker.CreateStandaloneFastMock(typeof(ProviderConstructedDependency), new MockCreationOptions(
                ConstructorArgs: [endpoint, queueName]));

            first.Instance.Endpoint.Should().Be(endpoint);
            first.Instance.QueueName.Should().Be(queueName);
            var secondInstance = second.Instance.Should().BeAssignableTo<ProviderConstructedDependency>().Which;
            secondInstance.Endpoint.Should().Be(endpoint);
            secondInstance.QueueName.Should().Be(queueName);
            second.Instance.Should().NotBeSameAs(first.Instance);
            mocker.TryGetTrackedMock(typeof(ProviderConstructedDependency), out var tracked).Should().BeFalse();
            tracked.Should().BeNull();
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

        [Fact]
        public void ReflectionProvider_Reset_ShouldClearTrackedInvocations()
        {
            using var providerScope = PushProvider("reflection");
            var mocker = new Mocker();

            var dependency = mocker.GetOrCreateMock<IProviderDependency>();
            dependency.Instance.Run("alpha");

            dependency.Reset();

            Action verify = () => mocker.Verify<IProviderDependency>(x => x.Run("alpha"), TimesSpec.Once);
            verify.Should().Throw<InvalidOperationException>();
            mocker.VerifyNoOtherCalls<IProviderDependency>();
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

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public void VerifyLoggedOnce_AndVerifyNotLogged_ShouldWork_ForProvidersThatSupportLoggerCapture(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var logger = mocker.GetObject<ILogger<NullLogger>>();

            logger.Should().NotBeNull();
            logger!.LogInformation("provider info");
            logger.LogError(12, new InvalidOperationException("provider boom"), "provider error");

            mocker.VerifyLoggedOnce(LogLevel.Information, "provider info");
            mocker.VerifyLoggedOnce(LogLevel.Error, "provider error", new InvalidOperationException("provider boom"), 12);
            mocker.VerifyNotLogged(LogLevel.Warning, "provider warning");
            mocker.VerifyNotLogged(LogLevel.Error, "provider error", new InvalidOperationException("other boom"), 12);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public void SetupLoggerCallback_ShouldMirrorCapturedEntries_WithoutBreakingStoredLogs(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var exception = new InvalidOperationException("provider callback boom");
            LogLevel? capturedLevel = null;
            EventId? capturedEventId = null;
            string? capturedMessage = null;
            Exception? capturedException = null;

            mocker.SetupLoggerCallback((logLevel, eventId, message, loggedException) =>
            {
                capturedLevel = logLevel;
                capturedEventId = eventId;
                capturedMessage = message;
                capturedException = loggedException;
            });

            var logger = mocker.GetRequiredObject<ILogger<NullLogger>>();
            logger.LogError(12, exception, "provider callback");

            capturedLevel.Should().Be(LogLevel.Error);
            capturedEventId.Should().Be(new EventId(12));
            capturedMessage.Should().Contain("provider callback");
            capturedException.Should().BeSameAs(exception);
            mocker.LogEntries.Should().Contain(entry =>
                entry.LogLevel == LogLevel.Error &&
                entry.EventId.Id == 12 &&
                entry.Message.Contains("provider callback", StringComparison.Ordinal) &&
                entry.Exception == exception);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        public void SetupLoggerCallbackThreeParameterOverload_ShouldCaptureFormattedMessages(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            LogLevel? capturedLevel = null;
            string? capturedMessage = null;

            mocker.SetupLoggerCallback((logLevel, _, message) =>
            {
                capturedLevel = logLevel;
                capturedMessage = message;
            });

            var logger = mocker.GetRequiredObject<ILogger<NullLogger>>();
            logger.LogInformation("provider callback info");

            capturedLevel.Should().Be(LogLevel.Information);
            capturedMessage.Should().Contain("provider callback info");
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
        public void VerifyLoggedOnce_ShouldFailFast_ForProviderWithoutLoggerCapture()
        {
            using var providerScope = PushProvider("reflection");
            var mocker = new Mocker();

            var action = () => mocker.VerifyLoggedOnce(LogLevel.Information, "provider info");

            action.Should().Throw<NotSupportedException>()
                .WithMessage("*ReflectionMockingProvider*");
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void CreateDiagnosticsSnapshot_ShouldCaptureTrackedMocks_ConstructorHistory_InstanceRegistrations_AndLogs(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var expectedUri = new Uri("https://fastmoq.test/providers/diagnostics");
            var keyedUri = new Uri("https://fastmoq.test/providers/diagnostics/keyed");
            var mocker = new Mocker();

            mocker.AddCapturedLoggerFactory(replace: true);
            mocker.AddType<Uri>(_ => expectedUri, replace: true);
            mocker.AddKeyedType<Uri>("primary", keyedUri, replace: true);

            var dependency = mocker.GetOrCreateMock<IProviderDependency>();
            var keyedDependency = mocker.GetOrCreateMock<IProviderDependency>(new MockRequestOptions
            {
                ServiceKey = "primary",
            });
            var consumer = mocker.CreateInstance<ProviderConsumer>();
            var uriConsumer = mocker.CreateInstance<ProviderUriConsumer>();
            var logger = mocker.GetObject<ILogger<NullLogger>>();

            consumer.Should().NotBeNull();
            consumer!.Dependency.Should().BeSameAs(dependency.Instance);
            keyedDependency.Instance.Run("primary");
            uriConsumer.Should().NotBeNull();
            uriConsumer!.Uri.Should().Be(expectedUri);
            logger.Should().NotBeNull();

            logger!.LogInformation("provider diagnostics");

            var snapshot = mocker.CreateDiagnosticsSnapshot();

            snapshot.ProviderName.Should().Be(MockingProviderRegistry.Default.GetType().Name);
            snapshot.TrackedMocks.Should().Contain(entry => entry.ServiceType == typeof(IProviderDependency).FullName);
            snapshot.TrackedMocks.Should().Contain(entry => entry.ServiceType == typeof(IProviderDependency).FullName && entry.ServiceKey == "primary");
            snapshot.ConstructorSelections.Should().Contain(entry => entry.RequestedType == typeof(ProviderConsumer).FullName);
            snapshot.InstanceRegistrations.Should().Contain(entry => entry.RequestedType == typeof(Uri).FullName && entry.HasFactory);
            snapshot.InstanceRegistrations.Should().Contain(entry => entry.RequestedType == typeof(Uri).FullName && entry.ServiceKey == "primary");
            snapshot.LogEntries.Should().Contain(entry => entry.Message.Contains("provider diagnostics", StringComparison.Ordinal));

            var debugView = snapshot.ToDebugView();
            debugView.Should().Contain("Tracked mocks:");
            debugView.Should().Contain(typeof(IProviderDependency).FullName);
            debugView.Should().Contain(typeof(ProviderConsumer).FullName);
            debugView.Should().Contain(typeof(ProviderUriConsumer).FullName);
            debugView.Should().Contain(typeof(Uri).FullName);
            debugView.Should().Contain("[key: primary]");
            debugView.Should().Contain("provider diagnostics");

            using var json = JsonDocument.Parse(snapshot.ToJson());
            json.RootElement.GetProperty("providerName").GetString().Should().Be(snapshot.ProviderName);
            json.RootElement.GetProperty("trackedMocks").GetArrayLength().Should().BeGreaterThan(0);
            json.RootElement.GetProperty("instanceRegistrations").GetArrayLength().Should().BeGreaterThan(0);
            var hasPrimaryServiceKey = false;
            foreach (var entry in json.RootElement.GetProperty("trackedMocks").EnumerateArray())
            {
                if (entry.TryGetProperty("serviceKey", out var serviceKey) && serviceKey.GetString() == "primary")
                {
                    hasPrimaryServiceKey = true;
                    break;
                }
            }

            hasPrimaryServiceKey.Should().BeTrue();
        }

        [Fact]
        public void CreateDiagnosticsSnapshot_ShouldPreferTrackedMockProviderName_WhenDefaultProviderChanges()
        {
            using var providerScope = PushProvider("moq");
            var mocker = new Mocker();

            _ = mocker.GetOrCreateMock<IProviderDependency>();

            using var switchedProvider = PushProvider("reflection");
            var snapshot = mocker.CreateDiagnosticsSnapshot();

            snapshot.ProviderName.Should().Be(nameof(MoqMockingProvider));
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
        public void DirectVerify_ShouldThrowArgumentNullException_ForNSubstitute()
        {
            var provider = NSubstituteMockingProvider.Instance;
            var mock = provider.CreateMock<IProviderDependency>();

            Action action = () => provider.Verify(mock, null!);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Verify_ShouldFallbackToLegacyPath_ForPropertySetter_WhenNSubstituteIsActive()
        {
            using var providerScope = PushProvider("nsubstitute");
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<IPropertySetterDependency>();
            var parameter = Expression.Parameter(typeof(IPropertySetterDependency), "dependency");
            var propertySetter = Expression.Lambda<Action<IPropertySetterDependency>>(
                Expression.Assign(
                    Expression.Property(parameter, nameof(IPropertySetterDependency.Value)),
                    Expression.Constant("alpha", typeof(string))),
                parameter);

            dependency.Instance.Value = "alpha";

            Action action = () => mocker.Verify(propertySetter, TimesSpec.Once);

            action.Should().NotThrow();
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
            MockingProviderRegistry.RegisteredProviderNames.Should().Contain(["moq", "reflection"]);
        }

        [Fact]
        public void Push_ShouldRegisterKnownOptionalProvider_WhenPackageIsAvailable()
        {
            ResetRegistry(includeOptionalProviders: false);

            try
            {
                using var providerScope = MockingProviderRegistry.Push("nsubstitute");

                MockingProviderRegistry.Default.Should().BeSameAs(GetProvider("nsubstitute"));
                MockingProviderRegistry.RegisteredProviderNames.Should().Contain("nsubstitute");
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void Default_ShouldTriggerDiscovery_WhenAccessedDirectly()
        {
            MockingProviderRegistry.Clear();
            MockingProviderRegistry.Register("reflection", ReflectionMockingProvider.Instance, setAsDefault: false);

            try
            {
                var discoveryExecutionsBefore = GetDiscoveryExecutionCount();

                _ = MockingProviderRegistry.Default;

                GetDiscoveryExecutionCount().Should().BeGreaterThan(discoveryExecutionsBefore);
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void TryGet_ShouldStopRescanningUnknownProviderAfterDiscoverySettles()
        {
            ResetRegistry(includeOptionalProviders: false);

            try
            {
                var discoveryExecutionsBefore = GetDiscoveryExecutionCount();

                MockingProviderRegistry.TryGet("missing-alpha", out _).Should().BeFalse();
                var discoveryExecutionsAfterFirstMiss = GetDiscoveryExecutionCount();

                MockingProviderRegistry.TryGet("missing-beta", out _).Should().BeFalse();
                var discoveryExecutionsAfterSecondMiss = GetDiscoveryExecutionCount();

                MockingProviderRegistry.TryGet("missing-gamma", out _).Should().BeFalse();
                var discoveryExecutionsAfterThirdMiss = GetDiscoveryExecutionCount();

                discoveryExecutionsAfterFirstMiss.Should().BeGreaterThan(discoveryExecutionsBefore);
                discoveryExecutionsAfterThirdMiss.Should().Be(discoveryExecutionsAfterSecondMiss);
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void TryGet_ShouldDiscoverLoadedAssemblyProvider_WhenDiscoveryModeIsLoadedAssembliesOnly()
        {
            ResetRegistry(includeOptionalProviders: false, includeMoqProvider: false);

            try
            {
                MockingProviderRegistry.DiscoveryMode = MockingProviderDiscoveryMode.LoadedAssembliesOnly;

                MockingProviderRegistry.TryGet("nsubstitute", out var provider).Should().BeTrue();
                provider.Should().BeSameAs(NSubstituteMockingProvider.Instance);
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void TryGet_ShouldIgnoreConventionDiscovery_WhenDiscoveryModeIsExplicitOnly()
        {
            ResetRegistry(includeOptionalProviders: false, includeMoqProvider: false);

            try
            {
                MockingProviderRegistry.DiscoveryMode = MockingProviderDiscoveryMode.ExplicitOnly;
                var providerName = typeof(AutoDiscoveredCustomMockingProvider).FullName
                    ?? throw new InvalidOperationException("Unable to resolve the full name for AutoDiscoveredCustomMockingProvider.");

                MockingProviderRegistry.TryGet(providerName, out _).Should().BeFalse();
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void TryGet_ShouldHonorExplicitAssemblyRegistration_WhenDiscoveryModeIsExplicitOnly()
        {
            ResetRegistry(includeOptionalProviders: false, includeMoqProvider: false);

            try
            {
                MockingProviderRegistry.DiscoveryMode = MockingProviderDiscoveryMode.ExplicitOnly;

                MockingProviderRegistry.TryGet("nsubstitute", out var provider).Should().BeTrue();
                provider.Should().BeSameAs(NSubstituteMockingProvider.Instance);
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void ApplyImplicitDefaultProvider_ShouldUseSingleNonReflectionProvider_WhenOnlyOneExists()
        {
            MockingProviderRegistry.Clear();
            MockingProviderRegistry.Register("reflection", ReflectionMockingProvider.Instance, setAsDefault: false);
            MockingProviderRegistry.Register("nsubstitute", NSubstituteMockingProvider.Instance, setAsDefault: false);

            MockingProviderRegistry.ApplyImplicitDefaultProvider();

            MockingProviderRegistry.Default.Should().BeSameAs(NSubstituteMockingProvider.Instance);
        }

        [Fact]
        public void ApplyImplicitDefaultProvider_ShouldKeepExplicitDefaultProvider_WhenOneCustomProviderExists()
        {
            MockingProviderRegistry.Clear();
            MockingProviderRegistry.Register("reflection", ReflectionMockingProvider.Instance, setAsDefault: true);
            MockingProviderRegistry.Register("nsubstitute", NSubstituteMockingProvider.Instance, setAsDefault: false);

            MockingProviderRegistry.ApplyImplicitDefaultProvider();

            MockingProviderRegistry.Default.Should().BeSameAs(ReflectionMockingProvider.Instance);
        }

        [Fact]
        public void ApplyImplicitDefaultProvider_ShouldKeepReflection_WhenMultipleNonReflectionProvidersExist()
        {
            MockingProviderRegistry.Clear();
            MockingProviderRegistry.Register("reflection", ReflectionMockingProvider.Instance, setAsDefault: false);
            MockingProviderRegistry.Register("moq", MoqMockingProvider.Instance, setAsDefault: false);
            MockingProviderRegistry.Register("nsubstitute", NSubstituteMockingProvider.Instance, setAsDefault: false);

            MockingProviderRegistry.ApplyImplicitDefaultProvider();

            GetStoredDefaultProvider().Should().BeSameAs(ReflectionMockingProvider.Instance);
        }

        [Fact]
        public void ApplyImplicitDefaultProvider_ShouldIgnoreReflectionAliases_WhenChoosingSingleNonReflectionProvider()
        {
            MockingProviderRegistry.Clear();
            MockingProviderRegistry.Register("baseline", ReflectionMockingProvider.Instance, setAsDefault: false);
            MockingProviderRegistry.Register("nsubstitute", NSubstituteMockingProvider.Instance, setAsDefault: false);

            MockingProviderRegistry.ApplyImplicitDefaultProvider();

            MockingProviderRegistry.Default.Should().BeSameAs(NSubstituteMockingProvider.Instance);
        }

        [Fact]
        public void ApplyAssemblyProviderRegistrations_ShouldRegisterCustomProviderByFullTypeName_WhenAssemblyContainsPublicProviderImplementation()
        {
            ResetRegistry(includeOptionalProviders: false, includeMoqProvider: false);

            try
            {
                var providerName = typeof(AutoDiscoveredCustomMockingProvider).FullName
                    ?? throw new InvalidOperationException("Unable to resolve the full name for AutoDiscoveredCustomMockingProvider.");

                MockingProviderRegistry.RegisteredProviderNames.Should().NotContain(providerName);

                MockingProviderRegistry.ApplyAssemblyProviderRegistrations([typeof(AutoDiscoveredCustomMockingProvider).Assembly]);

                MockingProviderRegistry.RegisteredProviderNames.Should().Contain(providerName);
                GetProvider(providerName).Should().BeSameAs(AutoDiscoveredCustomMockingProvider.Instance);
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void ApplyAssemblyProviderRegistrations_ShouldExposeConventionRegistrationMetadata_ForAutoDiscoveredProvider()
        {
            ResetRegistry(includeOptionalProviders: false, includeMoqProvider: false);

            try
            {
                var providerName = typeof(AutoDiscoveredCustomMockingProvider).FullName
                    ?? throw new InvalidOperationException("Unable to resolve the full name for AutoDiscoveredCustomMockingProvider.");

                MockingProviderRegistry.ApplyAssemblyProviderRegistrations([typeof(AutoDiscoveredCustomMockingProvider).Assembly]);

                MockingProviderRegistry.RegisteredProviders.Should().ContainSingle(info =>
                    info.Name == providerName &&
                    info.ProviderType == typeof(AutoDiscoveredCustomMockingProvider) &&
                    info.Source == MockingProviderRegistrationSource.ConventionDiscovery);
                MockingProviderRegistry.DiscoveryWarnings.Should().BeEmpty();
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void ApplyAssemblyProviderRegistrations_ShouldPreferExplicitAlias_OverFallbackFullTypeName_ForCustomProvider()
        {
            ResetRegistry(includeOptionalProviders: false, includeMoqProvider: false);

            try
            {
                const string alias = "custom-alias";
                var providerName = typeof(AutoDiscoveredCustomMockingProvider).FullName
                    ?? throw new InvalidOperationException("Unable to resolve the full name for AutoDiscoveredCustomMockingProvider.");
                var assembly = CreateAssemblyWithRegisterProviderAttribute(alias, typeof(AutoDiscoveredCustomMockingProvider));

                MockingProviderRegistry.ApplyAssemblyProviderRegistrations([assembly, typeof(AutoDiscoveredCustomMockingProvider).Assembly]);

                MockingProviderRegistry.RegisteredProviderNames.Should().Contain(alias);
                MockingProviderRegistry.RegisteredProviderNames.Should().NotContain(providerName);
                GetProvider(alias).Should().BeSameAs(AutoDiscoveredCustomMockingProvider.Instance);
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void ApplyAssemblyProviderRegistrations_ShouldSkipConventionFallbackName_WhenThatNameAlreadyMapsToAnotherProvider()
        {
            ResetRegistry(includeOptionalProviders: false, includeMoqProvider: false);

            try
            {
                var providerName = typeof(AutoDiscoveredCustomMockingProvider).FullName
                    ?? throw new InvalidOperationException("Unable to resolve the full name for AutoDiscoveredCustomMockingProvider.");
                var assembly = CreateAssemblyWithRegisterProviderAttribute(providerName, typeof(MoqMockingProvider));

                MockingProviderRegistry.ApplyAssemblyProviderRegistrations([assembly, typeof(AutoDiscoveredCustomMockingProvider).Assembly]);

                GetProvider(providerName).Should().BeSameAs(MoqMockingProvider.Instance);
                MockingProviderRegistry.RegisteredProviders.Should().ContainSingle(info =>
                    info.Name == providerName &&
                    info.ProviderType == typeof(MoqMockingProvider) &&
                    info.Source == MockingProviderRegistrationSource.AssemblyAttribute);
                var warning = MockingProviderRegistry.DiscoveryWarnings.Should().ContainSingle().Which;
                warning.Should().Contain(providerName);
                warning.Should().Contain(typeof(MoqMockingProvider).FullName);
                warning.Should().Contain(typeof(AutoDiscoveredCustomMockingProvider).FullName);
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void ApplyAssemblyProviderRegistrations_ShouldNotOverwriteExistingRegisteredProviderName()
        {
            ResetRegistry(includeOptionalProviders: false, includeMoqProvider: false);

            try
            {
                const string alias = "custom-provider";
                var assembly = CreateAssemblyWithRegisterProviderAttribute(alias, typeof(MoqMockingProvider));

                MockingProviderRegistry.Register(alias, NSubstituteMockingProvider.Instance, setAsDefault: false);

                MockingProviderRegistry.ApplyAssemblyProviderRegistrations([assembly]);

                GetProvider(alias).Should().BeSameAs(NSubstituteMockingProvider.Instance);
                MockingProviderRegistry.RegisteredProviders.Should().ContainSingle(info =>
                    info.Name == alias &&
                    info.ProviderType == typeof(NSubstituteMockingProvider) &&
                    info.Source == MockingProviderRegistrationSource.RuntimeRegistration);

                var warning = MockingProviderRegistry.DiscoveryWarnings.Should().ContainSingle().Which;
                warning.Should().Contain(alias);
                warning.Should().Contain(typeof(NSubstituteMockingProvider).FullName);
                warning.Should().Contain(typeof(MoqMockingProvider).FullName);
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void ApplyImplicitDefaultProvider_ShouldUseAutoDiscoveredCustomProvider_WhenItIsTheOnlyNonReflectionProvider()
        {
            var (assembly, providerType) = CreateAssemblyWithConventionProviderType();
            var providerName = providerType.FullName
                ?? throw new InvalidOperationException("Unable to resolve the full name for the convention-discovered provider type.");

            MockingProviderRegistry.Clear();
            MockingProviderRegistry.Register("reflection", ReflectionMockingProvider.Instance, setAsDefault: false);
            MockingProviderRegistry.ApplyAssemblyProviderRegistrations([assembly]);

            MockingProviderRegistry.ApplyImplicitDefaultProvider();

            MockingProviderRegistry.RegisteredProviderNames.Should().Contain(providerName);
            MockingProviderRegistry.Default.Should().BeSameAs(GetProvider(providerName));
            MockingProviderRegistry.Default.GetType().Should().Be(providerType);
        }

        [Fact]
        public void ApplyAssemblyProviderRegistrations_ShouldRegisterMoq_WhenAssemblyDeclaresProviderRegistration()
        {
            ResetRegistry(includeOptionalProviders: false, includeMoqProvider: false);

            try
            {
                var assembly = CreateAssemblyWithRegisterProviderAttribute("moq", typeof(MoqMockingProvider));

                MockingProviderRegistry.RegisteredProviderNames.Should().NotContain("moq");

                MockingProviderRegistry.ApplyAssemblyProviderRegistrations([assembly]);

                MockingProviderRegistry.RegisteredProviderNames.Should().Contain("moq");
                GetProvider("moq").Should().BeSameAs(MoqMockingProvider.Instance);
                MockingProviderRegistry.Default.Should().BeSameAs(GetProvider("reflection"));
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void ApplyAssemblyProviderRegistrations_ShouldRegisterNSubstitute_WhenAssemblyDeclaresProviderRegistration()
        {
            ResetRegistry(includeOptionalProviders: false);

            try
            {
                var assembly = CreateAssemblyWithRegisterProviderAttribute("nsubstitute", typeof(NSubstituteMockingProvider));

                MockingProviderRegistry.RegisteredProviderNames.Should().NotContain("nsubstitute");

                MockingProviderRegistry.ApplyAssemblyProviderRegistrations([assembly]);

                MockingProviderRegistry.RegisteredProviderNames.Should().Contain("nsubstitute");
                GetProvider("nsubstitute").Should().BeSameAs(NSubstituteMockingProvider.Instance);
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void ApplyAssemblyProviderRegistrations_ShouldThrow_WhenAssembliesDeclareDifferentTypesForSameProviderName()
        {
            ResetRegistry(includeOptionalProviders: false, includeMoqProvider: false);

            try
            {
                var moqAssembly = CreateAssemblyWithRegisterProviderAttribute("primary", typeof(MoqMockingProvider));
                var nsubstituteAssembly = CreateAssemblyWithRegisterProviderAttribute("primary", typeof(NSubstituteMockingProvider));

                Action action = () => MockingProviderRegistry.ApplyAssemblyProviderRegistrations([moqAssembly, nsubstituteAssembly]);

                action.Should().Throw<InvalidOperationException>()
                    .WithMessage("*Multiple FastMoq provider registrations were declared for 'primary'*");
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void ApplyAssemblyDefaultProviders_ShouldSetDefaultToMoq_WhenAssemblyDeclaresMoq()
        {
            ResetRegistry(includeOptionalProviders: true);

            try
            {
                var assembly = CreateAssemblyWithDefaultProviderAttribute("moq");

                MockingProviderRegistry.ApplyAssemblyDefaultProviders([assembly]);

                MockingProviderRegistry.Default.Should().BeSameAs(GetProvider("moq"));
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void ApplyAssemblyDefaultProviders_ShouldSetDefaultToMoq_WhenAssemblyRegistersMoqAsDefault()
        {
            ResetRegistry(includeOptionalProviders: false, includeMoqProvider: false);

            try
            {
                var assembly = CreateAssemblyWithRegisterProviderAttribute("moq", typeof(MoqMockingProvider), setAsDefault: true);

                MockingProviderRegistry.ApplyAssemblyProviderRegistrations([assembly]);
                MockingProviderRegistry.ApplyAssemblyDefaultProviders([assembly]);

                MockingProviderRegistry.Default.Should().BeSameAs(GetProvider("moq"));
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void ApplyAssemblyDefaultProviders_ShouldSetDefaultToNSubstitute_WhenAssemblyDeclaresNSubstitute()
        {
            ResetRegistry(includeOptionalProviders: false);

            try
            {
                var assembly = CreateAssemblyWithDefaultProviderAttribute("nsubstitute");

                MockingProviderRegistry.ApplyAssemblyDefaultProviders([assembly]);

                MockingProviderRegistry.Default.Should().BeSameAs(GetProvider("nsubstitute"));
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void ApplyAssemblyDefaultProviders_ShouldSetDefaultToNSubstitute_WhenAssemblyRegistersNSubstituteAsDefault()
        {
            ResetRegistry(includeOptionalProviders: false, includeMoqProvider: false);

            try
            {
                var assembly = CreateAssemblyWithRegisterProviderAttribute("nsubstitute", typeof(NSubstituteMockingProvider), setAsDefault: true);

                MockingProviderRegistry.ApplyAssemblyProviderRegistrations([assembly]);
                MockingProviderRegistry.ApplyAssemblyDefaultProviders([assembly]);

                MockingProviderRegistry.Default.Should().BeSameAs(GetProvider("nsubstitute"));
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
        }

        [Fact]
        public void ApplyAssemblyDefaultProviders_ShouldThrow_WhenAssembliesDeclareDifferentDefaults()
        {
            ResetRegistry(includeOptionalProviders: false);

            try
            {
                var moqAssembly = CreateAssemblyWithDefaultProviderAttribute("moq");
                var nsubstituteAssembly = CreateAssemblyWithDefaultProviderAttribute("nsubstitute");

                Action action = () => MockingProviderRegistry.ApplyAssemblyDefaultProviders([moqAssembly, nsubstituteAssembly]);

                action.Should().Throw<InvalidOperationException>()
                    .WithMessage("*Multiple FastMoq default providers were declared*");
            }
            finally
            {
                ResetRegistry(includeOptionalProviders: true);
            }
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
                var exception = action.Should().Throw<NotSupportedException>().Which;
                exception.Message.Should().Contain("requires the 'moq' provider");
                exception.Message.Should().Contain($"active provider is '{providerName}'");
                exception.Message.Should().Contain("MockingProviderRegistry.Push(\"moq\")");
                return;
            }

            action.Should().NotThrow();
        }

        [Theory]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public void MockModel_MockProperty_ShouldProvideProviderSelectionMessage_WhenProviderIsNotMoq(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();

            mocker.CreateMock<IProviderDependency>();
            var model = mocker.GetMockModel<IProviderDependency>();

            Action action = () => _ = model.Mock;

            var exception = action.Should().Throw<NotSupportedException>().Which;
            exception.Message.Should().Contain("requires the 'moq' provider");
            exception.Message.Should().Contain($"active provider is '{providerName}'");
            exception.Message.Should().Contain("MockingProviderRegistry.Push(\"moq\")");
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
        public void BuildExpressionCompatibility_ShouldWorkWithMoqSetupShortcut_WhenMoqIsActive()
        {
            using var providerScope = PushProvider("moq");
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<IExpressionConsumer>();

            dependency
                .Setup(x => x.Match(Mocker.BuildExpression<string>()))
                .Returns(true);

            var matched = dependency.Instance.Match(value => value == "alpha");

            matched.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void BuildExpression_ShouldReturnProviderSafePredicate_ForAllProviders(string providerName)
        {
            using var providerScope = PushProvider(providerName);

            var expression = Mocker.BuildExpression<string>();

            expression.Should().NotBeNull();
            expression.Compile().Invoke("alpha").Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(ProviderNames))]
        public void FastArgAnyExpression_ShouldReturnProviderSafePredicate_ForAllProviders(string providerName)
        {
            using var providerScope = PushProvider(providerName);

            var expression = FastArg.AnyExpression<string>();

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

        private static void ResetRegistry(bool includeOptionalProviders, bool includeMoqProvider = true)
        {
            MockingProviderRegistry.DiscoveryMode = MockingProviderDiscoveryMode.Automatic;
            MockingProviderRegistry.Clear();
            MockingProviderRegistry.Register("reflection", ReflectionMockingProvider.Instance, setAsDefault: true);

            if (includeMoqProvider)
            {
                MockingProviderRegistry.Register("moq", MoqMockingProvider.Instance, setAsDefault: false);
            }

            if (includeOptionalProviders)
            {
                MockingProviderRegistry.Register("nsubstitute", NSubstituteMockingProvider.Instance, setAsDefault: false);
            }

            MockingProviderRegistry.SetDefault(includeMoqProvider ? "moq" : "reflection");
        }

        private static Assembly CreateAssemblyWithDefaultProviderAttribute(string providerName)
        {
            var assemblyName = new AssemblyName($"FastMoq.DynamicProvider_{providerName}_{Guid.NewGuid():N}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var attributeConstructor = typeof(FastMoqDefaultProviderAttribute).GetConstructor([typeof(string)])
                ?? throw new InvalidOperationException("Unable to find FastMoqDefaultProviderAttribute(string) constructor.");
            var attribute = new CustomAttributeBuilder(attributeConstructor, [providerName]);

            assemblyBuilder.SetCustomAttribute(attribute);
            return assemblyBuilder;
        }

        private static Assembly CreateAssemblyWithRegisterProviderAttribute(string providerName, Type providerType, bool setAsDefault = false)
        {
            var assemblyName = new AssemblyName($"FastMoq.DynamicProviderRegistration_{providerName}_{Guid.NewGuid():N}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var attributeConstructor = typeof(FastMoqRegisterProviderAttribute).GetConstructor([typeof(string), typeof(Type)])
                ?? throw new InvalidOperationException("Unable to find FastMoqRegisterProviderAttribute(string, Type) constructor.");

            if (setAsDefault)
            {
                var setAsDefaultProperty = typeof(FastMoqRegisterProviderAttribute).GetProperty(nameof(FastMoqRegisterProviderAttribute.SetAsDefault))
                    ?? throw new InvalidOperationException("Unable to find FastMoqRegisterProviderAttribute.SetAsDefault property.");
                var attribute = new CustomAttributeBuilder(attributeConstructor, [providerName, providerType], [setAsDefaultProperty], [true]);

                assemblyBuilder.SetCustomAttribute(attribute);
                return assemblyBuilder;
            }

            assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(attributeConstructor, [providerName, providerType]));
            return assemblyBuilder;
        }

        private static (Assembly Assembly, Type ProviderType) CreateAssemblyWithConventionProviderType()
        {
            var assemblyName = new AssemblyName($"FastMoq.DynamicConventionProvider_{Guid.NewGuid():N}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"{assemblyName.Name}.dll");
            var providerTypeName = $"FastMoq.DynamicProviders.AutoDiscovered_{Guid.NewGuid():N}";
            var typeBuilder = moduleBuilder.DefineType(
                providerTypeName,
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed,
                typeof(ConventionDiscoveredDelegatingMockingProviderBase));

            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            var providerType = typeBuilder.CreateType()
                ?? throw new InvalidOperationException("Unable to create the convention-discovered provider type.");

            return (assemblyBuilder, providerType);
        }

        private static IMockingProvider GetProvider(string providerName)
        {
            if (!MockingProviderRegistry.TryGet(providerName, out var provider))
            {
                throw new InvalidOperationException($"Unable to find provider '{providerName}'.");
            }

            return provider;
        }

        private static int GetDiscoveryExecutionCount()
        {
            var field = typeof(MockingProviderRegistry).GetField("_discoveryExecutionCount", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("Unable to find MockingProviderRegistry._discoveryExecutionCount.");

            return (int)(field.GetValue(null)
                ?? throw new InvalidOperationException("MockingProviderRegistry._discoveryExecutionCount was null."));
        }

        private static IMockingProvider? GetStoredDefaultProvider()
        {
            var field = typeof(MockingProviderRegistry).GetField("_default", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("Unable to find MockingProviderRegistry._default.");

            return (IMockingProvider?)field.GetValue(null);
        }

        public interface IProviderDependency
        {
            void Run(string value);
        }

        public interface IProviderResultDependency
        {
            string? Lookup(string value);
        }

        public interface IPropertySetterDependency
        {
            string? Value { get; set; }
        }

        public interface IProviderDeploymentDependency
        {
            void CreateDeployment(string deploymentName, Dictionary<string, int> parameters, Stream template, Stream output, bool whatIf);
        }

        public interface IProviderOverloadedDeploymentDependency
        {
            void CreateDeployment(string deploymentName);

            void CreateDeployment(string deploymentName, Dictionary<string, int> parameters, Stream template, Stream output, bool whatIf);
        }

        public interface INullableProviderDependency
        {
            void Run(string? value);
        }

        public class ProviderConsumer(IProviderDependency dependency)
        {
            public IProviderDependency Dependency { get; } = dependency;
        }

        public class ProviderUriConsumer(Uri uri)
        {
            public Uri Uri { get; } = uri;
        }

        public class DualDependencyConsumer(IProviderDependency primary, IProviderDependency secondary)
        {
            public IProviderDependency Primary { get; } = primary;
            public IProviderDependency Secondary { get; } = secondary;
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

        public class ProviderConstructedDependency(Uri endpoint, string queueName)
        {
            public Uri Endpoint { get; } = endpoint;
            public string QueueName { get; } = queueName;
        }

        public interface IExpressionConsumer
        {
            bool Match(Expression<Func<string, bool>> predicate);
        }

        public interface IExpressionActionConsumer
        {
            void Observe(Expression<Func<string, bool>> predicate);
        }

        /// <summary>
        /// Test-only abstract base provider used by convention-discovery tests to delegate behavior to NSubstitute.
        /// </summary>
        public abstract class ConventionDiscoveredDelegatingMockingProviderBase : IMockingProvider, IMethodVerifyingMockingProvider
        {
            /// <inheritdoc />
            public IMockingProviderCapabilities Capabilities => NSubstituteMockingProvider.Instance.Capabilities;

            /// <inheritdoc />
            public Expression<Func<T, bool>> BuildExpression<T>()
            {
                return NSubstituteMockingProvider.Instance.BuildExpression<T>();
            }

            /// <inheritdoc />
            public IFastMock<T> CreateMock<T>(MockCreationOptions? options = null) where T : class
            {
                return NSubstituteMockingProvider.Instance.CreateMock<T>(options);
            }

            /// <inheritdoc />
            public IFastMock CreateMock(Type type, MockCreationOptions? options = null)
            {
                return NSubstituteMockingProvider.Instance.CreateMock(type, options);
            }

            /// <inheritdoc />
            public void SetupAllProperties(IFastMock mock)
            {
                NSubstituteMockingProvider.Instance.SetupAllProperties(mock);
            }

            /// <inheritdoc />
            public void SetCallBase(IFastMock mock, bool value)
            {
                NSubstituteMockingProvider.Instance.SetCallBase(mock, value);
            }

            /// <inheritdoc />
            public void Verify<T>(IFastMock<T> mock, Expression<Action<T>> expression, TimesSpec? times = null) where T : class
            {
                NSubstituteMockingProvider.Instance.Verify(mock, expression, times);
            }

            /// <inheritdoc />
            public void VerifyMethod<T>(IFastMock<T> mock, MethodInfo method, TimesSpec? times = null) where T : class
            {
                NSubstituteMockingProvider.Instance.VerifyMethod(mock, method, times);
            }

            /// <inheritdoc />
            public void VerifyNoOtherCalls(IFastMock mock)
            {
                NSubstituteMockingProvider.Instance.VerifyNoOtherCalls(mock);
            }

            /// <inheritdoc />
            public void ConfigureProperties(IFastMock mock)
            {
                NSubstituteMockingProvider.Instance.ConfigureProperties(mock);
            }

            /// <inheritdoc />
            public void ConfigureLogger(IFastMock mock, Action<LogLevel, EventId, string, Exception?> callback)
            {
                NSubstituteMockingProvider.Instance.ConfigureLogger(mock, callback);
            }

            /// <inheritdoc />
            public object? TryGetLegacy(IFastMock mock)
            {
                return NSubstituteMockingProvider.Instance.TryGetLegacy(mock);
            }

            /// <inheritdoc />
            public IFastMock? TryWrapLegacy(object legacyMock, Type mockedType)
            {
                return NSubstituteMockingProvider.Instance.TryWrapLegacy(legacyMock, mockedType);
            }
        }

        /// <summary>
        /// Test-only convention-discoverable provider used to validate fallback registration by full type name.
        /// </summary>
        public sealed class AutoDiscoveredCustomMockingProvider : ConventionDiscoveredDelegatingMockingProviderBase
        {
            /// <summary>
            /// Gets the shared singleton instance for the test provider.
            /// </summary>
            public static readonly AutoDiscoveredCustomMockingProvider Instance = new();

            private AutoDiscoveredCustomMockingProvider()
            {
            }
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
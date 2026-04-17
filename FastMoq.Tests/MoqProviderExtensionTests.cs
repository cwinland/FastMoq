using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable FMOQ0003 // Intentional coverage for Moq VerifyLogger compatibility shortcuts.

namespace FastMoq.Tests
{
    public class MoqProviderExtensionTests
    {
        [Fact]
        public void AsMoq_ShouldReturnUnderlyingMock_WhenProviderIsMoq()
        {
            using var providerScope = MockingProviderRegistry.Push("moq");
            var mocker = new Mocker();

            var dependency = mocker.GetOrCreateMock<ProviderTests.IProviderDependency>();

            var mock = dependency.AsMoq();

            mock.Should().BeSameAs(dependency.NativeMock);
        }

        [Fact]
        public void AsMoqNonGeneric_ShouldReturnUnderlyingMock_WhenProviderIsMoq()
        {
            using var providerScope = MockingProviderRegistry.Push("moq");
            var mocker = new Mocker();

            IFastMock dependency = mocker.GetOrCreateMock<ProviderTests.IProviderDependency>();

            var mock = dependency.AsMoq();

            mock.Should().BeSameAs(dependency.NativeMock);
        }

        [Theory]
        [InlineData("reflection")]
        [InlineData("nsubstitute")]
        public void AsMoq_ShouldThrow_WhenProviderIsNotMoq(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            var dependency = mocker.GetOrCreateMock<ProviderTests.IProviderDependency>();

            Action action = () => dependency.AsMoq();

            var exception = action.Should().Throw<NotSupportedException>().Which;
            exception.Message.Should().Contain("requires the 'moq' provider");
            exception.Message.Should().Contain($"active provider is '{providerName}'");
            exception.Message.Should().Contain("MockingProviderRegistry.Push(\"moq\")");
        }

        [Theory]
        [InlineData("reflection")]
        [InlineData("nsubstitute")]
        public void AsMoqNonGeneric_ShouldThrow_WhenProviderIsNotMoq(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            IFastMock dependency = mocker.GetOrCreateMock<ProviderTests.IProviderDependency>();

            Action action = () => dependency.AsMoq();

            var exception = action.Should().Throw<NotSupportedException>().Which;
            exception.Message.Should().Contain("requires the 'moq' provider");
            exception.Message.Should().Contain($"active provider is '{providerName}'");
            exception.Message.Should().Contain("MockingProviderRegistry.Push(\"moq\")");
        }

        [Fact]
        public void SetupShortcut_ShouldConfigureTrackedMock_WithoutCallingAsMoq()
        {
            using var providerScope = MockingProviderRegistry.Push("moq");
            var mocker = new Mocker();

            var dependency = mocker.GetOrCreateMock<ProviderTests.IProviderDependency>();

            dependency.Setup(x => x.Run("alpha")).Verifiable();

            dependency.Instance.Run("alpha");

            dependency.AsMoq().Verify(x => x.Run("alpha"), Times.Once);
        }

        [Fact]
        public void SetupResultShortcut_ShouldConfigureTrackedMock_WithoutCallingAsMoq()
        {
            using var providerScope = MockingProviderRegistry.Push("moq");
            var mocker = new Mocker();

            var dependency = mocker.GetOrCreateMock<IProviderValueDependency>();

            dependency.Setup(x => x.GetValue()).Returns("configured");

            dependency.Instance.GetValue().Should().Be("configured");
        }

        [Fact]
        public void SetupGetShortcut_ShouldConfigureTrackedMockProperty_WithoutCallingAsMoq()
        {
            using var providerScope = MockingProviderRegistry.Push("moq");
            var mocker = new Mocker();

            var dependency = mocker.GetOrCreateMock<IProviderPropertyDependency>();

            dependency.SetupGet(x => x.Value).Returns("configured");

            dependency.Instance.Value.Should().Be("configured");
        }

        [Fact]
        public void SetupSequenceShortcut_ShouldConfigureTrackedMock_WithoutCallingAsMoq()
        {
            using var providerScope = MockingProviderRegistry.Push("moq");
            var mocker = new Mocker();

            var dependency = mocker.GetOrCreateMock<IProviderValueDependency>();

            dependency.SetupSequence(x => x.GetValue())
                .Returns("first")
                .Returns("second");

            dependency.Instance.GetValue().Should().Be("first");
            dependency.Instance.GetValue().Should().Be("second");
        }

        [Theory]
        [InlineData("reflection")]
        [InlineData("nsubstitute")]
        public void MoqAuthoringShortcuts_ShouldThrowClearProviderMismatch_WhenProviderIsNotMoq(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            var dependency = mocker.GetOrCreateMock<ProviderTests.IProviderDependency>();
            var valueDependency = mocker.GetOrCreateMock<IProviderValueDependency>();
            var propertyDependency = mocker.GetOrCreateMock<IProviderPropertyDependency>();
            var handler = mocker.GetOrCreateMock<ProtectedShortcutDependency>();

            AssertRequiresMoqProvider(() => dependency.Setup(x => x.Run("alpha")), providerName);
            AssertRequiresMoqProvider(() => valueDependency.Setup(x => x.GetValue()), providerName);
            AssertRequiresMoqProvider(() => propertyDependency.SetupGet(x => x.Value), providerName);
            AssertRequiresMoqProvider(() => valueDependency.SetupSequence(x => x.GetValue()), providerName);
            AssertRequiresMoqProvider(() => handler.Protected(), providerName);
        }

        [Fact]
        public async Task ProtectedShortcut_ShouldExposeMoqProtectedApi_ForTrackedMock()
        {
            using var providerScope = MockingProviderRegistry.Push("moq");
            var mocker = new Mocker();

            var handler = mocker.GetOrCreateMock<HttpMessageHandler>();

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));

            var client = new HttpClient(handler.Instance)
            {
                BaseAddress = new Uri("http://localhost/"),
            };

            var response = await client.GetAsync("orders");

            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        [Fact]
        public void VerifyLoggerShortcut_ShouldUseTrackedLoggerMock_WithoutCallingAsMoq()
        {
            using var providerScope = MockingProviderRegistry.Push("moq");
            var mocker = new Mocker();

            var logger = mocker.GetOrCreateMock<ILogger<NullLogger>>();

            logger.Instance.LogInformation("processed order");

            logger.VerifyLogger(LogLevel.Information, "processed order", times: 1);
        }

        [Fact]
        public void VerifyLoggerShortcut_ShouldUseNonGenericLoggerMock_WithoutCallingAsMoq()
        {
            using var providerScope = MockingProviderRegistry.Push("moq");
            var mocker = new Mocker();

            var logger = mocker.GetOrCreateMock<ILogger>();

            logger.Instance.LogInformation("processed order");

            logger.VerifyLogger(LogLevel.Information, "processed order", times: 1);
        }

        [Fact]
        public void VerifyLoggerShortcut_ShouldSupportNonGenericExceptionOverload_WithoutCallingAsMoq()
        {
            using var providerScope = MockingProviderRegistry.Push("moq");
            var mocker = new Mocker();

            var logger = mocker.GetOrCreateMock<ILogger>();
            var exception = new InvalidOperationException("boom");

            logger.Instance.LogError(7, exception, "processing failed");

            logger.VerifyLogger(LogLevel.Error, "processing failed", exception, eventId: 7, times: 1);
        }

        [Fact]
        public void VerifyLoggerShortcut_ShouldSupportGenericExceptionOverload_WithoutCallingAsMoq()
        {
            using var providerScope = MockingProviderRegistry.Push("moq");
            var mocker = new Mocker();

            var logger = mocker.GetOrCreateMock<ILogger<NullLogger>>();
            var exception = new InvalidOperationException("boom");

            logger.Instance.LogError(9, exception, "processing failed");

            logger.VerifyLogger(LogLevel.Error, "processing failed", exception, eventId: 9, times: 1);
        }

        [Theory]
        [InlineData("reflection")]
        [InlineData("nsubstitute")]
        public void MoqLoggerCompatibilityShortcuts_ShouldThrowClearProviderMismatch_WhenProviderIsNotMoq(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            var logger = mocker.GetOrCreateMock<ILogger>();
            var genericLogger = mocker.GetOrCreateMock<ILogger<NullLogger>>();
            var exception = new InvalidOperationException("boom");

            AssertRequiresMoqProvider(() => logger.VerifyLogger(LogLevel.Information, "processed order", times: 1), providerName);
            AssertRequiresMoqProvider(() => genericLogger.VerifyLogger(LogLevel.Information, "processed order", times: 1), providerName);
            AssertRequiresMoqProvider(() => logger.VerifyLogger(LogLevel.Error, "processed order", exception, eventId: 7, times: 1), providerName);
            AssertRequiresMoqProvider(() => genericLogger.VerifyLogger(LogLevel.Error, "processed order", exception, eventId: 9, times: 1), providerName);
            AssertRequiresMoqProvider(() => logger.SetupLoggerCallback((_, _, _, _) => { }), providerName);
            AssertRequiresMoqProvider(() => genericLogger.SetupLoggerCallback((_, _, _, _) => { }), providerName);
        }

        [Fact]
        public void SetupLoggerCallbackShortcut_ShouldCaptureEntries_WithoutCallingAsMoq()
        {
            using var providerScope = MockingProviderRegistry.Push("moq");
            var mocker = new Mocker();

            var logger = mocker.GetOrCreateMock<ILogger<NullLogger>>();
            LogLevel? capturedLevel = null;
            string? capturedMessage = null;

            logger.SetupLoggerCallback((logLevel, _, message, _) =>
            {
                capturedLevel = logLevel;
                capturedMessage = message;
            });

            logger.Instance.LogInformation("callback message");

            capturedLevel.Should().Be(LogLevel.Information);
            capturedMessage.Should().Contain("callback message");
        }

        [Fact]
        public void SetupLoggerCallbackShortcut_ShouldCaptureEntries_ForNonGenericLogger()
        {
            using var providerScope = MockingProviderRegistry.Push("moq");
            var mocker = new Mocker();

            var logger = mocker.GetOrCreateMock<ILogger>();
            LogLevel? capturedLevel = null;
            string? capturedMessage = null;

            logger.SetupLoggerCallback((logLevel, _, message, _) =>
            {
                capturedLevel = logLevel;
                capturedMessage = message;
            });

            logger.Instance.LogWarning("callback warning");

            capturedLevel.Should().Be(LogLevel.Warning);
            capturedMessage.Should().Contain("callback warning");
        }

        public interface IProviderValueDependency
        {
            string GetValue();
        }

        public interface IProviderPropertyDependency
        {
            string Value { get; }
        }

        public class ProtectedShortcutDependency
        {
            public virtual string Run() => "ok";
        }

        private static void AssertRequiresMoqProvider(Action action, string providerName)
        {
            var exception = action.Should().Throw<NotSupportedException>().Which;
            exception.Message.Should().Contain("requires the 'moq' provider");
            exception.Message.Should().Contain($"active provider is '{providerName}'");
            exception.Message.Should().Contain("MockingProviderRegistry.Push(\"moq\")");
        }
    }
}

#pragma warning restore FMOQ0003
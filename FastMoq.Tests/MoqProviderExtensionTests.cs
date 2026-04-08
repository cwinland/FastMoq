using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq.Protected;

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

        public interface IProviderValueDependency
        {
            string GetValue();
        }
    }
}
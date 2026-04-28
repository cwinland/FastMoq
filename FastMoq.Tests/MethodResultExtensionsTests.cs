using System;
using System.Threading;
using System.Threading.Tasks;
using FastMoq.Extensions;
using FastMoq.Providers;

namespace FastMoq.Tests
{
    public class MethodResultExtensionsTests
    {
        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public void AddMethodResult_ShouldReturnConfiguredValue_AndPreserveTrackedVerification(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            _ = mocker.GetOrCreateMock<IMethodResultGateway>();
            var gateway = mocker.AddMethodResult<IMethodResultGateway, string?>(x => x.Fetch("alpha"), "configured");

            var configured = gateway.Fetch("alpha");
            var unmatched = gateway.Fetch("beta");

            configured.Should().Be("configured");
            unmatched.Should().NotBe("configured");
            mocker.Verify<IMethodResultGateway>(x => x.Fetch("alpha"), TimesSpec.Once);
            mocker.Verify<IMethodResultGateway>(x => x.Fetch("beta"), TimesSpec.Once);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public async Task AddMethodResultAsync_ShouldReturnConfiguredTaskResult_AndPreserveTrackedVerification(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            _ = mocker.GetOrCreateMock<IMethodResultGateway>();
            var gateway = mocker.AddMethodResultAsync<IMethodResultGateway, string?>(
                x => x.FetchAsync("alpha", CancellationToken.None),
                "configured");

            var configured = await gateway.FetchAsync("alpha", CancellationToken.None);

            configured.Should().Be("configured");
            mocker.Verify<IMethodResultGateway>(x => x.FetchAsync("alpha", CancellationToken.None), TimesSpec.Once);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public void AddMethodException_ShouldThrowConfiguredException_AndPreserveTrackedVerification(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            _ = mocker.GetOrCreateMock<IMethodResultGateway>();
            var expected = new InvalidOperationException("configured");
            var gateway = mocker.AddMethodException<IMethodResultGateway, string?>(x => x.Fetch("alpha"), expected);

            Action action = () => gateway.Fetch("alpha");

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("configured");
            mocker.Verify<IMethodResultGateway>(x => x.Fetch("alpha"), TimesSpec.Once);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public async Task AddMethodExceptionAsync_ShouldReturnFaultedTask_AndPreserveTrackedVerification(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            _ = mocker.GetOrCreateMock<IMethodResultGateway>();
            var expected = new InvalidOperationException("configured");
            var gateway = mocker.AddMethodExceptionAsync<IMethodResultGateway, string?>(
                x => x.FetchAsync("alpha", CancellationToken.None),
                expected);

            Func<Task> action = async () => _ = await gateway.FetchAsync("alpha", CancellationToken.None);

            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("configured");
            mocker.Verify<IMethodResultGateway>(x => x.FetchAsync("alpha", CancellationToken.None), TimesSpec.Once);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public async Task AddMethodCompletionAsync_ShouldReturnCompletedTask_AndPreserveTrackedVerification(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            _ = mocker.GetOrCreateMock<IMethodResultGateway>();
            var gateway = mocker.AddMethodCompletionAsync<IMethodResultGateway>(
                x => x.PublishAsync("alpha", CancellationToken.None));

            await gateway.PublishAsync("alpha", CancellationToken.None);

            mocker.Verify<IMethodResultGateway>(x => x.PublishAsync("alpha", CancellationToken.None), TimesSpec.Once);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public void AddMethodCallback_ShouldRunConfiguredCallback_ForExactVoidCall_AndPreserveTrackedVerification(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            _ = mocker.GetOrCreateMock<IMethodResultGateway>();
            var callbackCount = 0;
            var gateway = mocker.AddMethodCallback<IMethodResultGateway>(
                x => x.Publish("alpha"),
                () => callbackCount++);

            gateway.Publish("alpha");
            gateway.Publish("beta");

            callbackCount.Should().Be(1);
            mocker.Verify<IMethodResultGateway>(x => x.Publish("alpha"), TimesSpec.Once);
            mocker.Verify<IMethodResultGateway>(x => x.Publish("beta"), TimesSpec.Once);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public async Task AddMethodCallbackAsync_ShouldRunConfiguredCallback_ForExactTaskCall_AndPreserveTrackedVerification(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            _ = mocker.GetOrCreateMock<IMethodResultGateway>();
            var callbackCount = 0;
            var gateway = mocker.AddMethodCallbackAsync<IMethodResultGateway>(
                x => x.PublishAsync("alpha", CancellationToken.None),
                () => callbackCount++);

            await gateway.PublishAsync("alpha", CancellationToken.None);

            callbackCount.Should().Be(1);
            mocker.Verify<IMethodResultGateway>(x => x.PublishAsync("alpha", CancellationToken.None), TimesSpec.Once);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public void AddMethodException_ShouldThrowConfiguredException_ForVoidCall_AndPreserveTrackedVerification(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            _ = mocker.GetOrCreateMock<IMethodResultGateway>();
            var expected = new InvalidOperationException("configured-void");
            var gateway = mocker.AddMethodException<IMethodResultGateway>(x => x.Publish("alpha"), expected);

            Action action = () => gateway.Publish("alpha");

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("configured-void");
            mocker.Verify<IMethodResultGateway>(x => x.Publish("alpha"), TimesSpec.Once);
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("nsubstitute")]
        [InlineData("reflection")]
        public async Task AddMethodExceptionAsync_ShouldReturnFaultedTask_ForTaskCall_AndPreserveTrackedVerification(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            _ = mocker.GetOrCreateMock<IMethodResultGateway>();
            var expected = new InvalidOperationException("configured-task");
            var gateway = mocker.AddMethodExceptionAsync<IMethodResultGateway>(
                x => x.PublishAsync("alpha", CancellationToken.None),
                expected);

            Func<Task> action = async () => await gateway.PublishAsync("alpha", CancellationToken.None);

            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("configured-task");
            mocker.Verify<IMethodResultGateway>(x => x.PublishAsync("alpha", CancellationToken.None), TimesSpec.Once);
        }

        [Fact]
        public void AddMethodResult_ShouldOverrideConcreteRegistration_WithoutInvokingWrappedMethod()
        {
            var mocker = new Mocker();
            var fake = new MethodResultGatewayFake();
            mocker.AddType<IMethodResultGateway>(fake);

            var gateway = mocker.AddMethodResult<IMethodResultGateway, string?>(x => x.Fetch("alpha"), "configured");

            var configured = gateway.Fetch("alpha");
            var forwarded = gateway.Fetch("beta");

            configured.Should().Be("configured");
            forwarded.Should().Be("fake:beta");
            fake.FetchCalls.Should().Be(1);
        }

        [Fact]
        public void AddMethodException_ShouldOverrideConcreteRegistration_WithoutInvokingWrappedMethod()
        {
            var mocker = new Mocker();
            var fake = new MethodResultGatewayFake();
            mocker.AddType<IMethodResultGateway>(fake);

            var gateway = mocker.AddMethodException<IMethodResultGateway, string?>(
                x => x.Fetch("alpha"),
                new InvalidOperationException("configured"));

            Action action = () => gateway.Fetch("alpha");
            var forwarded = gateway.Fetch("beta");

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("configured");
            forwarded.Should().Be("fake:beta");
            fake.FetchCalls.Should().Be(1);
        }

        [Fact]
        public void AddMethodException_ShouldOverrideConcreteRegistration_ForVoidCall_WithoutInvokingWrappedMethod()
        {
            var mocker = new Mocker();
            var fake = new MethodResultGatewayFake();
            mocker.AddType<IMethodResultGateway>(fake);

            var gateway = mocker.AddMethodException<IMethodResultGateway>(
                x => x.Publish("alpha"),
                new InvalidOperationException("configured-void"));

            Action action = () => gateway.Publish("alpha");
            gateway.Publish("beta");

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("configured-void");
            fake.PublishCalls.Should().Be(1);
        }

        [Fact]
        public void AddMethodCallback_ShouldOverrideConcreteRegistration_ForVoidCall_WithoutInvokingWrappedMethod()
        {
            var mocker = new Mocker();
            var fake = new MethodResultGatewayFake();
            mocker.AddType<IMethodResultGateway>(fake);
            var callbackCount = 0;

            var gateway = mocker.AddMethodCallback<IMethodResultGateway>(
                x => x.Publish("alpha"),
                () => callbackCount++);

            gateway.Publish("alpha");
            gateway.Publish("beta");

            callbackCount.Should().Be(1);
            fake.PublishCalls.Should().Be(1);
        }

        [Fact]
        public void AddMethodResult_ShouldRejectNonInterfaceTypes()
        {
            var mocker = new Mocker();

            Action action = () => mocker.AddMethodResult<MethodResultConcreteTarget, string?>(x => x.Fetch("alpha"), "configured");

            action.Should().Throw<NotSupportedException>()
                .WithMessage("*interface types only*");
        }

        public interface IMethodResultGateway
        {
            string? Fetch(string key);

            Task<string?> FetchAsync(string key, CancellationToken cancellationToken);

            void Publish(string key);

            Task PublishAsync(string key, CancellationToken cancellationToken);
        }

        private sealed class MethodResultGatewayFake : IMethodResultGateway
        {
            public int FetchCalls { get; private set; }

            public int PublishCalls { get; private set; }

            public string? Fetch(string key)
            {
                FetchCalls++;
                return $"fake:{key}";
            }

            public Task<string?> FetchAsync(string key, CancellationToken cancellationToken)
            {
                return Task.FromResult<string?>($"fake:{key}");
            }

            public void Publish(string key)
            {
                PublishCalls++;
            }

            public Task PublishAsync(string key, CancellationToken cancellationToken)
            {
                PublishCalls++;
                return Task.CompletedTask;
            }
        }

        private sealed class MethodResultConcreteTarget
        {
            public string? Fetch(string key)
            {
                return key;
            }
        }
    }
}
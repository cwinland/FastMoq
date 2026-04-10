using FastMoq.Providers;
using FastMoq.Providers.NSubstituteProvider;
using NSubstitute;
using NSubstitute.Exceptions;
using System;

namespace FastMoq.Tests
{
    public class NSubstituteProviderExtensionTests
    {
        [Fact]
        public void AsNSubstitute_ShouldReturnUnderlyingSubstitute_WhenProviderIsNSubstitute()
        {
            using var providerScope = MockingProviderRegistry.Push("nsubstitute");
            var mocker = new Mocker();

            var dependency = mocker.GetOrCreateMock<IProviderValueDependency>();

            var substitute = dependency.AsNSubstitute();

            substitute.Should().BeSameAs(dependency.Instance);
            dependency.Instance.GetValue().Returns("configured");
            substitute.GetValue().Should().Be("configured");
        }

        [Theory]
        [InlineData("moq")]
        [InlineData("reflection")]
        public void AsNSubstitute_ShouldThrow_WhenProviderIsNotNSubstitute(string providerName)
        {
            using var providerScope = MockingProviderRegistry.Push(providerName);
            var mocker = new Mocker();

            var dependency = mocker.GetOrCreateMock<IProviderValueDependency>();

            Action action = () => dependency.AsNSubstitute();

            var exception = action.Should().Throw<NotSupportedException>().Which;
            exception.Message.Should().Contain("requires the 'nsubstitute' provider");
            exception.Message.Should().Contain($"active provider is '{providerName}'");
            exception.Message.Should().Contain("MockingProviderRegistry.Push(\"nsubstitute\")");
        }

        [Fact]
        public void ReceivedShortcut_ShouldVerifyCalls_WithoutCallingAsNSubstitute()
        {
            using var providerScope = MockingProviderRegistry.Push("nsubstitute");
            var mocker = new Mocker();

            var dependency = mocker.GetOrCreateMock<ProviderTests.IProviderDependency>();

            dependency.Instance.Run("alpha");

            dependency.Received(1).Run("alpha");
        }

        [Fact]
        public void DidNotReceiveShortcut_ShouldVerifyMissingCalls_WithoutCallingAsNSubstitute()
        {
            using var providerScope = MockingProviderRegistry.Push("nsubstitute");
            var mocker = new Mocker();

            var dependency = mocker.GetOrCreateMock<ProviderTests.IProviderDependency>();

            dependency.DidNotReceive().Run("beta");
        }

        [Fact]
        public void ClearReceivedCallsShortcut_ShouldResetCallHistory()
        {
            using var providerScope = MockingProviderRegistry.Push("nsubstitute");
            var mocker = new Mocker();

            var dependency = mocker.GetOrCreateMock<ProviderTests.IProviderDependency>();
            dependency.Instance.Run("alpha");

            dependency.ClearReceivedCalls();

            Action action = () => dependency.Received().Run("alpha");

            action.Should().Throw<ReceivedCallsException>();
        }

        public interface IProviderValueDependency
        {
            string GetValue();
        }
    }
}
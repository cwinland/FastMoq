using System;
using FastMoq.Providers;
using FluentAssertions;
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
    }
}
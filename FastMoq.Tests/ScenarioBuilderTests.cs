using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastMoq.Providers;

namespace FastMoq.Tests
{
    public class ScenarioBuilderTests
    {
        [Theory]
        [MemberData(nameof(ProviderTests.ProviderNames), MemberType = typeof(ProviderTests))]
        public void Verify_ShouldBeDeferredUntilExecute_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<ProviderTests.IProviderDependency>();

            var scenario = mocker.Scenario(new ProviderTests.ProviderConsumer(dependency.Instance))
                .When((_, service) => service.Dependency.Run("alpha"))
                .Verify<ProviderTests.IProviderDependency>(x => x.Run("alpha"), TimesSpec.Once)
                .VerifyNoOtherCalls<ProviderTests.IProviderDependency>();

            Action act = scenario.Execute;

            act.Should().NotThrow();
        }

        [Theory]
        [MemberData(nameof(ProviderTests.ProviderNames), MemberType = typeof(ProviderTests))]
        public async Task ExecuteAsync_ShouldSupportAsyncArrangeActAndAssert_ForSelectedProvider(string providerName)
        {
            using var providerScope = PushProvider(providerName);
            var mocker = new Mocker();
            var dependency = mocker.GetOrCreateMock<ProviderTests.IProviderDependency>();
            var component = new ProviderTests.ProviderConsumer(dependency.Instance);
            var executionOrder = new List<string>();

            await mocker.Scenario(component)
                .With(async _ =>
                {
                    executionOrder.Add("arrange");
                    await Task.CompletedTask;
                })
                .When(async service =>
                {
                    executionOrder.Add("act");
                    service.Dependency.Run("alpha");
                    await Task.CompletedTask;
                })
                .Then(async service =>
                {
                    executionOrder.Add("assert");
                    service.Dependency.Should().BeSameAs(dependency.Instance);
                    await Task.CompletedTask;
                })
                .Verify<ProviderTests.IProviderDependency>(x => x.Run("alpha"), TimesSpec.Once)
                .VerifyNoOtherCalls<ProviderTests.IProviderDependency>()
                .ExecuteAsync();

            executionOrder.Should().Equal("arrange", "act", "assert");
        }

        [Fact]
        public void MockerTestBase_ShouldExposeScenarioProperty()
        {
            var test = new ScenarioEnabledTestBase();

            var result = test.RunScenario();

            result.Should().Be(1);
        }

        [Fact]
        public void With_ShouldAcceptDirectInstance_ForScenarioTarget()
        {
            var mocker = new Mocker();
            var original = new ScenarioComponent();
            var replacement = new ScenarioComponent();
            var observed = 0;

            mocker.Scenario(original)
                .With(replacement)
                .When(component => component.Increment())
                .Then(component => observed = component.Count)
                .Execute();

            observed.Should().Be(1);
            original.Count.Should().Be(0);
            replacement.Count.Should().Be(1);
        }

        [Fact]
        public void ScenarioProperty_ShouldOperateOnBuiltInSut()
        {
            var test = new ScenarioEnabledTestBase();

            test.RunScenarioOnBuiltInComponent();

            test.ComponentCount.Should().Be(1);
        }

        [Fact]
        public void Scenario_ShouldSupportParameterlessArrangeActAndAssert()
        {
            var mocker = new Mocker();
            var component = new ScenarioComponent();
            var observed = 0;

            mocker.Scenario(component)
                .With(() => observed = -1)
                .When(() => component.Increment())
                .Then(() => observed = component.Count)
                .Execute();

            observed.Should().Be(1);
            component.Count.Should().Be(1);
        }

        [Fact]
        public void Scenario_ShouldSupportExceptionAssertionsAgainstBuiltInComponent()
        {
            var test = new ThrowingScenarioTestBase();

            var act = test.BuildFailingScenario;

            act.Should().NotThrow();
        }

        [Fact]
        public void Scenario_ShouldSupportTargetInstanceOutsideMockerTestBase()
        {
            var mocker = new Mocker();
            var component = new ThrowingScenarioComponent();

            var act = () => mocker.Scenario(component)
                .WhenThrows<InvalidOperationException>(service => service.Throw())
                .Execute();

            act.Should().NotThrow();
        }

        [Fact]
        public void ExecuteThrows_ShouldFail_WhenNoExceptionIsThrown()
        {
            var mocker = new Mocker();
            var component = new ScenarioComponent();

            var act = () => mocker.Scenario(component)
                .When(() => component.Increment())
                .ExecuteThrows<InvalidOperationException>();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*no exception was thrown*");
        }

        [Fact]
        public void WhenThrows_ShouldFail_WhenUnexpectedExceptionTypeIsThrown()
        {
            var mocker = new Mocker();
            var component = new ThrowingScenarioComponent();

            var act = () => mocker.Scenario(component)
                .WhenThrows<ArgumentException>(() => component.Throw())
                .Execute();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Expected act step to throw*")
                .WithInnerException<InvalidOperationException>();
        }

        [Fact]
        public void WhenThrows_ShouldAllowThenAssertionsAfterExpectedException()
        {
            var mocker = new Mocker();
            var component = new ThrowingScenarioComponent();
            var assertionRan = false;

            mocker.Scenario(component)
                .WhenThrows<InvalidOperationException>(service => service.Throw())
                .Then(() => assertionRan = true)
                .Execute();

            assertionRan.Should().BeTrue();
        }

        private static IDisposable PushProvider(string providerName)
        {
            if (!MockingProviderRegistry.TryGet(providerName, out var provider))
            {
                throw new InvalidOperationException($"Unable to find provider '{providerName}'.");
            }

            return MockingProviderRegistry.Push(provider);
        }

        private sealed class ScenarioEnabledTestBase : MockerTestBase<ScenarioComponent>
        {
            public int ComponentCount => Component.Count;

            public int RunScenario()
            {
                var count = 0;

                Scenario
                    .When(component => component.Increment())
                    .Then(component => count = component.Count)
                    .Execute();

                return count;
            }

            public void RunScenarioOnBuiltInComponent()
            {
                Scenario
                    .When(component => component.Increment())
                    .Execute();
            }
        }

        private sealed class ThrowingScenarioTestBase : MockerTestBase<ThrowingScenarioComponent>
        {
            public void BuildFailingScenario()
            {
                Scenario
                    .WhenThrows<InvalidOperationException>(() => Component.Throw())
                    .Execute();
            }
        }

        private sealed class ScenarioComponent
        {
            public int Count { get; private set; }

            public void Increment()
            {
                Count++;
            }
        }

        private sealed class ThrowingScenarioComponent
        {
            public void Throw()
            {
                throw new InvalidOperationException("Boom from component");
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FastMoq.Providers;

namespace FastMoq
{
    /// <summary>
    /// Provides a fluent arrange/act/assert pipeline for a tracked scenario target.
    /// </summary>
    /// <example>
    /// In <see cref="MockerTestBase{TComponent}"/>, prefer the parameterless overloads when <c>Component</c> is already available.
    /// <code>
    /// <![CDATA[
    /// Scenario
    ///     .With(() => Mocks.GetMock<IInvoiceRepository>()
    ///         .Setup(x => x.GetPastDueAsync(now, CancellationToken.None))
    ///         .ReturnsAsync(invoices))
    ///     .When(async () => reminderCount = await Component.SendRemindersAsync(now, CancellationToken.None))
    ///     .Then(() => reminderCount.Should().Be(2))
    ///     .Execute();
    /// ]]>
    /// </code>
    /// Outside <see cref="MockerTestBase{TComponent}"/>, use the target-instance overloads.
    /// <code>
    /// <![CDATA[
    /// await mocker.Scenario(service)
    ///     .When(component => component.RunAsync())
    ///     .Then(component => component.State.Should().Be(ServiceState.Completed))
    ///     .ExecuteAsync();
    /// ]]>
    /// </code>
    /// </example>
    public sealed class ScenarioBuilder<T> where T : class
    {
        private readonly Mocker mocker;
        private T currentInstance;
        private readonly List<Func<Task>> arrangeSteps = new();
        private readonly List<Func<Task>> actSteps = new();
        private readonly List<Func<Task>> assertSteps = new();
        private bool stopActExecution;

        internal ScenarioBuilder(Mocker mocker, T instance)
        {
            this.mocker = mocker;
            currentInstance = instance;
        }

        /// <summary>
        /// Gets the current scenario target instance.
        /// </summary>
        public T Instance => currentInstance;

        /// <summary>
        /// Replaces the scenario target instance.
        /// This enables fluent patterns such as <c>Scenario.With(Component)</c>.
        /// </summary>
        public ScenarioBuilder<T> With(T instance)
        {
            currentInstance = instance ?? throw new ArgumentNullException(nameof(instance));
            return this;
        }

        /// <summary>
        /// Adds an arrangement action executed before any When/Act operations.
        /// </summary>
        public ScenarioBuilder<T> With(Action arrange)
        {
            ArgumentNullException.ThrowIfNull(arrange);
            return With(() =>
            {
                arrange();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Adds an asynchronous arrangement action executed before any When/Act operations.
        /// </summary>
        public ScenarioBuilder<T> With(Func<Task> arrange)
        {
            ArgumentNullException.ThrowIfNull(arrange);
            arrangeSteps.Add(arrange);
            return this;
        }

        /// <summary>
        /// Adds an arrangement action executed before any When/Act operations.
        /// </summary>
        public ScenarioBuilder<T> With(Action<Mocker, T> arrange)
        {
            ArgumentNullException.ThrowIfNull(arrange);
            arrangeSteps.Add(() =>
            {
                arrange(mocker, currentInstance);
                return Task.CompletedTask;
            });
            return this;
        }

        /// <summary>
        /// Adds an asynchronous arrangement action executed before any When/Act operations.
        /// </summary>
        public ScenarioBuilder<T> With(Func<Mocker, T, Task> arrange)
        {
            ArgumentNullException.ThrowIfNull(arrange);
            arrangeSteps.Add(() => arrange(mocker, currentInstance));
            return this;
        }

        /// <summary>
        /// Adds an arrangement action using only the component instance.
        /// </summary>
        public ScenarioBuilder<T> With(Action<T> arrange)
        {
            ArgumentNullException.ThrowIfNull(arrange);
            return With((_, scenarioInstance) => arrange(scenarioInstance));
        }

        /// <summary>
        /// Adds an asynchronous arrangement action using only the component instance.
        /// </summary>
        public ScenarioBuilder<T> With(Func<T, Task> arrange)
        {
            ArgumentNullException.ThrowIfNull(arrange);
            return With((_, scenarioInstance) => arrange(scenarioInstance));
        }

        /// <summary>
        /// Adds an asynchronous act step.
        /// </summary>
        public ScenarioBuilder<T> When(Func<Task> act)
        {
            ArgumentNullException.ThrowIfNull(act);
            actSteps.Add(act);
            return this;
        }

        /// <summary>
        /// Adds a synchronous act step.
        /// </summary>
        public ScenarioBuilder<T> When(Action act)
        {
            ArgumentNullException.ThrowIfNull(act);
            return When(() =>
            {
                act();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Adds an asynchronous act step.
        /// </summary>
        public ScenarioBuilder<T> When(Func<Mocker, T, Task> act)
        {
            ArgumentNullException.ThrowIfNull(act);
            actSteps.Add(() => act(mocker, currentInstance));
            return this;
        }

        /// <summary>
        /// Adds a synchronous act step.
        /// </summary>
        public ScenarioBuilder<T> When(Action<Mocker, T> act)
        {
            ArgumentNullException.ThrowIfNull(act);
            actSteps.Add(() =>
            {
                act(mocker, currentInstance);
                return Task.CompletedTask;
            });
            return this;
        }

        /// <summary>
        /// Adds an asynchronous act step using only the component instance.
        /// </summary>
        public ScenarioBuilder<T> When(Func<T, Task> act)
        {
            ArgumentNullException.ThrowIfNull(act);
            return When((_, scenarioInstance) => act(scenarioInstance));
        }

        /// <summary>
        /// Adds a synchronous act step using only the component instance.
        /// </summary>
        public ScenarioBuilder<T> When(Action<T> act)
        {
            ArgumentNullException.ThrowIfNull(act);
            return When((_, scenarioInstance) => act(scenarioInstance));
        }

        /// <summary>
        /// Adds a synchronous act step that is expected to throw the specified exception type.
        /// </summary>
        /// <example>
        /// Use this when the act step is expected to fail but trailing assertions should still run.
        /// <code>
        /// <![CDATA[
        /// Scenario
        ///     .WhenThrows<InvalidOperationException>(() => Component.SendRemindersAsync(now, CancellationToken.None))
        ///     .Then(() => auditTrail.Count.Should().Be(1))
        ///     .Execute();
        /// ]]>
        /// </code>
        /// </example>
        public ScenarioBuilder<T> WhenThrows<TException>(Action act) where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(act);
            return WhenThrows<TException>(() =>
            {
                act();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Adds an asynchronous act step that is expected to throw the specified exception type.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// await mocker.Scenario(service)
        ///     .WhenThrows<InvalidOperationException>(component => component.RunAsync())
        ///     .Then(component => component.Attempts.Should().Be(1))
        ///     .ExecuteAsync();
        /// ]]>
        /// </code>
        /// </example>
        public ScenarioBuilder<T> WhenThrows<TException>(Func<Task> act) where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(act);
            actSteps.Add(async () =>
            {
                try
                {
                    await act().ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is TException)
                {
                    stopActExecution = true;
                    return;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Expected act step to throw {typeof(TException).FullName}, but {ex.GetType().FullName} was thrown instead.",
                        ex);
                }

                throw new InvalidOperationException(
                    $"Expected act step to throw {typeof(TException).FullName}, but no exception was thrown.");
            });
            return this;
        }

        /// <summary>
        /// Adds a synchronous act step using only the component instance that is expected to throw the specified exception type.
        /// </summary>
        public ScenarioBuilder<T> WhenThrows<TException>(Action<T> act) where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(act);
            return WhenThrows<TException>(() => act(currentInstance));
        }

        /// <summary>
        /// Adds an asynchronous act step using only the component instance that is expected to throw the specified exception type.
        /// </summary>
        public ScenarioBuilder<T> WhenThrows<TException>(Func<T, Task> act) where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(act);
            return WhenThrows<TException>(() => act(currentInstance));
        }

        /// <summary>
        /// Adds a synchronous act step using the mocker and component instance that is expected to throw the specified exception type.
        /// </summary>
        public ScenarioBuilder<T> WhenThrows<TException>(Action<Mocker, T> act) where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(act);
            return WhenThrows<TException>(() => act(mocker, currentInstance));
        }

        /// <summary>
        /// Adds an asynchronous act step using the mocker and component instance that is expected to throw the specified exception type.
        /// </summary>
        public ScenarioBuilder<T> WhenThrows<TException>(Func<Mocker, T, Task> act) where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(act);
            return WhenThrows<TException>(() => act(mocker, currentInstance));
        }

        /// <summary>
        /// Adds an assertion step.
        /// </summary>
        public ScenarioBuilder<T> Then(Action assertion)
        {
            ArgumentNullException.ThrowIfNull(assertion);
            return Then(() =>
            {
                assertion();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Adds an asynchronous assertion step.
        /// </summary>
        public ScenarioBuilder<T> Then(Func<Task> assertion)
        {
            ArgumentNullException.ThrowIfNull(assertion);
            assertSteps.Add(assertion);
            return this;
        }

        /// <summary>
        /// Adds an assertion step.
        /// </summary>
        public ScenarioBuilder<T> Then(Action<Mocker, T> assertion)
        {
            ArgumentNullException.ThrowIfNull(assertion);
            assertSteps.Add(() =>
            {
                assertion(mocker, currentInstance);
                return Task.CompletedTask;
            });
            return this;
        }

        /// <summary>
        /// Adds an asynchronous assertion step.
        /// </summary>
        public ScenarioBuilder<T> Then(Func<Mocker, T, Task> assertion)
        {
            ArgumentNullException.ThrowIfNull(assertion);
            assertSteps.Add(() => assertion(mocker, currentInstance));
            return this;
        }

        /// <summary>
        /// Adds a synchronous assertion step using only the component instance.
        /// </summary>
        public ScenarioBuilder<T> Then(Action<T> assertion)
        {
            ArgumentNullException.ThrowIfNull(assertion);
            return Then((_, scenarioInstance) => assertion(scenarioInstance));
        }

        /// <summary>
        /// Adds an asynchronous assertion step using only the component instance.
        /// </summary>
        public ScenarioBuilder<T> Then(Func<T, Task> assertion)
        {
            ArgumentNullException.ThrowIfNull(assertion);
            return Then((_, scenarioInstance) => assertion(scenarioInstance));
        }

        /// <summary>
        /// Queues a provider-first verification step to run during scenario execution.
        /// </summary>
        public ScenarioBuilder<T> Verify<TMock>(Expression<Action<TMock>> expression, TimesSpec? times = null) where TMock : class
        {
            ArgumentNullException.ThrowIfNull(expression);
            assertSteps.Add(() =>
            {
                mocker.Verify(expression, times);
                return Task.CompletedTask;
            });
            return this;
        }

        /// <summary>
        /// Executes the scenario (arrange -> act -> assert).
        /// </summary>
        public async Task ExecuteAsync()
        {
            stopActExecution = false;

            foreach (var arrange in arrangeSteps)
            {
                await arrange().ConfigureAwait(false);
            }

            foreach (var act in actSteps)
            {
                await act().ConfigureAwait(false);

                if (stopActExecution)
                {
                    break;
                }
            }

            foreach (var assert in assertSteps)
            {
                await assert().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Convenience execute for synchronous test bodies.
        /// </summary>
        public void Execute() => ExecuteAsync().GetAwaiter().GetResult();

        /// <summary>
        /// Executes the scenario and returns the expected exception.
        /// </summary>
        /// <example>
        /// Use this form when the exception object itself is the primary assertion target.
        /// <code>
        /// <![CDATA[
        /// var exception = Scenario
        ///     .When(() => Component.SendRemindersAsync(now, CancellationToken.None))
        ///     .ExecuteThrows<InvalidOperationException>();
        ///
        /// exception.Message.Should().Be("SMTP unavailable");
        /// ]]>
        /// </code>
        /// </example>
        public TException ExecuteThrows<TException>() where TException : Exception
        {
            return ExecuteThrowsAsync<TException>().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Executes the scenario and returns the expected exception.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// var exception = await mocker.Scenario(service)
        ///     .When(component => component.RunAsync())
        ///     .ExecuteThrowsAsync<InvalidOperationException>();
        /// ]]>
        /// </code>
        /// </example>
        public async Task<TException> ExecuteThrowsAsync<TException>() where TException : Exception
        {
            try
            {
                await ExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is TException expected)
            {
                return expected;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Expected scenario to throw {typeof(TException).FullName}, but {ex.GetType().FullName} was thrown instead.",
                    ex);
            }

            throw new InvalidOperationException(
                $"Expected scenario to throw {typeof(TException).FullName}, but no exception was thrown.");
        }

        /// <summary>
        /// Queues a provider-first VerifyNoOtherCalls step to run during scenario execution.
        /// </summary>
        public ScenarioBuilder<T> VerifyNoOtherCalls<TMock>() where TMock : class
        {
            assertSteps.Add(() =>
            {
                mocker.VerifyNoOtherCalls<TMock>();
                return Task.CompletedTask;
            });
            return this;
        }
    }
}

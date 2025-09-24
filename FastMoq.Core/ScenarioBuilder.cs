using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FastMoq.Providers;

namespace FastMoq
{
    /// <summary>
    /// Minimal Milestone 2 fluent scenario builder (initial scaffold).
    /// This will expand with .With / .When / .Then / .Verify phases.
    /// </summary>
    public sealed class ScenarioBuilder<T> where T : class
    {
        private readonly Mocker _mocker;
        private readonly T _instance;
        private readonly List<Action> _arrange = new();
        private readonly List<Func<Task>> _act = new();
        private readonly List<Action> _assert = new();

        internal ScenarioBuilder(Mocker mocker, T instance)
        {
            _mocker = mocker;
            _instance = instance;
        }

        /// <summary>
        /// Adds an arrangement action executed before any When/Act operations.
        /// </summary>
        public ScenarioBuilder<T> With(Action<Mocker, T> arrange)
        {
            ArgumentNullException.ThrowIfNull(arrange);
            _arrange.Add(() => arrange(_mocker, _instance));
            return this;
        }

        /// <summary>
        /// Adds an asynchronous act step.
        /// </summary>
        public ScenarioBuilder<T> When(Func<Mocker, T, Task> act)
        {
            ArgumentNullException.ThrowIfNull(act);
            _act.Add(() => act(_mocker, _instance));
            return this;
        }

        /// <summary>
        /// Adds a synchronous act step.
        /// </summary>
        public ScenarioBuilder<T> When(Action<Mocker, T> act)
        {
            ArgumentNullException.ThrowIfNull(act);
            _act.Add(() => { act(_mocker, _instance); return Task.CompletedTask; });
            return this;
        }

        /// <summary>
        /// Adds an assertion step.
        /// </summary>
        public ScenarioBuilder<T> Then(Action<Mocker, T> assertion)
        {
            ArgumentNullException.ThrowIfNull(assertion);
            _assert.Add(() => assertion(_mocker, _instance));
            return this;
        }

        /// <summary>
        /// Executes the scenario (arrange -> act -> assert).
        /// </summary>
        public async Task ExecuteAsync()
        {
            foreach (var a in _arrange) a();
            foreach (var act in _act) await act().ConfigureAwait(false);
            foreach (var assert in _assert) assert();
        }

        /// <summary>
        /// Convenience execute for synchronous test bodies.
        /// </summary>
        public void Execute() => ExecuteAsync().GetAwaiter().GetResult();

        /// <summary>
        /// Provider-first Verify passthrough enabling fluent chaining.
        /// </summary>
        public ScenarioBuilder<T> Verify<TMock>(Expression<Action<TMock>> expression, TimesSpec? times = null) where TMock : class
        {
            _mocker.Verify(expression, times);
            return this;
        }
    }
}

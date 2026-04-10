using FastMoq.Providers;
using System.Linq.Expressions;

namespace FastMoq
{
    /// <inheritdoc />
    public partial class Mocker
    {
        /// <summary>
        /// Builds a wildcard predicate expression using the active mocking provider.
        /// </summary>
        public static Expression<Func<T, bool>> BuildExpression<T>() => MockingProviderRegistry.Default.BuildExpression<T>();

        /// <summary>
        /// Creates a new fluent scenario builder for the specified component type using provider-first creation.
        /// </summary>
        public ScenarioBuilder<T> Scenario<T>(T? instance = null) where T : class
        {
            instance ??= GetObject<T>() ?? CreateInstance<T>() ?? throw new InvalidOperationException($"Unable to resolve instance for scenario of {typeof(T).Name}.");
            return new ScenarioBuilder<T>(this, instance);
        }
    }
}

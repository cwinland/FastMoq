using System.Linq.Expressions;

namespace FastMoq.Providers
{
    /// <summary>
    /// Provider-neutral argument matcher helpers for FastMoq expression-based setup and verification flows.
    /// </summary>
    public static class FastArg
    {
        /// <summary>
        /// Matches any argument of type <typeparamref name="T" />.
        /// </summary>
        /// <remarks>
        /// Use this only inside FastMoq-supported expression-based setup or verification APIs.
        /// </remarks>
        public static T Any<T>() => default!;

        /// <summary>
        /// Matches any expression argument of type <c>Expression&lt;Func&lt;T, bool&gt;&gt;</c>.
        /// </summary>
        /// <remarks>
        /// This is the provider-neutral replacement for older wildcard expression helpers such as <c>Mocker.BuildExpression&lt;T&gt;()</c> in new code.
        /// </remarks>
        public static Expression<Func<T, bool>> AnyExpression<T>() => _ => true;

        /// <summary>
        /// Matches an argument when the supplied predicate returns <see langword="true" />.
        /// </summary>
        /// <param name="predicate">The predicate used to evaluate the runtime argument.</param>
        public static T Is<T>(Expression<Func<T, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return default!;
        }

        /// <summary>
        /// Matches a <see langword="null" /> argument.
        /// </summary>
        public static T IsNull<T>() => default!;

        /// <summary>
        /// Matches a non-<see langword="null" /> argument.
        /// </summary>
        public static T IsNotNull<T>() => default!;
    }
}

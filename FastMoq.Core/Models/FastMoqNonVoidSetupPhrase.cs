using Moq.Language.Flow;
using System.Reflection;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class FastMoqNonVoidSetupPhrase.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>
    ///     Initializes a new instance of the <see cref="FastMoqNonVoidSetupPhrase{T}" /> class.
    /// </remarks>
    /// <param name="setupPhrase">The setup phrase.</param>
    public class FastMoqNonVoidSetupPhrase<T>(object setupPhrase)
    {
        /// <summary>
        ///     Returns the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="resultType">Type of the result.</param>
        public IReturnsResult<T>? Returns(Delegate value, Type resultType)
        {
            var genericFindType = typeof(Delegate);

            var returnsMethod = setupPhrase.GetType().GetMethod("Returns", BindingFlags.Public | BindingFlags.Instance, [genericFindType]);

            var returnObj = returnsMethod?.Invoke(setupPhrase, [value]);
            return returnObj as IReturnsResult<T>;
        }
    }
}

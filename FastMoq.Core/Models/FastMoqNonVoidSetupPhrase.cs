using Moq.Language.Flow;
using System.Reflection;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class FastMoqNonVoidSetupPhrase.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FastMoqNonVoidSetupPhrase<T>
    {
        #region Fields

        private readonly object setupPhrase;

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="FastMoqNonVoidSetupPhrase{T}" /> class.
        /// </summary>
        /// <param name="setupPhrase">The setup phrase.</param>
        public FastMoqNonVoidSetupPhrase(object setupPhrase) => this.setupPhrase = setupPhrase;

        /// <summary>
        ///     Returnses the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="resultType">Type of the result.</param>
        /// <returns>Returnses.</returns>
        public IReturnsResult<T> Returns(Delegate value, Type resultType)
        {
            var findType = typeof(Func<>);
            var genericFindType = findType.MakeGenericType(resultType);
            genericFindType = typeof(Delegate);

            var returnsMethod = setupPhrase.GetType().GetMethod("Returns", BindingFlags.Public | BindingFlags.Instance, new[] {genericFindType});

            var returnObj = returnsMethod.Invoke(setupPhrase, new object[] {value});
            return returnObj as IReturnsResult<T>;
        }
    }
}

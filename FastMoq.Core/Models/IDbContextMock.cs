using System.Reflection;

namespace FastMoq.Models
{
    /// <summary>
    ///     Interface IDbContextMock
    /// </summary>
    public interface IDbContextMock
    {
        /// <summary>
        ///     Setups the database context set methods.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        void SetupDbContextSetMethods(PropertyInfo propertyInfo);

        /// <summary>
        ///     Setups the database set properties.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="value">The value.</param>
        void SetupDbSetProperties(PropertyInfo propertyInfo, object value);

        /// <summary>
        ///     Setups the database set property get.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="value">The value.</param>
        void SetupDbSetPropertyGet(PropertyInfo propertyInfo, object value);

        /// <summary>
        ///     Setups the set method.
        /// </summary>
        /// <param name="setType">Type of the set.</param>
        /// <param name="propValueDelegate">The property value delegate.</param>
        /// <param name="types">The types.</param>
        /// <param name="parameters">The parameters.</param>
        void SetupSetMethod(Type setType, Delegate propValueDelegate, Type[]? types = null, object?[]? parameters = null);
    }
}

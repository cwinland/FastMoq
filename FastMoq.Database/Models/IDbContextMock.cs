using System.Reflection;

namespace FastMoq.Models
{
    /// <summary>
    /// Describes the DbContext-specific setup operations exposed by FastMoq's legacy DbContext mock wrappers.
    /// </summary>
    public interface IDbContextMock
    {
        /// <summary>
        /// Configures the <c>Set&lt;TEntity&gt;</c> style methods on the mocked DbContext for the supplied DbSet property.
        /// </summary>
        /// <param name="propertyInfo">The DbSet property being mapped back to the corresponding <c>Set&lt;TEntity&gt;</c> methods.</param>
        void SetupDbContextSetMethods(PropertyInfo propertyInfo);

        /// <summary>
        /// Configures DbSet-related properties on the mocked DbContext.
        /// </summary>
        /// <param name="propertyInfo">The DbSet property to configure.</param>
        /// <param name="value">The value the property should return.</param>
        void SetupDbSetProperties(PropertyInfo propertyInfo, object value);

        /// <summary>
        /// Configures the getter for a DbSet property on the mocked DbContext.
        /// </summary>
        /// <param name="propertyInfo">The DbSet property whose getter should be configured.</param>
        /// <param name="value">The value the getter should return.</param>
        void SetupDbSetPropertyGet(PropertyInfo propertyInfo, object value);

        /// <summary>
        /// Configures the generic <c>Set&lt;TEntity&gt;</c> method for the supplied entity type.
        /// </summary>
        /// <param name="setType">The entity type represented by the configured DbSet.</param>
        /// <param name="propValueDelegate">A delegate that returns the DbSet instance for the configured entity type.</param>
        /// <param name="types">Optional generic type arguments to apply when building the setup expression.</param>
        /// <param name="parameters">Optional method parameters to use while building the setup expression.</param>
        void SetupSetMethod(Type setType, Delegate propValueDelegate, Type[]? types = null, object?[]? parameters = null);
    }
}
using System.Reflection;

namespace FastMoq.Providers
{
    /// <summary>
    /// Optional provider extension for configuring one property getter on a tracked mock through FastMoq-owned helpers.
    /// </summary>
    public interface ITrackedMockPropertyConfigurator
    {
        /// <summary>
        /// Attempts to configure the getter for <paramref name="propertyInfo" /> on <paramref name="mock" /> so it returns <paramref name="value" />.
        /// </summary>
        /// <param name="mock">The tracked mock whose property getter should be configured.</param>
        /// <param name="propertyInfo">The property whose getter should be configured.</param>
        /// <param name="value">The value the getter should return.</param>
        /// <returns><see langword="true" /> when the provider configured the property successfully; otherwise, <see langword="false" />.</returns>
        bool TryConfigureMockProperty(IFastMock mock, PropertyInfo propertyInfo, object? value);
    }
}
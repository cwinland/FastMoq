using FastMoq.Providers;
using System.Reflection;

namespace FastMoq.AzureFunctions.Extensions
{
    internal static class MockPropertyConfigurationHelper
    {
        internal static void ConfigureNativeMockProperty(IFastMock fastMock, string propertyName, object? value, bool includeNonPublic = false)
        {
            _ = TryConfigureNativeMockProperty(fastMock, propertyName, value, includeNonPublic);
        }

        internal static bool TryConfigureNativeMockProperty(IFastMock fastMock, string propertyName, object? value, bool includeNonPublic = false)
        {
            ArgumentNullException.ThrowIfNull(fastMock);
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            if (includeNonPublic)
            {
                bindingFlags |= BindingFlags.NonPublic;
            }

            var propertyInfo = fastMock.MockedType.GetProperty(propertyName, bindingFlags);
            if (propertyInfo is null)
            {
                return false;
            }

            if (fastMock is not IProviderBoundFastMock providerBoundFastMock)
            {
                return false;
            }

            if (providerBoundFastMock.Provider is not ITrackedMockPropertyConfigurator propertyConfigurator)
            {
                return false;
            }

            return propertyConfigurator.TryConfigureMockProperty(fastMock, propertyInfo, value);
        }
    }
}
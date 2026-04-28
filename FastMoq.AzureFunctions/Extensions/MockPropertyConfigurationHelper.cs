using FastMoq.Extensions;
using FastMoq.Providers;
using System.Reflection;

namespace FastMoq.AzureFunctions.Extensions
{
    internal static class MockPropertyConfigurationHelper
    {
        private static readonly MethodInfo SetupMockPropertyByPropertyInfoMethod = typeof(MockerCreationExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(MockerCreationExtensions.SetupMockProperty) &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 3 &&
                method.GetParameters()[1].ParameterType == typeof(PropertyInfo));

        private static readonly MethodInfo? NSubstituteReturnsMethod = Type.GetType("NSubstitute.SubstituteExtensions, NSubstitute")
            ?.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(method =>
                method.Name == "Returns" &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 3 &&
                method.GetParameters()[0].ParameterType.IsGenericParameter &&
                method.GetParameters()[1].ParameterType.IsGenericParameter &&
                method.GetParameters()[2].ParameterType.IsArray &&
                method.GetParameters()[2].ParameterType.GetElementType()?.IsGenericParameter == true);

        internal static void ConfigureNativeMockProperty(IFastMock fastMock, string propertyName, object? value, bool includeNonPublic = false)
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
                return;
            }

            if (TryConfigureMoqNativeMockProperty(fastMock.NativeMock, propertyInfo, value))
            {
                return;
            }

            _ = TryConfigureNSubstitutePropertyGetter(fastMock, propertyInfo, value);
        }

        private static bool TryConfigureMoqNativeMockProperty(object? nativeMock, PropertyInfo propertyInfo, object? value)
        {
            if (nativeMock is null)
            {
                return false;
            }

            var nativeMockType = nativeMock.GetType();
            if (nativeMockType.Namespace != "Moq" || !nativeMockType.IsGenericType)
            {
                return false;
            }

            var mockedType = nativeMockType.GetGenericArguments()[0];
            var closedMethod = SetupMockPropertyByPropertyInfoMethod.MakeGenericMethod(mockedType);
            closedMethod.Invoke(null, [nativeMock, propertyInfo, value!]);
            return true;
        }

        private static bool TryConfigureNSubstitutePropertyGetter(IFastMock fastMock, PropertyInfo propertyInfo, object? value)
        {
            if (NSubstituteReturnsMethod is null || propertyInfo.GetMethod is null)
            {
                return false;
            }

            if (value is null)
            {
                if (propertyInfo.PropertyType.IsValueType && Nullable.GetUnderlyingType(propertyInfo.PropertyType) is null)
                {
                    return false;
                }
            }
            else if (!propertyInfo.PropertyType.IsAssignableFrom(value.GetType()))
            {
                return false;
            }

            try
            {
                var getterResult = propertyInfo.GetMethod.Invoke(fastMock.Instance, Array.Empty<object?>());
                var closedReturnsMethod = NSubstituteReturnsMethod.MakeGenericMethod(propertyInfo.PropertyType);
                var emptyReturnSequence = Array.CreateInstance(propertyInfo.PropertyType, 0);
                closedReturnsMethod.Invoke(null, [getterResult, value, emptyReturnSequence]);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
using System.Reflection;

namespace FastMoq.Models
{
    public interface IDbContextMock
    {
        void SetupDbContextSetMethods(PropertyInfo propertyInfo);
        void SetupDbSetProperties(PropertyInfo propertyInfo, object value);
        void SetupDbSetPropertyGet(PropertyInfo propertyInfo, object value);
        void SetupSetMethod(Type setType, Delegate propValueDelegate, Type[]? types = null, object?[]? parameters = null);
    }
}
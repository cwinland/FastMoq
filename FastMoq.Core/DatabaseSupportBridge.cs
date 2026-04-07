using System.Reflection;
using Moq;

namespace FastMoq
{
    internal static class DatabaseSupportBridge
    {
        private const string DbContextSupportTypeName = "FastMoq.DbContextMockerExtensions";
        private const string DatabaseAssemblyName = "FastMoq.Database";
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

        internal static bool IsEntityFrameworkDbContextType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            for (var current = type; current != null; current = current.BaseType)
            {
                if (string.Equals(current.FullName, "Microsoft.EntityFrameworkCore.DbContext", StringComparison.Ordinal) &&
                    string.Equals(current.Assembly.GetName().Name, "Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TryCreateManagedInstance(Mocker mocker, Type requestedType, out object? instance)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(requestedType);

            instance = null;
            if (!IsEntityFrameworkDbContextType(requestedType))
            {
                return false;
            }

            var supportType = GetSupportType();
            var method = supportType?.GetMethod("TryCreateManagedDbContextInstance", PublicStatic);
            if (method == null)
            {
                return false;
            }

            var args = new object?[] { mocker, requestedType, null };
            var created = method.Invoke(null, args) as bool? == true;
            instance = args[2];
            return created && instance != null;
        }

        internal static bool TryCreateLegacyDbContextMock(Type requestedType, MockBehavior behavior, IReadOnlyCollection<object?> constructorArgs, out Mock? mock)
        {
            ArgumentNullException.ThrowIfNull(requestedType);
            ArgumentNullException.ThrowIfNull(constructorArgs);

            mock = null;
            if (!IsEntityFrameworkDbContextType(requestedType))
            {
                return false;
            }

            var supportType = GetSupportType();
            var method = supportType?.GetMethod("CreateLegacyDbContextMock", PublicStatic);
            if (method == null)
            {
                return false;
            }

            mock = method.Invoke(null, [requestedType, behavior, constructorArgs.ToArray()]) as Mock;
            return mock != null;
        }

        private static Type? GetSupportType()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => string.Equals(candidate.GetName().Name, DatabaseAssemblyName, StringComparison.Ordinal));

            if (assembly == null)
            {
                try
                {
                    assembly = Assembly.Load(DatabaseAssemblyName);
                }
                catch
                {
                    return null;
                }
            }

            return assembly.GetType(DbContextSupportTypeName, throwOnError: false, ignoreCase: false);
        }
    }
}
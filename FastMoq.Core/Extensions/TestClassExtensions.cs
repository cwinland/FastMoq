using FastMoq.Models;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit.Abstractions;

namespace FastMoq.Extensions
{
    /// <summary>
    ///     Class TestClassExtensions.
    /// </summary>
    public static class TestClassExtensions
    {
        /// <summary>
        ///     Calls the generic method.
        /// </summary>
        /// <param name="typeParameter">The type parameter.</param>
        /// <param name="obj">The object.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Calls the generic method.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.MissingMethodException"></exception>
        public static object? CallGenericMethod(this Type typeParameter, object obj, [CallerMemberName] string? methodName = null,
            Type[]? parameterTypes = null, object[]? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(obj);

            parameterTypes ??= Array.Empty<Type>();
            parameters ??= Array.Empty<object>();

            var method = obj.GetType().GetMethods()
                             .FirstOrDefault(m => m.Name == methodName &&
                                                  m.IsGenericMethodDefinition &&
                                                  m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes)
                             ) ??
                         throw new MissingMethodException(obj.GetType().FullName, methodName);

            var genericMethod = method.MakeGenericMethod(typeParameter);
            return genericMethod.Invoke(obj, parameters);
        }

        /// <summary>
        ///     Ensures the null check thrown.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="constructorName">Name of the constructor.</param>
        /// <param name="output">The output.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void EnsureNullCheckThrown(this Action action, string parameterName, string? constructorName = "",
            Action<string>? output = null)
        {
            ArgumentNullException.ThrowIfNull(action);

            try
            {
                output?.Invoke($"Testing {constructorName}\n - {parameterName}");

                try
                {
                    action();
                }
                catch (ArgumentNullException ex)
                {
                    if (!ex.Message.Contains(parameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw;
                    }
                }

                output?.Invoke($"Passed {parameterName}");
            }
            catch
            {
                output?.Invoke($"Failed {parameterName}");
                throw;
            }
        }

        /// <summary>
        ///     Ensures the null check thrown.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="constructorName">Name of the constructor.</param>
        /// <param name="output">The output.</param>
        public static void EnsureNullCheckThrown(this Action action, string parameterName,
            string? constructorName, ITestOutputHelper? output) =>
            action.EnsureNullCheckThrown(parameterName, constructorName, s => output?.WriteLine(s));

        /// <summary>
        ///     Gets the default value.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see cref="Nullable{T}" />.</returns>
        public static object? GetDefaultValue(this Type type) => type switch
        {
            { FullName: "System.Uri" } => new UriBuilder { Scheme = "http", Host = "localhost" }.Uri,
            { FullName: "System.String" } => string.Empty,
            _ when typeof(IEnumerable).IsAssignableFrom(type) => Array.CreateInstance(type.GetElementType() ?? typeof(object), 0),
            { IsClass: true } => null,
            { IsInterface: true } => null,
            _ => Activator.CreateInstance(type),
        };

        /// <summary>
        ///     Gets the field.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <returns><see cref="Nullable{FieldInfo}" />.</returns>
        public static FieldInfo? GetField<TObject>(this TObject obj, string name) where TObject : class? =>
            obj?.GetType().GetRuntimeFields()
                .FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        ///     Gets the field information.
        /// </summary>
        /// <typeparam name="TType">The type of the t type.</typeparam>
        /// <param name="_">The object.</param>
        /// <param name="name">The name.</param>
        /// <returns>System.Nullable&lt;FieldInfo&gt;.</returns>
        public static FieldInfo GetFieldInfo<TType>(this object _, string name)
        {
            var fields = typeof(TType).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            return fields.First(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Gets the field value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TType">The type of the t type.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <returns>System.Nullable&lt;T&gt;.</returns>
        public static T? GetFieldValue<T, TType>(this object obj, string name) => obj.GetFieldValue<T>(obj.GetFieldInfo<TType>(name));

        /// <summary>
        ///     Gets the field value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="field">The field.</param>
        /// <returns>System.Nullable&lt;T&gt;.</returns>
        public static T? GetFieldValue<T>(this object? obj, FieldInfo field) => (T?) field.GetValue(obj);

        /// <summary>
        ///     Gets the field value.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns><see cref="Nullable{Object}" />.</returns>
        public static object? GetFieldValue<TObject>(this TObject obj, string name, TObject? defaultValue = null) where TObject : class?
            => obj.GetField(name)?.GetValue(obj) ?? defaultValue ?? default;

        /// <summary>
        ///     Gets the property value based on lambda.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TValue">The type of the t value.</typeparam>
        /// <param name="_">The object.</param>
        /// <param name="memberLambda">The member lambda.</param>
        /// <returns>System.Nullable&lt;TValue&gt;.</returns>
        public static MemberInfo GetMember<T, TValue>(this T _, Expression<Func<T, TValue>> memberLambda) =>
            memberLambda.GetMemberExpression().Member;

        /// <summary>
        ///     Gets the member expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method">The method.</param>
        /// <returns>MemberExpression.</returns>
        public static MemberExpression GetMemberExpression<T>(this Expression<T> method) => method.GetMemberExpressionInternal();

        /// <summary>
        ///     Gets the member expression.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>MemberExpression.</returns>
        public static MemberExpression GetMemberExpression(this Expression method) => method.GetMemberExpressionInternal();

        /// <summary>
        ///     Gets the name of the member.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TValue">The type of the t value.</typeparam>
        /// <param name="_">The .</param>
        /// <param name="memberLambda">The member lambda.</param>
        /// <returns>System.String.</returns>
        public static string GetMemberName<T, TValue>(this T _, Expression<Func<T, TValue>> memberLambda) =>
            memberLambda.GetMemberExpression().Member.Name;

        /// <summary>
        ///     Gets the name of the member.
        /// </summary>
        /// <param name="memberLambda">The member lambda.</param>
        /// <returns>System.String.</returns>
        public static string GetMemberName(this Expression memberLambda) => memberLambda.GetMemberExpressionInternal().Member.Name;

        /// <summary>
        ///     Gets the method.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <returns><see cref="Nullable{MethodInfo}" />.</returns>
        public static MethodInfo? GetMethod<TObject>(this TObject obj, string name) where TObject : class? =>
            obj?.GetType().GetRuntimeMethods()
                .FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        ///     Gets the method value.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>System.Nullable&lt;System.Object&gt;.</returns>
        public static object? GetMethodValue<TObject>(this TObject obj, string name, object? defaultValue = null, params object[] args)
            where TObject : class?
            => obj.GetMethod(name)?.Invoke(obj, args) ?? defaultValue;

        /// <summary>
        ///     Gets the property.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <returns><see cref="Nullable{PropertyInfo}" />.</returns>
        public static PropertyInfo? GetProperty<TObject>(this TObject obj, string name) =>
            obj?.GetType().GetRuntimeProperties()
                .FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        ///     Gets the property value.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns><see cref="Nullable{Object}" />.</returns>
        public static object? GetPropertyValue<TObject>(this TObject obj, string name, object? defaultValue = null)
            where TObject : class? =>
            obj.GetProperty(name)?.GetValue(obj) ?? defaultValue ?? default;

        /// <summary>
        ///     Gets the test data.
        /// </summary>
        /// <param name="testData">The test data.</param>
        /// <param name="i">The i.</param>
        /// <param name="p">The p.</param>
        /// <returns>object of the test data.</returns>
        public static object? GetTestData(this IReadOnlyList<object>? testData, int i, ParameterInfo p) =>
            testData != null && i < testData.Count ? testData[i] : p?.ParameterType.GetDefaultValue();

        /// <summary>
        ///     Gets the type from interface.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="tType">Type of the t.</param>
        /// <param name="typeList">The type list.</param>
        /// <returns>Type.</returns>
        /// <exception cref="System.Runtime.AmbiguousImplementationException"></exception>
        public static Type GetTypeFromInterface(this Mocker mocker, Type tType, List<Type>? typeList = null)
        {
            if (!tType.IsInterface)
            {
                return tType;
            }

            var mappedType = mocker.typeMap.Where(x => x.Key == tType).Select(x => x.Value).FirstOrDefault();

            if (mappedType != null)
            {
                return mappedType.InstanceType;
            }

            var types = typeList ?? tType.Assembly.GetTypes().ToList();
            return GetTypeFromInterfaceList(mocker, tType, types);
        }

        private static Type GetTypeFromInterfaceList(Mocker mocker, Type tType, List<Type> types)
        {
            // Get interfaces that contain T.
            var interfaces = types.Where(type => type.IsInterface && type.GetInterfaces().Contains(tType)).ToList();

            // Get Types that contain T, but are not interfaces.
            var possibleTypes = types.Where(type =>
                type.GetInterfaces().Contains(tType) &&
                interfaces.TrueForAll(iType => type != iType) &&
                !interfaces.Exists(iType => iType.IsAssignableFrom(type))
            ).ToList();

            return possibleTypes.Count switch
            {
                > 1 => possibleTypes.Count(x => x.IsPublic) > 1
                    ? throw mocker.GetAmbiguousImplementationException(tType, possibleTypes)
                    : possibleTypes.Find(x => x.IsPublic) ?? possibleTypes.FirstOrDefault() ?? tType,
                1 => possibleTypes[0],
                _ => tType,
            };
        }

        /// <summary>
        /// Throws the ambiguous implementation exception.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="message">The message.</param>
        /// <returns>System.Runtime.AmbiguousImplementationException.</returns>
        public static AmbiguousImplementationException GetAmbiguousImplementationException(this Mocker mocker, string message)
        {
            mocker.exceptionLog.Add(message);
            return new AmbiguousImplementationException(message);
        }

        /// <summary>
        /// Gets the ambiguous implementation exception.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="tType">Type of the t.</param>
        /// <param name="types">The types.</param>
        /// <returns>System.Runtime.AmbiguousImplementationException.</returns>
        public static AmbiguousImplementationException GetAmbiguousImplementationException(this Mocker mocker, Type tType, ICollection<Type>? types = null)
        {
            var builder = new StringBuilder($"Multiple components of type '{tType}' was found.");

            if (types?.Count > 1)
            {
                builder.AppendLine("\r\nTypes found:");

                builder.AppendJoin(", ", types.Select(x => x.FullName));
            }
            return mocker.GetAmbiguousImplementationException(builder.ToString());
        }

        /// <summary>
        /// Throws the ambiguous constructor implementation exception.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="tType">Type of the t.</param>
        /// <returns>System.Runtime.AmbiguousImplementationException.</returns>
        public static AmbiguousImplementationException GetAmbiguousConstructorImplementationException(this Mocker mocker, Type tType) =>
            mocker.GetAmbiguousImplementationException($"Multiple parameterized constructors exist of type '{tType}'. Cannot decide which to use.");

        /// <summary>
        ///     Sets the field value.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void SetFieldValue<TObject>(this TObject obj, string name, object? value) where TObject : class? =>
            obj.GetField(name)?.SetValue(obj, value);

        /// <summary>
        ///     Sets the property value.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void SetPropertyValue<TObject>(this TObject obj, string name, object? value) where TObject : class? =>
            obj.GetProperty(name)?.SetValue(obj, value);

        /// <summary>
        ///     ForEach for <see cref="IEnumerable{T}" />.
        /// </summary>
        /// <typeparam name="T">Type of item.</typeparam>
        /// <param name="iEnumerable">The <see cref="IEnumerable{T}" />.</param>
        /// <param name="action">The action.</param>
        internal static void ForEach<T>(this IEnumerable<T> iEnumerable, Action<T> action) => iEnumerable.ToList().ForEach(action);

        /// <summary>
        ///     Gets the argument data.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="method">The method.</param>
        /// <param name="data">The data.</param>
        /// <returns>object?[] of the argument data.</returns>
        internal static object?[] GetArgData(this Mocker mocker, MethodBase? method, Dictionary<Type, object?>? data)
        {
            var args = new List<object?>();

            method?.GetParameters().ToList().ForEach(p => args.Add(data?.Any(x => x.Key == p.ParameterType) ?? false
                    ? data.First(x => x.Key == p.ParameterType).Value
                    : mocker.GetParameter(p.ParameterType)
                )
            );

            return args.ToArray();
        }

        /// <summary>
        ///     Gets the injection fields.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="attributeType">Override attribute type.</param>
        /// <returns><see cref="IEnumerable{T}" />.</returns>
        internal static IEnumerable<FieldInfo> GetInjectionFields(this Type type, Type? attributeType = null) =>
            type
                .GetRuntimeFields()
                .Where(x => x.CustomAttributes.Any(y =>
                        y.AttributeType == attributeType ||
                        y.AttributeType.Name.Equals("InjectAttribute", StringComparison.OrdinalIgnoreCase)
                    )
                );

        /// <summary>
        ///     Gets the injection properties.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="attributeType">Override attribute type.</param>
        /// <returns><see cref="IEnumerable{T}" />.</returns>
        internal static IEnumerable<PropertyInfo> GetInjectionProperties(this Type type, Type? attributeType = null) =>
            type
                .GetRuntimeProperties()
                .Where(x => x.CustomAttributes.Any(y =>
                        y.AttributeType == attributeType ||
                        y.AttributeType.Name.Equals("InjectAttribute", StringComparison.OrdinalIgnoreCase)
                    )
                );

        /// <summary>
        ///     Gets the member expression internal.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>MemberExpression of the member expression internal.</returns>
        /// <exception cref="ArgumentNullException">nameof(method)</exception>
        internal static MemberExpression GetMemberExpressionInternal(this Expression method)
        {
            if (method is not LambdaExpression lambda)
            {
                throw new ArgumentNullException(nameof(method));
            }

            var memberExpr = lambda.Body.NodeType switch
            {
                ExpressionType.Convert => ((UnaryExpression) lambda.Body).Operand as MemberExpression,
                ExpressionType.MemberAccess => lambda.Body as MemberExpression,
                _ => null,
            };

            return memberExpr ?? throw new ArgumentNullException(nameof(method));
        }

        /// <summary>
        ///     Gets the tested constructors.
        /// </summary>
        /// <param name="mocker">Mocker object</param>
        /// <param name="type">The type to try to create.</param>
        /// <param name="constructors">The constructors to test with the specified type.</param>
        /// <returns>List&lt;FastMoq.Models.ConstructorModel&gt;.</returns>
        internal static List<ConstructorModel> GetTestedConstructors(this Mocker mocker, Type type, List<ConstructorModel> constructors)
        {
            constructors ??= new();
            var validConstructors = new List<ConstructorModel>();

            if (constructors.Count <= 1)
            {
                return constructors;
            }

            var targetError = new List<ConstructorModel>();

            foreach (var constructor in constructors)
            {
                try
                {
                    // Test Constructor.
                    var mock = mocker.CreateMockInternal(type, constructor.ParameterList);
                    _ = mock.Object;
                    validConstructors.Add(constructor);
                }
                catch (TargetInvocationException ex)
                {
                    // Track invocation issues to bubble up if a good constructor is not found.
                    mocker.exceptionLog.Add(ex.Message);
                    targetError.Add(constructor);
                }
                catch (Exception ex)
                {
                    mocker.exceptionLog.Add(ex.Message);
                }
            }

            return validConstructors.Count > 0 ? validConstructors : targetError;
        }

        /// <summary>
        ///     Determines whether [is nullable type] [the specified type].
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if [is nullable type] [the specified type]; otherwise, <c>false</c>.</returns>
        internal static bool IsNullableType(this Type type) =>
            type.IsClass || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));

        /// <summary>
        ///     Returns true if the argument list == 0 or the types match the constructor exactly.
        /// </summary>
        /// <param name="type">Type which the constructor is from.</param>
        /// <param name="info">Parameter information.</param>
        /// <param name="instanceParameterValues">Optional arguments.</param>
        /// <returns><c>true</c> if [is valid constructor] [the specified information]; otherwise, <c>false</c>.</returns>
        internal static bool IsValidConstructor(this Type type, MethodBase info, params object?[] instanceParameterValues)
        {
            var paramList = info.GetParameters().ToList();

            if (instanceParameterValues.Length == 0)
            {
                return paramList.All(x => x.ParameterType != type);
            }

            if (instanceParameterValues.Length > paramList.Count)
            {
                return false;
            }

            var isValid = true;

            for (var i = 0; i < instanceParameterValues.Length; i++)
            {
                var paramType = paramList[i].ParameterType;
                var instanceType = instanceParameterValues[i]?.GetType();

                isValid &= (instanceType == null && (paramType.IsNullableType() || paramType.IsInterface)) ||
                           (instanceType != null && paramType.IsAssignableFrom(instanceType));
            }

            return isValid;
        }

        /// <summary>
        ///     Returns true if the argument list == 0 or the types match the constructor exactly.
        /// </summary>
        /// <param name="info">Method.</param>
        /// <param name="instanceParameterValues">Optional arguments.</param>
        /// <returns><c>true</c> if [is valid constructor] [the specified information]; otherwise, <c>false</c>.</returns>
        internal static bool IsValidConstructorByType(this MethodBase info, params Type?[] instanceParameterValues)
        {
            if (instanceParameterValues.Length == 0)
            {
                return true;
            }

            var paramList = info.GetParameters().ToList();

            if (instanceParameterValues.Length != paramList.Count)
            {
                return false;
            }

            var isValid = true;

            for (var i = 0; i < paramList.Count; i++)
            {
                var paramType = paramList[i].ParameterType;
                var instanceType = instanceParameterValues[i];

                isValid &= paramType.IsAssignableFrom(instanceType);
            }

            return isValid;
        }

        /// <summary>
        ///     Throws the already exists.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <exception cref="System.ArgumentException"></exception>
        internal static void ThrowAlreadyExists(this Type type) => throw new ArgumentException($"{type} already exists.");

        internal static void ThrowIfCastleMethodAccessException(this Exception ex)
        {
            if (IsCastleMethodAccessException(ex))
            {
                ThrowInternalConstructorException(ex);
            }

            if (IsCastleMethodAccessException(ex.GetBaseException()))
            {
                ThrowInternalConstructorException(ex.GetBaseException());
            }

            bool IsCastleMethodAccessException(Exception innerException) =>
                innerException.Message.Contains("Castle.DynamicProxy.IInterceptor[]", StringComparison.Ordinal);

            void ThrowInternalConstructorException(Exception innerException) => throw new MethodAccessException(
                "The test cannot see the internal constructor. Add [assembly: InternalsVisibleTo(\"DynamicProxyGenAssembly2\")] to the AssemblyInfo or project file.",
                innerException
            );
        }
    }
}

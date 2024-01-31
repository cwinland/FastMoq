using Moq;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        public static object? CallGenericMethod(this Type typeParameter, object obj, [CallerMemberName] string? methodName = null,
            Type[]? parameterTypes = null, object[]? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(obj);

            parameterTypes ??= [];
            parameters ??= [];

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
        public static object GetTestData(this IReadOnlyList<object>? testData, int i, ParameterInfo p) =>
            testData != null && i < testData.Count ? testData[i] : p.ParameterType.GetDefaultValue();

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
        internal static bool IsValidConstructor(this Type type, ConstructorInfo info, params object?[] instanceParameterValues)
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
        /// <param name="info">Parameter information.</param>
        /// <param name="instanceParameterValues">Optional arguments.</param>
        /// <returns><c>true</c> if [is valid constructor] [the specified information]; otherwise, <c>false</c>.</returns>
        internal static bool IsValidConstructorByType(this ConstructorInfo info, params Type?[] instanceParameterValues)
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
    }
}

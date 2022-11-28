using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq
{
    public static class TestClassExtensions
    {
        /// <summary>
        ///     ForEach for <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of item.</typeparam>
        /// <param name="iEnumerable">The <see cref="IEnumerable{T}"/>.</param>
        /// <param name="action">The action.</param>
        internal static void ForEach<T>(this IEnumerable<T> iEnumerable, Action<T> action) => iEnumerable.ToList().ForEach(action);

        /// <summary>
        ///     Gets the field.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj"></param>
        /// <param name="name">The name.</param>
        /// <returns><see cref="Nullable{FieldInfo}" />.</returns>
        public static FieldInfo? GetField<TObject>(this TObject obj, string name) where TObject : class? =>
            obj?.GetType().GetRuntimeFields()
                .FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        ///     Gets the field value.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns><see cref="Nullable{Object}" />.</returns>
        public static object? GetFieldValue<TObject>(this TObject obj, string name, TObject? defaultValue = null)
            where TObject : class? => obj.GetField(name)?.GetValue(obj) ?? defaultValue ?? default;

        /// <summary>
        ///     Gets the property value based on lambda.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TValue">The type of the t value.</typeparam>
        /// <param name="obj"></param>
        /// <param name="memberLambda">The member lambda.</param>
        /// <returns>System.Nullable&lt;TValue&gt;.</returns>
        public static MemberInfo GetMember<T, TValue>(this T obj, Expression<Func<T, TValue>> memberLambda) =>
            GetMemberInfo(memberLambda).Member;

        /// <summary>
        ///     Get Member Info from expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TValue">The type of the t value.</typeparam>
        /// <param name="method">The method.</param>
        /// <returns>MemberExpression.</returns>
        /// <exception cref="System.ArgumentNullException">method</exception>
        public static MemberExpression GetMemberInfo<T, TValue>(this Expression<Func<T, TValue>> method)
        {
            if (method is not LambdaExpression lambda)
            {
                throw new ArgumentNullException(nameof(method));
            }

            var memberExpr = lambda.Body.NodeType switch
            {
                ExpressionType.Convert => ((UnaryExpression) lambda.Body).Operand as MemberExpression,
                ExpressionType.MemberAccess => lambda.Body as MemberExpression,
                _ => null
            };

            return memberExpr ?? throw new ArgumentNullException(nameof(method));
        }

        /// <summary>
        ///     Gets the method.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj"></param>
        /// <param name="name">The name.</param>
        /// <returns><see cref="Nullable{MethodInfo}" />.</returns>
        public static MethodInfo? GetMethod<TObject>(this TObject obj, string name) where TObject : class? =>
            obj?.GetType().GetRuntimeMethods()
                .FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        public static object? GetMethodValue<TObject>(this TObject obj, string name, object? defaultValue = null,
            params object[] args) where TObject : class? =>
            obj.GetMethod(name)?.Invoke(obj, args);

        /// <summary>
        ///     Gets the property.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj"></param>
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
    }
}
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Extensions
{
    /// <summary>
    ///     Class TestClassExtensions.
    /// </summary>
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
        public static T? GetFieldValue<T>(this object? obj, FieldInfo field) => (T?)field.GetValue(obj);

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
        public static MemberInfo GetMember<T, TValue>(this T _, Expression<Func<T, TValue>> memberLambda) => memberLambda.GetMemberExpression().Member;

        /// <summary>
        ///     Gets the name of the member.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TValue">The type of the t value.</typeparam>
        /// <param name="_">The .</param>
        /// <param name="memberLambda">The member lambda.</param>
        /// <returns>System.String.</returns>
        public static string GetMemberName<T, TValue>(this T _, Expression<Func<T, TValue>> memberLambda) => memberLambda.GetMemberExpression().Member.Name;

        /// <summary>
        ///     Gets the name of the member.
        /// </summary>
        /// <param name="memberLambda">The member lambda.</param>
        /// <returns>System.String.</returns>
        public static string GetMemberName(this Expression memberLambda) => memberLambda.GetMemberExpressionInternal().Member.Name;

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

        private static MemberExpression GetMemberExpressionInternal(this Expression method)
        {
            if (method is not LambdaExpression lambda)
            {
                throw new ArgumentNullException(nameof(method));
            }

            var memberExpr = lambda.Body.NodeType switch
            {
                ExpressionType.Convert => ((UnaryExpression)lambda.Body).Operand as MemberExpression,
                ExpressionType.MemberAccess => lambda.Body as MemberExpression,
                _ => null
            };

            return memberExpr ?? throw new ArgumentNullException(nameof(method));
        }

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
        public static object? GetMethodValue<TObject>(this TObject obj, string name, object? defaultValue = null, params object[] args) where TObject : class?
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
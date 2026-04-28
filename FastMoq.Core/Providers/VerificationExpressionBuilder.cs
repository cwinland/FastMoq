using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Providers
{
    internal static class VerificationExpressionBuilder
    {
        private static readonly MethodInfo FastArgAnyMethodDefinition = typeof(FastArg)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(FastArg.Any) &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 0);

        internal static Expression<Action<T>> BuildAnyArgsExpression<T>(string methodName, params Type[] parameterTypes) where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            var serviceType = typeof(T);
            var methods = serviceType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.Name == methodName)
                .ToArray();

            if (methods.Length == 0)
            {
                throw new InvalidOperationException($"Type '{serviceType.FullName}' does not contain an instance method named '{methodName}'.");
            }

            var selectedMethod = SelectMethod(serviceType, methodName, methods, parameterTypes);
            return BuildAnyArgsExpression<T>(selectedMethod);
        }

        internal static Expression<Action<T>> BuildAnyArgsExpression<T>(MethodInfo method) where T : class
        {
            ArgumentNullException.ThrowIfNull(method);

            var serviceType = typeof(T);
            if (method.DeclaringType is null || !serviceType.IsAssignableFrom(method.DeclaringType))
            {
                throw new InvalidOperationException($"Method '{method.DeclaringType?.FullName}.{method.Name}' does not belong to '{serviceType.FullName}'.");
            }

            method = ResolveMethod(serviceType, method);

            var parameter = Expression.Parameter(serviceType, "mock");
            var arguments = method
                .GetParameters()
                .Select(parameterInfo => CreateAnyArgumentExpression(parameterInfo.ParameterType))
                .ToArray();

            var call = Expression.Call(parameter, method, arguments);
            Expression body = method.ReturnType == typeof(void)
                ? call
                : Expression.Block(call, Expression.Empty());

            return Expression.Lambda<Action<T>>(body, parameter);
        }

        internal static MethodInfo GetSelectedMethod<T, TDelegate>(T instance, Func<T, TDelegate> methodSelector)
            where T : class
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(methodSelector);

            var selectedDelegate = methodSelector(instance)
                ?? throw new InvalidOperationException("The any-args method selector returned null. Return a method group such as x => x.Publish.");

            return selectedDelegate.Method;
        }

        private static MethodInfo ResolveMethod(Type serviceType, MethodInfo selectedMethod)
        {
            if (selectedMethod.DeclaringType == serviceType)
            {
                return selectedMethod;
            }

            var parameterTypes = selectedMethod.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
            var resolvedMethod = serviceType.GetMethod(
                selectedMethod.Name,
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: parameterTypes,
                modifiers: null);

            if (resolvedMethod is not null && resolvedMethod.ReturnType == selectedMethod.ReturnType)
            {
                return resolvedMethod;
            }

            throw new InvalidOperationException(
                $"Unable to map selected method '{selectedMethod.DeclaringType?.FullName}.{selectedMethod.Name}' back to '{serviceType.FullName}'. Use the method-name overload with explicit parameter types for this member.");
        }

        private static MethodInfo SelectMethod(Type serviceType, string methodName, IReadOnlyList<MethodInfo> methods, IReadOnlyList<Type> parameterTypes)
        {
            if (parameterTypes.Count == 0)
            {
                if (methods.Count == 1)
                {
                    return methods[0];
                }

                throw new InvalidOperationException(
                    $"Method '{serviceType.FullName}.{methodName}' is overloaded. Pass parameter types to the any-args verification helper to select the intended overload. Available overloads: {string.Join(", ", methods.Select(DescribeMethod))}.");
            }

            var matchingMethod = methods.SingleOrDefault(method => ParametersMatch(method, parameterTypes));
            if (matchingMethod is null)
            {
                throw new InvalidOperationException(
                    $"No overload of '{serviceType.FullName}.{methodName}' matches parameter types ({string.Join(", ", parameterTypes.Select(DescribeType))}). Available overloads: {string.Join(", ", methods.Select(DescribeMethod))}.");
            }

            return matchingMethod;
        }

        private static bool ParametersMatch(MethodInfo method, IReadOnlyList<Type> parameterTypes)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != parameterTypes.Count)
            {
                return false;
            }

            for (var index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].ParameterType != parameterTypes[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static Expression CreateAnyArgumentExpression(Type parameterType)
        {
            if (parameterType.IsByRef)
            {
                throw new NotSupportedException($"Any-args verification does not support by-ref parameters. Parameter type: '{parameterType}'.");
            }

            return Expression.Call(FastArgAnyMethodDefinition.MakeGenericMethod(parameterType));
        }

        private static string DescribeMethod(MethodInfo method)
        {
            var parameters = string.Join(", ", method.GetParameters().Select(parameter => DescribeType(parameter.ParameterType)));
            return $"{method.Name}({parameters})";
        }

        private static string DescribeType(Type type) => type.FullName ?? type.Name;
    }
}
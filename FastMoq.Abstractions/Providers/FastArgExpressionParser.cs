using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Providers
{
    /// <summary>
    /// Identifies the kind of FastMoq argument matcher parsed from an expression.
    /// </summary>
    public enum FastArgumentMatcherKind
    {
        /// <summary>
        /// Matches a specific exact value.
        /// </summary>
        Exact,

        /// <summary>
        /// Matches any value.
        /// </summary>
        Any,

        /// <summary>
        /// Matches values accepted by a predicate.
        /// </summary>
        Predicate,

        /// <summary>
        /// Matches only <see langword="null" /> values.
        /// </summary>
        Null,

        /// <summary>
        /// Matches only non-<see langword="null" /> values.
        /// </summary>
        NotNull,
    }

    /// <summary>
    /// Describes a single parsed FastMoq argument matcher.
    /// </summary>
    public sealed class FastArgumentMatcher
    {
        private readonly Func<object?, bool> _matches;

        private FastArgumentMatcher(
            FastArgumentMatcherKind kind,
            Type argumentType,
            object? expectedValue,
            LambdaExpression? predicateExpression,
            Func<object?, bool> matches)
        {
            Kind = kind;
            ArgumentType = argumentType;
            ExpectedValue = expectedValue;
            PredicateExpression = predicateExpression;
            _matches = matches;
        }

        /// <summary>
        /// Gets the matcher kind.
        /// </summary>
        public FastArgumentMatcherKind Kind { get; }

        /// <summary>
        /// Gets the runtime argument type that this matcher targets.
        /// </summary>
        public Type ArgumentType { get; }

        /// <summary>
        /// Gets the exact value expected by the matcher when <see cref="Kind" /> is <see cref="FastArgumentMatcherKind.Exact" />.
        /// </summary>
        public object? ExpectedValue { get; }

        /// <summary>
        /// Gets the predicate expression used by the matcher when <see cref="Kind" /> is <see cref="FastArgumentMatcherKind.Predicate" />.
        /// </summary>
        public LambdaExpression? PredicateExpression { get; }

        /// <summary>
        /// Evaluates the matcher against the supplied runtime value.
        /// </summary>
        /// <param name="actualValue">The runtime value to test.</param>
        /// <returns><see langword="true" /> when the value satisfies the matcher; otherwise <see langword="false" />.</returns>
        public bool Matches(object? actualValue) => _matches(actualValue);

        internal static FastArgumentMatcher Exact(Type argumentType, object? expectedValue)
        {
            ArgumentNullException.ThrowIfNull(argumentType);
            return new(FastArgumentMatcherKind.Exact, argumentType, expectedValue, null, actualValue => Equals(expectedValue, actualValue));
        }

        internal static FastArgumentMatcher Any(Type argumentType)
        {
            ArgumentNullException.ThrowIfNull(argumentType);
            return new(FastArgumentMatcherKind.Any, argumentType, null, null, _ => true);
        }

        internal static FastArgumentMatcher Predicate(Type argumentType, LambdaExpression predicateExpression)
        {
            ArgumentNullException.ThrowIfNull(argumentType);
            ArgumentNullException.ThrowIfNull(predicateExpression);

            var compiled = predicateExpression.Compile();
            return new(
                FastArgumentMatcherKind.Predicate,
                argumentType,
                null,
                predicateExpression,
                actualValue =>
                {
                    try
                    {
                        return compiled.DynamicInvoke(actualValue) is bool matched && matched;
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        internal static FastArgumentMatcher Null(Type argumentType)
        {
            ArgumentNullException.ThrowIfNull(argumentType);
            return new(FastArgumentMatcherKind.Null, argumentType, null, null, actualValue => actualValue is null);
        }

        internal static FastArgumentMatcher NotNull(Type argumentType)
        {
            ArgumentNullException.ThrowIfNull(argumentType);
            return new(FastArgumentMatcherKind.NotNull, argumentType, null, null, actualValue => actualValue is not null);
        }
    }

    /// <summary>
    /// Describes a parsed method invocation with FastMoq argument matchers.
    /// </summary>
    public sealed class FastInvocationMatcher
    {
        internal FastInvocationMatcher(MethodInfo method, IReadOnlyList<FastArgumentMatcher> arguments)
        {
            ArgumentNullException.ThrowIfNull(method);
            ArgumentNullException.ThrowIfNull(arguments);

            Method = method;
            Arguments = arguments;
        }

        /// <summary>
        /// Gets the target method represented by the parsed invocation.
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// Gets the parsed argument matchers in parameter order.
        /// </summary>
        public IReadOnlyList<FastArgumentMatcher> Arguments { get; }

        /// <summary>
        /// Determines whether the supplied method and argument values satisfy this invocation matcher.
        /// </summary>
        /// <param name="method">The runtime method to compare.</param>
        /// <param name="actualArguments">The runtime argument values to compare.</param>
        /// <returns><see langword="true" /> when the runtime invocation matches; otherwise <see langword="false" />.</returns>
        public bool Matches(MethodInfo method, IReadOnlyList<object?> actualArguments)
        {
            ArgumentNullException.ThrowIfNull(method);
            ArgumentNullException.ThrowIfNull(actualArguments);

            if (Method != method || actualArguments.Count != Arguments.Count)
            {
                return false;
            }

            for (var index = 0; index < Arguments.Count; index++)
            {
                if (!Arguments[index].Matches(actualArguments[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Parses expression trees that use <see cref="FastArg" /> markers into provider-neutral matcher metadata.
    /// </summary>
    public static class FastArgExpressionParser
    {
        private static readonly MethodInfo AnyMethodDefinition = GetRequiredMethodDefinition(nameof(FastArg.Any), parameterCount: 0);
        private static readonly MethodInfo AnyExpressionMethodDefinition = GetRequiredMethodDefinition(nameof(FastArg.AnyExpression), parameterCount: 0);
        private static readonly MethodInfo IsMethodDefinition = GetRequiredMethodDefinition(nameof(FastArg.Is), parameterCount: 1);
        private static readonly MethodInfo IsNullMethodDefinition = GetRequiredMethodDefinition(nameof(FastArg.IsNull), parameterCount: 0);
        private static readonly MethodInfo IsNotNullMethodDefinition = GetRequiredMethodDefinition(nameof(FastArg.IsNotNull), parameterCount: 0);

        /// <summary>
        /// Determines whether the supplied expression contains any recognized <see cref="FastArg" /> or compatibility matcher markers.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <returns><see langword="true" /> when the expression contains at least one matcher marker; otherwise <see langword="false" />.</returns>
        public static bool ContainsMatcher(LambdaExpression expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

            var visitor = new MarkerDetectionVisitor();
            visitor.Visit(expression.Body);
            return visitor.ContainsMarkers;
        }

        /// <summary>
        /// Parses the supplied method-call expression into a provider-neutral invocation matcher.
        /// </summary>
        /// <param name="expression">The method-call expression to parse.</param>
        /// <returns>A parsed invocation matcher describing the target method and argument matchers.</returns>
        public static FastInvocationMatcher ParseInvocation(LambdaExpression expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

            if (TryExtractMethodCall(expression.Body) is not MethodCallExpression methodCallExpression)
            {
                throw new NotSupportedException("Only direct method call expressions are supported by the FastMoq matcher parser.");
            }

            var parameters = methodCallExpression.Method.GetParameters();
            var arguments = new FastArgumentMatcher[methodCallExpression.Arguments.Count];
            for (var index = 0; index < methodCallExpression.Arguments.Count; index++)
            {
                arguments[index] = ParseArgument(methodCallExpression.Arguments[index], parameters[index].ParameterType);
            }

            return new FastInvocationMatcher(methodCallExpression.Method, arguments);
        }

        private static MethodCallExpression? TryExtractMethodCall(Expression expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

            expression = StripConvert(expression);
            if (expression is MethodCallExpression methodCallExpression)
            {
                return methodCallExpression;
            }

            if (expression is not BlockExpression blockExpression || blockExpression.Expressions.Count == 0)
            {
                return null;
            }

            var leadingExpression = StripConvert(blockExpression.Expressions[0]);
            if (leadingExpression is not MethodCallExpression blockMethodCallExpression)
            {
                return null;
            }

            for (var index = 1; index < blockExpression.Expressions.Count; index++)
            {
                var trailingExpression = StripConvert(blockExpression.Expressions[index]);
                if (trailingExpression is DefaultExpression { Type: not null } defaultExpression && defaultExpression.Type == typeof(void))
                {
                    continue;
                }

                return null;
            }

            return blockMethodCallExpression;
        }

        /// <summary>
        /// Attempts to parse a single expression node as a recognized FastMoq matcher.
        /// </summary>
        /// <param name="expression">The expression node to inspect.</param>
        /// <param name="parameterType">The target runtime parameter type.</param>
        /// <param name="matcher">When this method returns <see langword="true" />, contains the parsed matcher.</param>
        /// <returns><see langword="true" /> when the expression is a recognized matcher marker; otherwise <see langword="false" />.</returns>
        public static bool TryParseMatcher(Expression expression, Type parameterType, out FastArgumentMatcher matcher)
        {
            ArgumentNullException.ThrowIfNull(expression);
            ArgumentNullException.ThrowIfNull(parameterType);

            expression = StripConvert(expression);
            if (expression is not MethodCallExpression methodCallExpression)
            {
                matcher = null!;
                return false;
            }

            var method = methodCallExpression.Method.IsGenericMethod
                ? methodCallExpression.Method.GetGenericMethodDefinition()
                : methodCallExpression.Method;

            if (method == AnyMethodDefinition || method == AnyExpressionMethodDefinition || IsBuildExpressionAlias(methodCallExpression.Method))
            {
                matcher = FastArgumentMatcher.Any(parameterType);
                return true;
            }

            if (method == IsNullMethodDefinition)
            {
                matcher = FastArgumentMatcher.Null(parameterType);
                return true;
            }

            if (method == IsNotNullMethodDefinition)
            {
                matcher = FastArgumentMatcher.NotNull(parameterType);
                return true;
            }

            if (method == IsMethodDefinition)
            {
                matcher = FastArgumentMatcher.Predicate(parameterType, UnwrapLambda(methodCallExpression.Arguments[0]));
                return true;
            }

            matcher = null!;
            return false;
        }

        internal static object? Evaluate(Expression expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

            expression = StripConvert(expression);
            var boxed = Expression.Convert(expression, typeof(object));
            return Expression.Lambda<Func<object?>>(boxed).Compile().Invoke();
        }

        private static FastArgumentMatcher ParseArgument(Expression expression, Type parameterType)
        {
            if (TryParseMatcher(expression, parameterType, out var matcher))
            {
                return matcher;
            }

            return FastArgumentMatcher.Exact(parameterType, Evaluate(expression));
        }

        private static bool IsBuildExpressionAlias(MethodInfo method)
        {
            var candidate = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;
            return candidate.IsStatic
                && candidate.IsGenericMethodDefinition
                && candidate.GetParameters().Length == 0
                && string.Equals(candidate.Name, "BuildExpression", StringComparison.Ordinal)
                && string.Equals(candidate.DeclaringType?.FullName, "FastMoq.Mocker", StringComparison.Ordinal);
        }

        private static LambdaExpression UnwrapLambda(Expression expression)
        {
            expression = StripQuote(expression);
            if (expression is LambdaExpression lambdaExpression)
            {
                return lambdaExpression;
            }

            throw new NotSupportedException("FastArg.Is requires a lambda expression predicate.");
        }

        private static Expression StripConvert(Expression expression)
        {
            while (expression is UnaryExpression unaryExpression &&
                (unaryExpression.NodeType == ExpressionType.Convert || unaryExpression.NodeType == ExpressionType.ConvertChecked))
            {
                expression = unaryExpression.Operand;
            }

            return expression;
        }

        private static Expression StripQuote(Expression expression)
        {
            while (expression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Quote)
            {
                expression = unaryExpression.Operand;
            }

            return expression;
        }

        private static MethodInfo GetRequiredMethodDefinition(string methodName, int parameterCount)
        {
            return typeof(FastArg)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method.Name == methodName && method.IsGenericMethodDefinition && method.GetParameters().Length == parameterCount);
        }

        private sealed class MarkerDetectionVisitor : ExpressionVisitor
        {
            public bool ContainsMarkers { get; private set; }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (!ContainsMarkers && TryParseMatcher(node, node.Type, out _))
                {
                    ContainsMarkers = true;
                }

                return base.VisitMethodCall(node);
            }
        }
    }
}

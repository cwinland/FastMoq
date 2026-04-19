using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Providers.MoqProvider
{
    internal static class FastArgMoqExpressionRewriter
    {
        private static readonly MethodInfo ItIsAnyMethodDefinition = typeof(It)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(It.IsAny) && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);

        private static readonly MethodInfo ItIsMethodDefinition = typeof(It)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(It.Is)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 1
                && method.GetParameters()[0].ParameterType.IsGenericType
                && method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));

        private static readonly MethodInfo ObjectEqualsMethod = typeof(object).GetMethod(nameof(object.Equals), [typeof(object), typeof(object)])
            ?? throw new InvalidOperationException("Unable to resolve object.Equals(object?, object?).");

        internal static Expression<TDelegate> Rewrite<TDelegate>(Expression<TDelegate> expression)
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(expression);

            return (Expression<TDelegate>) new MatcherVisitor().Visit(expression)!;
        }

        private static Expression CreateAnyMatcher(Type argumentType)
        {
            return Expression.Call(ItIsAnyMethodDefinition.MakeGenericMethod(argumentType));
        }

        private static Expression CreatePredicateMatcher(Type argumentType, LambdaExpression predicateExpression)
        {
            ArgumentNullException.ThrowIfNull(predicateExpression);

            var typedPredicate = EnsureTypedPredicate(argumentType, predicateExpression);
            return Expression.Call(ItIsMethodDefinition.MakeGenericMethod(argumentType), Expression.Quote(typedPredicate));
        }

        private static Expression CreateNullMatcher(Type argumentType, bool matchNull)
        {
            var parameter = Expression.Parameter(argumentType, "value");
            Expression body = Expression.Call(
                ObjectEqualsMethod,
                Expression.Convert(parameter, typeof(object)),
                Expression.Constant(null, typeof(object)));

            if (!matchNull)
            {
                body = Expression.Not(body);
            }

            var lambda = Expression.Lambda(body, parameter);
            return Expression.Call(ItIsMethodDefinition.MakeGenericMethod(argumentType), Expression.Quote(lambda));
        }

        private static LambdaExpression EnsureTypedPredicate(Type argumentType, LambdaExpression predicateExpression)
        {
            if (predicateExpression.Parameters.Count == 1 && predicateExpression.Parameters[0].Type == argumentType)
            {
                return predicateExpression;
            }

            var parameter = Expression.Parameter(argumentType, predicateExpression.Parameters.Single().Name ?? "value");
            var body = new PredicateParameterReplacementVisitor(predicateExpression.Parameters.Single(), parameter).Visit(predicateExpression.Body)
                ?? throw new InvalidOperationException("Unable to rewrite the FastArg predicate expression.");

            return Expression.Lambda(body, parameter);
        }

        private sealed class MatcherVisitor : ExpressionVisitor
        {
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (FastArgExpressionParser.TryParseMatcher(node, node.Type, out var matcher))
                {
                    return matcher.Kind switch
                    {
                        FastArgumentMatcherKind.Any => CreateAnyMatcher(node.Type),
                        FastArgumentMatcherKind.Predicate => CreatePredicateMatcher(node.Type, matcher.PredicateExpression!),
                        FastArgumentMatcherKind.Null => CreateNullMatcher(node.Type, matchNull: true),
                        FastArgumentMatcherKind.NotNull => CreateNullMatcher(node.Type, matchNull: false),
                        _ => base.VisitMethodCall(node),
                    };
                }

                return base.VisitMethodCall(node);
            }
        }

        private sealed class PredicateParameterReplacementVisitor(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node) => node == source ? target : base.VisitParameter(node);
        }
    }
}

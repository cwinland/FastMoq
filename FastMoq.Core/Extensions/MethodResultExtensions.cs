using FastMoq.Providers;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Extensions
{
    /// <summary>
    /// Provides narrow provider-first helpers for exact-call fixed method results.
    /// </summary>
    public static class MethodResultExtensions
    {
        /// <summary>
        /// Replaces the current interface registration with a proxy that returns a fixed value for the specified method call expression.
        /// </summary>
        /// <typeparam name="TService">The interface type to wrap.</typeparam>
        /// <typeparam name="TResult">The synchronous result type returned by the configured method.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="callExpression">The exact method call expression to intercept. FastMoq matcher markers such as <see cref="FastArg" /> are supported.</param>
        /// <param name="value">The fixed value to return when the call matches <paramref name="callExpression" />.</param>
        /// <param name="replace">True to replace an existing registration for <typeparamref name="TService" />. Defaults to <see langword="true" /> because the helper intentionally swaps in a proxy-backed registration.</param>
        /// <returns>The proxy-backed instance now registered for <typeparamref name="TService" />.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var gateway = Mocks.AddMethodResult<IInventoryGateway, InventoryItem>(
        ///     x => x.Load("alpha"),
        ///     new InventoryItem("alpha"));
        ///
        /// gateway.Load("alpha").Sku.Should().Be("alpha");
        /// ]]></code>
        /// </example>
        public static TService AddMethodResult<TService, TResult>(this Mocker mocker, Expression<Func<TService, TResult>> callExpression, TResult value, bool replace = true)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(callExpression);

            return mocker.AddMethodBehavior(callExpression, () => value, replace);
        }

        /// <summary>
        /// Replaces the current interface registration with a proxy that returns a completed <see cref="Task{TResult}" /> for the specified async method call expression.
        /// </summary>
        /// <typeparam name="TService">The interface type to wrap.</typeparam>
        /// <typeparam name="TResult">The logical result type produced by the asynchronous method.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="callExpression">The exact asynchronous method call expression to intercept. FastMoq matcher markers such as <see cref="FastArg" /> are supported.</param>
        /// <param name="value">The logical result value to wrap in a completed <see cref="Task{TResult}" />.</param>
        /// <param name="replace">True to replace an existing registration for <typeparamref name="TService" />. Defaults to <see langword="true" /> because the helper intentionally swaps in a proxy-backed registration.</param>
        /// <returns>The proxy-backed instance now registered for <typeparamref name="TService" />.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var gateway = Mocks.AddMethodResultAsync<IInventoryGateway, InventoryItem>(
        ///     x => x.LoadAsync("alpha", CancellationToken.None),
        ///     new InventoryItem("alpha"));
        ///
        /// (await gateway.LoadAsync("alpha", CancellationToken.None)).Sku.Should().Be("alpha");
        /// ]]></code>
        /// </example>
        public static TService AddMethodResultAsync<TService, TResult>(this Mocker mocker, Expression<Func<TService, Task<TResult>>> callExpression, TResult value, bool replace = true)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(callExpression);

            return mocker.AddMethodBehavior(callExpression, () => Task.FromResult(value), replace);
        }

        /// <summary>
        /// Replaces the current interface registration with a proxy that returns a completed <see cref="Task" /> for the specified asynchronous method call expression.
        /// </summary>
        /// <typeparam name="TService">The interface type to wrap.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="callExpression">The exact asynchronous method call expression to intercept. FastMoq matcher markers such as <see cref="FastArg" /> are supported.</param>
        /// <param name="replace">True to replace an existing registration for <typeparamref name="TService" />. Defaults to <see langword="true" /> because the helper intentionally swaps in a proxy-backed registration.</param>
        /// <returns>The proxy-backed instance now registered for <typeparamref name="TService" />.</returns>
        public static TService AddMethodCompletionAsync<TService>(this Mocker mocker, Expression<Func<TService, Task>> callExpression, bool replace = true)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(callExpression);

            return mocker.AddMethodBehavior(callExpression, () => Task.CompletedTask, replace);
        }

        /// <summary>
        /// Replaces the current interface registration with a proxy that runs the supplied callback for the specified exact void method call expression.
        /// </summary>
        /// <typeparam name="TService">The interface type to wrap.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="callExpression">The exact void method call expression to intercept. FastMoq matcher markers such as <see cref="FastArg" /> are supported.</param>
        /// <param name="callback">The side effect to execute when the call matches <paramref name="callExpression" />.</param>
        /// <param name="replace">True to replace an existing registration for <typeparamref name="TService" />. Defaults to <see langword="true" /> because the helper intentionally swaps in a proxy-backed registration.</param>
        /// <returns>The proxy-backed instance now registered for <typeparamref name="TService" />.</returns>
        public static TService AddMethodCallback<TService>(this Mocker mocker, Expression<Action<TService>> callExpression, Action callback, bool replace = true)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(callExpression);
            ArgumentNullException.ThrowIfNull(callback);

            return mocker.AddMethodBehavior(callExpression, callback, replace);
        }

        /// <summary>
        /// Replaces the current interface registration with a proxy that runs the supplied callback and returns a completed <see cref="Task" /> for the specified exact asynchronous method call expression.
        /// </summary>
        /// <typeparam name="TService">The interface type to wrap.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="callExpression">The exact asynchronous method call expression to intercept. FastMoq matcher markers such as <see cref="FastArg" /> are supported.</param>
        /// <param name="callback">The side effect to execute when the call matches <paramref name="callExpression" />.</param>
        /// <param name="replace">True to replace an existing registration for <typeparamref name="TService" />. Defaults to <see langword="true" /> because the helper intentionally swaps in a proxy-backed registration.</param>
        /// <returns>The proxy-backed instance now registered for <typeparamref name="TService" />.</returns>
        public static TService AddMethodCallbackAsync<TService>(this Mocker mocker, Expression<Func<TService, Task>> callExpression, Action callback, bool replace = true)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(callExpression);
            ArgumentNullException.ThrowIfNull(callback);

            return mocker.AddMethodBehavior(callExpression, () =>
            {
                callback();
                return Task.CompletedTask;
            }, replace);
        }

        /// <summary>
        /// Replaces the current interface registration with a proxy that throws the supplied exception for the specified method call expression.
        /// </summary>
        /// <typeparam name="TService">The interface type to wrap.</typeparam>
        /// <typeparam name="TResult">The synchronous result type declared by the configured method.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="callExpression">The exact method call expression to intercept. FastMoq matcher markers such as <see cref="FastArg" /> are supported.</param>
        /// <param name="exception">The exception to throw when the call matches <paramref name="callExpression" />.</param>
        /// <param name="replace">True to replace an existing registration for <typeparamref name="TService" />. Defaults to <see langword="true" /> because the helper intentionally swaps in a proxy-backed registration.</param>
        /// <returns>The proxy-backed instance now registered for <typeparamref name="TService" />.</returns>
        public static TService AddMethodException<TService, TResult>(this Mocker mocker, Expression<Func<TService, TResult>> callExpression, Exception exception, bool replace = true)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(callExpression);
            ArgumentNullException.ThrowIfNull(exception);

            return mocker.AddMethodBehavior(callExpression, () => throw exception, replace);
        }

        /// <summary>
        /// Replaces the current interface registration with a proxy that throws the supplied exception for the specified void method call expression.
        /// </summary>
        /// <typeparam name="TService">The interface type to wrap.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="callExpression">The exact void method call expression to intercept. FastMoq matcher markers such as <see cref="FastArg" /> are supported.</param>
        /// <param name="exception">The exception to throw when the call matches <paramref name="callExpression" />.</param>
        /// <param name="replace">True to replace an existing registration for <typeparamref name="TService" />. Defaults to <see langword="true" /> because the helper intentionally swaps in a proxy-backed registration.</param>
        /// <returns>The proxy-backed instance now registered for <typeparamref name="TService" />.</returns>
        public static TService AddMethodException<TService>(this Mocker mocker, Expression<Action<TService>> callExpression, Exception exception, bool replace = true)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(callExpression);
            ArgumentNullException.ThrowIfNull(exception);

            return mocker.AddMethodBehavior(callExpression, () => throw exception, replace);
        }

        /// <summary>
        /// Replaces the current interface registration with a proxy that returns a faulted <see cref="Task{TResult}" /> for the specified asynchronous method call expression.
        /// </summary>
        /// <typeparam name="TService">The interface type to wrap.</typeparam>
        /// <typeparam name="TResult">The logical result type produced by the asynchronous method.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="callExpression">The exact asynchronous method call expression to intercept. FastMoq matcher markers such as <see cref="FastArg" /> are supported.</param>
        /// <param name="exception">The exception to surface through the returned faulted task when the call matches <paramref name="callExpression" />.</param>
        /// <param name="replace">True to replace an existing registration for <typeparamref name="TService" />. Defaults to <see langword="true" /> because the helper intentionally swaps in a proxy-backed registration.</param>
        /// <returns>The proxy-backed instance now registered for <typeparamref name="TService" />.</returns>
        public static TService AddMethodExceptionAsync<TService, TResult>(this Mocker mocker, Expression<Func<TService, Task<TResult>>> callExpression, Exception exception, bool replace = true)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(callExpression);
            ArgumentNullException.ThrowIfNull(exception);

            return mocker.AddMethodBehavior(callExpression, () => Task.FromException<TResult>(exception), replace);
        }

        /// <summary>
        /// Replaces the current interface registration with a proxy that returns a faulted <see cref="Task" /> for the specified asynchronous method call expression.
        /// </summary>
        /// <typeparam name="TService">The interface type to wrap.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="callExpression">The exact asynchronous method call expression to intercept. FastMoq matcher markers such as <see cref="FastArg" /> are supported.</param>
        /// <param name="exception">The exception to surface through the returned faulted task when the call matches <paramref name="callExpression" />.</param>
        /// <param name="replace">True to replace an existing registration for <typeparamref name="TService" />. Defaults to <see langword="true" /> because the helper intentionally swaps in a proxy-backed registration.</param>
        /// <returns>The proxy-backed instance now registered for <typeparamref name="TService" />.</returns>
        public static TService AddMethodExceptionAsync<TService>(this Mocker mocker, Expression<Func<TService, Task>> callExpression, Exception exception, bool replace = true)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(callExpression);
            ArgumentNullException.ThrowIfNull(exception);

            return mocker.AddMethodBehavior(callExpression, () => Task.FromException(exception), replace);
        }

        private static TService AddMethodBehavior<TService, TResult>(this Mocker mocker, Expression<Func<TService, TResult>> callExpression, Func<TResult> resultFactory, bool replace)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(resultFactory);

            var serviceType = typeof(TService);
            if (!serviceType.IsInterface)
            {
                throw new NotSupportedException($"{nameof(AddMethodResult)} currently supports interface types only. Use a fake or stub plus {nameof(Mocker.AddType)}(...) for {serviceType.Name}.");
            }

            var currentInstance = mocker.GetObject<TService>() ?? throw new InvalidOperationException($"Unable to resolve an instance for {serviceType.Name} before adding method behavior.");
            if (currentInstance is MethodResultProxy<TService> existingProxy)
            {
                existingProxy.AddBehavior(callExpression, () => resultFactory()!);
                return currentInstance;
            }

            var proxy = DispatchProxy.Create<TService, MethodResultProxy<TService>>();
            var proxyController = (MethodResultProxy<TService>) (object) proxy;
            proxyController.Initialize(currentInstance, mocker.TryGetTrackedMock<TService>(out var trackedMock) && ReferenceEquals(trackedMock.Instance, currentInstance));
            proxyController.AddBehavior(callExpression, () => resultFactory()!);

            mocker.AddType<TService>(proxy, replace);
            return proxy;
        }

        private static TService AddMethodBehavior<TService>(this Mocker mocker, Expression<Action<TService>> callExpression, Action behavior, bool replace)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(behavior);

            var serviceType = typeof(TService);
            if (!serviceType.IsInterface)
            {
                throw new NotSupportedException($"{nameof(AddMethodResult)} currently supports interface types only. Use a fake or stub plus {nameof(Mocker.AddType)}(...) for {serviceType.Name}.");
            }

            var currentInstance = mocker.GetObject<TService>() ?? throw new InvalidOperationException($"Unable to resolve an instance for {serviceType.Name} before adding method behavior.");
            if (currentInstance is MethodResultProxy<TService> existingProxy)
            {
                existingProxy.AddBehavior(callExpression, () =>
                {
                    behavior();
                    return null;
                });
                return currentInstance;
            }

            var proxy = DispatchProxy.Create<TService, MethodResultProxy<TService>>();
            var proxyController = (MethodResultProxy<TService>) (object) proxy;
            proxyController.Initialize(currentInstance, mocker.TryGetTrackedMock<TService>(out var trackedMock) && ReferenceEquals(trackedMock.Instance, currentInstance));
            proxyController.AddBehavior(callExpression, () =>
            {
                behavior();
                return null;
            });

            mocker.AddType<TService>(proxy, replace);
            return proxy;
        }
    }

    internal class MethodResultProxy<TService> : DispatchProxy where TService : class
    {
        private readonly List<MethodResultRegistration> _registrations = [];

        private TService? _inner;
        private bool _forwardConfiguredCallsToInner;

        public void Initialize(TService inner, bool forwardConfiguredCallsToInner)
        {
            ArgumentNullException.ThrowIfNull(inner);

            _inner = inner;
            _forwardConfiguredCallsToInner = forwardConfiguredCallsToInner;
        }

        public void AddBehavior(LambdaExpression callExpression, Func<object?> resultFactory)
        {
            ArgumentNullException.ThrowIfNull(callExpression);
            ArgumentNullException.ThrowIfNull(resultFactory);

            FastInvocationMatcher invocation;
            try
            {
                invocation = FastArgExpressionParser.ParseInvocation(callExpression);
            }
            catch (NotSupportedException ex)
            {
                throw new NotSupportedException($"{nameof(MethodResultExtensions.AddMethodResult)} supports direct method call expressions only.", ex);
            }

            _registrations.Add(new MethodResultRegistration(invocation, resultFactory));
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            var actualArguments = args ?? Array.Empty<object?>();
            for (var index = _registrations.Count - 1; index >= 0; index--)
            {
                var registration = _registrations[index];
                if (!registration.Invocation.Matches(targetMethod, actualArguments))
                {
                    continue;
                }

                if (_forwardConfiguredCallsToInner)
                {
                    _ = InvokeInner(targetMethod, actualArguments);
                }

                return registration.ResultFactory();
            }

            return InvokeInner(targetMethod, actualArguments);
        }

        private object? InvokeInner(MethodInfo targetMethod, object?[] actualArguments)
        {
            if (_inner is null)
            {
                throw new InvalidOperationException($"{nameof(MethodResultProxy<TService>)} has not been initialized.");
            }

            try
            {
                return targetMethod.Invoke(_inner, actualArguments);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }
        }

        private sealed record MethodResultRegistration(FastInvocationMatcher Invocation, Func<object?> ResultFactory);
    }
}
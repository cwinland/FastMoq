using System.Linq.Expressions;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Moq;
using Moq.Protected;

namespace FastMoq.Extensions
{
    /// <summary>
    /// Moq-specific HTTP setup compatibility helpers that remain available from the provider package.
    /// These helpers intentionally stay in the FastMoq.Extensions namespace for low-churn migration,
    /// while new tests prefer the provider-neutral HTTP helpers in FastMoq.Core.
    /// </summary>
    public static class MockerHttpMoqExtensions
    {
        /// <summary>
        /// Sets up an HTTP message handler using Moq compatibility behavior.
        /// Prefer <c>WhenHttpRequest(...)</c> or <c>WhenHttpRequestJson(...)</c> in new tests when protected-member setup is not required.
        /// </summary>
        public static void SetupHttpMessage(this object mocker, Func<HttpResponseMessage> messageFunc, Expression? request = null, Expression? cancellationToken = null)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(messageFunc);

            var usesTrackedHandler = ContainsTrackedMock<HttpMessageHandler>(mocker);
            if (!usesTrackedHandler && request == null && cancellationToken == null)
            {
                InvokeCoreWhenHttpRequest(mocker, _ => true, messageFunc);
                return;
            }

            request ??= ItExpr.IsAny<HttpRequestMessage>();
            cancellationToken ??= ItExpr.IsAny<CancellationToken>();
            GetFastMock<HttpMessageHandler>(mocker).AsMoq().Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", request, cancellationToken)
                .ReturnsAsync(messageFunc)
                .Verifiable();
        }

        /// <summary>
        /// Sets up a synchronous member using Moq compatibility behavior.
        /// </summary>
        public static void SetupMessage<TMock, TReturn>(this object mocker, Expression<Func<TMock, TReturn>> expression, Func<TReturn> messageFunc)
            where TMock : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(expression);
            ArgumentNullException.ThrowIfNull(messageFunc);

            GetFastMock<TMock>(mocker).AsMoq()
                .Setup(expression)
                ?.Returns(messageFunc)
                ?.Verifiable();
        }

        /// <summary>
        /// Sets up an asynchronous member using Moq compatibility behavior.
        /// </summary>
        public static void SetupMessageAsync<TMock, TReturn>(this object mocker, Expression<Func<TMock, Task<TReturn>>> expression, Func<TReturn> messageFunc)
            where TMock : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(expression);
            ArgumentNullException.ThrowIfNull(messageFunc);

            (GetFastMock<TMock>(mocker).AsMoq().Setup(expression)
                ?? throw new InvalidDataException($"Unable to setup '{typeof(TMock)}'."))
                .ReturnsAsync(messageFunc)
                ?.Verifiable();
        }

        /// <summary>
        /// Sets up a protected member using Moq compatibility behavior.
        /// </summary>
        public static void SetupMessageProtected<TMock, TReturn>(this object mocker, string methodOrPropertyName, Func<TReturn> messageFunc, params object?[]? args)
            where TMock : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodOrPropertyName);
            ArgumentNullException.ThrowIfNull(messageFunc);

            GetFastMock<TMock>(mocker).AsMoq().Protected()
                ?.Setup<TReturn>(methodOrPropertyName, args ?? [])
                ?.Returns(messageFunc)
                ?.Verifiable();
        }

        /// <summary>
        /// Sets up an asynchronous protected member using Moq compatibility behavior.
        /// </summary>
        public static void SetupMessageProtectedAsync<TMock, TReturn>(this object mocker, string methodOrPropertyName, Func<TReturn> messageFunc, params object?[]? args)
            where TMock : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodOrPropertyName);
            ArgumentNullException.ThrowIfNull(messageFunc);

            GetFastMock<TMock>(mocker).AsMoq().Protected()
                ?.Setup<Task<TReturn>>(methodOrPropertyName, args ?? [])
                ?.ReturnsAsync(messageFunc)
                ?.Verifiable();
        }

        private static bool ContainsTrackedMock<TMock>(object mocker)
            where TMock : class
        {
            var containsMethod = mocker.GetType()
                .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .FirstOrDefault(method => method.Name == "Contains" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0)
                ?.MakeGenericMethod(typeof(TMock))
                ?? throw new MissingMethodException(mocker.GetType().FullName, "Contains<T>");

            return containsMethod.Invoke(mocker, null) as bool? == true;
        }

        private static IFastMock<TMock> GetFastMock<TMock>(object mocker)
            where TMock : class
        {
            var method = mocker.GetType()
                .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == "GetOrCreateMock" && candidate.IsGenericMethodDefinition)
                ?.MakeGenericMethod(typeof(TMock))
                ?? throw new MissingMethodException(mocker.GetType().FullName, "GetOrCreateMock<T>");

            return method.Invoke(mocker, [null]) as IFastMock<TMock>
                ?? throw new InvalidOperationException($"Unable to get tracked mock for '{typeof(TMock).FullName}'.");
        }

        private static void InvokeCoreWhenHttpRequest(object mocker, Func<HttpRequestMessage, bool> predicate, Func<HttpResponseMessage> messageFunc)
        {
            var extensionType = mocker.GetType().Assembly.GetType("FastMoq.Extensions.MockerHttpExtensions")
                ?? throw new TypeLoadException("Unable to locate FastMoq.Extensions.MockerHttpExtensions.");

            var method = extensionType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .FirstOrDefault(candidate => candidate.Name == "WhenHttpRequest" && candidate.GetParameters().Length == 3)
                ?? throw new MissingMethodException(extensionType.FullName, "WhenHttpRequest");

            method.Invoke(null, [mocker, predicate, messageFunc]);
        }
    }
}

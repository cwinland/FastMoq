using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace FastMoq.Providers.MoqProvider
{
    /// <summary>
    /// Moq-specific convenience extensions for <see cref="IFastMock"/> and <see cref="IFastMock{T}"/>.
    /// These stay in the provider package so the core abstractions remain provider agnostic.
    /// Prefer provider-first members such as <see cref="IFastMock.Instance" />, <see cref="IFastMock.Reset" />, and FastMoq verification helpers first, and use these extensions when the test intentionally needs Moq-specific APIs.
    /// </summary>
    public static class IFastMockMoqExtensions
    {
        /// <summary>
        /// Returns the provider-native <see cref="Mock{T}"/> instance for a tracked FastMoq mock.
        /// Use this when the test intentionally needs a Moq-only API such as <c>Setup(...)</c> or <c>Protected()</c>.
        /// </summary>
        /// <typeparam name="T">Mocked type.</typeparam>
        /// <param name="fastMock">Tracked FastMoq mock.</param>
        /// <returns>The underlying <see cref="Mock{T}"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fastMock"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Thrown when the active provider is not Moq.</exception>
        /// <example>
        /// <para>Select the Moq provider first, then use <see cref="AsMoq{T}(IFastMock{T})"/> as the provider-native escape hatch when the test truly needs Moq semantics.</para>
        /// <code language="csharp"><![CDATA[
        /// using var providerScope = MockingProviderRegistry.Push("moq");
        ///
        /// var mocker = new Mocker();
        /// var gateway = mocker.GetOrCreateMock<IOrderGateway>();
        ///
        /// gateway.AsMoq()
        ///     .Setup(x => x.Publish(42))
        ///     .Returns(true);
        /// ]]></code>
        /// </example>
        public static Mock<T> AsMoq<T>(this IFastMock<T> fastMock) where T : class
        {
            ArgumentNullException.ThrowIfNull(fastMock);

            if (fastMock.NativeMock is Mock<T> mock)
            {
                return mock;
            }

            throw CreateProviderMismatchException(typeof(T), fastMock.NativeMock);
        }

        /// <summary>
        /// Returns the provider-native non-generic <see cref="Mock"/> instance for a tracked FastMoq mock.
        /// Use this only when the test intentionally needs Moq's non-generic APIs.
        /// </summary>
        /// <param name="fastMock">Tracked FastMoq mock.</param>
        /// <returns>The underlying non-generic <see cref="Mock"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fastMock"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Thrown when the active provider is not Moq.</exception>
        public static Mock AsMoq(this IFastMock fastMock)
        {
            ArgumentNullException.ThrowIfNull(fastMock);

            if (fastMock.NativeMock is Mock mock)
            {
                return mock;
            }

            throw CreateProviderMismatchException(fastMock.MockedType, fastMock.NativeMock);
        }

        /// <summary>
        /// Moq convenience shortcut for <c>fastMock.AsMoq().Setup(...)</c>.
        /// </summary>
        public static ISetup<T> Setup<T>(this IFastMock<T> fastMock, Expression<Action<T>> expression) where T : class
        {
            ArgumentNullException.ThrowIfNull(expression);
            return fastMock.AsMoq().Setup(expression);
        }

        /// <summary>
        /// Moq convenience shortcut for <c>fastMock.AsMoq().Setup(...)</c>.
        /// </summary>
        public static ISetup<T, TResult> Setup<T, TResult>(this IFastMock<T> fastMock, Expression<Func<T, TResult>> expression) where T : class
        {
            ArgumentNullException.ThrowIfNull(expression);
            return fastMock.AsMoq().Setup(expression);
        }

        /// <summary>
        /// Moq convenience shortcut for <c>fastMock.AsMoq().SetupGet(...)</c>.
        /// </summary>
        public static ISetupGetter<T, TProperty> SetupGet<T, TProperty>(this IFastMock<T> fastMock, Expression<Func<T, TProperty>> expression) where T : class
        {
            ArgumentNullException.ThrowIfNull(expression);
            return fastMock.AsMoq().SetupGet(expression);
        }

        /// <summary>
        /// Moq convenience shortcut for <c>fastMock.AsMoq().SetupSequence(...)</c>.
        /// </summary>
        public static Moq.Language.ISetupSequentialResult<TResult> SetupSequence<T, TResult>(this IFastMock<T> fastMock, Expression<Func<T, TResult>> expression) where T : class
        {
            ArgumentNullException.ThrowIfNull(expression);
            return fastMock.AsMoq().SetupSequence(expression);
        }

        /// <summary>
        /// Moq convenience shortcut for <c>fastMock.AsMoq().Protected()</c>.
        /// </summary>
        public static IProtectedMock<T> Protected<T>(this IFastMock<T> fastMock) where T : class
        {
            return fastMock.AsMoq().Protected();
        }

        /// <summary>
        /// Moq logger compatibility shortcut for <c>fastMock.AsMoq().VerifyLogger(...)</c>.
        /// </summary>
        public static void VerifyLogger(this IFastMock<ILogger> fastMock, LogLevel logLevel, string message, int times = 1)
        {
            fastMock.AsMoq().VerifyLogger(logLevel, message, times);
        }

        /// <summary>
        /// Moq logger compatibility shortcut for <c>fastMock.AsMoq().VerifyLogger(...)</c>.
        /// </summary>
        public static void VerifyLogger<TLogger>(this IFastMock<TLogger> fastMock, LogLevel logLevel, string message, int times = 1)
            where TLogger : class, ILogger
        {
            fastMock.AsMoq().VerifyLogger(logLevel, message, times);
        }

        /// <summary>
        /// Moq logger compatibility shortcut for <c>fastMock.AsMoq().VerifyLogger(...)</c>.
        /// </summary>
        public static void VerifyLogger(this IFastMock<ILogger> fastMock, LogLevel logLevel, string message, Exception? exception, int? eventId = null, int times = 1)
        {
            fastMock.AsMoq().VerifyLogger(logLevel, message, exception, eventId, times);
        }

        /// <summary>
        /// Moq logger compatibility shortcut for <c>fastMock.AsMoq().VerifyLogger(...)</c>.
        /// </summary>
        public static void VerifyLogger<TLogger>(this IFastMock<TLogger> fastMock, LogLevel logLevel, string message, Exception? exception, int? eventId = null, int times = 1)
            where TLogger : class, ILogger
        {
            fastMock.AsMoq().VerifyLogger(logLevel, message, exception, eventId, times);
        }

        /// <summary>
        /// Moq logger compatibility shortcut for <c>fastMock.AsMoq().SetupLoggerCallback(...)</c>.
        /// </summary>
        public static void SetupLoggerCallback(this IFastMock<ILogger> fastMock, Action<LogLevel, EventId, string, Exception?> callback)
        {
            fastMock.AsMoq().SetupLoggerCallback(callback);
        }

        /// <summary>
        /// Moq logger compatibility shortcut for <c>fastMock.AsMoq().SetupLoggerCallback(...)</c>.
        /// </summary>
        public static void SetupLoggerCallback<TLogger>(this IFastMock<TLogger> fastMock, Action<LogLevel, EventId, string, Exception?> callback)
            where TLogger : class, ILogger
        {
            fastMock.AsMoq().SetupLoggerCallback(callback);
        }

        private static NotSupportedException CreateProviderMismatchException(Type mockedType, object? nativeMock)
        {
            return new NotSupportedException(ProviderSelectionDiagnostics.BuildProviderMismatchMessage(
                "moq",
                mockedType,
                nativeMock,
                nativeMock,
                "AsMoq",
                "provider-neutral FastMoq APIs"));
        }
    }
}
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FastMoq.Providers.MoqProvider
{
    public static class MoqLoggerCompatibility
    {
        public static void VerifyLogger(this Mock<ILogger> loggerMock, LogLevel logLevel, string message, int times = 1) =>
            loggerMock.VerifyLogger(logLevel, message, null, null, times);

        public static void VerifyLogger<TLogger>(this Mock<TLogger> loggerMock, LogLevel logLevel, string message, int times = 1)
            where TLogger : class, ILogger =>
            loggerMock.VerifyLogger(logLevel, message, null, null, times);

        public static void VerifyLogger(this Mock<ILogger> loggerMock, LogLevel logLevel, string message, Exception? exception, int? eventId = null, int times = 1) =>
            loggerMock.VerifyLogger<Exception>(logLevel, message, exception, eventId, times);

        public static void VerifyLogger<TLogger>(this Mock<TLogger> loggerMock, LogLevel logLevel, string message, Exception? exception, int? eventId = null, int times = 1)
            where TLogger : class, ILogger =>
            loggerMock.VerifyLogger<Exception, TLogger>(logLevel, message, exception, eventId, times);

        public static void VerifyLogger<TException>(this Mock<ILogger> loggerMock, LogLevel logLevel, string message, TException? exception, int? eventId = null, int times = 1)
            where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(loggerMock);
            loggerMock.Verify(TestLoggerExpression<TException, ILogger>(logLevel, message, exception, eventId), Times.Exactly(times));
        }

        public static void VerifyLogger<TException>(this Mock<ILogger> loggerMock, LogLevel logLevel, string message, TException? exception, int? eventId, Times times)
            where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(loggerMock);
            loggerMock.Verify(TestLoggerExpression<TException, ILogger>(logLevel, message, exception, eventId), times);
        }

        public static void VerifyLogger<TException>(this Mock<ILogger> loggerMock, LogLevel logLevel, string message, TException? exception, int? eventId, Func<Times> times)
            where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(loggerMock);
            loggerMock.Verify(TestLoggerExpression<TException, ILogger>(logLevel, message, exception, eventId), times);
        }

        public static void VerifyLogger<TException, TLogger>(this Mock<TLogger> loggerMock, LogLevel logLevel, string message, TException? exception, int? eventId = null, int times = 1)
            where TException : Exception
            where TLogger : class, ILogger
        {
            ArgumentNullException.ThrowIfNull(loggerMock);
            loggerMock.Verify(TestLoggerExpression<TException, TLogger>(logLevel, message, exception, eventId), Times.Exactly(times));
        }

        public static void SetupLoggerCallback<TLogger>(this Mock<TLogger> logger, Action<LogLevel, EventId, string, Exception?> callback)
            where TLogger : class, ILogger
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(callback);

            logger
                .Setup(x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()))
                .Callback((LogLevel logLevel, EventId eventId, object state, Exception? exception, Delegate formatter) =>
                {
                    var message = formatter.DynamicInvoke(state, exception);
                    callback.Invoke(logLevel, eventId, message?.ToString() ?? string.Empty, exception);
                });
        }

        internal static Expression<Action<TLoggerType>> TestLoggerExpression<TException, TLoggerType>(LogLevel logLevel, string message, TException? exception, int? eventId)
            where TException : Exception
            where TLoggerType : ILogger =>
            logger =>
                logger.Log(
                    logLevel,
                    It.Is<EventId>(e => CheckEventId(e, eventId)),
                    It.Is<It.IsAnyType>((o, t) => CheckMessage(o.ToString() ?? string.Empty, t, message, t)),
                    It.Is<Exception>(e => CheckException(e, exception)),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>());

        internal static bool CheckMessage(string verifyMessage, Type type, string expectedMessage, Type expectedType) =>
            verifyMessage.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase) &&
            type.IsAssignableTo(expectedType);

        internal static bool CheckEventId(EventId verifyEventId, int? eventId) => eventId == null || verifyEventId == eventId;

        internal static bool CheckException(Exception? verifyException, Exception? expectedException) => expectedException == null ||
            (verifyException != null &&
             verifyException.Message.Contains(expectedException.Message, StringComparison.OrdinalIgnoreCase) &&
             verifyException.GetType().IsAssignableTo(expectedException.GetType()));
    }
}
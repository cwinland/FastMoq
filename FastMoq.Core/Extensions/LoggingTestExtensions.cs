using Microsoft.Extensions.Logging;

namespace FastMoq.Extensions
{
    /// <summary>
    /// Provides framework-neutral logger registration helpers for test composition.
    /// </summary>
    public static class LoggingTestExtensions
    {
        private const string DefaultLoggerCategoryName = "FastMoq";

        /// <summary>
        /// Creates a logger factory that forwards captured log entries into the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="configureLogging">Optional logging builder customization applied before the FastMoq capture provider is added.</param>
        /// <returns>A logger factory that writes into <see cref="Mocker.LogEntries" /> through the current mocker callback.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var loggerFactory = Mocks.CreateLoggerFactory();
        /// var provider = Mocks.CreateTypedServiceProvider(services =>
        /// {
        ///     services.AddSingleton(loggerFactory);
        ///     services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        /// });
        /// ]]></code>
        /// </example>
        public static ILoggerFactory CreateLoggerFactory(this Mocker mocker, Action<ILoggingBuilder>? configureLogging = null)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            mocker.EnableExplicitLoggerCapture();
            return new FastMoqLoggerFactory(mocker.LoggingCallback, configureLogging);
        }

        /// <summary>
        /// Creates and registers a logger factory along with direct <see cref="ILogger" /> and <see cref="ILogger{TCategoryName}" /> resolution support.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="configureLogging">Optional logging builder customization applied before the FastMoq capture provider is added.</param>
        /// <param name="replace">True to replace existing logger registrations.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var mocker = new Mocker()
        ///     .AddLoggerFactory();
        ///
        /// var logger = mocker.GetObject<ILogger<CheckoutService>>();
        /// logger.LogInformation("submitted order");
        ///
        /// mocker.VerifyLogged(LogLevel.Information, "submitted order");
        /// ]]></code>
        /// </example>
        public static Mocker AddLoggerFactory(this Mocker mocker, Action<ILoggingBuilder>? configureLogging = null, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            var loggerFactory = mocker.CreateLoggerFactory(configureLogging);
            return mocker.AddLoggerFactory(loggerFactory, replace);
        }

        /// <summary>
        /// Registers an <see cref="ILoggerFactory" /> and resolves <see cref="ILogger" /> services from that factory.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="loggerFactory">The logger factory to register.</param>
        /// <param name="replace">True to replace existing logger registrations.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        /// <remarks>
        /// Prefer <see cref="CreateLoggerFactory(Mocker, Action{ILoggingBuilder}?)" /> when you want log output to flow into <see cref="Mocker.LogEntries" /> and <see cref="TestClassExtensions.VerifyLogged(Mocker, LogLevel, string)" />.
        /// </remarks>
        public static Mocker AddLoggerFactory(this Mocker mocker, ILoggerFactory loggerFactory, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            mocker.AddType<ILoggerFactory>(loggerFactory, replace);
            mocker.AddKnownType<ILogger>(
                directInstanceFactory: (_, requestedType) => TryCreateLogger(loggerFactory, requestedType),
                managedInstanceFactory: (_, requestedType) => TryCreateLogger(loggerFactory, requestedType),
                includeDerivedTypes: true,
                replace: replace);

            return mocker;
        }

        private static ILogger? TryCreateLogger(ILoggerFactory loggerFactory, Type requestedType)
        {
            if (requestedType == typeof(ILogger))
            {
                return loggerFactory.CreateLogger(DefaultLoggerCategoryName);
            }

            if (!requestedType.IsGenericType || requestedType.GetGenericTypeDefinition() != typeof(ILogger<>))
            {
                return null;
            }

            var categoryType = requestedType.GetGenericArguments()[0];
            var loggerType = typeof(Logger<>).MakeGenericType(categoryType);
            return Activator.CreateInstance(loggerType, loggerFactory) as ILogger;
        }

        private sealed class FastMoqLoggerFactory : ILoggerFactory
        {
            private readonly ILoggerFactory innerFactory;

            public FastMoqLoggerFactory(Action<LogLevel, EventId, string, Exception?> callback, Action<ILoggingBuilder>? configureLogging)
            {
                ArgumentNullException.ThrowIfNull(callback);

                innerFactory = LoggerFactory.Create(builder =>
                {
                    configureLogging?.Invoke(builder);
                    builder.AddProvider(new FastMoqLoggerProvider(callback));
                });
            }

            public void AddProvider(ILoggerProvider provider)
            {
                innerFactory.AddProvider(provider);
            }

            public ILogger CreateLogger(string categoryName)
            {
                return innerFactory.CreateLogger(categoryName);
            }

            public void Dispose()
            {
                innerFactory.Dispose();
            }
        }

        private sealed class FastMoqLoggerProvider(Action<LogLevel, EventId, string, Exception?> callback) : ILoggerProvider
        {
            private readonly Action<LogLevel, EventId, string, Exception?> callback = callback ?? throw new ArgumentNullException(nameof(callback));

            public ILogger CreateLogger(string categoryName)
            {
                return new FastMoqLogger(callback);
            }

            public void Dispose()
            {
            }
        }

        private sealed class FastMoqLogger(Action<LogLevel, EventId, string, Exception?> callback) : ILogger
        {
            private static readonly IDisposable EmptyScope = new EmptyDisposable();
            private readonly Action<LogLevel, EventId, string, Exception?> callback = callback ?? throw new ArgumentNullException(nameof(callback));

            public IDisposable BeginScope<TState>(TState state) where TState : notnull
            {
                return EmptyScope;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel != LogLevel.None;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                ArgumentNullException.ThrowIfNull(formatter);

                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var message = formatter(state, exception);
                callback(logLevel, eventId, message ?? state?.ToString() ?? string.Empty, exception);
            }

            private sealed class EmptyDisposable : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}
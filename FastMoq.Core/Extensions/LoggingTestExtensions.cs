using Microsoft.Extensions.Logging;
using System.Text;

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

            return CreateLoggerFactoryCore(mocker, mocker.LoggingCallback, configureLogging);
        }

        /// <summary>
        /// Creates a logger factory that mirrors captured log entries to the supplied sink while still forwarding them into the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="sink">A sink that receives each captured log entry after FastMoq records it.</param>
        /// <param name="configureLogging">Optional logging builder customization applied before the FastMoq capture provider is added.</param>
        /// <returns>A logger factory that mirrors captured entries to <paramref name="sink" /> and <see cref="Mocker.LogEntries" />.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var loggerFactory = Mocks.CreateLoggerFactory((logLevel, eventId, message, exception) =>
        /// {
        ///     output.WriteLine($"[{logLevel}] {message}");
        /// });
        /// ]]></code>
        /// </example>
        public static ILoggerFactory CreateLoggerFactory(this Mocker mocker, Action<LogLevel, EventId, string, Exception?> sink, Action<ILoggingBuilder>? configureLogging = null)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(sink);

            return CreateLoggerFactoryCore(mocker, CreateMirroredCallback(mocker, sink), configureLogging);
        }

        /// <summary>
        /// Creates a logger factory that mirrors formatted log lines to the supplied writer while still forwarding captured entries into the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="lineWriter">A line writer that receives a formatted representation of each captured log entry.</param>
        /// <param name="configureLogging">Optional logging builder customization applied before the FastMoq capture provider is added.</param>
        /// <returns>A logger factory that mirrors formatted log lines to <paramref name="lineWriter" /> and raw entries to <see cref="Mocker.LogEntries" />.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var loggerFactory = Mocks.CreateLoggerFactory(output.WriteLine);
        /// ]]></code>
        /// </example>
        public static ILoggerFactory CreateLoggerFactory(this Mocker mocker, Action<string> lineWriter, Action<ILoggingBuilder>? configureLogging = null)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(lineWriter);

            return mocker.CreateLoggerFactory(CreateLineWriterCallback(lineWriter), configureLogging);
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
        /// Creates and registers a logger factory that mirrors captured log entries to the supplied sink while still forwarding them into the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="sink">A sink that receives each captured log entry after FastMoq records it.</param>
        /// <param name="configureLogging">Optional logging builder customization applied before the FastMoq capture provider is added.</param>
        /// <param name="replace">True to replace existing logger registrations.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddLoggerFactory(this Mocker mocker, Action<LogLevel, EventId, string, Exception?> sink, Action<ILoggingBuilder>? configureLogging = null, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(sink);

            var loggerFactory = mocker.CreateLoggerFactory(sink, configureLogging);
            return mocker.AddLoggerFactory(loggerFactory, replace);
        }

        /// <summary>
        /// Creates and registers a logger factory that mirrors formatted log lines to the supplied writer while still forwarding captured entries into the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="lineWriter">A line writer that receives a formatted representation of each captured log entry.</param>
        /// <param name="configureLogging">Optional logging builder customization applied before the FastMoq capture provider is added.</param>
        /// <param name="replace">True to replace existing logger registrations.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddLoggerFactory(this Mocker mocker, Action<string> lineWriter, Action<ILoggingBuilder>? configureLogging = null, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(lineWriter);

            var loggerFactory = mocker.CreateLoggerFactory(lineWriter, configureLogging);
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

        private static ILoggerFactory CreateLoggerFactoryCore(Mocker mocker, Action<LogLevel, EventId, string, Exception?> callback, Action<ILoggingBuilder>? configureLogging)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(callback);

            mocker.EnableExplicitLoggerCapture();
            return new FastMoqLoggerFactory(callback, configureLogging);
        }

        private static Action<LogLevel, EventId, string, Exception?> CreateMirroredCallback(Mocker mocker, Action<LogLevel, EventId, string, Exception?> sink)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(sink);

            return (logLevel, eventId, message, exception) =>
            {
                mocker.LoggingCallback(logLevel, eventId, message, exception);
                sink(logLevel, eventId, message, exception);
            };
        }

        private static Action<LogLevel, EventId, string, Exception?> CreateLineWriterCallback(Action<string> lineWriter)
        {
            ArgumentNullException.ThrowIfNull(lineWriter);

            return (logLevel, eventId, message, exception) => lineWriter(FormatLogEntry(logLevel, eventId, message, exception));
        }

        private static string FormatLogEntry(LogLevel logLevel, EventId eventId, string message, Exception? exception)
        {
            var builder = new StringBuilder();
            builder.Append('[');
            builder.Append(GetShortLogLevel(logLevel));
            builder.Append(']');

            if (eventId.Id != 0 || !string.IsNullOrWhiteSpace(eventId.Name))
            {
                builder.Append(" (");
                builder.Append(eventId.Id);

                if (!string.IsNullOrWhiteSpace(eventId.Name))
                {
                    builder.Append(": ");
                    builder.Append(eventId.Name);
                }

                builder.Append(')');
            }

            if (!string.IsNullOrEmpty(message))
            {
                builder.Append(' ');
                builder.Append(message);
            }

            if (exception is not null)
            {
                builder.Append(Environment.NewLine);
                builder.Append(exception);
            }

            return builder.ToString();
        }

        private static char GetShortLogLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => 'T',
                LogLevel.Debug => 'D',
                LogLevel.Information => 'I',
                LogLevel.Warning => 'W',
                LogLevel.Error => 'E',
                LogLevel.Critical => 'C',
                LogLevel.None => 'N',
                _ => 'U',
            };
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
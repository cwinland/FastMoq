using FastMoq.Extensions;
using FastMoq.Providers;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace FastMoq.AzureFunctions.Extensions
{
    /// <summary>
    /// Provides Durable Task orchestration helpers that route replay-safe logger creation back through FastMoq's existing logger capture APIs.
    /// </summary>
    public static class TaskOrchestrationContextTestExtensions
    {
        /// <summary>
        /// Configures a tracked <see cref="TaskOrchestrationContext" /> mock so <see cref="TaskOrchestrationContext.CreateReplaySafeLogger(string)" /> can use the supplied logger factory and replay state.
        /// </summary>
        /// <param name="fastMock">The tracked mock whose orchestration logging behavior should be configured.</param>
        /// <param name="loggerFactory">The logger factory that replay-safe logger creation should delegate to.</param>
        /// <param name="isReplaying">True to suppress replay-safe logger output, false to allow normal log capture.</param>
        /// <returns>The current tracked mock.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="fastMock" /> does not represent a <see cref="TaskOrchestrationContext" />.</exception>
        public static IFastMock AddTaskOrchestrationReplaySafeLogging(this IFastMock fastMock, ILoggerFactory loggerFactory, bool isReplaying = false)
        {
            ArgumentNullException.ThrowIfNull(fastMock);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            if (!typeof(TaskOrchestrationContext).IsAssignableFrom(fastMock.MockedType))
            {
                throw new ArgumentException($"The supplied mock must represent {typeof(TaskOrchestrationContext).FullName}.", nameof(fastMock));
            }

            if (!TryConfigureTaskOrchestrationReplaySafeLogging(fastMock, loggerFactory, isReplaying: false))
            {
                throw new NotSupportedException("Tracked TaskOrchestrationContext replay-safe logging currently requires a provider that can configure the protected Durable logger factory getter through FastMoq's tracked-property configuration contract. The built-in Moq provider supports this today. Use Mocker.AddTaskOrchestrationReplaySafeLogging(...) before resolving TaskOrchestrationContext for provider-neutral concrete-instance coverage.");
            }

            if (isReplaying)
            {
                throw new NotSupportedException("Tracked TaskOrchestrationContext replay-state suppression is not supported on the mock-backed helper. Use Mocker.AddTaskOrchestrationReplaySafeLogging(isReplaying: true, ...) before resolving TaskOrchestrationContext so FastMoq can supply a concrete orchestration context.");
            }

            return fastMock;
        }

        /// <summary>
        /// Registers a capture-backed logger factory and configures tracked <see cref="TaskOrchestrationContext" /> mocks to use replay-safe logger creation.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="isReplaying">True to suppress replay-safe logger output, false to allow normal log capture.</param>
        /// <param name="replace">True to replace an existing orchestration helper registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        /// <remarks>
        /// This helper keeps assertions on the normal <see cref="Mocker.LogEntries" /> and <see cref="TestClassExtensions.VerifyLogged(FastMoq.Mocker, LogLevel, string, TimesSpec?)" /> surface.
        /// When <see cref="TaskOrchestrationContext" /> has not already been resolved, this helper can register a concrete replay-safe orchestration context on Moq, NSubstitute, or reflection paths.
        /// If a tracked orchestration mock already exists, the active provider must support FastMoq's tracked-property configuration contract for the protected Durable logger factory getter; the built-in Moq provider supports that path today.
        /// </remarks>
        public static Mocker AddTaskOrchestrationReplaySafeLogging(this Mocker mocker, bool isReplaying = false, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            mocker.AddCapturedLoggerFactory(replace: replace);
            return mocker.AddTaskOrchestrationReplaySafeLogging(mocker.GetRequiredObject<ILoggerFactory>(), isReplaying, replace);
        }

        /// <summary>
        /// Configures tracked <see cref="TaskOrchestrationContext" /> mocks to use replay-safe logger creation with the supplied logger factory.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="loggerFactory">The logger factory that replay-safe logger creation should delegate to.</param>
        /// <param name="isReplaying">True to suppress replay-safe logger output, false to allow normal log capture.</param>
        /// <param name="replace">True to replace an existing orchestration helper registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddTaskOrchestrationReplaySafeLogging(this Mocker mocker, ILoggerFactory loggerFactory, bool isReplaying = false, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            mocker.AddLoggerFactory(loggerFactory, replace);

            if (mocker.Contains(typeof(TaskOrchestrationContext)))
            {
                var trackedContext = mocker.GetOrCreateMock<TaskOrchestrationContext>();
                if (!TryConfigureTaskOrchestrationReplaySafeLogging(trackedContext, loggerFactory, isReplaying))
                {
                    throw new NotSupportedException("AddTaskOrchestrationReplaySafeLogging(...) must run before resolving TaskOrchestrationContext when the active provider cannot configure the protected Durable logger factory getter through FastMoq's tracked-property configuration contract. Call the helper before GetObject<TaskOrchestrationContext>() when the selected provider does not support that tracked-mock path.");
                }
                return mocker;
            }

            mocker.AddType<TaskOrchestrationContext>(new ReplaySafeLoggerTaskOrchestrationContext(loggerFactory, isReplaying), replace);

            return mocker;
        }

        private static bool TryConfigureTaskOrchestrationReplaySafeLogging(IFastMock fastMock, ILoggerFactory loggerFactory, bool isReplaying)
        {
            if (!MockPropertyConfigurationHelper.TryConfigureNativeMockProperty(fastMock, "LoggerFactory", loggerFactory, includeNonPublic: true))
            {
                return false;
            }

            if (!isReplaying)
            {
                return true;
            }

            return MockPropertyConfigurationHelper.TryConfigureNativeMockProperty(fastMock, nameof(TaskOrchestrationContext.IsReplaying), true, includeNonPublic: true);
        }

        private sealed class ReplaySafeLoggerTaskOrchestrationContext(ILoggerFactory loggerFactory, bool isReplaying) : TaskOrchestrationContext
        {
            private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            private readonly TaskName _name = new("FastMoq.Orchestration");
            private readonly string _instanceId = Guid.NewGuid().ToString("N");
            private object? _customStatus;

            public override TaskName Name => _name;

            public override string InstanceId => _instanceId;

            public override ParentOrchestrationInstance? Parent => null;

            public override DateTime CurrentUtcDateTime => DateTime.UtcNow;

            public override bool IsReplaying => isReplaying;

            protected override ILoggerFactory LoggerFactory => _loggerFactory;

            public override T GetInput<T>()
            {
                throw new NotSupportedException("FastMoq's replay-safe orchestration helper only supports logger creation and replay-state assertions.");
            }

            public override Task<TResult> CallActivityAsync<TResult>(TaskName name, object? input = null, TaskOptions? options = null)
            {
                throw new NotSupportedException("FastMoq's replay-safe orchestration helper does not execute Durable activities.");
            }

            public override Task CreateTimer(DateTime fireAt, CancellationToken cancellationToken)
            {
                throw new NotSupportedException("FastMoq's replay-safe orchestration helper does not execute Durable timers.");
            }

            public override Task<T> WaitForExternalEvent<T>(string eventName, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException("FastMoq's replay-safe orchestration helper does not wait for Durable external events.");
            }

            public override void SendEvent(string instanceId, string eventName, object? payload)
            {
                throw new NotSupportedException("FastMoq's replay-safe orchestration helper does not send Durable events.");
            }

            public override void SetCustomStatus(object? customStatus)
            {
                _customStatus = customStatus;
            }

            public override Task<TResult> CallSubOrchestratorAsync<TResult>(TaskName orchestratorName, object? input = null, TaskOptions? options = null)
            {
                throw new NotSupportedException("FastMoq's replay-safe orchestration helper does not execute sub-orchestrators.");
            }

            public override void ContinueAsNew(object? newInput = null, bool preserveUnprocessedEvents = true)
            {
                throw new NotSupportedException("FastMoq's replay-safe orchestration helper does not continue orchestrations as new.");
            }

            public override Guid NewGuid()
            {
                return Guid.NewGuid();
            }
        }
    }
}
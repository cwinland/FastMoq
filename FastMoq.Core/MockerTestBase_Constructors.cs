using Microsoft.Extensions.Logging;

namespace FastMoq
{
    /// <inheritdoc />
    public partial class MockerTestBase<TComponent> where TComponent : class
    {
        #region Properties

        internal static Action<object> DefaultAction => _ => { };

        /// <summary>
        /// Controls how the component under test is created by the default constructor path.
        /// Override this to opt into alternate constructor-selection or optional-parameter behavior for the component.
        /// </summary>
        protected virtual InstanceCreationFlags ComponentCreationFlags => InstanceCreationFlags.None;

        private Func<Mocker, TComponent> DefaultCreateAction =>
            _ => Component = Mocks.CreateInstance<TComponent>(ComponentCreationFlags) ?? throw CannotCreateComponentException;

        #endregion

        /// <inheritdoc />
        /// <summary>
        /// Create instance with default actions and no extra setup. See other constructors to set Mocks before instantiation of the Component.
        /// </summary>
        protected MockerTestBase() : this(DefaultAction, null, DefaultAction) { }

        /// <inheritdoc />
        protected MockerTestBase(Action<Mocker> setupMocksAction) : this(setupMocksAction, null, DefaultAction) { }

        /// <inheritdoc />
        protected MockerTestBase(Action<Mocker> setupMocksAction, Func<Mocker, TComponent> createComponentAction)
            : this(setupMocksAction, createComponentAction, DefaultAction) { }

        /// <inheritdoc />
        protected MockerTestBase(Action<Mocker> setupMocksAction, Action<TComponent> createdComponentAction)
            : this(setupMocksAction, null, createdComponentAction) { }

        /// <inheritdoc />
        protected MockerTestBase(Func<Mocker, TComponent> createComponentAction)
            : this(DefaultAction, createComponentAction, DefaultAction) { }

        /// <inheritdoc />
        protected MockerTestBase(Func<Mocker, TComponent> createComponentAction,
            Action<TComponent> createdComponentAction)
            : this(DefaultAction, createComponentAction, createdComponentAction) { }

        /// <inheritdoc />
        protected MockerTestBase(Action<TComponent> createdComponentAction)
            : this(DefaultAction, null, createdComponentAction) { }

        /// <inheritdoc />
        /// <summary>
        /// Create instance and setting mock resolution. Mock resolution is on by default.
        /// When it is off, it may not be able to fill in properties or other injections of components.
        /// </summary>
        protected MockerTestBase(bool innerMockResolution) : this() => Mocks.InnerMockResolution = innerMockResolution;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockerTestBase{TComponent}" /> class with explicit hooks for arranging mocks, overriding construction, and applying post-creation setup.
        /// </summary>
        /// <param name="setupMocksAction">The action to set up the mocks before component creation.</param>
        /// <param name="createComponentAction">The action to override component creation.</param>
        /// <param name="createdComponentAction">The action to do after the component is created.</param>
        /// <example>
        /// <para>Use this overload when the test base needs full control over the arrange, create, and post-create phases.</para>
        /// <para><c>GetOrCreateMock</c> returns a provider-backed tracked mock handle. It works with the active FastMoq provider, but chaining Moq-specific setup APIs such as <c>Setup(...)</c> requires the Moq provider to be selected for the test assembly.</para>
        /// <code language="csharp"><![CDATA[
        /// public sealed class InvoiceServiceTests : MockerTestBase<InvoiceService>
        /// {
        ///     public InvoiceServiceTests()
        ///         : base(
        ///             setupMocksAction: mocker =>
        ///             {
        ///                 mocker.GetMock<IExchangeRateClient>()
        ///                     .Setup(x => x.GetRate("USD", "EUR"))
        ///                     .Returns(0.92m);
        ///             },
        ///             createComponentAction: mocker =>
        ///                 new InvoiceService(mocker.GetObject<IExchangeRateClient>(), "EUR"),
        ///             createdComponentAction: component =>
        ///                 component.Region = "EMEA")
        ///     {
        ///     }
        /// }
        /// ]]></code>
        /// <para>Use <c>GetOrCreateMock</c> when you need the tracked mock handle itself, for example to pass <c>Instance</c> into custom construction without relying on automatic resolution, to reuse the same tracked mock across calls, or to use keyed <see cref="MockRequestOptions"/>. Use <c>GetMock</c> only for the legacy Moq-specific setup path.</para>
        /// </example>
        protected MockerTestBase(Action<Mocker> setupMocksAction,
            Func<Mocker, TComponent>? createComponentAction,
            Action<TComponent> createdComponentAction)
        {
            Mocks = new Mocker((logLevel, eventId, message, exception) => LoggingCallback(logLevel, eventId, message, exception));
            ConfigureMockerPolicy?.Invoke(Mocks.Policy);

            SetupMocksAction = setupMocksAction;
            CreateComponentAction = createComponentAction ?? DefaultCreateAction;
            CreatedComponentAction = createdComponentAction;
            Component = GetComponent();
        }

        /// <summary>
        /// Receives log entries emitted through the current <see cref="Mocker"/> instance.
        /// Override this in a derived test base when log output should be captured or redirected.
        /// </summary>
        /// <param name="logLevel">The log severity.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="message">The formatted log message.</param>
        public virtual void LoggingCallback(LogLevel logLevel, EventId eventId, string message)
        {
            Console.WriteLine($"LogLevel: {logLevel}, EventId: {eventId}, Message: {message}");
        }

        /// <summary>
        /// Receives log entries and any attached exception emitted through the current <see cref="Mocker"/> instance.
        /// Override this overload when tests need exception-aware log capture.
        /// </summary>
        /// <param name="logLevel">The log severity.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="message">The formatted log message.</param>
        /// <param name="exception">The exception associated with the log entry, if any.</param>
        public virtual void LoggingCallback(LogLevel logLevel, EventId eventId, string message, Exception? exception)
        {
            LoggingCallback(logLevel, eventId, message);
        }

        /// <inheritdoc />
        /// <summary>Create the component using the constructor whose parameter types match <paramref name="createArgumentTypes"/>.</summary>
        /// <example>
        /// <para>Use this overload when the component exposes multiple constructors and the test should target a specific signature.</para>
        /// <code language="csharp"><![CDATA[
        /// public sealed class ReportExporterTests : MockerTestBase<ReportExporter>
        /// {
        ///     public ReportExporterTests()
        ///         : base(typeof(string), typeof(int))
        ///     {
        ///     }
        /// }
        ///
        /// // FastMoq will choose a constructor such as:
        /// // ReportExporter(IStorageClient storageClient, string containerName, int retryCount)
        /// ]]></code>
        /// </example>
        protected MockerTestBase(params Type[] createArgumentTypes)
            : this(DefaultAction, CreateActionWithTypes(createArgumentTypes), DefaultAction) { }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="MockerTestBase{TComponent}" /> class while combining mock setup, post-creation configuration, and explicit constructor selection.
        /// </summary>
        /// <param name="setupMocksAction">The setup mocks action.</param>
        /// <param name="createArgumentTypes">Create component using constructor with matching types.</param>
        /// <param name="createdComponentAction">The created component action.</param>
        /// <example>
        /// <para>This overload is useful when a service has several constructors and the selected one needs additional state configured after creation.</para>
        /// <para>If the test uses Moq-only setup methods such as <c>Setup(...)</c> or <c>ReturnsAsync(...)</c>, use the legacy Moq path intentionally and ensure the Moq provider is selected for that test assembly.</para>
        /// <code language="csharp"><![CDATA[
        /// public sealed class OrdersImportTests : MockerTestBase<OrdersImporter>
        /// {
        ///     public OrdersImportTests()
        ///         : base(
        ///             setupMocksAction: mocker =>
        ///             {
        ///                 mocker.GetMock<IOrdersApi>()
        ///                     .Setup(x => x.FetchPageAsync(1))
        ///                     .ReturnsAsync(new OrdersPage());
        ///             },
        ///             createdComponentAction: component =>
        ///                 component.BatchName = "nightly-import",
        ///             createArgumentTypes: typeof(string))
        ///     {
        ///     }
        /// }
        /// ]]></code>
        /// <para>Use <c>GetOrCreateMock</c> in constructor-focused tests only when you need the returned tracked mock object itself, such as keyed identity, provider options, resetting the tracked mock, or passing <c>Instance</c> into custom construction.</para>
        /// </example>
        protected MockerTestBase(Action<Mocker> setupMocksAction,
            Action<TComponent> createdComponentAction,
            params Type[] createArgumentTypes)
            : this(setupMocksAction, CreateActionWithTypes(createArgumentTypes), createdComponentAction) { }

        private static Func<Mocker, TComponent> CreateActionWithTypes(params Type[] args) =>
            m => m.CreateInstanceByType<TComponent>(args) ?? throw CannotCreateComponentException;
    }
}

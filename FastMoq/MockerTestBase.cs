namespace FastMoq
{
    /// <summary>
    ///     Auto Mocking Test Base with Fast Mocking <see cref="Mocker" />.
    /// </summary>
    /// <remarks>
    ///     Use the Mocks object on the test class to access fast mocking features.
    /// </remarks>
    /// <typeparam name="TComponent">The type of the t component.</typeparam>
    public abstract class MockerTestBase<TComponent> where TComponent : class
    {
        #region Properties

        /// <summary>
        ///     Gets or sets the custom mocks.
        /// </summary>
        /// <value>The custom mocks.</value>
        protected IEnumerable<MockModel> CustomMocks { get; set; } = new List<MockModel>();

        /// <summary>
        ///     Gets or sets the create component action.
        /// </summary>
        /// <value>The create component action.</value>
        protected Func<Mocker, TComponent?> CreateComponentAction { get; set; }

        /// <summary>
        ///     Gets or sets the setup mocks action.
        /// </summary>
        /// <value>The setup mocks action.</value>
        protected Action<Mocker>? SetupMocksAction { get; set; }

        /// <summary>
        ///     Gets or sets the created component action.
        /// </summary>
        /// <value>The created component action.</value>
        protected Action<TComponent?>? CreatedComponentAction { get; set; }

        private Func<Mocker, TComponent?> DefaultCreateAction => _ => Component = Mocks.CreateInstance<TComponent>();

        /// <summary>
        ///     Gets the mocks.
        /// </summary>
        /// <value>The mocks.</value>
        protected Mocker Mocks { get; } = new();

        /// <summary>
        ///     Gets or sets the service.
        /// </summary>
        /// <value>The service.</value>
        protected internal TComponent? Component { get; set; }

        #endregion

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="MockerTestBase{TComponent}" /> class with the default createAction.
        /// </summary>
        protected MockerTestBase() : this(null, null, null) { }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="MockerTestBase{TComponent}" /> class with a setup action.
        /// </summary>
        /// <param name="setupMocksAction">The setup mocks action.</param>
        protected MockerTestBase(Action<Mocker> setupMocksAction) : this(setupMocksAction, null, null) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockerTestBase{TComponent}" /> class.
        /// </summary>
        /// <param name="setupMocksAction">The setup mocks action.</param>
        /// <param name="createComponentAction">The create component action.</param>
        protected MockerTestBase(Action<Mocker> setupMocksAction, Func<Mocker, TComponent> createComponentAction)
            : this(setupMocksAction, createComponentAction, null) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockerTestBase{TComponent}" /> class.
        /// </summary>
        /// <param name="setupMocksAction">The setup mocks action.</param>
        /// <param name="createdComponentAction">The created component action.</param>
        protected MockerTestBase(Action<Mocker>? setupMocksAction, Action<TComponent?>? createdComponentAction = null)
            : this(setupMocksAction, null, createdComponentAction) { }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="MockerTestBase{TComponent}" /> class with a create action and optional
        ///     createdAction.
        /// </summary>
        /// <param name="createComponentAction">The create component action.</param>
        /// <param name="createdComponentAction">The created component action.</param>
        protected MockerTestBase(Func<Mocker, TComponent> createComponentAction,
            Action<TComponent?>? createdComponentAction = null)
            : this(null, createComponentAction, createdComponentAction) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockerTestBase{TComponent}" /> class.
        /// </summary>
        /// <param name="setupMocksAction">The setup mocks action.</param>
        /// <param name="createComponentAction">The create component action.</param>
        /// <param name="createdComponentAction">The created component action.</param>
        protected MockerTestBase(Action<Mocker>? setupMocksAction,
            Func<Mocker, TComponent?>? createComponentAction,
            Action<TComponent?>? createdComponentAction = null)
        {
            SetupMocksAction = setupMocksAction;
            CreateComponentAction = createComponentAction ?? DefaultCreateAction;
            CreatedComponentAction = createdComponentAction;
            CreateComponent();
        }

        /// <summary>
        ///     Creates the specified custom object.
        /// </summary>
        protected void CreateComponent()
        {
            CustomMocks ??= new List<MockModel>();
            foreach (var customMock in CustomMocks)
            {
                Mocks.AddMock(customMock.Mock, customMock.Type);
            }

            SetupMocksAction?.Invoke(Mocks);
            Component = CreateComponentAction?.Invoke(Mocks);
            CreatedComponentAction?.Invoke(Component);
        }
    }
}
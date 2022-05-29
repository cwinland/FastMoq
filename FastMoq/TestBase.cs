namespace FastMoq
{
    /// <summary>
    ///     Class TestBase.
    /// </summary>
    /// <typeparam name="TComponent">The type of the t component.</typeparam>
    public abstract class TestBase<TComponent> where TComponent : class
    {
        #region Properties

        private Func<Mocks, TComponent> DefaultCreateAction => _ => Component = Mocks.CreateInstance<TComponent>();

        /// <summary>
        ///     Gets the mocks.
        /// </summary>
        /// <value>The mocks.</value>
        protected Mocks Mocks { get; } = new();

        /// <summary>
        ///     Gets or sets the service.
        /// </summary>
        /// <value>The service.</value>
        protected internal TComponent? Component { get; set; }

        #endregion

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.TestBase`1" /> class with the default createAction.
        /// </summary>
        protected TestBase() : this(null, null, null)
        {
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.TestBase`1" /> class with a setup action.
        /// </summary>
        /// <param name="setupMocksAction">The setup mocks action.</param>
        protected TestBase(Action<Mocks> setupMocksAction) : this(setupMocksAction, null)
        {
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.TestBase`1" /> class with a create action and optional
        ///     createdAction.
        /// </summary>
        /// <param name="createComponentAction">The create component action.</param>
        /// <param name="createdComponentAction">The created component action.</param>
        protected TestBase(Func<Mocks, TComponent> createComponentAction, Action<TComponent?>? createdComponentAction = null) : this(
            null,
            createComponentAction,
            createdComponentAction
        )
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TestBase{TComponent}" /> class.
        /// </summary>
        /// <param name="setupMocksAction">The setup mocks action.</param>
        /// <param name="createComponentAction">The create component action.</param>
        /// <param name="createdComponentAction">The created component action.</param>
        protected TestBase(Action<Mocks>? setupMocksAction, Func<Mocks, TComponent>? createComponentAction,
            Action<TComponent?>? createdComponentAction = null)
        {
            createComponentAction ??= DefaultCreateAction;
            setupMocksAction?.Invoke(Mocks);
            Component = createComponentAction.Invoke(Mocks);
            createdComponentAction?.Invoke(Component);
        }
    }
}

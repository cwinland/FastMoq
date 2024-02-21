namespace FastMoq
{
    /// <inheritdoc />
    public partial class MockerTestBase<TComponent> where TComponent : class
    {
        private static Action<object> DefaultAction => _ => { };

        private Func<Mocker, TComponent> DefaultCreateAction => _ => Component = Mocks.CreateInstance<TComponent>() ?? throw CannotCreateComponentException;

        private static Func<Mocker, TComponent> CreateActionWithTypes(params Type?[] args) => m => m.CreateInstanceByType<TComponent>(false, args) ?? throw CannotCreateComponentException;

        /// <inheritdoc />
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
        protected MockerTestBase(bool innerMockResolution) : this() => Mocks.InnerMockResolution = innerMockResolution;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockerTestBase{TComponent}" /> class.
        /// </summary>
        /// <param name="setupMocksAction">The action to set up the mocks before component creation.</param>
        /// <param name="createComponentAction">The action to override component creation.</param>
        /// <param name="createdComponentAction">The action to do after the component is created.</param>
        protected MockerTestBase(Action<Mocker> setupMocksAction,
            Func<Mocker, TComponent>? createComponentAction,
            Action<TComponent> createdComponentAction)
        {
            SetupMocksAction = setupMocksAction;
            CreateComponentAction = createComponentAction ?? DefaultCreateAction;
            CreatedComponentAction = createdComponentAction;
            Component = GetComponent();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockerTestBase{TComponent}" /> class.
        /// </summary>
        /// <param name="useCreateActionWithTypes">if set to <c>true</c> [use create action with types].</param>
        /// <param name="setupMocksAction">The setup mocks action.</param>
        /// <param name="createComponentTypes">The create component types.</param>
        /// <param name="createdComponentAction">The created component action.</param>
        protected MockerTestBase(bool useCreateActionWithTypes, Action<Mocker> setupMocksAction,
                Type[]? createComponentTypes,
                Action<TComponent> createdComponentAction)
            : this(setupMocksAction, (useCreateActionWithTypes && createComponentTypes != null ? CreateActionWithTypes(createComponentTypes) : null), createdComponentAction) { }
    }
}

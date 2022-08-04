namespace FastMoq
{
    /// <summary>
    ///     Auto Mocking Test Base with Fast Automatic Mocking <see cref="Mocker" />.
    /// </summary>
    /// <example>
    /// Basic example of the base class creating the Car class and auto mocking ICarService.
    /// <code><![CDATA[
    ///public class CarTest : MockerTestBase<Car> {
    ///     [Fact]
    ///     public void TestCar() {
    ///         Component.Color.Should().Be(Color.Green);
    ///         Component.CarService.Should().NotBeNull();
    ///     }
    ///}
    ///
    ///public class Car {
    ///     public Color Color { get; set; } = Color.Green;
    ///     public ICarService CarService { get; }
    ///     public Car(ICarService carService) => CarService = carService;
    ///}
    ///
    ///public interface ICarService
    ///{
    ///     Color Color { get; set; }
    ///     ICarService CarService { get; }
    ///     bool StartCar();
    ///}
    /// ]]>
    /// </code>
    ///
    /// Example of how to set up for mocks that require specific functionality.
    /// <code><![CDATA[
    ///public class CarTest : MockerTestBase<Car> {
    ///     public CarTest() : base(mocks => {
    ///             mocks.Initialize<ICarService>(mock => mock.Setup(x => x.StartCar).Returns(true));
    ///     }
    ///}
    /// ]]>
    /// </code>
    /// </example>
    /// <typeparam name="TComponent">The type of the t component.</typeparam>
    public abstract class MockerTestBase<TComponent> where TComponent : class
    {
        #region Properties

        /// <summary>
        ///     Gets or sets the custom mocks. These are added whenever the component is created.
        /// </summary>
        /// <value>The custom mocks.</value>
        protected IEnumerable<MockModel> CustomMocks { get; set; } = new List<MockModel>();

        /// <summary>
        ///     Gets or sets the create component action. This action is run whenever the component is created.
        /// </summary>
        /// <value>The create component action.</value>
        protected virtual Func<Mocker, TComponent?> CreateComponentAction { get; set; }

        /// <summary>
        ///     Gets or sets the setup mocks action. This action is run before the component is created.
        /// </summary>
        /// <value>The setup mocks action.</value>
        protected virtual Action<Mocker>? SetupMocksAction { get; set; }

        /// <summary>
        ///     Gets or sets the created component action. This action is run after the component is created.
        /// </summary>
        /// <value>The created component action.</value>
        protected virtual Action<TComponent?>? CreatedComponentAction { get; set; }

        private Func<Mocker, TComponent?> DefaultCreateAction => _ => Component = Mocks.CreateInstance<TComponent>();

        /// <summary>
        ///     Gets the <see cref="Mocker"/>.
        /// </summary>
        /// <value>The mocks.</value>
        protected Mocker Mocks { get; } = new();

        /// <summary>
        ///     Gets or sets the component under test.
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
        ///     Sets the <see cref="Component" /> property with a new instance while maintaining the constructor setup and any other changes.
        /// </summary>
        /// <example>
        /// CreateComponent allows creating the component when desired, instead of in the base class constructor.
        /// <code><![CDATA[
        /// public void Test() {
        ///     Mocks.Initialize<ICarService>(mock => mock.Setup(x => x.StartCar).Returns(true));
        ///     CreateComponent();
        /// }
        /// ]]>
        /// </code>
        /// </example>
        protected void CreateComponent()
        {
            CustomMocks ??= new List<MockModel>();
            foreach (var customMock in CustomMocks)
            {
                Mocks.AddMock(customMock.Mock, customMock.Type, true);
            }

            SetupMocksAction?.Invoke(Mocks);
            Component = CreateComponentAction?.Invoke(Mocks);
            CreatedComponentAction?.Invoke(Component);
        }

        /// <summary>
        ///     Waits for an action.
        /// </summary>
        /// <typeparam name="T">Logic of T.</typeparam>
        /// <param name="logic">The action.</param>
        /// <param name="timespan">The maximum time to wait.</param>
        /// <param name="waitBetweenChecks">Time between each check.</param>
        /// <returns>T.</returns>
        /// <exception cref="System.ArgumentNullException">logic</exception>
        public static T WaitFor<T>(Func<T> logic, TimeSpan timespan, TimeSpan waitBetweenChecks)
        {
            if (logic == null)
            {
                throw new ArgumentNullException(nameof(logic));
            }

            var result = logic();
            var timeout = DateTimeOffset.Now.Add(timespan);

            while (EqualityComparer<T>.Default.Equals(result, default) && DateTimeOffset.Now <= timeout)
            {
                result = logic();
                Thread.Sleep(waitBetweenChecks);
            }

            return result;
        }
        /// <summary>
        ///     Waits for an action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logic">The action.</param>
        /// <returns>T.</returns>
        /// <exception cref="System.ArgumentNullException">logic</exception>
        public static T WaitFor<T>(Func<T> logic)
        {
            return WaitFor(logic, TimeSpan.FromSeconds(4));
        }

        /// <summary>
        ///     Waits for an action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logic">The action.</param>
        /// <param name="timespan">The timespan, defaults to 4 seconds.</param>
        /// <returns>T.</returns>
        /// <exception cref="System.ArgumentNullException">logic</exception>
        public static T WaitFor<T>(Func<T> logic, TimeSpan timespan)
        {
            return WaitFor(logic, timespan, TimeSpan.FromMilliseconds(100));
        }
    }
}

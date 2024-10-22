using FastMoq.Extensions;
using FastMoq.Models;
using System.Diagnostics.CodeAnalysis;

namespace FastMoq
{
    /// <summary>
    ///     Mock Test Base with Automatic Mocking using <see cref="Mocker" />.
    ///     This class contains the <see cref="Mocks"/> property to create and track all Mocks from creation of the component.
    /// </summary>
    /// <example>
    ///     Test example of the base class creating the Car class and auto mocking ICarService.
    ///     Start by creating the test class that inherits <see cref="MockerTestBase{TComponent}"/>. The object that is passed is the concrete class being tested.
    ///     Use <see cref="Component" /> Property to access the test object.
    ///     <code>
    /// <![CDATA[public class CarTest : MockerTestBase<Car> {]]>
    ///      public void TestCar() {
    ///          Component.CarService.Should().NotBeNull();
    ///      }
    /// </code>
    /// ICarService is automatically injected into the Car object when created by the base class.
    /// <code>
    /// public partial class Car {
    ///      public Car(ICarService carService) => CarService = carService;
    /// }
    ///  </code>
    ///     When needing to set up mocks before the component is created, use the <see cref="get_SetupMocksAction"/> property or the base class constructor.
    ///     This example shows creating a database context and adding it to the Type Map for resolution during creation of objects.
    ///     <code>
    /// <![CDATA[protected override Action<Mocker>]]> SetupMocksAction => mocker =>
    ///     mocker.AddType(_ => <![CDATA[mocker.GetMockDbContext<ApplicationDbContext>().Object);]]>
    ///  </code>
    /// </example>
    /// <typeparam name="TComponent">The type of the t component.</typeparam>
    /// <inheritdoc />
    public abstract partial class MockerTestBase<TComponent> : IDisposable where TComponent : class
    {
        #region Fields

        private bool disposedValue;
        private TComponent component;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the component under test.
        /// </summary>
        /// <value>The service.</value>
        protected internal TComponent Component
        {
            get => component.RaiseIfNull();
            set => component  = value.RaiseIfNull();
        }

        /// <summary>
        ///     Gets or sets the custom mocks. These are added whenever the component is created.
        /// </summary>
        /// <value>The custom mocks.</value>
        protected IEnumerable<MockModel> CustomMocks { get; set; } = new List<MockModel>();

        /// <summary>
        ///     Gets or sets the creation component action. This action is run whenever the component is created.
        /// </summary>
        /// <value>The action to override the component creation.</value>
        protected virtual Func<Mocker, TComponent> CreateComponentAction { get; }

        /// <summary>
        ///     Gets or sets the setup mocks action. This action is run before the component is created.
        /// </summary>
        /// <value>The setup mocks action.</value>
        protected virtual Action<Mocker>? SetupMocksAction { get; }

        /// <summary>
        ///     Gets or sets the created component action. This action is run after the component is created.
        /// </summary>
        /// <value>The created component action.</value>
        protected virtual Action<TComponent>? CreatedComponentAction { get; }

        /// <summary>
        ///     Gets the <see cref="Mocker" />.
        /// </summary>
        /// <value>The mocks.</value>
        protected Mocker Mocks { get; } = new();

        #endregion

        /// <summary>
        ///     Waits for an action.
        /// </summary>
        /// <typeparam name="T">Logic of T.</typeparam>
        /// <param name="logic">The action.</param>
        /// <param name="timespan">The maximum time to wait.</param>
        /// <param name="waitBetweenChecks">Time between each check.</param>
        /// <returns>T.</returns>
        /// <exception cref="System.ArgumentNullException">logic</exception>
        /// <exception cref="System.ApplicationException">Timeout waiting for condition</exception>
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

            return !EqualityComparer<T>.Default.Equals(result, default) && DateTimeOffset.Now > timeout
                ? throw new ApplicationException("Timeout waiting for condition")
                : result;
        }

        /// <summary>
        ///     Waits for an action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logic">The action.</param>
        /// <returns>T.</returns>
        /// <exception cref="System.ArgumentNullException">logic</exception>
        public static T WaitFor<T>(Func<T> logic) => WaitFor(logic, TimeSpan.FromSeconds(4));

        /// <summary>
        ///     Waits for an action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logic">The action.</param>
        /// <param name="timespan">The timespan, defaults to 4 seconds.</param>
        /// <returns>T.</returns>
        /// <exception cref="System.ArgumentNullException">logic</exception>
        public static T WaitFor<T>(Func<T> logic, TimeSpan timespan) => WaitFor(logic, timespan, TimeSpan.FromMilliseconds(100));

        /// <summary>
        ///     Sets the <see cref="Component" /> property with a new instance while maintaining the constructor setup and any
        /// other changes.
        /// </summary>
        /// <example>
        /// CreateComponent allows creating the component when desired, instead of in the base class constructor.
        /// <code><![CDATA[
        /// public void Test() {
        ///     Mocks.Initialize<ICarService>(mock => mock.Setup(x => x.StartCar).Returns(true));
        ///     CreateComponent();
        /// }
        /// ]]></code></example>
        protected void CreateComponent()
        {
            GetComponent();
        }

        [return: NotNull]
        private TComponent GetComponent()
        {
            foreach (var customMock in CustomMocks)
            {
                Mocks.AddMock(customMock.Mock, customMock.Type, true);
            }

            SetupMocksAction?.Invoke(Mocks);
            Component = CreateComponentAction.Invoke(Mocks) ?? throw CannotCreateComponentException;
            CreatedComponentAction?.Invoke(Component);

            return Component ?? throw CannotCreateComponentException;
        }

        private static InvalidProgramException CannotCreateComponentException => new("Cannot create component");

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
        /// unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                disposedValue = true;
            }
        }

        #region IDisposable

        /// <inheritdoc />
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
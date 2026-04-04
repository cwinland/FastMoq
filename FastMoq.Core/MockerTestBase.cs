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
    /// <para>Use <see cref="MockerTestBase{TComponent}"/> when you want the component under test created automatically with constructor dependencies resolved from FastMoq.</para>
    /// <code language="csharp"><![CDATA[
    /// public class CheckoutServiceTests : MockerTestBase<CheckoutService>
    /// {
    ///     protected override Action<Mocker> SetupMocksAction => mocker =>
    ///     {
    ///         mocker.GetMock<IPricingClient>()
    ///             .Setup(x => x.GetPrice("SKU-42"))
    ///             .Returns(125.50m);
    ///
    ///         mocker.AddType<IClock>(new FixedClock(new DateTimeOffset(2026, 4, 4, 12, 0, 0, TimeSpan.Zero)));
    ///     };
    ///
    ///     [Fact]
    ///     public void Calculates_total_using_auto_injected_dependencies()
    ///     {
    ///         var total = Component.CalculateTotal("SKU-42", quantity: 2);
    ///
    ///         total.Should().Be(251.00m);
    ///         Mocks.GetMock<IPricingClient>().Verify(x => x.GetPrice("SKU-42"), Times.Once);
    ///     }
    /// }
    /// ]]></code>
    /// <para>The base class creates <c>CheckoutService</c> before the test body runs, so the test can assert directly against <see cref="Component"/>.</para>
    /// <code language="csharp"><![CDATA[
    /// public sealed class CheckoutService
    /// {
    ///     public CheckoutService(IPricingClient pricingClient, IClock clock)
    ///     {
    ///         _pricingClient = pricingClient;
    ///         _clock = clock;
    ///     }
    /// }
    /// ]]></code>
    /// <para>Use <see cref="CreatedComponentAction"/> when you need a final arrangement step after construction, such as setting mutable state or attaching events.</para>
    /// <code language="csharp"><![CDATA[
    /// protected override Action<CheckoutService> CreatedComponentAction => component =>
    ///     component.Currency = "USD";
    /// ]]></code>
    /// </example>
    /// <typeparam name="TComponent">The type of the t component.</typeparam>
    /// <inheritdoc />
    public abstract partial class MockerTestBase<TComponent> : IDisposable where TComponent : class
    {
        #region Fields

        private bool disposedValue;
        // ReSharper disable once ReplaceWithFieldKeyword
        private TComponent? component;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the component under test.
        /// </summary>
        /// <value>The service.</value>
        protected internal TComponent Component
        {
            get => component.RaiseIfNull();
            set => component = value.RaiseIfNull();
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
        ///     Allows a test base to configure grouped <see cref="Mocker"/> policy defaults before the component is created.
        /// </summary>
        protected virtual Action<MockerPolicyOptions>? ConfigureMockerPolicy { get; }

        /// <summary>
        ///     Gets the <see cref="Mocker" />.
        /// </summary>
        /// <value>The mocks.</value>
        protected Mocker Mocks { get; }

        /// <summary>
        /// Gets a fluent scenario builder for the current component.
        /// </summary>
        protected ScenarioBuilder<TComponent> Scenario => Mocks.Scenario(Component);

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
        /// Sets the <see cref="Component" /> property with a new instance while maintaining the constructor setup and any other changes.
        /// </summary>
        /// <example>
        /// <para>Use <see cref="CreateComponent"/> when the test needs to change mock behavior first and then rebuild the component with the updated arrangement.</para>
        /// <code language="csharp"><![CDATA[
        /// [Fact]
        /// public void Recreates_component_after_overriding_a_dependency()
        /// {
        ///     Mocks.GetMock<IPricingClient>()
        ///         .Setup(x => x.GetPrice("SKU-42"))
        ///         .Returns(99.00m);
        ///
        ///     CreateComponent();
        ///
        ///     Component.CalculateTotal("SKU-42", 1).Should().Be(99.00m);
        /// }
        /// ]]></code>
        /// </example>
        protected void CreateComponent()
        {
            GetComponent();
        }

        [return: NotNull]
        private TComponent GetComponent()
        {
            foreach (var customMock in CustomMocks)
            {
                Mocks.AddFastMock(customMock.FastMock, customMock.Type, true, customMock.NonPublic);
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
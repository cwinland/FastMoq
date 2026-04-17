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
    /// <para>Use <see cref="MockerTestBase{TComponent}"/> when you want the component under test created automatically with constructor dependencies resolved from FastMoq's provider-first pipeline.</para>
    /// <para>The base class auto-resolves constructor dependencies, so you do not need to call <c>GetOrCreateMock</c> just to make a dependency exist before verification. When you do need the tracked mock handle itself, use <see cref="Mocker.GetOrCreateMock{T}(MockRequestOptions?)"/> in a custom create path.</para>
    /// <code language="csharp"><![CDATA[
    /// public sealed class OrderSubmitterTests : MockerTestBase<OrderSubmitter>
    /// {
    ///     [Fact]
    ///     public void Submit_should_publish_the_order_id()
    ///     {
    ///         Component.Submit(42);
    ///
    ///         Component.LastSubmittedOrderId.Should().Be(42);
    ///         Mocks.Verify<IOrderGateway>(x => x.Publish(42), TimesSpec.Once);
    ///     }
    /// }
    /// ]]></code>
    /// <para>The base class creates <c>OrderSubmitter</c> before the test body runs, so the test can assert directly against <see cref="Component"/>.</para>
    /// <para>When a component exposes multiple constructors and the test should target one specific signature, prefer overriding <see cref="MockerTestBase{TComponent}.ComponentConstructorParameterTypes"/> or <see cref="CreateComponentAction"/> in the test base instead of modifying production constructors just for testing.</para>
    /// <code language="csharp"><![CDATA[
    /// public interface IOrderGateway
    /// {
    ///     void Publish(int orderId);
    /// }
    ///
    /// public sealed class OrderSubmitter
    /// {
    ///     private readonly IOrderGateway _gateway;
    ///
    ///     public OrderSubmitter(IOrderGateway gateway) => _gateway = gateway;
    ///
    ///     public int? LastSubmittedOrderId { get; private set; }
    ///     public string SubmissionChannel { get; set; } = "default";
    ///
    ///     public void Submit(int orderId)
    ///     {
    ///         LastSubmittedOrderId = orderId;
    ///         _gateway.Publish(orderId);
    ///     }
    /// }
    /// ]]></code>
    /// <para>Use <see cref="CreatedComponentAction"/> when you need a final arrangement step after construction, such as setting mutable state that is part of the test context.</para>
    /// <code language="csharp"><![CDATA[
    /// protected override Action<OrderSubmitter> CreatedComponentAction => component =>
    ///     component.SubmissionChannel = "priority";
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
        /// Waits until <paramref name="logic" /> returns a value other than <c>default(T)</c>.
        /// </summary>
        /// <typeparam name="T">The result type produced by the polling logic.</typeparam>
        /// <param name="logic">The polling function to evaluate.</param>
        /// <param name="timespan">The maximum time to wait for a non-default result.</param>
        /// <param name="waitBetweenChecks">The delay between polling attempts while the result remains <c>default(T)</c>.</param>
        /// <returns>The first non-default value returned by <paramref name="logic" />.</returns>
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
                if (!EqualityComparer<T>.Default.Equals(result, default))
                {
                    break;
                }

                Thread.Sleep(waitBetweenChecks);
            }

            if (EqualityComparer<T>.Default.Equals(result, default))
            {
                throw new ApplicationException("Timeout waiting for condition");
            }

            return result;
        }

        /// <summary>
        /// Waits until <paramref name="logic" /> returns a value other than <c>default(T)</c>.
        /// </summary>
        /// <typeparam name="T">The result type produced by the polling logic.</typeparam>
        /// <param name="logic">The polling function to evaluate.</param>
        /// <returns>The first non-default value returned by <paramref name="logic" />.</returns>
        /// <exception cref="System.ArgumentNullException">logic</exception>
        public static T WaitFor<T>(Func<T> logic) => WaitFor(logic, TimeSpan.FromSeconds(4));

        /// <summary>
        /// Waits until <paramref name="logic" /> returns a value other than <c>default(T)</c>.
        /// </summary>
        /// <typeparam name="T">The result type produced by the polling logic.</typeparam>
        /// <param name="logic">The polling function to evaluate.</param>
        /// <param name="timespan">The maximum time to wait for a non-default result.</param>
        /// <returns>The first non-default value returned by <paramref name="logic" />.</returns>
        /// <exception cref="System.ArgumentNullException">logic</exception>
        public static T WaitFor<T>(Func<T> logic, TimeSpan timespan) => WaitFor(logic, timespan, TimeSpan.FromMilliseconds(100));

        /// <summary>
        /// Sets the <see cref="Component" /> property with a new instance while maintaining the constructor setup and any other changes.
        /// </summary>
        /// <example>
        /// <para>Use <see cref="CreateComponent"/> when you need to rebuild the component after changing test-base configuration that affects construction or post-creation state.</para>
        /// <code language="csharp"><![CDATA[
        /// private string CreatedChannel { get; set; } = "default";
        ///
        /// protected override Action<OrderSubmitter> CreatedComponentAction => component =>
        ///     component.SubmissionChannel = CreatedChannel;
        ///
        /// [Fact]
        /// public void Recreates_component_after_changing_created_state()
        /// {
        ///     CreatedChannel = "priority";
        ///
        ///     CreateComponent();
        ///
        ///     Component.SubmissionChannel.Should().Be("priority");
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
                    Mocks.Dispose();
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
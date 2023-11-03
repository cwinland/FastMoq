using FastMoq.Extensions;
using FastMoq.Models;
using Moq;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

#pragma warning disable CS8603

namespace FastMoq
{
    /// <summary>
    ///     Auto Mocking Test Base with Fast Automatic Mocking <see cref="Mocker" />.
    /// </summary>
    /// <example>
    ///     Basic example of the base class creating the Car class and auto mocking ICarService.
    ///     <code><![CDATA[
    /// public class CarTest : MockerTestBase<Car> {
    ///      [Fact]
    ///      public void TestCar() {
    ///          Component.Color.Should().Be(Color.Green);
    ///          Component.CarService.Should().NotBeNull();
    ///      }
    /// }
    /// 
    /// public class Car {
    ///      public Color Color { get; set; } = Color.Green;
    ///      public ICarService CarService { get; }
    ///      public Car(ICarService carService) => CarService = carService;
    /// }
    /// 
    /// public interface ICarService
    /// {
    ///      Color Color { get; set; }
    ///      ICarService CarService { get; }
    ///      bool StartCar();
    /// }
    ///  ]]>
    ///  </code>
    ///     Example of how to set up for mocks that require specific functionality.
    ///     <code><![CDATA[
    /// public class CarTest : MockerTestBase<Car> {
    ///      public CarTest() : base(mocks => mocks.Initialize<ICarService>(mock => mock.Setup(x => x.StartCar).Returns(true));
    /// }
    ///  ]]>
    ///  </code>
    /// </example>
    /// <typeparam name="TComponent">The type of the t component.</typeparam>
    /// <inheritdoc />
    public abstract partial class MockerTestBase<TComponent> : IDisposable where TComponent : class
    {
        #region Fields

        private bool disposedValue;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the component under test.
        /// </summary>
        /// <value>The service.</value>
        protected internal TComponent? Component { get; set; }

        /// <summary>
        ///     Gets or sets the custom mocks. These are added whenever the component is created.
        /// </summary>
        /// <value>The custom mocks.</value>
        protected IEnumerable<MockModel> CustomMocks { get; set; } = new List<MockModel>();

        /// <summary>
        ///     Gets or sets the create component action. This action is run whenever the component is created.
        /// </summary>
        /// <value>The create component action.</value>
        protected virtual Func<Mocker, TComponent?> CreateComponentAction { get; }

        /// <summary>
        ///     Gets or sets the setup mocks action. This action is run before the component is created.
        /// </summary>
        /// <value>The setup mocks action.</value>
        protected virtual Action<Mocker>? SetupMocksAction { get; }

        /// <summary>
        ///     Gets or sets the created component action. This action is run after the component is created.
        /// </summary>
        /// <value>The created component action.</value>
        protected virtual Action<TComponent?>? CreatedComponentAction { get; }

        /// <summary>
        ///     Gets the <see cref="Mocker" />.
        /// </summary>
        /// <value>The mocks.</value>
        protected Mocker Mocks { get; } = new();

        private Func<Mocker, TComponent?> DefaultCreateAction => _ => Component = Mocks.CreateInstance<TComponent>();

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
        ///     Initializes a new instance of the <see cref="T:FastMoq.MockerTestBase`1" /> class.
        /// </summary>
        /// <param name="setupMocksAction">The setup mocks action.</param>
        /// <param name="createComponentAction">The create component action.</param>
        /// <inheritdoc />
        protected MockerTestBase(Action<Mocker> setupMocksAction, Func<Mocker, TComponent> createComponentAction)
            : this(setupMocksAction, createComponentAction, null) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockerTestBase{TComponent}" /> class.
        /// </summary>
        /// <param name="setupMocksAction">The setup mocks action.</param>
        /// <param name="createdComponentAction">The created component action.</param>
        /// <inheritdoc />
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
        ///     Initializes a new instance of the <see cref="T:FastMoq.MockerTestBase`1" /> class.
        /// </summary>
        /// <param name="innerMockResolution">if set to <c>true</c> [inner mock resolution].</param>
        /// <inheritdoc />
        protected MockerTestBase(bool innerMockResolution) : this() => Mocks.InnerMockResolution = innerMockResolution;

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
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="constructorIndex">Index of the constructor.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>T.</returns>
        /// <exception cref="ArgumentException">Invalid constructor index.</exception>
        public static T CreateInstanceByConstructor<T>(int constructorIndex, params object[] parameters)
        {
            // Get the constructors for the specified type
            var constructors = typeof(T).GetConstructors();

            // Check if the provided index is within range
            if (constructorIndex < 0 || constructorIndex >= constructors.Length)
            {
                throw new ArgumentException("Invalid constructor index.");
            }

            // Get the selected constructor
            var constructor = constructors[constructorIndex];

            // Invoke the constructor with the provided parameters
            var instance = (T) constructor.Invoke(parameters);

            // Return the created instance
            return instance;
        }

        /// <summary>
        ///     Lists the constructors.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void ListConstructors<T>()
        {
            // Get the constructors for the specified type
            var constructors = typeof(T).GetConstructors();

            // List the constructors
            Console.WriteLine("Constructors for type {0}:", typeof(T).Name);
            for (var i = 0; i < constructors.Length; i++)
            {
                Console.WriteLine("{0}: {1}", i, constructors[i]);
            }
        }

        /// <summary>
        ///     Tests the constructor parameters.
        /// </summary>
        /// <param name="logReport">The log report.</param>
        /// <param name="testData">The test data.</param>
        /// <param name="testParameters">The test parameters.</param>
        protected internal void TestConstructorParameters(out IReadOnlyCollection<ITestReportItem> logReport, object[]? testData = null, IReadOnlyList<bool[]>? testParameters = null)
        {
            var constructors = typeof(TComponent).GetConstructors();
            TestParameters<TComponent, ConstructorInfo>(constructors, testData ?? Array.Empty<object>(), testParameters ?? new List<bool[]>().AsReadOnly(), out logReport);
        }

        /// <summary>
        ///     Tests the methods.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logReport">The log report.</param>
        /// <param name="testData">The test data.</param>
        /// <param name="testParameters">The test parameters.</param>
        /// <exception cref="AggregateException"></exception>
        public void TestMethodParameters<T>(out IReadOnlyCollection<ITestReportItem> logReport, object[]? testData = null, IReadOnlyList<bool[]>? testParameters = null)
        {
            // Get the methods for the specified type
            var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance);

            TestParameters<T, MethodInfo>(methods, testData ?? Array.Empty<object>(),  testParameters ?? new List<bool[]>().AsReadOnly(), out logReport);
        }

        /// <summary>
        ///     Tests the parameters.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TMethod">The type of the t method.</typeparam>
        /// <param name="methods">The methods.</param>
        /// <param name="testData">The test data.</param>
        /// <param name="testParameters">The test parameters.</param>
        /// <param name="logReport">The log report.</param>
        /// <exception cref="AggregateException"></exception>
        public void TestParameters<T, TMethod>(TMethod[] methods, object[] testData, IReadOnlyList<bool[]> testParameters, out IReadOnlyCollection<ITestReportItem> logReport)
            where TMethod : MethodBase
        {
            var log = new Collection<ITestReportItem>();

            // Create a list to store exceptions
            var exceptions = new List<Exception>();

            // Iterate over each constructor
            foreach (var method in methods.Select((value, index) => new { value, index }))
            {
                // Get the parameters for the constructor
                var parameters = method.value.GetParameters();

                // Iterate over each parameter
                foreach (var parameter in parameters.Select((value, index) => new { value, index })
                             .Where(x => IsNullable(x.value) && IsTestParameter(testParameters, method.index, x.index)))
                {
                    // Create an array of parameter values
                    var parameterValues = parameters.Select((p, i) =>
                            i == parameter.index
                                ? null
                                : testData.GetTestData(i, p))
                        .ToArray();

                    // Try to invoke the constructor with the parameter values
                    ITestReportItem testReportItem = new TestReportItem(method.value);

                    try
                    {
                        if (testReportItem.Method is ConstructorInfo constructorInfo)
                        {
                            constructorInfo.Invoke(parameterValues);
                        }
                        else
                        {
                            // Create an instance of the class
                            var instance = Activator.CreateInstance<T>();
                            method.value.Invoke(instance, parameterValues);
                        }

                        var exception = new Exception(
                            $"Constructor for {method.value.DeclaringType?.Name} ({method.index}) did not throw ArgumentNullException for null parameter {parameter.value.Name}."); 

                        testReportItem.IsErrorThrown = false;
                        testReportItem.Error = exception;
                        exceptions.Add(exception);
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException is ArgumentNullException)
                    {
                        // Constructor threw ArgumentNullException as expected
                        testReportItem.IsErrorThrown = true;
                        testReportItem.Error = ex.InnerException;
                    }
                    finally
                    {
                        log.Add(testReportItem);
                    }
                }
            }

            logReport = log.ToList().AsReadOnly();

            // Check if there were any exceptions
            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        ///     Waits for an action.
        /// </summary>
        /// <typeparam name="T">Logic of T.</typeparam>
        /// <param name="logic">The action.</param>
        /// <param name="timespan">The maximum time to wait.</param>
        /// <param name="waitBetweenChecks">Time between each check.</param>
        /// <returns>T.</returns>
        /// <exception cref="ArgumentNullException">logic</exception>
        /// <exception cref="ApplicationException">Waitfor Timeout</exception>
        /// <exception cref="System.ArgumentNullException">logic</exception>
        /// <exception cref="System.ApplicationException">Waitfor Timeout</exception>
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
                ? throw new ApplicationException("Waitfor Timeout")
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
            foreach (var customMock in CustomMocks)
            {
                Mocks.AddMock(customMock.Mock, customMock.Type, true);
            }

            SetupMocksAction?.Invoke(Mocks);
            Component = CreateComponentAction?.Invoke(Mocks);
            CreatedComponentAction?.Invoke(Component);
        }

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

        /// <summary>
        ///     Tests the asynchronous function.
        /// </summary>
        /// <param name="funcMethod">The function.</param>
        /// <param name="resultAction">The result action.</param>
        /// <param name="args">The arguments.</param>
        /// <exception cref="ArgumentNullException">funcMethod</exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="System.ArgumentNullException">funcMethod</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        [Obsolete("Use TestMethodParameters, TestConstructorParameters, or TestParameters. This will be removed.")]
        protected void TestMethodParametersAsync(Expression<Func<TComponent, object>> funcMethod,
            Action<Func<Task>?, string?, List<object?>?, ParameterInfo> resultAction,
            params object?[]? args)
        {
            if (funcMethod == null)
            {
                throw new ArgumentNullException(nameof(funcMethod));
            }

            if (funcMethod.Body is UnaryExpression unary &&
                unary.Operand is MethodCallExpression methodCallExpression &&
                methodCallExpression.Object.GetPropertyValue("Value") is MethodInfo methodInfo)
            {
                TestMethodParametersAsync(methodInfo, resultAction, args);
            }
            else
            {
                throw new InvalidOperationException($"{nameof(funcMethod)} is not a valid method.");
            }
        }

        /// <summary>
        ///     Tests the method parameters asynchronous.
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <param name="resultAction">The result action.</param>
        /// <param name="args">The arguments.</param>
        /// <exception cref="ArgumentNullException">methodInfo</exception>
        /// <exception cref="ArgumentNullException">resultAction</exception>
        /// <exception cref="System.ArgumentNullException">methodInfo</exception>
        /// <exception cref="System.ArgumentNullException">resultAction</exception>
        [Obsolete("Use TestMethodParameters, TestConstructorParameters, or TestParameters. This will be removed.")]
        protected void TestMethodParametersAsync(MethodInfo methodInfo, Action<Func<Task>?, string?, List<object?>?, ParameterInfo> resultAction,
            params object?[]? args)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (resultAction == null)
            {
                throw new ArgumentNullException(nameof(resultAction));
            }

            var names = methodInfo.GetParameters().ToList();
            var subs = Mocks.GetMethodDefaultData(methodInfo).ToList();

            for (var i = 0; i < subs.Count; i++)
            {
                var list = new List<object?>();
                list.AddRange(subs);

                for (var j = 0; j < subs.Count; j++)
                {
                    if (j != i && args?.Length >= j)
                    {
                        list[j] = args[j];
                    }
                }

                resultAction(() => methodInfo.Invoke(Component, list.ToArray()) as Task,
                    names.Select(x => x.Name).Skip(i).First(),
                    list,
                    names[i]
                );
            }
        }

        private static bool IsNullable(ParameterInfo parameterValue) =>
            !parameterValue.ParameterType.IsValueType || Nullable.GetUnderlyingType(parameterValue.ParameterType) != null;

        private static bool IsTestParameter(IReadOnlyList<bool[]> testParameters, int constructorIndex, int parameterIndex) =>
            testParameters == null ||
            !testParameters.Any() ||
            (constructorIndex < testParameters.Count &&
             testParameters[constructorIndex] != null &&
             parameterIndex < testParameters[constructorIndex].Length &&
             testParameters[constructorIndex][parameterIndex]);

        #region IDisposable

        /// <inheritdoc />
        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
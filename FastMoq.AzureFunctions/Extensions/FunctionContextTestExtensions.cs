using System.Reflection;
using FastMoq.Providers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace FastMoq.Extensions
{
    /// <summary>
    /// Provides Azure Functions worker helpers for typed <see cref="FunctionContext.InstanceServices" /> setup.
    /// </summary>
    public static class FunctionContextTestExtensions
    {
        private static readonly MethodInfo SetupMockPropertyByNameMethod = typeof(MockerCreationExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(MockerCreationExtensions.SetupMockProperty) &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 3 &&
                method.GetParameters()[1].ParameterType == typeof(string));

        /// <summary>
        /// Creates a typed <see cref="FunctionContext.InstanceServices" /> provider with the common Azure Functions worker defaults.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="configureServices">Optional service registrations to apply after the worker defaults.</param>
        /// <param name="includeWorkerDefaults">True to include logging, options, and <see cref="WorkerOptions" /> setup.</param>
        /// <returns>A typed <see cref="IServiceProvider" /> suitable for <see cref="FunctionContext.InstanceServices" />.</returns>
        public static IServiceProvider CreateFunctionContextInstanceServices(this Mocker mocker, Action<IServiceCollection>? configureServices = null, bool includeWorkerDefaults = true)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            return mocker.CreateTypedServiceProvider(services =>
            {
                if (includeWorkerDefaults)
                {
                    services.AddLogging();
                    services.AddOptions();
                    services.Configure<WorkerOptions>(_ => { });
                }

                configureServices?.Invoke(services);
            });
        }

        /// <summary>
        /// Registers typed <see cref="FunctionContext.InstanceServices" /> behavior for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="instanceServices">The provider to expose through <see cref="FunctionContext.InstanceServices" />.</param>
        /// <param name="replace">True to replace an existing known-type registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddFunctionContextInstanceServices(this Mocker mocker, IServiceProvider instanceServices, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(instanceServices);

            mocker.AddServiceProvider(instanceServices, replace);
            mocker.AddKnownType<FunctionContext>(
                configureMock: (_, _, fastMock) => ConfigureFunctionContextInstanceServices(fastMock, instanceServices),
                applyObjectDefaults: (_, functionContext) => AssignFunctionContextInstanceServices(functionContext, instanceServices),
                includeDerivedTypes: true,
                replace: replace);

            if (mocker.Contains(typeof(FunctionContext)))
            {
                ConfigureFunctionContextInstanceServices(mocker.GetOrCreateMock<FunctionContext>(), instanceServices);
            }

            return mocker;
        }

        /// <summary>
        /// Builds and registers typed <see cref="FunctionContext.InstanceServices" /> behavior for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="configureServices">Optional service registrations to apply after the worker defaults.</param>
        /// <param name="replace">True to replace an existing known-type registration.</param>
        /// <param name="includeWorkerDefaults">True to include logging, options, and <see cref="WorkerOptions" /> setup.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddFunctionContextInstanceServices(this Mocker mocker, Action<IServiceCollection>? configureServices = null, bool replace = false, bool includeWorkerDefaults = true)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            return mocker.AddFunctionContextInstanceServices(
                mocker.CreateFunctionContextInstanceServices(configureServices, includeWorkerDefaults),
                replace);
        }

        private static void ConfigureFunctionContextInstanceServices(IFastMock fastMock, IServiceProvider instanceServices)
        {
            ArgumentNullException.ThrowIfNull(fastMock);
            ArgumentNullException.ThrowIfNull(instanceServices);

            TryConfigureNativeMockProperty(fastMock, nameof(FunctionContext.InstanceServices), instanceServices);
            AssignFunctionContextInstanceServices(fastMock.Instance as FunctionContext, instanceServices);
        }

        private static void TryConfigureNativeMockProperty(IFastMock fastMock, string propertyName, object value)
        {
            var nativeMock = fastMock.NativeMock;
            if (nativeMock is null)
            {
                return;
            }

            var nativeMockType = nativeMock.GetType();
            if (nativeMockType.Namespace != "Moq" || !nativeMockType.IsGenericType)
            {
                return;
            }

            var mockedType = nativeMockType.GetGenericArguments()[0];
            var closedMethod = SetupMockPropertyByNameMethod.MakeGenericMethod(mockedType);
            closedMethod.Invoke(null, [nativeMock, propertyName, value]);
        }

        private static void AssignFunctionContextInstanceServices(FunctionContext? functionContext, IServiceProvider instanceServices)
        {
            if (functionContext is null)
            {
                return;
            }

            try
            {
                functionContext.InstanceServices = instanceServices;
            }
            catch
            {
            }
        }
    }
}
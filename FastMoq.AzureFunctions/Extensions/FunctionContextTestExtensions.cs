using Azure.Core;
using Azure.Core.Serialization;
using System.Reflection;
using FastMoq.Extensions;
using FastMoq.Providers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FastMoq.AzureFunctions.Extensions
{
    /// <summary>
    /// Provides Azure Functions worker helpers for typed <see cref="FunctionContext.InstanceServices" /> setup and execution-context shaping.
    /// </summary>
    public static class FunctionContextTestExtensions
    {
        private static readonly ObjectSerializer DefaultFunctionSerializer = new JsonObjectSerializer();

        private static readonly MethodInfo SetupMockPropertyByNameMethod = typeof(MockerCreationExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(MockerCreationExtensions.SetupMockProperty) &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 3 &&
                method.GetParameters()[1].ParameterType == typeof(string));

        private static readonly MethodInfo? NSubstituteReturnsMethod = Type.GetType("NSubstitute.SubstituteExtensions, NSubstitute")
            ?.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(method =>
                method.Name == "Returns" &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 3 &&
                method.GetParameters()[0].ParameterType.IsGenericParameter &&
                method.GetParameters()[1].ParameterType.IsGenericParameter &&
                method.GetParameters()[2].ParameterType.IsArray &&
                method.GetParameters()[2].ParameterType.GetElementType()?.IsGenericParameter == true);

        /// <summary>
        /// Configures a tracked <see cref="FunctionContext" /> mock to return the supplied invocation identifier.
        /// </summary>
        /// <param name="fastMock">The tracked mock whose <see cref="FunctionContext.InvocationId" /> getter should return <paramref name="invocationId" />.</param>
        /// <param name="invocationId">The invocation identifier to expose through <see cref="FunctionContext.InvocationId" />.</param>
        /// <returns>The current tracked mock.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="fastMock" /> does not represent a <see cref="FunctionContext" />.</exception>
        public static IFastMock AddFunctionContextInvocationId(this IFastMock fastMock, string invocationId)
        {
            ArgumentNullException.ThrowIfNull(fastMock);
            ArgumentException.ThrowIfNullOrWhiteSpace(invocationId);

            if (!typeof(FunctionContext).IsAssignableFrom(fastMock.MockedType))
            {
                throw new ArgumentException($"The supplied mock must represent {typeof(FunctionContext).FullName}.", nameof(fastMock));
            }

            ConfigureFunctionContextInvocationId(fastMock, invocationId);
            return fastMock;
        }

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
                    services.Configure<WorkerOptions>(options =>
                    {
                        options.Serializer ??= DefaultFunctionSerializer;
                    });
                    services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<WorkerOptions>>().Value);
                    services.AddSingleton<ObjectSerializer>(serviceProvider => serviceProvider.GetRequiredService<WorkerOptions>().Serializer ?? DefaultFunctionSerializer);
                }

                configureServices?.Invoke(services);
            });
        }

        /// <summary>
        /// Configures typed <see cref="FunctionContext.InstanceServices" /> behavior on a tracked <see cref="FunctionContext" /> mock without replacing the enclosing <see cref="Mocker" /> service registrations.
        /// </summary>
        /// <param name="fastMock">The tracked mock whose <see cref="FunctionContext.InstanceServices" /> getter should return <paramref name="instanceServices" />.</param>
        /// <param name="instanceServices">The provider to expose through <see cref="FunctionContext.InstanceServices" />.</param>
        /// <returns>The current tracked mock.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="fastMock" /> does not represent a <see cref="FunctionContext" />.</exception>
        public static IFastMock AddFunctionContextInstanceServices(this IFastMock fastMock, IServiceProvider instanceServices)
        {
            ArgumentNullException.ThrowIfNull(fastMock);
            ArgumentNullException.ThrowIfNull(instanceServices);

            if (!typeof(FunctionContext).IsAssignableFrom(fastMock.MockedType))
            {
                throw new ArgumentException($"The supplied mock must represent {typeof(FunctionContext).FullName}.", nameof(fastMock));
            }

            ConfigureFunctionContextInstanceServices(fastMock, instanceServices);
            return fastMock;
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

        /// <summary>
        /// Registers a <see cref="FunctionContext" /> invocation identifier for the current <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="invocationId">The invocation identifier to expose through <see cref="FunctionContext.InvocationId" />.</param>
        /// <param name="replace">True to replace an existing known-type registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        /// <remarks>
        /// This helper configures mock-backed <see cref="FunctionContext" /> instances. Concrete contexts with their own immutable execution metadata may ignore this registration.
        /// </remarks>
        public static Mocker AddFunctionContextInvocationId(this Mocker mocker, string invocationId, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentException.ThrowIfNullOrWhiteSpace(invocationId);

            mocker.AddKnownType<FunctionContext>(
                configureMock: (_, _, fastMock) => ConfigureFunctionContextInvocationId(fastMock, invocationId),
                includeDerivedTypes: true,
                replace: replace);

            if (mocker.Contains(typeof(FunctionContext)))
            {
                ConfigureFunctionContextInvocationId(mocker.GetOrCreateMock<FunctionContext>(), invocationId);
            }

            return mocker;
        }

        private static void ConfigureFunctionContextInstanceServices(IFastMock fastMock, IServiceProvider instanceServices)
        {
            ArgumentNullException.ThrowIfNull(fastMock);
            ArgumentNullException.ThrowIfNull(instanceServices);

            TryConfigureNativeMockProperty(fastMock, nameof(FunctionContext.InstanceServices), instanceServices);
            AssignFunctionContextInstanceServices(fastMock.Instance as FunctionContext, instanceServices);
        }

        private static void ConfigureFunctionContextInvocationId(IFastMock fastMock, string invocationId)
        {
            ArgumentNullException.ThrowIfNull(fastMock);
            ArgumentException.ThrowIfNullOrWhiteSpace(invocationId);

            TryConfigureNativeMockProperty(fastMock, nameof(FunctionContext.InvocationId), invocationId);
        }

        private static void TryConfigureNativeMockProperty(IFastMock fastMock, string propertyName, object value)
        {
            if (TryConfigureMoqNativeMockProperty(fastMock.NativeMock, propertyName, value))
            {
                return;
            }

            _ = TryConfigureNSubstitutePropertyGetter(fastMock, propertyName, value);
        }

        private static bool TryConfigureMoqNativeMockProperty(object? nativeMock, string propertyName, object value)
        {
            if (nativeMock is null)
            {
                return false;
            }

            var nativeMockType = nativeMock.GetType();
            if (nativeMockType.Namespace != "Moq" || !nativeMockType.IsGenericType)
            {
                return false;
            }

            var mockedType = nativeMockType.GetGenericArguments()[0];
            var closedMethod = SetupMockPropertyByNameMethod.MakeGenericMethod(mockedType);
            closedMethod.Invoke(null, [nativeMock, propertyName, value]);
            return true;
        }

        private static bool TryConfigureNSubstitutePropertyGetter(IFastMock fastMock, string propertyName, object value)
        {
            if (NSubstituteReturnsMethod is null)
            {
                return false;
            }

            var propertyInfo = fastMock.MockedType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo?.GetMethod is null)
            {
                return false;
            }

            if (value is null)
            {
                if (propertyInfo.PropertyType.IsValueType && Nullable.GetUnderlyingType(propertyInfo.PropertyType) is null)
                {
                    return false;
                }
            }
            else if (!propertyInfo.PropertyType.IsAssignableFrom(value.GetType()))
            {
                return false;
            }

            try
            {
                var getterResult = propertyInfo.GetMethod.Invoke(fastMock.Instance, Array.Empty<object?>());
                var closedReturnsMethod = NSubstituteReturnsMethod.MakeGenericMethod(propertyInfo.PropertyType);
                var emptyReturnSequence = Array.CreateInstance(propertyInfo.PropertyType, 0);
                closedReturnsMethod.Invoke(null, [getterResult, value, emptyReturnSequence]);
                return true;
            }
            catch
            {
                return false;
            }
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
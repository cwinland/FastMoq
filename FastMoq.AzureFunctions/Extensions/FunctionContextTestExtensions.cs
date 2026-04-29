using Azure.Core;
using Azure.Core.Serialization;
using System.Linq;
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
        /// <param name="replace">True to replace an existing <see cref="IServiceProvider" /> registration and overwrite previously configured instance-services helper values while preserving other <see cref="FunctionContext" /> behavior.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker AddFunctionContextInstanceServices(this Mocker mocker, IServiceProvider instanceServices, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(instanceServices);

            EnsureFunctionContextKnownTypeCanBeUpdated(mocker, replace);
            mocker.AddServiceProvider(instanceServices, replace);
            AddOrUpdateFunctionContextKnownTypeRegistration(
                mocker,
                replace,
                configureMock: fastMock => ConfigureFunctionContextInstanceServices(fastMock, instanceServices),
                applyObjectDefaults: functionContext => AssignFunctionContextInstanceServices(functionContext, instanceServices));

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
        /// <param name="replace">True to overwrite a previously configured invocation-identifier helper value while preserving other <see cref="FunctionContext" /> behavior.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        /// <remarks>
        /// This helper configures mock-backed <see cref="FunctionContext" /> instances. Concrete contexts with their own immutable execution metadata may ignore this registration.
        /// </remarks>
        public static Mocker AddFunctionContextInvocationId(this Mocker mocker, string invocationId, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentException.ThrowIfNullOrWhiteSpace(invocationId);

            EnsureFunctionContextKnownTypeCanBeUpdated(mocker, replace);
            AddOrUpdateFunctionContextKnownTypeRegistration(
                mocker,
                replace,
                configureMock: fastMock => ConfigureFunctionContextInvocationId(fastMock, invocationId));

            if (mocker.Contains(typeof(FunctionContext)))
            {
                ConfigureFunctionContextInvocationId(mocker.GetOrCreateMock<FunctionContext>(), invocationId);
            }

            return mocker;
        }

        private static void AddOrUpdateFunctionContextKnownTypeRegistration(
            Mocker mocker,
            bool replace,
            Action<IFastMock>? configureMock = null,
            Action<FunctionContext>? applyObjectDefaults = null)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            var existingRegistration = mocker.KnownTypeRegistrations.LastOrDefault(registration => registration.ServiceType == typeof(FunctionContext));
            var mergedRegistration = new KnownTypeRegistration(typeof(FunctionContext))
            {
                IncludeDerivedTypes = existingRegistration?.IncludeDerivedTypes ?? true,
                DirectInstanceFactory = existingRegistration?.DirectInstanceFactory,
                ManagedInstanceFactory = existingRegistration?.ManagedInstanceFactory,
                ConfigureMock = ComposeFunctionContextConfigureMock(existingRegistration?.ConfigureMock, configureMock),
                ApplyObjectDefaults = ComposeFunctionContextObjectDefaults(existingRegistration?.ApplyObjectDefaults, applyObjectDefaults),
            };

            mocker.AddKnownType(mergedRegistration, replace);
        }

        private static void EnsureFunctionContextKnownTypeCanBeUpdated(Mocker mocker, bool replace)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            if (replace)
            {
                return;
            }

            if (mocker.KnownTypeRegistrations.Any(registration => registration.ServiceType == typeof(FunctionContext)))
            {
                typeof(FunctionContext).ThrowAlreadyExists();
            }
        }

        private static Action<Mocker, Type, IFastMock>? ComposeFunctionContextConfigureMock(
            Action<Mocker, Type, IFastMock>? existing,
            Action<IFastMock>? additional)
        {
            if (additional is null)
            {
                return existing;
            }

            if (existing is null)
            {
                return (_, _, fastMock) => additional(fastMock);
            }

            return (registeredMocker, requestedType, fastMock) =>
            {
                existing(registeredMocker, requestedType, fastMock);
                additional(fastMock);
            };
        }

        private static Action<Mocker, object>? ComposeFunctionContextObjectDefaults(
            Action<Mocker, object>? existing,
            Action<FunctionContext>? additional)
        {
            if (additional is null)
            {
                return existing;
            }

            if (existing is null)
            {
                return (_, value) =>
                {
                    if (value is FunctionContext functionContext)
                    {
                        additional(functionContext);
                    }
                };
            }

            return (registeredMocker, value) =>
            {
                existing(registeredMocker, value);
                if (value is FunctionContext functionContext)
                {
                    additional(functionContext);
                }
            };
        }

        private static void ConfigureFunctionContextInstanceServices(IFastMock fastMock, IServiceProvider instanceServices)
        {
            ArgumentNullException.ThrowIfNull(fastMock);
            ArgumentNullException.ThrowIfNull(instanceServices);

            MockPropertyConfigurationHelper.ConfigureNativeMockProperty(fastMock, nameof(FunctionContext.InstanceServices), instanceServices);
            AssignFunctionContextInstanceServices(fastMock.Instance as FunctionContext, instanceServices);
        }

        private static void ConfigureFunctionContextInvocationId(IFastMock fastMock, string invocationId)
        {
            ArgumentNullException.ThrowIfNull(fastMock);
            ArgumentException.ThrowIfNullOrWhiteSpace(invocationId);

            MockPropertyConfigurationHelper.ConfigureNativeMockProperty(fastMock, nameof(FunctionContext.InvocationId), invocationId);
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
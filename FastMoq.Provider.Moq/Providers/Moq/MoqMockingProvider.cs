using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Providers.MoqProvider
{
    /// <summary>
    /// Provider implementation that adapts Moq to the provider-neutral FastMoq abstractions.
    /// </summary>
    public sealed class MoqMockingProvider : IMockingProvider, IMockingProviderCapabilities, ITrackedMockPropertyConfigurator
    {
        private static readonly MethodInfo SetupLoggerCallbackGenericMethod = typeof(MoqMockingProvider)
            .GetMethod(nameof(SetupLoggerCallbackGeneric), BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Could not resolve {nameof(SetupLoggerCallbackGeneric)} for logger setup dispatch.");
        private static readonly MethodInfo ConfigureMockPropertyGenericMethod = typeof(MoqMockingProvider)
            .GetMethod(nameof(TryConfigureMockPropertyGeneric), BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Could not resolve {nameof(TryConfigureMockPropertyGeneric)} for property setup dispatch.");
        private static readonly ConcurrentDictionary<Type, Action<Mock, Action<LogLevel, EventId, string, Exception?>>> LoggerSetupDispatchCache = new();
        private static readonly ConcurrentDictionary<Type, Func<Mock, PropertyInfo, object?, bool>> PropertySetupDispatchCache = new();

        /// <summary>
        /// Gets the shared singleton instance of the Moq provider.
        /// </summary>
        public static readonly MoqMockingProvider Instance = new();

        private MoqMockingProvider()
        {
        }

        /// <summary>
        /// Gets the capability descriptor for this provider.
        /// </summary>
        public IMockingProviderCapabilities Capabilities => this;

        /// <summary>
        /// Gets a value indicating whether Moq supports base-call behavior.
        /// </summary>
        public bool SupportsCallBase => true;

        /// <summary>
        /// Gets a value indicating whether Moq supports automatic property backing.
        /// </summary>
        public bool SupportsSetupAllProperties => true;

        /// <summary>
        /// Gets a value indicating whether Moq supports protected member interception.
        /// </summary>
        public bool SupportsProtectedMembers => true;

        /// <summary>
        /// Gets a value indicating whether Moq supports invocation tracking.
        /// </summary>
        public bool SupportsInvocationTracking => true;

        /// <summary>
        /// Gets a value indicating whether Moq supports logger capture helpers.
        /// </summary>
        public bool SupportsLoggerCapture => true;

        /// <summary>
        /// Builds a provider-neutral wildcard expression for compatibility with expression-valued arguments.
        /// </summary>
        public Expression<Func<T, bool>> BuildExpression<T>()
        {
            return FastArg.AnyExpression<T>();
        }

        /// <summary>
        /// Creates a typed FastMoq wrapper around a new Moq mock.
        /// </summary>
        public IFastMock<T> CreateMock<T>(MockCreationOptions? options = null) where T : class
        {
            MoqProviderTransitionWarning.EmitOnce();

            options ??= new();
            var behavior = options.Strict ? MockBehavior.Strict : MockBehavior.Loose;
            var mock = options.ConstructorArgs is { Length: > 0 }
                ? new Mock<T>(behavior, options.ConstructorArgs)
                : new Mock<T>(behavior);

            if (options.CallBase)
            {
                mock.CallBase = true;
            }

            return new MoqFastMockGeneric<T>(mock);
        }

        /// <summary>
        /// Creates an untyped FastMoq wrapper around a new Moq mock.
        /// </summary>
        public IFastMock CreateMock(Type type, MockCreationOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(type);

            MoqProviderTransitionWarning.EmitOnce();

            options ??= new();
            var behavior = options.Strict ? MockBehavior.Strict : MockBehavior.Loose;
            var generic = typeof(Mock<>).MakeGenericType(type);
            var args = options.ConstructorArgs is { Length: > 0 }
                ? new object?[] { behavior, options.ConstructorArgs }
                : new object?[] { behavior };
            var mock = (Mock) Activator.CreateInstance(generic, args)!;

            if (options.CallBase)
            {
                mock.CallBase = true;
            }

            return new MoqFastMock(mock);
        }

        /// <summary>
        /// Configures all settable properties on the supplied mock for automatic backing storage.
        /// </summary>
        public void SetupAllProperties(IFastMock mock)
        {
            var underlying = TryGetUnderlyingMock(mock);
            underlying?.GetType().GetMethod("SetupAllProperties")?.Invoke(underlying, null);
        }

        /// <summary>
        /// Enables or disables base-call behavior on the supplied mock.
        /// </summary>
        public void SetCallBase(IFastMock mock, bool value)
        {
            var underlying = TryGetUnderlyingMock(mock);
            if (underlying != null)
            {
                underlying.CallBase = value;
            }
        }

        /// <summary>
        /// Verifies that the supplied expression was invoked on the wrapped Moq mock.
        /// </summary>
        public void Verify<T>(IFastMock<T> mock, Expression<Action<T>> expression, TimesSpec? times = null) where T : class
        {
            ArgumentNullException.ThrowIfNull(expression);

            Mock<T>? moqMock = null;
            try
            {
                moqMock = Mock.Get(mock.Instance);
            }
            catch
            {
            }

            if (moqMock == null)
            {
                return;
            }

            var rewrittenExpression = FastArgMoqExpressionRewriter.Rewrite(expression);

            if (times?.Mode == TimesSpecMode.Exactly)
            {
                moqMock.Verify(rewrittenExpression, Times.Exactly(times.Value.Count ?? throw new InvalidOperationException("TimesSpec.Exactly requires a count.")));
                return;
            }

            if (times?.Mode == TimesSpecMode.AtLeast)
            {
                moqMock.Verify(rewrittenExpression, Times.AtLeast(times.Value.Count ?? throw new InvalidOperationException("TimesSpec.AtLeast requires a count.")));
                return;
            }

            if (times?.Mode == TimesSpecMode.AtMost)
            {
                moqMock.Verify(rewrittenExpression, Times.AtMost(times.Value.Count ?? throw new InvalidOperationException("TimesSpec.AtMost requires a count.")));
                return;
            }

            if (times?.Mode == TimesSpecMode.Never)
            {
                moqMock.Verify(rewrittenExpression, Times.Never());
                return;
            }

            moqMock.Verify(rewrittenExpression);
        }

        /// <summary>
        /// Verifies that the supplied method was invoked on the wrapped Moq mock while treating every argument as a wildcard matcher.
        /// </summary>
        public void VerifyMethod<T>(IFastMock<T> mock, MethodInfo method, TimesSpec? times = null) where T : class
        {
            ArgumentNullException.ThrowIfNull(method);

            Mock<T>? moqMock = null;
            try
            {
                moqMock = Mock.Get(mock.Instance);
            }
            catch
            {
            }

            if (moqMock == null)
            {
                return;
            }

            times ??= default;
            var matchingInvocations = moqMock.Invocations
                .Where(invocation => MethodsMatch(invocation.Method, method))
                .ToList();

            AssertExpectedInvocationCount(method.Name, matchingInvocations.Count, times.Value);
            if (times.Value.Mode != TimesSpecMode.Never && matchingInvocations.Count > 0)
            {
                MarkInvocationsVerified(matchingInvocations);
            }
        }

        /// <summary>
        /// Verifies that no unverified Moq calls remain on the supplied mock.
        /// </summary>
        public void VerifyNoOtherCalls(IFastMock mock)
        {
            var underlying = TryGetUnderlyingMock(mock);
            if (underlying == null)
            {
                return;
            }

            var staticMethod = typeof(Mock).GetMethod("VerifyNoOtherCalls", BindingFlags.Public | BindingFlags.Static);
            if (staticMethod == null)
            {
                return;
            }

            var parameters = staticMethod.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType.IsArray)
            {
                staticMethod.Invoke(null, new object?[] { new Mock[] { underlying } });
                return;
            }

            if (parameters.Length == 1)
            {
                staticMethod.Invoke(null, new object?[] { underlying });
            }
        }

        /// <summary>
        /// Configures property behavior on the supplied mock when the provider supports it.
        /// </summary>
        public void ConfigureProperties(IFastMock mock)
        {
            if (Capabilities.SupportsSetupAllProperties)
            {
                SetupAllProperties(mock);
            }
        }

        /// <summary>
        /// Configures logger callback capture on the supplied mock.
        /// </summary>
        public void ConfigureLogger(IFastMock mock, Action<LogLevel, EventId, string, Exception?> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            var underlying = TryGetUnderlyingMock(mock);
            if (underlying == null)
            {
                return;
            }

            var mockedType = mock.MockedType;
            if (!typeof(ILogger).IsAssignableFrom(mockedType))
            {
                return;
            }

            SetupLoggerCallback(underlying, mockedType, callback);
        }

        /// <summary>
        /// Attempts to configure a single property getter on the wrapped Moq mock.
        /// </summary>
        public bool TryConfigureMockProperty(IFastMock mock, PropertyInfo propertyInfo, object? value)
        {
            ArgumentNullException.ThrowIfNull(mock);
            ArgumentNullException.ThrowIfNull(propertyInfo);

            var underlying = TryGetUnderlyingMock(mock);
            if (underlying == null)
            {
                return false;
            }

            var dispatcher = PropertySetupDispatchCache.GetOrAdd(mock.MockedType, static mockedType =>
                (Func<Mock, PropertyInfo, object?, bool>)ConfigureMockPropertyGenericMethod
                    .MakeGenericMethod(mockedType)
                    .CreateDelegate(typeof(Func<Mock, PropertyInfo, object?, bool>)));

            return dispatcher(underlying, propertyInfo, value);
        }

        /// <summary>
        /// Attempts to expose the underlying Moq mock from a provider-neutral wrapper.
        /// </summary>
        public object? TryGetLegacy(IFastMock mock)
        {
            MoqProviderTransitionWarning.EmitOnce();
            return TryGetUnderlyingMock(mock);
        }

        /// <summary>
        /// Attempts to wrap an existing Moq mock in the provider-neutral FastMoq abstraction.
        /// </summary>
        public IFastMock? TryWrapLegacy(object legacyMock, Type mockedType)
        {
            ArgumentNullException.ThrowIfNull(legacyMock);
            ArgumentNullException.ThrowIfNull(mockedType);

            MoqProviderTransitionWarning.EmitOnce();

            if (legacyMock is not Mock mock)
            {
                return null;
            }

            var legacyGenericMockedType = TryGetLegacyGenericMockedType(legacyMock.GetType());
            if (legacyGenericMockedType == mockedType)
            {
                var wrapperType = typeof(MoqFastMockGeneric<>).MakeGenericType(mockedType);
                return (IFastMock) Activator.CreateInstance(wrapperType, legacyMock)!;
            }

            return new MoqFastMock(mock);
        }

        private static Type? TryGetLegacyGenericMockedType(Type legacyType)
        {
            for (var current = legacyType; current != null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Mock<>))
                {
                    return current.GetGenericArguments()[0];
                }
            }

            return null;
        }

        private static void SetupLoggerCallback(Mock logger, Type mockedType, Action<LogLevel, EventId, string, Exception?> callback)
        {
            var dispatcher = LoggerSetupDispatchCache.GetOrAdd(mockedType, static type =>
                (Action<Mock, Action<LogLevel, EventId, string, Exception?>>) SetupLoggerCallbackGenericMethod
                    .MakeGenericMethod(type)
                    .CreateDelegate(typeof(Action<Mock, Action<LogLevel, EventId, string, Exception?>>)));

            dispatcher(logger, callback);
        }

        private static void AssertExpectedInvocationCount(string methodName, int count, TimesSpec times)
        {
            if (times.Mode == TimesSpecMode.Never)
            {
                if (count > 0)
                {
                    throw new InvalidOperationException($"Expected no calls to {methodName} but found {count}.");
                }

                return;
            }

            if (times.Mode == TimesSpecMode.Exactly)
            {
                var expected = times.Count ?? throw new InvalidOperationException("TimesSpec.Exactly requires a count.");
                if (count != expected)
                {
                    throw new InvalidOperationException($"Expected exactly {expected} call(s) to {methodName} but found {count}.");
                }

                return;
            }

            if (times.Mode == TimesSpecMode.AtLeast)
            {
                var minimum = times.Count ?? throw new InvalidOperationException("TimesSpec.AtLeast requires a count.");
                if (count < minimum)
                {
                    throw new InvalidOperationException($"Expected at least {minimum} call(s) to {methodName} but found {count}.");
                }

                return;
            }

            if (times.Mode == TimesSpecMode.AtMost)
            {
                var maximum = times.Count ?? throw new InvalidOperationException("TimesSpec.AtMost requires a count.");
                if (count > maximum)
                {
                    throw new InvalidOperationException($"Expected at most {maximum} call(s) to {methodName} but found {count}.");
                }

                return;
            }

            if (count == 0)
            {
                throw new InvalidOperationException($"Expected at least one call to {methodName} but found none.");
            }
        }

        private static void MarkInvocationsVerified(IEnumerable<IInvocation> invocations)
        {
            foreach (var invocation in invocations)
            {
                invocation.GetType()
                    .GetMethod("MarkAsVerified", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(invocation, null);
            }
        }

        private static bool MethodsMatch(MethodInfo actualMethod, MethodInfo expectedMethod)
        {
            if (actualMethod == expectedMethod)
            {
                return true;
            }

            if (actualMethod.Name != expectedMethod.Name || actualMethod.ReturnType != expectedMethod.ReturnType)
            {
                return false;
            }

            var actualParameters = actualMethod.GetParameters();
            var expectedParameters = expectedMethod.GetParameters();
            if (actualParameters.Length != expectedParameters.Length)
            {
                return false;
            }

            for (var index = 0; index < actualParameters.Length; index++)
            {
                if (actualParameters[index].ParameterType != expectedParameters[index].ParameterType)
                {
                    return false;
                }
            }

            return true;
        }

        private static void SetupLoggerCallbackGeneric<TLogger>(Mock logger, Action<LogLevel, EventId, string, Exception?> callback)
            where TLogger : class, ILogger
        {
            if (logger is not Mock<TLogger> typedLogger)
            {
                return;
            }

            typedLogger.SetupLoggerCallback(callback);
        }

        private static Mock? TryGetUnderlyingMock(IFastMock wrapper)
        {
            if (wrapper.NativeMock is Mock nativeMock)
            {
                return nativeMock;
            }

            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var type = wrapper.GetType();
            string[] propertyNames = ["InnerMock", "Inner"];
            foreach (var propertyName in propertyNames)
            {
                var property = type.GetProperty(propertyName, Flags);
                if (property?.GetValue(wrapper) is Mock mock)
                {
                    return mock;
                }
            }

            try
            {
                var instance = wrapper.Instance;
                if (instance != null)
                {
                    var getMethod = typeof(Mock).GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(method => method.Name == "Get" && method.IsGenericMethodDefinition && method.GetParameters().Length == 1);
                    if (getMethod != null)
                    {
                        var constructed = getMethod.MakeGenericMethod(instance.GetType());
                        if (constructed.Invoke(null, new object[] { instance }) is Mock discovered)
                        {
                            return discovered;
                        }
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static bool TryConfigureMockPropertyGeneric<TMock>(Mock mock, PropertyInfo propertyInfo, object? value)
            where TMock : class
        {
            if (mock is not Mock<TMock> typedMock || propertyInfo.GetMethod is null)
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
                var instanceParam = Expression.Parameter(typeof(TMock), "instance");
                Expression propertyAccess = Expression.Property(instanceParam, propertyInfo);
                if (propertyInfo.PropertyType.IsValueType)
                {
                    propertyAccess = Expression.Convert(propertyAccess, typeof(object));
                }

                var getterExpression = Expression.Lambda<Func<TMock, object>>(propertyAccess, instanceParam);
                object configuredValue = value!;
                typedMock.Setup(getterExpression).Returns(configuredValue);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static class MoqProviderTransitionWarning
        {
            private static int _emitted;
            private static readonly HashSet<string> FastMoqProductAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
            {
                "FastMoq",
                "FastMoq.Abstractions",
                "FastMoq.Core",
                "FastMoq.Azure",
                "FastMoq.AzureFunctions",
                "FastMoq.Database",
                "FastMoq.Provider.Moq",
                "FastMoq.Provider.NSubstitute",
                "FastMoq.Web",
            };

            internal static void EmitOnce()
            {
                if (!ShouldEmit())
                {
                    return;
                }

                if (System.Threading.Interlocked.Exchange(ref _emitted, 1) == 1)
                {
                    return;
                }

                Console.Error.WriteLine("[FastMoq] Warning: FastMoq.Provider.Moq is a v4 transition dependency and will no longer be bundled by FastMoq.Core in v5. Install and select your mocking provider explicitly before upgrading.");
            }

            private static bool ShouldEmit()
            {
                return !IsReferencedByConsumerAssembly();
            }

            private static bool IsReferencedByConsumerAssembly()
            {
                var providerAssemblyName = typeof(MoqMockingProvider).Assembly.GetName().Name;
                if (string.IsNullOrWhiteSpace(providerAssemblyName))
                {
                    return false;
                }

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.IsDynamic)
                    {
                        continue;
                    }

                    var assemblyName = assembly.GetName().Name;
                    if (string.IsNullOrWhiteSpace(assemblyName) || FastMoqProductAssemblyNames.Contains(assemblyName))
                    {
                        continue;
                    }

                    try
                    {
                        if (assembly.GetReferencedAssemblies().Any(reference => string.Equals(reference.Name, providerAssemblyName, StringComparison.OrdinalIgnoreCase)))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                    }
                }

                return false;
            }
        }
    }
}

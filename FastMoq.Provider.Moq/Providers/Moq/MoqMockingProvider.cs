using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;

namespace FastMoq.Providers.MoqProvider
{
    /// <summary>
    /// Provider implementation that adapts Moq to the provider-neutral FastMoq abstractions.
    /// </summary>
    public sealed class MoqMockingProvider : IMockingProvider, IMockingProviderCapabilities
    {
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
        /// Builds a Moq expression scaffold for provider-specific use.
        /// </summary>
        public Expression<Func<T, bool>> BuildExpression<T>()
        {
            MoqProviderTransitionWarning.EmitOnce();
            return It.IsAny<Expression<Func<T, bool>>>();
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
            var mock = (Mock)Activator.CreateInstance(generic, args)!;

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

            if (times?.Mode == TimesSpecMode.Exactly)
            {
                moqMock.Verify(expression, Times.Exactly(times.Value.Count ?? throw new InvalidOperationException("TimesSpec.Exactly requires a count.")));
                return;
            }

            if (times?.Mode == TimesSpecMode.AtLeast)
            {
                moqMock.Verify(expression, Times.AtLeast(times.Value.Count ?? throw new InvalidOperationException("TimesSpec.AtLeast requires a count.")));
                return;
            }

            if (times?.Mode == TimesSpecMode.AtMost)
            {
                moqMock.Verify(expression, Times.AtMost(times.Value.Count ?? throw new InvalidOperationException("TimesSpec.AtMost requires a count.")));
                return;
            }

            if (times?.Mode == TimesSpecMode.Never)
            {
                moqMock.Verify(expression, Times.Never());
                return;
            }

            moqMock.Verify(expression);
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

            var legacyType = legacyMock.GetType();
            if (legacyType.IsGenericType && legacyType.GetGenericTypeDefinition() == typeof(Mock<>))
            {
                var genericArgument = legacyType.GetGenericArguments()[0];
                if (genericArgument == mockedType)
                {
                    var wrapperType = typeof(MoqFastMockGeneric<>).MakeGenericType(mockedType);
                    return (IFastMock)Activator.CreateInstance(wrapperType, legacyMock)!;
                }
            }

            return new MoqFastMock(mock);
        }

        private static void SetupLoggerCallback(Mock logger, Type mockedType, Action<LogLevel, EventId, string, Exception?> callback)
        {
            var typedMethod = typeof(MoqMockingProvider)
                .GetMethod(nameof(SetupLoggerCallbackGeneric), BindingFlags.NonPublic | BindingFlags.Static)?
                .MakeGenericMethod(mockedType);

            typedMethod?.Invoke(null, new object[] { logger, callback });
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

        private static class MoqProviderTransitionWarning
        {
            private static int _emitted;

            internal static void EmitOnce()
            {
                if (System.Threading.Interlocked.Exchange(ref _emitted, 1) == 1)
                {
                    return;
                }

                Console.Error.WriteLine("[FastMoq] Warning: FastMoq.Provider.Moq is a v4 transition dependency and will no longer be bundled by FastMoq.Core in v5. Install and select your mocking provider explicitly before upgrading.");
            }
        }
    }
}
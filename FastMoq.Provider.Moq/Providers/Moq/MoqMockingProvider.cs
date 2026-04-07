using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;

namespace FastMoq.Providers.MoqProvider
{
    public sealed class MoqMockingProvider : IMockingProvider, IMockingProviderCapabilities
    {
        public static readonly MoqMockingProvider Instance = new();

        private MoqMockingProvider()
        {
        }

        public IMockingProviderCapabilities Capabilities => this;
        public bool SupportsCallBase => true;
        public bool SupportsSetupAllProperties => true;
        public bool SupportsProtectedMembers => true;
        public bool SupportsInvocationTracking => true;
        public bool SupportsLoggerCapture => true;

        public Expression<Func<T, bool>> BuildExpression<T>()
        {
            MoqProviderTransitionWarning.EmitOnce();
            return It.IsAny<Expression<Func<T, bool>>>();
        }

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

        public void SetupAllProperties(IFastMock mock)
        {
            var underlying = TryGetUnderlyingMock(mock);
            underlying?.GetType().GetMethod("SetupAllProperties")?.Invoke(underlying, null);
        }

        public void SetCallBase(IFastMock mock, bool value)
        {
            var underlying = TryGetUnderlyingMock(mock);
            if (underlying != null)
            {
                underlying.CallBase = value;
            }
        }

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

        public void ConfigureProperties(IFastMock mock)
        {
            if (Capabilities.SupportsSetupAllProperties)
            {
                SetupAllProperties(mock);
            }
        }

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

        public object? TryGetLegacy(IFastMock mock)
        {
            MoqProviderTransitionWarning.EmitOnce();
            return TryGetUnderlyingMock(mock);
        }

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
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FastMoq.Providers;
using Moq;
using Microsoft.Extensions.Logging;

namespace FastMoq.Providers.MoqProvider
{
    internal sealed class MoqMockingProvider : IMockingProvider, IMockingProviderCapabilities
    {
        public static readonly MoqMockingProvider Instance = new();
        private MoqMockingProvider() { }
        public IMockingProviderCapabilities Capabilities => this;
        public bool SupportsCallBase => true;
        public bool SupportsSetupAllProperties => true;
        public bool SupportsProtectedMembers => true;
        public bool SupportsInvocationTracking => true;

        public IFastMock<T> CreateMock<T>(MockCreationOptions? options = null) where T : class
        {
            options ??= new();
            var behavior = options.Strict ? MockBehavior.Strict : MockBehavior.Loose; // Strict still driven by options
            var mock = options.ConstructorArgs is { Length: > 0 }
                ? new Mock<T>(behavior, options.ConstructorArgs)
                : new Mock<T>(behavior);
            if (options.CallBase) mock.CallBase = true;
            return new MoqFastMockGeneric<T>(mock);
        }

        public IFastMock CreateMock(Type type, MockCreationOptions? options = null)
        {
            options ??= new();
            var behavior = options.Strict ? MockBehavior.Strict : MockBehavior.Loose;
            var generic = typeof(Mock<>).MakeGenericType(type);
            var args = options.ConstructorArgs is { Length: > 0 }
                ? new object?[] { behavior, options.ConstructorArgs }
                : new object?[] { behavior };
            var mock = (Mock)Activator.CreateInstance(generic, args)!;
            if (options.CallBase) mock.CallBase = true;
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
            if (underlying != null) underlying.CallBase = value;
        }

        public void Verify<T>(IFastMock<T> mock, Expression<Action<T>> expression, TimesSpec? times = null) where T : class
        {
            Mock<T>? moqMock = null;
            try
            {
                moqMock = Mock.Get(mock.Instance);
            }
            catch { }
            if (moqMock == null) return;

            if (times?.Exactly is int e)
                moqMock.Verify(expression, Times.Exactly(e));
            else if (times?.AtLeast is int al)
                moqMock.Verify(expression, Times.AtLeast(al));
            else if (times?.AtMost is int am)
                moqMock.Verify(expression, Times.AtMost(am));
            else if (times?.Never == true)
                moqMock.Verify(expression, Times.Never());
            else
                moqMock.Verify(expression);
        }

        public void VerifyNoOtherCalls(IFastMock mock)
        {
            var underlying = TryGetUnderlyingMock(mock);
            if (underlying == null) return;
            var staticMethod = typeof(Mock).GetMethod("VerifyNoOtherCalls", BindingFlags.Public | BindingFlags.Static);
            if (staticMethod != null)
            {
                var parameters = staticMethod.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType.IsArray)
                {
                    staticMethod.Invoke(null, new object?[] { new Mock[] { underlying } });
                }
                else if (parameters.Length == 1)
                {
                    staticMethod.Invoke(null, new object?[] { underlying });
                }
            }
        }

        public void ConfigureProperties(IFastMock mock)
        {
            if (!Capabilities.SupportsSetupAllProperties) return;
            SetupAllProperties(mock);
        }

        public void ConfigureLogger(IFastMock mock, Action<LogLevel, EventId, string> callback)
        {
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

            var method = typeof(Mocker).GetMethod("SetupLoggerCallback", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
            {
                return;
            }

            var genericMethod = method.MakeGenericMethod(mockedType);
            genericMethod.Invoke(null, new object[] { underlying, callback });
        }

        public object? TryGetLegacy(IFastMock mock) => TryGetUnderlyingMock(mock);

        private static Mock? TryGetUnderlyingMock(IFastMock wrapper)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var type = wrapper.GetType();
            string[] propNames = ["InnerMock", "Inner"]; 
            foreach (var name in propNames)
            {
                var prop = type.GetProperty(name, Flags);
                if (prop?.GetValue(wrapper) is Mock m) return m;
            }
            try
            {
                var inst = wrapper.Instance;
                if (inst != null)
                {
                    var getMethod = typeof(Mock).GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(mi => mi.Name == "Get" && mi.IsGenericMethodDefinition && mi.GetParameters().Length == 1);
                    if (getMethod != null)
                    {
                        var constructed = getMethod.MakeGenericMethod(inst.GetType());
                        if (constructed.Invoke(null, new object[] { inst }) is Mock m2) return m2;
                    }
                }
            }
            catch { }
            return null;
        }
    }
}

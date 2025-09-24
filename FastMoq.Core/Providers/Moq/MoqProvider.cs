using System;
using System.Linq.Expressions;
using FastMoq.Providers;

namespace FastMoq.Core.Providers.MoqProvider
{
    internal sealed class MoqMockingProvider : IMockingProvider
    {
        public IMockingProviderCapabilities Capabilities => MoqCapabilities.Instance;

        IFastMock<T> IMockingProvider.CreateMock<T>(MockCreationOptions? options) => CreateMockInternal<T>(options);
        IFastMock IMockingProvider.CreateMock(Type type, MockCreationOptions? options) => CreateMockInternal(type, options);

        private MoqMockAdapter<T> CreateMockInternal<T>(MockCreationOptions? options) where T : class
        {
            var mock = CreateMoq<T>(options);
            if (!(options?.Strict ?? false) && Capabilities.SupportsSetupAllProperties)
            {
                mock.SetupAllProperties();
            }
            return new MoqMockAdapter<T>(mock);
        }

        private IFastMock CreateMockInternal(Type type, MockCreationOptions? options)
        {
            var mockType = typeof(global::Moq.Mock<>).MakeGenericType(type);
            var beh = (options?.Strict ?? false) ? global::Moq.MockBehavior.Strict : global::Moq.MockBehavior.Loose;
            var ctorArgs = options?.ConstructorArgs ?? Array.Empty<object?>();

            object? mock;
            if (options?.AllowNonPublic == true && (ctorArgs.Length == 0))
            {
                // Use Moq private ctor enabling non-public constructor resolution via flags.
                // Equivalent to new Mock<T>(behavior, defaultValue, MockBehavior, bool callBase?) pattern; we just flip Private to true via non-public overload.
                // Simplest approach: invoke (MockBehavior behavior, bool) overload then set CallBase if requested.
                mock = Activator.CreateInstance(mockType, beh, false); // second param = strictly: defaultValue? using false== DefaultValue.Empty
            }
            else
            {
                mock = Activator.CreateInstance(mockType, beh, ctorArgs);
            }
            if (mock is not global::Moq.Mock m) throw new InvalidOperationException("Unable to create mock");
            if (options?.CallBase == true && Capabilities.SupportsCallBase)
            {
                m.CallBase = true;
            }
            if (!(options?.Strict ?? false) && Capabilities.SupportsSetupAllProperties)
            {
                m.GetType().GetMethod("SetupAllProperties")?.Invoke(m, null);
            }
            return new MoqMockAdapter(m);
        }

        private static global::Moq.Mock<T> CreateMoq<T>(MockCreationOptions? options) where T : class
        {
            var behavior = (options?.Strict ?? false) ? global::Moq.MockBehavior.Strict : global::Moq.MockBehavior.Loose;
            global::Moq.Mock<T> mock;
            if (options?.AllowNonPublic == true && (options.ConstructorArgs is not { Length: > 0 }))
            {
                mock = new global::Moq.Mock<T>(behavior); // Moq will search non-public ctors if AllowNonPublic flag honored at provider-level (future extension if needed)
            }
            else if (options?.ConstructorArgs is { Length: > 0 })
            {
                mock = (global::Moq.Mock<T>)Activator.CreateInstance(typeof(global::Moq.Mock<T>), behavior, options.ConstructorArgs)!;
            }
            else
            {
                mock = new global::Moq.Mock<T>(behavior);
            }
            if (options?.CallBase == true)
            {
                mock.CallBase = true;
            }
            return mock;
        }

        public void SetupAllProperties(IFastMock mock)
        {
            if (mock is MoqMockAdapter { Inner: { } inner })
            {
                inner.GetType().GetMethod("SetupAllProperties")?.Invoke(inner, null);
            }
        }

        public void SetCallBase(IFastMock mock, bool value)
        {
            if (mock is MoqMockAdapter { Inner: { } inner })
            {
                inner.CallBase = value;
            }
        }

        void IMockingProvider.Verify<T>(IFastMock<T> mock, Expression<Action<T>> expression, TimesSpec? times)
        {
            if (mock is MoqMockAdapter<T> adapter)
            {
                var moqTimes = times switch
                {
                    { Never: true } => global::Moq.Times.Never(),
                    { Exactly: { } e } => global::Moq.Times.Exactly(e),
                    { AtLeast: { } a } => global::Moq.Times.AtLeast(a),
                    { AtMost: { } m } => global::Moq.Times.AtMost(m),
                    _ => global::Moq.Times.AtLeastOnce()
                };
                adapter.Inner.Verify(expression, moqTimes);
            }
        }

        public void VerifyNoOtherCalls(IFastMock mock) { }
        public void ConfigureProperties(IFastMock mock, bool strict)
        {
            if (strict) return;
            if (!Capabilities.SupportsSetupAllProperties) return;
            SetupAllProperties(mock);
        }
        public void ConfigureLogger(IFastMock mock, Action<Microsoft.Extensions.Logging.LogLevel, Microsoft.Extensions.Logging.EventId, string> callback) { }
        public object? TryGetLegacy(IFastMock mock)
        {
            if (mock is MoqMockAdapter mma) return mma.Inner;
            var type = mock.GetType();
            var prop = type.GetProperty("Inner") ?? type.GetProperty("InnerMock");
            return prop?.GetValue(mock);
        }
    }
}

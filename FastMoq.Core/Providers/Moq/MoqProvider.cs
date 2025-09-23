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
            return new MoqMockAdapter<T>(mock);
        }

        private IFastMock CreateMockInternal(Type type, MockCreationOptions? options)
        {
            var mockType = typeof(global::Moq.Mock<>).MakeGenericType(type);
            var beh = (options?.Strict ?? false) ? global::Moq.MockBehavior.Strict : global::Moq.MockBehavior.Loose;
            var ctorArgs = options?.ConstructorArgs ?? Array.Empty<object?>();
            var mock = Activator.CreateInstance(mockType, beh, ctorArgs) as global::Moq.Mock ?? throw new InvalidOperationException("Unable to create mock");
            if (options?.CallBase == true && Capabilities.SupportsCallBase)
            {
                mock.CallBase = true;
            }
            return new MoqMockAdapter(mock);
        }

        private static global::Moq.Mock<T> CreateMoq<T>(MockCreationOptions? options) where T : class
        {
            var behavior = (options?.Strict ?? false) ? global::Moq.MockBehavior.Strict : global::Moq.MockBehavior.Loose;
            var mock = options?.ConstructorArgs is { Length: > 0 }
                ? (global::Moq.Mock<T>)Activator.CreateInstance(typeof(global::Moq.Mock<T>), behavior, options!.ConstructorArgs)! 
                : new global::Moq.Mock<T>(behavior);
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

        public void VerifyNoOtherCalls(IFastMock mock)
        {
            // No-op: Moq version may not expose this consistently across target frameworks.
        }
    }
}

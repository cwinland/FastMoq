using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using FastMoq.Providers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;

namespace FastMoq.Core.Providers.NSubstituteProvider
{
    // Expanded NSubstitute adapter implementation with TimesSpec + basic call tracking
    internal sealed class NSubstituteMockingProvider : IMockingProvider, IMockingProviderCapabilities
    {
        public static readonly NSubstituteMockingProvider Instance = new();
        private NSubstituteMockingProvider() { }
        public IMockingProviderCapabilities Capabilities => this;

        public bool SupportsCallBase => false;
        public bool SupportsSetupAllProperties => false;
        public bool SupportsProtectedMembers => false;
        public bool SupportsInvocationTracking => true;

        // Tracks calls that have been verified so VerifyNoOtherCalls can detect extras.
        private static readonly ConcurrentDictionary<object, ConcurrentBag<ICall>> _verifiedCalls = new();

        public IFastMock<T> CreateMock<T>(MockCreationOptions? options = null) where T : class
        {
            options ??= new();
            T substitute;
            if (typeof(T).IsInterface)
            {
                substitute = Substitute.For<T>();
            }
            else
            {
                // Use non-generic overload (works for classes with virtual members); constructor args honored when possible.
                substitute = (T)Substitute.For(new[] { typeof(T) }, options.ConstructorArgs ?? Array.Empty<object?>());
            }
            return new NSubFastMock<T>(substitute);
        }

        public IFastMock CreateMock(Type type, MockCreationOptions? options = null)
        {
            options ??= new();
            object substitute = Substitute.For(new[] { type }, options.ConstructorArgs ?? Array.Empty<object?>());
            var wrapper = typeof(NSubFastMock<>).MakeGenericType(type);
            return (IFastMock)Activator.CreateInstance(wrapper, substitute)!;
        }

        public void SetupAllProperties(IFastMock mock) { }
        public void SetCallBase(IFastMock mock, bool value) { }

        public void Verify<T>(IFastMock<T> mock, Expression<Action<T>> expression, TimesSpec? times = null) where T : class
        {
            if (expression is null) return;
            times ??= default;
            var target = mock.Instance;

            if (times.Value.Never)
            {
                ExecuteWithWrapper(target.DidNotReceive, expression);
                return;
            }
            if (times.Value.Exactly is int exactly)
            {
                ExecuteWithWrapper(() => target.Received(exactly), expression);
                MarkCallsVerified(target, expression, exactly);
                return;
            }

            var (method, argCount) = ExtractMethodMeta(expression);
            if (method == null)
            {
                ExecuteWithWrapper(target.Received, expression);
                MarkCallsVerified(target, expression, 1);
                return;
            }

            var received = target.ReceivedCalls().Where(c => c.GetMethodInfo() == method && c.GetArguments().Length == argCount).ToList();
            if (times.Value.AtLeast is int atLeast)
            {
                if (received.Count < atLeast)
                    throw new InvalidOperationException($"Expected at least {atLeast} call(s) to {method.Name} but found {received.Count}.");
                ExecuteWithWrapper(target.Received, expression);
                MarkSpecificCallsVerified(target, received);
                return;
            }
            if (times.Value.AtMost is int atMost)
            {
                if (received.Count > atMost)
                    throw new InvalidOperationException($"Expected at most {atMost} call(s) to {method.Name} but found {received.Count}.");
                if (received.Count > 0)
                {
                    ExecuteWithWrapper(target.Received, expression);
                    MarkSpecificCallsVerified(target, received);
                }
                return;
            }

            if (!received.Any())
                throw new InvalidOperationException($"Expected at least one call to {method.Name} but found none.");
            ExecuteWithWrapper(target.Received, expression);
            MarkSpecificCallsVerified(target, received);
        }

        public void VerifyNoOtherCalls(IFastMock mock)
        {
            var target = mock.Instance;
            var all = target.ReceivedCalls().ToList();
            if (!_verifiedCalls.TryGetValue(target, out var verifiedBag)) return;
            var verified = verifiedBag.ToList();
            var extras = all.Except(verified).ToList();
            if (extras.Count > 0)
            {
                var summary = string.Join(", ", extras.Select(c => c.GetMethodInfo().Name));
                throw new InvalidOperationException($"Unexpected additional calls detected: {summary}");
            }
        }

        public void ConfigureProperties(IFastMock mock, bool strict) { }
        public void ConfigureLogger(IFastMock mock, Action<LogLevel, EventId, string> callback) { }
        public object? TryGetLegacy(IFastMock mock) => null;

        private static void ExecuteWithWrapper<T>(Func<T> wrapperFactory, Expression<Action<T>> expr) where T : class
        {
            var wrapper = wrapperFactory();
            expr.Compile()(wrapper);
        }

        private static (System.Reflection.MethodInfo? method, int argCount) ExtractMethodMeta<T>(Expression<Action<T>> expr)
        {
            if (expr.Body is MethodCallExpression mce) return (mce.Method, mce.Arguments.Count);
            return (null, 0);
        }

        private static void MarkCallsVerified<T>(T target, Expression<Action<T>> expr, int expected) where T : class
        {
            var (method, argCount) = ExtractMethodMeta(expr);
            if (method == null) return;
            var calls = target.ReceivedCalls().Where(c => c.GetMethodInfo() == method && c.GetArguments().Length == argCount).Take(expected).ToList();
            if (!calls.Any()) return;
            var bag = _verifiedCalls.GetOrAdd(target, _ => new ConcurrentBag<ICall>());
            foreach (var c in calls) bag.Add(c);
        }

        private static void MarkSpecificCallsVerified<T>(T target, System.Collections.Generic.IEnumerable<ICall> calls) where T : class
        {
            var list = calls.ToList();
            if (!list.Any()) return;
            var bag = _verifiedCalls.GetOrAdd(target, _ => new ConcurrentBag<ICall>());
            foreach (var c in list) bag.Add(c);
        }

        private sealed class NSubFastMock<T> : IFastMock<T> where T : class
        {
            public NSubFastMock(T instance) => Instance = instance;
            public Type MockedType => typeof(T);
            public T Instance { get; }
            public object NativeMock => Instance;
            object IFastMock.Instance => Instance!;
            public void Reset()
            {
                try
                {
                    Instance.ClearReceivedCalls();
                    _verifiedCalls.TryRemove(Instance, out _);
                }
                catch { }
            }
        }
    }
}

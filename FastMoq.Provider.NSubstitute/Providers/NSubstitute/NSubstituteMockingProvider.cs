using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;

namespace FastMoq.Providers.NSubstituteProvider
{
    public sealed class NSubstituteMockingProvider : IMockingProvider, IMockingProviderCapabilities
    {
        private static readonly ConcurrentDictionary<object, ConcurrentBag<ICall>> VerifiedCalls = new();
        private static readonly ConcurrentDictionary<object, byte> ConfiguredLoggers = new();

        public static readonly NSubstituteMockingProvider Instance = new();

        private NSubstituteMockingProvider()
        {
        }

        public IMockingProviderCapabilities Capabilities => this;
        public bool SupportsCallBase => false;
        public bool SupportsSetupAllProperties => false;
        public bool SupportsProtectedMembers => false;
        public bool SupportsInvocationTracking => true;
        public bool SupportsLoggerCapture => true;

        public Expression<Func<T, bool>> BuildExpression<T>()
        {
            return _ => true;
        }

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
                substitute = (T)Substitute.For(new[] { typeof(T) }, options.ConstructorArgs ?? Array.Empty<object?>());
            }

            return new NSubFastMock<T>(substitute);
        }

        public IFastMock CreateMock(Type type, MockCreationOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(type);

            options ??= new();
            var substitute = Substitute.For(new[] { type }, options.ConstructorArgs ?? Array.Empty<object?>());
            var wrapper = typeof(NSubFastMock<>).MakeGenericType(type);
            return (IFastMock)Activator.CreateInstance(wrapper, substitute)!;
        }

        public void SetupAllProperties(IFastMock mock)
        {
            throw CreateUnsupportedFeatureException(nameof(SupportsSetupAllProperties));
        }

        public void SetCallBase(IFastMock mock, bool value)
        {
            throw CreateUnsupportedFeatureException(nameof(SupportsCallBase));
        }

        public void Verify<T>(IFastMock<T> mock, Expression<Action<T>> expression, TimesSpec? times = null) where T : class
        {
            if (expression is null)
            {
                return;
            }

            times ??= default;
            var target = mock.Instance;

            if (times.Value.Mode == TimesSpecMode.Never)
            {
                ExecuteWithWrapper(target.DidNotReceive, expression);
                return;
            }

            if (times.Value.Mode == TimesSpecMode.Exactly)
            {
                var exactly = times.Value.Count ?? throw new InvalidOperationException("TimesSpec.Exactly requires a count.");
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

            var received = target.ReceivedCalls().Where(call => call.GetMethodInfo() == method && call.GetArguments().Length == argCount).ToList();
            if (times.Value.Mode == TimesSpecMode.AtLeast)
            {
                var atLeast = times.Value.Count ?? throw new InvalidOperationException("TimesSpec.AtLeast requires a count.");
                if (received.Count < atLeast)
                {
                    throw new InvalidOperationException($"Expected at least {atLeast} call(s) to {method.Name} but found {received.Count}.");
                }

                ExecuteWithWrapper(target.Received, expression);
                MarkSpecificCallsVerified(target, received);
                return;
            }

            if (times.Value.Mode == TimesSpecMode.AtMost)
            {
                var atMost = times.Value.Count ?? throw new InvalidOperationException("TimesSpec.AtMost requires a count.");
                if (received.Count > atMost)
                {
                    throw new InvalidOperationException($"Expected at most {atMost} call(s) to {method.Name} but found {received.Count}.");
                }

                if (received.Count > 0)
                {
                    ExecuteWithWrapper(target.Received, expression);
                    MarkSpecificCallsVerified(target, received);
                }

                return;
            }

            if (!received.Any())
            {
                throw new InvalidOperationException($"Expected at least one call to {method.Name} but found none.");
            }

            ExecuteWithWrapper(target.Received, expression);
            MarkSpecificCallsVerified(target, received);
        }

        public void VerifyNoOtherCalls(IFastMock mock)
        {
            var target = mock.Instance;
            var all = target.ReceivedCalls().ToList();
            if (!VerifiedCalls.TryGetValue(target, out var verifiedBag))
            {
                return;
            }

            var verified = verifiedBag.ToList();
            var extras = all.Except(verified).ToList();
            if (extras.Count > 0)
            {
                var summary = string.Join(", ", extras.Select(call => call.GetMethodInfo().Name));
                throw new InvalidOperationException($"Unexpected additional calls detected: {summary}");
            }
        }

        public void ConfigureProperties(IFastMock mock)
        {
            throw CreateUnsupportedFeatureException(nameof(SupportsSetupAllProperties));
        }

        public void ConfigureLogger(IFastMock mock, Action<LogLevel, EventId, string, Exception?> callback)
        {
            ArgumentNullException.ThrowIfNull(mock);
            ArgumentNullException.ThrowIfNull(callback);

            if (!ConfiguredLoggers.TryAdd(mock.Instance, 0))
            {
                return;
            }

            SetupLoggerCallback(mock.Instance, callback);
        }

        public object? TryGetLegacy(IFastMock mock) => null;

        public IFastMock? TryWrapLegacy(object legacyMock, Type mockedType) => null;

        private static void ExecuteWithWrapper<T>(Func<T> wrapperFactory, Expression<Action<T>> expression) where T : class
        {
            var wrapper = wrapperFactory();
            expression.Compile()(wrapper);
        }

        private static (System.Reflection.MethodInfo? method, int argCount) ExtractMethodMeta<T>(Expression<Action<T>> expression)
        {
            if (expression.Body is System.Linq.Expressions.MethodCallExpression methodCallExpression)
            {
                return (methodCallExpression.Method, methodCallExpression.Arguments.Count);
            }

            return (null, 0);
        }

        private static void MarkCallsVerified<T>(T target, Expression<Action<T>> expression, int expected) where T : class
        {
            var (method, argCount) = ExtractMethodMeta(expression);
            if (method == null)
            {
                return;
            }

            var calls = target.ReceivedCalls().Where(call => call.GetMethodInfo() == method && call.GetArguments().Length == argCount).Take(expected).ToList();
            if (!calls.Any())
            {
                return;
            }

            var bag = VerifiedCalls.GetOrAdd(target, _ => new ConcurrentBag<ICall>());
            foreach (var call in calls)
            {
                bag.Add(call);
            }
        }

        private static void MarkSpecificCallsVerified<T>(T target, System.Collections.Generic.IEnumerable<ICall> calls) where T : class
        {
            var callList = calls.ToList();
            if (!callList.Any())
            {
                return;
            }

            var bag = VerifiedCalls.GetOrAdd(target, _ => new ConcurrentBag<ICall>());
            foreach (var call in callList)
            {
                bag.Add(call);
            }
        }

        private static void SetupLoggerCallback(object instance, Action<LogLevel, EventId, string, Exception?> callback)
        {
            if (instance is not ILogger logger)
            {
                throw new InvalidOperationException($"Expected an ILogger-compatible substitute but received '{instance.GetType().Name}'.");
            }

            logger
                .WhenForAnyArgs(x => x.Log<object>(default, default, default!, default!, default!))
                .Do(callInfo =>
                {
                    var logLevel = callInfo.ArgAt<LogLevel>(0);
                    var eventId = callInfo.ArgAt<EventId>(1);
                    var state = callInfo.Args()[2];
                    var exception = callInfo.Args()[3] as Exception;
                    var formatter = callInfo.Args()[4] as Delegate;
                    var message = formatter?.DynamicInvoke(state, exception)?.ToString() ?? state?.ToString() ?? string.Empty;
                    callback(logLevel, eventId, message, exception);
                });
        }

        private static NotSupportedException CreateUnsupportedFeatureException(string capabilityName) =>
            new($"Provider '{nameof(NSubstituteMockingProvider)}' does not support '{capabilityName}'. Guard with '{nameof(IMockingProviderCapabilities)}' before calling provider-specific capability methods.");

        private sealed class NSubFastMock<T> : IFastMock<T> where T : class
        {
            public NSubFastMock(T instance)
            {
                Instance = instance;
            }

            public Type MockedType => typeof(T);
            public T Instance { get; }
            public object NativeMock => Instance;
            object IFastMock.Instance => Instance!;

            public void Reset()
            {
                try
                {
                    Instance.ClearReceivedCalls();
                    VerifiedCalls.TryRemove(Instance, out _);
                    ConfiguredLoggers.TryRemove(Instance, out _);
                }
                catch
                {
                }
            }
        }
    }
}
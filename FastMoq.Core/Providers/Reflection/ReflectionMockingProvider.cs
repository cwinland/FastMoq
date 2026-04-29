using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Providers.ReflectionProvider
{
    /// <summary>
    /// Minimal dependency-free reflection based mocking provider.
    /// Interface-only interception (uses DispatchProxy) with simple invocation tracking for verification.
    /// Does NOT support: strict mode, call base, property auto-setup, protected members.
    /// Intended as a fallback / baseline provider so FastMoq can operate without external mocking libraries.
    /// </summary>
    internal sealed class ReflectionMockingProvider : IMockingProvider, IMockingProviderCapabilities, ITrackedMockPropertyConfigurator
    {
        public static readonly ReflectionMockingProvider Instance = new();
        private ReflectionMockingProvider() { }

        public IMockingProviderCapabilities Capabilities => this;
        public bool SupportsCallBase => false;
        public bool SupportsSetupAllProperties => false;
        public bool SupportsProtectedMembers => false;
        public bool SupportsInvocationTracking => true;
        public bool SupportsLoggerCapture => false;

        public Expression<Func<T, bool>> BuildExpression<T>()
        {
            return FastArg.AnyExpression<T>();
        }

        // Target instance -> invocation records
        private static readonly ConcurrentDictionary<object, List<InvocationRecord>> _invocations = new();

        #region Creation
        public IFastMock<T> CreateMock<T>(MockCreationOptions? options = null) where T : class
        {
            if (!typeof(T).IsInterface)
            {
                // Non-interface fallback: attempt parameterless construction (no interception)
                var inst = TryCreateConcrete(typeof(T)) ?? throw new NotSupportedException($"Reflection provider only supports interfaces or classes with a public parameterless constructor. Type: {typeof(T).Name}");
                return new ReflectionFastMock<T>((T) inst, TrackNoOp);
            }
            var proxy = DispatchProxy.Create<T, TrackingDispatchProxy>();
            ((TrackingDispatchProxy) (object) proxy!).Initialize((m, a, r) => Track(proxy!, m, a));
            return new ReflectionFastMock<T>(proxy!, (m, a, r) => Track(proxy!, m, a));
        }

        public IFastMock CreateMock(Type type, MockCreationOptions? options = null)
        {
            var method = typeof(ReflectionMockingProvider).GetMethod(nameof(CreateGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(type);
            return (IFastMock) method.Invoke(this, new object?[] { options })!;
        }

        private IFastMock CreateGeneric<T>(MockCreationOptions? options) where T : class => CreateMock<T>(options);
        #endregion

        #region Configuration (unsupported direct calls)
        public void SetupAllProperties(IFastMock mock) => throw CreateUnsupportedFeatureException(nameof(SupportsSetupAllProperties));
        public void SetCallBase(IFastMock mock, bool value) => throw CreateUnsupportedFeatureException(nameof(SupportsCallBase));
        public void ConfigureProperties(IFastMock mock) => throw CreateUnsupportedFeatureException(nameof(SupportsSetupAllProperties));
        public void ConfigureLogger(IFastMock mock, Action<LogLevel, EventId, string, Exception?> callback) => throw CreateUnsupportedFeatureException(nameof(SupportsLoggerCapture));

        public bool TryConfigureMockProperty(IFastMock mock, PropertyInfo propertyInfo, object? value)
        {
            return false;
        }
        #endregion

        #region Verification
        public void Verify<T>(IFastMock<T> mock, Expression<Action<T>> expression, TimesSpec? times = null) where T : class
        {
            ArgumentNullException.ThrowIfNull(expression);

            var invocation = FastArgExpressionParser.ParseInvocation(expression);
            var records = GetInvocations(mock.Instance)
                .Where(r => invocation.Matches(r.Method, r.Arguments))
                .ToList();

            times ??= default;
            if (times.Value.Mode == TimesSpecMode.Never)
            {
                if (records.Count > 0) throw new InvalidOperationException($"Expected no calls to {invocation.Method.Name} but found {records.Count}.");
                return;
            }
            if (times.Value.Mode == TimesSpecMode.Exactly)
            {
                var e = times.Value.Count ?? throw new InvalidOperationException("TimesSpec.Exactly requires a count.");
                if (records.Count != e) throw new InvalidOperationException($"Expected exactly {e} call(s) to {invocation.Method.Name} but found {records.Count}.");
                MarkVerified(records);
                return;
            }
            if (times.Value.Mode == TimesSpecMode.AtLeast)
            {
                var al = times.Value.Count ?? throw new InvalidOperationException("TimesSpec.AtLeast requires a count.");
                if (records.Count < al) throw new InvalidOperationException($"Expected at least {al} call(s) to {invocation.Method.Name} but found {records.Count}.");
                MarkVerified(records);
                return;
            }
            if (times.Value.Mode == TimesSpecMode.AtMost)
            {
                var am = times.Value.Count ?? throw new InvalidOperationException("TimesSpec.AtMost requires a count.");
                if (records.Count > am) throw new InvalidOperationException($"Expected at most {am} call(s) to {invocation.Method.Name} but found {records.Count}.");
                MarkVerified(records);
                return;
            }
            if (records.Count == 0) throw new InvalidOperationException($"Expected at least one call to {invocation.Method.Name} but none were recorded.");
            MarkVerified(records);
        }

        public void VerifyMethod<T>(IFastMock<T> mock, MethodInfo method, TimesSpec? times = null) where T : class
        {
            ArgumentNullException.ThrowIfNull(method);

            var records = GetInvocations(mock.Instance)
                .Where(record => MethodsMatch(record.Method, method))
                .ToList();

            times ??= default;
            if (times.Value.Mode == TimesSpecMode.Never)
            {
                if (records.Count > 0)
                {
                    throw new InvalidOperationException($"Expected no calls to {method.Name} but found {records.Count}.");
                }

                return;
            }

            if (times.Value.Mode == TimesSpecMode.Exactly)
            {
                var expected = times.Value.Count ?? throw new InvalidOperationException("TimesSpec.Exactly requires a count.");
                if (records.Count != expected)
                {
                    throw new InvalidOperationException($"Expected exactly {expected} call(s) to {method.Name} but found {records.Count}.");
                }

                MarkVerified(records);
                return;
            }

            if (times.Value.Mode == TimesSpecMode.AtLeast)
            {
                var minimum = times.Value.Count ?? throw new InvalidOperationException("TimesSpec.AtLeast requires a count.");
                if (records.Count < minimum)
                {
                    throw new InvalidOperationException($"Expected at least {minimum} call(s) to {method.Name} but found {records.Count}.");
                }

                MarkVerified(records);
                return;
            }

            if (times.Value.Mode == TimesSpecMode.AtMost)
            {
                var maximum = times.Value.Count ?? throw new InvalidOperationException("TimesSpec.AtMost requires a count.");
                if (records.Count > maximum)
                {
                    throw new InvalidOperationException($"Expected at most {maximum} call(s) to {method.Name} but found {records.Count}.");
                }

                MarkVerified(records);
                return;
            }

            if (records.Count == 0)
            {
                throw new InvalidOperationException($"Expected at least one call to {method.Name} but none were recorded.");
            }

            MarkVerified(records);
        }

        public void VerifyNoOtherCalls(IFastMock mock)
        {
            var inv = GetInvocations(mock.Instance);
            var extras = inv.Where(r => !r.Verified).ToList();
            if (extras.Count == 0) return;
            var summary = string.Join(", ", extras.Select(e => e.Method.Name));
            throw new InvalidOperationException($"Unexpected additional calls: {summary}");
        }
        #endregion

        public object? TryGetLegacy(IFastMock mock) => null; // No legacy surface

        public IFastMock? TryWrapLegacy(object legacyMock, Type mockedType) => null;

        #region Invocation Tracking Helpers
        private static void Track(object target, MethodInfo method, object?[] args)
        {
            var list = _invocations.GetOrAdd(target, _ => new List<InvocationRecord>());
            lock (list)
            {
                list.Add(new InvocationRecord(method, args));
            }
        }
        private static void TrackNoOp(MethodInfo m, object?[] a, object? r) { }
        private static List<InvocationRecord> GetInvocations(object target) => _invocations.GetOrAdd(target, _ => new List<InvocationRecord>());
        private static void MarkVerified(IEnumerable<InvocationRecord> list)
        {
            foreach (var r in list) r.Verified = true;
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

        private sealed record InvocationRecord(MethodInfo Method, object?[] Arguments)
        {
            public bool Verified { get; set; }
        }
        #endregion

        #region Internal FastMock Wrapper
        private sealed class ReflectionFastMock<T> : IFastMock<T>, IProviderBoundFastMock where T : class
        {
            private readonly Action<MethodInfo, object?[], object?> _tracker;

            public ReflectionFastMock(T instance, Action<MethodInfo, object?[], object?> tracker)
            {
                Instance = instance;
                _tracker = tracker;
            }

            public ReflectionFastMock(T instance, Action<object, MethodInfo, object?[]> tracker)
            {
                Instance = instance;
                _tracker = (m, a, r) => tracker(instance!, m, a);
            }

            public Type MockedType => typeof(T);

            public IMockingProvider Provider => ReflectionMockingProvider.Instance;

            public T Instance { get; }

            public object NativeMock => Instance;

            object IFastMock.Instance => Instance!;

            public void Reset()
            {
                _invocations.TryRemove(Instance, out _);
            }

            public void Track(MethodInfo method, object?[] args, object? returnValue) => _tracker(method, args, returnValue);
        }

        private class TrackingDispatchProxy : DispatchProxy
        {
            private Action<MethodInfo, object?[], object?> _onInvoke = default!;
            internal void Initialize(Action<MethodInfo, object?[], object?> onInvoke) => _onInvoke = onInvoke;
            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                if (targetMethod == null) return null;
                var result = targetMethod.ReturnType != typeof(void)
                    ? (targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null)
                    : null;
                _onInvoke(targetMethod, args ?? Array.Empty<object?>(), result);
                return result;
            }
        }
        #endregion

        private static object? TryCreateConcrete(Type type)
        {
            try
            {
                if (type.IsAbstract) return null;
                var ctor = type.GetConstructor(Type.EmptyTypes);
                return ctor?.Invoke(null);
            }
            catch { return null; }
        }

        private static NotSupportedException CreateUnsupportedFeatureException(string capabilityName) =>
            new($"Provider '{nameof(ReflectionMockingProvider)}' does not support '{capabilityName}'. Guard with '{nameof(IMockingProviderCapabilities)}' before calling provider-specific capability methods.");
    }
}

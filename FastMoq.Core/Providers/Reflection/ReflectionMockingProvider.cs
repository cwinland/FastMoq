using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FastMoq.Providers;
using Microsoft.Extensions.Logging;

namespace FastMoq.Providers.ReflectionProvider
{
    /// <summary>
    /// Minimal dependency-free reflection based mocking provider.
    /// Interface-only interception (uses DispatchProxy) with simple invocation tracking for verification.
    /// Does NOT support: strict mode, call base, property auto-setup, protected members.
    /// Intended as a fallback / baseline provider so FastMoq can operate without external mocking libraries.
    /// </summary>
    internal sealed class ReflectionMockingProvider : IMockingProvider, IMockingProviderCapabilities
    {
        public static readonly ReflectionMockingProvider Instance = new();
        private ReflectionMockingProvider() { }

        public IMockingProviderCapabilities Capabilities => this;
        public bool SupportsCallBase => false;
        public bool SupportsSetupAllProperties => false;
        public bool SupportsProtectedMembers => false;
        public bool SupportsInvocationTracking => true;

        // Target instance -> invocation records
        private static readonly ConcurrentDictionary<object, List<InvocationRecord>> _invocations = new();

        #region Creation
        public IFastMock<T> CreateMock<T>(MockCreationOptions? options = null) where T : class
        {
            if (!typeof(T).IsInterface)
            {
                // Non-interface fallback: attempt parameterless construction (no interception)
                var inst = TryCreateConcrete(typeof(T)) ?? throw new NotSupportedException($"Reflection provider only supports interfaces or classes with a public parameterless constructor. Type: {typeof(T).Name}");
                return new ReflectionFastMock<T>((T)inst, TrackNoOp);
            }
            var proxy = DispatchProxy.Create<T, TrackingDispatchProxy>();
            ((TrackingDispatchProxy)(object)proxy!).Initialize((m, a, r) => Track(proxy!, m, a));
            return new ReflectionFastMock<T>(proxy!, (m, a, r) => Track(proxy!, m, a));
        }

        public IFastMock CreateMock(Type type, MockCreationOptions? options = null)
        {
            var method = typeof(ReflectionMockingProvider).GetMethod(nameof(CreateGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(type);
            return (IFastMock)method.Invoke(this, new object?[] { options })!;
        }

        private IFastMock CreateGeneric<T>(MockCreationOptions? options) where T : class => CreateMock<T>(options);
        #endregion

        #region Configuration (no-ops)
        public void SetupAllProperties(IFastMock mock) { }
        public void SetCallBase(IFastMock mock, bool value) { }
        public void ConfigureProperties(IFastMock mock, bool strict) { }
        public void ConfigureLogger(IFastMock mock, Action<LogLevel, EventId, string> callback) { }
        #endregion

        #region Verification
        public void Verify<T>(IFastMock<T> mock, Expression<Action<T>> expression, TimesSpec? times = null) where T : class
        {
            if (expression.Body is not MethodCallExpression mce)
                throw new NotSupportedException("Only direct method call expressions are supported by the reflection provider.");

            var records = GetInvocations(mock.Instance)
                .Where(r => r.Method == mce.Method && ArgumentsMatch(r.Arguments, mce.Arguments))
                .ToList();

            times ??= default;
            if (times.Value.Never)
            {
                if (records.Count > 0) throw new InvalidOperationException($"Expected no calls to {mce.Method.Name} but found {records.Count}.");
                return;
            }
            if (times.Value.Exactly is int e)
            {
                if (records.Count != e) throw new InvalidOperationException($"Expected exactly {e} call(s) to {mce.Method.Name} but found {records.Count}.");
                MarkVerified(records);
                return;
            }
            if (times.Value.AtLeast is int al)
            {
                if (records.Count < al) throw new InvalidOperationException($"Expected at least {al} call(s) to {mce.Method.Name} but found {records.Count}.");
                MarkVerified(records);
                return;
            }
            if (times.Value.AtMost is int am)
            {
                if (records.Count > am) throw new InvalidOperationException($"Expected at most {am} call(s) to {mce.Method.Name} but found {records.Count}.");
                MarkVerified(records);
                return;
            }
            if (records.Count == 0) throw new InvalidOperationException($"Expected at least one call to {mce.Method.Name} but none were recorded.");
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
        private static bool ArgumentsMatch(IReadOnlyList<object?> recorded, IReadOnlyList<Expression> expected)
        {
            if (recorded.Count != expected.Count) return false;
            for (int i = 0; i < recorded.Count; i++)
            {
                if (expected[i] is ConstantExpression ce && !Equals(ce.Value, recorded[i])) return false;
            }
            return true; // best-effort (no advanced matchers)
        }
        private sealed record InvocationRecord(MethodInfo Method, object?[] Arguments)
        {
            public bool Verified { get; set; }
        }
        #endregion

        #region Internal FastMock Wrapper
        private sealed class ReflectionFastMock<T> : IFastMock<T> where T : class
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
            public T Instance { get; }
            object IFastMock.Instance => Instance!;
            public void Reset() { }
            public void Track(MethodInfo method, object?[] args, object? returnValue) => _tracker(method, args, returnValue);
        }

        private sealed class TrackingDispatchProxy : DispatchProxy
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
    }
}

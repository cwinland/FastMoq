using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Providers.NSubstituteProvider
{
    /// <summary>
    /// Provider implementation that adapts NSubstitute to the provider-neutral FastMoq abstractions.
    /// </summary>
    public sealed class NSubstituteMockingProvider : IMockingProvider, IMockingProviderCapabilities, ITrackedMockPropertyConfigurator
    {
        private static readonly ConcurrentDictionary<object, ConcurrentBag<ICall>> VerifiedCalls = new();
        private static readonly ConcurrentDictionary<object, byte> ConfiguredLoggers = new();
        private static readonly MethodInfo? NSubstituteReturnsMethod = Type.GetType("NSubstitute.SubstituteExtensions, NSubstitute")
            ?.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(method =>
                method.Name == "Returns" &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 3 &&
                method.GetParameters()[0].ParameterType.IsGenericParameter &&
                method.GetParameters()[1].ParameterType.IsGenericParameter &&
                method.GetParameters()[2].ParameterType.IsArray &&
                method.GetParameters()[2].ParameterType.GetElementType()?.IsGenericParameter == true);

        /// <summary>
        /// Gets the shared singleton instance of the NSubstitute provider.
        /// </summary>
        public static readonly NSubstituteMockingProvider Instance = new();

        private NSubstituteMockingProvider()
        {
        }

        /// <summary>
        /// Gets the capability descriptor for this provider.
        /// </summary>
        public IMockingProviderCapabilities Capabilities => this;

        /// <summary>
        /// Gets a value indicating whether NSubstitute supports base-call behavior.
        /// </summary>
        public bool SupportsCallBase => false;

        /// <summary>
        /// Gets a value indicating whether NSubstitute supports automatic property backing.
        /// </summary>
        public bool SupportsSetupAllProperties => false;

        /// <summary>
        /// Gets a value indicating whether NSubstitute supports protected member interception.
        /// </summary>
        public bool SupportsProtectedMembers => false;

        /// <summary>
        /// Gets a value indicating whether NSubstitute supports invocation tracking.
        /// </summary>
        public bool SupportsInvocationTracking => true;

        /// <summary>
        /// Gets a value indicating whether NSubstitute supports logger capture helpers.
        /// </summary>
        public bool SupportsLoggerCapture => true;

        /// <summary>
        /// Builds a predicate expression suitable for provider-neutral matching.
        /// </summary>
        public Expression<Func<T, bool>> BuildExpression<T>()
        {
            return FastArg.AnyExpression<T>();
        }

        /// <summary>
        /// Creates a typed FastMoq wrapper around a new NSubstitute substitute.
        /// </summary>
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
                substitute = (T) Substitute.For(new[] { typeof(T) }, options.ConstructorArgs ?? Array.Empty<object?>());
            }

            return new NSubFastMock<T>(substitute);
        }

        /// <summary>
        /// Creates an untyped FastMoq wrapper around a new NSubstitute substitute.
        /// </summary>
        public IFastMock CreateMock(Type type, MockCreationOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(type);

            options ??= new();
            var substitute = Substitute.For(new[] { type }, options.ConstructorArgs ?? Array.Empty<object?>());
            var wrapper = typeof(NSubFastMock<>).MakeGenericType(type);
            return (IFastMock) Activator.CreateInstance(wrapper, substitute)!;
        }

        /// <summary>
        /// Requests property auto-configuration on the supplied mock.
        /// </summary>
        public void SetupAllProperties(IFastMock mock)
        {
            throw CreateUnsupportedFeatureException(nameof(SupportsSetupAllProperties));
        }

        /// <summary>
        /// Requests base-call behavior on the supplied mock.
        /// </summary>
        public void SetCallBase(IFastMock mock, bool value)
        {
            throw CreateUnsupportedFeatureException(nameof(SupportsCallBase));
        }

        /// <summary>
        /// Verifies that the supplied expression was invoked on the wrapped NSubstitute mock.
        /// </summary>
        public void Verify<T>(IFastMock<T> mock, Expression<Action<T>> expression, TimesSpec? times = null) where T : class
        {
            ArgumentNullException.ThrowIfNull(expression);

            times ??= default;
            var target = mock.Instance;

            if (TryGetMatchingReceivedCalls(target, expression, out var invocation, out var received))
            {
                AssertExpectedInvocationCount(invocation.Method.Name, received.Count, times.Value);
                if (times.Value.Mode != TimesSpecMode.Never && received.Count > 0)
                {
                    MarkSpecificCallsVerified(target, received);
                }

                return;
            }

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

            var methodCalls = target.ReceivedCalls().Where(call => call.GetMethodInfo() == method && call.GetArguments().Length == argCount).ToList();
            if (times.Value.Mode == TimesSpecMode.AtLeast)
            {
                var atLeast = times.Value.Count ?? throw new InvalidOperationException("TimesSpec.AtLeast requires a count.");
                if (methodCalls.Count < atLeast)
                {
                    throw new InvalidOperationException($"Expected at least {atLeast} call(s) to {method.Name} but found {methodCalls.Count}.");
                }

                ExecuteWithWrapper(target.Received, expression);
                MarkSpecificCallsVerified(target, methodCalls);
                return;
            }

            if (times.Value.Mode == TimesSpecMode.AtMost)
            {
                var atMost = times.Value.Count ?? throw new InvalidOperationException("TimesSpec.AtMost requires a count.");
                if (methodCalls.Count > atMost)
                {
                    throw new InvalidOperationException($"Expected at most {atMost} call(s) to {method.Name} but found {methodCalls.Count}.");
                }

                if (methodCalls.Count > 0)
                {
                    ExecuteWithWrapper(target.Received, expression);
                    MarkSpecificCallsVerified(target, methodCalls);
                }

                return;
            }

            if (!methodCalls.Any())
            {
                throw new InvalidOperationException($"Expected at least one call to {method.Name} but found none.");
            }

            ExecuteWithWrapper(target.Received, expression);
            MarkSpecificCallsVerified(target, methodCalls);
        }

        /// <summary>
        /// Verifies that no unverified calls remain on the supplied mock.
        /// </summary>
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

        /// <summary>
        /// Configures property behavior on the supplied mock when the provider supports it.
        /// </summary>
        public void ConfigureProperties(IFastMock mock)
        {
            throw CreateUnsupportedFeatureException(nameof(SupportsSetupAllProperties));
        }

        /// <summary>
        /// Configures logger callback capture on the supplied mock.
        /// </summary>
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

        /// <summary>
        /// Attempts to configure a single public property getter on the wrapped NSubstitute substitute.
        /// </summary>
        public bool TryConfigureMockProperty(IFastMock mock, PropertyInfo propertyInfo, object? value)
        {
            ArgumentNullException.ThrowIfNull(mock);
            ArgumentNullException.ThrowIfNull(propertyInfo);

            if (NSubstituteReturnsMethod is null || propertyInfo.GetMethod is null || !propertyInfo.GetMethod.IsPublic)
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
                var getterResult = propertyInfo.GetMethod.Invoke(mock.Instance, Array.Empty<object?>());
                var closedReturnsMethod = NSubstituteReturnsMethod.MakeGenericMethod(propertyInfo.PropertyType);
                var emptyReturnSequence = Array.CreateInstance(propertyInfo.PropertyType, 0);
                closedReturnsMethod.Invoke(null, [getterResult, value, emptyReturnSequence]);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to expose a provider-specific legacy mock object from a wrapper.
        /// </summary>
        public object? TryGetLegacy(IFastMock mock) => null;

        /// <summary>
        /// Attempts to wrap a provider-specific legacy mock object in the FastMoq abstraction.
        /// </summary>
        public IFastMock? TryWrapLegacy(object legacyMock, Type mockedType) => null;

        private static void ExecuteWithWrapper<T>(Func<T> wrapperFactory, Expression<Action<T>> expression) where T : class
        {
            var wrapper = wrapperFactory();
            expression.Compile()(wrapper);
        }

        private static bool TryGetMatchingReceivedCalls<T>(T target, Expression<Action<T>> expression, out FastInvocationMatcher invocation, out List<ICall> received) where T : class
        {
            invocation = null!;
            received = new List<ICall>();

            if (ContainsProviderSpecificMatcher(expression.Body))
            {
                return false;
            }

            if (expression.Body is not MethodCallExpression)
            {
                return false;
            }
            try
            {
                invocation = FastArgExpressionParser.ParseInvocation(expression);
            }
            catch (Exception)
            {
                return false;
            }

            var parsedInvocation = invocation;
            received = target.ReceivedCalls()
                .Where(call => parsedInvocation.Matches(call.GetMethodInfo(), call.GetArguments()))
                .ToList();
            return true;
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
            if (TryGetMatchingReceivedCalls(target, expression, out _, out var matchedCalls))
            {
                MarkSpecificCallsVerified(target, matchedCalls.Take(expected));
                return;
            }

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

        private static bool ContainsProviderSpecificMatcher(Expression expression)
        {
            return new ProviderSpecificMatcherDetector().ContainsMatcher(expression);
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

        private sealed class ProviderSpecificMatcherDetector : ExpressionVisitor
        {
            private const string MoqItTypeName = "Moq.It";
            private const string NSubstituteArgTypeName = "NSubstitute.Arg";

            private bool _containsMatcher;

            public bool ContainsMatcher(Expression expression)
            {
                Visit(expression);
                return _containsMatcher;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var declaringTypeName = node.Method.DeclaringType?.FullName;
                if (declaringTypeName is MoqItTypeName or NSubstituteArgTypeName)
                {
                    _containsMatcher = true;
                    return node;
                }

                return base.VisitMethodCall(node);
            }
        }

        private sealed class NSubFastMock<T> : IFastMock<T>, IProviderBoundFastMock where T : class
        {
            public NSubFastMock(T instance)
            {
                Instance = instance;
            }

            public Type MockedType => typeof(T);
            public IMockingProvider Provider => NSubstituteMockingProvider.Instance;
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
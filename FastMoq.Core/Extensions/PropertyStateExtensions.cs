using System.Reflection;

namespace FastMoq.Extensions
{
    /// <summary>
    /// Provides first-party helpers for lightweight stateful interface properties without relying on provider-specific <c>SetupAllProperties()</c> behavior.
    /// </summary>
    public static class PropertyStateExtensions
    {
        /// <summary>
        /// Replaces the current interface registration with a proxy that preserves assignments for all readable and writable non-indexer properties while forwarding unrelated members to the previously resolved instance.
        /// </summary>
        /// <typeparam name="TService">The interface type to wrap.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="replace">True to replace an existing registration for <typeparamref name="TService" />. Defaults to <see langword="true" /> because the helper intentionally swaps in a property-state proxy.</param>
        /// <returns>The proxy-backed instance now registered for <typeparamref name="TService" />.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var channel = Mocks.AddPropertyState<IOrderSubmissionChannel>();
        /// CreateComponent();
        ///
        /// await Component.SubmitAsync("order-42", expedited: true, CancellationToken.None);
        ///
        /// channel.Mode.Should().Be("fast");
        /// ]]></code>
        /// </example>
        /// <remarks>
        /// This overload preserves the original write-through behavior by using <see cref="PropertyStateMode.WriteThrough" />.
        ///
        /// When you use this helper from a <c>MockerTestBase&lt;TComponent&gt;</c>-based test, add the helper during the setup phase or call <c>CreateComponent()</c> after the registration change so the component is rebuilt against the proxy-wrapped dependency.
        /// </remarks>
        public static TService AddPropertyState<TService>(this Mocker mocker, bool replace = true)
            where TService : class
        {
            return mocker.AddPropertyState<TService>(PropertyStateMode.WriteThrough, replace);
        }

        /// <summary>
        /// Replaces the current interface registration with a proxy that preserves assignments for all readable and writable non-indexer properties while forwarding unrelated members to the previously resolved instance.
        /// </summary>
        /// <typeparam name="TService">The interface type to wrap.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="mode">Controls whether property assignments also write through to the wrapped inner instance or stay on the proxy only.</param>
        /// <param name="replace">True to replace an existing registration for <typeparamref name="TService" />. Defaults to <see langword="true" /> because the helper intentionally swaps in a property-state proxy.</param>
        /// <returns>The proxy-backed instance now registered for <typeparamref name="TService" />.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var channel = Mocks.AddPropertyState<IOrderSubmissionChannel>(PropertyStateMode.ProxyOnly);
        /// CreateComponent();
        ///
        /// await Component.SubmitAsync("order-42", expedited: true, CancellationToken.None);
        ///
        /// channel.Mode.Should().Be("fast");
        /// ]]></code>
        /// </example>
        /// <remarks>
        /// Use <see cref="PropertyStateMode.ProxyOnly" /> when the test needs detached property state on the proxy registration without mutating the previously wrapped instance.
        ///
        /// When you use this helper from a <c>MockerTestBase&lt;TComponent&gt;</c>-based test, add the helper during the setup phase or call <c>CreateComponent()</c> after the registration change so the component is rebuilt against the proxy-wrapped dependency.
        /// </remarks>
        public static TService AddPropertyState<TService>(this Mocker mocker, PropertyStateMode mode, bool replace = true)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(mocker);

            var serviceType = typeof(TService);
            if (!serviceType.IsInterface)
            {
                throw new NotSupportedException($"{nameof(AddPropertyState)} currently supports interface types only. Use a fake or stub plus {nameof(Mocker.AddType)}(...) for {serviceType.Name}.");
            }

            var currentInstance = mocker.GetObject<TService>() ?? throw new InvalidOperationException($"Unable to resolve an instance for {serviceType.Name} before adding property state.");
            if (currentInstance is PropertyStateProxy<TService> existingProxy)
            {
                existingProxy.SetPropertyStateMode(mode);
                existingProxy.EnableAutomaticPropertyState();
                return currentInstance;
            }

            var proxy = DispatchProxy.Create<TService, PropertyStateProxy<TService>>();
            var proxyController = (PropertyStateProxy<TService>) (object) proxy;
            proxyController.Initialize(currentInstance);
            proxyController.SetPropertyStateMode(mode);
            proxyController.EnableAutomaticPropertyState();

            mocker.AddType<TService>(proxy, replace);
            return proxy;
        }
    }

    internal class PropertyStateProxy<TService> : DispatchProxy where TService : class
    {
        private readonly Dictionary<MethodInfo, PropertyStateRegistration> _propertyRegistrations = [];

        private TService? _inner;
        private PropertyStateMode _propertyStateMode = PropertyStateMode.WriteThrough;

        public void Initialize(TService inner)
        {
            ArgumentNullException.ThrowIfNull(inner);
            _inner = inner;
        }

        public void EnableAutomaticPropertyState()
        {
            foreach (var propertyInfo in typeof(TService).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propertyInfo.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                if (propertyInfo.GetMethod is null || propertyInfo.SetMethod is null)
                {
                    continue;
                }

                EnsurePropertyRegistration(propertyInfo);
            }
        }

        public void SetPropertyStateMode(PropertyStateMode propertyStateMode)
        {
            _propertyStateMode = propertyStateMode;
        }

        public void AddCapture<TValue>(PropertyInfo propertyInfo, PropertyValueCapture<TValue> capture)
        {
            ArgumentNullException.ThrowIfNull(propertyInfo);
            ArgumentNullException.ThrowIfNull(capture);

            var registration = EnsurePropertyRegistration(propertyInfo);
            registration.AddObserver(value => capture.Record(ConvertAssignedValue<TValue>(value)));
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (_propertyRegistrations.TryGetValue(targetMethod, out var registration))
            {
                return registration.Invoke(_inner, targetMethod, args, _propertyStateMode == PropertyStateMode.WriteThrough);
            }

            if (_inner is null)
            {
                throw new InvalidOperationException($"{nameof(PropertyStateProxy<TService>)} has not been initialized.");
            }

            try
            {
                return targetMethod.Invoke(_inner, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }
        }

        private PropertyStateRegistration EnsurePropertyRegistration(PropertyInfo propertyInfo)
        {
            ArgumentNullException.ThrowIfNull(propertyInfo);

            if (propertyInfo.SetMethod is null)
            {
                throw new ArgumentException($"Property '{propertyInfo.Name}' must have a setter.", nameof(propertyInfo));
            }

            if (propertyInfo.GetMethod is null)
            {
                throw new ArgumentException($"Property '{propertyInfo.Name}' must have a getter.", nameof(propertyInfo));
            }

            if (_propertyRegistrations.TryGetValue(propertyInfo.GetMethod, out var existingRegistration))
            {
                return existingRegistration;
            }

            var registration = new PropertyStateRegistration(propertyInfo);
            _propertyRegistrations[propertyInfo.GetMethod] = registration;
            _propertyRegistrations[propertyInfo.SetMethod] = registration;
            return registration;
        }

        private static TValue ConvertAssignedValue<TValue>(object? value)
        {
            if (value is null)
            {
                return default!;
            }

            return (TValue) value;
        }

        private sealed class PropertyStateRegistration
        {
            private readonly List<Action<object?>> _observers = [];

            public PropertyStateRegistration(PropertyInfo propertyInfo)
            {
                PropertyInfo = propertyInfo;
            }

            public PropertyInfo PropertyInfo { get; }

            public bool HasAssignedValue { get; private set; }

            public object? AssignedValue { get; private set; }

            public void AddObserver(Action<object?> observer)
            {
                ArgumentNullException.ThrowIfNull(observer);
                _observers.Add(observer);
            }

            public object? Invoke(object? inner, MethodInfo targetMethod, object?[]? args, bool writeThroughToInner)
            {
                if (targetMethod == PropertyInfo.GetMethod)
                {
                    if (HasAssignedValue)
                    {
                        return AssignedValue;
                    }

                    if (inner is null)
                    {
                        return GetDefaultValue();
                    }

                    return PropertyInfo.GetValue(inner);
                }

                if (targetMethod == PropertyInfo.SetMethod)
                {
                    var assignedValue = args is not null && args.Length > 0
                        ? args[0]
                        : GetDefaultValue();

                    AssignedValue = assignedValue;
                    HasAssignedValue = true;

                    foreach (var observer in _observers)
                    {
                        observer(assignedValue);
                    }

                    if (writeThroughToInner && inner is not null)
                    {
                        PropertyInfo.SetValue(inner, assignedValue);
                    }

                    return null;
                }

                throw new InvalidOperationException($"Method '{targetMethod.Name}' is not registered for property state on '{PropertyInfo.Name}'.");
            }

            private object? GetDefaultValue()
            {
                return PropertyInfo.PropertyType.IsValueType
                    ? Activator.CreateInstance(PropertyInfo.PropertyType)
                    : null;
            }
        }
    }
}
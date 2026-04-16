using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Extensions
{
    /// <summary>
    /// Provides first-party helpers for capturing interface property assignments without relying on provider-specific <c>SetupSet(...)</c> behavior.
    /// </summary>
    public static class PropertySetterCaptureExtensions
    {
        /// <summary>
        /// Replaces the current interface registration with a proxy that captures assignments to the selected property while forwarding unrelated members to the previously resolved instance.
        /// </summary>
        /// <typeparam name="TService">The interface type to wrap.</typeparam>
        /// <typeparam name="TValue">The property value type.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="propertyExpression">The interface property whose setter should be captured.</param>
        /// <param name="replace">True to replace an existing registration for <typeparamref name="TService" />. Defaults to <see langword="true" /> because the helper intentionally swaps in a capture proxy.</param>
        /// <returns>A <see cref="PropertyValueCapture{TValue}" /> that records the assigned values.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var modeCapture = Mocks.AddPropertySetterCapture<IOrderGateway, string?>(x => x.Mode);
        ///
        /// await Component.RunAsync();
        ///
        /// modeCapture.Value.Should().Be("fast");
        /// ]]></code>
        /// </example>
        public static PropertyValueCapture<TValue> AddPropertySetterCapture<TService, TValue>(this Mocker mocker, Expression<Func<TService, TValue>> propertyExpression, bool replace = true)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(propertyExpression);

            return mocker.AddPropertySetterCapture(propertyExpression, new PropertyValueCapture<TValue>(), replace);
        }

        /// <summary>
        /// Replaces the current interface registration with a proxy that records assignments to the selected property into the supplied capture instance while forwarding unrelated members to the previously resolved instance.
        /// </summary>
        /// <typeparam name="TService">The interface type to wrap.</typeparam>
        /// <typeparam name="TValue">The property value type.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="propertyExpression">The interface property whose setter should be captured.</param>
        /// <param name="capture">The capture that should record assigned values.</param>
        /// <param name="replace">True to replace an existing registration for <typeparamref name="TService" />. Defaults to <see langword="true" /> because the helper intentionally swaps in a capture proxy.</param>
        /// <returns>The supplied <paramref name="capture" />.</returns>
        public static PropertyValueCapture<TValue> AddPropertySetterCapture<TService, TValue>(this Mocker mocker, Expression<Func<TService, TValue>> propertyExpression, PropertyValueCapture<TValue> capture, bool replace = true)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(propertyExpression);
            ArgumentNullException.ThrowIfNull(capture);

            var serviceType = typeof(TService);
            if (!serviceType.IsInterface)
            {
                throw new NotSupportedException($"{nameof(AddPropertySetterCapture)} currently supports interface types only. Use a fake or stub plus {nameof(PropertyValueCapture<TValue>)} for {serviceType.Name}.");
            }

            var propertyInfo = propertyExpression.GetPropertyInfo();
            if (propertyInfo.SetMethod is null)
            {
                throw new ArgumentException($"Property '{propertyInfo.Name}' must have a setter.", nameof(propertyExpression));
            }

            var currentInstance = mocker.GetObject<TService>() ?? throw new InvalidOperationException($"Unable to resolve an instance for {serviceType.Name} before adding a setter capture.");
            if (currentInstance is PropertySetterCaptureProxy<TService> existingProxy)
            {
                existingProxy.AddCapture(propertyInfo, capture);
                return capture;
            }

            var proxy = DispatchProxy.Create<TService, PropertySetterCaptureProxy<TService>>();
            var proxyController = (PropertySetterCaptureProxy<TService>) (object) proxy;
            proxyController.Initialize(currentInstance);
            proxyController.AddCapture(propertyInfo, capture);

            mocker.AddType<TService>(proxy, replace);
            return capture;
        }
    }

    internal class PropertySetterCaptureProxy<TService> : DispatchProxy where TService : class
    {
        private readonly Dictionary<MethodInfo, Func<object?[]?, object?>> _handlers = [];

        private TService? _inner;

        public void Initialize(TService inner)
        {
            ArgumentNullException.ThrowIfNull(inner);
            _inner = inner;
        }

        public void AddCapture<TValue>(PropertyInfo propertyInfo, PropertyValueCapture<TValue> capture)
        {
            ArgumentNullException.ThrowIfNull(propertyInfo);
            ArgumentNullException.ThrowIfNull(capture);

            if (propertyInfo.GetMethod is MethodInfo getter)
            {
                _handlers[getter] = _ =>
                {
                    if (capture.HasValue)
                    {
                        return capture.Value;
                    }

                    return _inner is null ? default(TValue) : propertyInfo.GetValue(_inner);
                };
            }

            if (propertyInfo.SetMethod is MethodInfo setter)
            {
                _handlers[setter] = arguments =>
                {
                    var assignedValue = arguments is not null && arguments.Length > 0
                        ? (TValue) arguments[0]!
                        : default!;

                    capture.Record(assignedValue);

                    if (_inner is not null)
                    {
                        propertyInfo.SetValue(_inner, assignedValue);
                    }

                    return null;
                };
            }
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (_handlers.TryGetValue(targetMethod, out var handler))
            {
                return handler(args);
            }

            if (_inner is null)
            {
                throw new InvalidOperationException($"{nameof(PropertySetterCaptureProxy<TService>)} has not been initialized.");
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
    }
}
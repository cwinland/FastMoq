using System;
using Moq;

namespace FastMoq
{
    /// <summary>
    /// Describes custom behavior for a known type handled specially by <see cref="Mocker"/>.
    /// Registrations can supply direct instances, managed instances, mock configuration, and post-creation defaults.
    /// </summary>
    public sealed class KnownTypeRegistration
    {
        /// <summary>
        /// Initializes a new registration for the supplied service type.
        /// </summary>
        public KnownTypeRegistration(Type serviceType)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        }

        /// <summary>
        /// Gets the service type this registration applies to.
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        /// When true, the registration also applies to types assignable to <see cref="ServiceType"/>.
        /// </summary>
        public bool IncludeDerivedTypes { get; init; }

        /// <summary>
        /// Optional factory for direct instance resolution in <see cref="Mocker.GetObject(Type, Action{object}?)"/>.
        /// Return <c>null</c> to indicate that the registration did not resolve the requested type.
        /// </summary>
        public Func<Mocker, Type, object?>? DirectInstanceFactory { get; init; }

        /// <summary>
        /// Optional factory for managed instance resolution in <see cref="Mocker.CreateInstance{T}(InstanceCreationOptions, object?[])"/>.
        /// Return <c>null</c> to indicate that the registration did not resolve the requested type.
        /// </summary>
        public Func<Mocker, Type, object?>? ManagedInstanceFactory { get; init; }

        /// <summary>
        /// Optional callback to configure a provider mock after it has been created.
        /// </summary>
        public Action<Mocker, Type, Mock>? ConfigureMock { get; init; }

        /// <summary>
        /// Optional callback to apply post-creation defaults to resolved objects.
        /// </summary>
        public Action<Mocker, object>? ApplyObjectDefaults { get; init; }

        internal bool Matches(Type requestedType)
        {
            return requestedType == ServiceType || (IncludeDerivedTypes && requestedType.IsAssignableTo(ServiceType));
        }

        internal bool TryCreateDirectInstance(Mocker mocker, Type requestedType, out object? instance)
        {
            instance = DirectInstanceFactory?.Invoke(mocker, requestedType);
            return instance != null;
        }

        internal bool TryCreateManagedInstance(Mocker mocker, Type requestedType, out object? instance)
        {
            instance = ManagedInstanceFactory?.Invoke(mocker, requestedType);
            return instance != null;
        }
    }
}
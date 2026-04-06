using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FastMoq.Extensions;
using FastMoq.Models;
using FastMoq.Providers;
using FastMoq.Core.Providers;
using System.Runtime; // For AmbiguousImplementationException per refactor

namespace FastMoq
{
    /// <summary>
    /// Core mock coordinator (provider-based creation integrated).
    /// </summary>
    /// <remarks>
    /// <h3>Resolution Hierarchy (v4)</h3>
    /// FastMoq uses consistent resolution hierarchies across different APIs:
    /// 
    /// <h4>GetObject&lt;T&gt;() - Dependency Injection (Property/Field/Constructor)</h4>
    /// <list type="number">
    ///   <item><description>Custom registration via AddType&lt;T&gt;() or AddKnownType&lt;T&gt;()</description></item>
    ///   <item><description>Built-in known instances (e.g., IFileSystem returns in-memory file system)</description></item>
    ///   <item><description>Tracked mocks via GetOrCreateFastMock (get-or-create paradigm)</description></item>
    ///   <item><description>Default value (null or type default)</description></item>
    /// </list>
    /// <para>GetObject respects all custom configurations and auto-creates mocks when needed.</para>
    /// 
    /// <h4>GetMock&lt;T&gt;() / GetOrCreateMock&lt;T&gt;() - Test Setup (Mock-Only Path)</h4>
    /// <list type="number">
    ///   <item><description>Tracked mocks only (get-or-create tracked mock, no custom registrations applied)</description></item>
    ///   <item><description>Mocks are preconfigured via SetupFastMock (e.g., IFileSystem delegates to built-in)</description></item>
    /// </list>
    /// <para>GetMock/GetOrCreateMock always return tracked mocks for test setup, bypassing custom AddType registrations. 
    /// This is intentional: AddType is for runtime instances, while GetMock is for test mocks.
    /// For setup values, use AddMock() or component constructor parameters instead.</para>
    /// 
    /// <h4>GetParameter() - Constructor Parameter Resolution</h4>
    /// <list type="number">
    ///   <item><description>Custom type mapping (AddType&lt;TInterface, TImpl&gt;())</description></item>
    ///   <item><description>Falls back to GetObject for all other resolution</description></item>
    /// </list>
    /// <para>Constructor parameters follow the same hierarchy as GetObject after type mapping.</para>
    /// </remarks>
    /// <example>
    /// <para>Use <see cref="Mocker" /> as the main test composition root when you want provider-first arrangement based on concrete registrations and policy-driven construction.</para>
    /// <code language="csharp"><![CDATA[
    /// var fileSystem = new MockFileSystem();
    /// fileSystem.AddFile(@"c:\orders\42.json", new MockFileData("{ \"id\": 42, \"total\": 125.50 }"));
    ///
    /// var mocker = new Mocker()
    ///     .AddType<IFileSystem>(fileSystem);
    ///
    /// var resolvedFileSystem = mocker.GetRequiredObject<IFileSystem>();
    ///
    /// resolvedFileSystem.File.Exists(@"c:\orders\42.json").Should().BeTrue();
    /// resolvedFileSystem.Should().BeSameAs(fileSystem);
    /// ]]></code>
    /// </example>
    public partial class Mocker
    {
        public const string SETUP_ALL_PROPERTIES_METHOD_NAME = "SetupAllProperties";
        public const string SETUP = "Setup";

        private readonly record struct InstanceConstructionRequest(
            bool? PublicOnly,
            Type?[]? ConstructorParameterTypes,
            OptionalParameterResolutionMode OptionalParameterResolution);

        public readonly MockFileSystem fileSystem;
        protected internal readonly List<Type> creatingTypeList = new();
        protected internal readonly List<MockModel> mockCollection = new();
        private readonly List<KnownTypeRegistration> knownTypeRegistrations = new();
        private readonly Dictionary<ServiceRegistrationKey, MockModel> keyedMockCollection = new();
        private readonly Dictionary<ServiceRegistrationKey, IInstanceModel> keyedTypeMap = new();
        internal Dictionary<Type, IInstanceModel> typeMap = new();
        private readonly ObservableExceptionLog exceptionLog = new();
        private readonly ObservableLogEntries logEntries = new();
        private readonly Action<LogLevel, EventId, string, Exception?> externalLoggingCallback;
        public ConstructorHistory ConstructorHistory { get; } = new();

        public Action<LogLevel, EventId, string, Exception?> LoggingCallback { get; }
        public OptionalParameterResolutionMode OptionalParameterResolution { get; set; } = OptionalParameterResolutionMode.UseDefaultOrNull;
        public MockerPolicyOptions Policy { get; } = new();

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use Policy.EnabledBuiltInTypeResolutions instead.")]
        public BuiltInTypeResolutionFlags EnabledBuiltInTypeResolutions
        {
            get => Policy.EnabledBuiltInTypeResolutions;
            set => Policy.EnabledBuiltInTypeResolutions = value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use Policy.DefaultFallbackToNonPublicConstructors instead.")]
        public bool DefaultFallbackToNonPublicConstructors
        {
            get => Policy.DefaultFallbackToNonPublicConstructors;
            set => Policy.DefaultFallbackToNonPublicConstructors = value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use Policy.DefaultFallbackToNonPublicMethods instead.")]
        public bool DefaultFallbackToNonPublicMethods
        {
            get => Policy.DefaultFallbackToNonPublicMethods;
            set => Policy.DefaultFallbackToNonPublicMethods = value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use Policy.DefaultStrictMockCreation instead.")]
        public bool? DefaultStrictMockCreation
        {
            get => Policy.DefaultStrictMockCreation;
            set => Policy.DefaultStrictMockCreation = value;
        }

        /// <summary>
        /// Obsolete compatibility alias for <see cref="OptionalParameterResolution"/>.
        /// Prefer <see cref="OptionalParameterResolution"/>, <see cref="InvocationOptions"/>, or focused component-construction overrides in new code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("MockOptional is obsolete and kept only for compatibility. Use OptionalParameterResolution, InvocationOptions, or focused component-construction overrides instead.")]
        public bool MockOptional
        {
            get => OptionalParameterResolution == OptionalParameterResolutionMode.ResolveViaMocker;
            set => OptionalParameterResolution = value
                ? OptionalParameterResolutionMode.ResolveViaMocker
                : OptionalParameterResolutionMode.UseDefaultOrNull;
        }
        public ObservableExceptionLog ExceptionLog => exceptionLog;
        public ObservableLogEntries LogEntries => logEntries;
        public HttpClient HttpClient { get; }
        public Uri Uri { get; }
        public bool InnerMockResolution { get; set; } = true;

        /// <summary>
        /// Feature flag container controlling behavior (replaces monolithic Strict boolean).
        /// </summary>
        public MockBehaviorOptions Behavior { get; set; } = MockBehaviorOptions.LenientPreset.Clone();

        /// <summary>
        /// Backward compatibility: maps to FailOnUnconfigured flag (Strict semantics) and presets.
        /// </summary>
        [Obsolete("Use Behavior.Enabled flags instead (FailOnUnconfigured -> Strict semantics).")]
        public bool Strict
        {
            get => Behavior.Has(MockFeatures.FailOnUnconfigured);
            set
            {
                if (value)
                {
                    Behavior.Enable(MockFeatures.FailOnUnconfigured);
                }
                else
                {
                    Behavior.Disable(MockFeatures.FailOnUnconfigured);
                }

                ApplyCompatibilityDefaults(value);
            }
        }

        /// <summary>
        /// Applies the predefined strict behavior preset.
        /// This is broader than <see cref="Strict"/>, which is retained for backward compatibility.
        /// </summary>
        public void UseStrictPreset()
        {
            Behavior = MockBehaviorOptions.StrictPreset.Clone();
            ApplyCompatibilityDefaults(strictCompatibility: true);
        }

        /// <summary>
        /// Applies the predefined lenient behavior preset.
        /// This is broader than disabling <see cref="MockFeatures.FailOnUnconfigured"/> alone.
        /// </summary>
        public void UseLenientPreset()
        {
            Behavior = MockBehaviorOptions.LenientPreset.Clone();
            ApplyCompatibilityDefaults(strictCompatibility: false);
        }

        #region Ctors
        public Mocker() : this((_, _, _, _) => { }) { }
        public Mocker(Action<LogLevel, EventId, string, Exception?> loggingCallback)
        {
            ProviderBootstrap.Ensure();
            fileSystem = new MockFileSystem();
            HttpClient = this.CreateHttpClientCore();
            Uri = HttpClient.BaseAddress ?? new Uri("http://localhost");
            externalLoggingCallback = loggingCallback;
            LoggingCallback = CaptureLogEntry;
        }
        public Mocker(Dictionary<Type, IInstanceModel> map) : this() => typeMap = map;
        public Mocker(Dictionary<Type, IInstanceModel> map, Action<LogLevel, EventId, string, Exception?> loggingCallback) : this(loggingCallback) => typeMap = map;
        #endregion

        private void CaptureLogEntry(LogLevel logLevel, EventId eventId, string message, Exception? exception)
        {
            logEntries.Add(new LogEntry(logLevel, eventId, message, exception));
            externalLoggingCallback(logLevel, eventId, message, exception);
        }

        #region Type Mapping
        internal IReadOnlyList<KnownTypeRegistration> KnownTypeRegistrations => knownTypeRegistrations;

        private void ApplyCompatibilityDefaults(bool strictCompatibility)
        {
            Policy.EnabledBuiltInTypeResolutions = strictCompatibility
                ? BuiltInTypeResolutionFlags.StrictCompatibilityDefaults
                : BuiltInTypeResolutionFlags.LenientDefaults;

            Policy.DefaultFallbackToNonPublicConstructors = !strictCompatibility;
            Policy.DefaultFallbackToNonPublicMethods = !strictCompatibility;
            Policy.DefaultStrictMockCreation = strictCompatibility;
        }

        internal bool ShouldCreateStrictMocks() => Policy.DefaultStrictMockCreation ?? Behavior.Has(MockFeatures.FailOnUnconfigured);

        public Mocker AddType(Type tInterface, Type tClass, Func<Mocker, object>? createFunc = null, bool replace = false, params object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(tInterface);
            ArgumentNullException.ThrowIfNull(tClass);
            ValidateAndReplaceType(tInterface, tClass, replace);
            typeMap[tInterface] = new InstanceModel(tInterface, tClass, createFunc, args?.ToList() ?? new List<object?>());
            return this;
        }

        [Obsolete("Use the explicit AddType(...) overloads for normal type mapping. Use AddKnownType(...) when resolution depends on framework-style requested-type/context behavior. This context-aware AddType overload is retained for compatibility only.")]
        public Mocker AddType(Type tInterface, Type tClass, Func<Mocker, object, object>? createFunc, bool replace = false, params object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(tInterface);
            ArgumentNullException.ThrowIfNull(tClass);
            ValidateAndReplaceType(tInterface, tClass, replace);
            typeMap[tInterface] = new InstanceModel(tInterface, tClass, createFunc, args?.ToList() ?? new List<object?>());
            return this;
        }
        /// <summary>
        /// Registers a concrete instance so future object resolution returns that instance instead of auto-creating a mock.
        /// </summary>
        /// <example>
        /// <para>Use this overload when a dependency should be a known runtime object rather than a tracked mock.</para>
        /// <code language="csharp"><![CDATA[
        /// var fileSystem = new MockFileSystem();
        ///
        /// var mocker = new Mocker()
        ///     .AddType<IFileSystem>(fileSystem);
        ///
        /// var resolvedFileSystem = mocker.GetRequiredObject<IFileSystem>();
        /// resolvedFileSystem.Should().BeSameAs(fileSystem);
        /// ]]></code>
        /// </example>
        public Mocker AddType<T>(T value, bool replace = false)
        {
            if (typeMap.ContainsKey(typeof(T)))
            {
                if (!replace)
                {
                    typeof(T).ThrowAlreadyExists();
                }

                typeMap.Remove(typeof(T));
            }

            typeMap[typeof(T)] = new InstanceModel<T>(_ => value);
            return this;
        }
        public Mocker AddType<TInterface, TClass>(bool replace = false, params object?[]? args)
            where TInterface : class where TClass : class => AddType(typeof(TInterface), typeof(TClass), (Func<Mocker, object>?)null, replace, args);
        public Mocker AddType<TInterface, TClass>(Func<Mocker, TClass>? createFunc, bool replace = false, params object?[]? args)
            where TInterface : class where TClass : class => AddType(typeof(TInterface), typeof(TClass), createFunc is null ? null : new Func<Mocker, object>(m => createFunc(m)!), replace, args);
        public Mocker AddType<T>(Func<Mocker, T>? createFunc = null, bool replace = false, params object?[]? args) where T : class => AddType<T, T>(createFunc, replace, args);

        /// <summary>
        /// Adds a keyed type registration for this <see cref="Mocker"/> instance.
        /// </summary>
        public Mocker AddKeyedType(Type tInterface, object serviceKey, Type tClass, Func<Mocker, object>? createFunc = null, bool replace = false, params object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(serviceKey);
            ArgumentNullException.ThrowIfNull(tInterface);
            ArgumentNullException.ThrowIfNull(tClass);

            ValidateAndReplaceKeyedType(tInterface, tClass, serviceKey, replace);
            keyedTypeMap[CreateServiceRegistrationKey(tInterface, serviceKey)] = new InstanceModel(tInterface, tClass, createFunc, args?.ToList() ?? new List<object?>());
            return this;
        }

        /// <summary>
        /// Adds a keyed concrete value for this <see cref="Mocker"/> instance.
        /// </summary>
        public Mocker AddKeyedType<T>(object serviceKey, T value, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(serviceKey);

            var registrationKey = CreateServiceRegistrationKey(typeof(T), serviceKey);
            if (keyedTypeMap.ContainsKey(registrationKey))
            {
                if (!replace)
                {
                    typeof(T).ThrowAlreadyExists();
                }

                keyedTypeMap.Remove(registrationKey);
            }

            keyedTypeMap[registrationKey] = new InstanceModel(typeof(T), typeof(T), _ => value!, new List<object?>());
            return this;
        }

        /// <summary>
        /// Adds a keyed interface-to-concrete registration for this <see cref="Mocker"/> instance.
        /// </summary>
        public Mocker AddKeyedType<TInterface, TClass>(object serviceKey, bool replace = false, params object?[]? args)
            where TInterface : class where TClass : class => AddKeyedType(typeof(TInterface), serviceKey, typeof(TClass), (Func<Mocker, object>?)null, replace, args);

        /// <summary>
        /// Adds a keyed interface-to-concrete registration with factory for this <see cref="Mocker"/> instance.
        /// </summary>
        public Mocker AddKeyedType<TInterface, TClass>(object serviceKey, Func<Mocker, TClass>? createFunc, bool replace = false, params object?[]? args)
            where TInterface : class where TClass : class =>
            AddKeyedType(typeof(TInterface), serviceKey, typeof(TClass), createFunc is null ? null : new Func<Mocker, object>(m => createFunc(m)!), replace, args);

        /// <summary>
        /// Adds a keyed concrete registration with factory for this <see cref="Mocker"/> instance.
        /// </summary>
        public Mocker AddKeyedType<T>(object serviceKey, Func<Mocker, T>? createFunc = null, bool replace = false, params object?[]? args) where T : class =>
            AddKeyedType<T, T>(serviceKey, createFunc, replace, args);

        [Obsolete("Use AddType<string>(...) or AddType(value) for ordinary type mapping. Use AddKnownType(...) for framework-style special handling. This context-driven string AddType overload is retained for compatibility only.")]
        public Mocker AddType(Func<Mocker, object?, object?> createFunc, bool replace = false, params object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(createFunc);

            if (typeMap.ContainsKey(typeof(string)))
            {
                if (!replace)
                {
                    typeof(string).ThrowAlreadyExists();
                }

                typeMap.Remove(typeof(string));
            }

            typeMap[typeof(string)] = new InstanceModel(typeof(string), typeof(string), (Func<Mocker, object, object>)((m, ctx) => createFunc(m, ctx)!), args?.ToList() ?? new List<object?>());
            return this;
        }

        private void ValidateAndReplaceType(Type tInterface, Type tClass, bool replace)
        {
            if (tClass.IsInterface)
            {
                throw new ArgumentException(tInterface.Name.Equals(tClass.Name)
                    ? $"{nameof(AddType)} does not support mapping an interface to itself."
                    : $"{tClass.Name} cannot be an interface. Provide a concrete implementation.");
            }
            if (!tInterface.IsAssignableFrom(tClass))
            {
                throw new ArgumentException($"{tClass.Name} is not assignable to {tInterface.Name}.");
            }

            if (typeMap.ContainsKey(tInterface))
            {
                if (!replace)
                {
                    tInterface.ThrowAlreadyExists();
                }

                typeMap.Remove(tInterface);
            }
        }

        private void ValidateAndReplaceKeyedType(Type tInterface, Type tClass, object serviceKey, bool replace)
        {
            if (tClass.IsInterface)
            {
                throw new ArgumentException(tInterface.Name.Equals(tClass.Name)
                    ? $"{nameof(AddKeyedType)} does not support mapping an interface to itself."
                    : $"{tClass.Name} cannot be an interface. Provide a concrete implementation.");
            }

            if (!tInterface.IsAssignableFrom(tClass))
            {
                throw new ArgumentException($"{tClass.Name} is not assignable to {tInterface.Name}.");
            }

            var registrationKey = CreateServiceRegistrationKey(tInterface, serviceKey);
            if (keyedTypeMap.ContainsKey(registrationKey))
            {
                if (!replace)
                {
                    tInterface.ThrowAlreadyExists();
                }

                keyedTypeMap.Remove(registrationKey);
            }
        }

        public void AddFileSystemAbstractionMapping()
        {
            AddType(typeof(IDirectory), typeof(DirectoryBase));
            AddType(typeof(IDirectoryInfo), typeof(DirectoryInfoBase));
            AddType(typeof(IDirectoryInfoFactory), typeof(MockDirectoryInfoFactory));
            AddType(typeof(IDriveInfo), typeof(DriveInfoBase));
            AddType(typeof(IDriveInfoFactory), typeof(MockDriveInfoFactory));
            AddType(typeof(IFile), typeof(FileBase));
            AddType(typeof(IFileInfo), typeof(FileInfoBase));
            AddType(typeof(IFileInfoFactory), typeof(MockFileInfoFactory));
            AddType(typeof(IFileStreamFactory), typeof(MockFileStreamFactory));
            AddType(typeof(IFileSystem), typeof(FileSystemBase));
            AddType(typeof(IFileSystemInfo), typeof(FileSystemInfoBase));
            AddType(typeof(IFileSystemWatcherFactory), typeof(MockFileSystemWatcherFactory));
            AddType(typeof(IPath), typeof(PathBase));
        }

        /// <summary>
        /// Adds a custom known-type registration for this <see cref="Mocker"/> instance.
        /// This is the extensibility point for framework-like types that need special resolution or setup behavior.
        /// </summary>
        public Mocker AddKnownType(KnownTypeRegistration registration, bool replace = false)
        {
            ArgumentNullException.ThrowIfNull(registration);

            var existing = knownTypeRegistrations.Where(x => x.ServiceType == registration.ServiceType).ToList();
            if (existing.Count > 0)
            {
                if (!replace)
                {
                    registration.ServiceType.ThrowAlreadyExists();
                }

                foreach (var item in existing)
                {
                    knownTypeRegistrations.Remove(item);
                }
            }

            knownTypeRegistrations.Add(registration);
            return this;
        }

        /// <summary>
        /// Adds a typed custom known-type registration for this <see cref="Mocker"/> instance.
        /// </summary>
        public Mocker AddKnownType<TKnown>(
            Func<Mocker, Type, TKnown?>? directInstanceFactory = null,
            Func<Mocker, Type, TKnown?>? managedInstanceFactory = null,
            Action<Mocker, Type, IFastMock>? configureMock = null,
            Action<Mocker, TKnown>? applyObjectDefaults = null,
            bool includeDerivedTypes = false,
            bool replace = false)
        {
            return AddKnownType(new KnownTypeRegistration(typeof(TKnown))
            {
                IncludeDerivedTypes = includeDerivedTypes,
                DirectInstanceFactory = directInstanceFactory is null ? null : (mocker, requestedType) => directInstanceFactory(mocker, requestedType),
                ManagedInstanceFactory = managedInstanceFactory is null ? null : (mocker, requestedType) => managedInstanceFactory(mocker, requestedType),
                ConfigureMock = configureMock,
                ApplyObjectDefaults = applyObjectDefaults is null
                    ? null
                    : (mocker, obj) =>
                    {
                        if (obj is TKnown typed)
                        {
                            applyObjectDefaults(mocker, typed);
                        }
                    },
            }, replace);
        }
        #endregion

        #region Type Resolution Helpers
        internal IInstanceModel GetTypeFromInterface<T>() where T : class => new InstanceModel(typeof(T), GetTypeFromInterface(typeof(T)));
        internal Type GetTypeFromInterface(Type type)
        {
            if (!type.IsInterface)
            {
                return type;
            }

            if (type == typeof(ILogger) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ILogger<>)))
            {
                return type;
            }

            return TestClassExtensions.GetTypeFromInterface(this, type);
        }

        internal IInstanceModel GetTypeModel(Type type) => typeMap.TryGetValue(type, out var model) && model is not null ? model : new InstanceModel(type, GetTypeFromInterface(type));
        internal IInstanceModel GetKeyedTypeModel(Type type, object serviceKey)
        {
            var registrationKey = CreateServiceRegistrationKey(type, serviceKey);
            return keyedTypeMap.TryGetValue(registrationKey, out var model) && model is not null
                ? model
                : new InstanceModel(type, GetTypeFromInterface(type));
        }

        internal bool HasKeyedTypeModel(Type type, object serviceKey) => keyedTypeMap.ContainsKey(CreateServiceRegistrationKey(type, serviceKey));
        internal static Type CleanType(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Mock<>) ? type.GetGenericArguments()[0] : type;
        private static ServiceRegistrationKey CreateServiceRegistrationKey(Type type, object serviceKey)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(serviceKey);
            return new ServiceRegistrationKey(CleanType(type), serviceKey);
        }
        #endregion

        #region Injection / Object Creation
        internal object? GetParameter(ParameterInfo parameter)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            if (TryGetServiceKey(parameter, out var serviceKey))
            {
                return GetKeyedParameter(parameter, serviceKey!);
            }

            var pt = parameter.ParameterType;
            if (!typeMap.ContainsKey(pt) && !pt.IsClass && !pt.IsInterface)
            {
                return pt.GetDefaultValue();
            }

            var m = GetTypeModel(pt);
            return m.CreateFunc != null ? m.CreateFunc.Invoke(this, parameter) : (!pt.IsSealed ? GetObject(pt) : pt.GetDefaultValue());
        }

        internal object? GetKeyedParameter(ParameterInfo parameter, object serviceKey)
        {
            ArgumentNullException.ThrowIfNull(parameter);
            ArgumentNullException.ThrowIfNull(serviceKey);

            var parameterType = parameter.ParameterType;
            if (!HasKeyedTypeModel(parameterType, serviceKey) && !parameterType.IsClass && !parameterType.IsInterface)
            {
                return parameterType.GetDefaultValue();
            }

            var resolved = GetKeyedObject(parameterType, serviceKey);
            return resolved ?? parameterType.GetDefaultValue();
        }

        internal object? ResolveParameter(ParameterInfo parameter, OptionalParameterResolutionMode optionalParameterResolution)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            return optionalParameterResolution == OptionalParameterResolutionMode.UseDefaultOrNull && parameter.IsOptional
                ? parameter.HasDefaultValue ? parameter.DefaultValue : null
                : GetParameter(parameter);
        }

        public T AddInjections<T>(T obj, Type? referenceType = null) where T : class?
        {
            if (obj == null)
            {
                return obj;
            }

            referenceType ??= obj.GetType();
            foreach (var p in referenceType.GetInjectionProperties())
            {
                try { obj.SetPropertyValue(p.Name, GetObject(p.PropertyType)); } catch (Exception ex) { ExceptionLog.Add(ex.Message); }
            }
            foreach (var f in referenceType.GetInjectionFields())
            {
                try { obj.SetFieldValue(f.Name, GetObject(f.FieldType)); } catch (Exception ex) { ExceptionLog.Add(ex.Message); }
            }
            return obj;
        }

        public object? GetObject(Type type, Action<object?>? initAction = null)
        {
            ArgumentNullException.ThrowIfNull(type);

            type = CleanType(type);
            var m = GetTypeModel(type);
            if (m.CreateFunc != null)
            {
                return AddInjections(m.CreateFunc.Invoke(this, m.InstanceType), m.InstanceType);
            }
            if (KnownTypeRegistry.TryGetDirectInstance(this, type, out var knownInstance))
            {
                initAction?.Invoke(knownInstance);
                return knownInstance;
            }

            if (KnownTypeRegistry.TryGetCustomManagedInstance(this, type, out var customManagedInstance))
            {
                initAction?.Invoke(customManagedInstance);
                return customManagedInstance;
            }

            if ((type.IsClass || type.IsInterface) && !type.IsSealed)
            {
                if (Contains(type))
                {
                    return ResolveTrackedMockObject(type, initAction);
                }

                if (KnownTypeRegistry.TryGetBuiltInManagedInstance(this, type, out var builtInManagedInstance))
                {
                    initAction?.Invoke(builtInManagedInstance);
                    return builtInManagedInstance;
                }

                return ResolveTrackedMockObject(type, initAction);
            }
            var def = type.GetDefaultValue();
            initAction?.Invoke(def);
            return def;
        }

        /// <summary>
        /// Resolves a keyed object for the specified service type.
        /// </summary>
        public object? GetKeyedObject(Type type, object serviceKey, Action<object?>? initAction = null)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(serviceKey);

            type = CleanType(type);
            var m = GetKeyedTypeModel(type, serviceKey);
            if (m.CreateFunc != null)
            {
                var created = AddInjections(m.CreateFunc.Invoke(this, m.InstanceType), m.InstanceType);
                initAction?.Invoke(created);
                return created;
            }

            if ((type.IsClass || type.IsInterface) && !type.IsSealed)
            {
                return ResolveTrackedMockObject(type, initAction, serviceKey);
            }

            var def = type.GetDefaultValue();
            initAction?.Invoke(def);
            return def;
        }

        private object? ResolveTrackedMockObject(Type type, Action<object?>? initAction, object? serviceKey = null)
        {
            var fast = serviceKey == null ? GetOrCreateFastMock(type) : GetOrCreateFastMock(type, serviceKey);
            var provider = MockingProviderRegistry.Default;
            if (Behavior.Has(MockFeatures.AutoSetupProperties) && provider.Capabilities.SupportsSetupAllProperties)
            {
                provider.ConfigureProperties(fast);
                ReapplyTrackedMockConfiguration(type, fast);
            }
            if (Behavior.Has(MockFeatures.LoggerCallback))
            {
                var mockedType = fast.MockedType;
                if (provider.Capabilities.SupportsLoggerCapture && typeof(ILogger).IsAssignableFrom(mockedType))
                {
                    provider.ConfigureLogger(fast, LoggingCallback);
                }
            }
            var obj = fast.Instance;
            if (Behavior.Has(MockFeatures.AutoInjectDependencies))
            {
                AddInjections(obj, GetTypeModel(type).InstanceType);
            }

            KnownTypeRegistry.ApplyObjectDefaults(this, obj);

            initAction?.Invoke(obj);
            return obj;
        }

        private void ReapplyTrackedMockConfiguration(Type type, IFastMock fastMock)
        {
            KnownTypeRegistry.ConfigureMock(this, type, fastMock);

            if (!DatabaseSupportBridge.IsEntityFrameworkDbContextType(type))
            {
                return;
            }

            if (TryGetLegacyMock(fastMock) is not Mock legacyMock)
            {
                return;
            }

            legacyMock.GetType().GetMethod("SetupDbSets")?.Invoke(legacyMock, [this]);
        }

        public object? GetObject(ParameterInfo info)
        {
            ArgumentNullException.ThrowIfNull(info);

            try
            {
                return ResolveParameter(info, OptionalParameterResolution);
            }
            catch (Exception ex) when (ex is FileNotFoundException or AmbiguousImplementationException)
            {
                ExceptionLog.Add(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                ExceptionLog.Add(ex.Message);
                return info.ParameterType.GetDefaultValue();
            }
        }
        /// <summary>
        /// Resolves an object using FastMoq's runtime resolution pipeline.
        /// </summary>
        /// <example>
        /// <para>Resolve a dependency after configuring a concrete registration through the provider-first pipeline.</para>
        /// <code language="csharp"><![CDATA[
        /// var fileSystem = new MockFileSystem();
        /// fileSystem.AddDirectory(@"c:\exports");
        ///
        /// var mocker = new Mocker()
        ///     .AddType<IFileSystem>(fileSystem);
        ///
        /// var resolvedFileSystem = mocker.GetRequiredObject<IFileSystem>();
        ///
        /// resolvedFileSystem.Directory.Exists(@"c:\exports").Should().BeTrue();
        /// ]]></code>
        /// </example>
        public T? GetObject<T>() where T : class => GetObject(typeof(T)) as T;
        public T? GetObject<T>(Action<T?> init) where T : class => GetObject(typeof(T), o => init((T?)o)) as T;
        public T? GetKeyedObject<T>(object serviceKey) where T : class => GetKeyedObject(typeof(T), serviceKey) as T;
        public T? GetKeyedObject<T>(object serviceKey, Action<T?> init) where T : class => GetKeyedObject(typeof(T), serviceKey, o => init((T?)o)) as T;
        public T? GetObject<T>(object?[] args) where T : class
        {
            if (args == null)
            {
                return GetObject<T>();
            }

            var type = typeof(T).IsInterface ? GetTypeFromInterface<T>() : new InstanceModel<T>();
            var constructor = FindConstructor(type.InstanceType, true, args);
            return CreateInstanceInternal<T>(constructor.ConstructorInfo, args);
        }

        private static bool TryGetServiceKey(ParameterInfo parameter, out object? serviceKey)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            foreach (var attribute in parameter.GetCustomAttributes(false))
            {
                var attributeType = attribute.GetType();
                if (!string.Equals(attributeType.FullName, "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute", StringComparison.Ordinal))
                {
                    continue;
                }

                serviceKey = attributeType.GetProperty("Key")?.GetValue(attribute)
                    ?? attributeType.GetProperty("ServiceKey")?.GetValue(attribute);
                return serviceKey != null;
            }

            serviceKey = null;
            return false;
        }

        public object? AddProperties(Type type, object? obj, params KeyValuePair<string, object>[] data)
        {
            if (obj == null)
            {
                return null;
            }

            if (creatingTypeList.Contains(type))
            {
                return obj;
            }

            try
            {
                creatingTypeList.Add(type);
                var props = type.GetProperties().Where(p => p.CanRead && (p.CanWrite || data.Any(d => d.Key.Equals(p.Name, StringComparison.OrdinalIgnoreCase)))).ToList();
                foreach (var prop in props)
                {
                    try
                    {
                        var val = prop.GetValue(obj);
                        var shouldAssign = !creatingTypeList.Contains(prop.PropertyType) && (val == null || data.Any(x => x.Key.Contains(prop.Name, StringComparison.OrdinalIgnoreCase)));
                        if (!shouldAssign)
                        {
                            continue;
                        }

                        val = data.Any(x => x.Key.Contains(prop.Name, StringComparison.OrdinalIgnoreCase))
                            ? data.First(x => x.Key.Contains(prop.Name, StringComparison.OrdinalIgnoreCase)).Value
                            : GetObject(prop.PropertyType);
                        if (prop.CanWrite)
                        {
                            prop.SetValue(obj, val);
                        }
                        else
                        {
                            var backing = type.GetField($"<{prop.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                            backing?.SetValue(obj, val);
                        }
                    }
                    catch (Exception ex) { ExceptionLog.Add(ex.Message); }
                }
            }
            finally { creatingTypeList.Remove(type); }
            return obj;
        }
        public T? AddProperties<T>(T obj, params KeyValuePair<string, object>[] data) => (T?)AddProperties(typeof(T), obj, data);
        #endregion

        #region Constructor Resolution / Instance Creation
        public T? CreateInstance<T>(params object?[] args) where T : class =>
            CreateInstance<T>(InstanceCreationFlags.None, args);

        /// <summary>
        /// Creates an instance of <typeparamref name="T"/> using the supplied per-call construction flags.
        /// </summary>
        public T? CreateInstance<T>(InstanceCreationFlags flags, params object?[] args) where T : class =>
            CreateInstanceCore<T>(CreateInstanceConstructionRequest(flags, constructorParameterTypes: null), args);

        /// <summary>
        /// Legacy compatibility overload retained for source compatibility.
        /// File-system resolution now follows <see cref="Policy"/> instead of a per-call flag.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Ignored. Use CreateInstance<T>() to follow Policy.EnabledBuiltInTypeResolutions and Policy.DefaultFallbackToNonPublicConstructors.")]
        public IFileSystem? CreateInstance<T>(bool usePredefinedFileSystem) where T : class, IFileSystem =>
            CreateInstance<T>(usePredefinedFileSystem, Array.Empty<object?>());

        /// <summary>
        /// Legacy compatibility overload retained for source compatibility.
        /// File-system resolution now follows <see cref="Policy"/> instead of a per-call flag.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Ignored. Use CreateInstance<T>() to follow Policy.EnabledBuiltInTypeResolutions and Policy.DefaultFallbackToNonPublicConstructors.")]
        public T? CreateInstance<T>(bool usePredefinedFileSystem, params object?[] args) where T : class =>
            CreateInstance<T>(args);

        /// <summary>
        /// Legacy compatibility alias retained for source compatibility.
        /// Use <see cref="CreateInstance{T}(InstanceCreationFlags, object?[])"/> with <see cref="InstanceCreationFlags.AllowNonPublicConstructorFallback"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use CreateInstance<T>(InstanceCreationFlags.AllowNonPublicConstructorFallback, ...) for the legacy non-public behavior, or CreateInstance<T>() to follow the current policy defaults.")]
        public T? CreateInstanceNonPublic<T>(params object?[] args) where T : class =>
            CreateInstance<T>(InstanceCreationFlags.AllowNonPublicConstructorFallback, args);

        /// <summary>
        /// Legacy compatibility alias retained for source compatibility.
        /// Public and non-public constructor selection now follows the shared constructor-resolution rules.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use CreateInstance<T>(InstanceCreationFlags.AllowNonPublicConstructorFallback, ...) where possible. This Type-based compatibility overload remains only for older callers.")]
        public object? CreateInstanceNonPublic(Type type, params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(type);

            var constructor = GetConstructorByArgs(args, type, true, fallbackToNonPublicConstructors: true, OptionalParameterResolution);
            return CreateInstanceInternal(type, constructor.ConstructorInfo, OptionalParameterResolution, constructor.ParameterList);
        }

        internal T? CreateInstance<T>(bool? publicOnly, OptionalParameterResolutionMode optionalParameterResolution, params object?[] args) where T : class =>
            CreateInstanceCore<T>(CreateInstanceConstructionRequest(publicOnly, null, optionalParameterResolution), args);

        public T? CreateInstanceByType<T>(params Type?[] parameterTypes) where T : class
        {
            return CreateInstanceByType<T>(InstanceCreationFlags.None, parameterTypes);
        }

        public T? CreateInstanceByType<T>(InstanceCreationFlags flags, params Type?[] parameterTypes) where T : class =>
            CreateInstanceCore<T>(CreateInstanceConstructionRequest(flags, parameterTypes), Array.Empty<object?>());

        internal T? CreateInstanceByType<T>(bool? publicOnly, OptionalParameterResolutionMode optionalParameterResolution, params Type?[] parameterTypes) where T : class =>
            CreateInstanceCore<T>(CreateInstanceConstructionRequest(publicOnly, parameterTypes, optionalParameterResolution), Array.Empty<object?>());

        private InstanceConstructionRequest CreateInstanceConstructionRequest(InstanceCreationFlags flags, Type?[]? constructorParameterTypes)
        {
            var publicOnly = ResolvePublicOnlyOverride(flags);
            var optionalParameterResolution = ResolveOptionalParameterResolution(flags);
            return CreateInstanceConstructionRequest(publicOnly, constructorParameterTypes, optionalParameterResolution);
        }

        private InstanceConstructionRequest CreateInstanceConstructionRequest(bool? publicOnly, Type?[]? constructorParameterTypes, OptionalParameterResolutionMode optionalParameterResolution) =>
            new(publicOnly, constructorParameterTypes, optionalParameterResolution);

        private static bool? ResolvePublicOnlyOverride(InstanceCreationFlags flags)
        {
            var publicOnly = flags.HasFlag(InstanceCreationFlags.PublicConstructorsOnly);
            var allowNonPublicFallback = flags.HasFlag(InstanceCreationFlags.AllowNonPublicConstructorFallback);

            if (publicOnly && allowNonPublicFallback)
            {
                throw new ArgumentException("InstanceCreationFlags cannot combine PublicConstructorsOnly with AllowNonPublicConstructorFallback.", nameof(flags));
            }

            if (publicOnly)
            {
                return true;
            }

            if (allowNonPublicFallback)
            {
                return false;
            }

            return null;
        }

        private OptionalParameterResolutionMode ResolveOptionalParameterResolution(InstanceCreationFlags flags)
        {
            var resolveViaMocker = flags.HasFlag(InstanceCreationFlags.ResolveOptionalParametersViaMocker);
            var useDefaultOrNull = flags.HasFlag(InstanceCreationFlags.UseDefaultOrNullOptionalParameters);

            if (resolveViaMocker && useDefaultOrNull)
            {
                throw new ArgumentException("InstanceCreationFlags cannot combine ResolveOptionalParametersViaMocker with UseDefaultOrNullOptionalParameters.", nameof(flags));
            }

            if (resolveViaMocker)
            {
                return OptionalParameterResolutionMode.ResolveViaMocker;
            }

            if (useDefaultOrNull)
            {
                return OptionalParameterResolutionMode.UseDefaultOrNull;
            }

            return OptionalParameterResolution;
        }

        /// <summary>
        /// Centralized creation logic used by all public CreateInstance* methods.
        /// </summary>
        private T? CreateInstanceCore<T>(InstanceConstructionRequest request, object?[] args) where T : class
        {
            var requestedType = typeof(T);
            var model = GetTypeModel(requestedType);

            if (TryGetExistingObject(requestedType, model, out T? instance))
            {
                return instance;
            }

            if (model.Arguments.Count > 0 && args.Length == 0)
            {
                args = model.Arguments.ToArray();
            }

            var targetType = model.InstanceType ?? requestedType;

            if (targetType.IsInterface)
            {
                if (Behavior.Has(MockFeatures.FailOnUnconfigured))
                {
                    throw new NotImplementedException("Unable to find the constructor.");
                }

                return GetObject<T>();
            }

            if (request.ConstructorParameterTypes != null)
            {
                var constructor = FindConstructorByType(targetType, false, ShouldFallbackToNonPublicConstructors(request.PublicOnly), request.ConstructorParameterTypes);
                return CreateInstanceInternal(targetType, constructor, request.OptionalParameterResolution, args) as T;
            }

            var ctorModel = GetConstructorByArgs(args, targetType, false, ShouldFallbackToNonPublicConstructors(request.PublicOnly), request.OptionalParameterResolution);
            var created = CreateInstanceInternal(targetType, ctorModel.ConstructorInfo, request.OptionalParameterResolution, ctorModel.ParameterList);
            return created as T;
        }

        private bool ShouldFallbackToNonPublicConstructors(bool? publicOnly)
        {
            return publicOnly switch
            {
                true => false,
                false => true,
                _ => Policy.DefaultFallbackToNonPublicConstructors,
            };
        }

        internal bool ShouldAllowNonPublicConstructorsForMockRequest(bool? allowNonPublicConstructors)
        {
            return allowNonPublicConstructors ?? !ShouldCreateStrictMocks();
        }

        private ConstructorModel GetConstructorByArgs(object?[] args, Type instanceType, bool nonPublic, bool fallbackToNonPublicConstructors, OptionalParameterResolutionMode optionalParameterResolution)
        {
            return args.Length > 0
                ? FindConstructor(instanceType, nonPublic, fallbackToNonPublicConstructors, optionalParameterResolution, args)
                : FindPreferredConstructor(instanceType, nonPublic, fallbackToNonPublicConstructors, optionalParameterResolution);
        }

        private bool TryGetExistingObject<T>(Type requestedType, IInstanceModel typeInstanceModel, out T? instance) where T : class
        {
            instance = default;

            if (creatingTypeList.Contains(requestedType))
            {
                return false;
            }

            if (TryGetModelInstance(typeInstanceModel, requestedType, out var modelInstance))
            {
                instance = modelInstance as T;
                return true;
            }

            if (KnownTypeRegistry.TryGetManagedInstance(this, requestedType, out var managedInstance))
            {
                instance = managedInstance as T;
                return instance != null;
            }

            return false;
        }

        private bool TryGetModelInstance(IInstanceModel typeInstanceModel, Type requestedType, out object? instance)
        {
            instance = null;

            if (typeInstanceModel.CreateFunc == null)
            {
                return false;
            }

            creatingTypeList.Add(requestedType);
            try
            {
                ConstructorHistory.AddOrUpdate(requestedType, typeInstanceModel);
                instance = typeInstanceModel.CreateFunc.Invoke(this, requestedType);
                return true;
            }
            finally
            {
                creatingTypeList.Remove(requestedType);
            }
        }
        #endregion

        #region Constructor Resolution (internal helpers restored)
        internal ConstructorModel GetTypeConstructor(Type type, bool nonPublic, object?[] args) =>
            GetTypeConstructor(type, nonPublic, OptionalParameterResolution, args);

        internal ConstructorModel GetTypeConstructor(Type type, bool nonPublic, OptionalParameterResolutionMode optionalParameterResolution, object?[] args)
        {
            var constructor = new ConstructorModel(null, args);
            try
            {
                if (!type.IsInterface)
                {
                    constructor = args.Length > 0 || nonPublic
                        ? FindConstructor(type, nonPublic, optionalParameterResolution, args)
                        : FindPreferredConstructor(type, nonPublic, optionalParameterResolution);
                }
            }
            catch (Exception ex) { ExceptionLog.Add(ex.Message); }
            if (constructor.ConstructorInfo == null && !HasParameterlessConstructor(type))
            {
                try { constructor = GetConstructors(type, nonPublic, optionalParameterResolution).MinBy(x => x.ParameterList.Length) ?? constructor; }
                catch (Exception ex) { ExceptionLog.Add(ex.Message); }
            }
            return constructor;
        }
        internal ConstructorInfo FindConstructorByType(Type type, bool nonPublic, params Type?[] args)
        {
            var strict = Behavior.Has(MockFeatures.FailOnUnconfigured);
            var ctors = GetConstructorsByType(nonPublic, type, args);
            if (ctors.Count == 0 && !nonPublic && !strict)
            {
                return FindConstructorByType(type, true, args);
            }

            if (ctors.Count == 0)
            {
                throw new NotImplementedException("Unable to find the constructor.");
            }

            return ctors[0];
        }

        internal ConstructorInfo FindConstructorByType(Type type, bool? publicOnly, params Type?[] args) =>
            FindConstructorByType(type, false, ShouldFallbackToNonPublicConstructors(publicOnly), args);

        private ConstructorInfo FindConstructorByType(Type type, bool nonPublic, bool fallbackToNonPublicConstructors, params Type?[] args)
        {
            var ctors = GetConstructorsByType(nonPublic, type, args);
            if (ctors.Count == 0 && !nonPublic && fallbackToNonPublicConstructors)
            {
                return FindConstructorByType(type, true, fallbackToNonPublicConstructors, args);
            }

            if (ctors.Count == 0)
            {
                throw new NotImplementedException("Unable to find the constructor.");
            }

            return ctors[0];
        }
        internal ConstructorModel FindConstructor(Type type, bool nonPublic, params object?[] args) =>
            FindConstructor(type, nonPublic, OptionalParameterResolution, args);

        internal ConstructorModel FindConstructor(Type type, bool nonPublic, OptionalParameterResolutionMode optionalParameterResolution, params object?[] args)
        {
            if (args.Length == 0)
            {
                return FindPreferredConstructor(type, nonPublic, optionalParameterResolution);
            }

            var strict = Behavior.Has(MockFeatures.FailOnUnconfigured);
            var all = GetConstructors(type, nonPublic, optionalParameterResolution, args);
            var filtered = all.Where(x => type.IsValidConstructor(x.ConstructorInfo!, args)).ToList();
            if (!filtered.Any())
            {
                if (!nonPublic && !strict)
                {
                    return FindConstructor(type, true, optionalParameterResolution, args);
                }

                throw new NotImplementedException("Unable to find the constructor.");
            }
            return filtered.FirstOrDefault(x => x.ParameterList.Length == args.Length) ?? filtered[0];
        }

        private ConstructorModel FindConstructor(Type type, bool nonPublic, bool fallbackToNonPublicConstructors, OptionalParameterResolutionMode optionalParameterResolution, params object?[] args)
        {
            if (args.Length == 0)
            {
                return FindPreferredConstructor(type, nonPublic, fallbackToNonPublicConstructors, optionalParameterResolution);
            }

            var all = GetConstructors(type, nonPublic, optionalParameterResolution, args);
            var filtered = all.Where(x => type.IsValidConstructor(x.ConstructorInfo!, args)).ToList();
            if (!filtered.Any())
            {
                if (!nonPublic && fallbackToNonPublicConstructors)
                {
                    return FindConstructor(type, true, fallbackToNonPublicConstructors, optionalParameterResolution, args);
                }

                throw new NotImplementedException("Unable to find the constructor.");
            }

            return filtered.FirstOrDefault(x => x.ParameterList.Length == args.Length) ?? filtered[0];
        }
        internal ConstructorModel FindPreferredConstructor(Type type, bool nonPublic, List<ConstructorInfo>? excludeList = null) =>
            FindPreferredConstructor(type, nonPublic, OptionalParameterResolution, excludeList);

        internal ConstructorModel FindPreferredConstructor(Type type, bool nonPublic, OptionalParameterResolutionMode optionalParameterResolution, List<ConstructorInfo>? excludeList = null)
        {
            var strict = Behavior.Has(MockFeatures.FailOnUnconfigured);
            return FindPreferredConstructor(type, nonPublic, !strict, optionalParameterResolution, excludeList);
        }

        private ConstructorModel FindPreferredConstructor(Type type, bool nonPublic, bool fallbackToNonPublicConstructors, OptionalParameterResolutionMode optionalParameterResolution, List<ConstructorInfo>? excludeList = null)
        {
            excludeList ??= new();

            var publicConstructors = GetConstructors(type, false, optionalParameterResolution)
                .Where(c => excludeList.TrueForAll(e => e != c.ConstructorInfo))
                .ToList();

            var preferredPublicConstructor = SelectPreferredConstructor(type, this.GetTestedConstructors(type, publicConstructors));
            if (preferredPublicConstructor != null)
            {
                return preferredPublicConstructor;
            }

            if (!nonPublic && fallbackToNonPublicConstructors)
            {
                return FindPreferredConstructor(type, true, false, optionalParameterResolution, excludeList);
            }

            var nonPublicConstructors = GetConstructors(type, true, optionalParameterResolution)
                .Where(c => excludeList.TrueForAll(e => e != c.ConstructorInfo))
                .Where(c => c.ConstructorInfo?.IsPublic == false)
                .ToList();

            return SelectPreferredConstructor(type, this.GetTestedConstructors(type, nonPublicConstructors))
                ?? throw new NotImplementedException("Unable to find the constructor.");
        }

        private ConstructorModel? SelectPreferredConstructor(Type type, List<ConstructorModel>? constructors)
        {
            constructors ??= [];
            if (constructors.Count == 0)
            {
                return null;
            }

            var largestArity = constructors.Max(c => c.ParameterList.Length);
            var bestMatches = constructors.Where(c => c.ParameterList.Length == largestArity).ToList();
            if (bestMatches.Count > 1)
            {
                throw this.GetAmbiguousConstructorImplementationException(type);
            }

            return bestMatches[0];
        }
        public bool HasParameterlessConstructor(Type type, bool nonPublic = false) => GetConstructors(type, nonPublic).Any(x => x.ParameterList.Length == 0);
        internal List<ConstructorModel> GetConstructors(Type type, bool nonPublic, params object?[] values) =>
            GetConstructors(type, nonPublic, OptionalParameterResolution, values);

        internal List<ConstructorModel> GetConstructors(Type type, bool nonPublic, OptionalParameterResolutionMode optionalParameterResolution, params object?[] values)
        {
            var flags = nonPublic ? BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public : BindingFlags.Instance | BindingFlags.Public;
            return type.GetConstructors(flags)
                .Where(c => c.GetParameters().All(p => p.ParameterType != type))
                .Select(ci => new ConstructorModel(ci, values.Length > 0 ? values : ci.GetParameters().Select(p => ResolveParameter(p, optionalParameterResolution)).ToArray()))
                .OrderBy(c => c.ParameterList.Length)
                .ToList();
        }
        internal static List<ConstructorInfo> GetConstructorsByType(bool nonPublic, Type type, params Type?[] parameterTypes) =>
            type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                .Where(x => (nonPublic || x.IsPublic) && x.IsValidConstructorByType(parameterTypes))
                .OrderBy(x => x.GetParameters().Length)
                .ToList();
        internal T? CreateInstanceInternal<T>(ConstructorInfo? info, params object?[] args) where T : class => CreateInstanceInternal(typeof(T), info, OptionalParameterResolution, args) as T;
        internal T? CreateInstanceInternal<T>(ConstructorInfo? info, OptionalParameterResolutionMode optionalParameterResolution, params object?[] args) where T : class => CreateInstanceInternal(typeof(T), info, optionalParameterResolution, args) as T;
        internal object? CreateInstanceInternal(Type type, ConstructorInfo? info, params object?[] args) => CreateInstanceInternal(type, info, OptionalParameterResolution, args);
        internal object? CreateInstanceInternal(Type type, ConstructorInfo? info, OptionalParameterResolutionMode optionalParameterResolution, params object?[] args)
        {
            ConstructorHistory.AddOrUpdate(type, new ConstructorModel(info, args));
            var paramList = info?.GetParameters().ToList() ?? new List<ParameterInfo>();
            var newArgs = args.ToList();
            if (args.Length < paramList.Count)
            {
                for (var i = args.Length; i < paramList.Count; i++)
                {
                    var p = paramList[i];
                    newArgs.Add(ResolveParameter(p, optionalParameterResolution));
                }
            }
            var obj = AddInjections(info?.Invoke(newArgs.ToArray()));
            return InnerMockResolution ? AddProperties(type, obj) : obj;
        }
        #endregion

        #region Provider Mock Creation
        /// <summary>
        /// Internal helper to build provider-first fast mock for a runtime <see cref="Type"/>
        /// </summary>
        internal IFastMock CreateFastMock(Type type, bool nonPublic = false, params object?[] args)
        {
            type = CleanType(type);
            if (Contains(type))
            {
                return GetMockModel(type).FastMock; // reuse existing
            }

            object?[] ctorArgs = Array.Empty<object?>();
            if (type.IsClass && !type.IsAbstract)
            {
                var constructor = this.GetTypeConstructor(type, nonPublic, args);
                ctorArgs = constructor.ParameterList ?? Array.Empty<object?>();
            }

            var isEntityFrameworkDbContextType = DatabaseSupportBridge.IsEntityFrameworkDbContextType(type);
            if (isEntityFrameworkDbContextType &&
                DatabaseSupportBridge.TryCreateLegacyDbContextMock(type, ShouldCreateStrictMocks() ? MockBehavior.Strict : MockBehavior.Loose, ctorArgs, out var legacyDbContextMock))
            {
                ArgumentNullException.ThrowIfNull(legacyDbContextMock);
                var model = AddMock(legacyDbContextMock, type, nonPublic: nonPublic);
                SetupMock(type, legacyDbContextMock);
                return model.FastMock;
            }

            var provider = MockingProviderRegistry.Default;
            var callBase = Behavior.Has(MockFeatures.CallBase) && provider.Capabilities.SupportsCallBase && !type.IsInterface;
            var options = new MockCreationOptions(ShouldCreateStrictMocks(), CallBase: callBase, ConstructorArgs: ctorArgs, AllowNonPublic: nonPublic);
            var fast = provider.CreateMock(type, options);
            AddFastMock(fast, type, nonPublic: nonPublic);
            SetupFastMock(type, fast);
            return fast;
        }

        [Obsolete("Use GetOrCreateMock(Type, ...) for provider-neutral mock creation. This legacy Moq compatibility API will be removed in v5.")]
        public List<MockModel> CreateMock(Type type, bool nonPublic = false, params object?[] args)
        {
            // Legacy Moq-oriented method – now a thin shim over provider-first fast mock creation.
            type = CleanType(type);
            if (Contains(type))
            {
                type.ThrowAlreadyExists();
            }

            var fast = CreateFastMock(type, nonPublic, args);

            // Attempt legacy surface hydration for backward compatibility (if provider exposes a Moq mock).
            var legacy = TryGetLegacyMock(fast);
            if (legacy != null)
            {
                // Attach the legacy Moq surface to the existing model after FastMock registration.
                AddMock(legacy, type, overwrite: true, nonPublic: nonPublic);
            }
            return mockCollection;
        }
        [Obsolete("Use GetOrCreateMock<T>(...) for provider-neutral mock creation. This legacy Moq compatibility API will be removed in v5.")]
        public List<MockModel> CreateMock<T>(bool nonPublic = false, params object?[] args) where T : class => CreateMock(typeof(T), nonPublic, args);

        [Obsolete("Use GetOrCreateMock<T>(...) and provider-neutral verification instead. This legacy Moq compatibility API will be removed in v5.")]
        public Mock<T> CreateMockInstance<T>(bool nonPublic = false, params object?[] args) where T : class
        {
            var legacy = CreateMockInstance(typeof(T), nonPublic, args);
            if (legacy is Mock<T> typed)
            {
                return typed;
            }

            throw new NotSupportedException($"Active provider '{MockingProviderRegistry.Default.GetType().Name}' does not expose Moq legacy surface for {typeof(T).Name}.");
        }

        [Obsolete("Use GetOrCreateMock(Type, ...) and provider-neutral verification instead. This legacy Moq compatibility API will be removed in v5.")]
        public Mock CreateMockInstance(Type type, bool nonPublic = false, params object?[] args)
        {
            if (type == null || (!type.IsClass && !type.IsInterface))
            {
                throw new ArgumentException("Type must be a class or interface.", nameof(type));
            }

            var provider = MockingProviderRegistry.Default;
            object?[] ctorArgs = Array.Empty<object?>();

            if (type.IsClass && !type.IsAbstract)
            {
                var constructor = GetTypeConstructor(type, nonPublic, args);
                ctorArgs = constructor.ParameterList ?? Array.Empty<object?>();
            }

            var callBase = Behavior.Has(MockFeatures.CallBase) && provider.Capabilities.SupportsCallBase && !type.IsInterface;
            var options = new MockCreationOptions(ShouldCreateStrictMocks(), CallBase: callBase, ConstructorArgs: ctorArgs, AllowNonPublic: nonPublic);
            var fast = provider.CreateMock(type, options);
            SetupFastMock(type, fast);
            var legacy = TryGetLegacyMock(fast);
            if (legacy != null)
            {
                legacy.RaiseIfNull();
                return legacy;
            }

            throw new NotSupportedException($"Active provider '{MockingProviderRegistry.Default.GetType().Name}' does not expose Moq legacy surface for {type.Name}.");
        }

        /// <summary>
        /// Creates a detached mock instance that is not added to the tracked mock collection.
        /// Useful when the same interface is needed multiple times without constructor injection override.
        /// </summary>
        [Obsolete("Use GetOrCreateMock<T>(...) on a dedicated Mocker instance when possible. This legacy Moq compatibility API will be removed in v5.")]
        public Mock<T> CreateDetachedMock<T>(bool nonPublic = false, params object?[] args) where T : class =>
            CreateMockInstance<T>(nonPublic, args);

        /// <summary>
        /// Creates a detached mock instance that is not added to the tracked mock collection.
        /// Useful when the same interface is needed multiple times without constructor injection override.
        /// </summary>
        [Obsolete("Use GetOrCreateMock(Type, ...) on a dedicated Mocker instance when possible. This legacy Moq compatibility API will be removed in v5.")]
        public Mock CreateDetachedMock(Type type, bool nonPublic = false, params object?[] args) =>
            CreateMockInstance(type, nonPublic, args);

        internal MockModel AddFastMock(IFastMock fastMock, Type type, bool overwrite = false, bool nonPublic = false)
        {
            if (Contains(type))
            {
                if (!overwrite)
                {
                    type.ThrowAlreadyExists();
                }

                var existing = GetMockModel(type);
                existing.FastMock = fastMock;
                var legacyExisting = TryGetLegacyMock(fastMock);
                if (legacyExisting != null && !existing.TryGetLegacyMock(out _))
                {
                    existing.SetLegacyMock(legacyExisting);
                }
                return existing;
            }
            var model = new MockModel(fastMock, nonPublic);
            mockCollection.Add(model);
            return model;
        }

        internal MockModel AddMock(Mock mock, Type type, bool overwrite = false, bool nonPublic = false)
        {
            // Fixed ordering: do not access mm.Mock (legacy getter) before assignment; rely on adapter introspection instead.
            if (Contains(type))
            {
                var mm = GetMockModel(type);
                if (!overwrite)
                {
                    type.ThrowAlreadyExists();
                }
                // Assign (setter refreshes adapter)
                mm.SetLegacyMock(mock);
                return mm;
            }
            // No existing model – create using adapter wrapper.
            mockCollection.Add(new MockModel(type, mock, nonPublic));
            return GetMockModel(type);
        }
        [Obsolete("Use GetOrCreateMock<T>(...) for tracked mocks or AddType<T>(...) for concrete instances. This legacy Moq compatibility API will be removed in v5.")]
        public MockModel<T> AddMock<T>(Mock<T> mock, bool overwrite = false, bool nonPublic = false) where T : class
            => new MockModel<T>(AddMock((Mock)mock, typeof(T), overwrite, nonPublic));
        private static Mock? TryGetLegacyMock(IFastMock fastMock)
        {
            if (fastMock.NativeMock is Mock nativeMock)
            {
                return nativeMock;
            }

            return null;
        }
        #endregion

        #region Mock Retrieval & Setup
        [Obsolete("Use GetOrCreateMock(Type, ...) for provider-neutral access. This legacy Moq compatibility API will be removed in v5.")]
        public Mock GetMock(Type type, params object?[]? args)
        {
            type = CleanType(type);
            if (!Contains(type))
            {
                CreateMock(type, false, args ?? Array.Empty<object?>());
            }

            return GetRequiredMock(type);
        }
        /// <summary>
        /// Returns a tracked mock for the requested type, creating it if needed.
        /// </summary>
        /// <example>
        /// <para><c>GetMock&lt;T&gt;()</c> remains the Moq-compatibility path. For new examples, prefer <c>GetOrCreateMock&lt;T&gt;()</c> plus provider-neutral verification.</para>
        /// <code language="csharp"><![CDATA[
        /// var mocker = new Mocker();
        ///
        /// mocker.GetOrCreateMock<IPaymentGateway>()
        ///     .Setup(x => x.Charge(125.50m))
        ///     .Returns(true);
        ///
        /// var service = mocker.GetRequiredObject<CheckoutService>();
        /// var approved = service.SubmitPayment(125.50m);
        ///
        /// approved.Should().BeTrue();
        /// mocker.Verify<IPaymentGateway>(x => x.Charge(125.50m), TimesSpec.Once);
        /// ]]></code>
        /// </example>
        [Obsolete("Use GetOrCreateMock<T>(...) for provider-neutral access. This legacy Moq compatibility API will be removed in v5.")]
        public Mock<T> GetMock<T>(params object?[] args) where T : class => (Mock<T>)GetMock(typeof(T), args);
        [Obsolete("Use GetOrCreateMock<T>(...) for provider-neutral access. This legacy Moq compatibility API will be removed in v5.")]
        public Mock<T> GetMock<T>(Action<Mock<T>> action, params object?[] args) where T : class
        {
            var m = GetMock<T>(args);
            action?.Invoke(m);
            return m;
        }
        [Obsolete("Use GetOrCreateMock(Type, ...) with provider-neutral verification instead. This legacy Moq compatibility API will be removed in v5.")]
        public Mock GetRequiredMock(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (!type.IsClass && !type.IsInterface)
            {
                throw new ArgumentException("Type must be a class.", nameof(type));
            }

            var model = mockCollection.First(x => x.Type == type);
            if (!model.TryGetLegacyMock(out var mock))
            {
                throw new NotSupportedException($"Active provider '{MockingProviderRegistry.Default.GetType().Name}' does not expose a legacy Moq.Mock instance for {type.Name}.");
            }

            mock.RaiseIfNull();
            return mock;
        }
        [Obsolete("Use GetOrCreateMock<T>(...) with provider-neutral verification instead. This legacy Moq compatibility API will be removed in v5.")]
        public Mock<T> GetRequiredMock<T>() where T : class => (Mock<T>)GetRequiredMock(typeof(T));
        public object GetNativeMock(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            type = CleanType(type);
            if (!Contains(type))
            {
                _ = GetOrCreateFastMock(type);
            }

            return GetMockModel(type).NativeMock;
        }
        public object GetNativeMock<T>(params object?[] args) where T : class
        {
            if (!Contains<T>())
            {
                _ = GetOrCreateFastMock(typeof(T), false, args ?? Array.Empty<object?>());
            }

            return GetMockModel(typeof(T)).NativeMock;
        }
        public MockModel<T> GetMockModel<T>() where T : class => new(GetMockModel(typeof(T)));
        public int GetMockModelIndexOf(Type type, bool throwIfMissing = true)
        {
            var idx = mockCollection.FindIndex(m => m.Type == type);
            if (idx >= 0)
            {
                return idx;
            }

            if (!throwIfMissing)
            {
                throw new NotImplementedException("Unable to find the constructor.");
            }

            _ = GetOrCreateFastMock(type);
            idx = mockCollection.FindIndex(m => m.Type == type);
            if (idx < 0)
            {
                throw new NotImplementedException("Unable to find the constructor.");
            }

            return idx;
        }
        [Obsolete("Use provider-neutral mock lifecycle APIs instead. This legacy Moq compatibility API will be removed in v5.")]
        public bool RemoveMock(Mock mock)
        {
            var model = mockCollection.FirstOrDefault(m => m.TryGetLegacyMock(out var legacyMock) && legacyMock == mock);
            if (model == null)
            {
                return false;
            }

            mockCollection.Remove(model);
            return true;
        }

        internal void SetupMock(Type type, Mock oMock)
        {
            SetupFastMock(type, MockingProviderRegistry.WrapLegacy(oMock, type));
        }

        internal void SetupFastMock(Type type, IFastMock fastMock)
        {
            ArgumentNullException.ThrowIfNull(fastMock);

            var provider = MockingProviderRegistry.Default;
            if (Behavior.Has(MockFeatures.AutoSetupProperties) && provider.Capabilities.SupportsSetupAllProperties)
            {
                provider.ConfigureProperties(fastMock);
            }

            if (InnerMockResolution && Behavior.Has(MockFeatures.AutoInjectDependencies))
            {
                AddProperties(type, fastMock.Instance);
            }

            if (Behavior.Has(MockFeatures.LoggerCallback) && provider.Capabilities.SupportsLoggerCapture && type.IsAssignableTo(typeof(ILogger)))
            {
                provider.ConfigureLogger(fastMock, LoggingCallback);
            }

            KnownTypeRegistry.ConfigureMock(this, type, fastMock);

            if (Behavior.Has(MockFeatures.AutoInjectDependencies))
            {
                AddInjections(fastMock.Instance, GetTypeModel(type).InstanceType);
            }

            KnownTypeRegistry.ApplyObjectDefaults(this, fastMock.Instance);
        }

        private static void TrySetupAllProperties(Mock mock)
        {
            var t = mock.GetType();
            if (!t.IsGenericType)
            {
                return;
            }

            try
            {
                var ext = typeof(MockExtensions).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "SetupAllProperties" && m.GetParameters().Length == 1);
                if (ext == null)
                {
                    return;
                }

                var gm = ext.MakeGenericMethod(t.GetGenericArguments()[0]);
                gm.Invoke(null, new object[] { mock });
            }
            catch { }
        }

        #endregion

        #region FastMock helpers / collection helpers
        internal IFastMock GetOrCreateFastMock(Type type, MockRequestOptions? options)
        {
            ArgumentNullException.ThrowIfNull(type);

            options ??= new MockRequestOptions();
            var args = options.ConstructorArgs ?? Array.Empty<object?>();
            var allowNonPublicConstructors = ShouldAllowNonPublicConstructorsForMockRequest(options.AllowNonPublicConstructors);

            return options.ServiceKey == null
                ? GetOrCreateFastMock(type, allowNonPublicConstructors, args)
                : GetOrCreateFastMock(type, options.ServiceKey, allowNonPublicConstructors, args);
        }

        internal IFastMock GetOrCreateFastMock(Type type, bool nonPublic = false, params object?[] args)
        {
            type = CleanType(type);
            if (!Contains(type))
            {
                return CreateFastMock(type, nonPublic, args);
            }

            return GetMockModel(type).FastMock;
        }
        internal IFastMock GetOrCreateFastMock(Type type, object serviceKey, bool nonPublic = false, params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(serviceKey);

            type = CleanType(type);
            if (!Contains(type, serviceKey))
            {
                CreateKeyedMock(type, serviceKey, nonPublic, args);
            }

            return keyedMockCollection[CreateServiceRegistrationKey(type, serviceKey)].FastMock;
        }
        internal IFastMock<T> GetOrCreateTypedFastMock<T>(MockRequestOptions? options = null) where T : class
        {
            options ??= new MockRequestOptions();
            var args = options.ConstructorArgs ?? Array.Empty<object?>();
            var allowNonPublicConstructors = ShouldAllowNonPublicConstructorsForMockRequest(options.AllowNonPublicConstructors);

            MockModel model = options.ServiceKey == null
                ? GetMockModelFast(typeof(T), allowNonPublicConstructors, args)
                : GetKeyedMockModelFast(typeof(T), options.ServiceKey, allowNonPublicConstructors, args);

            if (model.FastMock is IFastMock<T> typedFastMock)
            {
                return typedFastMock;
            }

            if (model.TryGetLegacyMock(out var legacyMock) && legacyMock is Mock<T> typedLegacyMock)
            {
                var upgraded = MockingProviderRegistry.WrapLegacy(typedLegacyMock, typeof(T));
                if (upgraded is not IFastMock<T> typedUpgraded)
                {
                    throw new NotSupportedException($"Stored mock for {typeof(T).Name} could not be rewrapped as a typed provider-first mock.");
                }

                model.FastMock = typedUpgraded;
                return typedUpgraded;
            }

            throw new NotSupportedException($"Stored mock for {typeof(T).Name} is not available as a typed provider-first mock.");
        }

        internal MockModel GetMockModelFast(Type type, bool nonPublic = false, params object?[] args)
        {
            type = CleanType(type);
            if (!Contains(type))
            {
                _ = CreateFastMock(type, nonPublic, args);
            }

            return GetMockModel(type);
        }

        internal MockModel GetKeyedMockModelFast(Type type, object serviceKey, bool nonPublic = false, params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(serviceKey);

            type = CleanType(type);
            if (!Contains(type, serviceKey))
            {
                CreateKeyedMock(type, serviceKey, nonPublic, args);
            }

            return keyedMockCollection[CreateServiceRegistrationKey(type, serviceKey)];
        }

        internal bool Contains(Type type) => mockCollection.Any(m => m.Type == type);
        internal bool Contains(Type type, object serviceKey)
        {
            ArgumentNullException.ThrowIfNull(serviceKey);
            return keyedMockCollection.ContainsKey(CreateServiceRegistrationKey(type, serviceKey));
        }
        internal bool HasTypeRegistration(Type type) => typeMap.ContainsKey(type);
        internal bool Contains<T>() => Contains(typeof(T));
        internal MockModel GetMockModel(Type type, Mock? mock = null, bool autoCreate = true) => mockCollection.First(m => m.Type == type);

        private MockModel CreateKeyedMock(Type type, object serviceKey, bool nonPublic = false, params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(serviceKey);

            type = CleanType(type);
            var registrationKey = CreateServiceRegistrationKey(type, serviceKey);
            if (keyedMockCollection.TryGetValue(registrationKey, out var existing))
            {
                return existing;
            }

            var provider = MockingProviderRegistry.Default;
            object?[] ctorArgs = Array.Empty<object?>();

            if (type.IsClass && !type.IsAbstract)
            {
                var constructor = GetTypeConstructor(type, nonPublic, args);
                ctorArgs = constructor.ParameterList ?? Array.Empty<object?>();
            }

            var callBase = Behavior.Has(MockFeatures.CallBase) && provider.Capabilities.SupportsCallBase && !type.IsInterface;
            var options = new MockCreationOptions(ShouldCreateStrictMocks(), CallBase: callBase, ConstructorArgs: ctorArgs, AllowNonPublic: nonPublic);
            var fast = provider.CreateMock(type, options);
            SetupFastMock(type, fast);

            var model = new MockModel(fast, nonPublic);
            keyedMockCollection[registrationKey] = model;
            return model;
        }
        #endregion

        #region Legacy Helper Methods (Batch C)
        public IFileSystem GetFileSystem(Action<MockFileSystem>? configure = null)
        {
            configure?.Invoke(fileSystem);
            return fileSystem;
        }
        public IFileSystem GetFileSystem() => fileSystem;

        public static List<T> GetList<T>(int count, Func<T> factory)
        {
            var list = new List<T>(count);
            for (var i = 0; i < count; i++)
            {
                list.Add(factory());
            }

            return list;
        }
        public static List<T> GetList<T>(int count, Func<int, T> factory)
        {
            var list = new List<T>(count);
            for (var i = 0; i < count; i++)
            {
                list.Add(factory(i));
            }

            return list;
        }
        public static List<T> GetList<T>(int count, Func<int, T> factory, Action<int, T> init)
        {
            var list = new List<T>(count);
            for (var i = 0; i < count; i++)
            {
                var item = factory(i);
                init(i, item);
                list.Add(item);
            }
            return list;
        }

        public object?[] GetMethodArgData(MethodBase? method) => GetMethodArgData(method, new InvocationOptions
        {
            OptionalParameterResolution = OptionalParameterResolution,
        });

        public object?[] GetMethodArgData(MethodBase? method, InvocationOptions? options)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            options ??= new InvocationOptions
            {
                OptionalParameterResolution = OptionalParameterResolution,
            };

            return method.GetParameters().Select(p => ResolveParameter(p, options.OptionalParameterResolution)).ToArray();
        }
        public object?[] GetMethodDefaultData(MethodBase? method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            return method.GetParameters().Select(p => p.ParameterType.GetDefaultValue()).ToArray();
        }
        public object?[] GetArgData<T>() where T : class
        {
            return GetArgData<T>(InstanceCreationFlags.None);
        }

        public object?[] GetArgData<T>(OptionalParameterResolutionMode optionalParameterResolution) where T : class =>
            GetArgData<T>(publicOnly: null, optionalParameterResolution);

        public object?[] GetArgData<T>(InstanceCreationFlags flags) where T : class
        {
            var publicOnly = ResolvePublicOnlyOverride(flags);
            var optionalParameterResolution = ResolveOptionalParameterResolution(flags);
            return GetArgData<T>(publicOnly, optionalParameterResolution);
        }

        internal object?[] GetArgData<T>(bool? publicOnly, OptionalParameterResolutionMode optionalParameterResolution) where T : class
        {
            var type = typeof(T).IsInterface ? GetTypeFromInterface<T>() : new InstanceModel<T>();
            var constructor = FindPreferredConstructor(type.InstanceType, false, ShouldFallbackToNonPublicConstructors(publicOnly), optionalParameterResolution);
            return constructor.ConstructorInfo == null
                ? Array.Empty<object?>()
                : constructor.ConstructorInfo.GetParameters().Select(p => ResolveParameter(p, optionalParameterResolution)).ToArray();
        }

        private object?[] BuildInvocationArgs(ParameterInfo[] parameters, InvocationOptions? options, object?[] provided)
        {
            options ??= new InvocationOptions
            {
                OptionalParameterResolution = OptionalParameterResolution,
            };

            var list = new List<object?>();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < provided.Length && provided[i] != null)
                {
                    list.Add(provided[i]);
                    continue;
                }

                var parameter = parameters[i];
                if (i < provided.Length && provided[i] == null && (parameter.ParameterType.IsClass || parameter.ParameterType.IsInterface || parameter.ParameterType.IsNullableType()))
                {
                    list.Add(null);
                    continue;
                }

                list.Add(ResolveParameter(parameter, options.OptionalParameterResolution));
            }

            return list.ToArray();
        }

        public object? InvokeMethod(object? instance, string methodName, bool nonPublic = false, params object?[] args) =>
            InvokeMethod(new InvocationOptions
            {
                OptionalParameterResolution = OptionalParameterResolution,
            }, instance, methodName, nonPublic, args);

        public object? InvokeMethod(InvocationOptions? options, object? instance, string methodName, bool nonPublic = false, params object?[] args)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            options ??= new InvocationOptions
            {
                OptionalParameterResolution = OptionalParameterResolution,
                FallbackToNonPublicMethods = Policy.DefaultFallbackToNonPublicMethods,
            };

            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | (nonPublic ? BindingFlags.NonPublic : 0);
            var method = instance.GetType().GetMethod(methodName, flags);
            if (method == null)
            {
                if (!nonPublic && ShouldFallbackToNonPublicMethods(options))
                {
                    return InvokeMethod(options, instance, methodName, true, args);
                }

                throw new ArgumentOutOfRangeException(nameof(methodName));
            }

            return method.Invoke(instance, flags, null, BuildInvocationArgs(method.GetParameters(), options, args), null);
        }
        public object? InvokeMethod<T>(object? instance, string methodName, bool nonPublic = false, params object?[] args) =>
            InvokeMethod<T>(new InvocationOptions
            {
                OptionalParameterResolution = OptionalParameterResolution,
            }, instance, methodName, nonPublic, args);

        public object? InvokeMethod<T>(InvocationOptions? options, object? instance, string methodName, bool nonPublic = false, params object?[] args)
        {
            options ??= new InvocationOptions
            {
                OptionalParameterResolution = OptionalParameterResolution,
                FallbackToNonPublicMethods = Policy.DefaultFallbackToNonPublicMethods,
            };

            var type = typeof(T).IsInterface ? GetTypeFromInterface(typeof(T)) : typeof(T);
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | (nonPublic ? BindingFlags.NonPublic : 0);
            var method = type.GetMethod(methodName, flags);
            if (method == null)
            {
                if (!nonPublic && ShouldFallbackToNonPublicMethods(options))
                {
                    return InvokeMethod<T>(options, instance, methodName, true, args);
                }

                throw new ArgumentOutOfRangeException(nameof(methodName));
            }

            return method.Invoke(instance, flags, null, BuildInvocationArgs(method.GetParameters(), options, args), null);
        }

        private bool ShouldFallbackToNonPublicMethods(InvocationOptions options)
        {
            return options.FallbackToNonPublicMethods ?? Policy.DefaultFallbackToNonPublicMethods;
        }
        public object? InvokeMethod<T>(string methodName, bool nonPublic = false, params object?[] args) =>
            InvokeMethod<T>((object?)null, methodName, nonPublic, args);
        public object? InvokeMethod<T>(InvocationOptions? options, string methodName, bool nonPublic = false, params object?[] args) => InvokeMethod<T>(options, null, methodName, nonPublic, args);

        private object?[] BuildInvocationArgs(Delegate del, InvocationOptions? options, object?[] provided)
        {
            return BuildInvocationArgs(del.Method.GetParameters(), options, provided);
        }
        public TReturn CallMethod<TReturn>(Delegate del, params object?[] args) =>
            CallMethod<TReturn>(new InvocationOptions
            {
                OptionalParameterResolution = OptionalParameterResolution,
            }, del, args);

        public TReturn CallMethod<TReturn>(InvocationOptions? options, Delegate del, params object?[] args)
        {
            if (del == null)
            {
                throw new ArgumentNullException(nameof(del));
            }

            var invocationArgs = BuildInvocationArgs(del, options, args);
            try
            {
                var result = del.DynamicInvoke(invocationArgs);
                return (TReturn)result!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is { } inner)
            {
                throw inner;
            }
        }
        public void CallMethod(Delegate del, params object?[] args) => CallMethod<object?>(del, args);
        public void CallMethod(InvocationOptions? options, Delegate del, params object?[] args) => CallMethod<object?>(options, del, args);
        public TReturn CallMethod<TReturn>(Func<TReturn> func, params object?[] args) => CallMethod<TReturn>((Delegate)func, args);
        public TReturn CallMethod<TReturn>(InvocationOptions? options, Func<TReturn> func, params object?[] args) => CallMethod<TReturn>(options, (Delegate)func, args);
        public void CallMethod(Action action, params object?[] args) => CallMethod<object?>((Delegate)action, args);
        public void CallMethod(InvocationOptions? options, Action action, params object?[] args) => CallMethod<object?>(options, (Delegate)action, args);
        public async Task<TReturn> CallMethodAsync<TReturn>(Delegate del, params object?[] args)
        {
            return await CallMethodAsync<TReturn>(new InvocationOptions
            {
                OptionalParameterResolution = OptionalParameterResolution,
            }, del, args).ConfigureAwait(false);
        }

        public async Task<TReturn> CallMethodAsync<TReturn>(InvocationOptions? options, Delegate del, params object?[] args)
        {
            var result = CallMethod<object>(options, del, args);
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                if (task.GetType().IsGenericType)
                {
                    return (TReturn)task.GetType().GetProperty("Result")!.GetValue(task)!;
                }
                return default!;
            }
            return (TReturn)result;
        }

        [Obsolete("Use GetMock<T>() and configure the returned mock directly. Initialize<T>() is a compatibility wrapper and may be removed in a future major version.")]
        public void Initialize<T>(Action<Mock<T>> init) where T : class
        {
            var m = GetMock<T>();
            init?.Invoke(m);
        }

        public async Task<string> GetStringContent(HttpContent? content) => content == null ? string.Empty : await content.ReadAsStringAsync().ConfigureAwait(false);

        public T GetRequiredObject<T>() where T : class => GetObject<T>() ?? throw new InvalidOperationException($"Unable to resolve object of type {typeof(T).Name}.");
        #endregion
    }
}

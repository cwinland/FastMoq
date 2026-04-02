using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
    public partial class Mocker
    {
        public const string SETUP_ALL_PROPERTIES_METHOD_NAME = "SetupAllProperties";
        public const string SETUP = "Setup";

        public readonly MockFileSystem fileSystem;
        protected internal readonly List<Type> creatingTypeList = new();
        protected internal readonly List<MockModel> mockCollection = new();
        private readonly List<KnownTypeRegistration> knownTypeRegistrations = new();
        internal Dictionary<Type, IInstanceModel> typeMap = new();
        private readonly ObservableExceptionLog exceptionLog = new();
        public ConstructorHistory ConstructorHistory { get; } = new();

        public Action<LogLevel, EventId, string> LoggingCallback { get; }
        public OptionalParameterResolutionMode OptionalParameterResolution { get; set; } = OptionalParameterResolutionMode.UseDefaultOrNull;

        /// <summary>
        /// Obsolete compatibility alias for <see cref="OptionalParameterResolution"/>.
        /// Prefer explicit <see cref="InstanceCreationOptions"/> or <see cref="InvocationOptions"/> in new code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("MockOptional is obsolete and kept only for compatibility. Use OptionalParameterResolution, InstanceCreationOptions, InvocationOptions, or ComponentCreationOptions instead.")]
        public bool MockOptional
        {
            get => OptionalParameterResolution == OptionalParameterResolutionMode.ResolveViaMocker;
            set => OptionalParameterResolution = value
                ? OptionalParameterResolutionMode.ResolveViaMocker
                : OptionalParameterResolutionMode.UseDefaultOrNull;
        }
        public DbConnection DbConnection { get; internal set; } = new SqliteConnection("DataSource=:memory:");
        public ObservableExceptionLog ExceptionLog => exceptionLog;
        public HttpClient HttpClient { get; }
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
            }
        }

        /// <summary>
        /// Applies the predefined strict behavior preset.
        /// This is broader than <see cref="Strict"/>, which is retained for backward compatibility.
        /// </summary>
        public void UseStrictPreset() => Behavior = MockBehaviorOptions.StrictPreset.Clone();

        /// <summary>
        /// Applies the predefined lenient behavior preset.
        /// This is broader than disabling <see cref="MockFeatures.FailOnUnconfigured"/> alone.
        /// </summary>
        public void UseLenientPreset() => Behavior = MockBehaviorOptions.LenientPreset.Clone();

        #region Ctors
        public Mocker() : this((_, _, _) => { }) { }
        public Mocker(Action<LogLevel, EventId, string> loggingCallback)
        {
            ProviderBootstrap.Ensure();
            fileSystem = new MockFileSystem();
            HttpClient = this.CreateHttpClient();
            LoggingCallback = loggingCallback;
        }
        public Mocker(Dictionary<Type, IInstanceModel> map) : this() => typeMap = map;
        public Mocker(Dictionary<Type, IInstanceModel> map, Action<LogLevel, EventId, string> loggingCallback) : this(loggingCallback) => typeMap = map;
        #endregion

        #region Type Mapping
        internal IReadOnlyList<KnownTypeRegistration> KnownTypeRegistrations => knownTypeRegistrations;

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
        internal static Type CleanType(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Mock<>) ? type.GetGenericArguments()[0] : type;
        #endregion

        #region Injection / Object Creation
        internal object? GetParameter(ParameterInfo parameter)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            var pt = parameter.ParameterType;
            if (!typeMap.ContainsKey(pt) && !pt.IsClass && !pt.IsInterface)
            {
                return pt.GetDefaultValue();
            }

            var m = GetTypeModel(pt);
            return m.CreateFunc != null ? m.CreateFunc.Invoke(this, parameter) : (!pt.IsSealed ? GetObject(pt) : pt.GetDefaultValue());
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

            var strict = Behavior.Has(MockFeatures.FailOnUnconfigured);
            if ((type.IsClass || type.IsInterface) && !type.IsSealed)
            {
                var fast = GetOrCreateFastMock(type);
                var provider = MockingProviderRegistry.Default;
                if (Behavior.Has(MockFeatures.AutoSetupProperties))
                {
                    provider.ConfigureProperties(fast, strict);
                }
                if (Behavior.Has(MockFeatures.LoggerCallback))
                {
                    var mockedType = fast.MockedType;
                    if (typeof(ILogger).IsAssignableFrom(mockedType))
                    {
                        try { provider.ConfigureLogger(fast, LoggingCallback); } catch { }
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
            var def = type.GetDefaultValue();
            initAction?.Invoke(def);
            return def;
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
        public T? GetObject<T>() where T : class => GetObject(typeof(T)) as T;
        public T? GetObject<T>(Action<T?> init) where T : class => GetObject(typeof(T), o => init((T?)o)) as T;
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
            CreateInstance<T>(CreateDefaultInstanceCreationOptions(usePredefinedFileSystem: true, allowNonPublicConstructors: false), args);

        /// <summary>
        /// Creates an instance of <typeparamref name="T"/> using a single options object instead of separate public/non-public entry points.
        /// </summary>
        public T? CreateInstance<T>(InstanceCreationOptions options, params object?[] args) where T : class =>
            CreateInstanceCore<T>(options ?? throw new ArgumentNullException(nameof(options)), args);

        public T? CreateInstance<T>(bool usePredefinedFileSystem, params object?[] args) where T : class =>
            CreateInstanceCore<T>(CreateDefaultInstanceCreationOptions(usePredefinedFileSystem, allowNonPublicConstructors: false), args);

        public T? CreateInstanceNonPublic<T>(params object?[] args) where T : class =>
            CreateInstanceCore<T>(CreateDefaultInstanceCreationOptions(usePredefinedFileSystem: false, allowNonPublicConstructors: true), args);

        public T? CreateInstanceByType<T>(params Type?[] parameterTypes) where T : class
        {
            return CreateInstanceByType<T>(CreateDefaultInstanceCreationOptions(usePredefinedFileSystem: false, allowNonPublicConstructors: true), parameterTypes);
        }

        public T? CreateInstanceByType<T>(InstanceCreationOptions options, params Type?[] parameterTypes) where T : class
        {
            ArgumentNullException.ThrowIfNull(options);

            return CreateInstanceCore<T>(new InstanceCreationOptions
            {
                UsePredefinedFileSystem = options.UsePredefinedFileSystem,
                AllowNonPublicConstructors = options.AllowNonPublicConstructors,
                ConstructorParameterTypes = parameterTypes,
                OptionalParameterResolution = options.OptionalParameterResolution,
            }, Array.Empty<object?>());
        }

        private InstanceCreationOptions CreateDefaultInstanceCreationOptions(bool usePredefinedFileSystem, bool allowNonPublicConstructors)
        {
            return new InstanceCreationOptions
            {
                UsePredefinedFileSystem = usePredefinedFileSystem,
                AllowNonPublicConstructors = allowNonPublicConstructors,
                OptionalParameterResolution = OptionalParameterResolution,
            };
        }

        /// <summary>
        /// Centralized creation logic used by all public CreateInstance* methods.
        /// </summary>
        private T? CreateInstanceCore<T>(InstanceCreationOptions options, object?[] args) where T : class
        {
            ArgumentNullException.ThrowIfNull(options);

            if (options.UsePredefinedFileSystem && fileSystem is T fs)
            {
                return fs;
            }

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

            if (options.ConstructorParameterTypes != null)
            {
                var constructor = FindConstructorByType(targetType, options.AllowNonPublicConstructors, options.ConstructorParameterTypes);
                return CreateInstanceInternal(targetType, constructor, options.OptionalParameterResolution, args) as T;
            }

            var ctorModel = GetConstructorByArgs(args, targetType, options.AllowNonPublicConstructors, options.OptionalParameterResolution);
            var created = CreateInstanceInternal(targetType, ctorModel.ConstructorInfo, options.OptionalParameterResolution, ctorModel.ParameterList);
            return created as T;
        }

        private ConstructorModel GetConstructorByArgs(object?[] args, Type instanceType, bool nonPublic, OptionalParameterResolutionMode optionalParameterResolution)
        {
            return args.Length > 0
                ? FindConstructor(instanceType, nonPublic, optionalParameterResolution, args)
                : FindConstructor(false, instanceType, nonPublic, optionalParameterResolution);
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
                        : FindConstructor(bestGuess: true, type, nonPublic, optionalParameterResolution);
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
        internal ConstructorModel FindConstructor(Type type, bool nonPublic, params object?[] args) =>
            FindConstructor(type, nonPublic, OptionalParameterResolution, args);

        internal ConstructorModel FindConstructor(Type type, bool nonPublic, OptionalParameterResolutionMode optionalParameterResolution, params object?[] args)
        {
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
        internal ConstructorModel FindConstructor(bool bestGuess, Type type, bool nonPublic, List<ConstructorInfo>? excludeList = null) =>
            FindConstructor(bestGuess, type, nonPublic, OptionalParameterResolution, excludeList);

        internal ConstructorModel FindConstructor(bool bestGuess, Type type, bool nonPublic, OptionalParameterResolutionMode optionalParameterResolution, List<ConstructorInfo>? excludeList = null)
        {
            var strict = Behavior.Has(MockFeatures.FailOnUnconfigured);
            excludeList ??= new();
            var ctors = GetConstructors(type, nonPublic, optionalParameterResolution).Where(c => excludeList.TrueForAll(e => e != c.ConstructorInfo)).ToList();
            if (!bestGuess && ctors.Count(x => x.ParameterList.Length > 0) > 1)
            {
                throw this.GetAmbiguousConstructorImplementationException(type);
            }

            if (!(ctors.Count > 0) && !nonPublic && !strict)
            {
                return FindConstructor(bestGuess, type, true, optionalParameterResolution, excludeList);
            }

            var validCtors = this.GetTestedConstructors(type, ctors);
            return validCtors.LastOrDefault() ?? throw new NotImplementedException("Unable to find the constructor.");
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

            var provider = MockingProviderRegistry.Default;
            object?[] ctorArgs = Array.Empty<object?>();
            if (type.IsClass && !type.IsAbstract)
            {
                var constructor = this.GetTypeConstructor(type, nonPublic, args);
                ctorArgs = constructor.ParameterList ?? Array.Empty<object?>();
            }
            var strict = Behavior.Has(MockFeatures.FailOnUnconfigured);
            var callBase = Behavior.Has(MockFeatures.CallBase) && provider.Capabilities.SupportsCallBase && !type.IsInterface;
            var options = new MockCreationOptions(strict, CallBase: callBase, ConstructorArgs: ctorArgs, AllowNonPublic: nonPublic);
            var fast = provider.CreateMock(type, options);
            AddFastMock(fast, type, nonPublic: nonPublic);
            SetupFastMock(type, fast);
            return fast;
        }

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
        public List<MockModel> CreateMock<T>(bool nonPublic = false, params object?[] args) where T : class => CreateMock(typeof(T), nonPublic, args);

        public Mock<T> CreateMockInstance<T>(bool nonPublic = false, params object?[] args) where T : class
        {
            var legacy = CreateMockInstance(typeof(T), nonPublic, args);
            if (legacy is Mock<T> typed)
            {
                return typed;
            }

            throw new NotSupportedException($"Active provider '{MockingProviderRegistry.Default.GetType().Name}' does not expose Moq legacy surface for {typeof(T).Name}.");
        }

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

            var strict = Behavior.Has(MockFeatures.FailOnUnconfigured);
            var callBase = Behavior.Has(MockFeatures.CallBase) && provider.Capabilities.SupportsCallBase && !type.IsInterface;
            var options = new MockCreationOptions(strict, CallBase: callBase, ConstructorArgs: ctorArgs, AllowNonPublic: nonPublic);
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
        public Mock GetMock(Type type, params object?[]? args)
        {
            type = CleanType(type);
            if (!Contains(type))
            {
                CreateMock(type, false, args ?? Array.Empty<object?>());
            }

            return GetRequiredMock(type);
        }
        public Mock<T> GetMock<T>(params object?[] args) where T : class => (Mock<T>)GetMock(typeof(T), args);
        public Mock<T> GetMock<T>(Action<Mock<T>> action, params object?[] args) where T : class
        {
            var m = GetMock<T>(args);
            action?.Invoke(m);
            return m;
        }
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
        public Mock<T> GetRequiredMock<T>() where T : class => (Mock<T>)GetRequiredMock(typeof(T));
        public object GetNativeMock(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            type = CleanType(type);
            if (!Contains(type))
            {
                CreateMock(type, false);
            }

            return GetMockModel(type).NativeMock;
        }
        public object GetNativeMock<T>(params object?[] args) where T : class
        {
            if (!Contains<T>())
            {
                CreateMock(typeof(T), false, args ?? Array.Empty<object?>());
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

            GetMock(type);
            idx = mockCollection.FindIndex(m => m.Type == type);
            if (idx < 0)
            {
                throw new NotImplementedException("Unable to find the constructor.");
            }

            return idx;
        }
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
            SetupFastMock(type, new FastMoq.Providers.MoqProvider.MoqMockAdapter(oMock));
        }

        internal void SetupFastMock(Type type, IFastMock fastMock)
        {
            ArgumentNullException.ThrowIfNull(fastMock);

            var strict = Behavior.Has(MockFeatures.FailOnUnconfigured);
            var provider = MockingProviderRegistry.Default;
            if (!strict)
            {
                if (Behavior.Has(MockFeatures.AutoSetupProperties))
                {
                    provider.ConfigureProperties(fastMock, strict);
                }

                if (InnerMockResolution && Behavior.Has(MockFeatures.AutoInjectDependencies))
                {
                    AddProperties(type, fastMock.Instance);
                }

                if (Behavior.Has(MockFeatures.LoggerCallback) && type.IsAssignableTo(typeof(ILogger)))
                {
                    provider.ConfigureLogger(fastMock, LoggingCallback);
                }
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

        internal static void SetupLoggerCallback<TLogger>(Mock<TLogger> logger, Action<LogLevel, EventId, string> callback) where TLogger : class, ILogger
        {
            logger.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()))
                  .Callback((LogLevel level, EventId evt, object state, Exception? ex, object formatter) => callback(level, evt, state?.ToString() ?? string.Empty));
        }
        #endregion

        #region FastMock helpers / collection helpers
        internal IFastMock GetOrCreateFastMock(Type type, bool nonPublic = false, params object?[] args)
        {
            type = CleanType(type);
            if (!Contains(type))
            {
                CreateMock(type, nonPublic, args);
            }

            return GetMockModel(type).FastMock;
        }
        internal IFastMock<T> GetFastMock<T>(bool nonPublic = false, params object?[] args) where T : class
        {
            var model = GetMockModelFast(typeof(T), nonPublic, args);
            if (model.FastMock is IFastMock<T> typedFastMock)
            {
                return typedFastMock;
            }

            if (model.TryGetLegacyMock(out var legacyMock) && legacyMock is Mock<T> typedLegacyMock)
            {
                var upgraded = new FastMoq.Providers.MoqProvider.MoqMockAdapter<T>(typedLegacyMock);
                model.FastMock = upgraded;
                return upgraded;
            }

            throw new NotSupportedException($"Stored mock for {typeof(T).Name} is not available as a typed provider-first mock.");
        }
        internal MockModel GetMockModelFast(Type type, bool nonPublic = false, params object?[] args)
        {
            type = CleanType(type);
            if (!Contains(type))
            {
                CreateMock(type, nonPublic, args);
            }

            return GetMockModel(type);
        }

        internal bool Contains(Type type) => mockCollection.Any(m => m.Type == type);
        internal bool Contains<T>() => Contains(typeof(T));
        internal MockModel GetMockModel(Type type, Mock? mock = null, bool autoCreate = true) => mockCollection.First(m => m.Type == type);
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
            return GetArgData<T>(new InstanceCreationOptions
            {
                OptionalParameterResolution = OptionalParameterResolution,
            });
        }

        public object?[] GetArgData<T>(InstanceCreationOptions? options) where T : class
        {
            options ??= new InstanceCreationOptions
            {
                OptionalParameterResolution = OptionalParameterResolution,
            };

            var type = typeof(T).IsInterface ? GetTypeFromInterface<T>() : new InstanceModel<T>();
            var constructor = FindConstructor(false, type.InstanceType, true, options.OptionalParameterResolution);
            return constructor.ConstructorInfo == null
                ? Array.Empty<object?>()
                : constructor.ConstructorInfo.GetParameters().Select(p => ResolveParameter(p, options.OptionalParameterResolution)).ToArray();
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
            };

            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | (nonPublic ? BindingFlags.NonPublic : 0);
            var method = instance.GetType().GetMethod(methodName, flags);
            var failOnUnconfigured = Behavior.Has(MockFeatures.FailOnUnconfigured);
            if (method == null)
            {
                if (!nonPublic && !failOnUnconfigured)
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
            };

            var type = typeof(T).IsInterface ? GetTypeFromInterface(typeof(T)) : typeof(T);
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | (nonPublic ? BindingFlags.NonPublic : 0);
            var method = type.GetMethod(methodName, flags);
            var failOnUnconfigured = Behavior.Has(MockFeatures.FailOnUnconfigured);
            if (method == null)
            {
                if (!nonPublic && !failOnUnconfigured)
                {
                    return InvokeMethod<T>(options, instance, methodName, true, args);
                }

                throw new ArgumentOutOfRangeException(nameof(methodName));
            }

            return method.Invoke(instance, flags, null, BuildInvocationArgs(method.GetParameters(), options, args), null);
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

        public DbContextMock<TContext> GetMockDbContext<TContext>() where TContext : DbContext
        {
            if (Contains<TContext>())
            {
                return (DbContextMock<TContext>)GetMock<TContext>();
            }

            if (DbConnection.State != System.Data.ConnectionState.Open)
            {
                DbConnection.Open();
            }

            var options = new DbContextOptionsBuilder<TContext>()
                .UseSqlite(DbConnection)
                .Options;

            AddType<DbContextOptions<TContext>>(_ => options, replace: true);

            var mock = new DbContextMock<TContext>(Behavior.Has(MockFeatures.FailOnUnconfigured) ? MockBehavior.Strict : MockBehavior.Default, options);
            AddMock(mock, overwrite: true, nonPublic: true);
            SetupMock(typeof(TContext), mock);
            return mock.SetupDbSets(this);
        }
        public Mock GetMockDbContext(Type dbContextType) =>
            dbContextType.CallGenericMethod(this) as Mock ??
            throw new InvalidOperationException("Unable to get MockDb. Try GetDbContext to use internal database.");

        public TContext GetDbContext<TContext>(Func<DbContextOptions<TContext>, TContext>? factory = null) where TContext : DbContext
        {
            DbConnection = new SqliteConnection("DataSource=:memory:");
            DbConnection.Open();

            var options = new DbContextOptionsBuilder<TContext>()
                .UseSqlite(DbConnection)
                .Options;

            var context = factory != null
                ? factory(options)
                : (TContext)Activator.CreateInstance(typeof(TContext), options)!;

            context.Database.EnsureCreated();
            context.SaveChanges();

            return context;
        }
        public T GetRequiredObject<T>() where T : class => GetObject<T>() ?? throw new InvalidOperationException($"Unable to resolve object of type {typeof(T).Name}.");
        #endregion
    }
}

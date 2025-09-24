using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
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
using FastMoq.Core.Providers.MoqProvider; // ensure unified adapter
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
        internal Dictionary<Type, IInstanceModel> typeMap = new();
        private readonly ObservableExceptionLog _exceptionLog = new();
        public ConstructorHistory ConstructorHistory { get; } = new();

        public Action<LogLevel, EventId, string> LoggingCallback { get; }
        public bool MockOptional { get; set; }
        public DbConnection DbConnection { get; internal set; } = new SqliteConnection("DataSource=:memory:");
        public ObservableExceptionLog ExceptionLog => _exceptionLog;
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
            set => Behavior = value ? MockBehaviorOptions.StrictPreset.Clone() : MockBehaviorOptions.LenientPreset.Clone();
        }

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
        public Mocker AddType(Type tInterface, Type tClass, Func<Mocker, object>? createFunc = null, bool replace = false, params object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(tInterface);
            ArgumentNullException.ThrowIfNull(tClass);
            ValidateAndReplaceType(tInterface, tClass, replace);
            typeMap[tInterface] = new InstanceModel(tInterface, tClass, createFunc, args?.ToList() ?? new List<object?>());
            return this;
        }
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
            if (replace && typeMap.ContainsKey(typeof(T))) typeMap.Remove(typeof(T));
            typeMap[typeof(T)] = new InstanceModel<T>(_ => value);
            return this;
        }
        public Mocker AddType<TInterface, TClass>(bool replace = false, params object?[]? args)
            where TInterface : class where TClass : class => AddType(typeof(TInterface), typeof(TClass), (Func<Mocker, object>?)null, replace, args);
        public Mocker AddType<TInterface, TClass>(Func<Mocker, TClass>? createFunc, bool replace = false, params object?[]? args)
            where TInterface : class where TClass : class => AddType(typeof(TInterface), typeof(TClass), createFunc is null ? null : new Func<Mocker, object>(m => createFunc(m)!), replace, args);
        public Mocker AddType<T>(Func<Mocker, T>? createFunc = null, bool replace = false, params object?[]? args) where T : class => AddType<T, T>(createFunc, replace, args);
        public Mocker AddType(Func<Mocker, object?, object?> createFunc, bool replace = false, params object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(createFunc);
            if (replace && typeMap.ContainsKey(typeof(string))) typeMap.Remove(typeof(string));
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
            if (!tInterface.IsAssignableFrom(tClass)) throw new ArgumentException($"{tClass.Name} is not assignable to {tInterface.Name}.");
            if (replace) typeMap.Remove(tInterface);
        }

        public void AddFileSystemAbstractionMapping()
        {
            return;
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
        #endregion

        #region Type Resolution Helpers
        internal IInstanceModel GetTypeFromInterface<T>() where T : class => new InstanceModel(typeof(T), GetTypeFromInterface(typeof(T)));
        internal Type GetTypeFromInterface(Type type)
        {
            if (!type.IsInterface) return type;
            var possibles = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic) // exclude dynamic (reflection emit) assemblies
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                })
                .Where(t => type.IsAssignableFrom(t)
                            && !t.IsInterface
                            && !t.IsAbstract
                            && !t.Assembly.IsDynamic
                            && !IsProxyLike(t))
                .ToList();
            switch (possibles.Count)
            {
                case 0:
                    return type; // fallback to interface for provider mock
                case 1:
                    return possibles[0];
                default:
                    // Heuristic 1: prefer class whose name matches interface name without leading 'I'
                    var trimmed = type.Name.StartsWith("I") ? type.Name[1..] : type.Name;
                    var nameMatch = possibles.Where(p => string.Equals(p.Name, trimmed, StringComparison.Ordinal)).ToList();
                    if (nameMatch.Count == 1) return nameMatch[0];

                    // Heuristic 2: if all candidates are from System.IO.Abstractions (file system) keep interface (avoid ambiguity for wrappers like IDirectory)
                    if (possibles.All(p => (p.Namespace ?? string.Empty).StartsWith("System.IO.Abstractions", StringComparison.Ordinal)))
                    {
                        return type; // let provider generate a mock. Avoid throwing.
                    }

                    // Still ambiguous – throw
                    throw new AmbiguousImplementationException($"Multiple implementations found for {type.Name}.");
            }
        }

        private static bool IsProxyLike(Type t)
        {
            var name = t.FullName ?? string.Empty;
            // Heuristics: skip common proxy / dynamic patterns (Moq, DispatchProxy, our reflection proxies, Castle, etc.)
            return name.Contains("Proxy", StringComparison.OrdinalIgnoreCase)
                   || name.Contains("DispatchProxy", StringComparison.OrdinalIgnoreCase)
                   || name.Contains("Castle.Proxies", StringComparison.OrdinalIgnoreCase)
                   || name.StartsWith("FastMoq.ReflectionProxy.");
        }
        internal IInstanceModel GetTypeModel(Type type) => typeMap.TryGetValue(type, out var model) && model is not null ? model : new InstanceModel(type, GetTypeFromInterface(type));
        internal static Type CleanType(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Mock<>) ? type.GetGenericArguments()[0] : type;
        #endregion

        #region Injection / Object Creation
        internal object? GetParameter(ParameterInfo parameter)
        {
            var pt = parameter.ParameterType;
            if (!typeMap.ContainsKey(pt) && !pt.IsClass && !pt.IsInterface) return pt.GetDefaultValue();
            var m = GetTypeModel(pt);
            return m.CreateFunc != null ? m.CreateFunc.Invoke(this, pt) : (!pt.IsSealed ? GetObject(pt) : pt.GetDefaultValue());
        }

        public T AddInjections<T>(T obj, Type? referenceType = null) where T : class?
        {
            if (obj == null) return obj;
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
            type = CleanType(type);
            var m = GetTypeModel(type);
            if (m.CreateFunc != null)
            {
                return AddInjections(m.CreateFunc.Invoke(this, m.InstanceType), m.InstanceType);
            }
            var strict = Behavior.Has(MockFeatures.FailOnUnconfigured);
            if (!strict)
            {
                if (!Contains<IFileSystem>() && type.IsEquivalentTo(typeof(IFileSystem))) return fileSystem;
                if (!Contains<HttpClient>() && type.IsEquivalentTo(typeof(HttpClient))) return HttpClient;
            }
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
                initAction?.Invoke(obj);
                return obj;
            }
            var def = type.GetDefaultValue();
            initAction?.Invoke(def);
            return def;
        }
        public object? GetObject(ParameterInfo info)
        {
            try
            {
                return (!MockOptional && info.IsOptional) switch
                {
                    true when info.HasDefaultValue => info.DefaultValue,
                    true => null,
                    _ => GetParameter(info)
                };
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
            // Legacy helper: create instance using provided args (constructor style) similar to CreateInstance but via GetObject semantics in tests.
            if (args == null) return GetObject<T>();
            var type = typeof(T);
            var ctor = FindConstructor(type, false, args);
            var instance = CreateInstanceInternal(type, ctor.ConstructorInfo, ctor.ParameterList) as T;
            return instance;
        }

        public object? AddProperties(Type type, object? obj, params KeyValuePair<string, object>[] data)
        {
            if (obj == null) return null;
            if (creatingTypeList.Contains(type)) return obj;
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
                        if (!shouldAssign) continue;
                        val = data.Any(x => x.Key.Contains(prop.Name, StringComparison.OrdinalIgnoreCase))
                            ? data.First(x => x.Key.Contains(prop.Name, StringComparison.OrdinalIgnoreCase)).Value
                            : GetObject(prop.PropertyType);
                        if (prop.CanWrite) prop.SetValue(obj, val);
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
        public T? CreateInstance<T>(params object?[] args) where T : class => CreateInstance<T>(true, args);
        public T? CreateInstance<T>(bool usePredefinedFileSystem, params object?[] args) where T : class
        {
            if (usePredefinedFileSystem && fileSystem is T fs) return fs;
            var tType = typeof(T);
            var model = GetTypeModel(tType);
            if (model.CreateFunc != null) return (T?)model.CreateFunc.Invoke(this, model.InstanceType);
            var ctorModel = GetTypeConstructor(tType, false, args);
            return CreateInstanceInternal<T>(ctorModel.ConstructorInfo, ctorModel.ParameterList);
        }
        public T? CreateInstanceNonPublic<T>(params object?[] args) where T : class
        {
            var tType = typeof(T);
            var ctorModel = GetTypeConstructor(tType, true, args);
            return CreateInstanceInternal<T>(ctorModel.ConstructorInfo, ctorModel.ParameterList);
        }
        public T? CreateInstanceByType<T>(params Type?[] args) where T : class
        {
            var tType = typeof(T);
            var ctor = FindConstructorByType(tType, true, args);
            return (T?)ctor.Invoke(new object?[args.Length]);
        }
        internal ConstructorModel GetTypeConstructor(Type type, bool nonPublic, object?[] args)
        {
            var constructor = new ConstructorModel(null, args);
            try
            {
                if (!type.IsInterface)
                {
                    constructor = args.Length > 0 || nonPublic ? FindConstructor(type, nonPublic, args) : FindConstructor(true, type, nonPublic);
                }
            }
            catch (Exception ex) { ExceptionLog.Add(ex.Message); }
            if (constructor.ConstructorInfo == null && !HasParameterlessConstructor(type))
            {
                try { constructor = GetConstructors(type, nonPublic).MinBy(x => x.ParameterList.Length) ?? constructor; }
                catch (Exception ex) { ExceptionLog.Add(ex.Message); }
            }
            return constructor;
        }
        internal ConstructorInfo FindConstructorByType(Type type, bool nonPublic, params Type?[] args)
        {
            var strict = Behavior.Has(MockFeatures.FailOnUnconfigured);
            var ctors = GetConstructorsByType(nonPublic, type, args);
            if (ctors.Count == 0 && !nonPublic && !strict) return FindConstructorByType(type, true, args);
            if (ctors.Count == 0) throw new NotImplementedException("Unable to find the constructor.");
            return ctors[0];
        }
        internal ConstructorModel FindConstructor(Type type, bool nonPublic, params object?[] args)
        {
            var strict = Behavior.Has(MockFeatures.FailOnUnconfigured);
            var all = GetConstructors(type, nonPublic, args);
            var filtered = all.Where(x => x.ParameterList.Select(z => z?.GetType()).SequenceEqual(args.Select(a => a?.GetType()))).ToList();
            if (!filtered.Any())
            {
                if (!nonPublic && !strict) return FindConstructor(type, true, args);
                throw new NotImplementedException("Unable to find the constructor.");
            }
            return filtered.First();
        }
        internal ConstructorModel FindConstructor(bool bestGuess, Type type, bool nonPublic, List<ConstructorInfo>? excludeList = null)
        {
            var strict = Behavior.Has(MockFeatures.FailOnUnconfigured);
            excludeList ??= new();
            var ctors = GetConstructors(type, nonPublic).Where(c => excludeList.TrueForAll(e => e != c.ConstructorInfo)).ToList();
            if (!bestGuess && ctors.Count(x => x.ParameterList.Length > 0) > 1) throw new AmbiguousImplementationException("Multiple parameterized constructors exist.");
            if (!(ctors.Count > 0) && !nonPublic && !strict) return FindConstructor(bestGuess, type, true, excludeList);
            return ctors.LastOrDefault() ?? throw new NotImplementedException("Unable to find the constructor.");
        }
        public bool HasParameterlessConstructor(Type type, bool nonPublic = false) => GetConstructors(type, nonPublic).Any(x => x.ParameterList.Length == 0);
        internal List<ConstructorModel> GetConstructors(Type type, bool nonPublic, params object?[] values)
        {
            var flags = nonPublic ? BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public : BindingFlags.Instance | BindingFlags.Public;
            return type.GetConstructors(flags)
                .Where(c => c.GetParameters().All(p => p.ParameterType != type))
                .Select(ci => new ConstructorModel(ci, values.Length > 0 ? values : ci.GetParameters().Select(p => GetObject(p)).ToArray()))
                .OrderBy(c => c.ParameterList.Length)
                .ToList();
        }
        internal static List<ConstructorInfo> GetConstructorsByType(bool nonPublic, Type type, params Type?[] parameterTypes) =>
            type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                .Where(x => (nonPublic || x.IsPublic) && x.IsValidConstructorByType(parameterTypes))
                .OrderBy(x => x.GetParameters().Length)
                .ToList();
        internal T? CreateInstanceInternal<T>(ConstructorInfo? info, params object?[] args) where T : class => CreateInstanceInternal(typeof(T), info, args) as T;
        internal object? CreateInstanceInternal(Type type, ConstructorInfo? info, params object?[] args)
        {
            ConstructorHistory.AddOrUpdate(type, new ConstructorModel(info, args));
            var paramList = info?.GetParameters().ToList() ?? new List<ParameterInfo>();
            var newArgs = args.ToList();
            if (args.Length < paramList.Count)
            {
                for (var i = args.Length; i < paramList.Count; i++)
                {
                    var p = paramList[i];
                    newArgs.Add(p.IsOptional ? null : GetParameter(p));
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
            if (Contains(type)) return GetMockModel(type).FastMock; // reuse existing

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
            AddFastMock(fast, type, nonPublic: nonPublic);
            return fast;
        }

        public List<MockModel> CreateMock(Type type, bool nonPublic = false, params object?[] args)
        {
            // Legacy Moq-oriented method – now a thin shim over provider-first fast mock creation.
            type = CleanType(type);
            if (Contains(type)) type.ThrowAlreadyExists();
            var fast = CreateFastMock(type, nonPublic, args);

            // Attempt legacy surface hydration for backward compatibility (if provider exposes a Moq mock).
            var legacy = TryGetLegacyMock(fast);
            if (legacy != null)
            {
                // Assign without overwriting existing FastMock implementation.
                AddMock(legacy, type, overwrite: false, nonPublic: nonPublic);
                SetupMock(type, legacy); // retain legacy setup behavior
            }
            return mockCollection;
        }
        public List<MockModel> CreateMock<T>(bool nonPublic = false, params object?[] args) where T : class => CreateMock(typeof(T), nonPublic, args);

        public Mock<T> CreateMockInstance<T>(bool nonPublic = false, params object?[] args) where T : class
        {
            // Legacy convenience returning Moq.Mock<T>. Uses provider-first path internally.
            CreateMock(typeof(T), nonPublic, args);
            var model = GetMockModel(typeof(T));
            var legacy = TryGetLegacyMock(model.FastMock);
            if (legacy is Mock<T> typed) return typed;
            throw new NotSupportedException($"Active provider '{MockingProviderRegistry.Default.GetType().Name}' does not expose Moq legacy surface for {typeof(T).Name}.");
        }

        public Mock CreateMockInstance(Type type, bool nonPublic = false, params object?[] args)
        {
            // Legacy non-generic convenience.
            CreateMock(type, nonPublic, args);
            var model = GetMockModel(type);
            var legacy = TryGetLegacyMock(model.FastMock);
            if (legacy != null) return legacy;
            throw new NotSupportedException($"Active provider '{MockingProviderRegistry.Default.GetType().Name}' does not expose Moq legacy surface for {type.Name}.");
        }

        internal MockModel AddFastMock(IFastMock fastMock, Type type, bool overwrite = false, bool nonPublic = false)
        {
            if (Contains(type))
            {
                if (!overwrite) type.ThrowAlreadyExists();
                var existing = GetMockModel(type);
                existing.FastMock = fastMock;
                var legacyExisting = TryGetLegacyMock(fastMock);
                if (legacyExisting != null && existing.Mock == null)
                {
                    existing.Mock = legacyExisting;
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
                // Determine if an existing distinct legacy mock is already associated.
                var existingLegacy = TryGetLegacyMock(mm.FastMock); // avoids triggering Mock property hydration/exception
                if (!overwrite && existingLegacy != null && !ReferenceEquals(existingLegacy, mock) && mm.Type == type)
                {
                    type.ThrowAlreadyExists();
                }
                // Assign (setter refreshes adapter)
                mm.Mock = mock;
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
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var t = fastMock.GetType();
            foreach (var name in new[] { "Inner", "InnerMock" })
            {
                var p = t.GetProperty(name, flags);
                if (p?.GetValue(fastMock) is Mock m) return m;
            }
            return null;
        }
        #endregion

        #region Mock Retrieval & Setup
        public Mock GetMock(Type type, params object?[]? args)
        {
            type = CleanType(type);
            if (!Contains(type)) CreateMock(type, args?.Length > 0, args ?? Array.Empty<object?>());
            return GetRequiredMock(type);
        }
        public Mock<T> GetMock<T>(params object?[] args) where T : class => (Mock<T>)GetMock(typeof(T), args);
        public Mock<T> GetMock<T>(Action<Mock<T>> action, params object?[] args) where T : class
        {
            var m = GetMock<T>(args);
            action?.Invoke(m);
            return m;
        }
        public Mock GetRequiredMock(Type type) => mockCollection.First(x => x.Type == type).Mock;
        public Mock<T> GetRequiredMock<T>() where T : class => (Mock<T>)GetRequiredMock(typeof(T));
        public MockModel<T> GetMockModel<T>() where T : class => new(GetMockModel(typeof(T)));
        public int GetMockModelIndexOf(Type type, bool throwIfMissing = true)
        {
            var idx = mockCollection.FindIndex(m => m.Type == type);
            if (idx < 0 && throwIfMissing) throw new ArgumentException($"Mock of type {type} not found.");
            return idx;
        }
        public bool RemoveMock(Mock mock)
        {
            var model = mockCollection.FirstOrDefault(m => m.Mock == mock);
            if (model == null) return false;
            mockCollection.Remove(model);
            return true;
        }

        internal void SetupMock(Type type, Mock oMock)
        {
            var strict = Behavior.Has(MockFeatures.FailOnUnconfigured);
            if (!strict)
            {
                if (Behavior.Has(MockFeatures.AutoSetupProperties) && oMock.Setups.Count == 0)
                {
                    TrySetupAllProperties(oMock);
                }
                if (InnerMockResolution && Behavior.Has(MockFeatures.AutoInjectDependencies)) AddProperties(type, oMock.Object);
                if (Behavior.Has(MockFeatures.LoggerCallback) && type.IsAssignableTo(typeof(ILogger)))
                {
                    try { if (oMock is Mock<ILogger> logger) SetupLoggerCallback(logger, LoggingCallback); } catch { }
                }
            }
            if (Behavior.Has(MockFeatures.AutoInjectDependencies))
            {
                AddInjections(oMock.Object, GetTypeModel(type).InstanceType);
            }
        }

        private static void TrySetupAllProperties(Mock mock)
        {
            var t = mock.GetType();
            if (!t.IsGenericType) return;
            try
            {
                var ext = typeof(MockExtensions).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "SetupAllProperties" && m.GetParameters().Length == 1);
                if (ext == null) return;
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
            if (!Contains(type)) CreateMock(type, nonPublic, args);
            return GetMockModel(type).FastMock;
        }
        internal IFastMock<T> GetFastMock<T>(bool nonPublic = false, params object?[] args) where T : class => (IFastMock<T>)GetOrCreateFastMock(typeof(T), nonPublic, args);
        internal MockModel GetMockModelFast(Type type, bool nonPublic = false, params object?[] args)
        {
            type = CleanType(type);
            if (!Contains(type)) CreateMock(type, nonPublic, args);
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
            for (var i = 0; i < count; i++) list.Add(factory());
            return list;
        }
        public static List<T> GetList<T>(int count, Func<int, T> factory)
        {
            var list = new List<T>(count);
            for (var i = 0; i < count; i++) list.Add(factory(i));
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

        public object?[] GetMethodArgData(MethodBase? method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            return method.GetParameters().Select(p => GetObject(p)).ToArray();
        }
        public object?[] GetMethodDefaultData(MethodBase? method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            return method.GetParameters().Select(p => p.ParameterType.GetDefaultValue()).ToArray();
        }
        public object?[] GetArgData<T>() where T : class
        {
            var ctor = typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
            return ctor == null ? Array.Empty<object?>() : ctor.GetParameters().Select(p => GetObject(p)).ToArray();
        }

        public object? InvokeMethod(object? instance, string methodName, bool nonPublic = false, params object?[] args)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | (nonPublic ? BindingFlags.NonPublic : 0);
            var method = instance.GetType().GetMethod(methodName, flags) ?? throw new MissingMethodException(instance.GetType().FullName, methodName);
            return method.Invoke(instance, args);
        }
        public object? InvokeMethod<T>(object? instance, string methodName, bool nonPublic = false, params object?[] args)
        {
            var type = typeof(T);
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | (nonPublic ? BindingFlags.NonPublic : 0);
            var method = type.GetMethod(methodName, flags) ?? throw new MissingMethodException(type.FullName, methodName);
            return method.Invoke(instance, args);
        }
        public object? InvokeMethod<T>(string methodName, bool nonPublic = false, params object?[] args) => InvokeMethod<T>(null, methodName, nonPublic, args);

        private object?[] BuildInvocationArgs(Delegate del, object?[] provided)
        {
            var method = del.Method;
            var parameters = method.GetParameters();
            var list = new List<object?>();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < provided.Length && provided[i] != null)
                {
                    list.Add(provided[i]);
                    continue;
                }
                var p = parameters[i];
                if (i < provided.Length && provided[i] == null && (p.ParameterType.IsClass || p.ParameterType.IsInterface || p.ParameterType.IsNullableType()))
                {
                    list.Add(null);
                    continue;
                }
                list.Add(GetObject(p));
            }
            return list.ToArray();
        }
        public TReturn CallMethod<TReturn>(Delegate del, params object?[] args)
        {
            if (del == null) throw new ArgumentNullException(nameof(del));
            var invocationArgs = BuildInvocationArgs(del, args);
            var result = del.DynamicInvoke(invocationArgs);
            return (TReturn)result!;
        }
        public void CallMethod(Delegate del, params object?[] args) => CallMethod<object?>(del, args);
        public TReturn CallMethod<TReturn>(Func<TReturn> func, params object?[] args) => CallMethod<TReturn>((Delegate)func, args);
        public void CallMethod(Action action, params object?[] args) => CallMethod<object?>((Delegate)action, args);
        public async Task<TReturn> CallMethodAsync<TReturn>(Delegate del, params object?[] args)
        {
            var result = CallMethod<object>(del, args);
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

        public void Initialize<T>(Action<Mock<T>> init) where T : class
        {
            var m = GetMock<T>();
            init?.Invoke(m);
        }

        public async Task<string> GetStringContent(HttpContent? content) => content == null ? string.Empty : await content.ReadAsStringAsync().ConfigureAwait(false);

        public Mock<TContext> GetMockDbContext<TContext>() where TContext : DbContext => GetMock<TContext>();
        public Mock<DbContext> GetMockDbContext(Type dbContextType)
        {
            var m = typeof(Mocker).GetMethod(nameof(GetMockDbContext), BindingFlags.Public | BindingFlags.Instance)!.MakeGenericMethod(dbContextType);
            return (Mock<DbContext>)m.Invoke(this, null)!;
        }
        public TContext GetDbContext<TContext>(Func<DbContextOptions<TContext>, TContext>? factory = null) where TContext : DbContext
        {
            var builder = new DbContextOptionsBuilder<TContext>().UseSqlite(DbConnection);
            var options = builder.Options;
            return factory != null ? factory(options) : GetObject<TContext>() ?? (TContext)Activator.CreateInstance(typeof(TContext), options)!;
        }
        public T GetRequiredObject<T>() where T : class => GetObject<T>() ?? throw new InvalidOperationException($"Unable to resolve object of type {typeof(T).Name}.");
        #endregion
    }
}

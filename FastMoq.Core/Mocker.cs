using FastMoq.Extensions;
using FastMoq.Models;
using Microsoft.Data.Sqlite;
using Moq;
using Moq.Language.Flow;
using Moq.Protected;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime;

namespace FastMoq
{
    /// <summary>
    ///     Initializes the mocking helper object. This class creates and manages the automatic mocking and custom mocking.
    /// </summary>
    [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract", Justification = "Because")]
    [SuppressMessage("ReSharper", "NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract", Justification = "Because")]
    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract", Justification = "Because")]
    public partial class Mocker
    {
        #region Fields

        /// <summary>
        ///     The virtual mock file system that is used by mocker unless overridden with the <see cref="Strict" /> property.
        /// </summary>
        public readonly MockFileSystem fileSystem;

        /// <summary>
        ///     The list of types in the process of being created. This is used to prevent circular creations.
        /// </summary>
        protected internal readonly List<Type> creatingTypeList = new();

        /// <summary>
        ///     List of <see cref="MockModel" />.
        /// </summary>
        protected internal readonly List<MockModel> mockCollection;

        /// <summary>
        ///     <see cref="Dictionary{TKey,TValue}" /> of <see cref="Type" /> mapped to <see cref="InstanceModel" />.
        ///     This map assists in resolution of interfaces to instances.
        /// </summary>
        /// <value>The type map.</value>
        internal readonly Dictionary<Type, IInstanceModel> typeMap;

        /// <summary>
        ///     The constructor history
        /// </summary>
        private readonly Dictionary<Type, List<IHistoryModel>> constructorHistory = new();

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the constructor history.
        /// </summary>
        /// <value>The constructor history.</value>
        public ILookup<Type, ReadOnlyCollection<IHistoryModel>> ConstructorHistory => constructorHistory.ToLookup(pair => pair.Key, pair => pair.Value.AsReadOnly());

        /// <summary>
        ///     Gets the database connection.
        /// </summary>
        /// <value>The database connection.</value>
        public DbConnection DbConnection { get; private set; } = new SqliteConnection("DataSource=:memory:");

        /// <summary>
        ///     When creating a mocks of a class, this indicates to recursively inject the mocks inside of that class.
        /// </summary>
        /// <value>The inner mock resolution.</value>
        public bool InnerMockResolution { get; set; } = true;

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="Mocker" /> is strict.
        /// </summary>
        /// <value><c>true</c> if strict <see cref="IFileSystem" /> resolution; otherwise, <c>false</c> uses the built-in virtual
        /// <see cref="MockFileSystem" />.</value>
        /// <remarks>If strict, the mock
        /// <see cref="IFileSystem" /> does
        /// not use <see cref="MockFileSystem" /> and uses <see cref="Mock" /> of <see cref="IFileSystem" />.
        /// Gets or sets a value indicating whether this <see cref="Mocker" /> is strict. If strict, the mock
        /// <see cref="HttpClient" /> does
        /// not use the pre-built HttpClient and uses <see cref="Mock" /> of <see cref="HttpClient" />.</remarks>
        public bool Strict { get; set; }

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="Mocker" /> class.
        /// </summary>
        public Mocker()
        {
            fileSystem = new();
            mockCollection = new();
            typeMap = new();
            HttpClient = CreateHttpClient();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="Mocker" /> class using the specific typeMap.
        ///     The typeMap assists in resolution of interfaces to instances.
        /// </summary>
        /// <param name="typeMap">The type map.</param>
        public Mocker(Dictionary<Type, IInstanceModel> typeMap) : this() => this.typeMap = typeMap;

        /// <summary>
        ///     Adds the injections to the specified object properties and fields.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="referenceType">Override object type used for injection attribute references.</param>
        /// <returns>T.</returns>
        public T AddInjections<T>(T obj, Type? referenceType = null) where T : class?
        {
            if (obj == null)
            {
                return obj;
            }

            referenceType ??= obj.GetType();
            var properties = referenceType.GetInjectionProperties();
            var fields = referenceType.GetInjectionFields();

            properties.ForEach(y => obj.SetPropertyValue(y.Name, GetObject(y.PropertyType)));
            fields.ForEach(y => obj.SetFieldValue(y.Name, GetObject(y.FieldType)));

            return obj;
        }

        /// <summary>
        ///     Creates a <see cref="MockModel" /> with the given <see cref="Mock" /> with the option of overwriting an existing
        /// <see cref="MockModel" />
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="mock">Mock to Add.</param>
        /// <param name="overwrite">Overwrite if the mock exists or throw <see cref="ArgumentException" /> if this parameter is
        /// false.</param>
        /// <param name="nonPublic">if set to <c>true</c> uses public and non public constructors.</param>
        /// <returns><see cref="MockModel{T}" />.</returns>
        public MockModel<T> AddMock<T>(Mock<T> mock, bool overwrite, bool nonPublic = false) where T : class =>
            new(AddMock(mock, typeof(T), overwrite, nonPublic));

        /// <summary>
        ///     Adds the property data to the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object.</param>
        /// <returns>T.</returns>
        public T? AddProperties<T>(T obj)
        {
            var o = AddProperties(typeof(T), obj);
            return o is not null ? (T) o : default;
        }

        /// <summary>
        ///     Adds the property data to the object.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="obj">The object.</param>
        /// <returns>object.</returns>
        public object? AddProperties(Type type, object? obj)
        {
            if (creatingTypeList.Contains(type))
            {
                return obj;
            }

            try
            {
                creatingTypeList.Add(type);
                var writableProperties = type.GetProperties().Where(x => x.CanWrite && x.CanRead).ToList();

                foreach (var writableProperty in writableProperties)
                {
                    try
                    {
                        if (writableProperty.GetValue(obj) is null && !creatingTypeList.Contains(writableProperty.PropertyType))
                        {
                            writableProperty.SetValue(obj, GetObject(writableProperty.PropertyType));
                        }
                    }
                    catch
                    {
                        // Continue
                    }
                }
            }
            finally
            {
                creatingTypeList.Remove(type);
            }

            return obj;
        }

        /// <summary>
        ///     Adds an interface to Class mapping to the <see cref="typeMap" /> for easier resolution.
        /// </summary>
        /// <param name="tInterface">The interface or class Type which can be mapped to a specific Class.</param>
        /// <param name="tClass">The Class Type (cannot be an interface) that can be created and assigned to tInterface.</param>
        /// <param name="createFunc">An optional create function used to create the class.</param>
        /// <param name="replace">Replace type if already exists. Default: false.</param>
        /// <param name="args">arguments needed in model.</param>
        /// <exception cref="ArgumentException"></exception>
        public void AddType(Type tInterface, Type tClass, Func<Mocker, object>? createFunc = null, bool replace = false, params object?[]? args)
        {
            if (tClass.IsInterface)
            {
                throw new ArgumentException($"{tClass.Name} cannot be an interface.");
            }

            if (!tInterface.IsAssignableFrom(tClass))
            {
                throw new ArgumentException($"{tClass.Name} is not assignable to {tInterface.Name}.");
            }

            if (typeMap.ContainsKey(tInterface) && replace)
            {
                typeMap.Remove(tInterface);
            }

            typeMap.Add(tInterface, new InstanceModel(tInterface, tClass, createFunc, args?.ToList() ?? new()));
        }

        /// <summary>
        ///     Adds the type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="createFunc">The create function.</param>
        /// <param name="replace">if set to <c>true</c> [replace].</param>
        /// <param name="args">arguments needed in model.</param>
        public void AddType<T>(Func<Mocker, T>? createFunc = null, bool replace = false, params object?[]? args) where T : class =>
            AddType<T, T>(createFunc, replace, args);

        /// <summary>
        ///     Adds an interface to Class mapping to the <see cref="typeMap" /> for easier resolution.
        /// </summary>
        /// <typeparam name="TInterface">The interface or class Type which can be mapped to a specific Class.</typeparam>
        /// <typeparam name="TClass">The Class Type (cannot be an interface) that can be created and assigned to TInterface /&gt;.</typeparam>
        /// <param name="createFunc">An optional create function used to create the class.</param>
        /// <param name="replace">Replace type if already exists. Default: false.</param>
        /// <param name="args">arguments needed in model.</param>
        /// <exception cref="ArgumentException">$"{typeof(TClass).Name} cannot be an interface."</exception>
        /// <exception cref="ArgumentException">$"{typeof(TClass).Name} is not assignable to {typeof(TInterface).Name}."</exception>
        public void AddType<TInterface, TClass>(Func<Mocker, TClass>? createFunc = null, bool replace = false, params object?[]? args)
            where TInterface : class where TClass : class => AddType(typeof(TInterface), typeof(TClass), createFunc, replace, args);

        /// <summary>
        ///     Determines whether this instance contains a Mock of <c>T</c>.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <returns><c>true</c> if the <c><![CDATA[Mock<T>]]></c> exists; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">type is null.</exception>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        public bool Contains<T>() where T : class => Contains(typeof(T));

        /// <summary>
        ///     Determines whether this instance contains the Mock of <c>type</c>.
        /// </summary>
        /// <param name="type">The <see cref="T:Type" />, usually an interface.</param>
        /// <returns><c>true</c> if <see cref="Mock{T}" /> exists; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ArgumentNullException">type</exception>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        public bool Contains(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return !type.IsClass && !type.IsInterface
                ? throw new ArgumentException("type must be a class.", nameof(type))
                : mockCollection.Any(x => x.Type == type);
        }

        /// <summary>
        ///     Gets the argument data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data.</param>
        /// <returns>System.Nullable&lt;System.Object&gt;[].</returns>
        public object?[] GetArgData<T>(Dictionary<Type, object?>? data = null) where T : class
        {
            var type = typeof(T).IsInterface ? GetTypeFromInterface<T>() : new InstanceModel<T>();

            data ??= new Dictionary<Type, object?>();
            var constructor = FindConstructor(false, type.InstanceType, true);
            return GetArgData(constructor.ConstructorInfo, data);
        }

        /// <summary>
        ///     Gets the content bytes.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>byte[].</returns>
        public async Task<byte[]> GetContentBytes(HttpContent content) =>
            content is ByteArrayContent data ? await data.ReadAsByteArrayAsync() : Array.Empty<byte>();

        /// <summary>
        ///     Gets the content stream.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>System.IO.Stream.</returns>
        public async Task<Stream> GetContentStream(HttpContent content) =>
            content is ByteArrayContent data ? await data.ReadAsStreamAsync() : Stream.Null;

        /// <summary>
        ///     Gets the HTTP handler setup.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>ISetup&lt;HttpMessageHandler, Task&lt;HttpResponseMessage&gt;&gt;.</returns>
        public ISetup<HttpMessageHandler, Task<HttpResponseMessage>>? GetHttpHandlerSetup(Expression? request = null,
            Expression? cancellationToken = null) =>
            GetMessageProtectedAsync<HttpMessageHandler, HttpResponseMessage>("SendAsync",
                request ?? ItExpr.IsAny<HttpRequestMessage>(),
                cancellationToken ?? ItExpr.IsAny<CancellationToken>()
            );

        /// <summary>
        ///     Gets a list with the specified number of list items, using a custom function.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="count">The number of list items.</param>
        /// <param name="func">The function for creating the list items.</param>
        /// <param name="initAction">The initialize action.</param>
        /// <returns><see cref="List{T}" />.</returns>
        /// <example>
        /// Example of how to create a list.
        /// <code><![CDATA[
        /// GetList<Model>(3, (i) => new Model(name: i.ToString()));
        /// ]]></code>
        /// or
        /// <code><![CDATA[
        /// GetList<IModel>(3, (i) => Mocks.CreateInstance<IModel>(i));
        /// ]]></code></example>
        public static List<T> GetList<T>(int count, Func<int, T>? func, Action<int, T>? initAction)
        {
            var results = new List<T>();

            if (func != null)
            {
                for (var i = 0; i < count; i++)
                {
                    var f = func.Invoke(i);
                    initAction?.Invoke(i, f);
                    results.Add(f);
                }
            }

            return results;
        }

        /// <summary>
        ///     Gets a list with the specified number of list items, using a custom function.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="count">The number of list items.</param>
        /// <param name="func">The function for creating the list items.</param>
        /// <returns><see cref="List{T}" />.</returns>
        /// <example>
        /// Example of how to create a list.
        /// <code><![CDATA[
        /// GetList<Model>(3, (i) => new Model(name: i.ToString()));
        /// ]]></code>
        /// or
        /// <code><![CDATA[
        /// GetList<IModel>(3, (i) => Mocks.CreateInstance<IModel>(i));
        /// ]]></code></example>
        public static List<T> GetList<T>(int count, Func<int, T>? func) => GetList(count, func, null);

        /// <summary>
        ///     Gets a list with the specified number of list items, using a custom function.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="count">The number of list items.</param>
        /// <param name="func">The function for creating the list items.</param>
        /// <returns><see cref="List{T}" />.</returns>
        /// <example>
        /// Example of how to create a list.
        /// <code><![CDATA[
        /// GetList<Model>(3, () => new Model(name: Guid.NewGuid().ToString()));
        /// ]]></code>
        /// or
        /// <code><![CDATA[
        /// GetList<IModel>(3, () => Mocks.CreateInstance<IModel>());
        /// ]]></code></example>
        public static List<T> GetList<T>(int count, Func<T>? func) =>
            func == null ? new List<T>() : GetList(count, _ => func.Invoke());

        /// <summary>
        ///     Gets the message protected asynchronous.
        /// </summary>
        /// <typeparam name="TMock">The type of the t mock.</typeparam>
        /// <typeparam name="TReturn">The type of the t return.</typeparam>
        /// <param name="methodOrPropertyName">Name of the method or property.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>ISetup&lt;TMock, Task&lt;TReturn&gt;&gt;.</returns>
        public ISetup<TMock, Task<TReturn>>? GetMessageProtectedAsync<TMock, TReturn>(string methodOrPropertyName, params object?[]? args)
            where TMock : class =>
            GetMock<TMock>().Protected()
                ?.Setup<Task<TReturn>>(methodOrPropertyName, args ?? Array.Empty<object>());

        /// <summary>
        ///     Gets the method argument data.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="data">The data.</param>
        /// <returns>System.Nullable&lt;System.Object&gt;[].</returns>
        /// <exception cref="ArgumentNullException">method</exception>
        public object?[] GetMethodArgData(MethodInfo method, Dictionary<Type, object?>? data = null)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            var args = new List<object?>();

            method.GetParameters().ToList().ForEach(p =>
                {
                    args.Add(data?.Any(x => x.Key == p.ParameterType) ?? false
                        ? data.First(x => x.Key == p.ParameterType).Value
                        : GetParameter(p.ParameterType)
                    );
                }
            );

            return args.ToArray();
        }

        /// <summary>
        ///     Gets the method default data.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>object?[].</returns>
        /// <exception cref="ArgumentNullException">method</exception>
        public object?[] GetMethodDefaultData(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            var args = new List<object?>();
            method.GetParameters().ToList().ForEach(p => args.Add(p.ParameterType.GetDefaultValue()));

            return args.ToArray();
        }

        /// <summary>
        ///     Gets or creates the mock of type <c>T</c>.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="args">The arguments to get the constructor.</param>
        /// <returns><see cref="Mock{T}" />.</returns>
        public Mock<T> GetMock<T>(params object?[] args) where T : class => (Mock<T>) GetMock(typeof(T), args);

        /// <summary>
        ///     Gets of creates the mock of <c>type</c>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="args">The arguments used to find the correct constructor for a class.</param>
        /// <returns><see cref="Mock" />.</returns>
        public Mock GetMock(Type type, params object?[] args)
        {
            type = CleanType(type);

            if (!Contains(type))
            {
                CreateMock(type, args.Length > 0, args);
            }

            return GetRequiredMock(type);
        }

        /// <summary>
        ///     Gets the instance for the given <see cref="ParameterInfo" />.
        /// </summary>
        /// <param name="info">The <see cref="ParameterInfo" />.</param>
        /// <returns><see cref="Nullable{Object}" /></returns>
        /// <exception cref="ArgumentNullException">info</exception>
        /// <exception cref="System.ArgumentNullException">nameof(info)</exception>
        /// <exception cref="System.InvalidProgramException">nameof(info)</exception>
        public object? GetObject(ParameterInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            try
            {
                return info.IsOptional ? null : GetParameter(info.ParameterType);
            }
            catch
            {
                return info.ParameterType.GetDefaultValue();
            }
        }

        /// <summary>
        ///     Gets the instance for the given <c>type</c>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="initAction">The initialize action.</param>
        /// <returns><see cref="Nullable{Object}" />.</returns>
        /// <exception cref="ArgumentNullException">type</exception>
        /// <exception cref="InvalidProgramException">Unable to get the Mock.</exception>
        /// <exception cref="System.ArgumentNullException">nameof(type)</exception>
        /// <exception cref="System.InvalidProgramException">nameof(type)</exception>
        public object? GetObject(Type type, Action<object?>? initAction = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            type = CleanType(type);

            var typeValueModel = GetTypeModel(type);

            if (typeValueModel.CreateFunc != null)
            {
                // If a create function is provided, use it instead of a mock object.
                return AddInjections(typeValueModel.CreateFunc.Invoke(this), typeValueModel.InstanceType);
            }

            if (!Strict)
            {
                // Only substitute the pre-made objects if the mock was not created.
                if (!Contains<IFileSystem>() && type.IsEquivalentTo(typeof(IFileSystem)))
                {
                    return fileSystem;
                }

                if (!Contains<HttpClient>() && type.IsEquivalentTo(typeof(HttpClient)))
                {
                    return HttpClient;
                }
            }

            if ((type.IsClass || type.IsInterface) && !type.IsSealed)
            {
                var mock = GetMock(type) ?? throw new InvalidProgramException("Unable to get the Mock.");

                if (!type.IsInterface)
                {
                    mock.CallBase = true;
                }

                var mockObject = mock.Object;
                initAction?.Invoke(mockObject);
                return mockObject;
            }
            else
            {
                var mockObject = type.GetDefaultValue();
                initAction?.Invoke(mockObject);
                return mockObject;
            }
        }

        /// <summary>
        ///     Gets the instance for the given <c>T</c> and runs the given function against the object.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="initAction">The initialize action.</param>
        /// <returns><c>T</c>.</returns>
        public T? GetObject<T>(Action<T?> initAction) where T : class => GetObject(typeof(T), t => initAction.Invoke(t as T)) as T;

        /// <summary>
        ///     Gets the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>System.Nullable&lt;T&gt;.</returns>
        public T? GetObject<T>() where T : class => GetObject(typeof(T)) as T;

        /// <summary>
        ///     Gets the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args">The arguments.</param>
        /// <returns>T.</returns>
        public T? GetObject<T>(params object?[] args) where T : class
        {
            var type = typeof(T).IsInterface ? GetTypeFromInterface<T>() : new InstanceModel<T>();
            var constructor = FindConstructor(type.InstanceType, true, args);
            return CreateInstanceInternal<T>(constructor.ConstructorInfo, args);
        }

        /// <summary>
        ///     Gets the protected mock.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>Mock of the protected mock.</returns>
        public Mock GetProtectedMock(Type type, params object?[] args)
        {
            type = CleanType(type);

            if (!Contains(type))
            {
                CreateMock(type, true, args);
            }

            return GetRequiredMock(type);
        }

        /// <summary>
        ///     Gets the required mock.
        /// </summary>
        /// <param name="type">The mock type, usually an interface.</param>
        /// <returns>Mock.</returns>
        /// <exception cref="ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.InvalidOperationException">type must be a class. - type</exception>
        public Mock GetRequiredMock(Type type) => type == null || (!type.IsClass && !type.IsInterface)
            ? throw new ArgumentException("type must be a class.", nameof(type))
            : mockCollection.First(x => x.Type == type).Mock;

        /// <summary>
        ///     Gets the required mock.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <returns><see cref="Mock{T}" />.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.InvalidOperationException">Mock must exist. - type</exception>
        public Mock<T> GetRequiredMock<T>() where T : class => (Mock<T>) GetRequiredMock(typeof(T));

        /// <summary>
        ///     Gets the content of the string.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>string.</returns>
        public async Task<string> GetStringContent(HttpContent content) =>
            content is ByteArrayContent data ? await data.ReadAsStringAsync() : string.Empty;

        /// <summary>
        ///     Gets or Creates then Initializes the specified Mock of <c>T</c>.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="action">The action.</param>
        /// <param name="reset"><c>False to keep the existing setup.</c></param>
        /// <returns><see cref="Mock{T}" /></returns>
        /// <exception cref="InvalidOperationException">Invalid Mock.</exception>
        /// <exception cref="System.InvalidOperationException">Invalid Mock.</exception>
        /// <example>
        /// Example of how to set up for mocks that require specific functionality.
        /// <code><![CDATA[
        /// mocks.Initialize<ICarService>(mock => {
        /// mock.Setup(x => x.StartCar).Returns(true));
        /// mock.Setup(x => x.StopCar).Returns(false));
        /// }
        /// ]]></code></example>
        public Mock<T> Initialize<T>(Action<Mock<T>> action, bool reset = true) where T : class
        {
            var mock = GetMock<T>() ?? throw new InvalidOperationException("Invalid Mock.");

            if (reset)
            {
                mock.Reset();
            }

            mock.SetupAllProperties();
            action.Invoke(mock);
            return mock;
        }

        /// <summary>
        ///     Invokes the static method.
        /// </summary>
        /// <typeparam name="TClass">The type of the t class.</typeparam>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="nonPublic">if set to <c>true</c> [non public].</param>
        /// <param name="args">The arguments used for the method.</param>
        /// <returns><see cref="Nullable" />.</returns>
        public object? InvokeMethod<TClass>(string methodName, bool nonPublic = false, params object?[] args)
            where TClass : class => InvokeMethod<TClass>(null, methodName, nonPublic, args);

        /// <summary>
        ///     Invokes the method.
        /// </summary>
        /// <typeparam name="TClass">The type of the t class.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="nonPublic">if set to <c>true</c> [non public].</param>
        /// <param name="args">The arguments used for the method.</param>
        /// <returns><see cref="Nullable" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public object? InvokeMethod<TClass>(TClass? obj, string methodName, bool nonPublic = false, params object?[] args)
            where TClass : class
        {
            var type = typeof(TClass).IsInterface ? GetTypeFromInterface<TClass>() : new InstanceModel<TClass>();

            var flags = BindingFlags.IgnoreCase |
                        BindingFlags.Public |
                        (obj != null ? BindingFlags.Instance : BindingFlags.Static) |
                        (nonPublic ? BindingFlags.NonPublic : BindingFlags.Public);

            var method = type.InstanceType.GetMethod(methodName, flags);

            return method switch
            {
                null when !nonPublic && !Strict => InvokeMethod(obj, methodName, true, args),
                null => throw new ArgumentOutOfRangeException(),
                _ => method.Invoke(obj, flags, null, args?.Any() ?? false ? args.ToArray() : GetMethodArgData(method), null),
            };
        }

        /// <summary>
        ///     Remove specified Mock of <c>T</c>.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="mock">Mock to Remove.</param>
        /// <returns><c>true</c> if the mock is successfully removed, <c>false</c> otherwise.</returns>
        public bool RemoveMock<T>(Mock<T> mock) where T : class
        {
            var mockModel = mockCollection.FirstOrDefault(x => x.Type == typeof(T) && x.Mock == mock);

            return mockModel != null && mockCollection.Remove(mockModel);
        }

        /// <summary>
        ///     Setups the HTTP message.
        /// </summary>
        /// <param name="messageFunc">The message function.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void SetupHttpMessage(Func<HttpResponseMessage> messageFunc, Expression? request = null, Expression? cancellationToken = null)
        {
            request ??= ItExpr.IsAny<HttpRequestMessage>();
            cancellationToken ??= ItExpr.IsAny<CancellationToken>();

            SetupMessageProtectedAsync<HttpMessageHandler, HttpResponseMessage>("SendAsync", messageFunc, request, cancellationToken);
        }

        /// <summary>
        ///     Setups the message.
        /// </summary>
        /// <typeparam name="TMock">The type of the mock.</typeparam>
        /// <typeparam name="TReturn">The type of the return value.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <param name="messageFunc">The message function.</param>
        public void SetupMessage<TMock, TReturn>(Expression<Func<TMock, TReturn>> expression, Func<TReturn> messageFunc)
            where TMock : class =>
            GetMock<TMock>()
                .Setup(expression)?
                .Returns(messageFunc)?.Verifiable();

        /// <summary>
        ///     Setups the message asynchronous.
        /// </summary>
        /// <typeparam name="TMock">The type of the mock.</typeparam>
        /// <typeparam name="TReturn">The type of the return value.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <param name="messageFunc">The message function.</param>
        /// <exception cref="InvalidDataException">Unable to setup '{typeof(TMock)}'.</exception>
        public void SetupMessageAsync<TMock, TReturn>(Expression<Func<TMock, Task<TReturn>>> expression, Func<TReturn> messageFunc)
            where TMock : class =>
            (GetMock<TMock>()
                 .Setup(expression) ??
             throw new InvalidDataException($"Unable to setup '{typeof(TMock)}'."))
            .ReturnsAsync(messageFunc)?.Verifiable();

        /// <summary>
        ///     Setups the message protected.
        /// </summary>
        /// <typeparam name="TMock">The type of the mock.</typeparam>
        /// <typeparam name="TReturn">The type of the return value.</typeparam>
        /// <param name="methodOrPropertyName">Name of the method or property.</param>
        /// <param name="messageFunc">The message function.</param>
        /// <param name="args">The arguments.</param>
        public void SetupMessageProtected<TMock, TReturn>(string methodOrPropertyName, Func<TReturn> messageFunc, params object?[]? args)
            where TMock : class =>
            GetMock<TMock>().Protected()
                ?.Setup<TReturn>(methodOrPropertyName, args ?? Array.Empty<object>())
                ?.Returns(messageFunc)?.Verifiable();

        /// <summary>
        ///     Setups the message protected asynchronous.
        /// </summary>
        /// <typeparam name="TMock">The type of the mock.</typeparam>
        /// <typeparam name="TReturn">The type of the return value.</typeparam>
        /// <param name="methodOrPropertyName">Name of the method or property.</param>
        /// <param name="messageFunc">The message function.</param>
        /// <param name="args">The arguments.</param>
        public void SetupMessageProtectedAsync<TMock, TReturn>(string methodOrPropertyName, Func<TReturn> messageFunc, params object?[]? args)
            where TMock : class =>
            GetMock<TMock>().Protected()
                ?.Setup<Task<TReturn>>(methodOrPropertyName, args ?? Array.Empty<object>())
                ?.ReturnsAsync(messageFunc)?.Verifiable();

        /// <summary>
        ///     Add specified Mock. Internal API only.
        /// </summary>
        /// <param name="mock">Mock to Add.</param>
        /// <param name="type">Type of Mock.</param>
        /// <param name="overwrite">
        ///     Overwrite if the mock exists or throw <see cref="ArgumentException" /> if this parameter is
        ///     false.
        /// </param>
        /// <param name="nonPublic">if set to <c>true</c> [non public].</param>
        /// <returns><see cref="Mock{T}" />.</returns>
        /// <exception cref="ArgumentNullException">nameof(mock)</exception>
        /// <exception cref="ArgumentNullException">nameof(type)</exception>
        /// <exception cref="System.ArgumentNullException">nameof(mock)</exception>
        /// <exception cref="System.ArgumentNullException">nameof(type)</exception>
        internal MockModel AddMock(Mock mock, Type type, bool overwrite = false, bool nonPublic = false)
        {
            if (mock == null)
            {
                throw new ArgumentNullException(nameof(mock));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (Contains(type))
            {
                if (!overwrite)
                {
                    mock.GetType().ThrowAlreadyExists();
                }

                var mockModel = GetMockModel(type);
                mockModel.Mock = mock;
                return GetMockModel(type);
            }

            mockCollection.Add(new MockModel(type, mock, nonPublic));

            return GetMockModel(type);
        }

        /// <summary>
        ///     Adds to constructor history.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="instanceModel">The instance model.</param>
        /// <returns>bool.</returns>
        internal bool AddToConstructorHistory(Type key, IHistoryModel instanceModel)
        {
            if (key is null || instanceModel is null)
            {
                return false;
            }

            var item = ConstructorHistory.FirstOrDefault(x => x.Key == key);

            if (item?.Key is null)
            {
                constructorHistory.Add(key, new List<IHistoryModel> { instanceModel });
            }
            else
            {
                constructorHistory[key].Add(instanceModel);
            }

            return true;
        }

        /// <summary>
        ///     Adds to constructor history.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="constructorInfo">The constructor information.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>bool.</returns>
        internal bool AddToConstructorHistory(Type key, ConstructorInfo? constructorInfo, List<object?> args) =>
            AddToConstructorHistory(key, new ConstructorModel(constructorInfo, args));

        /// <summary>
        ///     Ensure Type is correct.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>Type.</returns>
        internal Type CleanType(Type type) => type.Name.EndsWith('&')
            ? type.Assembly.GetTypes().FirstOrDefault(x => x.Name.Equals(type.Name.TrimEnd('&'))) ?? type
            : type;

        /// <summary>
        ///     Finds the constructor matching args EXACTLY by type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic">if set to <c>true</c> [non public].</param>
        /// <param name="args">The arguments.</param>
        /// <returns>ConstructorInfo.</returns>
        /// <exception cref="System.NotImplementedException">Unable to find the constructor.</exception>
        internal ConstructorModel FindConstructor(Type type, bool nonPublic, params object?[] args)
        {
            var allConstructors =
                nonPublic ? GetConstructorsNonPublic(type, args) : GetConstructors(type, args);

            var constructors = allConstructors
                .Where(x => x.ParameterList
                    .Select(z => z?.GetType())
                    .SequenceEqual(args.Select(y => y?.GetType()))
                )
                .ToList();

            return constructors.Any() switch
            {
                false when !nonPublic && !Strict => FindConstructor(type, true, args),
                false => throw new NotImplementedException("Unable to find the constructor."),
                _ => constructors[0],
            };
        }

        /// <summary>
        ///     Finds the constructor.
        /// </summary>
        /// <param name="bestGuess">if set to <c>true</c> [best guess].</param>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic">if set to <c>true</c> [non public].</param>
        /// <param name="excludeList">Constructors to ignore.</param>
        /// <returns><see cref="Tuple{ConstructorInfo, List}" />.</returns>
        /// <exception cref="AmbiguousImplementationException">
        ///     Multiple parameterized constructors exist. Cannot decide which to
        ///     use.
        /// </exception>
        /// <exception cref="NotImplementedException">Unable to find the constructor.</exception>
        /// <exception cref="System.Runtime.AmbiguousImplementationException">
        ///     Multiple parameterized constructors exist. Cannot decide which to
        ///     use.
        /// </exception>
        /// <exception cref="System.NotImplementedException">Unable to find the constructor.</exception>
        internal ConstructorModel FindConstructor(bool bestGuess, Type type, bool nonPublic, List<ConstructorInfo>? excludeList = null)
        {
            excludeList ??= new();

            var constructors = (nonPublic ? GetConstructorsNonPublic(type) : GetConstructors(type))
                .Where(x => excludeList.All(y => y != x.ConstructorInfo)).ToList();

            // if it is not best guess, then we can't know which constructor.
            if (!bestGuess && constructors.Count(x => x.ParameterList.Any()) > 1)
            {
                throw new AmbiguousImplementationException(
                    "Multiple parameterized constructors exist. Cannot decide which to use."
                );
            }

            // Best Guess //
            // Didn't find anything, should look for public version if it was not public.
            if (!constructors.Any() && !nonPublic && !Strict)
            {
                return FindConstructor(bestGuess, type, true, excludeList);
            }

            var validConstructors = GetTestedConstructors(type, constructors);

            if (!validConstructors.Any())
            {
                throw new NotImplementedException("Unable to find the constructor.");
            }

            return validConstructors.Last();
        }

        /// <summary>
        ///     Finds the type of the constructor by.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic">if set to <c>true</c> [non public].</param>
        /// <param name="args">The arguments.</param>
        /// <returns>ConstructorInfo.</returns>
        /// <exception cref="System.NotImplementedException">Unable to find the constructor.</exception>
        internal ConstructorInfo FindConstructorByType(Type type, bool nonPublic, params Type?[] args)
        {
            var constructors = GetConstructorsByType(nonPublic, type, args);

            return constructors.Any() switch
            {
                false when !nonPublic && !Strict => FindConstructorByType(type, true, args),
                false => throw new NotImplementedException("Unable to find the constructor."),
                _ => constructors[0],
            };
        }

        /// <summary>
        ///     Gets the argument data.
        /// </summary>
        /// <param name="constructor">The constructor.</param>
        /// <param name="data">The data.</param>
        /// <returns>Array of nullable objects.</returns>
        internal object?[] GetArgData(ConstructorInfo? constructor, Dictionary<Type, object?>? data)
        {
            var args = new List<object?>();

            constructor?.GetParameters().ToList().ForEach(p =>
                {
                    args.Add(data?.Any(x => x.Key == p.ParameterType) ?? false
                        ? data.First(x => x.Key == p.ParameterType).Value
                        : GetParameter(p.ParameterType)
                    );
                }
            );

            return args.ToArray();
        }

        /// <summary>
        ///     Gets the constructors.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="instanceParameterValues">Optional arguments.</param>
        /// <returns><see cref="Dictionary{ConstructorInfo, List}" />.</returns>
        internal List<ConstructorModel> GetConstructors(Type type, params object?[] instanceParameterValues)
        {
            var constructors = type.GetConstructors()
                .Where(x => x.GetParameters().All(y => y.ParameterType != type))
                .Where(x => type.IsValidConstructor(x, instanceParameterValues))
                .OrderBy(x => x.GetParameters().Length);

            return constructors
                .ToDictionary(x => x,
                    y => (instanceParameterValues.Length > 0 ? instanceParameterValues : y.GetParameters().Select(GetObject))
                        .ToList()
                )
                .Select(x => new ConstructorModel(x)).ToList();
        }

        /// <summary>
        ///     Gets the constructors non public.
        /// </summary>
        /// <param name="nonPublic">Include non public constructors.</param>
        /// <param name="type">The type.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <returns><see cref="List{ConstructorInfo}" />.</returns>
        internal List<ConstructorInfo> GetConstructorsByType(bool nonPublic, Type type, params Type?[] parameterTypes) =>
            type
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.IsValidConstructorByType(parameterTypes) && (nonPublic || x.IsPublic))
                .OrderBy(x => x.GetParameters().Length)
                .ToList();

        /// <summary>
        ///     Gets the constructors non public.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="instanceParameterValues">The instance parameter values.</param>
        /// <returns><see cref="Dictionary{ConstructorInfo, List}" />.</returns>
        internal List<ConstructorModel> GetConstructorsNonPublic(Type type,
            params object?[] instanceParameterValues)
        {
            var constructors = type
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                .Where(x => type.IsValidConstructor(x, instanceParameterValues))
                .OrderBy(x => x.GetParameters().Length);

            return constructors
                .ToDictionary(x => x,
                    y => (instanceParameterValues.Length > 0 ? instanceParameterValues : y.GetParameters().Select(GetObject))
                        .ToList()
                )
                .Select(x => new ConstructorModel(x)).ToList();
        }

        /// <summary>
        ///     Gets the mock model.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="mock">The mock.</param>
        /// <param name="autoCreate">Create Mock if it doesn't exist.</param>
        /// <returns><see cref="MockModel" />.</returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="System.NotImplementedException"></exception>
        internal MockModel GetMockModel(Type type, Mock? mock = null, bool autoCreate = true)
        {
            var first = mockCollection.FirstOrDefault(x => x.Type == type && (x.Mock == mock || mock == null));

            if (first != null)
            {
                return first;
            }

            if (!autoCreate)
            {
                throw new NotImplementedException();
            }

            return mock == null ? GetMockModel(type, GetMock(type), autoCreate) : AddMock(mock, type);
        }

        /// <summary>
        ///     Gets the mock model.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="mock">The mock.</param>
        /// <param name="autoCreate">Create Mock if it doesn't exist.</param>
        /// <returns><see cref="MockModel{T}" />.</returns>
        internal MockModel<T> GetMockModel<T>(Mock<T>? mock = null, bool autoCreate = true) where T : class =>
            new(GetMockModel(typeof(T), mock, autoCreate));

        /// <summary>
        ///     Gets the mock model index of.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="autoCreate">Create Mock if it doesn't exist.</param>
        /// <returns>System.Int32.</returns>
        internal int GetMockModelIndexOf(Type type, bool autoCreate = true) => mockCollection.IndexOf(GetMockModel(type, null, autoCreate));

        /// <summary>
        ///     Gets the parameter.
        /// </summary>
        /// <param name="parameterType">Type of the parameter.</param>
        /// <returns>object?.</returns>
        internal object? GetParameter(Type parameterType)
        {
            if (!parameterType.IsClass && !parameterType.IsInterface)
            {
                return parameterType.GetDefaultValue();
            }

            var typeValueModel = GetTypeModel(parameterType);

            if (typeValueModel.CreateFunc != null)
            {
                return typeValueModel.CreateFunc.Invoke(this);
            }

            return !parameterType.IsSealed ? GetObject(parameterType) : parameterType.GetDefaultValue();
        }

        /// <summary>
        ///     Gets the type from interface.
        /// </summary>
        /// <param name="tType">Type of the t.</param>
        /// <returns>Type.</returns>
        /// <exception cref="AmbiguousImplementationException"></exception>
        internal Type GetTypeFromInterface(Type tType)
        {
            if (!tType.IsInterface)
            {
                return tType;
            }

            var mappedType = typeMap.Where(x => x.Key == tType).Select(x => x.Value).FirstOrDefault();

            if (mappedType != null)
            {
                return mappedType.InstanceType;
            }

            var types = tType.Assembly.GetTypes().ToList();

            // Get interfaces that contain T.
            var interfaces = types.Where(type => type.IsInterface && type.GetInterfaces().Contains(tType)).ToList();

            // Get Types that contain T, but are not interfaces.
            var possibleTypes = types.Where(type =>
                type.GetInterfaces().Contains(tType) &&
                interfaces.All(iType => type != iType) &&
                !interfaces.Any(iType => iType.IsAssignableFrom(type))
            ).ToList();

            if (possibleTypes.Count > 1)
            {
                var publicCount = possibleTypes.Count(x => x.IsPublic);

                if (publicCount > 1)
                {
                    throw new AmbiguousImplementationException();
                }
            }

            return !possibleTypes.Any() ? tType : possibleTypes[0];
        }

        /// <summary>
        ///     Gets the type from interface.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <returns>InstanceModel.</returns>
        /// <exception cref="System.Runtime.AmbiguousImplementationException"></exception>
        /// <exception cref="System.NotImplementedException"></exception>
        internal IInstanceModel GetTypeFromInterface<T>() where T : class
        {
            var tType = typeof(T);
            var newType = GetTypeFromInterface(tType);
            var model = new InstanceModel(tType, newType) ?? throw new NotImplementedException();

            return model;
        }

        /// <summary>
        ///     Gets the map model.
        /// </summary>
        /// <typeparam name="TModel">The type of the t model.</typeparam>
        /// <returns>FastMoq.Models.InstanceModel&lt;TModel&gt;?.</returns>
        internal IInstanceModel GetTypeModel<TModel>() where TModel : class => GetTypeModel(typeof(TModel)) ?? new InstanceModel<TModel>();

        /// <summary>
        ///     Gets the map model.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>FastMoq.Models.InstanceModel?.</returns>
        internal IInstanceModel GetTypeModel(Type type) =>
            typeMap.ContainsKey(type) ? typeMap[type] : new InstanceModel(type, GetTypeFromInterface(type));

        /// <summary>
        ///     Determines whether [is mock file system] [the specified use predefined file system].
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="usePredefinedFileSystem">if set to <c>true</c> [use predefined file system].</param>
        /// <returns><c>true</c> if [is mock file system] [the specified use predefined file system]; otherwise, <c>false</c>.</returns>
        internal static bool IsMockFileSystem<T>(bool usePredefinedFileSystem) => usePredefinedFileSystem &&
                                                                                  (typeof(T) == typeof(IFileSystem) ||
                                                                                   typeof(T) == typeof(FileSystem));

        /// <summary>
        ///     Creates the mock internal.
        /// </summary>
        /// <param name="type">The type to create.</param>
        /// <param name="constructor">The constructor model.</param>
        /// <returns>Mock.</returns>
        private Mock CreateMockInternal(Type type, ConstructorModel constructor)
        {
            var newType = typeof(Mock<>).MakeGenericType(type);

            // Execute new Mock with Loose Behavior and arguments from constructor, if applicable.
            var parameters = new List<object?> { Strict ? MockBehavior.Strict : MockBehavior.Loose };
            constructor?.ParameterList.ToList().ForEach(parameters.Add);

            return Activator.CreateInstance(newType, parameters.ToArray()) is not Mock oMock
                ? throw new ApplicationException("Cannot create instance.")
                : oMock;
        }

        /// <summary>
        ///     Gets the tested constructors.
        /// </summary>
        /// <param name="type">The type to try to create.</param>
        /// <param name="constructors">The constructors to test with the specified type.</param>
        /// <returns>List&lt;FastMoq.Models.ConstructorModel&gt;.</returns>
        private List<ConstructorModel> GetTestedConstructors(Type type, List<ConstructorModel> constructors)
        {
            constructors ??= new();
            var validConstructors = new List<ConstructorModel>();

            if (constructors.Count <= 1)
            {
                return constructors;
            }

            var targetError = new List<ConstructorModel>();

            foreach (var constructor in constructors)
            {
                try
                {
                    // Test Constructor.
                    var mock = CreateMockInternal(type, constructor);
                    _ = mock.Object;
                    validConstructors.Add(constructor);
                }
                catch (TargetInvocationException)
                {
                    // Track invocation issues to bubble up if a good constructor is not found.
                    targetError.Add(constructor);
                }
                catch
                {
                    // Ignore
                }
            }

            return validConstructors.Any() ? validConstructors : targetError;
        }
    }
}

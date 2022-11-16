using Microsoft.AspNetCore.Components;
using Moq;
using Moq.Protected;
using System.Collections;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime;

namespace FastMoq
{
    /// <summary>
    ///     Initializes the mocking helper object. This class creates and manages the automatic mocking and custom mocking.
    /// </summary>
    public class Mocker
    {
        #region Fields

        /// <summary>
        ///     The virtual mock file system that is used by mocker unless overridden with the <see cref="Strict" /> property.
        /// </summary>
        public readonly MockFileSystem fileSystem;

        /// <summary>
        ///     List of <see cref="MockModel" />.
        /// </summary>
        protected internal readonly List<MockModel> mockCollection;

        /// <summary>
        ///     <see cref="Dictionary{TKey,TValue}" /> of <see cref="Type" /> mapped to <see cref="InstanceModel" />.
        ///     This map assists in resolution of interfaces to instances.
        /// </summary>
        /// <value>The type map.</value>
        internal readonly Dictionary<Type, InstanceModel> typeMap;

        private bool SetupHttpFactory;

        #endregion

        #region Properties

        /// <summary>
        ///     The virtual mock http client that is used by mocker unless overridden with the <see cref="Strict" /> property.
        /// </summary>
        public HttpClient HttpClient { get; }

        public bool InnerMockResolution { get; set; } = true;

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="Mocker" /> is strict. If strict, the mock
        ///     <see cref="IFileSystem" /> does
        ///     not use <see cref="MockFileSystem" /> and uses <see cref="Mock" /> of <see cref="IFileSystem" />.
        /// </summary>
        /// <value>
        ///     <c>true</c> if strict <see cref="IFileSystem" /> resolution; otherwise, <c>false</c> uses the built-in virtual
        ///     <see cref="MockFileSystem" />.
        /// </value>
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
        public Mocker(Dictionary<Type, InstanceModel> typeMap) : this() => this.typeMap = typeMap;

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
            IEnumerable<PropertyInfo> properties = GetInjectionProperties(referenceType);
            IEnumerable<FieldInfo> fields = GetInjectionFields(referenceType);

            properties.ForEach(y => obj.SetPropertyValue(y.Name, GetObject(y.PropertyType)));
            fields.ForEach(y => obj.SetFieldValue(y.Name, GetObject(y.FieldType)));

            return obj;
        }

        /// <summary>
        ///     Creates a <see cref="MockModel" /> with the given <see cref="Mock" /> with the option of overwriting an existing
        ///     <see cref="MockModel" />
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="mock">Mock to Add.</param>
        /// <param name="overwrite">
        ///     Overwrite if the mock exists or throw <see cref="ArgumentException" /> if this parameter is
        ///     false.
        /// </param>
        /// <param name="nonPublic">if set to <c>true</c> uses public and non public constructors.</param>
        /// <returns><see cref="MockModel{T}" />.</returns>
        public MockModel<T> AddMock<T>(Mock<T> mock, bool overwrite, bool nonPublic = false) where T : class =>
            new(AddMock(mock, typeof(T), overwrite, nonPublic));

        /// <summary>
        ///     Adds an interface to Class mapping to the <see cref="typeMap" /> for easier resolution.
        /// </summary>
        /// <typeparam name="TInterface">The interface or class Type which can be mapped to a specific Class.</typeparam>
        /// <typeparam name="TClass">The Class Type (cannot be an interface) that can be created from <see cref="TInterface" />.</typeparam>
        /// <param name="createFunc">An optional create function used to create the class.</param>
        public void AddType<TInterface, TClass>(Func<Mocker, TClass>? createFunc = null)
            where TInterface : class where TClass : class
        {
            if (typeof(TClass).IsInterface)
            {
                throw new ArgumentException($"{typeof(TClass).Name} cannot be an interface.");
            }

            typeMap.Add(typeof(TInterface), new InstanceModel<TClass>(createFunc));
        }

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
        /// <exception cref="System.ArgumentNullException">type</exception>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        public bool Contains(Type type) =>
            type == null ? throw new ArgumentNullException(nameof(type)) :
            !type.IsClass && !type.IsInterface ? throw new ArgumentException("type must be a class.", nameof(type)) :
            mockCollection.Any(x => x.Type == type);

        /// <summary>
        ///     Creates the HTTP client.
        /// </summary>
        /// <returns><see cref="httpClient" />.</returns>
        public HttpClient CreateHttpClient(string clientName = "FastMoqHttpClient", string baseAddress = "http://localhost",
            HttpStatusCode statusCode = HttpStatusCode.OK, string stringContent = "[{'id':1, 'value':'1'}]")
        {
            var baseUri = new Uri(baseAddress);

            if (!Contains<HttpMessageHandler>())
            {
                GetMock<HttpMessageHandler>().Protected()
                    ?.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(() =>
                        new HttpResponseMessage
                        {
                            StatusCode = statusCode,
                            Content = new StringContent(stringContent)
                        }
                    ).Verifiable();
            }

            if (!Contains<IHttpClientFactory>())
            {
                SetupHttpFactory = true;

                GetMock<IHttpClientFactory>().Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(() => CreateHttpClientInternal(baseUri));
            }

            return SetupHttpFactory
                ? GetObject<IHttpClientFactory>()?.CreateClient(clientName) ?? throw new ApplicationException("Unable to create IHttpClientFactory.")
                : CreateHttpClientInternal(baseUri);
        }

        /// <summary>
        ///     Creates an instance of <c>T</c>. Parameters allow matching of constructors and using those values in the creation
        ///     of the instance.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="args">The optional arguments used to create the instance.</param>
        /// <returns><see cref="Nullable{T}" />.</returns>
        /// <example>
        ///     <code><![CDATA[
        /// IFileSystem fileSystem = CreateInstance<IFileSystem>();
        /// ]]></code>
        /// </example>
        public T? CreateInstance<T>(params object?[] args) where T : class => CreateInstance<T>(true, args);

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1>(Dictionary<Type, object?> data) where T : class => CreateInstanceInternal<T>(
            model => FindConstructorByType(model.InstanceType, true, typeof(TParam1)),
            data
        );

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <typeparam name="TParam2">The type of the t param2.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1, TParam2>(Dictionary<Type, object?> data) where T : class => CreateInstanceInternal<T>(
            model => FindConstructorByType(model.InstanceType, true, typeof(TParam1), typeof(TParam2)),
            data
        );

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <typeparam name="TParam2">The type of the t param2.</typeparam>
        /// <typeparam name="TParam3">The type of the t param3.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1, TParam2, TParam3>(Dictionary<Type, object?> data) where T : class =>
            CreateInstanceInternal<T>(
                model => FindConstructorByType(model.InstanceType, true, typeof(TParam1), typeof(TParam2), typeof(TParam3)),
                data
            );

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <typeparam name="TParam2">The type of the t param2.</typeparam>
        /// <typeparam name="TParam3">The type of the t param3.</typeparam>
        /// <typeparam name="TParam4">The type of the t param4.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1, TParam2, TParam3, TParam4>(Dictionary<Type, object?> data) where T : class =>
            CreateInstanceInternal<T>(
                model => FindConstructorByType(model.InstanceType,
                    true,
                    typeof(TParam1),
                    typeof(TParam2),
                    typeof(TParam3),
                    typeof(TParam4)
                ),
                data
            );

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <typeparam name="TParam2">The type of the t param2.</typeparam>
        /// <typeparam name="TParam3">The type of the t param3.</typeparam>
        /// <typeparam name="TParam4">The type of the t param4.</typeparam>
        /// <typeparam name="TParam5">The type of the t param5.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1, TParam2, TParam3, TParam4, TParam5>(Dictionary<Type, object?> data) where T : class =>
            CreateInstanceInternal<T>(
                model => FindConstructorByType(model.InstanceType,
                    true,
                    typeof(TParam1),
                    typeof(TParam2),
                    typeof(TParam3),
                    typeof(TParam4),
                    typeof(TParam5)
                ),
                data
            );

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <typeparam name="TParam2">The type of the t param2.</typeparam>
        /// <typeparam name="TParam3">The type of the t param3.</typeparam>
        /// <typeparam name="TParam4">The type of the t param4.</typeparam>
        /// <typeparam name="TParam5">The type of the t param5.</typeparam>
        /// <typeparam name="TParam6">The type of the t param6.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>(Dictionary<Type, object?> data)
            where T : class => CreateInstanceInternal<T>(
            model => FindConstructorByType(model.InstanceType,
                true,
                typeof(TParam1),
                typeof(TParam2),
                typeof(TParam3),
                typeof(TParam4),
                typeof(TParam5),
                typeof(TParam6)
            ),
            data
        );

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <typeparam name="TParam2">The type of the t param2.</typeparam>
        /// <typeparam name="TParam3">The type of the t param3.</typeparam>
        /// <typeparam name="TParam4">The type of the t param4.</typeparam>
        /// <typeparam name="TParam5">The type of the t param5.</typeparam>
        /// <typeparam name="TParam6">The type of the t param6.</typeparam>
        /// <typeparam name="TParam7">The type of the t param7.</typeparam>
        /// <param name="data">The arguments.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7>(Dictionary<Type, object?> data)
            where T : class => CreateInstanceInternal<T>(
            model => FindConstructorByType(model.InstanceType,
                true,
                typeof(TParam1),
                typeof(TParam2),
                typeof(TParam3),
                typeof(TParam4),
                typeof(TParam5),
                typeof(TParam6),
                typeof(TParam7)
            ),
            data
        );

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <typeparam name="TParam2">The type of the t param2.</typeparam>
        /// <typeparam name="TParam3">The type of the t param3.</typeparam>
        /// <typeparam name="TParam4">The type of the t param4.</typeparam>
        /// <typeparam name="TParam5">The type of the t param5.</typeparam>
        /// <typeparam name="TParam6">The type of the t param6.</typeparam>
        /// <typeparam name="TParam7">The type of the t param7.</typeparam>
        /// <typeparam name="TParam8">The type of the t param8.</typeparam>
        /// <param name="data">The arguments.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8>(
            Dictionary<Type, object?> data) where T : class => CreateInstanceInternal<T>(
            model => FindConstructorByType(model.InstanceType,
                true,
                typeof(TParam1),
                typeof(TParam2),
                typeof(TParam3),
                typeof(TParam4),
                typeof(TParam5),
                typeof(TParam6),
                typeof(TParam7),
                typeof(TParam8)
            ),
            data
        );

        /// <summary>
        ///     Creates an instance of <see cref="IFileSystem" />.
        /// </summary>
        /// <typeparam name="T"><see cref="IFileSystem" />.</typeparam>
        /// <param name="usePredefinedFileSystem">if set to <c>true</c> [use predefined file system].</param>
        /// <returns><see cref="Nullable{IFileSystem}" />.</returns>
        public IFileSystem? CreateInstance<T>(bool usePredefinedFileSystem) where T : class, IFileSystem =>
            CreateInstance<T>(usePredefinedFileSystem, Array.Empty<object>());

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="usePredefinedFileSystem">if set to <c>true</c> [use predefined file system].</param>
        /// <param name="args">The arguments.</param>
        /// <returns><see cref="Nullable{T}" />.</returns>
        public T? CreateInstance<T>(bool usePredefinedFileSystem, params object?[] args) where T : class
        {
            if (IsMockFileSystem<T>(usePredefinedFileSystem))
            {
                return fileSystem as T;
            }

            var tType = typeof(T);
            var typeInstanceModel = GetMapModel<T>() ?? (tType.IsInterface ? GetTypeFromInterface<T>() : new InstanceModel<T>());

            if (typeInstanceModel.CreateFunc != null)
            {
                return (T) typeInstanceModel.CreateFunc.Invoke(this);
            }

            args ??= Array.Empty<object>();

            var constructor =
                args.Length > 0
                    ? FindConstructor(typeInstanceModel.InstanceType, false, args)
                    : FindConstructor(false, typeInstanceModel.InstanceType, false);

            return CreateInstanceInternal<T>(constructor);
        }

        /// <summary>
        ///     Creates an instance of <c>T</c>.
        ///     Non public constructors are included as options for creating the instance.
        ///     Parameters allow matching of constructors and using those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="args">The arguments.</param>
        /// <returns>
        ///     <see cref="Nullable{T}" />
        /// </returns>
        /// <example>
        ///     <code><![CDATA[
        /// IModel model = CreateInstanceNonPublic<IModel>();
        /// ]]></code>
        /// </example>
        public T? CreateInstanceNonPublic<T>(params object?[] args) where T : class
        {
            var type = typeof(T).IsInterface ? GetTypeFromInterface<T>() : new InstanceModel<T>();

            return type.CreateFunc != null
                ? (T) type.CreateFunc.Invoke(this)
                : CreateInstanceNonPublic(type.InstanceType, args) as T;
        }

        /// <summary>
        ///     Creates the instance non public.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>System.Nullable&lt;System.Object&gt;.</returns>
        public object? CreateInstanceNonPublic(Type type, params object?[] args)
        {
            var constructor =
                args.Length > 0
                    ? FindConstructor(type, true, args)
                    : FindConstructor(false, type, true);

            return constructor.ConstructorInfo?.Invoke(constructor.ParameterList);
        }

        /// <summary>
        ///     Creates the <see cref="MockModel" /> from the <c>Type</c>. This throws an exception if the mock already exists.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic"><c>true</c> if non public and public constructors are used.</param>
        /// <returns><see cref="List{Mock}" />.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ApplicationException">Cannot create instance.</exception>
        public List<MockModel> CreateMock(Type type, bool nonPublic = false)
        {
            if (Contains(type))
            {
                ThrowAlreadyExists(type);
            }

            var oMock = CreateMockInstance(type, nonPublic);

            AddMock(oMock, type);
            return mockCollection;
        }

        /// <summary>
        ///     Creates the <see cref="MockModel" /> from the type <c>T</c>. This throws an exception if the mock already exists.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="nonPublic">if set to <c>true</c> public and non public constructors are used.</param>
        /// <returns><see cref="List{T}" />.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ArgumentException">type already exists. - type</exception>
        /// <exception cref="System.ApplicationException">Cannot create instance.</exception>
        public List<MockModel> CreateMock<T>(bool nonPublic = false) where T : class => CreateMock(typeof(T), nonPublic);

        /// <summary>
        ///     Creates the mock instance that is not automatically injected.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic">if set to <c>true</c> [non public].</param>
        /// <returns>Mock.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ApplicationException">Cannot create instance.</exception>
        public Mock CreateMockInstance(Type type, bool nonPublic = false)
        {
            if (type == null || (!type.IsClass && !type.IsInterface))
            {
                throw new ArgumentException("type must be a class.", nameof(type));
            }

            var constructor = new ConstructorModel(null, new List<object?>());

            try
            {
                if (!type.IsInterface && InnerMockResolution)
                {
                    // Find the best constructor and build the parameters.
                    constructor = FindConstructor(true, type, nonPublic);
                }
            }
            catch
            {
                // Ignore
            }

            var newType = typeof(Mock<>).MakeGenericType(type);

            // Execute new Mock with Loose Behavior and arguments from constructor, if applicable.
            var parameters = new List<object?> {Strict ? MockBehavior.Strict : MockBehavior.Loose};
            constructor?.ParameterList.ToList().ForEach(parameter => parameters.Add(parameter));

            if (Activator.CreateInstance(newType, parameters.ToArray()) is not Mock oMock)
            {
                throw new ApplicationException("Cannot create instance.");
            }

            if (!Strict)
            {
                InvokeMethod<Mock>(null, "SetupAllProperties", true, oMock);
            }

            AddInjections(oMock.Object, GetMapModel(type)?.InstanceType ?? type);

            return oMock;
        }

        /// <summary>
        ///     Creates the mock instance that is not automatically injected.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nonPublic">if set to <c>true</c> [non public].</param>
        /// <returns>Mock.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ApplicationException">Cannot create instance.</exception>
        public Mock<T> CreateMockInstance<T>(bool nonPublic = false) where T : class => (Mock<T>) CreateMockInstance(typeof(T), nonPublic);

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
        ///     Gets the default value.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see cref="Nullable{T}" />.</returns>
        public static object? GetDefaultValue(Type type) => type switch
        {
            {FullName: "System.String"} => string.Empty,
            _ when typeof(IEnumerable).IsAssignableFrom(type) => Array.CreateInstance(type.GetElementType() ?? typeof(object), 0),
            {IsClass: true} => null,
            _ => Activator.CreateInstance(type)
        };

        /// <summary>
        ///     Gets a list with the specified number of list items, using a custom function.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="count">The number of list items.</param>
        /// <param name="func">The function for creating the list items.</param>
        /// <param name="initAction">The initialize action.</param>
        /// <returns><see cref="List{T}" />.</returns>
        /// <example>
        ///     Example of how to create a list.
        ///     <code><![CDATA[
        /// GetList<Model>(3, (i) => new Model(name: i.ToString()));
        /// ]]></code>
        ///     or
        ///     <code><![CDATA[
        /// GetList<IModel>(3, (i) => Mocks.CreateInstance<IModel>(i));
        /// ]]></code>
        /// </example>
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
        ///     Example of how to create a list.
        ///     <code><![CDATA[
        /// GetList<Model>(3, (i) => new Model(name: i.ToString()));
        /// ]]></code>
        ///     or
        ///     <code><![CDATA[
        /// GetList<IModel>(3, (i) => Mocks.CreateInstance<IModel>(i));
        /// ]]></code>
        /// </example>
        public static List<T> GetList<T>(int count, Func<int, T>? func) => GetList(count, func, null);

        /// <summary>
        ///     Gets a list with the specified number of list items, using a custom function.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="count">The number of list items.</param>
        /// <param name="func">The function for creating the list items.</param>
        /// <returns><see cref="List{T}" />.</returns>
        /// <example>
        ///     Example of how to create a list.
        ///     <code><![CDATA[
        /// GetList<Model>(3, () => new Model(name: Guid.NewGuid().ToString()));
        /// ]]></code>
        ///     or
        ///     <code><![CDATA[
        /// GetList<IModel>(3, () => Mocks.CreateInstance<IModel>());
        /// ]]></code>
        /// </example>
        public static List<T> GetList<T>(int count, Func<T>? func) =>
            func == null ? new List<T>() : GetList(count, _ => func.Invoke());

        /// <summary>
        ///     Gets the method argument data.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="data">The data.</param>
        /// <returns>System.Nullable&lt;System.Object&gt;[].</returns>
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

        public object?[] GetMethodDefaultData(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            var args = new List<object?>();
            method.GetParameters().ToList().ForEach(p => { args.Add(GetDefaultValue(p.ParameterType)); });

            return args.ToArray();
        }

        /// <summary>
        ///     Gets or creates the mock of type <c>T</c>.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <returns><see cref="Mock{T}" />.</returns>
        public Mock<T> GetMock<T>() where T : class => (Mock<T>) GetMock(typeof(T));

        /// <summary>
        ///     Gets of creates the mock of <c>type</c>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see cref="Mock" />.</returns>
        public Mock GetMock(Type type)
        {
            if (!Contains(type))
            {
                CreateMock(type);
            }

            return GetRequiredMock(type);
        }

        /// <summary>
        ///     Gets the instance for the given <see cref="ParameterInfo" />.
        /// </summary>
        /// <param name="info">The <see cref="ParameterInfo" />.</param>
        /// <returns>
        ///     <see cref="Nullable{Object}" />
        /// </returns>
        /// <exception cref="System.ArgumentNullException">type</exception>
        /// <exception cref="System.InvalidProgramException">Unable to get the Mock.</exception>
        public object? GetObject(ParameterInfo info)
        {
            try
            {
                return GetParameter(info.ParameterType);
            }
            catch
            {
                return GetDefaultValue(info.ParameterType);
            }
        }

        /// <summary>
        ///     Gets the instance for the given <c>type</c>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="initAction">The initialize action.</param>
        /// <returns><see cref="Nullable{Object}" />.</returns>
        /// <exception cref="System.ArgumentNullException">type</exception>
        /// <exception cref="System.InvalidProgramException">Unable to get the Mock.</exception>
        public object? GetObject(Type type, Action<object?>? initAction = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var typeValueModel = GetMapModel(type);

            if (typeValueModel?.CreateFunc != null)
            {
                // If a create function is provided, use it instead of a mock object.
                return AddInjections(typeValueModel.CreateFunc?.Invoke(this), typeValueModel.InstanceType);
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
                var mockObject = GetDefaultValue(type);
                initAction?.Invoke(mockObject);
                return mockObject;
            }
        }

        /// <summary>
        ///     Gets the instance for the given <c>T</c>.
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
            var constructor = FindConstructor(false, type.InstanceType, true);
            return CreateInstanceInternal<T>(constructor.ConstructorInfo, args);
        }

        /// <summary>
        ///     Gets the required mock.
        /// </summary>
        /// <param name="type">The mock type, usually an interface.</param>
        /// <returns>Mock.</returns>
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
        /// <param name="reset">
        ///     <c>False to keep the existing setup.</c>
        /// </param>
        /// <returns>
        ///     <see cref="Mock{T}" />
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Invalid Mock.</exception>
        /// <example>
        ///     Example of how to set up for mocks that require specific functionality.
        ///     <code><![CDATA[
        /// mocks.Initialize<ICarService>(mock => {
        ///     mock.Setup(x => x.StartCar).Returns(true));
        ///     mock.Setup(x => x.StopCar).Returns(false));
        /// }
        /// ]]></code>
        /// </example>
        public Mock<T> Initialize<T>(Action<Mock<T>> action, bool reset = true) where T : class
        {
            Mock<T> mock = GetMock<T>() ?? throw new InvalidOperationException("Invalid Mock.");

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
        /// <param name="args">The arguments.</param>
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
        /// <param name="args">The arguments.</param>
        /// <returns><see cref="Nullable" />.</returns>
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

            return method == null && !nonPublic && !Strict ? InvokeMethod(obj, methodName, true, args) :
                method == null ? throw new ArgumentOutOfRangeException() :
                method.Invoke(obj, flags, null, args?.Any() ?? false ? args.ToArray() : GetMethodArgData(method), null);
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
        public void SetupMessage<TMock, TReturn>(Expression<Func<TMock, TReturn>> expression, Func<TReturn> messageFunc) where TMock : class =>
            GetMock<TMock>()
                ?.Setup(expression)
                .Returns(messageFunc).Verifiable();

        /// <summary>
        ///     Setups the message asynchronous.
        /// </summary>
        /// <typeparam name="TMock">The type of the mock.</typeparam>
        /// <typeparam name="TReturn">The type of the return value.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <param name="messageFunc">The message function.</param>
        public void SetupMessageAsync<TMock, TReturn>(Expression<Func<TMock, Task<TReturn>>> expression, Func<TReturn> messageFunc)
            where TMock : class =>
            GetMock<TMock>()
                ?.Setup(expression)
                .ReturnsAsync(messageFunc).Verifiable();

        /// <summary>
        ///     Setups the message protected.
        /// </summary>
        /// <typeparam name="TMock">The type of the mock.</typeparam>
        /// <typeparam name="TReturn">The type of the return value.</typeparam>
        /// <param name="methodOrPropertyName">Name of the method or property.</param>
        /// <param name="messageFunc">The message function.</param>
        /// <param name="args">The arguments.</param>
        public void SetupMessageProtected<TMock, TReturn>(string methodOrPropertyName, Func<TReturn> messageFunc, params Expression[] args)
            where TMock : class =>
            GetMock<TMock>().Protected()
                ?.Setup<TReturn>(methodOrPropertyName, args)
                .Returns(messageFunc).Verifiable();

        /// <summary>
        ///     Setups the message protected asynchronous.
        /// </summary>
        /// <typeparam name="TMock">The type of the mock.</typeparam>
        /// <typeparam name="TReturn">The type of the return value.</typeparam>
        /// <param name="methodOrPropertyName">Name of the method or property.</param>
        /// <param name="messageFunc">The message function.</param>
        /// <param name="args">The arguments.</param>
        public void SetupMessageProtectedAsync<TMock, TReturn>(string methodOrPropertyName, Func<TReturn> messageFunc, params Expression[] args)
            where TMock : class =>
            GetMock<TMock>().Protected()
            ?.Setup<Task<TReturn>>(methodOrPropertyName, args)
            .ReturnsAsync(messageFunc).Verifiable();

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
        /// <exception cref="System.ArgumentNullException">mock</exception>
        /// <exception cref="System.ArgumentNullException">type</exception>
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
                    ThrowAlreadyExists(mock.GetType());
                }

                var mockModel = GetMockModel(type);
                mockModel.Mock = mock;
                return GetMockModel(type);
            }

            mockCollection.Add(new MockModel(type, mock, nonPublic));

            return GetMockModel(type);
        }

        internal HttpClient CreateHttpClientInternal(Uri baseUri) =>
            new(GetObject<HttpMessageHandler>() ?? throw new ApplicationException("Unable to create HttpMessageHandler."))
            {
                BaseAddress = baseUri
            };

        /// <summary>
        ///     Create an instance using the constructor by the function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="constructorFunc">The constructor function.</param>
        /// <param name="data">The arguments.</param>
        /// <returns>T.</returns>
        internal T? CreateInstanceInternal<T>(Func<InstanceModel, ConstructorInfo> constructorFunc, Dictionary<Type, object?>? data) where T : class
        {
            var type = typeof(T).IsInterface ? GetTypeFromInterface<T>() : new InstanceModel<T>();

            if (type.CreateFunc != null)
            {
                return (T) type.CreateFunc.Invoke(this);
            }

            data ??= new Dictionary<Type, object?>();
            var constructor = constructorFunc(type);

            var args = GetArgData(constructor, data);

            return CreateInstanceInternal<T>(constructor, args);
        }

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="constructorModel">The constructor model.</param>
        /// <returns>T.</returns>
        internal T? CreateInstanceInternal<T>(ConstructorModel constructorModel) where T : class =>
            CreateInstanceInternal<T>(constructorModel.ConstructorInfo, constructorModel.ParameterList);

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="info">The information.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>T.</returns>
        internal T? CreateInstanceInternal<T>(ConstructorInfo? info, params object?[] args) where T : class =>
            CreateInstanceInternal(typeof(T), info, args) as T;

        internal object? CreateInstanceInternal(Type type, ConstructorInfo? info, params object?[] args) => AddInjections(info?.Invoke(args));

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
            Dictionary<ConstructorInfo, List<object?>> allConstructors =
                nonPublic ? GetConstructorsNonPublic(type, args) : GetConstructors(type, args);

            List<KeyValuePair<ConstructorInfo, List<object?>>> constructors = allConstructors
                .Where(x => x.Value
                    .Select(z => z?.GetType())
                    .SequenceEqual(args.Select(y => y?.GetType()))
                )
                .ToList();

            return !constructors.Any() && !nonPublic && !Strict ? FindConstructor(type, true, args) :
                !constructors.Any() ? throw new NotImplementedException("Unable to find the constructor.") :
                new ConstructorModel(constructors.First());
        }

        /// <summary>
        ///     Finds the constructor.
        /// </summary>
        /// <param name="bestGuess">if set to <c>true</c> [best guess].</param>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic">if set to <c>true</c> [non public].</param>
        /// <returns><see cref="Tuple{ConstructorInfo, List}" />.</returns>
        /// <exception cref="System.Runtime.AmbiguousImplementationException">
        ///     Multiple parameterized constructors exist. Cannot
        ///     decide which to use.
        /// </exception>
        /// <exception cref="System.NotImplementedException">Unable to find the constructor.</exception>
        internal ConstructorModel FindConstructor(bool bestGuess, Type type, bool nonPublic)
        {
            Dictionary<ConstructorInfo, List<object?>> constructors = nonPublic ? GetConstructorsNonPublic(type) : GetConstructors(type);

            if (!bestGuess && constructors.Values.Count(x => x.Count > 0) > 1)
            {
                throw new AmbiguousImplementationException(
                    "Multiple parameterized constructors exist. Cannot decide which to use."
                );
            }

            return !constructors.Any() && !nonPublic && !Strict ? FindConstructor(bestGuess, type, true) :
                !constructors.Any() ? throw new NotImplementedException("Unable to find the constructor.") :
                new ConstructorModel(constructors.First());
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
            List<ConstructorInfo> constructors = GetConstructorsByType(nonPublic, type, args);

            return !constructors.Any() && !nonPublic && !Strict ? FindConstructorByType(type, true, args) :
                !constructors.Any() ? throw new NotImplementedException("Unable to find the constructor.") : constructors.First();
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
        internal Dictionary<ConstructorInfo, List<object?>>
            GetConstructors(Type type, params object?[] instanceParameterValues) => type.GetConstructors()
            .Where(x => IsValidConstructor(x, instanceParameterValues))
            .OrderByDescending(x => x.GetParameters().Length)
            .ToDictionary(x => x,
                y => (instanceParameterValues.Length > 0 ? instanceParameterValues : y.GetParameters().Select(GetObject))
                    .ToList()
            );

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
                .Where(x => IsValidConstructorByType(x, parameterTypes) && (nonPublic || x.IsPublic))
                .OrderByDescending(x => x.GetParameters().Length)
                .ToList();

        /// <summary>
        ///     Gets the constructors non public.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="instanceParameterValues">The instance parameter values.</param>
        /// <returns><see cref="Dictionary{ConstructorInfo, List}" />.</returns>
        internal Dictionary<ConstructorInfo, List<object?>> GetConstructorsNonPublic(Type type,
            params object?[] instanceParameterValues) => type
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
            .Where(x => IsValidConstructor(x, instanceParameterValues))
            .OrderByDescending(x => x.GetParameters().Length)
            .ToDictionary(x => x,
                y => (instanceParameterValues.Length > 0 ? instanceParameterValues : y.GetParameters().Select(GetObject))
                    .ToList()
            );

        /// <summary>
        ///     Gets the injection fields.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="attributeType">Override attribute type.</param>
        /// <returns><see cref="IEnumerable{T}" />.</returns>
        internal IEnumerable<FieldInfo> GetInjectionFields(Type type, Type? attributeType = null) =>
            type
                .GetRuntimeFields()
                .Where(x => x.CustomAttributes.Any(y =>
                        y.AttributeType == (attributeType ?? typeof(InjectAttribute)) ||
                        y.AttributeType.Name.Equals("InjectAttribute", StringComparison.OrdinalIgnoreCase)
                    )
                );

        /// <summary>
        ///     Gets the injection properties.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="attributeType">Override attribute type.</param>
        /// <returns><see cref="IEnumerable{T}" />.</returns>
        internal IEnumerable<PropertyInfo> GetInjectionProperties(Type type, Type? attributeType = null) =>
            type
                .GetRuntimeProperties()
                .Where(x => x.CustomAttributes.Any(y =>
                        y.AttributeType == (attributeType ?? typeof(InjectAttribute)) ||
                        y.AttributeType.Name.Equals("InjectAttribute", StringComparison.OrdinalIgnoreCase)
                    )
                );

        internal InstanceModel<TModel>? GetMapModel<TModel>() where TModel : class => GetMapModel(typeof(TModel)) as InstanceModel<TModel>;

        internal InstanceModel? GetMapModel(Type type) => typeMap.ContainsKey(type) ? typeMap[type] : null;

        /// <summary>
        ///     Gets the mock model.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="mock">The mock.</param>
        /// <param name="autoCreate">Create Mock if it doesn't exist.</param>
        /// <returns><see cref="MockModel" />.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        internal MockModel GetMockModel(Type type, Mock? mock = null, bool autoCreate = true) =>
            mockCollection.FirstOrDefault(x => x.Type == type && (x.Mock == mock || mock == null)) ??
            (mock == null ? autoCreate ? GetMockModel(type, GetMock(type), autoCreate) : throw new NotImplementedException() :
                autoCreate ? AddMock(mock, type) : throw new NotImplementedException());

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

        internal object? GetParameter(Type parameterType) =>
            (parameterType.IsClass || parameterType.IsInterface) && !parameterType.IsSealed
                ? GetObject(parameterType)
                : GetDefaultValue(parameterType);

        /// <summary>
        ///     Gets the type from interface.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <returns>InstanceModel.</returns>
        /// <exception cref="System.Runtime.AmbiguousImplementationException"></exception>
        /// <exception cref="System.NotImplementedException"></exception>
        internal InstanceModel GetTypeFromInterface<T>() where T : class
        {
            var tType = typeof(T);

            if (!tType.IsInterface)
            {
                return new InstanceModel<T>();
            }

            var mappedType = typeMap.Where(x => x.Key == typeof(T)).Select(x => x.Value).FirstOrDefault();

            if (mappedType != null)
            {
                return mappedType;
            }

            List<Type> types = tType.Assembly.GetTypes().ToList();

            // Get interfaces that contain T.
            List<Type> interfaces = types.Where(type => type.IsInterface && type.GetInterfaces().Contains(tType)).ToList();

            // Get Types that contain T, but are not interfaces.
            List<Type> possibleTypes = types.Where(type =>
                type.GetInterfaces().Contains(tType) &&
                interfaces.All(iType => type != iType) &&
                !interfaces.Any(iType => iType.IsAssignableFrom(type))
            ).ToList();

            return new InstanceModel(possibleTypes.Count > 1 ? throw new AmbiguousImplementationException() :
                !possibleTypes.Any() ? throw new NotImplementedException() : possibleTypes.First()
            );
        }

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
        ///     Determines whether [is nullable type] [the specified type].
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if [is nullable type] [the specified type]; otherwise, <c>false</c>.</returns>
        internal static bool IsNullableType(Type type) => type.IsClass ||
                                                          (
                                                              type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));

        /// <summary>
        ///     Returns true if the argument list == 0 or the types match the constructor exactly.
        /// </summary>
        /// <param name="info">Parameter information.</param>
        /// <param name="instanceParameterValues">Optional arguments.</param>
        /// <returns><c>true</c> if [is valid constructor] [the specified information]; otherwise, <c>false</c>.</returns>
        internal static bool IsValidConstructor(ConstructorInfo info, params object?[] instanceParameterValues)
        {
            if (instanceParameterValues.Length == 0)
            {
                return true;
            }

            List<ParameterInfo> paramList = info.GetParameters().ToList();

            if (instanceParameterValues.Length != paramList.Count)
            {
                return false;
            }

            var isValid = true;

            for (var i = 0; i < paramList.Count; i++)
            {
                var paramType = paramList[i].ParameterType;
                var instanceType = instanceParameterValues[i]?.GetType();

                isValid &= (instanceType == null && IsNullableType(paramType)) ||
                           (instanceType != null && paramType.IsAssignableFrom(instanceType));
            }

            return isValid;
        }

        /// <summary>
        ///     Returns true if the argument list == 0 or the types match the constructor exactly.
        /// </summary>
        /// <param name="info">Parameter information.</param>
        /// <param name="instanceParameterValues">Optional arguments.</param>
        /// <returns><c>true</c> if [is valid constructor] [the specified information]; otherwise, <c>false</c>.</returns>
        internal static bool IsValidConstructorByType(ConstructorInfo info, params Type?[] instanceParameterValues)
        {
            if (instanceParameterValues.Length == 0)
            {
                return true;
            }

            List<ParameterInfo> paramList = info.GetParameters().ToList();

            if (instanceParameterValues.Length != paramList.Count)
            {
                return false;
            }

            var isValid = true;

            for (var i = 0; i < paramList.Count; i++)
            {
                var paramType = paramList[i].ParameterType;
                var instanceType = instanceParameterValues[i];

                isValid &= paramType.IsAssignableFrom(instanceType);
            }

            return isValid;
        }

        /// <summary>
        ///     Throws the already exists.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <exception cref="System.ArgumentException"></exception>
        internal static void ThrowAlreadyExists(Type type) => throw new ArgumentException($"{type} already exists.");
    }
}

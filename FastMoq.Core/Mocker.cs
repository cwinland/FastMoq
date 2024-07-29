using FastMoq.Extensions;
using FastMoq.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Language.Flow;
using Moq.Protected;
using System.Data.Common;
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
    /// <remarks>
    ///     This class is used inside <see cref="MockerTestBase{TComponent}" />.
    ///     This may be used independently; however, to ensure that mocks are not duplicated, use only one instance of this
    ///     class.
    ///     If using <see cref="MockerTestBase{TComponent}" />, use the <see cref="Mocks" /> property or create this class for
    ///     a specific test only.
    /// </remarks>
    /// <example>
    ///     <code>
    /// <![CDATA[
    /// // Arrange
    /// var id = 1234;
    /// var blogsTestData = new List<Blog> { new Blog { Id = id } };
    /// 
    /// // Create a mock DbContext
    /// var mocker = new Mocker(); // Create a new mocker only if not already using MockerTestBase; otherwise use Mocks included in the base class.
    /// 
    /// var dbContextMock = mocker.GetMockDbContext<ApplicationDbContext>();
    /// var dbContext = dbContextMock.Object;
    /// dbContext.Blogs.Add(blogsTestData); // Can also be dbContext.Set<Blog>().Add(blogsTestData)
    /// 
    /// var validator = new BtnValidator(dbContext);
    /// 
    /// // Act
    /// var result = validator.IsValid(id);
    /// 
    /// // Assert
    /// Assert.True(result);
    /// ]]></code>
    /// </example>
    /// <seealso cref="MockerHttpExtensions" />
    /// <seealso cref="MockerCreationExtensions" />
    /// <seealso cref="MockerHttpExtensions" />
    /// <seealso cref="TestClassExtensions" />
    public class Mocker
    {
        #region Fields

        public const string SETUP_ALL_PROPERTIES_METHOD_NAME = "SetupAllProperties";
        public const string SETUP = "Setup";

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
        ///     Gets the constructor history.
        /// </summary>
        /// <value>The constructor history.</value>
        public ConstructorHistory ConstructorHistory { get; } = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the mock optional.
        /// </summary>
        /// <value>The mock optional.</value>
        public bool MockOptional { get; set; } = false;

        /// <summary>
        ///     Gets the database connection. The default value is a memory Sqlite database connection unless otherwise set.
        /// </summary>
        /// <value>The database connection.</value>
        public DbConnection DbConnection { get; internal set; } = new SqliteConnection("DataSource=:memory:");

        /// <summary>
        ///     Tracks internal exceptions for debugging.
        /// </summary>
        public ObservableExceptionLog ExceptionLog => new();

        /// <summary>
        ///     The virtual mock http client that is used by mocker unless overridden with the <see cref="Strict" /> property.
        /// </summary>
        /// <value>The HTTP client.</value>
        public HttpClient HttpClient { get; }

        /// <summary>
        ///     When creating a mocks of a class, <c>true</c> indicates to recursively inject the mocks inside of that class;
        ///     otherwise properties are not auto mocked.
        /// </summary>
        /// <value>The inner mock resolution.</value>
        public bool InnerMockResolution { get; set; } = true;

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="Mocker" /> is strict.
        ///     Strict prevents certain features for automatically assuming or substituting preconfigured objects such as
        ///     IFileSystem and HttpClient.
        ///     Strict prevents private methods from being used during an invoke if public was requested. Not strict would mean
        ///     that private methods may be
        ///     substituted if the public methods are not found.
        ///     Strict prevents private constructors from being used during object creation if public was requested. Not strict
        ///     would mean that private constructors may be
        ///     substituted if the public constructors are not found.
        ///     Strict sets the <see cref="MockBehavior" /> option.
        /// </summary>
        /// <value>
        ///     <c>true</c> if strict <see cref="IFileSystem" /> and <see cref="HttpClient" /> resolution; otherwise, <c>false</c>
        ///     uses the built-in virtual
        ///     <see cref="MockFileSystem" />.
        /// </value>
        /// <remarks>
        ///     If strict, the mock <see cref="IFileSystem" /> does not use <see cref="MockFileSystem" /> and uses
        ///     <see cref="Mock" /> of <see cref="IFileSystem" />.
        ///     Gets or sets a value indicating whether this <see cref="Mocker" /> is strict. If strict, the mock
        ///     <see cref="HttpClient" /> does not use the pre-built HttpClient and uses <see cref="Mock" /> of
        ///     <see cref="HttpClient" />.
        /// </remarks>
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
            HttpClient = this.CreateHttpClient();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Mocker" /> class with a given type map (mock dependency injection).
        /// </summary>
        /// <inheritdoc />
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
        /// Adds the file io abstraction mapping.
        /// </summary>
        public void AddFileSystemAbstractionMapping()
        {
            AddType<IDirectory, DirectoryBase>()
               .AddType<IDirectoryInfo, DirectoryInfoBase>()
               .AddType<IDirectoryInfoFactory, MockDirectoryInfoFactory>()
               .AddType<IDriveInfo, DriveInfoBase>()
               .AddType<IDriveInfoFactory, MockDriveInfoFactory>()
               .AddType<IFile, FileBase>()
               .AddType<IFileInfo, FileInfoBase>()
               .AddType<IFileInfoFactory, MockFileInfoFactory>()
               .AddType<IFileStreamFactory, MockFileStreamFactory>()
               .AddType<IFileSystem, FileSystemBase>()
               .AddType<IFileSystemInfo, FileSystemInfoBase>()
               .AddType<IFileSystemWatcherFactory, MockFileSystemWatcherFactory>()
               .AddType<IPath, PathBase>();
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
        /// <param name="nonPublic">if set to <c>true</c> uses public and non-public constructors.</param>
        /// <returns><see cref="MockModel{T}" />.</returns>
        public MockModel<T> AddMock<T>(Mock<T> mock, bool overwrite, bool nonPublic = false) where T : class =>
            new(AddMock(mock, typeof(T), overwrite, nonPublic));

        /// <summary>
        ///     Adds a default mock object to the writable properties in the object, if possible.
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
        ///     Adds a default mock object to the writable properties in the object, if possible.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="obj">The object.</param>
        /// <returns>object.</returns>
        public object? AddProperties(Type type, object? obj)
        {
            ArgumentNullException.ThrowIfNull(type);

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
                    AddProperty(obj, writableProperty);
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
        ///     This is similar to dependency injection. It will resolve an interface to the specified concrete class.
        /// </summary>
        /// <param name="tInterface">The interface or class Type which can be mapped to a specific Class.</param>
        /// <param name="tClass">The Class Type (cannot be an interface) that can be created and assigned to tInterface.</param>
        /// <param name="createFunc">An optional create function used to create the class.</param>
        /// <param name="replace">Replace type if already exists. Default: false.</param>
        /// <param name="args">arguments needed in model.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public Mocker AddType(Type tInterface, Type tClass, Func<Mocker, object>? createFunc = null, bool replace = false, params object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(tClass);
            ArgumentNullException.ThrowIfNull(tInterface);

            if (tClass.IsInterface)
            {
                var message = tInterface.Name switch
                {
                    _ when tInterface.Name.Equals(tClass.Name) =>
                        $"{nameof(AddType)} does not support mapping an interface to itself. Therefore specify a concrete class or map an interface to a concrete class.",
                    _ =>
                        $"{tClass.Name} cannot be an interface. An interface must be mapped to a concrete class.",
                };

                throw new ArgumentException(message);
            }

            if (!tInterface.IsAssignableFrom(tClass))
            {
                throw new ArgumentException($"{tClass.Name} is not assignable to {tInterface.Name}.");
            }

            if (replace && typeMap.ContainsKey(tInterface))
            {
                typeMap.Remove(tInterface);
            }

            typeMap.Add(tInterface, new InstanceModel(tInterface, tClass, createFunc, args?.ToList() ?? []));

            return this;
        }

        /// <summary>
        ///     Adds an interface to Class mapping to the <see cref="typeMap" /> for easier resolution.
        ///     This is similar to dependency injection. It will resolve an interface to the specified concrete class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="createFunc">An optional create function used to create the class.</param>
        /// <param name="replace">Replace type if already exists. Default: false.</param>
        /// <param name="args">arguments needed in model.</param>
        public Mocker AddType<T>(Func<Mocker, T>? createFunc = null, bool replace = false, params object?[]? args) where T : class =>
            AddType<T, T>(createFunc, replace, args);

        /// <summary>
        ///     Adds an interface to Class mapping to the <see cref="typeMap" /> for easier resolution.
        ///     This is similar to dependency injection. It will resolve an interface to the specified concrete class.
        /// </summary>
        /// <typeparam name="TInterface">The interface or class Type which can be mapped to a specific Class.</typeparam>
        /// <typeparam name="TClass">The Class Type (cannot be an interface) that can be created and assigned to TInterface /&gt;.</typeparam>
        /// <param name="createFunc">An optional create function used to create the class.</param>
        /// <param name="replace">Replace type if already exists. Default: false.</param>
        /// <param name="args">arguments needed in model.</param>
        /// <exception cref="ArgumentException">$"{typeof(TClass).Name} cannot be an interface."</exception>
        /// <exception cref="ArgumentException">$"{typeof(TClass).Name} is not assignable to {typeof(TInterface).Name}."</exception>
        public Mocker AddType<TInterface, TClass>(Func<Mocker, TClass>? createFunc = null, bool replace = false, params object?[]? args)
            where TInterface : class where TClass : class => AddType(typeof(TInterface), typeof(TClass), createFunc, replace, args);

        /// <summary>
        ///     Creates an instance of <c>T</c>. Parameter data allows matching of constructors by type and uses those values in
        ///     the creation of the instance.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="args">The optional arguments used to create the instance.</param>
        /// <returns><see cref="Nullable{T}" />.</returns>
        /// <example>
        ///     <code><![CDATA[
        /// IFileSystem fileSystem = CreateInstance<IFileSystem>();
        /// ]]></code>
        /// </example>
        /// <seealso cref="MockerCreationExtensions" />
        public T? CreateInstance<T>(params object?[] args) where T : class => CreateInstance<T>(true, args);

        /// <summary>
        ///     Creates an instance of <see cref="IFileSystem" />.
        /// </summary>
        /// <typeparam name="T"><see cref="IFileSystem" />.</typeparam>
        /// <param name="usePredefinedFileSystem">if set to <c>true</c> [use predefined file system].</param>
        /// <returns><see cref="Nullable{IFileSystem}" />.</returns>
        public IFileSystem? CreateInstance<T>(bool usePredefinedFileSystem) where T : class, IFileSystem =>
            CreateInstance<T>(usePredefinedFileSystem, Array.Empty<object?>());

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="usePredefinedFileSystem">if set to <c>true</c> [use predefined file system].</param>
        /// <param name="args">The arguments.</param>
        /// <returns><see cref="Nullable{T}" />.</returns>
        public T? CreateInstance<T>(bool usePredefinedFileSystem, params object?[] args) where T : class
        {
            if (usePredefinedFileSystem && fileSystem is T mockFileSystem)
            {
                return mockFileSystem;
            }

            var tType = typeof(T);
            var typeInstanceModel = GetTypeModel(tType);

            if (TryGetExistingObject(tType, typeInstanceModel, out T? instance))
            {
                return instance;
            }

            args.RaiseIfNull();

            if (typeInstanceModel.Arguments.Count > 0 && args.Length == 0)
            {
                args = typeInstanceModel.Arguments.ToArray();
            }

            var instanceType = typeInstanceModel.InstanceType;

            var constructor = GetConstructorByArgs(args, instanceType, false);

            return this.CreateInstanceInternal<T>(constructor);
        }

        private ConstructorModel GetConstructorByArgs(object?[] args, Type instanceType, bool nonPublic)
        {
            var constructor =
                args.Length > 0
                    ? FindConstructor(instanceType, nonPublic, args)
                    : FindConstructor(false, instanceType, nonPublic);

            return constructor;
        }

        private bool TryGetExistingObject<T>(Type tType, IInstanceModel typeInstanceModel, out T? instance) where T : class
        {
            instance = default;

            if (!creatingTypeList.Contains(tType))
            {
                if (TryGetModelInstance(typeInstanceModel, tType, out var modelInstance))
                {
                    {
                        instance = (T?) modelInstance;
                        return true;
                    }
                }

                // Special handling for DbContext types.
                if (tType.IsAssignableTo(typeof(DbContext)))
                {
                    var mockObj = GetMockDbContext(tType);

                    {
                        instance = (T?) mockObj.Object;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryGetModelInstance(IInstanceModel typeInstanceModel, Type tType, out object? instance)
        {
            if (typeInstanceModel.CreateFunc != null)
            {
                creatingTypeList.Add(tType);
                object obj;

                try
                {
                    ConstructorHistory.AddOrUpdate(tType, typeInstanceModel);
                    obj = typeInstanceModel.CreateFunc(this);
                }
                finally
                {
                    creatingTypeList.Remove(tType);
                }

                {
                    instance = obj;
                    return true;
                }
            }

            instance = null;
            return false;
        }

        /// <summary>
        ///     Creates instance by type args.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args">The type arguments.</param>
        /// <returns></returns>
        public T? CreateInstanceByType<T>(params Type?[] args) where T : class
        {
            var tType = typeof(T);
            var constructor = FindConstructorByType(tType, true, args);

            return this.CreateInstanceInternal<T>(constructor);
        }

        /// <summary>
        ///     Creates the instance of the given type.
        ///     Public and non-public constructors are searched.
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
                ? (T) type.CreateFunc(this)
                : CreateInstanceNonPublic(type.InstanceType, args) as T;
        }

        /// <summary>
        ///     Creates the instance of the given type.
        ///     Public and non-public constructors are searched.
        ///     Parameters allow matching of constructors and using those values in the creation of the instance.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>System.Nullable&lt;System.Object&gt;.</returns>
        public object? CreateInstanceNonPublic(Type type, params object?[] args)
        {
            var constructor = GetConstructorByArgs(args, type, true);
            return constructor.ConstructorInfo?.Invoke(constructor.ParameterList);
        }

        /// <summary>
        ///     Creates the <see cref="MockModel" /> from the <c>Type</c>. This throws an exception if the mock already exists.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic"><c>true</c> if non-public and public constructors are used.</param>
        /// <param name="args">The arguments used to match to the constructor.</param>
        /// <returns><see cref="List{Mock}" />.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ApplicationException">Cannot create instance.</exception>
        public List<MockModel> CreateMock(Type type, bool nonPublic = false, params object?[] args)
        {
            type = CleanType(type);

            if (this.Contains(type))
            {
                type.ThrowAlreadyExists();
            }

            var oMock = CreateMockInstance(type, nonPublic, args);

            AddMock(oMock, type);
            return mockCollection;
        }

        /// <summary>
        ///     Creates the <see cref="MockModel" /> from the type <c>T</c>. This throws an exception if the mock already exists.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="nonPublic">if set to <c>true</c> public and non-public constructors are used.</param>
        /// <param name="args">The arguments used to find the correct constructor for a class.</param>
        /// <returns><see cref="List{T}" />.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ArgumentException">type already exists. - type</exception>
        /// <exception cref="System.ApplicationException">Cannot create instance.</exception>
        public List<MockModel> CreateMock<T>(bool nonPublic = false, params object?[] args) where T : class => CreateMock(typeof(T), nonPublic, args);

        /// <summary>
        ///     Creates the mock instance that is not automatically injected.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nonPublic">if set to <c>true</c> can use non-public constructor.</param>
        /// <param name="args">The arguments used to find the correct constructor for a class.</param>
        /// <returns>Mock.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ApplicationException">Cannot create instance.</exception>
        public Mock<T> CreateMockInstance<T>(bool nonPublic = false, params object?[] args) where T : class =>
            (Mock<T>) CreateMockInstance(typeof(T), nonPublic, args);

        /// <summary>
        ///     Creates an instance of the mock.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic">if set to <c>true</c> can use non-public constructor.</param>
        /// <param name="args">The arguments used to find the correct constructor for a class.</param>
        /// <returns>Mock.</returns>
        /// <exception cref="System.ArgumentException">type must be a class or interface. - type.</exception>
        /// <exception cref="ApplicationException">Type must be a class or interface. - type.</exception>
        /// <exception cref="System.ApplicationException">Type must be a class or interface., nameof(type)</exception>
        public Mock CreateMockInstance(Type type, bool nonPublic = false, params object?[] args)
        {
            if (type == null || (!type.IsClass && !type.IsInterface))
            {
                throw new ArgumentException("Type must be a class or interface.", nameof(type));
            }

            var constructor = this.GetTypeConstructor(type, nonPublic, args);

            var oMock = this.CreateMockInternal(type, constructor.ParameterList, nonPublic);

            SetupMock(type, oMock);
            oMock.RaiseIfNull();
            return oMock;
        }

        /// <summary>
        ///     Gets the argument data for the given type T.
        ///     Public and non-public constructors are used.
        ///     A data map is used to fill in the data used in the constructor by the parameter type.
        ///     If the data is missing for a given parameter type, Mocks or default values are used.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data map used when generating the object argument data for a constructor.</param>
        /// <returns>System.Nullable&lt;System.Object&gt;[].</returns>
        public object?[] GetArgData<T>(Dictionary<Type, object?>? data = null) where T : class
        {
            var type = typeof(T).IsInterface ? GetTypeFromInterface<T>() : new InstanceModel<T>();

            data ??= new();
            var constructor = FindConstructor(false, type.InstanceType, true);
            return this.GetArgData(constructor.ConstructorInfo, data);
        }

        /// <summary>
        ///     Gets the database context.
        /// </summary>
        /// <typeparam name="TContext">The type of the t context.</typeparam>
        /// <param name="options">The options.</param>
        /// <param name="connection">The connection.</param>
        /// <returns>TContext.</returns>
        public TContext GetDbContext<TContext>(DbContextOptions<TContext>? options = null, DbConnection? connection = null)
            where TContext : DbContext =>
            GetDbContext(contextOptions =>
                {
                    AddType(_ => contextOptions, true);
                    var newInstance = CreateInstanceNonPublic<TContext>() ?? throw new InvalidOperationException("Unable to create DbContext.");

                    AddType(_ => newInstance, true);

                    return newInstance;
                },
                options,
                connection
            );

        /// <summary>
        ///     Gets the database context.
        /// </summary>
        /// <typeparam name="TContext">The type of the t context.</typeparam>
        /// <param name="newObjectFunc">The new object function.</param>
        /// <returns>TContext.</returns>
        public TContext GetDbContext<TContext>(Func<DbContextOptions, TContext> newObjectFunc) where TContext : DbContext
        {
            DbConnection = new SqliteConnection("DataSource=:memory:");
            DbConnection.Open();

            var dbContextOptions = new DbContextOptionsBuilder<TContext>()
                .UseSqlite(DbConnection)
                .Options;

            var context = newObjectFunc(dbContextOptions);
            context.Database.EnsureCreated();
            context.SaveChanges();

            return context;
        }

        /// <summary>
        ///     Gets the database context using a SqlLite DB or provided options and DbConnection.
        /// </summary>
        /// <typeparam name="TContext">The type of the t context.</typeparam>
        /// <param name="newObjectFunc">The new object function.</param>
        /// <param name="options">The options.</param>
        /// <param name="connection">The connection.</param>
        /// <returns>TContext.</returns>
        public TContext GetDbContext<TContext>(Func<DbContextOptions<TContext>, TContext> newObjectFunc, DbContextOptions<TContext>? options,
            DbConnection? connection) where TContext : DbContext
        {
            DbConnection = connection ?? new SqliteConnection("DataSource=:memory:");
            DbConnection.Open();

            var dbContextOptions = options ??
                                   new DbContextOptionsBuilder<TContext>()
                                       .UseSqlite(DbConnection)
                                       .Options;

            var context = newObjectFunc(dbContextOptions);
            context.Database.EnsureCreated();
            context.SaveChanges();

            return context;
        }

        /// <summary>
        ///     Gets the IFileSystem used in this context.
        ///     Generally, this means the Mocker.fileSystem property; unless strict is turned on, then it is a Mock.
        /// </summary>
        /// <returns></returns>
        public IFileSystem GetFileSystem(Action<IFileSystem>? action = null)
        {
            var fs = GetObject<IFileSystem>() ?? fileSystem;
            action?.Invoke(fs);
            return fs;
        }

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
            func == null ? new() : GetList(count, _ => func.Invoke());

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
        /// <exception cref="System.ArgumentNullException">method</exception>
        public object?[] GetMethodArgData(MethodInfo method, List<KeyValuePair<Type, object?>>? data = null)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            var args = new List<object?>();

            method.GetParameters().ToList().ForEach(p =>
                args.Add(data?.Any(x => x.Key == p.ParameterType) ?? false
                    ? data.First(x => x.Key == p.ParameterType).Value
                    : GetParameter(p.ParameterType)
                )
            );

            return args.ToArray();
        }

        /// <summary>
        ///     Gets the method default data.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>object?[].</returns>
        /// <exception cref="System.ArgumentNullException">method</exception>
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
        ///     Gets the mock and allows an action against the mock.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mockAction">The mock action.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>Mock&lt;T&gt; of the mock.</returns>
        public Mock<T> GetMock<T>(Action<Mock<T>> mockAction, params object?[] args) where T : class
        {
            ArgumentNullException.ThrowIfNull(mockAction);

            var mock = GetMock<T>(args);
            mockAction.Invoke(mock);

            return mock;
        }

        /// <summary>
        ///     Gets the mock and allows an action against the mock.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mockFunc">The mock action.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>Mock&lt;T&gt; of the mock.</returns>
        public Task GetMockAsync<T>(Func<Mock<T>, Task> mockFunc, params object?[] args) where T : class
        {
            ArgumentNullException.ThrowIfNull(mockFunc);

            var mock = GetMock<T>(args);
            return mockFunc.Invoke(mock);
        }

        /// <summary>
        ///     Gets of creates the <see cref="Mock"/> of <c>type</c>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="args">The arguments used to find the correct constructor for a class.</param>
        /// <returns><see cref="Mock" />.</returns>
        public Mock GetMock(Type type, params object?[] args)
        {
            type = CleanType(type);

            if (!this.Contains(type))
            {
                CreateMock(type, args?.Length > 0, args ?? Array.Empty<object?>());
            }

            return GetRequiredMock(type);
        }

        /// <summary>
        ///     Gets the mock database context.
        /// </summary>
        /// <param name="contextType">Type of the context.</param>
        /// <returns>Mock of the mock database context.</returns>
        /// <exception cref="System.InvalidOperationException">Unable to get MockDb. Try GetDbContext to use internal database.</exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="MissingMethodException">GetMockDbContext</exception>
        public Mock GetMockDbContext(Type contextType) => contextType.CallGenericMethod(this) as Mock ??
                                                          throw new InvalidOperationException(
                                                              "Unable to get MockDb. Try GetDbContext to use internal database."
                                                          );

        /// <summary>
        ///     Gets the mock database context.
        /// </summary>
        /// <typeparam name="TDbContext">The type of the t database context.</typeparam>
        /// <returns>Mock&lt;TDbContext&gt; of the mock database context.</returns>
        public DbContextMock<TDbContext> GetMockDbContext<TDbContext>() where TDbContext : DbContext
        {
            if (this.Contains<TDbContext>())
            {
                return (DbContextMock<TDbContext>) GetMock<TDbContext>();
            }

            // Add DbContextOptions wrapper to mock DbContextOptions.
            if (!this.Contains<DbContextOptions<TDbContext>>())
            {
                AddMock(new MockDbContextOptions<TDbContext>(), false);
            }

            var mock = (DbContextMock<TDbContext>) GetProtectedMock<TDbContext>();

            return mock.SetupDbSets(this);
        }

        /// <summary>
        ///     Gets the instance for the given <see cref="ParameterInfo" />.
        /// </summary>
        /// <param name="info">The <see cref="ParameterInfo" />.</param>
        /// <returns>
        ///     <see cref="Nullable{Object}" />
        /// </returns>
        /// <exception cref="System.ArgumentNullException">info</exception>
        /// <exception cref="System.InvalidProgramException">info</exception>
        public object? GetObject(ParameterInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            try
            {
                return !MockOptional && info.IsOptional ? null : GetParameter(info.ParameterType);
            }
            catch (FileNotFoundException ex)
            {
                ExceptionLog.Add(ex.Message);
                throw;
            }
            catch (AmbiguousImplementationException ex)
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
                if (!this.Contains<IFileSystem>() && type.IsEquivalentTo(typeof(IFileSystem)))
                {
                    return fileSystem;
                }

                if (!this.Contains<HttpClient>() && type.IsEquivalentTo(typeof(HttpClient)))
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

                var mockObject = this.GetSafeMockObject(mock);
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
            return this.CreateInstanceInternal<T>(constructor.ConstructorInfo, args);
        }

        /// <summary>
        ///     Gets the protected mock.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args">The arguments.</param>
        /// <returns>Mock of the protected mock.</returns>
        public Mock GetProtectedMock<T>(params object?[] args) => GetProtectedMock(typeof(T), args);

        /// <summary>
        ///     Gets the protected mock.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>Mock of the protected mock.</returns>
        public Mock GetProtectedMock(Type type, params object?[] args)
        {
            type = CleanType(type);

            if (!this.Contains(type))
            {
                CreateMock(type, true, args);
            }

            return GetRequiredMock(type);
        }

        /// <summary>
        ///     Gets the required mock that already exists. If it doesn't exist, an error is raised.
        /// </summary>
        /// <param name="type">The mock type, usually an interface.</param>
        /// <returns>Mock.</returns>
        /// <exception cref="ArgumentNullException">Type cannot be null.</exception>
        /// <exception cref="System.ArgumentException">Type must be a class.</exception>
        /// <exception cref="System.InvalidOperationException">Type must be a class. - type</exception>
        /// <exception cref="InvalidOperationException">Not Found.</exception>
        public Mock GetRequiredMock(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            var mock = (!type.IsClass && !type.IsInterface)
                ? throw new ArgumentException("Type must be a class.", nameof(type))
                : mockCollection.First(x => x.Type == type).Mock;

            mock.RaiseIfNull();

            return mock;
        }

        /// <summary>
        ///     Gets the required mock.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <returns><see cref="Mock{T}" />.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.InvalidOperationException">Mock must exist. - type</exception>
        public Mock<T> GetRequiredMock<T>() where T : class => (Mock<T>) GetRequiredMock(typeof(T));

        /// <summary>
        ///     Gets the required object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>T of the required object.</returns>
        public T GetRequiredObject<T>() where T : class
        {
            var obj = GetObject<T>();

            obj.RaiseIfNull();

            return obj;
        }

        /// <summary>
        ///     Gets the content of the string.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>string.</returns>
        public async Task<string> GetStringContent(HttpContent content) =>
            content is ByteArrayContent data ? await data.ReadAsStringAsync() : string.Empty;

        /// <summary>
        ///     Determines whether [has parameterless constructor] [the specified type].
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic">The non-public.</param>
        /// <returns>Has the parameterless constructor.</returns>
        public bool HasParameterlessConstructor(Type type, bool nonPublic = false) =>
            GetConstructors(type, nonPublic).Find(x => x.ParameterList.Length == 0) != null;

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
        /// mock.Setup(x => x.StartCar).Returns(true));
        /// mock.Setup(x => x.StopCar).Returns(false));
        /// }
        /// ]]></code>
        /// </example>
        public Mock<T> Initialize<T>(Action<Mock<T>> action, bool reset = true) where T : class
        {
            ArgumentNullException.ThrowIfNull(action);

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
        /// <param name="nonPublic">if set to <c>true</c> [non-public].</param>
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
        /// <param name="nonPublic">if set to <c>true</c> [non-public].</param>
        /// <param name="args">The arguments used for the method.</param>
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

            return method switch
            {
                null when !nonPublic && !Strict => InvokeMethod(obj, methodName, true, args),
                null => throw new ArgumentOutOfRangeException(nameof(methodName)),
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
            var mockModel = mockCollection.Find(x => x.Type == typeof(T) && x.Mock == mock);

            return mockModel != null && mockCollection.Remove(mockModel);
        }

        /// <summary>
        ///     Add specified Mock. Internal API only.
        /// </summary>
        /// <param name="mock">Mock to Add.</param>
        /// <param name="type">Type of Mock.</param>
        /// <param name="overwrite">
        ///     Overwrite if the mock exists or throw <see cref="ArgumentException" /> if this parameter is
        ///     false.
        /// </param>
        /// <param name="nonPublic">if set to <c>true</c> [non-public].</param>
        /// <returns><see cref="Mock{T}" />.</returns>
        /// <exception cref="ArgumentNullException">nameof(mock)</exception>
        /// <exception cref="ArgumentNullException">nameof(type)</exception>
        /// <exception cref="System.ArgumentNullException">nameof(mock)</exception>
        /// <exception cref="System.ArgumentNullException">nameof(type)</exception>
        internal MockModel AddMock(Mock mock, Type type, bool overwrite = false, bool nonPublic = false)
        {
            ArgumentNullException.ThrowIfNull(mock);
            ArgumentNullException.ThrowIfNull(type);

            if (this.Contains(type))
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
        ///     Builds IsAny expression for the Type T.
        /// </summary>
        /// <typeparam name="T">Type which to build IsAny expression.</typeparam>
        /// <returns>The expression.</returns>
        public static Expression<Func<T, bool>> BuildExpression<T>() => It.IsAny<Expression<Func<T, bool>>>();

        /// <summary>
        ///     Calls the method without needing to specify the parameters.Parameters can be specified if particular values are required.
        /// </summary>
        /// <example>
        /// This example shows different ways to call the method. The method can be called with or without parameters.
        /// All parameters are not required, but the order does matter.
        /// The generic "T" type is the return value type expected from the method.
        /// <code>
        /// <![CDATA[
        /// object[] a = Mocks.CallMethod<object[]>(CallTestMethod);
        /// object[] b = Mocks.CallMethod<object[]>(CallTestMethod, 4);
        /// object[] c = Mocks.CallMethod<object?[]>(CallTestMethod, 4, Mocks.fileSystem);
        /// string d = Mocks.CallMethodS<string>(CallStringMethod);
        /// int e = Mocks.CallMethodI<int>(CallStringMethod);
        /// ]]></code>
        /// </example>
        /// <typeparam name="T">Return value type.</typeparam>
        /// <param name="method">The method.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>Returns value of the called method.</returns>
        /// <exception cref="System.ArgumentNullException" />
        public T? CallMethod<T>(Delegate method, params object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(method);

            var parameters = GetMethodArgData(method.GetMethodInfo(), CreateArgPairList(method.GetMethodInfo(), args));
            try
            {
                return (T?) method.DynamicInvoke(parameters);
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException te && te.InnerException is { } e)
                {
                    throw e;
                }

                throw;
            }
        }

        /// <summary>
        ///     Calls the method without needing to specify the parameters.Parameters can be specified if particular values are required.
        /// </summary>
        /// <example>
        /// This example shows different ways to call the method. The method can be called with or without parameters.
        /// All parameters are not required, but the order does matter.
        /// <code>
        /// <![CDATA[
        /// Mocks.CallMethod(CallTestMethod);
        /// Mocks.CallMethod(CallTestMethod, 4);
        /// Mocks.CallMethod(CallTestMethod, 4, Mocks.fileSystem);
        /// ]]></code>
        /// </example>
        /// <param name="method">The method.</param>
        /// <param name="args">The arguments.</param>
        /// <exception cref="System.ArgumentNullException" />
        public void CallMethod(Delegate method, params object?[]? args) => CallMethod<object>(method, args);

        internal void AddProperty(object? obj, PropertyInfo writableProperty)
        {
            try
            {
                if (writableProperty.GetValue(obj) is null && !creatingTypeList.Contains(writableProperty.PropertyType))
                {
                    writableProperty.SetValue(obj, GetObject(writableProperty.PropertyType));
                }
            }
            catch (Exception ex)
            {
                ExceptionLog.Add(ex.Message);
            }
        }

        internal List<KeyValuePair<Type, object?>> CreateArgPairList(MethodBase info, params object?[]? args)
        {
            var paramList = info?.GetParameters().ToList() ?? new();
            var newArgs = new List<KeyValuePair<Type, object?>>();
            args ??= [];

            for (var i = 0; i < paramList.Count; i++)
            {
                var p = paramList[i];

                var val = i switch
                {
                    _ when i < args.Length => args[i],
                    _ when p.IsOptional => null,
                    _ => GetParameter(p.ParameterType),
                };

                newArgs.Add(new (p.ParameterType, val));
            }

            return newArgs;
        }

        /// <summary>
        ///     Ensure Type is correct.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>Type.</returns>
        internal static Type CleanType(Type type) => type.Name.EndsWith('&')
            ? type.Assembly.GetTypes().FirstOrDefault(x => x.Name.Equals(type.Name.TrimEnd('&'), StringComparison.Ordinal)) ?? type
            : type;

        /// <summary>
        ///     Finds the constructor matching args EXACTLY by type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic">if set to <c>true</c> [non-public].</param>
        /// <param name="args">The arguments.</param>
        /// <returns>ConstructorModel.</returns>
        /// <exception cref="System.NotImplementedException">Unable to find the constructor.</exception>
        internal ConstructorModel FindConstructor(Type type, bool nonPublic, params object?[] args)
        {
            // Get all constructors
            var allConstructors = GetConstructors(type, nonPublic, args);

            // Filter constructors
            var constructors = allConstructors
                .Where(x => x.ParameterList
                    .Select(z => z?.GetType())
                    .SequenceEqual(args.Select(y => y?.GetType()))
                )
                .ToList();

            return constructors.Any() switch
            {
                // Since we looked for only public, see if there is a protected/private/internal constructor.
                false when !nonPublic && !Strict => FindConstructor(type, true, args),
                false => throw new NotImplementedException("Unable to find the constructor."),
                _ => constructors.FirstOrDefault(x => x.ParameterList.Length == args.Length) ?? constructors[0],
            };
        }

        /// <summary>
        ///     Finds the constructor.
        /// </summary>
        /// <param name="bestGuess">if set to <c>true</c> [best guess].</param>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic">if set to <c>true</c> [non-public].</param>
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

            var constructors = GetConstructors(type, nonPublic)
                .Where(x => excludeList.TrueForAll(y => y != x.ConstructorInfo)).ToList();

            // if it is not best guess, then we can't know which constructor.
            if (!bestGuess && constructors.Count(x => x.ParameterList.Length > 0) > 1)
            {
                throw this.GetAmbiguousConstructorImplementationException(type);
            }

            // Best Guess //
            // Didn't find anything, should look for public version if it was not public.
            if (!(constructors.Count > 0) && !nonPublic && !Strict)
            {
                return FindConstructor(bestGuess, type, true, excludeList);
            }

            var validConstructors = this.GetTestedConstructors(type, constructors);

            return validConstructors.Count > 0 ? validConstructors.Last() : throw new NotImplementedException("Unable to find the constructor.");
        }

        /// <summary>
        ///     Finds the type of the constructor by.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic">if set to <c>true</c> [non-public].</param>
        /// <param name="args">The arguments.</param>
        /// <returns>ConstructorInfo.</returns>
        /// <exception cref="System.NotImplementedException">Unable to find the constructor.</exception>
        internal ConstructorInfo FindConstructorByType(Type type, bool nonPublic, params Type?[] args)
        {
            var constructors = GetConstructorsByType(nonPublic, type, args);

            return (constructors.Count > 0) switch
            {
                false when !nonPublic && !Strict => FindConstructorByType(type, true, args),
                false => throw new NotImplementedException("Unable to find the constructor."),
                _ => constructors[0],
            };
        }

        /// <summary>
        ///     Gets the constructors.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic">The non-public.</param>
        /// <param name="instanceParameterValues">The instance parameter values.</param>
        /// <returns><see cref="List{ConstructorModel}" />.</returns>
        internal List<ConstructorModel> GetConstructors(Type type, bool nonPublic, params object?[] instanceParameterValues)
        {
            var flags = nonPublic
                ? BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public
                : BindingFlags.Instance | BindingFlags.Public;

            var constructors = type
                .GetConstructors(flags)
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
        ///     Gets the constructors non-public.
        /// </summary>
        /// <param name="nonPublic">Include non-public constructors.</param>
        /// <param name="type">The type.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <returns><see cref="List{ConstructorInfo}" />.</returns>
        internal static List<ConstructorInfo> GetConstructorsByType(bool nonPublic, Type type, params Type?[] parameterTypes) =>
            type
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.IsValidConstructorByType(parameterTypes) && (nonPublic || x.IsPublic))
                .OrderBy(x => x.GetParameters().Length)
                .ToList();

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
            var first = mockCollection.Find(x => x.Type == type && (x.Mock == mock || mock == null));

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
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <returns>InstanceModel.</returns>
        /// <exception cref="System.Runtime.AmbiguousImplementationException"></exception>
        /// <exception cref="System.NotImplementedException"></exception>
        internal IInstanceModel GetTypeFromInterface<T>() where T : class
        {
            var tType = typeof(T);
            var newType = this.GetTypeFromInterface(tType);
            var model = new InstanceModel(tType, newType);

            return model;
        }

        /// <summary>
        ///     Gets the type model from the type map or create a model if it does not exist.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>FastMoq.Models.IInstanceModel.</returns>
        internal IInstanceModel GetTypeModel(Type type) =>
            typeMap.TryGetValue(type, out var model) && model is not null ? model : new InstanceModel(type, this.GetTypeFromInterface(type));

        internal void SetupMock(Type type, Mock oMock)
        {
            if (!Strict)
            {
                if (oMock.Setups.Count == 0)
                {
                    // Only run this if there are no setups.
                    InvokeMethod<Mock>(null, SETUP_ALL_PROPERTIES_METHOD_NAME, true, oMock);
                }

                if (InnerMockResolution)
                {
                    AddProperties(type, this.GetSafeMockObject(oMock));
                }
            }

            AddInjections(this.GetSafeMockObject(oMock), GetTypeModel(type)?.InstanceType ?? type);
        }
    }
}

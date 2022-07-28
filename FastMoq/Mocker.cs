using Moq;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
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
        protected readonly List<MockModel> mockCollection;

        /// <summary>
        ///     <see cref="Dictionary{TKey,TValue}" /> of <see cref="Type" /> mapped to <see cref="InstanceModel" />.
        /// This map assists in resolution of interfaces to instances.
        /// </summary>
        /// <value>The type map.</value>
        internal readonly Dictionary<Type, InstanceModel> typeMap;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="Mocker" /> is strict. If strict, the mock <see cref="IFileSystem" /> does
        /// not use <see cref="MockFileSystem" /> and uses <see cref="Mock" /> of <see cref="IFileSystem" />.
        /// </summary>
        /// <value><c>true</c> if strict <see cref="IFileSystem" /> resolution; otherwise, <c>false</c> uses the built-in virtual <see cref="MockFileSystem" />.</value>
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
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="Mocker" /> class using the specific typeMap.
        ///     The typeMap assists in resolution of interfaces to instances.
        /// </summary>
        /// <param name="typeMap">The type map.</param>
        public Mocker(Dictionary<Type, InstanceModel> typeMap) : this() => this.typeMap = typeMap;

        /// <summary>
        ///     Creates a <see cref="MockModel" /> with the given <see cref="Mock" /> with the option of overwriting an existing <see cref="MockModel" />
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
        ///     Adds an interface to Class mapping to the <see cref="typeMap" /> for easier resolution.
        /// </summary>
        /// <typeparam name="TInterface">The interface Type which can be mapped to a specific Class.</typeparam>
        /// <typeparam name="TClass">The Class Type (cannot be an interface) that can be created from <see cref="TInterface" />.</typeparam>
        /// <param name="createFunc">An optional create function used to create the class.</param>
        /// <exception cref="System.ArgumentException">Must be different types.</exception>
        /// <exception cref="System.ArgumentException"></exception>
        public void AddType<TInterface, TClass>(Func<Mocker, TClass>? createFunc = null)
            where TInterface : class where TClass : class, new()
        {
            if (typeof(TInterface) == typeof(TClass))
            {
                throw new ArgumentException("Must be different types.");
            }

            if (!typeof(TInterface).IsInterface)
            {
                throw new ArgumentException($"{typeof(TInterface).Name} must be an interface.");
            }

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
        public bool Contains(Type type) => type == null ? throw new ArgumentNullException(nameof(type)) :
            !type.IsClass && !type.IsInterface ? throw new ArgumentException("type must be a class.", nameof(type)) :
            mockCollection.Any(x => x.Type == type);

        /// <summary>
        ///     Creates an instance of <c>T</c>. Parameters allow matching of constructors and using those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="args">The optional arguments used to create the instance.</param>
        /// <example>
        /// <code><![CDATA[
        /// IFileSystem fileSystem = CreateInstance<IFileSystem>();
        /// ]]>
        /// </code>
        /// </example>
        /// <returns><see cref="Nullable{T}" />.</returns>
        public T? CreateInstance<T>(params object[] args) where T : class => CreateInstance<T>(true, args);

        /// <summary>
        ///     Creates an instance of <see cref="IFileSystem" />.
        /// </summary>
        /// <typeparam name="T"><see cref="IFileSystem" />.</typeparam>
        /// <param name="usePredefinedFileSystem">if set to <c>true</c> [use predefined file system].</param>
        /// <returns><see cref="Nullable{IFileSystem}" />.</returns>
        public IFileSystem? CreateInstance<T>(bool usePredefinedFileSystem) where T : class, IFileSystem =>
            CreateInstance<T>(usePredefinedFileSystem, Array.Empty<object>());

        /// <summary>
        ///     Creates an instance of <c>T</c>.
        /// Non public constructors are included as options for creating the instance.
        /// Parameters allow matching of constructors and using those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="args">The arguments.</param>
        /// <example>
        /// <code><![CDATA[
        /// IModel model = CreateInstanceNonPublic<IModel>();
        /// ]]>
        /// </code>
        /// </example>
        /// <returns><see cref="Nullable{T}" /></returns>
        public T? CreateInstanceNonPublic<T>(params object[] args) where T : class
        {
            var type = typeof(T).IsInterface ? GetTypeFromInterface<T>() : new InstanceModel<T>();

            if (type.CreateFunc != null)
            {
                return (T) type.CreateFunc.Invoke(this);
            }

            var constructor =
                args.Length > 0
                    ? FindConstructor(type.InstanceType, true, args)
                    : FindConstructor(false, type.InstanceType, true);

            return (T) constructor.ConstructorInfo.Invoke(constructor.ParameterList);
        }

        /// <summary>
        ///     Creates the <see cref="MockModel" /> from the <c>Type</c>. This throws an exception if the mock already exists.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic"><c>true</c> if non public and public constructors are used.</param>
        /// <returns><see cref="List{Mock}" />.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ArgumentException">type already exists. - type</exception>
        /// <exception cref="System.ApplicationException">Cannot create instance.</exception>
        public List<MockModel> CreateMock(Type type, bool nonPublic = false)
        {
            if (type == null || (!type.IsClass && !type.IsInterface))
            {
                throw new ArgumentException("type must be a class.", nameof(type));
            }

            if (Contains(type))
            {
                ThrowAlreadyExists(type);
            }

            var newType = typeof(Mock<>).MakeGenericType(type);

            if (Activator.CreateInstance(newType, nonPublic) is not Mock oMock)
            {
                throw new ApplicationException("Cannot create instance.");
            }

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
        ///     Gets a list with the specified number of list items, using a custom function.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="count">The number of list items.</param>
        /// <param name="func">The function for creating the list items.</param>
        /// <example>
        /// Example of how to create a list.
        /// <code><![CDATA[
        /// GetList<Model>(3, () => new Model(name: Guid.NewGuid().ToString()));
        /// ]]>
        /// </code>
        /// or
        /// <code><![CDATA[
        /// GetList<IModel>(3, () => Mocks.GetObject<IModel>());
        /// ]]>
        /// </code>
        /// </example>
        /// <returns><see cref="List{T}" />.</returns>
        public static List<T> GetList<T>(int count, Func<T>? func)
        {
            var results = new List<T>();

            if (func != null)
            {
                for (var i = 0; i < count; i++)
                {
                    results.Add(func.Invoke());
                }
            }

            return results;
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
        /// <returns><see cref="Mock"/>.</returns>
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
        /// <param name="info">The <see cref="ParameterInfo"/>.</param>
        /// <returns><see cref="Nullable{Object}" /></returns>
        /// <exception cref="System.ArgumentNullException">type</exception>
        /// <exception cref="System.InvalidProgramException">Unable to get the Mock.</exception>
        public object? GetObject(ParameterInfo info)
        {
            try
            {
                return GetObject(info.ParameterType);
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
        /// <returns><see cref="Nullable{Object}" />.</returns>
        /// <exception cref="System.ArgumentNullException">type</exception>
        /// <exception cref="System.InvalidProgramException">Unable to get the Mock.</exception>
        public object? GetObject(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!Strict && type.IsEquivalentTo(typeof(IFileSystem)))
            {
                return fileSystem;
            }

            var mock = GetMock(type) ?? throw new InvalidProgramException("Unable to get the Mock.");

            if (!type.IsInterface)
            {
                mock.CallBase = true;
            }

            return mock.Object;
        }

        /// <summary>
        ///     Gets the instance for the given <c>T</c>.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <returns><c>T</c>.</returns>
        public T? GetObject<T>() where T : class => GetObject(typeof(T)) as T;

        /// <summary>
        ///     Gets the required mock.
        /// </summary>
        /// <param name="type">The mock type, usually an interface.</param>
        /// <returns>Mock.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.InvalidOperationException">Mock must exist. - type</exception>
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
        ///     Gets or Creates then Initializes the specified Mock of <c>T</c>.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="action">The action.</param>
        /// <param name="reset"><c>False to keep the existing setup.</c></param>
        /// <returns><see cref="Mock{T}" /></returns>
        /// <example>
        /// Example of how to set up for mocks that require specific functionality.
        /// <code><![CDATA[
        /// mocks.Initialize<ICarService>(mock => {
        ///     mock.Setup(x => x.StartCar).Returns(true));
        ///     mock.Setup(x => x.StopCar).Returns(false));
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <exception cref="System.InvalidOperationException">Invalid Mock.</exception>
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
        ///     Add specified Mock. Internal API only.
        /// </summary>
        /// <param name="mock">Mock to Add.</param>
        /// <param name="type">Type of Mock.</param>
        /// <param name="overwrite">Overwrite if the mock exists or throw <see cref="ArgumentException" /> if this parameter is
        /// false.</param>
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

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="usePredefinedFileSystem">if set to <c>true</c> [use predefined file system].</param>
        /// <param name="args">The arguments.</param>
        /// <returns><see cref="Nullable{T}" />.</returns>
        internal T? CreateInstance<T>(bool usePredefinedFileSystem, params object[] args) where T : class
        {
            if (IsMockFileSystem<T>(usePredefinedFileSystem))
            {
                return fileSystem as T;
            }

            var type = typeof(T).IsInterface ? GetTypeFromInterface<T>() : new InstanceModel<T>();

            if (type.CreateFunc != null)
            {
                return (T) type.CreateFunc.Invoke(this);
            }

            args ??= Array.Empty<object>();

            var constructor =
                args.Length > 0
                    ? FindConstructor(type.InstanceType, false, args)
                    : FindConstructor(false, type.InstanceType, false);

            return (T) constructor.ConstructorInfo.Invoke(constructor.ParameterList);
        }

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
            var allConstructors = nonPublic ? GetConstructorsNonPublic(type, args) : GetConstructors(type, args);

            var constructors = allConstructors
                .Where(x => x.Value
                    .Select(z => z?.GetType())
                    .SequenceEqual(args.Select(y => y?.GetType())))
                .ToList();

            return !constructors.Any()
                ? throw new NotImplementedException("Unable to find the constructor.")
                : new ConstructorModel(constructors.First());
        }

        /// <summary>
        ///     Finds the constructor.
        /// </summary>
        /// <param name="bestGuess">if set to <c>true</c> [best guess].</param>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic">if set to <c>true</c> [non public].</param>
        /// <returns><see cref="Tuple{ConstructorInfo, List}" />.</returns>
        /// <exception cref="System.Runtime.AmbiguousImplementationException">Multiple parameterized constructors exist. Cannot decide which to use.</exception>
        /// <exception cref="System.NotImplementedException">Unable to find the constructor.</exception>
        internal ConstructorModel FindConstructor(bool bestGuess, Type type, bool nonPublic)
        {
            var constructors = nonPublic ? GetConstructorsNonPublic(type) : GetConstructors(type);

            if (!bestGuess && constructors.Values.Count(x => x.Count > 0) > 1)
            {
                throw new AmbiguousImplementationException(
                    "Multiple parameterized constructors exist. Cannot decide which to use."
                );
            }

            return !constructors.Any()
                ? throw new NotImplementedException("Unable to find the constructor.")
                : new ConstructorModel(constructors.First());
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
        /// <param name="type">The type.</param>
        /// <param name="instanceParameterValues">The instance parameter values.</param>
        /// <returns>Dictionary&lt;ConstructorInfo, List&lt;System.Nullable&lt;System.Object&gt;&gt;&gt;.</returns>
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
        ///     Gets the default value.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Nullable&lt;System.Object&gt;.</returns>
        internal static object? GetDefaultValue(Type type) => type.IsClass ? null : Activator.CreateInstance(type);

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
        internal MockModel<T> GetMockModel<T>(Mock<T>? mock = null, bool autoCreate = true) where T : class => new(GetMockModel(typeof(T), mock, autoCreate));

        /// <summary>
        ///     Gets the mock model index of.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="autoCreate">Create Mock if it doesn't exist.</param>
        /// <returns>System.Int32.</returns>
        internal int GetMockModelIndexOf(Type type, bool autoCreate = true) => mockCollection.IndexOf(GetMockModel(type, null, autoCreate));

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

            var types = tType.Assembly.GetTypes().ToList();

            // Get interfaces that contain T.
            var interfaces = types.Where(type => type.IsInterface && type.GetInterfaces().Contains(tType)).ToList();

            // Get Types that contain T, but are not interfaces.
            var possibleTypes = types.Where(type =>
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
        internal static bool IsNullableType(Type type) => type.IsClass || (
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

            var paramList = info.GetParameters().ToList();

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
        ///     Throws the already exists.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <exception cref="System.ArgumentException"></exception>
        internal static void ThrowAlreadyExists(Type type) => throw new ArgumentException($"{type} already exists.");
    }
}
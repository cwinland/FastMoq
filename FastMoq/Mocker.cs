using Moq;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Runtime;

namespace FastMoq
{
    /// <summary>
    ///     Class Mocks.
    /// </summary>
    public class Mocker
    {
        #region Fields

        /// <summary>
        ///     The file system
        /// </summary>
        public readonly MockFileSystem fileSystem;

        /// <summary>
        ///     The mock collection
        /// </summary>
        protected readonly List<MockModel> mockCollection;

        /// <summary>
        ///     Gets the type map.
        /// </summary>
        /// <value>The type map.</value>
        private readonly Dictionary<Type, InstanceModel> typeMap;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="Mocker" /> is strict. If strict, the mock IFileSystem does
        ///     not use FileSystemMock and uses Mock of IFileSystem.
        /// </summary>
        /// <value><c>true</c> if strict; otherwise, <c>false</c>.</value>
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
        ///     Initializes a new instance of the <see cref="T:FastMoq.Mocks" /> class.
        /// </summary>
        /// <param name="typeMap">The type map.</param>
        public Mocker(Dictionary<Type, InstanceModel> typeMap) : this() => this.typeMap = typeMap;

        /// <summary>
        ///     Add specified Mock.
        /// </summary>
        /// <typeparam name="T">Mock Type.</typeparam>
        /// <param name="mock">Mock to Add.</param>
        /// <param name="overwrite">
        ///     Overwrite if the mock exists or throw <see cref="ArgumentException" /> if this parameter is
        ///     false.
        /// </param>
        /// <returns><see cref="Mock{T}" />.</returns>
        public MockModel<T> AddMock<T>(Mock<T> mock, bool overwrite) where T : class => new(AddMock(mock, typeof(T), overwrite));

        /// <summary>
        ///     Adds the type.
        /// </summary>
        /// <typeparam name="TInterface">The type of the t interface.</typeparam>
        /// <typeparam name="TClass">The type of the t class.</typeparam>
        /// <param name="createFunc">The create function.</param>
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
        ///     Determines whether this instance contains the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><c>true</c> if [contains]; otherwise, <c>false</c>.</returns>
        public bool Contains<T>() where T : class => Contains(typeof(T));

        /// <summary>
        ///     Determines whether this instance contains the object.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if [contains] [the specified type]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">type</exception>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        public bool Contains(Type type) => type == null ? throw new ArgumentNullException(nameof(type)) :
            !type.IsClass && !type.IsInterface ? throw new ArgumentException("type must be a class.", nameof(type)) :
            mockCollection.Any(x => x.Type == type);

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args">The arguments.</param>
        /// <returns><see cref="Nullable{T}" />.</returns>
        public T? CreateInstance<T>(params object[] args) where T : class => CreateInstance<T>(true, args);

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="usePredefinedFileSystem">if set to <c>true</c> [use predefined file system].</param>
        /// <returns>System.Nullable&lt;IFileSystem&gt;.</returns>
        public IFileSystem? CreateInstance<T>(bool usePredefinedFileSystem) where T : class, IFileSystem =>
            CreateInstance<T>(usePredefinedFileSystem, Array.Empty<object>());

        /// <summary>
        ///     Creates the instance non public.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args">The arguments.</param>
        /// <returns><see cref="Nullable{T}"/></returns>
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

            return (T) constructor.Key.Invoke(constructor.Value.ToArray());
        }

        /// <summary>
        ///     Creates the mock.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see cref="List{Mock}" />.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ApplicationException">Cannot create instance.</exception>
        public List<MockModel> CreateMock(Type type)
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

            if (Activator.CreateInstance(newType) is not Mock oMock)
            {
                throw new ApplicationException("Cannot create instance.");
            }

            AddMock(oMock, type);
            return mockCollection;
        }

        /// <summary>
        ///     Creates the mock.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="List{T}" />.</returns>
        public List<MockModel> CreateMock<T>() where T : class => CreateMock(typeof(T));

        /// <summary>
        ///     Gets the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="count">The count.</param>
        /// <param name="func">The function.</param>
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
        ///     Gets the mock and creates it if necessary.
        /// </summary>
        /// <typeparam name="T"><see cref="Type" /> of Class.</typeparam>
        /// <returns><see cref="Mock{T}" />.</returns>
        public Mock<T> GetMock<T>() where T : class => (Mock<T>) GetMock(typeof(T));

        /// <summary>
        ///     Gets the mock.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>Mock.</returns>
        public Mock GetMock(Type type)
        {
            if (!Contains(type))
            {
                CreateMock(type);
            }

            return GetRequiredMock(type);
        }

        /// <summary>
        ///     Gets the object for the given <see cref="ParameterInfo" /> object.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>
        ///     <see cref="Nullable{Object}" />
        /// </returns>
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
        ///     Gets the object.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see cref="Object" />.</returns>
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
        ///     Gets the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>T.</returns>
        public T? GetObject<T>() where T : class => GetObject(typeof(T)) as T;

        /// <summary>
        ///     Gets the required mock.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>Mock.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        public Mock GetRequiredMock(Type type)
        {
            if (type == null || (!type.IsClass && !type.IsInterface))
            {
                throw new ArgumentException("type must be a class.", nameof(type));
            }

            return mockCollection.First(x => x.Type == type).Mock;
        }

        /// <summary>
        ///     Gets the required mock.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Mock&lt;T&gt;.</returns>
        public Mock<T> GetRequiredMock<T>() where T : class => (Mock<T>) GetRequiredMock(typeof(T));

        /// <summary>
        ///     Initializes the specified action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action">The action.</param>
        /// <returns>Mock&lt;T&gt;.</returns>
        /// <exception cref="System.InvalidOperationException">Invalid Mock.</exception>
        public Mock<T> Initialize<T>(Action<Mock<T>> action) where T : class
        {
            var mock = GetMock<T>() ?? throw new InvalidOperationException("Invalid Mock.");

            mock.SetupAllProperties();
            action.Invoke(mock);
            return mock;
        }

        /// <summary>
        ///     Remove specified Mock.
        /// </summary>
        /// <typeparam name="T">Mock Type.</typeparam>
        /// <param name="mock">Mock to Remove.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
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
        /// <param name="overwrite">
        ///     Overwrite if the mock exists or throw <see cref="ArgumentException" /> if this parameter is
        ///     false.
        /// </param>
        /// <returns><see cref="Mock{T}" />.</returns>
        /// <exception cref="System.ArgumentNullException">mock</exception>
        /// <exception cref="System.ArgumentNullException">type</exception>
        internal MockModel AddMock(Mock mock, Type type, bool overwrite = false)
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

            mockCollection.Add(new MockModel(type, mock));

            return GetMockModel(type);
        }

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
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

            return (T) constructor.Key.Invoke(constructor.Value.ToArray());
        }

        /// <summary>
        ///     Finds the constructor matching args EXACTLY.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic">if set to <c>true</c> [non public].</param>
        /// <param name="args">The arguments.</param>
        /// <returns>ConstructorInfo.</returns>
        /// <exception cref="System.NotImplementedException">Unable to find the constructor.</exception>
        internal KeyValuePair<ConstructorInfo, List<object?>> FindConstructor(Type type, bool nonPublic, params object?[] args)
        {
            var allConstructors = nonPublic ? GetConstructorsNonPublic(type, args) : GetConstructors(type, args);

            var constructors = allConstructors
                .Where(x => x.Value.Select(z => z?.GetType()).SequenceEqual(args.Select(y => y?.GetType()))).ToList();

            return !constructors.Any()
                ? throw new NotImplementedException("Unable to find the constructor.")
                : constructors.First();
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
        internal KeyValuePair<ConstructorInfo, List<object?>> FindConstructor(bool bestGuess, Type type, bool nonPublic)
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
                : constructors.First();
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
        /// <returns>MockModel.</returns>
        internal MockModel GetMockModel(Type type, Mock? mock = null) =>
            mockCollection.FirstOrDefault(x => x.Type == type && (x.Mock == mock || mock == null)) ??
            (mock == null ? GetMockModel(type, GetMock(type)) : AddMock(mock, type));

        /// <summary>
        ///     Gets the mock model.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mock">The mock.</param>
        /// <returns>MockModel&lt;T&gt;.</returns>
        internal MockModel<T> GetMockModel<T>(Mock<T>? mock = null) where T : class => new(GetMockModel(typeof(T), mock));

        /// <summary>
        ///     Gets the mock model index of.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Int32.</returns>
        internal int GetMockModelIndexOf(Type type) => mockCollection.IndexOf(GetMockModel(type));

        /// <summary>
        ///     Gets the type from interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
        /// <typeparam name="T"></typeparam>
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
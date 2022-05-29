using Moq;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Runtime;

namespace FastMoq
{
    /// <summary>
    /// Class Mocks.
    /// </summary>
    public class Mocks
    {
        #region Fields

        /// <summary>
        /// The file system
        /// </summary>
        public readonly MockFileSystem fileSystem;

        /// <summary>
        /// The mock collection
        /// </summary>
        protected readonly List<Mock> mockCollection;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Mocks"/> is strict.
        /// </summary>
        /// <value><c>true</c> if strict; otherwise, <c>false</c>.</value>
        public bool Strict { get; set; }

        /// <summary>
        /// Gets the type map.
        /// </summary>
        /// <value>The type map.</value>
        public Dictionary<Type, Type> TypeMap { get; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Mocks"/> class.
        /// </summary>
        public Mocks()
        {
            fileSystem = new();
            mockCollection = new();
            TypeMap = new();
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:FastMoq.Mocks" /> class.
        /// </summary>
        /// <param name="typeMap">The type map.</param>
        public Mocks(Dictionary<Type, Type> typeMap) : this() => TypeMap = typeMap;

        /// <summary>
        /// Add specified Mock.
        /// </summary>
        /// <typeparam name="T">Mock Type.</typeparam>
        /// <param name="mock">Mock to Add.</param>
        /// <param name="overwrite">Overwrite if the mock exists or throw <see cref="ArgumentException" /> if this parameter is
        /// false.</param>
        /// <returns>Mock&lt;T&gt;.</returns>
        public Mock<T> AddMock<T>(Mock<T> mock, bool overwrite) where T : class
        {
            if (Contains<T>())
            {
                if (!overwrite)
                {
                    ThrowAlreadyExists(typeof(T));
                }

                mockCollection[mockCollection.IndexOf(mock)] = mock;
                return GetMock<T>();
            }

            mockCollection.Add(mock);

            return GetMock<T>();
        }

        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><c>true</c> if [contains]; otherwise, <c>false</c>.</returns>
        public bool Contains<T>() where T : class => Contains(typeof(T));

        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if [contains] [the specified type]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">type</exception>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        public bool Contains(Type type) =>
            type == null ? throw new ArgumentNullException(nameof(type)) :
            !type.IsClass && !type.IsInterface ? throw new ArgumentException("type must be a class.", nameof(type)) :
            mockCollection.Any(x =>
                (x.GetType().GenericTypeArguments.First().FullName ?? string.Empty)
                .Equals(type.FullName, StringComparison.OrdinalIgnoreCase)
            );

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="usePredefinedFileSystem">if set to <c>true</c> [use predefined file system].</param>
        /// <param name="args">The arguments.</param>
        /// <returns>System.Nullable&lt;T&gt;.</returns>
        public T? CreateInstance<T>(bool usePredefinedFileSystem = true, params object[] args) where T : class
        {
            if (usePredefinedFileSystem && (typeof(T) == typeof(IFileSystem) || typeof(T) == typeof(FileSystem)))
            {
                return fileSystem as T;
            }

            var type = typeof(T).IsInterface ? GetTypeFromInterface<T>() : typeof(T);

            KeyValuePair<ConstructorInfo, List<object?>> constructor = FindConstructor(type, args);

            return constructor.Key.Invoke(constructor.Value.ToArray()) as T;
        }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="usePredefinedFileSystem">if set to <c>true</c> [use predefined file system].</param>
        /// <param name="bestGuess">if set to <c>true</c> the constructor with the most parameters is chosen.</param>
        /// <returns>T.</returns>
        /// <exception cref="System.Runtime.AmbiguousImplementationException">Multiple parameterized constructors exist. Cannot
        /// decide which to use.</exception>
        public T? CreateInstance<T>(bool usePredefinedFileSystem = true, bool bestGuess = false) where T : class
        {
            if (usePredefinedFileSystem && (typeof(T) == typeof(IFileSystem) || typeof(T) == typeof(FileSystem)))
            {
                return fileSystem as T;
            }

            var t = typeof(T).IsInterface ? GetTypeFromInterface<T>() : typeof(T);

            KeyValuePair<ConstructorInfo, List<object?>> constructor = FindConstructor(bestGuess, t);

            return constructor.Key.Invoke(constructor.Value.ToArray()) as T;
        }

        /// <summary>
        /// Creates the mock.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see cref="List{Mock}" />.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ApplicationException">Cannot create instance.</exception>
        public List<Mock> CreateMock(Type type)
        {
            if (type == null || !type.IsClass && !type.IsInterface)
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

            mockCollection.Add(oMock);
            return mockCollection;
        }

        /// <summary>
        /// Creates the mock.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="List{T}" />.</returns>
        public List<Mock> CreateMock<T>() where T : class => CreateMock(typeof(T));

        /// <summary>
        /// Gets the list.
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
        /// Gets the mock and creates it if necessary.
        /// </summary>
        /// <typeparam name="T"><see cref="Type" /> of Class.</typeparam>
        /// <returns><see cref="Mock{T}" />.</returns>
        public Mock<T> GetMock<T>() where T : class => (Mock<T>)GetMock(typeof(T));

        /// <summary>
        /// Gets the mock.
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
        /// Gets the object for the given <see cref="ParameterInfo" /> object.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns><see cref="Nullable{Object}" /></returns>
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
        /// Gets the object.
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
        /// Gets the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>T.</returns>
        public T? GetObject<T>() where T : class => GetObject(typeof(T)) as T;

        /// <summary>
        /// Gets the required mock.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>Mock.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        public Mock GetRequiredMock(Type type)
        {
            if (type == null || !type.IsClass && !type.IsInterface)
            {
                throw new ArgumentException("type must be a class.", nameof(type));
            }

            return mockCollection.First(x =>
                x.GetType().GenericTypeArguments.First().FullName?.Equals(type.FullName, StringComparison.OrdinalIgnoreCase) ??
                false
            );
        }

        /// <summary>
        /// Gets the required mock.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Mock&lt;T&gt;.</returns>
        public Mock<T> GetRequiredMock<T>() where T : class => (Mock<T>) GetRequiredMock(typeof(T));

        /// <summary>
        /// Initializes the specified action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action">The action.</param>
        /// <returns>Mock&lt;T&gt;.</returns>
        /// <exception cref="System.InvalidOperationException">Invalid Mock.</exception>
        public Mock<T> Initialize<T>(Action<Mock<T>> action) where T : class
        {
            Mock<T> mock = GetMock<T>() ?? throw new InvalidOperationException("Invalid Mock.");

            mock.SetupAllProperties();
            action.Invoke(mock);
            return mock;
        }

        /// <summary>
        /// Remove specified Mock.
        /// </summary>
        /// <typeparam name="T">Mock Type.</typeparam>
        /// <param name="mock">Mock to Remove.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool RemoveMock<T>(Mock<T> mock) where T : class => mockCollection.Remove(mock);

        /// <summary>
        ///     Finds the constructor.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>ConstructorInfo.</returns>
        /// <exception cref="System.NotImplementedException">Unable to find the constructor.</exception>
        internal KeyValuePair<ConstructorInfo, List<object?>> FindConstructor(Type type, params object?[] args)
        {
            Dictionary<ConstructorInfo, List<object?>> allConstructors = GetConstructors(type, args);

            List<KeyValuePair<ConstructorInfo, List<object?>>> constructors = allConstructors
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
        /// <returns><see cref="Tuple{ConstructorInfo, List}" />.</returns>
        /// <exception cref="System.Runtime.AmbiguousImplementationException">
        ///     Multiple parameterized constructors exist. Cannot
        ///     decide which to use.
        /// </exception>
        /// <exception cref="System.NotImplementedException">Unable to find the constructor.</exception>
        internal KeyValuePair<ConstructorInfo, List<object?>> FindConstructor(bool bestGuess, Type type)
        {
            Dictionary<ConstructorInfo, List<object?>> constructors = GetConstructors(type);

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
        /// <param name="args">Optional arguments.</param>
        /// <returns><see cref="Dictionary{ConstructorInfo, List}" />.</returns>
        internal Dictionary<ConstructorInfo, List<object?>> GetConstructors(Type type, params object?[] args) =>
            type.GetConstructors()
                .Where(x => IsValidConstructor(x, args))
                .OrderByDescending(x => x.GetParameters().Length)
                .ToDictionary(x => x,
                    y => (args.Length > 0 ? args : y.GetParameters().Select(GetObject)).ToList()
                );

        internal static object? GetDefaultValue(Type type) => type.IsClass ? null : Activator.CreateInstance(type);

        internal Type GetTypeFromInterface<T>() where T : class
        {
            var tType = typeof(T);

            if (!tType.IsInterface)
            {
                return tType;
            }

            var mappedType = TypeMap.Where(x => x.Key == typeof(T)).Select(x=>x.Value).FirstOrDefault();

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
                !interfaces.Any(type.IsAssignableTo)
            ).ToList();

            return possibleTypes.Count > 1 ? throw new AmbiguousImplementationException() :
                !possibleTypes.Any() ? throw new NotImplementedException() : possibleTypes.First();
        }

        /// <summary>
        ///     Returns true if the argument list == 0 or the types match the constructor exactly.
        /// </summary>
        /// <param name="info">Parameter information.</param>
        /// <param name="args">Optional arguments.</param>
        /// <returns></returns>
        internal bool IsValidConstructor(ConstructorInfo info, params object?[] args) =>
            args.Length == 0 ||
            info.GetParameters()
                .Select(x => x.ParameterType)
                .SequenceEqual(args.Select(x => x?.GetType()));

        private static void ThrowAlreadyExists(Type type) => throw new ArgumentException($"{type} already exists.");
    }
}

using System.IO.Abstractions;

namespace FastMoq.Extensions
{
    /// <summary>
    ///     Mocker Boolean Extensions Class.
    /// </summary>
    public static class MockerBooleanExtensions
    {
        /// <summary>
        ///     Determines whether this instance contains a Mock of <c>T</c>.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <returns><c>true</c> if the Mock exists for the given type; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">type is null.</exception>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        public static bool Contains<T>(this Mocker mocker) where T : class => mocker.Contains(typeof(T));

        /// <summary>
        ///     Determines whether this instance contains the object.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if [contains] [the specified type]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        public static bool Contains(this Mocker mocker, Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return !type.IsClass && !type.IsInterface
                ? throw new ArgumentException("type must be a class.", nameof(type))
                : mocker.mockCollection.Exists(x => x.Type == type);
        }

        /// <summary>
        ///     Determines whether [is mock file system] [the specified use predefined file system].
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="usePredefinedFileSystem">if set to <c>true</c> [use predefined file system].</param>
        /// <returns><c>true</c> if [is mock file system] [the specified use predefined file system]; otherwise, <c>false</c>.</returns>
        internal static bool IsMockFileSystem(this Type type, bool usePredefinedFileSystem) => usePredefinedFileSystem &&
                                                                                               (type == typeof(IFileSystem) ||
                                                                                                type == typeof(FileSystem));
    }
}

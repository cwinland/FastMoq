namespace FastMoq
{
    /// <summary>
    /// Specifies which built-in framework and utility types <see cref="Mocker"/> may resolve automatically.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These flags control FastMoq's built-in object creation behavior for well-known types that are commonly
    /// needed during unit and integration-style test setup.
    /// </para>
    /// <para>
    /// The values can be combined to enable multiple categories at once, or compared against the provided preset
    /// values such as <see cref="StrictCompatibilityDefaults"/> and <see cref="LenientDefaults"/>.
    /// </para>
    /// <para>
    /// Use <see cref="None"/> to disable all built-in resolutions and require explicit registrations, or use
    /// <see cref="All"/> to enable every built-in category currently supported.
    /// </para>
    /// </remarks>
    [Flags]
    public enum BuiltInTypeResolutionFlags
    {
        /// <summary>
        /// Disables all built-in type resolution.
        /// </summary>
        /// <remarks>
        /// When this value is used, FastMoq will not automatically provide any built-in framework objects and will
        /// instead rely on explicit registrations, known types, or provider-created mocks.
        /// </remarks>
        None = 0,

        /// <summary>
        /// Enables automatic resolution for file system abstractions.
        /// </summary>
        /// <remarks>
        /// This flag is intended for scenarios where tests depend on common file system services and FastMoq can
        /// provide a built-in implementation or mapping without requiring manual setup.
        /// </remarks>
        FileSystem = 1 << 0,

        /// <summary>
        /// Enables automatic resolution for <see cref="HttpClient"/> instances.
        /// </summary>
        /// <remarks>
        /// When enabled, FastMoq may construct or supply an <see cref="HttpClient"/> using its built-in HTTP test
        /// support so consuming components can be created with minimal ceremony.
        /// </remarks>
        HttpClient = 1 << 1,

        /// <summary>
        /// Enables automatic resolution for <see cref="Uri"/> values.
        /// </summary>
        /// <remarks>
        /// This flag is commonly used together with <see cref="HttpClient"/> support so tests can receive a matching
        /// base address or related URI dependency without registering one explicitly.
        /// </remarks>
        Uri = 1 << 2,

        /// <summary>
        /// Enables automatic resolution for database context types.
        /// </summary>
        /// <remarks>
        /// This allows FastMoq to provide supported database context handling through its built-in database support,
        /// which is especially useful for tests that expect a tracked or mocked context instance.
        /// </remarks>
        DbContext = 1 << 3,

        /// <summary>
        /// Enables every built-in type resolution category currently supported.
        /// </summary>
        /// <remarks>
        /// This is the broadest preset and is equivalent to combining <see cref="FileSystem"/>,
        /// <see cref="HttpClient"/>, <see cref="Uri"/>, and <see cref="DbContext"/>.
        /// </remarks>
        All = FileSystem | HttpClient | Uri | DbContext,

        /// <summary>
        /// Enables the conservative compatibility preset used for stricter legacy behavior.
        /// </summary>
        /// <remarks>
        /// This preset currently includes only <see cref="DbContext"/> resolution so that other built-in helpers stay
        /// disabled unless explicitly requested.
        /// </remarks>
        StrictCompatibilityDefaults = DbContext,

        /// <summary>
        /// Enables the lenient preset used for maximum built-in convenience.
        /// </summary>
        /// <remarks>
        /// This preset is equivalent to <see cref="All"/> and turns on every built-in category supported by the
        /// current version of FastMoq.
        /// </remarks>
        LenientDefaults = All,
    }
}
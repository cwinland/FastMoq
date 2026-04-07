namespace FastMoq
{
    /// <summary>
    /// Per-call overrides for constructor selection and optional-parameter resolution during instance creation.
    /// <see cref="None"/> preserves the current <see cref="Mocker"/> defaults.
    /// </summary>
    [Flags]
    public enum InstanceCreationFlags
    {
        /// <summary>
        /// Use the current <see cref="Mocker"/> defaults for constructor selection and optional-parameter resolution.
        /// </summary>
        None = 0,

        /// <summary>
        /// Restrict constructor selection to public constructors only.
        /// </summary>
        PublicConstructorsOnly = 1 << 0,

        /// <summary>
        /// Allow constructor selection to fall back to non-public constructors.
        /// </summary>
        AllowNonPublicConstructorFallback = 1 << 1,

        /// <summary>
        /// Resolve optional constructor parameters through the normal FastMoq mock and object pipeline.
        /// </summary>
        ResolveOptionalParametersViaMocker = 1 << 2,

        /// <summary>
        /// Use declared default values for optional parameters, or <see langword="null"/> when no default value is available.
        /// </summary>
        UseDefaultOrNullOptionalParameters = 1 << 3,
    }
}
namespace FastMoq
{
    /// <summary>
    /// Controls how FastMoq resolves optional parameters when it needs to supply values automatically.
    /// </summary>
    public enum OptionalParameterResolutionMode
    {
        /// <summary>
        /// Use the declared default value when present; otherwise pass null.
        /// </summary>
        UseDefaultOrNull = 0,

        /// <summary>
        /// Resolve optional parameters through the normal FastMoq pipeline, which may create mocks or concrete instances.
        /// </summary>
        ResolveViaMocker = 1,
    }
}
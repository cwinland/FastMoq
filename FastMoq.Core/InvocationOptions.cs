namespace FastMoq
{
    /// <summary>
    /// Controls how FastMoq fills method parameters when invoking delegates or reflected methods.
    /// </summary>
    public sealed class InvocationOptions
    {
        /// <summary>
        /// Controls how optional parameters are resolved when values are not explicitly provided.
        /// </summary>
        public OptionalParameterResolutionMode OptionalParameterResolution { get; set; } = OptionalParameterResolutionMode.UseDefaultOrNull;
    }
}
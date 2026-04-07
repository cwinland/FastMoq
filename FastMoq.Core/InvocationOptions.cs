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

        /// <summary>
        /// Controls whether FastMoq may fall back from a public-method search to a non-public-method search.
        /// When not set, FastMoq uses the current <see cref="MockerPolicyOptions.DefaultFallbackToNonPublicMethods"/> policy.
        /// Compatibility helpers such as <c>Strict</c> and preset methods can update that default policy.
        /// </summary>
        public bool? FallbackToNonPublicMethods { get; set; }
    }
}
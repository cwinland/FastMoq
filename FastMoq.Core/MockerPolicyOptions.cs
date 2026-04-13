namespace FastMoq
{
    /// <summary>
    /// Groups the default policy settings that shape how a <see cref="Mocker"/> resolves built-ins and creates mocks.
    /// Per-call options may still override the relevant creation or invocation behavior where supported.
    /// </summary>
    public sealed class MockerPolicyOptions
    {
        /// <summary>
        /// Controls which built-in type resolutions FastMoq applies automatically when a requested type has not been explicitly registered by the test.
        /// </summary>
        public BuiltInTypeResolutionFlags EnabledBuiltInTypeResolutions { get; set; } = BuiltInTypeResolutionFlags.LenientDefaults;

        /// <summary>
        /// Indicates whether instance creation should fall back to non-public constructors by default when no public constructor can satisfy the request.
        /// </summary>
        public bool DefaultFallbackToNonPublicConstructors { get; set; } = true;

        /// <summary>
        /// Controls how FastMoq resolves constructor ambiguity when multiple equally viable constructors remain after candidate filtering.
        /// The default preserves the existing throw behavior for backward compatibility.
        /// </summary>
        public ConstructorAmbiguityBehavior DefaultConstructorAmbiguityBehavior { get; set; } = ConstructorAmbiguityBehavior.Throw;

        /// <summary>
        /// Indicates whether method invocation helpers should consider non-public methods by default when matching a target member.
        /// </summary>
        public bool DefaultFallbackToNonPublicMethods { get; set; } = true;

        /// <summary>
        /// Controls the default strict-vs-loose behavior for provider-backed mock creation paths.
        /// When not set, FastMoq falls back to <see cref="MockFeatures.FailOnUnconfigured"/>.
        /// This applies to provider-backed and legacy mock creation helpers, but not to the optional database helper package's
        /// DbContext-specific mock creation path, which stays on the supported DbContext helper behavior.
        /// </summary>
        public bool? DefaultStrictMockCreation { get; set; }
    }
}
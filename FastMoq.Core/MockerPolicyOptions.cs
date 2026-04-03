namespace FastMoq
{
    /// <summary>
    /// Groups the default policy settings that shape how a <see cref="Mocker"/> resolves built-ins and creates mocks.
    /// Per-call options may still override the relevant creation or invocation behavior where supported.
    /// </summary>
    public sealed class MockerPolicyOptions
    {
        public BuiltInTypeResolutionFlags EnabledBuiltInTypeResolutions { get; set; } = BuiltInTypeResolutionFlags.LenientDefaults;

        public bool DefaultFallbackToNonPublicConstructors { get; set; } = true;

        public bool DefaultFallbackToNonPublicMethods { get; set; } = true;

        /// <summary>
        /// Controls the default strict-vs-loose behavior for provider-backed mock creation paths.
        /// When not set, FastMoq falls back to <see cref="MockFeatures.FailOnUnconfigured"/>.
        /// This applies to provider-backed and legacy mock creation helpers, but not to <see cref="Mocker.GetMockDbContext{TContext}()"/>,
        /// which stays on the supported DbContext helper behavior.
        /// </summary>
        public bool? DefaultStrictMockCreation { get; set; }
    }
}
namespace FastMoq
{
    /// <summary>
    /// Feature flags that control optional FastMoq runtime behaviors.
    /// </summary>
    [Flags]
    public enum MockFeatures
    {
        /// <summary>
        /// No optional features are enabled.
        /// </summary>
        None = 0,

        /// <summary>
        /// Allows class-based mocks to call through to their base implementation when supported by the active provider.
        /// </summary>
        CallBase = 1 << 0,

        /// <summary>
        /// Configures supported mocks so writable properties behave like normal auto-properties.
        /// </summary>
        AutoSetupProperties = 1 << 1,

        /// <summary>
        /// Automatically resolves and injects constructor, field, and property dependencies when FastMoq creates or prepares objects.
        /// </summary>
        AutoInjectDependencies = 1 << 2,

        /// <summary>
        /// Enables recursive nested member population after object creation or mock setup.
        /// </summary>
        ResolveNestedMembers = 1 << 7,

        /// <summary>
        /// Captures logger invocations into the current <see cref="Mocker"/> instance when supported by the active provider.
        /// </summary>
        LoggerCallback = 1 << 3,

        /// <summary>
        /// Enables verification helpers that fail when unexpected calls were recorded.
        /// </summary>
        VerifyNoUnexpected = 1 << 4,

        /// <summary>
        /// Records invocation history for later verification and diagnostics.
        /// </summary>
        TrackInvocations = 1 << 5,

        /// <summary>
        /// Causes unresolved or unconfigured interactions to fail immediately instead of using lenient defaults.
        /// </summary>
        FailOnUnconfigured = 1 << 6
    }

    /// <summary>
    /// Mutable container for the enabled <see cref="MockFeatures"/> on a <see cref="Mocker"/> instance.
    /// </summary>
    public sealed class MockBehaviorOptions
    {
        /// <summary>
        /// Gets or sets the currently enabled feature flags.
        /// </summary>
        public MockFeatures Enabled { get; set; }

        /// <summary>
        /// Returns <see langword="true"/> when the supplied feature flag is enabled.
        /// </summary>
        /// <param name="feature">The feature flag to test.</param>
        public bool Has(MockFeatures feature) => (Enabled & feature) != 0;

        /// <summary>
        /// Gets the predefined strict behavior preset.
        /// </summary>
        public static MockBehaviorOptions StrictPreset => new()
        {
            Enabled = MockFeatures.FailOnUnconfigured | MockFeatures.TrackInvocations | MockFeatures.VerifyNoUnexpected | MockFeatures.ResolveNestedMembers
        };

        /// <summary>
        /// Gets the predefined lenient behavior preset.
        /// </summary>
        public static MockBehaviorOptions LenientPreset => new()
        {
            Enabled = MockFeatures.CallBase | MockFeatures.AutoSetupProperties | MockFeatures.AutoInjectDependencies | MockFeatures.ResolveNestedMembers | MockFeatures.LoggerCallback | MockFeatures.TrackInvocations
        };

        /// <summary>
        /// Creates a copy of the current behavior options.
        /// </summary>
        public MockBehaviorOptions Clone() => new() { Enabled = Enabled };
    }

    internal static class MockBehaviorOptionsExtensions
    {
        public static MockBehaviorOptions Enable(this MockBehaviorOptions opts, MockFeatures f)
        {
            opts.Enabled |= f; return opts;
        }
        public static MockBehaviorOptions Disable(this MockBehaviorOptions opts, MockFeatures f)
        {
            opts.Enabled &= ~f; return opts;
        }
    }
}

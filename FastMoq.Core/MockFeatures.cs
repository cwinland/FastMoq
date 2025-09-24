using System;

namespace FastMoq
{
    [Flags]
    public enum MockFeatures
    {
        None = 0,
        CallBase = 1 << 0,
        AutoSetupProperties = 1 << 1,
        AutoInjectDependencies = 1 << 2,
        LoggerCallback = 1 << 3,
        VerifyNoUnexpected = 1 << 4,
        TrackInvocations = 1 << 5,
        FailOnUnconfigured = 1 << 6
    }

    public sealed class MockBehaviorOptions
    {
        public MockFeatures Enabled { get; set; }
        public bool Has(MockFeatures feature) => (Enabled & feature) != 0;

        public static MockBehaviorOptions StrictPreset => new()
        {
            Enabled = MockFeatures.FailOnUnconfigured | MockFeatures.TrackInvocations | MockFeatures.VerifyNoUnexpected
        };

        public static MockBehaviorOptions LenientPreset => new()
        {
            Enabled = MockFeatures.CallBase | MockFeatures.AutoSetupProperties | MockFeatures.AutoInjectDependencies | MockFeatures.LoggerCallback | MockFeatures.TrackInvocations
        };

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

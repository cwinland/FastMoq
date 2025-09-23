using FastMoq.Providers;

namespace FastMoq.Core.Providers.MoqProvider
{
    internal sealed class MoqCapabilities : IMockingProviderCapabilities
    {
        public static IMockingProviderCapabilities Instance { get; } = new MoqCapabilities();
        private MoqCapabilities() { }
        public bool SupportsCallBase => true;
        public bool SupportsStrict => true;
        public bool SupportsSetupAllProperties => true;
        public bool SupportsProtectedMembers => true;
        public bool SupportsInvocationTracking => true;
    }
}

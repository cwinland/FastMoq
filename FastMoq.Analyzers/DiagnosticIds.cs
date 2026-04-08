namespace FastMoq.Analyzers
{
    public static class DiagnosticIds
    {
        public const string UseProviderFirstObjectAccess = "FMOQ0001";
        public const string UseProviderFirstReset = "FMOQ0002";
        public const string UseVerifyLogged = "FMOQ0003";
        public const string UseConsistentMockRetrieval = "FMOQ0004";
        public const string UseExplicitOptionalParameterResolution = "FMOQ0005";
        public const string ReplaceInitializeCompatibilityWrapper = "FMOQ0006";
        public const string AvoidStrictCompatibilityProperty = "FMOQ0007";
        public const string UseTimesSpecAtHelperBoundary = "FMOQ0008";
        public const string SelectProviderBeforeProviderSpecificApi = "FMOQ0009";
        public const string PreferTypedProviderExtensions = "FMOQ0010";
        public const string PreferWebTestHelpers = "FMOQ0011";
        public const string PreferProviderNeutralHttpHelpers = "FMOQ0012";
    }
}
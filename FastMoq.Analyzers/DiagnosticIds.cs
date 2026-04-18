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
        public const string PreferTypedServiceProviderHelpers = "FMOQ0013";
        public const string PreferKnownTypeRegistrations = "FMOQ0014";
        public const string PreserveKeyedServiceDistinctness = "FMOQ0015";
        public const string UseProviderFirstMockRetrieval = "FMOQ0016";
        public const string AvoidLegacyRequiredMockRetrieval = "FMOQ0017";
        public const string AvoidLegacyMockCreationAndLifecycleApis = "FMOQ0018";
        public const string PreferSetupOptionsHelper = "FMOQ0019";
        public const string PreferPropertySetterCaptureHelper = "FMOQ0020";
        public const string PreferPropertyStateHelper = "FMOQ0021";
        public const string PreserveTrackedResolutionDuringAddTypeMigration = "FMOQ0022";
        public const string RequireExplicitMoqOnboarding = "FMOQ0023";
        public const string UseProviderFirstVerify = "FMOQ0024";
        public const string AvoidBareTrackedVerify = "FMOQ0025";
        public const string AvoidTrackedMockShimAlias = "FMOQ0026";
        public const string AvoidRawMockCreationInFastMoqSuites = "FMOQ0027";
        public const string ReferenceFastMoqHelperPackage = "FMOQ0028";
        public const string PreferFunctionContextExecutionHelpers = "FMOQ0029";
    }
}
namespace FastMoq.Analyzers
{
    /// <summary>
    /// Diagnostic identifiers used by the FastMoq migration analyzers and code fixes.
    /// </summary>
    public static class DiagnosticIds
    {
        /// <summary>
        /// Use provider-first object access instead of provider-native object surfaces.
        /// </summary>
        public const string UseProviderFirstObjectAccess = "FMOQ0001";

        /// <summary>
        /// Use provider-first reset instead of provider-native reset calls.
        /// </summary>
        public const string UseProviderFirstReset = "FMOQ0002";

        /// <summary>
        /// Use provider-safe log verification instead of legacy VerifyLogger helpers.
        /// </summary>
        public const string UseVerifyLogged = "FMOQ0003";

        /// <summary>
        /// Keep tracked mock retrieval consistent within a document.
        /// </summary>
        public const string UseConsistentMockRetrieval = "FMOQ0004";

        /// <summary>
        /// Use explicit optional-parameter resolution settings.
        /// </summary>
        public const string UseExplicitOptionalParameterResolution = "FMOQ0005";

        /// <summary>
        /// Replace the Initialize compatibility wrapper with direct mock access.
        /// </summary>
        public const string ReplaceInitializeCompatibilityWrapper = "FMOQ0006";

        /// <summary>
        /// Avoid the strict compatibility property.
        /// </summary>
        public const string AvoidStrictCompatibilityProperty = "FMOQ0007";

        /// <summary>
        /// Use TimesSpec at helper boundaries.
        /// </summary>
        public const string UseTimesSpecAtHelperBoundary = "FMOQ0008";

        /// <summary>
        /// Resolve the matching provider before using provider-specific APIs.
        /// </summary>
        public const string SelectProviderBeforeProviderSpecificApi = "FMOQ0009";

        /// <summary>
        /// Prefer typed provider extensions over raw provider-native surfaces.
        /// </summary>
        public const string PreferTypedProviderExtensions = "FMOQ0010";

        /// <summary>
        /// Prefer FastMoq.Web helpers for HTTP and ASP.NET test authoring.
        /// </summary>
        public const string PreferWebTestHelpers = "FMOQ0011";

        /// <summary>
        /// Prefer provider-neutral HTTP helpers over provider-specific HTTP mocking.
        /// </summary>
        public const string PreferProviderNeutralHttpHelpers = "FMOQ0012";

        /// <summary>
        /// Prefer typed service-provider helpers over manual shim setups.
        /// </summary>
        public const string PreferTypedServiceProviderHelpers = "FMOQ0013";

        /// <summary>
        /// Prefer known-type registrations when resolution depends on requested type.
        /// </summary>
        public const string PreferKnownTypeRegistrations = "FMOQ0014";

        /// <summary>
        /// Preserve distinct keyed service dependencies during migration.
        /// </summary>
        public const string PreserveKeyedServiceDistinctness = "FMOQ0015";

        /// <summary>
        /// Use provider-first mock retrieval instead of GetMock compatibility APIs.
        /// </summary>
        public const string UseProviderFirstMockRetrieval = "FMOQ0016";

        /// <summary>
        /// Avoid legacy GetRequiredMock compatibility retrieval.
        /// </summary>
        public const string AvoidLegacyRequiredMockRetrieval = "FMOQ0017";

        /// <summary>
        /// Avoid legacy mock creation and lifecycle compatibility APIs.
        /// </summary>
        public const string AvoidLegacyMockCreationAndLifecycleApis = "FMOQ0018";

        /// <summary>
        /// Prefer the first-party options setup helper.
        /// </summary>
        public const string PreferSetupOptionsHelper = "FMOQ0019";

        /// <summary>
        /// Prefer the property-setter capture helper.
        /// </summary>
        public const string PreferPropertySetterCaptureHelper = "FMOQ0020";

        /// <summary>
        /// Prefer the property-state helper.
        /// </summary>
        public const string PreferPropertyStateHelper = "FMOQ0021";

        /// <summary>
        /// Preserve tracked resolution behavior when migrating AddType patterns.
        /// </summary>
        public const string PreserveTrackedResolutionDuringAddTypeMigration = "FMOQ0022";

        /// <summary>
        /// Resolve Moq provider selection when legacy Moq-shaped APIs remain in use.
        /// </summary>
        public const string RequireExplicitMoqOnboarding = "FMOQ0023";

        /// <summary>
        /// Use provider-first verification instead of provider-native Verify calls.
        /// </summary>
        public const string UseProviderFirstVerify = "FMOQ0024";

        /// <summary>
        /// Avoid bare tracked Verify calls.
        /// </summary>
        public const string AvoidBareTrackedVerify = "FMOQ0025";

        /// <summary>
        /// Avoid tracked Mock&lt;T&gt; shim aliases that keep tests on provider-native surfaces.
        /// </summary>
        public const string AvoidTrackedMockShimAlias = "FMOQ0026";

        /// <summary>
        /// Avoid raw Mock&lt;T&gt; creation inside FastMoq-based test infrastructure.
        /// </summary>
        public const string AvoidRawMockCreationInFastMoqSuites = "FMOQ0027";

        /// <summary>
        /// Reference the required FastMoq helper package for the suggested rewrite.
        /// </summary>
        public const string ReferenceFastMoqHelperPackage = "FMOQ0028";

        /// <summary>
        /// Prefer function-context execution helpers.
        /// </summary>
        public const string PreferFunctionContextExecutionHelpers = "FMOQ0029";

        /// <summary>
        /// Prefer logger-factory helpers.
        /// </summary>
        public const string PreferLoggerFactoryHelpers = "FMOQ0030";

        /// <summary>
        /// Avoid IFastMock.Verify helper wrappers that only forward to FastMoq verification APIs.
        /// </summary>
        public const string AvoidFastMockVerifyHelperWrappers = "FMOQ0031";

        /// <summary>
        /// Avoid provider-specific IFastMock.Verify helper wrappers.
        /// </summary>
        public const string AvoidProviderSpecificFastMockVerifyHelperWrappers = "FMOQ0032";

        /// <summary>
        /// Prefer the shared FastMoq file system in MockerTestBase contexts.
        /// </summary>
        public const string PreferSharedMockFileSystem = "FMOQ0033";

        /// <summary>
        /// Prefer FastArg matchers over Moq It.* matchers inside provider-first Verify expressions when the rewrite is mechanical.
        /// </summary>
        public const string UseFastArgMatcherInProviderFirstVerify = "FMOQ0034";

        /// <summary>
        /// Avoid Moq-specific matcher helpers inside provider-first Verify expressions when no direct FastArg rewrite exists.
        /// </summary>
        public const string AvoidUnsupportedMoqMatcherInProviderFirstVerify = "FMOQ0035";

        /// <summary>
        /// Prefer provider-neutral logger callback capture over provider-specific ILogger.Log setup when the callback only needs normalized output.
        /// </summary>
        public const string PreferSetupLoggerCallbackHelper = "FMOQ0036";

        /// <summary>
        /// Prefer inheritance over thin local helper-instance composition around MockerTestBase&lt;T&gt;.
        /// </summary>
        public const string DirectMockerTestBaseInheritance = "FMOQ0037";

        /// <summary>
        /// Avoid unnecessary helper indirection around inherited MockerTestBase&lt;T&gt; members.
        /// </summary>
        public const string UnnecessaryMockerTestBaseHelperIndirection = "FMOQ0038";
    }
}
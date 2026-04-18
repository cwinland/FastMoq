using Microsoft.CodeAnalysis;

namespace FastMoq.Analyzers
{
    internal static class DiagnosticDescriptors
    {
        private const string Category = "FastMoq";

        public static readonly DiagnosticDescriptor UseProviderFirstObjectAccess = new(
            DiagnosticIds.UseProviderFirstObjectAccess,
            "Use provider-first object access",
            "Use '{0}' instead of '.Object' on tracked FastMoq mocks",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Tracked FastMoq mocks should use provider-first instance access instead of raw Mock.Object retrieval.");

        public static readonly DiagnosticDescriptor UseProviderFirstReset = new(
            DiagnosticIds.UseProviderFirstReset,
            "Use provider-first reset",
            "Use '{0}' instead of provider-native Reset() on tracked FastMoq mocks",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Tracked FastMoq mocks expose Reset() directly through IFastMock and should not require provider-native reset calls.");

        public static readonly DiagnosticDescriptor UseVerifyLogged = new(
            DiagnosticIds.UseVerifyLogged,
            "Use VerifyLogged for provider-safe logger assertions",
            "Use '{0}' instead of VerifyLogger(...) for provider-safe logger assertions",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Prefer Mocker.VerifyLogged(...) over provider-specific VerifyLogger(...) when the assertion can stay provider-safe.");

        public static readonly DiagnosticDescriptor UseConsistentMockRetrieval = new(
            DiagnosticIds.UseConsistentMockRetrieval,
            "Use provider-first mock retrieval consistently",
            "Document already uses GetOrCreateMock<T>(); convert this GetMock<T>() call to keep retrieval consistent",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "When a file or helper layer already uses provider-first retrieval, remaining obsolete GetMock<T>() calls are usually migration leftovers that should be normalized.");

        public static readonly DiagnosticDescriptor UseProviderFirstMockRetrieval = new(
            DiagnosticIds.UseProviderFirstMockRetrieval,
            "Use provider-first mock retrieval",
            "Use 'GetOrCreateMock<T>()' instead of obsolete 'GetMock<T>()' when the call only needs a tracked FastMoq mock",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "GetMock<T>() is a Moq compatibility surface. Prefer GetOrCreateMock<T>() for provider-first tracked mock retrieval when the call site does not depend on raw Mock<T>-shaped behavior. Mixed files continue to use the stronger consistency diagnostic.");

        public static readonly DiagnosticDescriptor AvoidLegacyRequiredMockRetrieval = new(
            DiagnosticIds.AvoidLegacyRequiredMockRetrieval,
            "Avoid legacy GetRequiredMock retrieval",
            "'GetRequiredMock(...)' is a legacy Moq compatibility API. Prefer provider-first retrieval based on intent: 'GetRequiredTrackedMock(...)', 'TryGetTrackedMock(...)', 'GetOrCreateMock(...)', 'GetObject(...)', or 'GetRequiredObject(...)'.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "GetRequiredMock(...) is a Moq-only compatibility path with no single safe automatic replacement. Use GetRequiredTrackedMock(...) or TryGetTrackedMock(...) when the dependency must already be tracked, GetOrCreateMock(...) when creating the tracked mock is acceptable, or GetObject(...) / GetRequiredObject(...) when only the instance is needed.");

        public static readonly DiagnosticDescriptor AvoidLegacyMockCreationAndLifecycleApis = new(
            DiagnosticIds.AvoidLegacyMockCreationAndLifecycleApis,
            "Avoid legacy Moq creation and lifecycle APIs",
            "API '{0}(...)' is a legacy Moq compatibility surface. Prefer {1}.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Compatibility creation and lifecycle APIs such as CreateMock(...), CreateMockInstance(...), CreateDetachedMock(...), AddMock(...), and RemoveMock(...) are not the preferred provider-first end state. Prefer tracked provider-first mocks, AddType(...), or dedicated Mocker scopes instead of expanding the legacy Mock<T>-shaped surface.");

        public static readonly DiagnosticDescriptor UseExplicitOptionalParameterResolution = new(
            DiagnosticIds.UseExplicitOptionalParameterResolution,
            "Use explicit optional-parameter resolution",
            "Use '{0}' instead of the MockOptional compatibility property",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "MockOptional is a compatibility alias. Prefer explicit OptionalParameterResolution values in current FastMoq code.");

        public static readonly DiagnosticDescriptor ReplaceInitializeCompatibilityWrapper = new(
            DiagnosticIds.ReplaceInitializeCompatibilityWrapper,
            "Replace Initialize<T>(...) compatibility wrapper",
            "Use '{0}' instead of Initialize<T>(...)",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Initialize<T>(...) is a legacy wrapper over GetMock<T>(...) and should be replaced with the direct API.");

        public static readonly DiagnosticDescriptor AvoidStrictCompatibilityProperty = new(
            DiagnosticIds.AvoidStrictCompatibilityProperty,
            "Avoid compatibility Strict property",
            "Strict is a compatibility alias; use explicit Behavior or preset APIs instead",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Use Behavior.Enabled with MockFeatures.FailOnUnconfigured for narrow strictness, or UseStrictPreset() for the broader strict profile.");

        public static readonly DiagnosticDescriptor UseTimesSpecAtHelperBoundary = new(
            DiagnosticIds.UseTimesSpecAtHelperBoundary,
            "Use TimesSpec in helper signatures",
            "Use 'TimesSpec' instead of '{0}' in shared helper signatures",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Shared test helpers should prefer provider-neutral TimesSpec over Moq Times or Func<Times> parameters.");

        public static readonly DiagnosticDescriptor PreferSetupOptionsHelper = new(
            DiagnosticIds.PreferSetupOptionsHelper,
            "Prefer SetupOptions for IOptions test setup",
            "Use '{0}' instead of manual IOptions<T> setup",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Prefer SetupOptions<T>(...) over repeated IOptions<T> AddType(...)/Options.Create(...) and Setup(x => x.Value).Returns(...) patterns when registering test options values.");

        public static readonly DiagnosticDescriptor PreferPropertySetterCaptureHelper = new(
            DiagnosticIds.PreferPropertySetterCaptureHelper,
            "Prefer provider-neutral property setter capture",
            "Prefer '{0}' instead of 'SetupSet(...)' when the test only needs setter observation or value capture",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Prefer AddPropertySetterCapture<TService, TValue>(...) for simple interface-property setter capture flows, or a fake plus PropertyValueCapture<TValue> when the test needs a broader replacement than Moq-specific SetupSet(...).");

        public static readonly DiagnosticDescriptor PreferPropertyStateHelper = new(
            DiagnosticIds.PreferPropertyStateHelper,
            "Prefer provider-neutral property state",
            "Prefer '{0}' instead of 'SetupAllProperties()' when the test only needs lightweight property state",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Prefer AddPropertyState<TService>(...) for simple interface-property state flows, or a concrete fake registered with AddType(...) when the old Moq setup relied on broader class or provider-specific property behavior.");

        public static readonly DiagnosticDescriptor SelectProviderBeforeProviderSpecificApi = new(
            DiagnosticIds.SelectProviderBeforeProviderSpecificApi,
            "Select a provider before using provider-specific FastMoq APIs",
            "API '{0}' requires FastMoq provider '{1}', but this project does not select it. Reflection remains the default. Use [assembly: FastMoqDefaultProvider(\"{1}\")], [assembly: FastMoqRegisterProvider(\"{1}\", typeof(...), SetAsDefault = true)], Push(\"{1}\"), SetDefault(\"{1}\"), or Register(\"{1}\", ..., setAsDefault: true).",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Provider-specific FastMoq APIs require an explicit provider selection because reflection remains the default in v4. Assembly-level defaults can use FastMoqDefaultProviderAttribute when the provider name is already resolvable, or FastMoqRegisterProviderAttribute when registration and selection need to happen together.");

        public static readonly DiagnosticDescriptor PreferTypedProviderExtensions = new(
            DiagnosticIds.PreferTypedProviderExtensions,
            "Prefer typed provider extensions over raw native mock access",
            "Prefer '{0}' over raw '{1}' access when this file already uses FastMoq provider '{2}' APIs",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Prefer AsMoq()/AsNSubstitute() over direct NativeMock or GetNativeMock(...) access when the test intentionally uses provider-specific FastMoq APIs.");

        public static readonly DiagnosticDescriptor PreferWebTestHelpers = new(
            DiagnosticIds.PreferWebTestHelpers,
            "Prefer FastMoq.Web helpers for web test setup",
            "Use '{0}' instead of hand-rolled '{1}' setup",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Prefer CreateHttpContext(...), CreateControllerContext(...), AddHttpContext(...), AddHttpContextAccessor(...), SetRequestBody(...), and SetRequestJsonBody(...) over hand-rolled AddType(...), DefaultHttpContext, or direct HttpRequest body wiring for common web test primitives.");

        public static readonly DiagnosticDescriptor PreferProviderNeutralHttpHelpers = new(
            DiagnosticIds.PreferProviderNeutralHttpHelpers,
            "Prefer provider-neutral HTTP helpers",
            "Prefer '{0}' instead of '{1}' when the test only needs request/response behavior",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Prefer WhenHttpRequest(...) or WhenHttpRequestJson(...) over Moq-specific protected HttpMessageHandler compatibility helpers when the test only configures request and response behavior.");

        public static readonly DiagnosticDescriptor PreferTypedServiceProviderHelpers = new(
            DiagnosticIds.PreferTypedServiceProviderHelpers,
            "Prefer typed IServiceProvider helpers",
            "Prefer FastMoq's typed service-provider helpers instead of '{0}' for framework service-provider test setup",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Mocking IServiceProvider, IServiceScopeFactory, or IServiceScope directly often creates one-object-for-all-types shims. Prefer CreateTypedServiceProvider(...), CreateTypedServiceScope(...), AddServiceProvider(...), AddServiceScope(...), CreateFunctionContextInstanceServices(...), or AddFunctionContextInstanceServices(...) so framework code resolves services by requested type.");

        public static readonly DiagnosticDescriptor PreferFunctionContextExecutionHelpers = new(
            DiagnosticIds.PreferFunctionContextExecutionHelpers,
            "Prefer FunctionContext execution helpers",
            "Prefer FastMoq's FunctionContext execution helpers instead of '{0}' for Azure Functions execution metadata setup",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Prefer AddFunctionContextInvocationId(...) over raw provider-native InvocationId setup so Azure Functions tests stay on FastMoq's helper surface and package-aware guidance remains consistent.");

        public static readonly DiagnosticDescriptor PreferKnownTypeRegistrations = new(
            DiagnosticIds.PreferKnownTypeRegistrations,
            "Prefer AddKnownType for framework-style resolution",
            "Use 'AddKnownType(...)' instead of '{0}' when the registration depends on requested-type or framework-style resolution context",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Context-aware AddType overloads are a compatibility path. Prefer AddKnownType(...) when a registration depends on requested-type resolution context, mock post-processing, or framework-style defaults.");

        public static readonly DiagnosticDescriptor PreserveKeyedServiceDistinctness = new(
            DiagnosticIds.PreserveKeyedServiceDistinctness,
            "Preserve keyed same-type dependencies",
            "Type '{0}' has multiple keyed '{1}' constructor dependencies. Avoid unkeyed '{2}' here; use keyed mocks, AddKeyedType(...), or explicit separate doubles instead.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "When the system under test has multiple same-type keyed constructor dependencies, an unkeyed test double can collapse distinct roles into one object and hide routing bugs. Prefer keyed FastMoq setup or explicit separate doubles.");

        public static readonly DiagnosticDescriptor PreserveTrackedResolutionDuringAddTypeMigration = new(
            DiagnosticIds.PreserveTrackedResolutionDuringAddTypeMigration,
            "Preserve tracked resolution when migrating to AddType",
            "AddType<{0}>(...) replaces tracked resolution for '{0}', but this file still uses '{1}' for the same service. Keep a tracked mock/helper path instead of rewriting this dependency to AddType(...).",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "AddType(...) is for concrete replacement. When a migrated helper still relies on tracked resolution, property-state helpers, or setter-capture helpers for the same service, replacing the tracked path with AddType(...) can silently change behavior. Prefer GetOrCreateMock<T>(), GetRequiredTrackedMock<T>(), AddPropertyState<TService>(...), or AddPropertySetterCapture<TService, TValue>(...) in those flows.");

        public static readonly DiagnosticDescriptor RequireExplicitMoqOnboarding = new(
            DiagnosticIds.RequireExplicitMoqOnboarding,
            "Add explicit Moq onboarding for legacy compatibility usage",
            "Legacy Moq-shaped FastMoq API '{0}' is in use without explicit Moq onboarding. If this project stays on FastMoq.Core, add 'FastMoq.Provider.Moq' and select 'moq' with [assembly: FastMoqDefaultProvider(\"moq\")] or [assembly: FastMoqRegisterProvider(\"moq\", typeof(MoqMockingProvider), SetAsDefault = true)].",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Core-only projects can keep legacy Moq-shaped FastMoq usage during migration, but that path should be explicit. Add the FastMoq.Provider.Moq package when staying on FastMoq.Core, and declare moq as the selected provider so future cleanup and analyzer guidance stay deterministic.");

        public static readonly DiagnosticDescriptor UseProviderFirstVerify = new(
            DiagnosticIds.UseProviderFirstVerify,
            "Use provider-first verification",
            "Use '{0}' instead of provider-native Verify(...) on tracked FastMoq mocks",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Tracked FastMoq mocks should use Mocker.Verify<T>(...) and TimesSpec instead of Moq Verify(...) when the translation is mechanical and provider-neutral." );

        public static readonly DiagnosticDescriptor AvoidBareTrackedVerify = new(
            DiagnosticIds.AvoidBareTrackedVerify,
            "Avoid bare tracked Verify()",
            "Tracked provider-native Verify() keeps this test on the Moq surface. Replace it with explicit Mocker.Verify<T>(...) assertions or remove the redundant Verify().",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Bare Verify() on tracked FastMoq mocks usually reflects Verifiable()-style migration leftovers and should be made explicit or removed instead of staying on the provider-native surface.");

        public static readonly DiagnosticDescriptor AvoidTrackedMockShimAlias = new(
            DiagnosticIds.AvoidTrackedMockShimAlias,
            "Avoid tracked Mock<T> shim aliases",
            "Tracked alias '{0}' is typed as 'Mock<T>' and keeps later work on the Moq surface. Prefer an IFastMock<T> handle plus Mocker.Verify<T>(...).",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Properties, fields, and locals typed as Mock<T> but sourced from tracked FastMoq mocks unnecessarily keep verification and helper usage provider-specific.");

        public static readonly DiagnosticDescriptor AvoidRawMockCreationInFastMoqSuites = new(
            DiagnosticIds.AvoidRawMockCreationInFastMoqSuites,
            "Avoid raw Mock<T> creation in FastMoq suites",
            "Raw '{0}' inside FastMoq test infrastructure is usually a migration leftover. Prefer {1}.",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "When a test already uses Mocker or MockerTestBase infrastructure, raw new Mock<T>() creation is usually a leftover from older Moq-first patterns. Prefer tracked GetOrCreateMock<T>() for the single-instance path, or a standalone provider-first handle when the test truly needs another independent mock of the same type.");

        public static readonly DiagnosticDescriptor ReferenceFastMoqHelperPackage = new(
            DiagnosticIds.ReferenceFastMoqHelperPackage,
            "Add required FastMoq helper package",
            "'{0}' lives in '{1}'. Add package '{1}' and import '{2}'.",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Some provider-first helper replacements live in split FastMoq packages such as FastMoq.Web or FastMoq.AzureFunctions. When the helper package is missing, guide the user to the package and namespace instead of surfacing a non-actionable rewrite diagnostic.");
    }
}
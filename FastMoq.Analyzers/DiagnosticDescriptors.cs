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
            description: "Prefer CreateHttpContext(...), CreateControllerContext(...), AddHttpContext(...), and AddHttpContextAccessor(...) over hand-rolled AddType(...), DefaultHttpContext, or ControllerContext setup for common web test primitives.");

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
            description: "Mocking IServiceProvider directly or manually wiring FunctionContext.InstanceServices often creates one-object-for-all-types shims. Prefer CreateTypedServiceProvider(...), AddServiceProvider(...), CreateFunctionContextInstanceServices(...), or AddFunctionContextInstanceServices(...) so framework code resolves services by requested type.");

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
    }
}
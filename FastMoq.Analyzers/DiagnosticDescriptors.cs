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
            description: "Prefer CreateHttpContext(...), CreateControllerContext(...), AddHttpContext(...), and AddHttpContextAccessor(...) over hand-rolled AddType(...) setup for common web test primitives.");

        public static readonly DiagnosticDescriptor PreferProviderNeutralHttpHelpers = new(
            DiagnosticIds.PreferProviderNeutralHttpHelpers,
            "Prefer provider-neutral HTTP helpers",
            "Prefer '{0}' instead of '{1}' when the test only needs request/response behavior",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Prefer WhenHttpRequest(...) or WhenHttpRequestJson(...) over Moq-specific protected HttpMessageHandler compatibility helpers when the test only configures request and response behavior.");
    }
}
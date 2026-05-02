using Microsoft.CodeAnalysis;

namespace FastMoq.Analyzers
{
    internal static class DiagnosticDescriptors
    {
        private const string Category = "FastMoq";

        public static readonly DiagnosticDescriptor UseProviderFirstObjectAccess = new(
            DiagnosticIds.UseProviderFirstObjectAccess,
            "Use provider-first dependency instance access",
            "Use '{0}' for the dependency instance instead of using '.Object' on a tracked FastMoq mock",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "When the code only needs the resolved dependency instance, stay on the provider-first surface instead of going through Mock<T>.Object. Use .Instance when you already have an IFastMock<T> handle, or use GetObject<T>() / GetRequiredObject<T>() when the code is retrieving the dependency from Mocker. Keep AsMoq().Object only for a deliberate local Moq-only compatibility pocket.");

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

        public static readonly DiagnosticDescriptor PreferLoggerFactoryHelpers = new(
            DiagnosticIds.PreferLoggerFactoryHelpers,
            "Prefer AddLoggerFactory for output-helper logger registration",
            "Use '{0}' instead of direct output-helper logger registration",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Prefer AddLoggerFactory(...) over direct AddType<ILoggerFactory>(new ...output-helper...), AddType<ILogger>(new ...output-helper...), or AddType<ILogger<T>>(new ...output-helper...) registrations when the logger registration only mirrors logs into xUnit-style output helpers.");

        public static readonly DiagnosticDescriptor PreferSetupLoggerCallbackHelper = new(
            DiagnosticIds.PreferSetupLoggerCallbackHelper,
            "Prefer SetupLoggerCallback for normalized logger mirroring",
            "Use '{0}' instead of provider-specific ILogger.Log<TState> setup when the callback only needs normalized log output",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Prefer Mocker.SetupLoggerCallback(...) over tracked ILogger.Log<TState> Setup(...).Callback(...) chains when the callback only consumes log level, event id, formatted message text, or exception data. Keep the Moq compatibility path when the test needs raw structured logger state.");

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
            "Resolve the matching provider before using provider-specific FastMoq APIs",
            "API '{0}' requires FastMoq provider '{1}', but this project does not resolve '{1}' as the effective provider. Use [assembly: FastMoqDefaultProvider(\"{1}\")], [assembly: FastMoqRegisterProvider(\"{1}\", typeof(...), SetAsDefault = true)], Push(\"{1}\"), SetDefault(\"{1}\"), or Register(\"{1}\", ..., setAsDefault: true).",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Provider-specific FastMoq APIs require the matching effective provider. FastMoq can infer a single registered non-reflection provider when that provider is visible through compile-time registration metadata, but once multiple providers are visible or the intended provider is not registered, select it explicitly at assembly scope or inside the local execution scope.");

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
            "Resolve Moq provider selection for legacy compatibility usage",
            "Legacy Moq-shaped FastMoq API '{0}' is in use, but this project does not resolve 'moq' as the effective provider. If the Moq provider is not already referenced, add 'FastMoq.Provider.Moq', then select 'moq' with [assembly: FastMoqDefaultProvider(\"moq\")] or [assembly: FastMoqRegisterProvider(\"moq\", typeof(MoqMockingProvider), SetAsDefault = true)] when provider selection is otherwise ambiguous.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Legacy Moq-shaped FastMoq usage requires the Moq provider to be available and selected as the effective provider. A single compile-time-visible provider registration can satisfy that automatically, but when the Moq provider is missing or multiple providers are visible, add the Moq provider package if needed and select moq explicitly so the compatibility path stays deterministic.");

        public static readonly DiagnosticDescriptor UseProviderFirstVerify = new(
            DiagnosticIds.UseProviderFirstVerify,
            "Use provider-first verification",
            "Use '{0}' instead of provider-native Verify(...) on FastMoq mock handles",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Tracked FastMoq mocks should use Mocker.Verify<T>(...) and detached IFastMock<T> handles should use MockingProviderRegistry.Default.Verify(...) instead of Moq Verify(...) when the translation is mechanical and provider-neutral." );

        public static readonly DiagnosticDescriptor AvoidBareTrackedVerify = new(
            DiagnosticIds.AvoidBareTrackedVerify,
            "Avoid bare tracked Verify()",
            "Tracked provider-native Verify() keeps this test on the Moq surface. Replace it with explicit Mocker.Verify<T>(...) assertions or remove the redundant Verify().",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Bare Verify() on tracked FastMoq mocks usually reflects Verifiable()-style migration leftovers and should be made explicit or removed instead of staying on the provider-native surface.");

        public static readonly DiagnosticDescriptor AvoidFastMockVerifyHelperWrappers = new(
            DiagnosticIds.AvoidFastMockVerifyHelperWrappers,
            "Avoid IFastMock.Verify helper wrappers",
            "Avoid '{0}(...)' wrappers on IFastMock<T> that only forward to FastMoq verification helpers. Keep tracked-versus-detached verification explicit at the call site instead.",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Custom IFastMock.Verify wrappers hide tracked-versus-detached verification intent and spread another helper surface across the suite even when they only forward to FastMoq's official verification APIs. Prefer calling Mocker.Verify<T>(...) or MockingProviderRegistry.Default.Verify(...) directly at the call site instead of wrapping them behind a new IFastMock-centric helper API.");

        public static readonly DiagnosticDescriptor AvoidProviderSpecificFastMockVerifyHelperWrappers = new(
            DiagnosticIds.AvoidProviderSpecificFastMockVerifyHelperWrappers,
            "Avoid provider-specific IFastMock.Verify wrappers",
            "Wrapper '{0}(...)' reintroduces provider-specific verification through IFastMock<T>. Use provider-first verification at the call site instead of routing through AsMoq().Verify(...) or provider-specific Times adapters.",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "IFastMock.Verify wrappers that route through AsMoq().Verify(...), Moq Verify(...), or TimesSpec-to-Times conversion helpers hide tracked-versus-detached intent and pull provider-specific verification back into shared helper code. Keep those provider-specific escape hatches local to the call site instead of baking them into a new wrapper API.");

        public static readonly DiagnosticDescriptor PreferSharedMockFileSystem = new(
            DiagnosticIds.PreferSharedMockFileSystem,
            "Prefer the shared FastMoq file system",
            "'{0}' creates a new MockFileSystem even though this MockerTestBase-based test can reuse FastMoq's shared GetFileSystem() instance",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "MockerTestBase already exposes a shared in-memory file system through GetFileSystem(). Creating a fresh MockFileSystem for an IFileSystem slot inside that test-base flow can desynchronize constructor injection from the rest of the test setup. Prefer the shared FastMoq file system unless the test intentionally needs an independent file system instance.");

        /// <summary>
        /// Warns when a provider-first Verify expression still uses a Moq matcher that can be rewritten directly to FastArg.
        /// </summary>
        public static readonly DiagnosticDescriptor UseFastArgMatcherInProviderFirstVerify = new(
            DiagnosticIds.UseFastArgMatcherInProviderFirstVerify,
            "Use FastArg matchers in provider-first Verify",
            "Use '{0}' instead of '{1}' inside provider-first Verify(...) so the assertion stays provider-neutral",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Once a test has moved to Mocker.Verify<T>(...) or MockingProviderRegistry.Default.Verify(...), nesting Moq It.* matchers inside the verified expression keeps the assertion provider-specific. Replace direct It.IsAny(...) and compatible It.Is(...) usages with FastArg matchers when the substitution is mechanical.");

        /// <summary>
        /// Informs when a provider-first Verify expression still uses a Moq matcher that does not have a direct FastArg replacement.
        /// </summary>
        public static readonly DiagnosticDescriptor AvoidUnsupportedMoqMatcherInProviderFirstVerify = new(
            DiagnosticIds.AvoidUnsupportedMoqMatcherInProviderFirstVerify,
            "Avoid unsupported Moq matchers in provider-first Verify",
            "'{0}' is a Moq-specific matcher inside provider-first Verify(...). Prefer a FastArg matcher or another provider-neutral assertion shape when possible.",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Provider-first Verify(...) expressions should avoid Moq-specific matcher helpers. When a direct FastArg replacement does not exist for a given It.* helper, keep the assertion provider-neutral by restructuring the verification or using a provider-neutral value or predicate matcher instead.");

        public static readonly DiagnosticDescriptor AvoidTrackedMockShimAlias = new(
            DiagnosticIds.AvoidTrackedMockShimAlias,
            "Avoid verification-only Mock<T> aliases for tracked mocks",
            "Tracked alias '{0}' is only used for Moq Verify calls. Prefer an IFastMock<T> handle or verify directly with Mocker.Verify<T>(...).",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "This rule applies when a field, property, or local is typed as Mock<T>, comes from a tracked FastMoq mock, and is only used later for Moq Verify(...) calls. Replace the alias with an IFastMock<T> handle, or keep verification explicit at the call site with Mocker.Verify<T>(...). Keep a Mock<T> alias only when that symbol also needs Moq-specific setup or other raw Mock<T> behavior.");

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

        public static readonly DiagnosticDescriptor DirectMockerTestBaseInheritance = new(
            DiagnosticIds.DirectMockerTestBaseInheritance,
            "Prefer inheritance over MockerTestBase helper composition",
            "Nested helper '{2}' composes MockerTestBase<{1}> through an instance wrapper. Prefer inheritance in test class '{0}' directly or through a dedicated shared test base.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "When a local nested helper only wraps MockerTestBase<TComponent> for a single outer test class, prefer direct inheritance on the outer class or a manually authored shared intermediate base instead of keeping the helper as an extra instance-composition layer. Phase 1 keeps the automatic fix narrow and only rewrites clearly mechanical local wrapper shapes.");

        public static readonly DiagnosticDescriptor UnnecessaryMockerTestBaseHelperIndirection = new(
            DiagnosticIds.UnnecessaryMockerTestBaseHelperIndirection,
            "Avoid unnecessary MockerTestBase helper indirection",
            "Helper member '{0}' only forwards to inherited MockerTestBase behavior through '{1}'. Prefer the inherited surface directly when the wrapper adds no meaningful behavior.",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Thin aliases such as helper-backed Component or Mocks accessors, and helper members that only forward to inherited tracked-mock retrieval, can add another indirection layer without improving behavior. Keep the advisory rule conservative and leave readability- or behavior-improving wrappers alone.");
    }
}
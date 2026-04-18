using FastMoq.Analyzers;
using FastMoq.Analyzers.Analyzers;
using FastMoq.Analyzers.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace FastMoq.Analyzers.Tests
{
    public class MigrationAnalyzerTests
    {
        private readonly FastMoqMigrationCodeFixProvider codeFixProvider = new();

        public static TheoryData<DiagnosticAnalyzer, DiagnosticDescriptor> AnalyzerDescriptorPairs => new()
        {
            { new TrackedMockObjectAnalyzer(), DiagnosticDescriptors.UseProviderFirstObjectAccess },
            { new TrackedMockResetAnalyzer(), DiagnosticDescriptors.UseProviderFirstReset },
            { new VerifyLoggerAnalyzer(), DiagnosticDescriptors.UseVerifyLogged },
            { new MixedMockRetrievalAnalyzer(), DiagnosticDescriptors.UseConsistentMockRetrieval },
            { new GetMockCompatibilityAnalyzer(), DiagnosticDescriptors.UseProviderFirstMockRetrieval },
            { new GetRequiredMockCompatibilityAnalyzer(), DiagnosticDescriptors.AvoidLegacyRequiredMockRetrieval },
            { new LegacyMoqCreationLifecycleAnalyzer(), DiagnosticDescriptors.AvoidLegacyMockCreationAndLifecycleApis },
            { new MockOptionalAnalyzer(), DiagnosticDescriptors.UseExplicitOptionalParameterResolution },
            { new InitializeCompatibilityAnalyzer(), DiagnosticDescriptors.ReplaceInitializeCompatibilityWrapper },
            { new StrictCompatibilityAnalyzer(), DiagnosticDescriptors.AvoidStrictCompatibilityProperty },
            { new TimesSpecHelperBoundaryAnalyzer(), DiagnosticDescriptors.UseTimesSpecAtHelperBoundary },
            { new OptionsSetupAnalyzer(), DiagnosticDescriptors.PreferSetupOptionsHelper },
            { new SetupSetAnalyzer(), DiagnosticDescriptors.PreferPropertySetterCaptureHelper },
            { new SetupAllPropertiesAnalyzer(), DiagnosticDescriptors.PreferPropertyStateHelper },
            { new TrackedMockVerificationAnalyzer(), DiagnosticDescriptors.UseProviderFirstVerify },
            { new BareTrackedVerifyAnalyzer(), DiagnosticDescriptors.AvoidBareTrackedVerify },
            { new TrackedMockShimAnalyzer(), DiagnosticDescriptors.AvoidTrackedMockShimAlias },
            { new RawMockCreationAnalyzer(), DiagnosticDescriptors.AvoidRawMockCreationInFastMoqSuites },
            { new ProviderBootstrapAnalyzer(), DiagnosticDescriptors.SelectProviderBeforeProviderSpecificApi },
            { new NativeMockAuthoringAnalyzer(), DiagnosticDescriptors.PreferTypedProviderExtensions },
            { new WebHelperAuthoringAnalyzer(), DiagnosticDescriptors.PreferWebTestHelpers },
            { new MissingHelperPackageAnalyzer(), DiagnosticDescriptors.ReferenceFastMoqHelperPackage },
            { new HttpRequestHelperAuthoringAnalyzer(), DiagnosticDescriptors.PreferProviderNeutralHttpHelpers },
            { new ServiceProviderShimAnalyzer(), DiagnosticDescriptors.PreferTypedServiceProviderHelpers },
            { new ServiceProviderShimAnalyzer(), DiagnosticDescriptors.PreferFunctionContextExecutionHelpers },
            { new KnownTypeAuthoringAnalyzer(), DiagnosticDescriptors.PreferKnownTypeRegistrations },
            { new KeyedDependencyAnalyzer(), DiagnosticDescriptors.PreserveKeyedServiceDistinctness },
            { new TrackedAddTypeMigrationAnalyzer(), DiagnosticDescriptors.PreserveTrackedResolutionDuringAddTypeMigration },
            { new LegacyMoqOnboardingAnalyzer(), DiagnosticDescriptors.RequireExplicitMoqOnboarding },
        };

        public static TheoryData<DiagnosticDescriptor, DiagnosticSeverity> DescriptorSeverityPairs => new()
        {
            { DiagnosticDescriptors.UseProviderFirstObjectAccess, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.UseProviderFirstReset, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.UseVerifyLogged, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.UseConsistentMockRetrieval, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.UseProviderFirstMockRetrieval, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.AvoidLegacyRequiredMockRetrieval, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.AvoidLegacyMockCreationAndLifecycleApis, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.UseExplicitOptionalParameterResolution, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.ReplaceInitializeCompatibilityWrapper, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.AvoidStrictCompatibilityProperty, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.UseTimesSpecAtHelperBoundary, DiagnosticSeverity.Info },
            { DiagnosticDescriptors.PreferSetupOptionsHelper, DiagnosticSeverity.Info },
            { DiagnosticDescriptors.PreferPropertySetterCaptureHelper, DiagnosticSeverity.Info },
            { DiagnosticDescriptors.PreferPropertyStateHelper, DiagnosticSeverity.Info },
            { DiagnosticDescriptors.UseProviderFirstVerify, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.AvoidBareTrackedVerify, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.AvoidTrackedMockShimAlias, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.AvoidRawMockCreationInFastMoqSuites, DiagnosticSeverity.Info },
            { DiagnosticDescriptors.SelectProviderBeforeProviderSpecificApi, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.PreferTypedProviderExtensions, DiagnosticSeverity.Info },
            { DiagnosticDescriptors.PreferWebTestHelpers, DiagnosticSeverity.Info },
            { DiagnosticDescriptors.ReferenceFastMoqHelperPackage, DiagnosticSeverity.Info },
            { DiagnosticDescriptors.PreferProviderNeutralHttpHelpers, DiagnosticSeverity.Info },
            { DiagnosticDescriptors.PreferTypedServiceProviderHelpers, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.PreferFunctionContextExecutionHelpers, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.PreferKnownTypeRegistrations, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.PreserveKeyedServiceDistinctness, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.PreserveTrackedResolutionDuringAddTypeMigration, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.RequireExplicitMoqOnboarding, DiagnosticSeverity.Warning },
        };

        [Theory]
        [MemberData(nameof(AnalyzerDescriptorPairs))]
        public void Analyzer_ShouldExposeExpectedSupportedDescriptor(DiagnosticAnalyzer analyzer, DiagnosticDescriptor expectedDescriptor)
        {
            Assert.Contains(expectedDescriptor, analyzer.SupportedDiagnostics);
        }

        [Theory]
        [MemberData(nameof(DescriptorSeverityPairs))]
        public void Descriptor_ShouldExposeExpectedDefaultSeverity(DiagnosticDescriptor descriptor, DiagnosticSeverity expectedSeverity)
        {
            Assert.Equal(expectedSeverity, descriptor.DefaultSeverity);
        }

        [Fact]
        public async Task TrackedMockObjectAnalyzer_ShouldReportAndFix_GetMockObjectUsage()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.Extensions.Logging;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var logger = Mocks.GetMock<ILogger<Sample>>().Object;
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new TrackedMockObjectAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.UseProviderFirstObjectAccess));
            Assert.Equal(DiagnosticIds.UseProviderFirstObjectAccess, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new TrackedMockObjectAnalyzer(), codeFixProvider, DiagnosticIds.UseProviderFirstObjectAccess);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using Microsoft.Extensions.Logging;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var logger = Mocks.GetObject<ILogger<Sample>>();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task TrackedMockResetAnalyzer_ShouldReportAndFix_ResetUsage()
        {
            const string SOURCE = @"
using FastMoq;

class Sample
{
    private interface IService
    {
    }

    void Execute(Mocker Mocks)
    {
        var mock = Mocks.GetMock<IService>();
        mock.Reset();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new TrackedMockResetAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.UseProviderFirstReset));
            Assert.Equal(DiagnosticIds.UseProviderFirstReset, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new TrackedMockResetAnalyzer(), codeFixProvider, DiagnosticIds.UseProviderFirstReset);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;

class Sample
{
    private interface IService
    {
    }

    void Execute(Mocker Mocks)
    {
        var mock = Mocks.GetMock<IService>();
        Mocks.GetOrCreateMock<IService>().Reset();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task VerifyLoggerAnalyzer_ShouldReportAndFix_SimpleVerifyLoggerUsage()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Extensions;
using Microsoft.Extensions.Logging;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var logger = Mocks.GetMock<ILogger<Sample>>();
        logger.VerifyLogger(LogLevel.Information, ""processed order"", 1);
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new VerifyLoggerAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.UseVerifyLogged));
            Assert.Equal(DiagnosticIds.UseVerifyLogged, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new VerifyLoggerAnalyzer(), codeFixProvider, DiagnosticIds.UseVerifyLogged);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Extensions;
using Microsoft.Extensions.Logging;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var logger = Mocks.GetMock<ILogger<Sample>>();
        Mocks.VerifyLogged(LogLevel.Information, ""processed order"", 1);
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task OptionsSetupAnalyzer_ShouldReportAndFix_AddTypeOptionsCreateUsage()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.Extensions.Options;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.AddType<IOptions<SampleOptions>>(Options.Create(new SampleOptions { RetryCount = 3 }), true);
    }
}

class SampleOptions
{
    public int RetryCount { get; set; }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new OptionsSetupAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferSetupOptionsHelper));
            Assert.Equal(DiagnosticIds.PreferSetupOptionsHelper, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new OptionsSetupAnalyzer(), codeFixProvider, DiagnosticIds.PreferSetupOptionsHelper);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using Microsoft.Extensions.Options;
using FastMoq.Extensions;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.SetupOptions<SampleOptions>(new SampleOptions { RetryCount = 3 }, replace: true);
    }
}

class SampleOptions
{
    public int RetryCount { get; set; }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task OptionsSetupAnalyzer_ShouldReportAndFix_IOptionsValueSetupUsageWithoutExistingExtensionsUsing()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.Extensions.Options;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.GetMock<IOptions<SampleOptions>>()
            .Setup(x => x.Value)
            .Returns(new SampleOptions { RetryCount = 5 });
    }
}

class SampleOptions
{
    public int RetryCount { get; set; }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new OptionsSetupAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferSetupOptionsHelper));
            Assert.Equal(DiagnosticIds.PreferSetupOptionsHelper, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new OptionsSetupAnalyzer(), codeFixProvider, DiagnosticIds.PreferSetupOptionsHelper);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using Microsoft.Extensions.Options;
using Moq;
using FastMoq.Extensions;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.SetupOptions<SampleOptions>(new SampleOptions { RetryCount = 5 });
    }
}

class SampleOptions
{
    public int RetryCount { get; set; }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task OptionsSetupAnalyzer_ShouldReportAndFix_IOptionsValueSetupUsage()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Extensions;
using Microsoft.Extensions.Options;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.GetMock<IOptions<SampleOptions>>()
            .Setup(x => x.Value)
            .Returns(new SampleOptions { RetryCount = 5 });
    }
}

class SampleOptions
{
    public int RetryCount { get; set; }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new OptionsSetupAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferSetupOptionsHelper));
            Assert.Equal(DiagnosticIds.PreferSetupOptionsHelper, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new OptionsSetupAnalyzer(), codeFixProvider, DiagnosticIds.PreferSetupOptionsHelper);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Extensions;
using Microsoft.Extensions.Options;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.SetupOptions<SampleOptions>(new SampleOptions { RetryCount = 5 });
    }
}

class SampleOptions
{
    public int RetryCount { get; set; }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task OptionsSetupAnalyzer_ShouldReportAndFix_DeferredIOptionsValueSetupUsage()
        {
            const string SOURCE = @"
using FastMoq;
using Moq;
using Microsoft.Extensions.Options;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var nextRetryCount = 1;
        Mocks.GetMock<IOptions<SampleOptions>>()
            .Setup(x => x.Value)
            .Returns(() => new SampleOptions { RetryCount = nextRetryCount++ });
    }
}

class SampleOptions
{
    public int RetryCount { get; set; }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new OptionsSetupAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferSetupOptionsHelper));
            Assert.Equal(DiagnosticIds.PreferSetupOptionsHelper, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new OptionsSetupAnalyzer(), codeFixProvider, DiagnosticIds.PreferSetupOptionsHelper);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using Moq;
using Microsoft.Extensions.Options;
using FastMoq.Extensions;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var nextRetryCount = 1;
        Mocks.SetupOptions<SampleOptions>(() => new SampleOptions { RetryCount = nextRetryCount++ });
    }
}

class SampleOptions
{
    public int RetryCount { get; set; }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task MixedMockRetrievalAnalyzer_ShouldReportAndFix_GetMockUsageInProviderFirstDocument()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.Extensions.Logging;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var tracked = Mocks.GetOrCreateMock<ILogger<Sample>>();
        var legacy = Mocks.GetMock<ILogger<Sample>>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new MixedMockRetrievalAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.UseConsistentMockRetrieval));
            Assert.Equal(DiagnosticIds.UseConsistentMockRetrieval, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new MixedMockRetrievalAnalyzer(), codeFixProvider, DiagnosticIds.UseConsistentMockRetrieval);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using Microsoft.Extensions.Logging;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var tracked = Mocks.GetOrCreateMock<ILogger<Sample>>();
        var legacy = Mocks.GetOrCreateMock<ILogger<Sample>>();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task GetMockCompatibilityAnalyzer_ShouldReportAndFix_StandaloneGetMockUsage()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.Extensions.Logging;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var logger = Mocks.GetMock<ILogger<Sample>>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new GetMockCompatibilityAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.UseProviderFirstMockRetrieval));
            Assert.Equal(DiagnosticIds.UseProviderFirstMockRetrieval, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new GetMockCompatibilityAnalyzer(), codeFixProvider, DiagnosticIds.UseProviderFirstMockRetrieval);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using Microsoft.Extensions.Logging;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var logger = Mocks.GetOrCreateMock<ILogger<Sample>>();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task GetMockCompatibilityAnalyzer_ShouldNotReport_WhenDocumentAlreadyUsesGetOrCreateMock()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.Extensions.Logging;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var tracked = Mocks.GetOrCreateMock<ILogger<Sample>>();
        var legacy = Mocks.GetMock<ILogger<Sample>>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new GetMockCompatibilityAnalyzer());

            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.UseProviderFirstMockRetrieval);
        }

        [Fact]
        public async Task GetRequiredMockCompatibilityAnalyzer_ShouldReport_WhenLegacyGetRequiredMockIsUsed()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.Extensions.Logging;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var logger = Mocks.GetRequiredMock<ILogger<Sample>>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new GetRequiredMockCompatibilityAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.AvoidLegacyRequiredMockRetrieval));
            Assert.Equal(DiagnosticIds.AvoidLegacyRequiredMockRetrieval, diagnostic.Id);
        }

        [Fact]
        public async Task GetRequiredMockCompatibilityAnalyzer_ShouldReport_WhenLegacyGetRequiredMockTypeOverloadIsUsed()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using Microsoft.Extensions.Logging;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var logger = Mocks.GetRequiredMock(typeof(ILogger<Sample>));
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new GetRequiredMockCompatibilityAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.AvoidLegacyRequiredMockRetrieval));
            Assert.Equal(DiagnosticIds.AvoidLegacyRequiredMockRetrieval, diagnostic.Id);
        }

        [Fact]
        public async Task LegacyMoqCreationLifecycleAnalyzer_ShouldReport_WhenCreateDetachedMockIsUsed()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.Extensions.Logging;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var logger = Mocks.CreateDetachedMock<ILogger<Sample>>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new LegacyMoqCreationLifecycleAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.AvoidLegacyMockCreationAndLifecycleApis));
            Assert.Equal(DiagnosticIds.AvoidLegacyMockCreationAndLifecycleApis, diagnostic.Id);
        }

        [Fact]
        public async Task LegacyMoqCreationLifecycleAnalyzer_ShouldReport_WhenAddMockIsUsed()
        {
            const string SOURCE = @"
using FastMoq;
using Moq;

class Sample
{
    interface IService
    {
    }

    void Execute(Mocker Mocks, Mock<IService> legacy)
    {
        Mocks.AddMock(legacy);
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new LegacyMoqCreationLifecycleAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.AvoidLegacyMockCreationAndLifecycleApis));
            Assert.Equal(DiagnosticIds.AvoidLegacyMockCreationAndLifecycleApis, diagnostic.Id);
        }

        [Fact]
        public async Task MockOptionalAnalyzer_ShouldReportAndFix_TrueAssignment()
        {
            const string SOURCE = @"
using FastMoq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.MockOptional = true;
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new MockOptionalAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.UseExplicitOptionalParameterResolution));
            Assert.Equal(DiagnosticIds.UseExplicitOptionalParameterResolution, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new MockOptionalAnalyzer(), codeFixProvider, DiagnosticIds.UseExplicitOptionalParameterResolution);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker;
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReport_WhenServiceProviderIsMockedDirectly()
        {
            const string SOURCE = @"
using System;
using FastMoq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var serviceProvider = Mocks.GetOrCreateMock<IServiceProvider>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, includeAzureFunctionsHelpers: true, new ServiceProviderShimAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferTypedServiceProviderHelpers));
            Assert.Equal(DiagnosticIds.PreferTypedServiceProviderHelpers, diagnostic.Id);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReportAndFix_WhenServiceProviderIsMockedDirectly()
        {
            const string SOURCE = @"
using System;
using FastMoq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var serviceProvider = Mocks.GetOrCreateMock<IServiceProvider>();
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new ServiceProviderShimAnalyzer(),
                codeFixProvider,
                DiagnosticIds.PreferTypedServiceProviderHelpers);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using System;
using FastMoq;
using FastMoq.Extensions;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var serviceProvider = Mocks.CreateTypedServiceProvider();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReport_WhenServiceScopeFactoryIsMockedDirectly()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.Extensions.DependencyInjection;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var scopeFactory = Mocks.GetOrCreateMock<IServiceScopeFactory>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, includeAzureFunctionsHelpers: true, new ServiceProviderShimAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferTypedServiceProviderHelpers));
            Assert.Equal(DiagnosticIds.PreferTypedServiceProviderHelpers, diagnostic.Id);
            Assert.Contains("GetOrCreateMock<IServiceScopeFactory>()", diagnostic.GetMessage());
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReportAndFix_WhenServiceScopeFactoryIsMockedDirectly()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.Extensions.DependencyInjection;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var scopeFactory = Mocks.GetOrCreateMock<IServiceScopeFactory>();
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new ServiceProviderShimAnalyzer(),
                codeFixProvider,
                DiagnosticIds.PreferTypedServiceProviderHelpers);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using Microsoft.Extensions.DependencyInjection;
using FastMoq.Extensions;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var scopeFactory = Mocks.CreateTypedServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReport_WhenScopeFactoryIsExtractedFromProvider()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using Microsoft.Extensions.DependencyInjection;

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        Mocks.AddType<IServiceScopeFactory>(provider.GetRequiredService<IServiceScopeFactory>());
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ServiceProviderShimAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferTypedServiceProviderHelpers && item.GetMessage().Contains("AddType<IServiceScopeFactory>(GetRequiredService<IServiceScopeFactory>())")));
            Assert.Equal(DiagnosticIds.PreferTypedServiceProviderHelpers, diagnostic.Id);
            Assert.Contains("AddType<IServiceScopeFactory>(GetRequiredService<IServiceScopeFactory>())", diagnostic.GetMessage());
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReportAndFix_WhenScopeFactoryIsExtractedFromProvider()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using Microsoft.Extensions.DependencyInjection;

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        Mocks.AddType<IServiceScopeFactory>(provider.GetRequiredService<IServiceScopeFactory>());
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new ServiceProviderShimAnalyzer(),
                codeFixProvider,
                DiagnosticIds.PreferTypedServiceProviderHelpers);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using System;
using FastMoq;
using Microsoft.Extensions.DependencyInjection;
using FastMoq.Extensions;

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        Mocks.AddServiceProvider(provider);
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReport_WhenScopeFactoryCreateScopeIsMockedDirectly()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.DependencyInjection;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var scopeFactory = Mocks.GetOrCreateMock<IServiceScopeFactory>();
        var scope = Mocks.GetOrCreateMock<IServiceScope>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Instance);
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ServiceProviderShimAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferTypedServiceProviderHelpers && item.GetMessage().Contains("Setup(x => x.CreateScope())")));
            Assert.Equal(DiagnosticIds.PreferTypedServiceProviderHelpers, diagnostic.Id);
            Assert.Contains("Setup(x => x.CreateScope())", diagnostic.GetMessage());
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReport_WhenScopeServiceProviderIsMockedDirectly()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.DependencyInjection;

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var scope = Mocks.GetOrCreateMock<IServiceScope>();
        scope.SetupGet(x => x.ServiceProvider).Returns(provider);
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ServiceProviderShimAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferTypedServiceProviderHelpers && item.GetMessage().Contains("SetupGet(x => x.ServiceProvider)")));
            Assert.Equal(DiagnosticIds.PreferTypedServiceProviderHelpers, diagnostic.Id);
            Assert.Contains("SetupGet(x => x.ServiceProvider)", diagnostic.GetMessage());
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReportAndFix_WhenScopeServiceProviderIsMockedDirectly()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.DependencyInjection;

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var scope = Mocks.GetOrCreateMock<IServiceScope>();
        scope.SetupGet(x => x.ServiceProvider).Returns(provider);
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new ServiceProviderShimAnalyzer(),
                codeFixProvider,
                DiagnosticIds.PreferTypedServiceProviderHelpers,
                diagnosticMessageContains: "SetupGet(x => x.ServiceProvider)");
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.DependencyInjection;
using FastMoq.Extensions;

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var scope = Mocks.GetOrCreateMock<IServiceScope>();
        Mocks.AddServiceScope(provider);
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReportAndFix_WhenScopeServiceProviderUsesSetupProperty()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.DependencyInjection;

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var scope = Mocks.GetOrCreateMock<IServiceScope>();
        scope.AsMoq().SetupProperty(x => x.ServiceProvider, provider);
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new ServiceProviderShimAnalyzer(),
                codeFixProvider,
                DiagnosticIds.PreferTypedServiceProviderHelpers,
                diagnosticMessageContains: "SetupProperty(x => x.ServiceProvider)");
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.DependencyInjection;
using FastMoq.Extensions;

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var scope = Mocks.GetOrCreateMock<IServiceScope>();
        Mocks.AddServiceScope(provider);
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReportAndFix_WhenScopeSetupAlsoDrivesCreateScope()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.DependencyInjection;

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var scopeFactory = Mocks.GetOrCreateMock<IServiceScopeFactory>();
        var scope = Mocks.GetOrCreateMock<IServiceScope>();
        scope.SetupGet(x => x.ServiceProvider).Returns(provider);
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Instance);
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new ServiceProviderShimAnalyzer(),
                codeFixProvider,
                DiagnosticIds.PreferTypedServiceProviderHelpers,
                diagnosticMessageContains: "SetupGet(x => x.ServiceProvider)");
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.DependencyInjection;
using FastMoq.Extensions;

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var scopeFactory = Mocks.GetOrCreateMock<IServiceScopeFactory>();
        var scope = Mocks.GetOrCreateMock<IServiceScope>();
        Mocks.AddServiceScope(provider);
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReportAndFix_WhenCreateScopeReturnsTrackedScopeWithConfiguredProvider()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.DependencyInjection;

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var scopeFactory = Mocks.GetOrCreateMock<IServiceScopeFactory>();
        var scope = Mocks.GetOrCreateMock<IServiceScope>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Instance);
        scope.SetupGet(x => x.ServiceProvider).Returns(provider);
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new ServiceProviderShimAnalyzer(),
                codeFixProvider,
                DiagnosticIds.PreferTypedServiceProviderHelpers,
                diagnosticMessageContains: "Setup(x => x.CreateScope())");
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.DependencyInjection;
using FastMoq.Extensions;

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var scopeFactory = Mocks.GetOrCreateMock<IServiceScopeFactory>();
        var scope = Mocks.GetOrCreateMock<IServiceScope>();
        Mocks.AddServiceScope(provider);
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldNotOfferCreateScopeFix_WhenOnlyNestedLocalFunctionHasMatchingProviderSetup()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.DependencyInjection;

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var scopeFactory = Mocks.GetOrCreateMock<IServiceScopeFactory>();
        var scope = Mocks.GetOrCreateMock<IServiceScope>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Instance);

        void Configure(Mocker Mocks, IServiceProvider provider)
        {
            var scope = Mocks.GetOrCreateMock<IServiceScope>();
            scope.SetupGet(x => x.ServiceProvider).Returns(provider);
        }
    }
}";

            var titles = await AnalyzerTestHelpers.GetCodeFixTitlesAsync(
                SOURCE,
                new ServiceProviderShimAnalyzer(),
                codeFixProvider,
                DiagnosticIds.PreferTypedServiceProviderHelpers,
                diagnosticMessageContains: "Setup(x => x.CreateScope())");

            Assert.Empty(titles);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReportAndFix_WhenServiceScopeIsMockedDirectly()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.Extensions.DependencyInjection;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var scope = Mocks.GetOrCreateMock<IServiceScope>();
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new ServiceProviderShimAnalyzer(),
                codeFixProvider,
                DiagnosticIds.PreferTypedServiceProviderHelpers);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using Microsoft.Extensions.DependencyInjection;
using FastMoq.Extensions;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var scope = Mocks.CreateTypedServiceScope();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReport_WhenFunctionContextInstanceServicesIsMockedDirectly()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;

namespace Microsoft.Azure.Functions.Worker
{
    abstract class FunctionContext
    {
        public virtual IServiceProvider InstanceServices { get; set; }
    }
}

class Sample
{
    void Execute(Mocker Mocks)
    {
        var context = Mocks.GetOrCreateMock<Microsoft.Azure.Functions.Worker.FunctionContext>();
        context.Setup(x => x.InstanceServices);
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, includeAzureFunctionsHelpers: true, new ServiceProviderShimAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferTypedServiceProviderHelpers));
            Assert.Equal(DiagnosticIds.PreferTypedServiceProviderHelpers, diagnostic.Id);
        }

        [Fact]
        public async Task SetupSetAnalyzer_ShouldReportHelperSuggestion_ForSimpleInterfacePropertyCapture()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;

public interface IOrderGateway
{
    string? Mode { get; set; }
}

class Sample
{
    void Execute(Mocker Mocks)
    {
        var gateway = Mocks.GetOrCreateMock<IOrderGateway>();
        gateway.AsMoq().SetupSet(x => x.Mode = It.IsAny<string?>());
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new SetupSetAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferPropertySetterCaptureHelper));

            Assert.Equal(DiagnosticIds.PreferPropertySetterCaptureHelper, diagnostic.Id);
            Assert.Contains("Mocks.AddPropertySetterCapture<IOrderGateway, string?>(x => x.Mode)", diagnostic.GetMessage());
        }

        [Fact]
        public async Task SetupSetAnalyzer_ShouldReportAndFix_SimpleInterfacePropertyCapture()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;

public interface IOrderGateway
{
    string? Mode { get; set; }
}

class Sample
{
    void Execute(Mocker Mocks)
    {
        var gateway = Mocks.GetOrCreateMock<IOrderGateway>();
        gateway.AsMoq().SetupSet(x => x.Mode = It.IsAny<string?>());
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new SetupSetAnalyzer(), codeFixProvider, DiagnosticIds.PreferPropertySetterCaptureHelper);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;
using FastMoq.Extensions;

public interface IOrderGateway
{
    string? Mode { get; set; }
}

class Sample
{
    void Execute(Mocker Mocks)
    {
        var gateway = Mocks.GetOrCreateMock<IOrderGateway>();
        Mocks.AddPropertySetterCapture<IOrderGateway, string?>(x => x.Mode);
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task SetupSetAnalyzer_ShouldReportFakePatternSuggestion_ForChainedSetupSetUsage()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;

public interface IOrderGateway
{
    string? Mode { get; set; }
}

class Sample
{
    void Execute(Mocker Mocks)
    {
        var gateway = Mocks.GetOrCreateMock<IOrderGateway>();
        gateway.AsMoq().SetupSet(x => x.Mode = It.IsAny<string?>()).Verifiable();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new SetupSetAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferPropertySetterCaptureHelper));

            Assert.Equal(DiagnosticIds.PreferPropertySetterCaptureHelper, diagnostic.Id);
            Assert.Contains("PropertyValueCapture<string?>", diagnostic.GetMessage());
        }

        [Fact]
        public async Task SetupSetAnalyzer_ShouldNotOfferFix_ForChainedSetupSetUsage()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;

public interface IOrderGateway
{
    string? Mode { get; set; }
}

class Sample
{
    void Execute(Mocker Mocks)
    {
        var gateway = Mocks.GetOrCreateMock<IOrderGateway>();
        gateway.AsMoq().SetupSet(x => x.Mode = It.IsAny<string?>()).Verifiable();
    }
}";

            var codeFixTitles = await AnalyzerTestHelpers.GetCodeFixTitlesAsync(SOURCE, new SetupSetAnalyzer(), codeFixProvider, DiagnosticIds.PreferPropertySetterCaptureHelper);
            Assert.Empty(codeFixTitles);
        }

        [Fact]
        public async Task SetupAllPropertiesAnalyzer_ShouldReportPropertyStateSuggestion_ForSimpleInterfaceUsage()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;

public interface IOrderGateway
{
    string? Mode { get; set; }
}

class Sample
{
    void Execute(Mocker Mocks)
    {
        var gateway = Mocks.GetOrCreateMock<IOrderGateway>();
        gateway.AsMoq().SetupAllProperties();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new SetupAllPropertiesAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferPropertyStateHelper));

            Assert.Equal(DiagnosticIds.PreferPropertyStateHelper, diagnostic.Id);
            Assert.Contains("AddPropertyState<IOrderGateway>()", diagnostic.GetMessage());
        }

        [Fact]
        public async Task SetupAllPropertiesAnalyzer_ShouldReportAndFix_SimpleInterfaceUsage()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;

public interface IOrderGateway
{
    string? Mode { get; set; }
}

class Sample
{
    void Execute(Mocker Mocks)
    {
        var gateway = Mocks.GetOrCreateMock<IOrderGateway>();
        gateway.AsMoq().SetupAllProperties();
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new SetupAllPropertiesAnalyzer(), codeFixProvider, DiagnosticIds.PreferPropertyStateHelper);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using FastMoq.Extensions;

public interface IOrderGateway
{
    string? Mode { get; set; }
}

class Sample
{
    void Execute(Mocker Mocks)
    {
        var gateway = Mocks.GetOrCreateMock<IOrderGateway>();
        Mocks.AddPropertyState<IOrderGateway>();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task SetupAllPropertiesAnalyzer_ShouldReportFakeSuggestion_ForClassUsage()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;

public class OrderGateway
{
    public virtual string? Mode { get; set; }
}

class Sample
{
    void Execute(Mocker Mocks)
    {
        var gateway = Mocks.GetOrCreateMock<OrderGateway>();
        gateway.AsMoq().SetupAllProperties();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new SetupAllPropertiesAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferPropertyStateHelper));

            Assert.Equal(DiagnosticIds.PreferPropertyStateHelper, diagnostic.Id);
            Assert.Contains("concrete fake or stub registered with AddType(...)", diagnostic.GetMessage());
        }

        [Fact]
        public async Task SetupAllPropertiesAnalyzer_ShouldNotOfferFix_ForClassUsage()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;

public class OrderGateway
{
    public virtual string? Mode { get; set; }
}

class Sample
{
    void Execute(Mocker Mocks)
    {
        var gateway = Mocks.GetOrCreateMock<OrderGateway>();
        gateway.AsMoq().SetupAllProperties();
    }
}";

            var codeFixTitles = await AnalyzerTestHelpers.GetCodeFixTitlesAsync(SOURCE, new SetupAllPropertiesAnalyzer(), codeFixProvider, DiagnosticIds.PreferPropertyStateHelper);
            Assert.Empty(codeFixTitles);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReportAndFix_FunctionContextInstanceServicesReturnsUsage()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;

namespace Microsoft.Azure.Functions.Worker
{
    abstract class FunctionContext
    {
        public virtual IServiceProvider InstanceServices { get; set; }
    }
}

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var context = Mocks.GetOrCreateMock<Microsoft.Azure.Functions.Worker.FunctionContext>();
        context.Setup(x => x.InstanceServices).Returns(provider);
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, includeAzureFunctionsHelpers: true, new ServiceProviderShimAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferTypedServiceProviderHelpers));
            Assert.Equal(DiagnosticIds.PreferTypedServiceProviderHelpers, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new ServiceProviderShimAnalyzer(),
                codeFixProvider,
                DiagnosticIds.PreferTypedServiceProviderHelpers,
                includeAzureFunctionsHelpers: true);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using FastMoq.AzureFunctions.Extensions;

namespace Microsoft.Azure.Functions.Worker
{
    abstract class FunctionContext
    {
        public virtual IServiceProvider InstanceServices { get; set; }
    }
}

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var context = Mocks.GetOrCreateMock<Microsoft.Azure.Functions.Worker.FunctionContext>();
        context.AddFunctionContextInstanceServices(provider);
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldNotReport_WhenFunctionContextHelperPackageIsUnavailable()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;

namespace Microsoft.Azure.Functions.Worker
{
    abstract class FunctionContext
    {
        public virtual IServiceProvider InstanceServices { get; set; }
    }
}

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var context = Mocks.GetOrCreateMock<Microsoft.Azure.Functions.Worker.FunctionContext>();
        context.SetupGet(x => x.InstanceServices).Returns(provider);
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ServiceProviderShimAnalyzer());

            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.PreferTypedServiceProviderHelpers);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReportAndFix_FunctionContextInstanceServicesSetupPropertyUsage()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;

namespace Microsoft.Azure.Functions.Worker
{
    abstract class FunctionContext
    {
        public virtual IServiceProvider InstanceServices { get; set; }
    }
}

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var context = Mocks.GetOrCreateMock<Microsoft.Azure.Functions.Worker.FunctionContext>();
        context.AsMoq().SetupProperty(x => x.InstanceServices, provider);
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, includeAzureFunctionsHelpers: true, new ServiceProviderShimAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferTypedServiceProviderHelpers));
            Assert.Equal(DiagnosticIds.PreferTypedServiceProviderHelpers, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new ServiceProviderShimAnalyzer(),
                codeFixProvider,
                DiagnosticIds.PreferTypedServiceProviderHelpers,
                includeAzureFunctionsHelpers: true);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using System;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using FastMoq.AzureFunctions.Extensions;

namespace Microsoft.Azure.Functions.Worker
{
    abstract class FunctionContext
    {
        public virtual IServiceProvider InstanceServices { get; set; }
    }
}

class Sample
{
    void Execute(Mocker Mocks, IServiceProvider provider)
    {
        var context = Mocks.GetOrCreateMock<Microsoft.Azure.Functions.Worker.FunctionContext>();
        context.AddFunctionContextInstanceServices(provider);
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task ServiceProviderShimAnalyzer_ShouldReportAndFix_FunctionContextInvocationIdReturnsUsage()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;

namespace Microsoft.Azure.Functions.Worker
{
    abstract class FunctionContext
    {
        public abstract string InvocationId { get; }
    }
}

class Sample
{
    void Execute(Mocker Mocks)
    {
        var context = Mocks.GetOrCreateMock<Microsoft.Azure.Functions.Worker.FunctionContext>();
        context.SetupGet(x => x.InvocationId).Returns(""inv-123"");
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, includeAzureFunctionsHelpers: true, new ServiceProviderShimAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferFunctionContextExecutionHelpers));
            Assert.Equal(DiagnosticIds.PreferFunctionContextExecutionHelpers, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new ServiceProviderShimAnalyzer(),
                codeFixProvider,
                DiagnosticIds.PreferFunctionContextExecutionHelpers,
                includeAzureFunctionsHelpers: true);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using FastMoq.AzureFunctions.Extensions;

namespace Microsoft.Azure.Functions.Worker
{
    abstract class FunctionContext
    {
        public abstract string InvocationId { get; }
    }
}

class Sample
{
    void Execute(Mocker Mocks)
    {
        var context = Mocks.GetOrCreateMock<Microsoft.Azure.Functions.Worker.FunctionContext>();
        context.AddFunctionContextInvocationId(""inv-123"");
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task KnownTypeAuthoringAnalyzer_ShouldReport_WhenContextAwareAddTypeOverloadIsUsed()
        {
            const string SOURCE = @"
using FastMoq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.AddType(typeof(string), typeof(string), (mocker, context) => context.ToString());
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new KnownTypeAuthoringAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferKnownTypeRegistrations));
            Assert.Equal(DiagnosticIds.PreferKnownTypeRegistrations, diagnostic.Id);
        }

        [Fact]
        public async Task KeyedDependencyAnalyzer_ShouldReport_WhenUnkeyedMockIsUsedForSameTypeKeyedDependencies()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Parameter)]
    sealed class FromKeyedServicesAttribute : Attribute
    {
        public FromKeyedServicesAttribute(object serviceKey)
        {
        }
    }
}

class SampleTests : MockerTestBase<KeyedSample>
{
    void Execute()
    {
        var dependency = Mocks.GetOrCreateMock<IKeyedDependency>();
    }
}

interface IKeyedDependency
{
}

class KeyedSample
{
    public KeyedSample(
        [FromKeyedServices(""primary"")] IKeyedDependency primary,
        [FromKeyedServices(""secondary"")] IKeyedDependency secondary)
    {
    }
}
";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new KeyedDependencyAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreserveKeyedServiceDistinctness));
            Assert.Equal(DiagnosticIds.PreserveKeyedServiceDistinctness, diagnostic.Id);
        }

        [Fact]
        public async Task KeyedDependencyAnalyzer_ShouldNotReport_WhenKeyedMockOptionsAreUsed()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Parameter)]
    sealed class FromKeyedServicesAttribute : Attribute
    {
        public FromKeyedServicesAttribute(object serviceKey)
        {
        }
    }
}

class SampleTests : MockerTestBase<KeyedSample>
{
    void Execute()
    {
        var dependency = Mocks.GetOrCreateMock<IKeyedDependency>(new MockRequestOptions
        {
            ServiceKey = ""primary""
        });
    }
}

interface IKeyedDependency
{
}

class KeyedSample
{
    public KeyedSample(
        [FromKeyedServices(""primary"")] IKeyedDependency primary,
        [FromKeyedServices(""secondary"")] IKeyedDependency secondary)
    {
    }
}
";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new KeyedDependencyAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.PreserveKeyedServiceDistinctness);
        }

        [Fact]
        public async Task MockOptionalAnalyzer_ShouldReportAndFix_FalseAssignment()
        {
            const string SOURCE = @"
using FastMoq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.MockOptional = false;
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new MockOptionalAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.UseExplicitOptionalParameterResolution));
            Assert.Equal(DiagnosticIds.UseExplicitOptionalParameterResolution, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new MockOptionalAnalyzer(), codeFixProvider, DiagnosticIds.UseExplicitOptionalParameterResolution);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.OptionalParameterResolution = OptionalParameterResolutionMode.UseDefaultOrNull;
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task InitializeCompatibilityAnalyzer_ShouldReportAndFix_InitializeUsage()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.Extensions.Logging;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.Initialize<ILogger<Sample>>(mock => { });
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new InitializeCompatibilityAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.ReplaceInitializeCompatibilityWrapper));
            Assert.Equal(DiagnosticIds.ReplaceInitializeCompatibilityWrapper, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new InitializeCompatibilityAnalyzer(), codeFixProvider, DiagnosticIds.ReplaceInitializeCompatibilityWrapper);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using Microsoft.Extensions.Logging;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.GetMock<ILogger<Sample>>(mock =>
        {
        });
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task StrictCompatibilityAnalyzer_ShouldReport_StrictAssignment()
        {
            const string SOURCE = @"
using FastMoq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.Strict = true;
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new StrictCompatibilityAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.AvoidStrictCompatibilityProperty));
            Assert.Equal(DiagnosticIds.AvoidStrictCompatibilityProperty, diagnostic.Id);
        }

        [Fact]
        public async Task VerifyLoggerAnalyzer_ShouldReportAndFix_TimesSpecCompatibleUsage()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Extensions;
using FastMoq.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using System;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var logger = Mocks.GetMock<ILogger>();
        logger.VerifyLogger<Exception>(LogLevel.Error, ""processed order"", null, null, Times.AtMost(2));
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new VerifyLoggerAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.UseVerifyLogged));
            Assert.Equal(DiagnosticIds.UseVerifyLogged, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new VerifyLoggerAnalyzer(), codeFixProvider, DiagnosticIds.UseVerifyLogged);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Extensions;
using FastMoq.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using System;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var logger = Mocks.GetMock<ILogger>();
        Mocks.VerifyLogged(LogLevel.Error, ""processed order"", null, null, TimesSpec.AtMost(2));
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task VerifyLoggerAnalyzer_ShouldReportAndFix_DeferredAtLeastOnceUsage()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using System;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var logger = Mocks.GetMock<ILogger>();
        logger.VerifyLogger<Exception>(LogLevel.Error, ""processed order"", null, null, () => Times.AtLeastOnce());
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new VerifyLoggerAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.UseVerifyLogged));
            Assert.Equal(DiagnosticIds.UseVerifyLogged, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new VerifyLoggerAnalyzer(), codeFixProvider, DiagnosticIds.UseVerifyLogged);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using System;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var logger = Mocks.GetMock<ILogger>();
        Mocks.VerifyLogged(LogLevel.Error, ""processed order"", null, null);
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task TimesSpecHelperBoundaryAnalyzer_ShouldReport_TimesBasedHelperParameters()
        {
            const string SOURCE = @"
using Microsoft.Extensions.Logging;
using Moq;
using System;

class Sample
{
    void AssertLogged(Mock<ILogger> logger, Times times)
    {
    }

    void AssertLoggedDeferred(Mock<ILogger> logger, Func<Times> times)
    {
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new TimesSpecHelperBoundaryAnalyzer());
            var relevantDiagnostics = diagnostics.Where(item => item.Id == DiagnosticIds.UseTimesSpecAtHelperBoundary).ToList();

            Assert.Equal(2, relevantDiagnostics.Count);
        }

        [Fact]
        public async Task ProviderBootstrapAnalyzer_ShouldReport_WhenMoqApiIsUsedWithoutProviderSelection()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute()
    {
        var mocks = new Mocker();
        var dependency = mocks.GetOrCreateMock<IService>();
        dependency.AsMoq();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderBootstrapAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.SelectProviderBeforeProviderSpecificApi));
            Assert.Equal(DiagnosticIds.SelectProviderBeforeProviderSpecificApi, diagnostic.Id);
        }

        [Fact]
        public async Task ProviderBootstrapAnalyzer_ShouldReport_WhenNSubstituteApiIsUsedWithoutProviderSelection()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.NSubstituteProvider;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute()
    {
        var mocks = new Mocker();
        var dependency = mocks.GetOrCreateMock<IService>();
        dependency.Received();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderBootstrapAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.SelectProviderBeforeProviderSpecificApi));
            Assert.Equal(DiagnosticIds.SelectProviderBeforeProviderSpecificApi, diagnostic.Id);
        }

        [Fact]
        public async Task ProviderBootstrapAnalyzer_ShouldReport_WhenLegacyGetMockIsUsedWithoutProviderSelection()
        {
            const string SOURCE = @"
using FastMoq;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute()
    {
        var mocks = new Mocker();
        var dependency = mocks.GetMock<IService>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderBootstrapAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.SelectProviderBeforeProviderSpecificApi));
            Assert.Equal(DiagnosticIds.SelectProviderBeforeProviderSpecificApi, diagnostic.Id);
        }

        [Fact]
        public async Task ProviderBootstrapAnalyzer_ShouldReport_WhenLegacyMockPropertyIsUsedWithoutProviderSelection()
        {
            const string SOURCE = @"
using FastMoq;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        mocks.CreateMock<IService>();
        var model = mocks.GetMockModel<IService>();
        var legacy = model.Mock;
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderBootstrapAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.SelectProviderBeforeProviderSpecificApi));
            Assert.Equal(DiagnosticIds.SelectProviderBeforeProviderSpecificApi, diagnostic.Id);
        }

        [Fact]
        public async Task ProviderBootstrapAnalyzer_ShouldNotReport_WhenProviderIsSelectedInScope()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute()
    {
        using var providerScope = MockingProviderRegistry.Push(""moq"");
        var mocks = new Mocker();
        var dependency = mocks.GetOrCreateMock<IService>();
        dependency.AsMoq();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderBootstrapAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.SelectProviderBeforeProviderSpecificApi);
        }

        [Fact]
        public async Task ProviderBootstrapAnalyzer_ShouldNotReport_WhenProviderIsSelectedInOuterScopeAndApiIsInsideLambda()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using FastMoq.Providers;
using FastMoq.Providers.NSubstituteProvider;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute()
    {
        using var providerScope = MockingProviderRegistry.Push(""nsubstitute"");
        var mocks = new Mocker();
        var dependency = mocks.GetOrCreateMock<IService>();
        Action assertion = () => dependency.Received();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderBootstrapAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.SelectProviderBeforeProviderSpecificApi);
        }

        [Fact]
        public async Task ProviderBootstrapAnalyzer_ShouldReport_WhenProviderIsSelectedOnlyInsideNestedLambda()
        {
            const string SOURCE = @"
using System;
using FastMoq;
using FastMoq.Providers;
using FastMoq.Providers.NSubstituteProvider;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute()
    {
        var mocks = new Mocker();
        var dependency = mocks.GetOrCreateMock<IService>();
        Action configure = () =>
        {
            using var providerScope = MockingProviderRegistry.Push(""nsubstitute"");
        };

        dependency.Received();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderBootstrapAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.SelectProviderBeforeProviderSpecificApi));
            Assert.Equal(DiagnosticIds.SelectProviderBeforeProviderSpecificApi, diagnostic.Id);
        }

        [Fact]
        public async Task ProviderBootstrapAnalyzer_ShouldNotReport_WhenProviderIsSelectedByDefault()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;

static class Bootstrap
{
    static void Initialize()
    {
        MockingProviderRegistry.Register(""moq"", MoqMockingProvider.Instance, setAsDefault: true);
    }
}

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute()
    {
        var mocks = new Mocker();
        var dependency = mocks.GetMock<IService>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderBootstrapAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.SelectProviderBeforeProviderSpecificApi);
        }

        [Fact]
        public async Task ProviderBootstrapAnalyzer_ShouldNotReport_WhenAssemblyDeclaresMoqDefaultProvider()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers;

[assembly: FastMoqDefaultProvider(""moq"")]

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute()
    {
        var mocks = new Mocker();
        var dependency = mocks.GetMock<IService>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderBootstrapAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.SelectProviderBeforeProviderSpecificApi);
        }

        [Fact]
        public async Task ProviderBootstrapAnalyzer_ShouldNotReport_WhenAssemblyRegistersMoqAsDefaultProvider()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;

[assembly: FastMoqRegisterProvider(""moq"", typeof(MoqMockingProvider), SetAsDefault = true)]

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute()
    {
        var mocks = new Mocker();
        var dependency = mocks.GetMock<IService>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderBootstrapAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.SelectProviderBeforeProviderSpecificApi);
        }

        [Fact]
        public async Task ProviderBootstrapAnalyzer_ShouldNotReport_WhenAssemblyDeclaresNSubstituteDefaultProvider()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers;
using FastMoq.Providers.NSubstituteProvider;

[assembly: FastMoqDefaultProvider(""nsubstitute"")]

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute()
    {
        var mocks = new Mocker();
        var dependency = mocks.GetOrCreateMock<IService>();
        dependency.Received();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderBootstrapAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.SelectProviderBeforeProviderSpecificApi);
        }

        [Fact]
        public async Task ProviderBootstrapAnalyzer_ShouldNotReport_WhenAssemblyRegistersNSubstituteAsDefaultProvider()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers;
using FastMoq.Providers.NSubstituteProvider;

[assembly: FastMoqRegisterProvider(""nsubstitute"", typeof(NSubstituteMockingProvider), SetAsDefault = true)]

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute()
    {
        var mocks = new Mocker();
        var dependency = mocks.GetOrCreateMock<IService>();
        dependency.Received();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderBootstrapAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.SelectProviderBeforeProviderSpecificApi);
        }

        [Fact]
        public async Task TrackedAddTypeMigrationAnalyzer_ShouldReport_WhenTrackedReplacementStillUsesGetObjectForSameService()
        {
            const string SOURCE = @"
using FastMoq;
using Moq;

class Sample
{
    interface IService
    {
        string? Value { get; set; }
    }

    void Execute(Mocker mocks)
    {
        var tracked = mocks.GetMock<IService>();
        mocks.AddType<IService>(tracked.Object, replace: true);
        var resolved = mocks.GetObject<IService>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new TrackedAddTypeMigrationAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreserveTrackedResolutionDuringAddTypeMigration));
            Assert.Equal(DiagnosticIds.PreserveTrackedResolutionDuringAddTypeMigration, diagnostic.Id);
            Assert.Contains("GetObject<T>()", diagnostic.GetMessage());
        }

        [Fact]
        public async Task TrackedAddTypeMigrationAnalyzer_ShouldReport_WhenTrackedReplacementUsesGetRequiredTrackedMockInstance()
        {
            const string SOURCE = @"
using FastMoq;

class Sample
{
    interface IService
    {
        string? Value { get; set; }
    }

    void Execute(Mocker mocks)
    {
        mocks.GetOrCreateMock<IService>();
        mocks.AddType<IService>(mocks.GetRequiredTrackedMock<IService>().Instance, replace: true);
        var resolved = mocks.GetObject<IService>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new TrackedAddTypeMigrationAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreserveTrackedResolutionDuringAddTypeMigration));
            Assert.Contains("GetObject<T>()", diagnostic.GetMessage());
        }

        [Fact]
        public async Task TrackedAddTypeMigrationAnalyzer_ShouldReport_WhenNonGenericAddTypeFactoryReturnsTrackedObject()
        {
            const string SOURCE = @"
using FastMoq;
using Moq;

class Sample
{
    interface IService
    {
        string? Value { get; set; }
    }

    sealed class FakeService : IService
    {
        public string? Value { get; set; }
    }

    void Execute(Mocker mocks)
    {
        var tracked = mocks.GetMock<IService>();
        mocks.AddType(typeof(IService), typeof(FakeService), _ => tracked.Object, replace: true);
        var resolved = mocks.GetObject<IService>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new TrackedAddTypeMigrationAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreserveTrackedResolutionDuringAddTypeMigration));
            Assert.Contains("GetObject<T>()", diagnostic.GetMessage());
        }

        [Fact]
        public async Task TrackedAddTypeMigrationAnalyzer_ShouldNotReport_WhenConcreteFakeReplacementOwnsResolution()
        {
            const string SOURCE = @"
using FastMoq;

class Sample
{
    interface IService
    {
        string? Value { get; set; }
    }

    sealed class FakeService : IService
    {
        public string? Value { get; set; }
    }

    void Execute(Mocker mocks)
    {
        mocks.AddType<IService>(_ => new FakeService(), replace: true);
        var resolved = mocks.GetObject<IService>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new TrackedAddTypeMigrationAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.PreserveTrackedResolutionDuringAddTypeMigration);
        }

        [Fact]
        public async Task LegacyMoqOnboardingAnalyzer_ShouldReport_WhenLegacyGetMockIsUsedWithoutExplicitOnboarding()
        {
            const string SOURCE = @"
using FastMoq;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var dependency = mocks.GetMock<IService>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new LegacyMoqOnboardingAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.RequireExplicitMoqOnboarding));
            Assert.Equal(DiagnosticIds.RequireExplicitMoqOnboarding, diagnostic.Id);
            Assert.Contains("GetMock<T>()", diagnostic.GetMessage());
        }

        [Fact]
        public async Task LegacyMoqOnboardingAnalyzer_ShouldReport_WhenLegacyGetMockUsesAssemblyDefaultButMoqProviderPackageIsMissing()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers;

[assembly: FastMoqDefaultProvider(""moq"")]

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var dependency = mocks.GetMock<IService>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(
                SOURCE,
                includeAzureFunctionsHelpers: false,
                includeMoqProviderPackage: false,
                includeNSubstituteProviderPackage: true,
                new LegacyMoqOnboardingAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.RequireExplicitMoqOnboarding));
            Assert.Equal(DiagnosticIds.RequireExplicitMoqOnboarding, diagnostic.Id);

            var codeFixTitles = await AnalyzerTestHelpers.GetCodeFixTitlesAsync(
                SOURCE,
                new LegacyMoqOnboardingAnalyzer(),
                codeFixProvider,
                DiagnosticIds.RequireExplicitMoqOnboarding,
                includeAzureFunctionsHelpers: false,
                includeMoqProviderPackage: false,
                includeNSubstituteProviderPackage: true,
                includeWebHelpers: true);
            Assert.Empty(codeFixTitles);
        }

        [Fact]
        public async Task LegacyMoqOnboardingAnalyzer_ShouldNotReport_WhenProviderPackageAndAssemblyDefaultArePresent()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers;

[assembly: FastMoqDefaultProvider(""moq"")]

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var dependency = mocks.GetMock<IService>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new LegacyMoqOnboardingAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.RequireExplicitMoqOnboarding);
        }

        [Fact]
        public async Task LegacyMoqOnboardingAnalyzer_CodeFix_ShouldAddAssemblyDefault_WhenProviderPackageIsPresent()
        {
            const string SOURCE = @"
using FastMoq;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var dependency = mocks.GetMock<IService>();
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new LegacyMoqOnboardingAnalyzer(),
                codeFixProvider,
                DiagnosticIds.RequireExplicitMoqOnboarding,
                codeFixTitle: "Add [assembly: FastMoqDefaultProvider(\"moq\")]");
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Providers;

[assembly: FastMoqDefaultProvider(""moq"")]

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var dependency = mocks.GetMock<IService>();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public void FastMoqMigrationCodeFixProvider_ShouldTreatAssemblyDefaultProviderNamesCaseInsensitively()
        {
            const string SOURCE = @"
using FastMoq.Providers;

[assembly: FastMoqDefaultProvider(""MOQ"")]
";

            var compilationUnit = CSharpSyntaxTree.ParseText(SOURCE).GetCompilationUnitRoot();
            var hasDefaultProviderMethod = typeof(FastMoqMigrationCodeFixProvider).GetMethod(
                "HasAssemblyDefaultProviderAttribute",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(hasDefaultProviderMethod);
            var result = hasDefaultProviderMethod!.Invoke(null, [compilationUnit, "moq"]);

            Assert.IsType<bool>(result);
            Assert.True((bool) result!);
        }

        [Fact]
        public async Task LegacyMoqOnboardingAnalyzer_CodeFix_ShouldOfferBothMoqOnboardingChoices_WhenProviderPackageIsPresent()
        {
            const string SOURCE = @"
using FastMoq;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var dependency = mocks.GetMock<IService>();
    }
}";

            var codeFixTitles = await AnalyzerTestHelpers.GetCodeFixTitlesAsync(
                SOURCE,
                new LegacyMoqOnboardingAnalyzer(),
                codeFixProvider,
                DiagnosticIds.RequireExplicitMoqOnboarding);

            Assert.Equal(2, codeFixTitles.Length);
            Assert.Contains("Add [assembly: FastMoqDefaultProvider(\"moq\")]", codeFixTitles);
            Assert.Contains("Add [assembly: FastMoqRegisterProvider(\"moq\", typeof(MoqMockingProvider), SetAsDefault = true)]", codeFixTitles);
        }

        [Fact]
        public async Task LegacyMoqOnboardingAnalyzer_CodeFix_ShouldAddAssemblyRegistration_WhenExplicitRegisterAndSelectIsPreferred()
        {
            const string SOURCE = @"
using FastMoq;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var dependency = mocks.GetMock<IService>();
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new LegacyMoqOnboardingAnalyzer(),
                codeFixProvider,
                DiagnosticIds.RequireExplicitMoqOnboarding,
                codeFixTitle: "Add [assembly: FastMoqRegisterProvider(\"moq\", typeof(MoqMockingProvider), SetAsDefault = true)]");
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;

[assembly: FastMoqRegisterProvider(""moq"", typeof(MoqMockingProvider), SetAsDefault = true)]

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var dependency = mocks.GetMock<IService>();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public void FastMoqMigrationCodeFixProvider_ShouldTreatAssemblyRegisteredDefaultProviderNamesCaseInsensitively()
        {
            const string SOURCE = @"
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;

[assembly: FastMoqRegisterProvider(""MOQ"", typeof(MoqMockingProvider), SetAsDefault = true)]
";

            var compilationUnit = CSharpSyntaxTree.ParseText(SOURCE).GetCompilationUnitRoot();
            var hasRegisteredDefaultProviderMethod = typeof(FastMoqMigrationCodeFixProvider).GetMethod(
                "HasAssemblyRegisteredDefaultProviderAttribute",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(hasRegisteredDefaultProviderMethod);
            var result = hasRegisteredDefaultProviderMethod!.Invoke(null, [compilationUnit, "moq"]);

            Assert.IsType<bool>(result);
            Assert.True((bool) result!);
        }

        [Fact]
        public async Task NativeMockAuthoringAnalyzer_ShouldReport_WhenGetNativeMockIsUsedInMoqOrientedFile()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var native = mocks.GetNativeMock<IService>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new NativeMockAuthoringAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferTypedProviderExtensions));
            Assert.Equal(DiagnosticIds.PreferTypedProviderExtensions, diagnostic.Id);
        }

        [Fact]
        public async Task NativeMockAuthoringAnalyzer_ShouldReportAndFix_WhenGetNativeMockIsUsedInMoqOrientedFile()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var native = mocks.GetNativeMock<IService>();
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new NativeMockAuthoringAnalyzer(), codeFixProvider, DiagnosticIds.PreferTypedProviderExtensions);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Providers.MoqProvider;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var native = mocks.GetOrCreateMock<IService>().AsMoq();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task NativeMockAuthoringAnalyzer_ShouldReport_WhenNativeMockPropertyIsUsedInNSubstituteOrientedFile()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.NSubstituteProvider;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var fast = mocks.GetOrCreateMock<IService>();
        var native = fast.NativeMock;
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new NativeMockAuthoringAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferTypedProviderExtensions));
            Assert.Equal(DiagnosticIds.PreferTypedProviderExtensions, diagnostic.Id);
        }

        [Fact]
        public async Task NativeMockAuthoringAnalyzer_ShouldReportAndFix_WhenNativeMockPropertyIsUsedInNSubstituteOrientedFile()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.NSubstituteProvider;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var fast = mocks.GetOrCreateMock<IService>();
        var native = fast.NativeMock;
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new NativeMockAuthoringAnalyzer(), codeFixProvider, DiagnosticIds.PreferTypedProviderExtensions);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Providers.NSubstituteProvider;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var fast = mocks.GetOrCreateMock<IService>();
        var native = fast.AsNSubstitute();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task NativeMockAuthoringAnalyzer_ShouldNotReport_WithoutProviderNamespacePreference()
        {
            const string SOURCE = @"
using FastMoq;

class Sample
{
    interface IService
    {
        void Run();
    }

    void Execute(Mocker mocks)
    {
        var native = mocks.GetNativeMock<IService>();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new NativeMockAuthoringAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.PreferTypedProviderExtensions);
        }

        [Fact]
        public async Task WebHelperAuthoringAnalyzer_ShouldReport_WhenAddTypeRegistersHttpContext()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.AspNetCore.Http;

class Sample
{
    void Execute(Mocker mocks)
    {
        mocks.AddType<HttpContext>(_ => new DefaultHttpContext());
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new WebHelperAuthoringAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferWebTestHelpers));
            Assert.Equal(DiagnosticIds.PreferWebTestHelpers, diagnostic.Id);
        }

        [Fact]
        public async Task WebHelperAuthoringAnalyzer_ShouldReportAndFix_WhenAddTypeRegistersHttpContext()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.AspNetCore.Http;

class Sample
{
    void Execute(Mocker mocks)
    {
        mocks.AddType<HttpContext>(_ => new DefaultHttpContext());
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new WebHelperAuthoringAnalyzer(), codeFixProvider, DiagnosticIds.PreferWebTestHelpers);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using Microsoft.AspNetCore.Http;
using FastMoq.Web.Extensions;

class Sample
{
    void Execute(Mocker mocks)
    {
        mocks.AddHttpContext();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task WebHelperAuthoringAnalyzer_ShouldReport_WhenAddTypeRegistersHttpContextAccessor()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.AspNetCore.Http;

class Sample
{
    void Execute(Mocker mocks)
    {
        mocks.AddType<IHttpContextAccessor, HttpContextAccessor>(_ => new HttpContextAccessor());
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new WebHelperAuthoringAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferWebTestHelpers));
            Assert.Equal(DiagnosticIds.PreferWebTestHelpers, diagnostic.Id);
        }

        [Fact]
        public async Task WebHelperAuthoringAnalyzer_ShouldReportAndFix_WhenAddTypeRegistersHttpContextAccessor()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.AspNetCore.Http;

class Sample
{
    void Execute(Mocker mocks)
    {
        mocks.AddType<IHttpContextAccessor, HttpContextAccessor>(_ => new HttpContextAccessor());
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new WebHelperAuthoringAnalyzer(), codeFixProvider, DiagnosticIds.PreferWebTestHelpers);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using Microsoft.AspNetCore.Http;
using FastMoq.Web.Extensions;

class Sample
{
    void Execute(Mocker mocks)
    {
        mocks.AddHttpContextAccessor();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task WebHelperAuthoringAnalyzer_ShouldReportOnce_WhenAddTypeRegistersHttpContextAccessorWithNestedHttpContext()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.AspNetCore.Http;

class Sample
{
    void Execute(Mocker mocks)
    {
        mocks.AddType<IHttpContextAccessor, HttpContextAccessor>(_ => new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new WebHelperAuthoringAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferWebTestHelpers));
            Assert.Equal(DiagnosticIds.PreferWebTestHelpers, diagnostic.Id);
        }

        [Fact]
        public async Task WebHelperAuthoringAnalyzer_ShouldReport_WhenDefaultHttpContextIsConstructedDirectly()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.AspNetCore.Http;

class Sample
{
    void Execute(Mocker mocks)
    {
        var context = new DefaultHttpContext();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new WebHelperAuthoringAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferWebTestHelpers));
            Assert.Equal(DiagnosticIds.PreferWebTestHelpers, diagnostic.Id);
        }

        [Fact]
        public async Task WebHelperAuthoringAnalyzer_ShouldReport_WhenControllerContextUsesDefaultHttpContextInitializer()
        {
            const string SOURCE = @"
using FastMoq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

class Sample
{
    void Execute(Mocker mocks)
    {
        var controllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new WebHelperAuthoringAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferWebTestHelpers));
            Assert.Equal(DiagnosticIds.PreferWebTestHelpers, diagnostic.Id);
        }

        [Fact]
        public async Task WebHelperAuthoringAnalyzer_ShouldReport_WhenCreateHttpContextDirectlySetsRequestBody()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Web.Extensions;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;

class Sample
{
    void Execute(Mocker mocks)
    {
        mocks.CreateHttpContext().Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(""alpha""));
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new WebHelperAuthoringAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferWebTestHelpers));
            Assert.Equal(DiagnosticIds.PreferWebTestHelpers, diagnostic.Id);
            Assert.Contains("SetRequestBody(...) or SetRequestJsonBody(...)", diagnostic.GetMessage());
        }

        [Fact]
        public async Task WebHelperAuthoringAnalyzer_ShouldReportAndFix_WhenCreateHttpContextDirectlySetsRequestBodyAndContentType()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Web.Extensions;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;

class Sample
{
    void Execute(Mocker mocks)
    {
        mocks.CreateHttpContext().Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(""alpha""));
        mocks.CreateHttpContext().Request.ContentType = ""text/plain"";
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new WebHelperAuthoringAnalyzer(), codeFixProvider, DiagnosticIds.PreferWebTestHelpers);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Web.Extensions;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;

class Sample
{
    void Execute(Mocker mocks)
    {
        mocks.CreateHttpContext().SetRequestBody(new MemoryStream(Encoding.UTF8.GetBytes(""alpha"")), ""text/plain"");
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task WebHelperAuthoringAnalyzer_ShouldReport_WhenCreateHttpContextDirectlySetsRequestContentType()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Web.Extensions;
using Microsoft.AspNetCore.Http;

class Sample
{
    void Execute(Mocker mocks)
    {
        mocks.CreateHttpContext().Request.ContentType = ""application/json"";
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new WebHelperAuthoringAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferWebTestHelpers));
            Assert.Equal(DiagnosticIds.PreferWebTestHelpers, diagnostic.Id);
            Assert.Contains("SetRequestBody(...) or SetRequestJsonBody(...)", diagnostic.GetMessage());
        }

        [Fact]
        public async Task HttpRequestHelperAuthoringAnalyzer_ShouldReport_WhenSetupHttpMessageIsUsed()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Extensions;
using FastMoq.Providers.MoqProvider;
using System.Net.Http;

class Sample
{
    void Execute(Mocker mocks)
    {
        mocks.SetupHttpMessage(() => new HttpResponseMessage());
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferProviderNeutralHttpHelpers));
            Assert.Equal(DiagnosticIds.PreferProviderNeutralHttpHelpers, diagnostic.Id);
        }

        [Fact]
        public async Task HttpRequestHelperAuthoringAnalyzer_ShouldReport_WhenTrackedHttpMessageHandlerUsesProtectedSendAsyncSetup()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;
using Moq.Protected;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

class Sample
{
    void Execute(Mocker mocks)
    {
        var handler = mocks.GetOrCreateMock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                ""SendAsync"",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferProviderNeutralHttpHelpers));
            Assert.Equal(DiagnosticIds.PreferProviderNeutralHttpHelpers, diagnostic.Id);
        }

        [Fact]
        public async Task HttpRequestHelperAuthoringAnalyzer_ShouldFix_TrackedHttpMessageHandlerProtectedSendAsyncSetupToWhenHttpRequestJson()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Sample
{
    async Task Execute(Mocker mocks, string requestUri)
    {
        var handler = mocks.GetOrCreateMock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                ""SendAsync"",
                ItExpr.Is<HttpRequestMessage>(request => request.Method == HttpMethod.Get && request.RequestUri == new Uri(requestUri)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Accepted,
                Content = new StringContent(""{}"", Encoding.UTF8, ""application/json""),
            });
        using var client = new HttpClient(handler.Object);
        client.BaseAddress = new Uri(""https://example.test/"");
        await client.GetAsync(""/api/test"");
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer(), codeFixProvider, DiagnosticIds.PreferProviderNeutralHttpHelpers);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FastMoq.Extensions;

class Sample
{
    async Task Execute(Mocker mocks, string requestUri)
    {
        mocks.WhenHttpRequestJson(HttpMethod.Get, requestUri, ""{}"", HttpStatusCode.Accepted);
        using var client = mocks.CreateHttpClient();
        client.BaseAddress = new Uri(""https://example.test/"");
        await client.GetAsync(""/api/test"");
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task HttpRequestHelperAuthoringAnalyzer_ShouldFix_TrackedHttpMessageHandlerProtectedSendAsyncSetupToPredicateBasedWhenHttpRequest()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

class Sample
{
    void Execute(Mocker mocks)
    {
        var handler = mocks.GetOrCreateMock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                ""SendAsync"",
                ItExpr.Is<HttpRequestMessage>(request => request.Headers.Contains(""X-Test"")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler.Object);
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer(), codeFixProvider, DiagnosticIds.PreferProviderNeutralHttpHelpers);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastMoq.Extensions;

class Sample
{
    void Execute(Mocker mocks)
    {
        mocks.WhenHttpRequest(request => request.Headers.Contains(""X-Test""), () => new HttpResponseMessage(HttpStatusCode.OK));
        var client = mocks.CreateHttpClient();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task HttpRequestHelperAuthoringAnalyzer_ShouldFix_TrackedHttpMessageHandlerProtectedSendAsyncSetupSequenceToQueuedWhenHttpRequest()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

class Sample
{
    void Execute(Mocker mocks)
    {
        var handler = mocks.GetOrCreateMock<HttpMessageHandler>();
        handler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                ""SendAsync"",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.Accepted));
        var client = new HttpClient(handler.Object);
    }
}";

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer(), codeFixProvider, DiagnosticIds.PreferProviderNeutralHttpHelpers);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FastMoq.Extensions;
using System;
using System.Collections.Generic;

class Sample
{
    void Execute(Mocker mocks)
    {
        var fastMoqHttpResponseFactories = new Queue<Func<HttpResponseMessage>>(new Func<HttpResponseMessage>[]
        {
            () => new HttpResponseMessage(HttpStatusCode.OK),
            () => new HttpResponseMessage(HttpStatusCode.Accepted)
        });
        mocks.WhenHttpRequest(_ => true, () => fastMoqHttpResponseFactories.Count > 0 ? fastMoqHttpResponseFactories.Dequeue().Invoke() : throw new InvalidOperationException(""No queued HTTP response remains.""));
        var client = mocks.CreateHttpClient();
    }
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task HttpRequestHelperAuthoringAnalyzer_ShouldNotOfferCodeFix_WhenProtectedSendAsyncUsesSpecificCancellationTokenMatcher()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

class Sample
{
    void Execute(Mocker mocks)
    {
        var handler = mocks.GetOrCreateMock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                ""SendAsync"",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(token => token.CanBeCanceled))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer());
            Assert.Contains(diagnostics, item => item.Id == DiagnosticIds.PreferProviderNeutralHttpHelpers);

            var codeFixTitles = await AnalyzerTestHelpers.GetCodeFixTitlesAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer(), codeFixProvider, DiagnosticIds.PreferProviderNeutralHttpHelpers);
            Assert.Empty(codeFixTitles);
        }

        [Fact]
        public async Task HttpRequestHelperAuthoringAnalyzer_ShouldNotOfferCodeFix_WhenProtectedSendAsyncReturnsAsyncUsesParameterizedLambda()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

class Sample
{
    void Execute(Mocker mocks)
    {
        var handler = mocks.GetOrCreateMock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                ""SendAsync"",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken cancellationToken) => new HttpResponseMessage(HttpStatusCode.OK));
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer());
            Assert.Contains(diagnostics, item => item.Id == DiagnosticIds.PreferProviderNeutralHttpHelpers);

            var codeFixTitles = await AnalyzerTestHelpers.GetCodeFixTitlesAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer(), codeFixProvider, DiagnosticIds.PreferProviderNeutralHttpHelpers);
            Assert.Empty(codeFixTitles);
        }

        [Fact]
        public async Task HttpRequestHelperAuthoringAnalyzer_ShouldNotOfferCodeFix_WhenTrackedHandlerStillHasUnsupportedUsage()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

class Sample
{
    void Execute(Mocker mocks)
    {
        var handler = mocks.GetOrCreateMock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                ""SendAsync"",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler.Object, true);
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer());
            Assert.Contains(diagnostics, item => item.Id == DiagnosticIds.PreferProviderNeutralHttpHelpers);

            var codeFixTitles = await AnalyzerTestHelpers.GetCodeFixTitlesAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer(), codeFixProvider, DiagnosticIds.PreferProviderNeutralHttpHelpers);
            Assert.Empty(codeFixTitles);
        }

        [Fact]
        public async Task HttpRequestHelperAuthoringAnalyzer_ShouldNotOfferCodeFix_WhenProtectedSendAsyncIncludesCallback()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

class Sample
{
    void Execute(Mocker mocks)
    {
        var handler = mocks.GetOrCreateMock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                ""SendAsync"",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => { })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer());
            Assert.Contains(diagnostics, item => item.Id == DiagnosticIds.PreferProviderNeutralHttpHelpers);

            var codeFixTitles = await AnalyzerTestHelpers.GetCodeFixTitlesAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer(), codeFixProvider, DiagnosticIds.PreferProviderNeutralHttpHelpers);
            Assert.Empty(codeFixTitles);
        }

        [Fact]
        public async Task HttpRequestHelperAuthoringAnalyzer_ShouldNotOfferCodeFix_WhenProtectedSendAsyncIsMarkedVerifiable()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

class Sample
{
    void Execute(Mocker mocks)
    {
        var handler = mocks.GetOrCreateMock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                ""SendAsync"",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Verifiable();
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer());
            Assert.Contains(diagnostics, item => item.Id == DiagnosticIds.PreferProviderNeutralHttpHelpers);

            var codeFixTitles = await AnalyzerTestHelpers.GetCodeFixTitlesAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer(), codeFixProvider, DiagnosticIds.PreferProviderNeutralHttpHelpers);
            Assert.Empty(codeFixTitles);
        }

        [Fact]
        public async Task HttpRequestHelperAuthoringAnalyzer_ShouldNotReport_WhenProtectedSetupTargetsNonHttpMember()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq.Protected;

class SampleGateway
{
    protected virtual int Compute() => 0;
}

class Sample
{
    void Execute(Mocker mocks)
    {
        var gateway = mocks.GetOrCreateMock<SampleGateway>();
        gateway.Protected().Setup<int>(""Compute"");
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.PreferProviderNeutralHttpHelpers);
        }

        [Fact]
        public async Task HttpRequestHelperAuthoringAnalyzer_ShouldNotReport_WhenProviderNeutralHttpHelperIsUsed()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Extensions;
using System.Net.Http;

class Sample
{
    void Execute(Mocker mocks)
    {
        mocks.WhenHttpRequest(HttpMethod.Get, ""/api/orders"", () => new HttpResponseMessage());
    }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new HttpRequestHelperAuthoringAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.PreferProviderNeutralHttpHelpers);
        }
    }
}

using FastMoq.Analyzers;
using FastMoq.Analyzers.Analyzers;
using FastMoq.Analyzers.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;
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
            { new MockOptionalAnalyzer(), DiagnosticDescriptors.UseExplicitOptionalParameterResolution },
            { new InitializeCompatibilityAnalyzer(), DiagnosticDescriptors.ReplaceInitializeCompatibilityWrapper },
            { new StrictCompatibilityAnalyzer(), DiagnosticDescriptors.AvoidStrictCompatibilityProperty },
            { new TimesSpecHelperBoundaryAnalyzer(), DiagnosticDescriptors.UseTimesSpecAtHelperBoundary },
            { new ProviderBootstrapAnalyzer(), DiagnosticDescriptors.SelectProviderBeforeProviderSpecificApi },
            { new NativeMockAuthoringAnalyzer(), DiagnosticDescriptors.PreferTypedProviderExtensions },
            { new WebHelperAuthoringAnalyzer(), DiagnosticDescriptors.PreferWebTestHelpers },
            { new HttpRequestHelperAuthoringAnalyzer(), DiagnosticDescriptors.PreferProviderNeutralHttpHelpers },
            { new ServiceProviderShimAnalyzer(), DiagnosticDescriptors.PreferTypedServiceProviderHelpers },
            { new KnownTypeAuthoringAnalyzer(), DiagnosticDescriptors.PreferKnownTypeRegistrations },
            { new KeyedDependencyAnalyzer(), DiagnosticDescriptors.PreserveKeyedServiceDistinctness },
        };

        public static TheoryData<DiagnosticDescriptor, DiagnosticSeverity> DescriptorSeverityPairs => new()
        {
            { DiagnosticDescriptors.UseProviderFirstObjectAccess, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.UseProviderFirstReset, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.UseVerifyLogged, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.UseConsistentMockRetrieval, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.UseExplicitOptionalParameterResolution, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.ReplaceInitializeCompatibilityWrapper, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.AvoidStrictCompatibilityProperty, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.UseTimesSpecAtHelperBoundary, DiagnosticSeverity.Info },
            { DiagnosticDescriptors.SelectProviderBeforeProviderSpecificApi, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.PreferTypedProviderExtensions, DiagnosticSeverity.Info },
            { DiagnosticDescriptors.PreferWebTestHelpers, DiagnosticSeverity.Info },
            { DiagnosticDescriptors.PreferProviderNeutralHttpHelpers, DiagnosticSeverity.Info },
            { DiagnosticDescriptors.PreferTypedServiceProviderHelpers, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.PreferKnownTypeRegistrations, DiagnosticSeverity.Warning },
            { DiagnosticDescriptors.PreserveKeyedServiceDistinctness, DiagnosticSeverity.Warning },
        };

        [Theory]
        [MemberData(nameof(AnalyzerDescriptorPairs))]
        public void Analyzer_ShouldExposeExpectedSupportedDescriptor(DiagnosticAnalyzer analyzer, DiagnosticDescriptor expectedDescriptor)
        {
            var actualDescriptor = Assert.Single(analyzer.SupportedDiagnostics);
            Assert.Same(expectedDescriptor, actualDescriptor);
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

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ServiceProviderShimAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferTypedServiceProviderHelpers));
            Assert.Equal(DiagnosticIds.PreferTypedServiceProviderHelpers, diagnostic.Id);
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

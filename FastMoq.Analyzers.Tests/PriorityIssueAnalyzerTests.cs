using FastMoq.Analyzers;
using FastMoq.Analyzers.Analyzers;
using FastMoq.Analyzers.CodeFixes;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FastMoq.Analyzers.Tests
{
    public class PriorityIssueAnalyzerTests
    {
        private readonly FastMoqMigrationCodeFixProvider codeFixProvider = new();

        [Fact]
        public async Task TrackedMockVerificationAnalyzer_ShouldReportAndFix_PropertyAliasFromGetRequiredTrackedMock()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Moq;

class Sample
{
    private Mocker Helper { get; } = new();
    private Mock<IDependency> DependencyMock => Helper.GetRequiredTrackedMock<IDependency>().AsMoq();

    void Execute()
    {
        Helper.GetOrCreateMock<IDependency>();
        DependencyMock.Verify(x => x.Run(""alpha""), Times.Never);
    }
}

interface IDependency
{
    void Run(string value);
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new TrackedMockVerificationAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.UseProviderFirstVerify));
            Assert.Equal(DiagnosticIds.UseProviderFirstVerify, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new TrackedMockVerificationAnalyzer(), codeFixProvider, DiagnosticIds.UseProviderFirstVerify);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Moq;

class Sample
{
    private Mocker Helper { get; } = new();
    private Mock<IDependency> DependencyMock => Helper.GetRequiredTrackedMock<IDependency>().AsMoq();

    void Execute()
    {
        Helper.GetOrCreateMock<IDependency>();
        Helper.Verify<IDependency>(x => x.Run(""alpha""), TimesSpec.NeverCalled);
    }
}

interface IDependency
{
    void Run(string value);
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task TrackedMockVerificationAnalyzer_ShouldReportAndFix_LocalMockAliasExactCountUsage()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var dependency = Mocks.GetOrCreateMock<IDependency>().AsMoq();
        dependency.Verify(x => x.Run(""alpha""), Times.Exactly(2));
    }
}

interface IDependency
{
    void Run(string value);
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new TrackedMockVerificationAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.UseProviderFirstVerify));
            Assert.Equal(DiagnosticIds.UseProviderFirstVerify, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new TrackedMockVerificationAnalyzer(), codeFixProvider, DiagnosticIds.UseProviderFirstVerify);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using FastMoq;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var dependency = Mocks.GetOrCreateMock<IDependency>().AsMoq();
        Mocks.Verify<IDependency>(x => x.Run(""alpha""), TimesSpec.Exactly(2));
    }
}

interface IDependency
{
    void Run(string value);
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task BareTrackedVerifyAnalyzer_ShouldReport_ForTrackedVerifyWithoutExpression()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.GetOrCreateMock<IDependency>().AsMoq().Verify();
    }
}

interface IDependency
{
    void Run(string value);
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new BareTrackedVerifyAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.AvoidBareTrackedVerify));
            Assert.Equal(DiagnosticIds.AvoidBareTrackedVerify, diagnostic.Id);
        }

        [Fact]
        public async Task TrackedMockShimAnalyzer_ShouldReport_ForVerificationOnlyPropertyAlias()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;

class Sample
{
    private Mocker Helper { get; } = new();
    private Mock<IDependency> DependencyMock => Helper.GetOrCreateMock<IDependency>().AsMoq();

    void Execute()
    {
        DependencyMock.Verify(x => x.Run(""alpha""));
    }
}

interface IDependency
{
    void Run(string value);
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new TrackedMockShimAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.AvoidTrackedMockShimAlias));
            Assert.Equal(DiagnosticIds.AvoidTrackedMockShimAlias, diagnostic.Id);
            Assert.Contains("DependencyMock", diagnostic.GetMessage());
        }

        [Fact]
        public async Task TrackedMockShimAnalyzer_ShouldNotReport_WhenAliasIsUsedForSetup()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var dependency = Mocks.GetOrCreateMock<IDependency>().AsMoq();
        dependency.Setup(x => x.Run(""alpha""));
    }
}

interface IDependency
{
    void Run(string value);
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new TrackedMockShimAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.AvoidTrackedMockShimAlias);
        }

        [Fact]
        public async Task OptionsSetupAnalyzer_ShouldReportAndFix_ForPropertyAliasFromGetRequiredTrackedMock()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.Options;
using Moq;

class Sample
{
    private Mocker Mocks { get; } = new();
    private Mock<IOptions<SampleOptions>> OptionsMock => Mocks.GetRequiredTrackedMock<IOptions<SampleOptions>>().AsMoq();

    void Execute()
    {
        Mocks.GetOrCreateMock<IOptions<SampleOptions>>();
        OptionsMock.Setup(x => x.Value).Returns(new SampleOptions { RetryCount = 5 });
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
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.Options;
using Moq;
using FastMoq.Extensions;

class Sample
{
    private Mocker Mocks { get; } = new();
    private Mock<IOptions<SampleOptions>> OptionsMock => Mocks.GetRequiredTrackedMock<IOptions<SampleOptions>>().AsMoq();

    void Execute()
    {
        Mocks.GetOrCreateMock<IOptions<SampleOptions>>();
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
        public async Task SetupSetAnalyzer_ShouldReport_ForFieldAliasAssignedFromGetRequiredTrackedMock()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;

class Sample
{
    private readonly Mocker _mocks = new();
    private readonly Mock<IPropertyGateway> _gateway;

    public Sample()
    {
        _mocks.GetOrCreateMock<IPropertyGateway>();
        _gateway = _mocks.GetRequiredTrackedMock<IPropertyGateway>().AsMoq();
    }

    void Execute()
    {
        _gateway.SetupSet(x => x.Mode = It.IsAny<string?>());
    }
}

interface IPropertyGateway
{
    string? Mode { get; set; }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new SetupSetAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferPropertySetterCaptureHelper));
            Assert.Equal(DiagnosticIds.PreferPropertySetterCaptureHelper, diagnostic.Id);
        }

        [Fact]
        public async Task SetupAllPropertiesAnalyzer_ShouldReport_ForFieldAliasAssignedFromGetRequiredTrackedMock()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;

class Sample
{
    private readonly Mocker _mocks = new();
    private readonly Mock<IPropertyGateway> _gateway;

    public Sample()
    {
        _mocks.GetOrCreateMock<IPropertyGateway>();
        _gateway = _mocks.GetRequiredTrackedMock<IPropertyGateway>().AsMoq();
    }

    void Execute()
    {
        _gateway.SetupAllProperties();
    }
}

interface IPropertyGateway
{
    string? Mode { get; set; }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new SetupAllPropertiesAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.PreferPropertyStateHelper));
            Assert.Equal(DiagnosticIds.PreferPropertyStateHelper, diagnostic.Id);
        }

        [Fact]
        public async Task RawMockCreationAnalyzer_ShouldReportSingleInstanceGuidance_InMockerTestBaseSuite()
        {
            const string SOURCE = @"
using FastMoq;
using Moq;

class SampleTests : MockerTestBase<SampleService>
{
    void Execute()
    {
        var dependency = new Mock<IDependency>();
    }
}

class SampleService
{
}

interface IDependency
{
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new RawMockCreationAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.AvoidRawMockCreationInFastMoqSuites));
            Assert.Contains("GetOrCreateMock<IDependency>()", diagnostic.GetMessage());
        }

        [Fact]
        public async Task RawMockCreationAnalyzer_ShouldReportStandaloneGuidance_ForMultipleSameTypeMocks()
        {
            const string SOURCE = @"
using FastMoq;
using Moq;

class Sample
{
    void Execute(Mocker mocks)
    {
        var first = new Mock<IDependency>();
        Mock<IDependency> second = new();
    }
}

interface IDependency
{
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new RawMockCreationAnalyzer());
            var matches = diagnostics.Where(item => item.Id == DiagnosticIds.AvoidRawMockCreationInFastMoqSuites).ToArray();

            Assert.Equal(2, matches.Length);
            Assert.All(matches, diagnostic => Assert.Contains("CreateStandaloneFastMock<IDependency>()", diagnostic.GetMessage()));
        }

        [Fact]
        public async Task RawMockCreationAnalyzer_ShouldIgnore_NonFastMoqContext()
        {
            const string SOURCE = @"
using Moq;

class Sample
{
    void Execute()
    {
        var dependency = new Mock<IDependency>();
    }
}

interface IDependency
{
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new RawMockCreationAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.AvoidRawMockCreationInFastMoqSuites);
        }

        [Fact]
        public async Task MissingHelperPackageAnalyzer_ShouldReport_WhenWebHelpersAreMissing()
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

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(
                SOURCE,
                includeAzureFunctionsHelpers: false,
                includeMoqProviderPackage: true,
                includeNSubstituteProviderPackage: true,
                includeWebHelpers: false,
                new MissingHelperPackageAnalyzer(),
                new WebHelperAuthoringAnalyzer());

            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.ReferenceFastMoqHelperPackage));
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.PreferWebTestHelpers);
            Assert.Contains("FastMoq.Web", diagnostic.GetMessage());
            Assert.Contains("FastMoq.Web.Extensions", diagnostic.GetMessage());
        }

        [Fact]
        public async Task MissingHelperPackageAnalyzer_ShouldReport_WhenAzureFunctionsHelpersAreMissing()
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

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(
                SOURCE,
                includeAzureFunctionsHelpers: false,
                includeMoqProviderPackage: true,
                includeNSubstituteProviderPackage: true,
                includeWebHelpers: true,
                new MissingHelperPackageAnalyzer(),
                new ServiceProviderShimAnalyzer());

            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.ReferenceFastMoqHelperPackage));
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.PreferTypedServiceProviderHelpers);
            Assert.Contains("FastMoq.AzureFunctions", diagnostic.GetMessage());
            Assert.Contains("FastMoq.AzureFunctions.Extensions", diagnostic.GetMessage());
            Assert.Contains("AddFunctionContextInstanceServices", diagnostic.GetMessage());
        }
    }
}
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
        public async Task TrackedMockVerificationAnalyzer_ShouldReportAndFix_AsyncReturningTrackedMockAlias()
        {
            const string SOURCE = @"
using System.Threading.Tasks;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var dependency = Mocks.GetOrCreateMock<IDependency>().AsMoq();
        dependency.Verify(x => x.RunAsync(""alpha""), Times.Once);
    }
}

interface IDependency
{
    Task RunAsync(string value);
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new TrackedMockVerificationAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.UseProviderFirstVerify));
            Assert.Equal(DiagnosticIds.UseProviderFirstVerify, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new TrackedMockVerificationAnalyzer(), codeFixProvider, DiagnosticIds.UseProviderFirstVerify);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using System.Threading.Tasks;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var dependency = Mocks.GetOrCreateMock<IDependency>().AsMoq();
        Mocks.Verify<IDependency>(x => x.RunAsync(""alpha""), FastMoq.Providers.TimesSpec.Once);
    }
}

interface IDependency
{
    Task RunAsync(string value);
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task TrackedMockVerificationAnalyzer_ShouldReportAndFix_DetachedFastMockAliasWithAsyncReturn()
        {
            const string SOURCE = @"
using System.Threading.Tasks;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var dependency = Mocks.CreateStandaloneFastMock<IDependency>();
        dependency.AsMoq().Verify(x => x.LoadAsync(""alpha""), Times.Once);
    }
}

interface IDependency
{
    Task<string> LoadAsync(string value);
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new TrackedMockVerificationAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.UseProviderFirstVerify));
            Assert.Equal(DiagnosticIds.UseProviderFirstVerify, diagnostic.Id);

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(SOURCE, new TrackedMockVerificationAnalyzer(), codeFixProvider, DiagnosticIds.UseProviderFirstVerify);
            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using System.Threading.Tasks;
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;
using FastMoq.Providers;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var dependency = Mocks.CreateStandaloneFastMock<IDependency>();
        MockingProviderRegistry.Default.Verify<IDependency>(dependency, x => x.LoadAsync(""alpha""), FastMoq.Providers.TimesSpec.Once);
    }
}

interface IDependency
{
    Task<string> LoadAsync(string value);
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task TrackedMockVerificationAnalyzer_ShouldReportAndFix_DetachedPropertyAlias_UsingPropertyAtCallSite()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Moq;

abstract class BaseSample
{
    private readonly IFastMock<IDependency> _dependency = new Mocker().CreateStandaloneFastMock<IDependency>();

    protected IFastMock<IDependency> Dependency => _dependency;
}

class Sample : BaseSample
{
    void Execute()
    {
        Dependency.AsMoq().Verify(x => x.Run(""alpha""), Times.Once);
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

abstract class BaseSample
{
    private readonly IFastMock<IDependency> _dependency = new Mocker().CreateStandaloneFastMock<IDependency>();

    protected IFastMock<IDependency> Dependency => _dependency;
}

class Sample : BaseSample
{
    void Execute()
    {
        MockingProviderRegistry.Default.Verify<IDependency>(Dependency, x => x.Run(""alpha""), TimesSpec.Once);
    }
}

interface IDependency
{
    void Run(string value);
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task TrackedMockVerificationAnalyzer_ShouldNotReport_WhenDetachedVerifyUsesTransientCreationExpression()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers.MoqProvider;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.CreateStandaloneFastMock<IDependency>().AsMoq().Verify(x => x.Run(""alpha""), Times.Once);
    }
}

interface IDependency
{
    void Run(string value);
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new TrackedMockVerificationAnalyzer());
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.UseProviderFirstVerify);
        }

        [Fact]
        public async Task ProviderFirstVerifyMatcherAnalyzer_ShouldReportAndFix_WhenTrackedVerifyUsesMoqPredicateMatcher()
        {
            const string SOURCE = @"
using System.Threading;
using System.Threading.Tasks;
using FastMoq;
using FastMoq.Providers;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.Verify<IDependency>(x => x.RunAsync(It.Is<Request>(request => request.Id == 42), It.IsAny<CancellationToken>()), TimesSpec.Once);
    }
}

interface IDependency
{
    Task RunAsync(Request request, CancellationToken cancellationToken);
}

class Request
{
    public int Id { get; set; }
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderFirstVerifyMatcherAnalyzer());
            var matches = diagnostics.Where(item => item.Id == DiagnosticIds.UseFastArgMatcherInProviderFirstVerify).ToArray();

            Assert.Equal(2, matches.Length);

            var fixedPredicateSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new ProviderFirstVerifyMatcherAnalyzer(),
                codeFixProvider,
                DiagnosticIds.UseFastArgMatcherInProviderFirstVerify,
                diagnosticOccurrence: 0);

            var expectedPredicateSource = AnalyzerTestHelpers.NormalizeCode(@"
using System.Threading;
using System.Threading.Tasks;
using FastMoq;
using FastMoq.Providers;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.Verify<IDependency>(x => x.RunAsync(FastArg.Is<Request>(request => request.Id == 42), It.IsAny<CancellationToken>()), TimesSpec.Once);
    }
}

interface IDependency
{
    Task RunAsync(Request request, CancellationToken cancellationToken);
}

class Request
{
    public int Id { get; set; }
}");

            Assert.Equal(expectedPredicateSource, fixedPredicateSource);

            var fixedAnySource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new ProviderFirstVerifyMatcherAnalyzer(),
                codeFixProvider,
                DiagnosticIds.UseFastArgMatcherInProviderFirstVerify,
                diagnosticOccurrence: 1);

            var expectedAnySource = AnalyzerTestHelpers.NormalizeCode(@"
using System.Threading;
using System.Threading.Tasks;
using FastMoq;
using FastMoq.Providers;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.Verify<IDependency>(x => x.RunAsync(It.Is<Request>(request => request.Id == 42), FastArg.Any<CancellationToken>()), TimesSpec.Once);
    }
}

interface IDependency
{
    Task RunAsync(Request request, CancellationToken cancellationToken);
}

class Request
{
    public int Id { get; set; }
}");

            Assert.Equal(expectedAnySource, fixedAnySource);
        }

        [Fact]
        public async Task ProviderFirstVerifyMatcherAnalyzer_ShouldReportAndFix_WhenDetachedVerifyUsesExpressionWildcardMatcher()
        {
            const string SOURCE = @"
using System;
using System.Linq.Expressions;
using FastMoq;
using FastMoq.Providers;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var dependency = Mocks.CreateStandaloneFastMock<IDependency>();
        MockingProviderRegistry.Default.Verify<IDependency>(dependency, x => x.Match(It.IsAny<Expression<Func<string, bool>>>()), TimesSpec.Once);
    }
}

interface IDependency
{
    void Match(Expression<Func<string, bool>> predicate);
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderFirstVerifyMatcherAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.UseFastArgMatcherInProviderFirstVerify));

            Assert.Contains("FastArg.AnyExpression<string>()", diagnostic.GetMessage());

            var fixedSource = await AnalyzerTestHelpers.ApplyCodeFixAsync(
                SOURCE,
                new ProviderFirstVerifyMatcherAnalyzer(),
                codeFixProvider,
                DiagnosticIds.UseFastArgMatcherInProviderFirstVerify);

            var expected = AnalyzerTestHelpers.NormalizeCode(@"
using System;
using System.Linq.Expressions;
using FastMoq;
using FastMoq.Providers;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        var dependency = Mocks.CreateStandaloneFastMock<IDependency>();
        MockingProviderRegistry.Default.Verify<IDependency>(dependency, x => x.Match(FastArg.AnyExpression<string>()), TimesSpec.Once);
    }
}

interface IDependency
{
    void Match(Expression<Func<string, bool>> predicate);
}");

            Assert.Equal(expected, fixedSource);
        }

        [Fact]
        public async Task ProviderFirstVerifyMatcherAnalyzer_ShouldReportInfoWithoutFix_WhenProviderFirstVerifyUsesUnsupportedMoqMatcher()
        {
            const string SOURCE = @"
using FastMoq;
using FastMoq.Providers;
using Moq;

class Sample
{
    void Execute(Mocker Mocks)
    {
        Mocks.Verify<IDependency>(x => x.Run(It.IsInRange<int>(0, 10, Moq.Range.Inclusive)), TimesSpec.Once);
    }
}

interface IDependency
{
    void Run(int value);
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderFirstVerifyMatcherAnalyzer());
            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.AvoidUnsupportedMoqMatcherInProviderFirstVerify));

            Assert.Contains("It.IsInRange<int>(0,10,Moq.Range.Inclusive)", string.Concat(diagnostic.GetMessage().Where(character => !char.IsWhiteSpace(character))));

            var codeFixTitles = await AnalyzerTestHelpers.GetCodeFixTitlesAsync(
                SOURCE,
                new ProviderFirstVerifyMatcherAnalyzer(),
                codeFixProvider,
                DiagnosticIds.AvoidUnsupportedMoqMatcherInProviderFirstVerify);

            Assert.Empty(codeFixTitles);
        }

        [Fact]
        public async Task ProviderFirstVerifyMatcherAnalyzer_ShouldNotReport_WhenMoqMatcherStaysOnProviderNativeVerify()
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
        dependency.Verify(x => x.Run(It.IsAny<int>()), Times.Once);
    }
}

interface IDependency
{
    void Run(int value);
}";

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(SOURCE, new ProviderFirstVerifyMatcherAnalyzer());

            Assert.DoesNotContain(diagnostics, item =>
                item.Id == DiagnosticIds.UseFastArgMatcherInProviderFirstVerify ||
                item.Id == DiagnosticIds.AvoidUnsupportedMoqMatcherInProviderFirstVerify);
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

        [Fact]
        public async Task MissingHelperPackageAnalyzer_ShouldReport_WhenFunctionContextInvocationIdHelpersAreMissing()
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

            var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(
                SOURCE,
                includeAzureFunctionsHelpers: false,
                includeMoqProviderPackage: true,
                includeNSubstituteProviderPackage: true,
                includeWebHelpers: true,
                new MissingHelperPackageAnalyzer(),
                new ServiceProviderShimAnalyzer());

            var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == DiagnosticIds.ReferenceFastMoqHelperPackage));
            Assert.DoesNotContain(diagnostics, item => item.Id == DiagnosticIds.PreferFunctionContextExecutionHelpers);
            Assert.Contains("FastMoq.AzureFunctions", diagnostic.GetMessage());
            Assert.Contains("FastMoq.AzureFunctions.Extensions", diagnostic.GetMessage());
            Assert.Contains("AddFunctionContextInvocationId", diagnostic.GetMessage());
        }
    }
}
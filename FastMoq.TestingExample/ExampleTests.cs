using AwesomeAssertions;
using FastMoq.Extensions;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

#pragma warning disable CS8604
#pragma warning disable CS8602

namespace FastMoq.TestingExample
{
    public class TestClassNormalTestsDefaultBase : MockerTestBase<TestClassNormal>
    {
        [Fact]
        public void Test1()
        {
            Component.FileSystem.Should().NotBeNull();
            Component.FileSystem.Should().BeOfType<MockFileSystem>();
            Component.FileSystem.File.Should().NotBeNull();
            Component.FileSystem.Directory.Should().NotBeNull();
        }
    }

    public class TestClassNormalTestsSetupBase : MockerTestBase<TestClassNormal>
    {
        // Show different constructor base types

        internal TestClassNormalTestsSetupBase(int i) : base()
        {

        }

        public TestClassNormalTestsSetupBase() : base(SetupMocks) { }

        internal TestClassNormalTestsSetupBase(IFileSystem fs) : base(SetupMocks, mocker => { })
        {

        }

        internal TestClassNormalTestsSetupBase(IFileSystem fs, IFile f) : base(SetupMocks, mocker => new TestClassNormal(new MockFileSystem()))
        {

        }

        internal TestClassNormalTestsSetupBase(IFile f) : base(SetupMocks, mocker => new TestClassNormal(new MockFileSystem()), normal => { })
        {

        }

        [Fact]
        public void TestStrict()
        {
            Component.FileSystem.Should().NotBeNull();
            Component.FileSystem.Should().NotBeOfType<MockFileSystem>();
            Component.FileSystem.File.Should().NotBeNull();
            Component.FileSystem.Directory.Should().BeNull();
        }

        private static void SetupMocks(Mocker mocks)
        {
            var iFile = new FileSystem().File;
            mocks.Behavior.Enabled |= MockFeatures.FailOnUnconfigured;
            var fileSystemMock = mocks.GetOrCreateMock<IFileSystem>();
            fileSystemMock.Setup(x => x.File).Returns(iFile);
            fileSystemMock.Setup(x => x.Directory).Returns((IDirectory) null!);
        }
    }

    public class TestClassNormalTestsFull : MockerTestBase<TestClassNormal>
    {
        #region Fields

        private static bool testEventCalled;
        private static IFastMock<IFileSystem> fileSystemMock = default!;

        #endregion

        public TestClassNormalTestsFull() : base(SetupMocks, CreateComponent, CreatedComponent) =>
            testEventCalled = false;

        [Fact]
        public void Test1()
        {
            Component.FileSystem.Should().Be(fileSystemMock.Instance);
            Component.FileSystem.Should().NotBeNull();
            Component.FileSystem.File.Should().NotBeNull();
            Component.FileSystem.Directory.Should().BeNull();
            testEventCalled.Should().BeFalse();
            Component.CallTestEvent();
            testEventCalled.Should().BeTrue();

            fileSystemMock.Setup(x => x.Directory).Returns(new FileSystem().Directory);
            Component.FileSystem.Directory.Should().NotBeNull();
        }

        private static TestClassNormal CreateComponent(Mocker mocks) => new(mocks.GetObject<IFileSystem>());
        private static void CreatedComponent(TestClassNormal? obj) => obj.TestEvent += (_, _) => testEventCalled = true;

        private static void SetupMocks(Mocker mocks)
        {
            var iFile = new FileSystem().File;
            mocks.Behavior.Enabled |= MockFeatures.FailOnUnconfigured;
            fileSystemMock = mocks.GetOrCreateMock<IFileSystem>();
            fileSystemMock.Setup(x => x.File).Returns(iFile);
            fileSystemMock.Setup(x => x.Directory).Returns((IDirectory)null!);
        }
    }

    public class WorkerProcessorMigrationExampleTests : MockerTestBase<WorkerProcessorMigrationExample>
    {
        public WorkerProcessorMigrationExampleTests() : base(static mocks =>
        {
            mocks.AddLoggerFactory();
            mocks.SetupOptions(new WorkerProcessorOptions
            {
                RetryCount = 3,
            });
        })
        {
        }

        [Fact]
        public void Execute_ShouldUseHelperRegisteredLoggerAndOptions()
        {
            Component.Execute().Should().Be(3);

            Mocks.VerifyLogged(LogLevel.Information, "Running with retry count 3");
        }
    }

    public sealed class WorkerProcessorMigrationExample
    {
        private readonly ILogger<WorkerProcessorMigrationExample> _logger;
        private readonly IOptions<WorkerProcessorOptions> _options;

        public WorkerProcessorMigrationExample(ILogger<WorkerProcessorMigrationExample> logger, IOptions<WorkerProcessorOptions> options)
        {
            _logger = logger;
            _options = options;
        }

        public int Execute()
        {
            var retryCount = _options.Value.RetryCount;
            _logger.LogInformation("Running with retry count {RetryCount}", retryCount);
            return retryCount;
        }
    }

    public sealed class WorkerProcessorOptions
    {
        public int RetryCount { get; set; }
    }
}
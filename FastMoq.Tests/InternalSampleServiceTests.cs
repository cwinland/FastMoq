using System;
using System.IO.Abstractions;
using Xunit;

namespace FastMoq.Tests
{
    public class InternalSampleServiceTests
    {
        [Fact]
        public void PublicTestClass_ShouldCreateInternalService_WhenInternalHarnessEnablesNonPublicFallback()
        {
            using var testBase = new InternalSampleServiceWithFallbackTestBase();

            testBase.Sut.Should().NotBeNull();
            testBase.Sut.HasJsonExtension("settings.json").Should().BeTrue();
        }

        [Fact]
        public void PublicTestClass_ShouldExerciseInternalServiceBehavior_ThroughInternalHarness()
        {
            using var testBase = new InternalSampleServiceWithFallbackTestBase();

            testBase.Sut.HasJsonExtension("settings.txt").Should().BeFalse();
        }
    }

    internal sealed class InternalSampleServiceWithFallbackTestBase : MockerTestBase<InternalSampleService>
    {
        protected override Action<MockerPolicyOptions>? ConfigureMockerPolicy => policy =>
        {
            policy.DefaultFallbackToNonPublicConstructors = false;
        };

        protected override InstanceCreationFlags ComponentCreationFlags => InstanceCreationFlags.AllowNonPublicConstructorFallback;

        internal InternalSampleService Sut => Component;
    }

    internal sealed class InternalSampleService
    {
        private readonly IFileSystem _fileSystem;

        internal InternalSampleService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public bool HasJsonExtension(string path)
        {
            return string.Equals(_fileSystem.Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase);
        }
    }
}
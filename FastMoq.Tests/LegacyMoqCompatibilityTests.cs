using FastMoq.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Runtime;

namespace FastMoq.Tests
{
    public class LegacyMoqCompatibilityTests
    {
        [Fact]
        public void VerifyLogger_ShouldPass_WhenMatches()
        {
            var mocker = new Mocker();
            var logger = mocker.GetMock<ILogger>();
            logger.VerifyLogger(LogLevel.Information, "test", 0);

            logger.Object.LogInformation("test");
            logger.VerifyLogger(LogLevel.Information, "test");

            logger.Object.LogInformation("test");
            logger.VerifyLogger(LogLevel.Information, "test", 2);
            logger.VerifyLogger(LogLevel.Information, "test", null, null, 2);

            logger.Invocations.Clear();
            logger.Object.LogError(1, new AmbiguousImplementationException("Test Exception"), "test message");
            logger.VerifyLogger(LogLevel.Error, "test", new AmbiguousImplementationException("Test Exception"), 1);
            logger.VerifyLogger<Exception>(LogLevel.Error, "test", new AmbiguousImplementationException("Test Exception"), 1);
            logger.VerifyLogger<Exception>(LogLevel.Error, "test", new AmbiguousImplementationException("Test Exception"), null, Times.AtLeastOnce);
            logger.VerifyLogger<Exception>(LogLevel.Error, "test", new AmbiguousImplementationException("Test Exception"), null, Times.AtLeastOnce());
        }

        [Fact]
        public void VerifyLogger_ShouldPass_WhenMatchesILoggerSubtype()
        {
            var mocker = new Mocker();
            var logger = mocker.GetMock<ILogger<NullLogger>>();
            logger.VerifyLogger(LogLevel.Information, "test", 0);

            logger.Object.LogInformation("test");
            logger.VerifyLogger(LogLevel.Information, "test");

            logger.Object.LogInformation("test");
            logger.VerifyLogger(LogLevel.Information, "test", 2);
            logger.VerifyLogger(LogLevel.Information, "test", null, null, 2);

            logger.Invocations.Clear();
            logger.Object.LogError(1, new AmbiguousImplementationException("Test Exception"), "test message");
            logger.VerifyLogger(LogLevel.Error, "test", new AmbiguousImplementationException("Test Exception"), 1);
        }

        [Fact]
        public void VerifyLogger_ShouldThrow_WhenNotMatches()
        {
            var mocker = new Mocker();
            var logger = mocker.GetMock<ILogger<NullLogger>>();
            logger.VerifyLogger(LogLevel.Information, "test", 0);

            logger.Object.LogInformation("test");
            Assert.Throws<MockException>(() => logger.VerifyLogger(LogLevel.Information, "test2"));

            logger.Object.LogInformation("test");
            Assert.Throws<MockException>(() => logger.VerifyLogger(LogLevel.Information, "test"));

            logger.Invocations.Clear();
            logger.Object.LogError(1, new AmbiguousImplementationException("Test Exception"), "test message");
            Assert.Throws<MockException>(() => logger.VerifyLogger(LogLevel.Error, "test", new AmbiguousImplementationException("Test Exception"), 0));
        }
    }
}
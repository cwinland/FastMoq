using System;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging; // for logger callback contracts

namespace FastMoq.Providers
{
    /// <summary>
    /// Abstraction implemented by concrete mocking libraries (Moq, NSubstitute, etc.).
    /// </summary>
    public interface IMockingProvider
    {
        IMockingProviderCapabilities Capabilities { get; }

        IFastMock<T> CreateMock<T>(MockCreationOptions? options = null) where T : class;
        IFastMock CreateMock(Type type, MockCreationOptions? options = null);

        void SetupAllProperties(IFastMock mock);
        void SetCallBase(IFastMock mock, bool value);

        void Verify<T>(IFastMock<T> mock, Expression<Action<T>> expression, TimesSpec? times = null) where T : class;
        void VerifyNoOtherCalls(IFastMock mock);

        // v2 provider-first enhancement hooks
        /// <summary>
        /// Allows provider to perform property initialization / automatic setup after creation.
        /// Implementations should NO-OP when not supported or when strict mode is enabled (caller passes the flag).
        /// </summary>
        void ConfigureProperties(IFastMock mock, bool strict);

        /// <summary>
        /// Allows provider to attach a logger callback (when underlying type implements ILogger / ILogger&lt;T&gt;).
        /// Implementations should defensively NO-OP if not supported or callback is null.
        /// </summary>
        void ConfigureLogger(IFastMock mock, Action<LogLevel, EventId, string> callback);

        /// <summary>
        /// Attempts to expose the legacy Moq <see cref="Moq.Mock"/> surface (for migration compatibility).
        /// Non-Moq providers return null.
        /// </summary>
        object? TryGetLegacy(IFastMock mock);
    }
}

using System;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace FastMoq.Providers
{
    /// <summary>
    /// Abstraction implemented by concrete mocking libraries (Moq, NSubstitute, etc.).
    /// </summary>
    public interface IMockingProvider
    {
        IMockingProviderCapabilities Capabilities { get; }

        Expression<Func<T, bool>> BuildExpression<T>();

        IFastMock<T> CreateMock<T>(MockCreationOptions? options = null) where T : class;
        IFastMock CreateMock(Type type, MockCreationOptions? options = null);

        void SetupAllProperties(IFastMock mock);
        void SetCallBase(IFastMock mock, bool value);

        void Verify<T>(IFastMock<T> mock, Expression<Action<T>> expression, TimesSpec? times = null) where T : class;
        void VerifyNoOtherCalls(IFastMock mock);

        void ConfigureProperties(IFastMock mock);
        void ConfigureLogger(IFastMock mock, Action<LogLevel, EventId, string, Exception?> callback);
        object? TryGetLegacy(IFastMock mock);
        IFastMock? TryWrapLegacy(object legacyMock, Type mockedType);
    }
}
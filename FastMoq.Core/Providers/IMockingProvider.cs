using System;
using System.Linq.Expressions;

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
    }
}

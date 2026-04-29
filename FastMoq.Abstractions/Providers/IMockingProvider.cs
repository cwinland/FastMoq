using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Providers
{
    /// <summary>
    /// Abstraction implemented by concrete mocking libraries (Moq, NSubstitute, etc.).
    /// </summary>
    public interface IMockingProvider
    {
        /// <summary>
        /// Gets the capabilities exposed by the current provider implementation.
        /// </summary>
        IMockingProviderCapabilities Capabilities { get; }

        /// <summary>
        /// Builds a wildcard predicate expression for the mocked type.
        /// </summary>
        /// <typeparam name="T">The mocked type.</typeparam>
        /// <returns>A wildcard expression that can be used as a compatibility helper for expression-valued arguments.</returns>
        Expression<Func<T, bool>> BuildExpression<T>();

        /// <summary>
        /// Creates a mock wrapper for the specified type.
        /// </summary>
        /// <typeparam name="T">The mocked type.</typeparam>
        /// <param name="options">Optional creation settings for the mock.</param>
        /// <returns>A provider-backed mock wrapper.</returns>
        IFastMock<T> CreateMock<T>(MockCreationOptions? options = null) where T : class;

        /// <summary>
        /// Creates a mock wrapper for the specified runtime type.
        /// </summary>
        /// <param name="type">The mocked runtime type.</param>
        /// <param name="options">Optional creation settings for the mock.</param>
        /// <returns>A provider-backed mock wrapper.</returns>
        IFastMock CreateMock(Type type, MockCreationOptions? options = null);

        /// <summary>
        /// Configures the mock so settable properties behave like auto-properties.
        /// </summary>
        /// <param name="mock">The mock to configure.</param>
        void SetupAllProperties(IFastMock mock);

        /// <summary>
        /// Enables or disables call-through behavior to the underlying implementation.
        /// </summary>
        /// <param name="mock">The mock to configure.</param>
        /// <param name="value">True to call the base implementation; otherwise false.</param>
        void SetCallBase(IFastMock mock, bool value);

        /// <summary>
        /// Verifies that the specified call expression was invoked on the mock.
        /// </summary>
        /// <typeparam name="T">The mocked type.</typeparam>
        /// <param name="mock">The mock to verify.</param>
        /// <param name="expression">The invocation expression to verify.</param>
        /// <param name="times">The expected invocation count.</param>
        void Verify<T>(IFastMock<T> mock, Expression<Action<T>> expression, TimesSpec? times = null) where T : class;

        /// <summary>
        /// Verifies that the specified method was invoked on the mock while treating every argument as a wildcard matcher.
        /// </summary>
        /// <typeparam name="T">The mocked type.</typeparam>
        /// <param name="mock">The mock to verify.</param>
        /// <param name="method">The method to verify.</param>
        /// <param name="times">The expected invocation count.</param>
        /// <exception cref="NotSupportedException">
        /// Thrown when the current provider does not implement wildcard method verification.
        /// </exception>
        void VerifyMethod<T>(IFastMock<T> mock, MethodInfo method, TimesSpec? times = null) where T : class
        {
            throw new NotSupportedException("The current mocking provider does not support VerifyMethod<T>. Implement IMockingProvider.VerifyMethod<T> to enable wildcard method verification.");
        }

        /// <summary>
        /// Verifies that no unexpected invocations remain on the mock.
        /// </summary>
        /// <param name="mock">The mock to verify.</param>
        void VerifyNoOtherCalls(IFastMock mock);

        /// <summary>
        /// Configures tracked properties for the supplied mock.
        /// </summary>
        /// <param name="mock">The mock to configure.</param>
        void ConfigureProperties(IFastMock mock);

        /// <summary>
        /// Configures logger behavior for the supplied mock.
        /// </summary>
        /// <param name="mock">The logger mock to configure.</param>
        /// <param name="callback">A callback that receives log details for each invocation.</param>
        void ConfigureLogger(IFastMock mock, Action<LogLevel, EventId, string, Exception?> callback);

        /// <summary>
        /// Attempts to retrieve the provider's native legacy mock object for the supplied wrapper.
        /// </summary>
        /// <param name="mock">The wrapped mock.</param>
        /// <returns>The native legacy mock instance when available; otherwise null.</returns>
        object? TryGetLegacy(IFastMock mock);

        /// <summary>
        /// Attempts to wrap a provider-specific legacy mock instance in a FastMoq abstraction.
        /// </summary>
        /// <param name="legacyMock">The provider-specific mock instance.</param>
        /// <param name="mockedType">The mocked type represented by the legacy mock.</param>
        /// <returns>A FastMoq wrapper when the object can be adapted; otherwise null.</returns>
        IFastMock? TryWrapLegacy(object legacyMock, Type mockedType);
    }
}
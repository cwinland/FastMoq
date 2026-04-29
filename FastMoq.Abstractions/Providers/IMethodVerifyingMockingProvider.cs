using System.Reflection;

namespace FastMoq.Providers
{
    /// <summary>
    /// Optional provider capability for verifying a selected method while treating every argument as a wildcard matcher.
    /// </summary>
    public interface IMethodVerifyingMockingProvider
    {
        /// <summary>
        /// Verifies that the specified method was invoked on the mock while treating every argument as a wildcard matcher.
        /// </summary>
        /// <typeparam name="T">The mocked type.</typeparam>
        /// <param name="mock">The mock to verify.</param>
        /// <param name="method">The method to verify.</param>
        /// <param name="times">The expected invocation count.</param>
        void VerifyMethod<T>(IFastMock<T> mock, MethodInfo method, TimesSpec? times = null) where T : class;
    }
}
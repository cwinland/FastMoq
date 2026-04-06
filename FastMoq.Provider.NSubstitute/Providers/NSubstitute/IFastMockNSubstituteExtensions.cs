using System;
using FastMoq.Providers;
using NSubstitute;
using NSubstitute.Exceptions;

namespace FastMoq.Providers.NSubstituteProvider
{
    /// <summary>
    /// NSubstitute-specific convenience extensions for <see cref="IFastMock{T}"/>.
    /// These stay in the provider package so the core abstractions remain provider agnostic.
    /// </summary>
    public static class IFastMockNSubstituteExtensions
    {
        /// <summary>
        /// Returns the tracked substitute instance after validating that the active provider produced an NSubstitute substitute.
        /// </summary>
        public static T AsNSubstitute<T>(this IFastMock<T> fastMock) where T : class
        {
            ArgumentNullException.ThrowIfNull(fastMock);

            try
            {
                _ = fastMock.Instance.ReceivedCalls();
                return fastMock.Instance;
            }
            catch (NotASubstituteException)
            {
                throw CreateProviderMismatchException(typeof(T), fastMock.NativeMock);
            }
        }

        /// <summary>
        /// NSubstitute convenience shortcut for <c>fastMock.AsNSubstitute().Received()</c>.
        /// </summary>
        public static T Received<T>(this IFastMock<T> fastMock) where T : class
        {
            return fastMock.AsNSubstitute().Received();
        }

        /// <summary>
        /// NSubstitute convenience shortcut for <c>fastMock.AsNSubstitute().Received(requiredNumberOfCalls)</c>.
        /// </summary>
        public static T Received<T>(this IFastMock<T> fastMock, int requiredNumberOfCalls) where T : class
        {
            return fastMock.AsNSubstitute().Received(requiredNumberOfCalls);
        }

        /// <summary>
        /// NSubstitute convenience shortcut for <c>fastMock.AsNSubstitute().ReceivedWithAnyArgs()</c>.
        /// </summary>
        public static T ReceivedWithAnyArgs<T>(this IFastMock<T> fastMock) where T : class
        {
            return fastMock.AsNSubstitute().ReceivedWithAnyArgs();
        }

        /// <summary>
        /// NSubstitute convenience shortcut for <c>fastMock.AsNSubstitute().DidNotReceive()</c>.
        /// </summary>
        public static T DidNotReceive<T>(this IFastMock<T> fastMock) where T : class
        {
            return fastMock.AsNSubstitute().DidNotReceive();
        }

        /// <summary>
        /// NSubstitute convenience shortcut for <c>fastMock.AsNSubstitute().DidNotReceiveWithAnyArgs()</c>.
        /// </summary>
        public static T DidNotReceiveWithAnyArgs<T>(this IFastMock<T> fastMock) where T : class
        {
            return fastMock.AsNSubstitute().DidNotReceiveWithAnyArgs();
        }

        /// <summary>
        /// Clears recorded calls on the tracked substitute.
        /// </summary>
        public static void ClearReceivedCalls<T>(this IFastMock<T> fastMock) where T : class
        {
            fastMock.AsNSubstitute().ClearReceivedCalls();
        }

        private static NotSupportedException CreateProviderMismatchException(Type mockedType, object? nativeMock)
        {
            var nativeType = nativeMock?.GetType().FullName ?? "null";
            return new NotSupportedException($"Tracked mock for '{mockedType.FullName}' is not backed by NSubstitute. The active provider returned '{nativeType}'. Select the NSubstitute provider for this test assembly or use provider-neutral FastMoq APIs.");
        }
    }
}
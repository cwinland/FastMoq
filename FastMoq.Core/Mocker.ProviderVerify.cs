using System;
using System.Linq.Expressions;
using FastMoq.Providers;

namespace FastMoq
{
    // Provider-first verification + fast mock creation conveniences (Moq-disconnected core surface).
    public partial class Mocker
    {
        /// <summary>
        /// Gets an existing tracked provider-backed mock or creates and tracks one when it does not yet exist.
        /// </summary>
        public IFastMock<T> GetOrCreateMock<T>(MockRequestOptions? options = null) where T : class
        {
            return GetOrCreateTypedFastMock<T>(options);
        }

        /// <summary>
        /// Gets an existing tracked provider-backed mock or creates and tracks one when it does not yet exist.
        /// </summary>
        public IFastMock GetOrCreateMock(Type type, MockRequestOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(type);
            return GetOrCreateFastMock(type, options);
        }

        /// <summary>
        /// Provider-first verification helper (provider agnostic).
        /// </summary>
        public void Verify<T>(Expression<Action<T>> expression, TimesSpec? times = null) where T : class
        {
            ArgumentNullException.ThrowIfNull(expression);
            var model = GetMockModelFast(typeof(T));
            if (model.FastMock is IFastMock<T> typed)
            {
                var provider = MockingProviderRegistry.Default;
                provider.Verify(typed, expression, times);
            }
            // If the stored fast mock is not strongly typed (should not occur normally), no-op.
        }

        /// <summary>
        /// Ensures no other calls were made for a given mock (provider-first only).
        /// </summary>
        public void VerifyNoOtherCalls<T>() where T : class
        {
            var model = GetMockModelFast(typeof(T));
            var provider = MockingProviderRegistry.Default;
            provider.VerifyNoOtherCalls(model.FastMock);
        }

        /// <summary>
        /// Creates a provider-first mock and registers it.
        /// </summary>
        public IFastMock<T> CreateFastMock<T>(MockCreationOptions? options = null) where T : class
        {
            var provider = MockingProviderRegistry.Default;
            var fast = provider.CreateMock<T>(options);
            AddFastMock(fast, typeof(T), overwrite: false, nonPublic: false);
            return fast;
        }

    }
}

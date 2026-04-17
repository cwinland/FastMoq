using FastMoq.Providers;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace FastMoq
{
    // Provider-first verification + fast mock creation conveniences (Moq-disconnected core surface).
    /// <inheritdoc />
    public partial class Mocker
    {
        /// <summary>
        /// Gets an existing tracked provider-backed mock or creates and tracks one when it does not yet exist.
        /// </summary>
        /// <example>
        /// <para>Use <see cref="GetOrCreateMock{T}(MockRequestOptions?)"/> when the test needs the tracked mock handle itself, not just automatic constructor resolution.</para>
        /// <para><c>fastMock.Instance</c> is the provider-first replacement for older <c>GetMock&lt;T&gt;().Object</c> access when the test still wants the tracked handle.</para>
        /// <para><c>GetOrCreateMock</c> uses the active FastMoq provider. It does not require the Moq provider to be selected unless the test later calls Moq-specific extensions such as <c>AsMoq()</c>, <c>Setup(...)</c>, or <c>Protected()</c>.</para>
        /// <code language="csharp"><![CDATA[
        /// var mocker = new Mocker();
        /// var gateway = mocker.GetOrCreateMock<IOrderGateway>();
        ///
        /// var submitter = new OrderSubmitter(gateway.Instance);
        /// submitter.Submit(42);
        ///
        /// mocker.Verify<IOrderGateway>(x => x.Publish(42), TimesSpec.Once);
        /// mocker.VerifyNoOtherCalls<IOrderGateway>();
        /// ]]></code>
        /// <para>Provider-neutral usage works the same way under the reflection provider because the tracked handle is still an <see cref="IFastMock{T}"/>.</para>
        /// <code language="csharp"><![CDATA[
        /// using var providerScope = MockingProviderRegistry.Push("reflection");
        ///
        /// var mocker = new Mocker();
        /// var dependency = mocker.GetOrCreateMock<IOrderGateway>();
        ///
        /// var submitter = new OrderSubmitter(dependency.Instance);
        /// submitter.Submit(42);
        ///
        /// mocker.Verify<IOrderGateway>(x => x.Publish(42), TimesSpec.Once);
        /// ]]></code>
        /// <para>Typical reasons to call it explicitly are: passing <c>Instance</c> into custom component construction, reusing the same tracked mock across calls, resetting it, or using keyed <see cref="MockRequestOptions"/>. If the test needs Moq-specific setup or verification APIs, select the Moq provider for that test assembly before calling those extensions.</para>
        /// </example>
        public IFastMock<T> GetOrCreateMock<T>(MockRequestOptions? options = null) where T : class
        {
            return GetOrCreateTypedFastMock<T>(options);
        }

        /// <summary>
        /// Gets an existing tracked provider-backed mock or creates and tracks one when it does not yet exist,
        /// using the supplied constructor arguments for concrete mock creation.
        /// </summary>
        /// <remarks>
        /// This is a convenience overload for <see cref="GetOrCreateMock{T}(MockRequestOptions?)" /> when the request only needs constructor arguments.
        /// Use <see cref="GetOrCreateMock{T}(MockRequestOptions?)" /> when also setting a service key or non-public constructor behavior.
        /// </remarks>
        public IFastMock<T> GetOrCreateMock<T>(params object?[] constructorArgs) where T : class
        {
            return GetOrCreateTypedFastMock<T>(new MockRequestOptions
            {
                ConstructorArgs = constructorArgs ?? Array.Empty<object?>(),
            });
        }

        /// <summary>
        /// Gets an existing tracked provider-backed mock or creates and tracks one when it does not yet exist.
        /// Use the returned <see cref="IFastMock" /> when the test needs the tracked handle itself, and prefer <see cref="Mocker.GetObject(Type, Action{object?}?)" /> when only the instance is needed.
        /// </summary>
        public IFastMock GetOrCreateMock(Type type, MockRequestOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(type);
            return GetOrCreateFastMock(type, options);
        }

        /// <summary>
        /// Gets an existing tracked provider-backed mock or creates and tracks one when it does not yet exist,
        /// using the supplied constructor arguments for concrete mock creation.
        /// </summary>
        /// <remarks>
        /// This is a convenience overload for <see cref="GetOrCreateMock(Type, MockRequestOptions?)" /> when the request only needs constructor arguments.
        /// Use <see cref="GetOrCreateMock(Type, MockRequestOptions?)" /> when also setting a service key or non-public constructor behavior.
        /// </remarks>
        public IFastMock GetOrCreateMock(Type type, params object?[] constructorArgs)
        {
            ArgumentNullException.ThrowIfNull(type);

            return GetOrCreateFastMock(type, new MockRequestOptions
            {
                ConstructorArgs = constructorArgs ?? Array.Empty<object?>(),
            });
        }

        /// <summary>
        /// Attempts to get an already tracked provider-backed mock without creating one.
        /// </summary>
        public bool TryGetTrackedMock<T>([NotNullWhen(true)] out IFastMock<T>? mock) where T : class
        {
            if (!TryGetMockModel(typeof(T), out var model) || model is null)
            {
                mock = null;
                return false;
            }

            mock = GetTypedFastMockFromModel<T>(model);
            return true;
        }

        /// <summary>
        /// Attempts to get an already tracked keyed provider-backed mock without creating one.
        /// </summary>
        public bool TryGetTrackedMock<T>(object serviceKey, [NotNullWhen(true)] out IFastMock<T>? mock) where T : class
        {
            if (!TryGetMockModel(typeof(T), serviceKey, out var model) || model is null)
            {
                mock = null;
                return false;
            }

            mock = GetTypedFastMockFromModel<T>(model);
            return true;
        }

        /// <summary>
        /// Attempts to get an already tracked provider-backed mock for the supplied runtime type without creating one.
        /// </summary>
        public bool TryGetTrackedMock(Type type, [NotNullWhen(true)] out IFastMock? mock)
        {
            type = ValidateTrackedMockLookupType(type, nameof(type));
            if (!TryGetMockModel(type, out var model) || model is null)
            {
                mock = null;
                return false;
            }

            mock = model.FastMock;
            return true;
        }

        /// <summary>
        /// Attempts to get an already tracked keyed provider-backed mock for the supplied runtime type without creating one.
        /// </summary>
        public bool TryGetTrackedMock(Type type, object serviceKey, [NotNullWhen(true)] out IFastMock? mock)
        {
            type = ValidateTrackedMockLookupType(type, nameof(type));
            ArgumentNullException.ThrowIfNull(serviceKey);

            if (!TryGetMockModel(type, serviceKey, out var model) || model is null)
            {
                mock = null;
                return false;
            }

            mock = model.FastMock;
            return true;
        }

        /// <summary>
        /// Gets an already tracked provider-backed mock and throws when no tracked mock exists.
        /// </summary>
        public IFastMock<T> GetRequiredTrackedMock<T>() where T : class
        {
            if (TryGetTrackedMock<T>(out var mock))
            {
                return mock;
            }

            throw CreateTrackedMockNotFoundException(typeof(T));
        }

        /// <summary>
        /// Gets an already tracked keyed provider-backed mock and throws when no tracked mock exists for the supplied service key.
        /// </summary>
        public IFastMock<T> GetRequiredTrackedMock<T>(object serviceKey) where T : class
        {
            if (TryGetTrackedMock<T>(serviceKey, out var mock))
            {
                return mock;
            }

            throw CreateTrackedMockNotFoundException(typeof(T), serviceKey);
        }

        /// <summary>
        /// Gets an already tracked provider-backed mock for the supplied runtime type and throws when no tracked mock exists.
        /// </summary>
        public IFastMock GetRequiredTrackedMock(Type type)
        {
            type = ValidateTrackedMockLookupType(type, nameof(type));
            if (TryGetTrackedMock(type, out var mock))
            {
                return mock;
            }

            throw CreateTrackedMockNotFoundException(type);
        }

        /// <summary>
        /// Gets an already tracked keyed provider-backed mock for the supplied runtime type and throws when no tracked mock exists for the supplied service key.
        /// </summary>
        public IFastMock GetRequiredTrackedMock(Type type, object serviceKey)
        {
            type = ValidateTrackedMockLookupType(type, nameof(type));
            if (TryGetTrackedMock(type, serviceKey, out var mock))
            {
                return mock;
            }

            throw CreateTrackedMockNotFoundException(type, serviceKey);
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

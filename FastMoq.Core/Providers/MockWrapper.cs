using System;
using System.Linq.Expressions;
using FastMoq.Providers;

namespace FastMoq.Core.Providers
{
    /// <summary>
    /// Provider-agnostic wrapper around an <see cref="IFastMock{T}"/> supplying:
    ///  - Instance (test subject dependency)
    ///  - NativeMock (legacy underlying mock if the provider exposes one)
    ///  - Verify delegation through <see cref="IMockingProvider"/>
    ///  - Transitional Setup helpers (Moq pass-through). Other providers throw until a common Setup abstraction exists.
    /// </summary>
    /// <typeparam name="T">Mocked type.</typeparam>
    public sealed class MockWrapper<T> where T : class
    {
        private readonly IMockingProvider _provider;
        private readonly IFastMock<T> _fast;
        private object? _native; // lazy capture

        internal MockWrapper(IMockingProvider provider, IFastMock<T> fast)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _fast = fast ?? throw new ArgumentNullException(nameof(fast));
        }

        /// <summary>Strongly-typed mocked instance (injected dependency).</summary>
        public T Instance => _fast.Instance;

        /// <summary>
        /// Underlying provider-specific object used for native mocking-library interaction.
        /// For Moq this is typically a Moq.Mock&lt;T&gt;; for providers like NSubstitute this is the substitute instance itself.
        /// </summary>
        public object? NativeMock => _native ??= _fast.NativeMock;

        /// <summary>
        /// Executes provider verification for the supplied expression.
        /// </summary>
        public void Verify(Expression<Action<T>> expression, TimesSpec? times = null)
            => _provider.Verify(_fast, expression, times);

        /// <summary>
        /// Moq transitional Setup (Action form). Returns the provider-native setup object when available; otherwise throws.
        /// </summary>
        public object Setup(Expression<Action<T>> expression)
        {
            if (NativeMock is global::Moq.Mock<T> moq)
            {
                return moq.Setup(expression);
            }
            throw new NotSupportedException("Setup(Action) not supported by active mocking provider.");
        }

        /// <summary>
        /// Moq transitional Setup (Func&lt;TResult&gt; form). Returns the provider-native setup object when available; otherwise throws.
        /// </summary>
        public object Setup<TResult>(Expression<Func<T, TResult>> expression)
        {
            if (NativeMock is global::Moq.Mock<T> moq)
            {
                return moq.Setup(expression);
            }
            throw new NotSupportedException("Setup(Func) not supported by active mocking provider.");
        }

        /// <summary>
        /// Factory helper creating a wrapper from an existing fast mock (internal use pending wider integration).
        /// </summary>
        internal static MockWrapper<T> Create(IMockingProvider provider, IFastMock<T> fast) => new(provider, fast);
    }
}

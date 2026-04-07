using System.Diagnostics.CodeAnalysis;
using FastMoq.Providers;
using Moq; // legacy (to be removed in future)

namespace FastMoq.Models
{
    /// <summary>
    ///     Contains provider-agnostic mock and Type information.
    /// </summary>
    public class MockModel : IComparable<MockModel>, IComparable, IEquatable<MockModel>, IEqualityComparer<MockModel>
    {
        #region Fields / Backing
        private Mock? legacyMock; // lazy hydrated legacy mock
        #endregion

        #region Properties

        /// <summary>
        /// Legacy Moq mock surface. Will be removed in a future major version.
        /// Prefer using <see cref="FastMock" /> / <see cref="Instance" />.
        /// Lazily hydrated from the provider adapter when first accessed.
        /// </summary>
        [Obsolete("Use FastMock / Instance instead. This legacy Moq surface will be removed.")]
        public Mock Mock
        {
            get
            {
                if (!TryGetLegacyMock(out var legacyMock))
                {
                    var providerName = MockingProviderRegistry.Default.GetType().Name;
                    throw new NotSupportedException(
                        $"Active provider '{providerName}' does not expose a legacy Moq.Mock instance for {Type.Name}. " +
                        $"Use FastMock/Instance or GetOrCreateMock(...) for provider-neutral access, or select the Moq provider before using the legacy Mock property.");
                }

                return legacyMock;
            }
            internal set
            {
                SetLegacyMock(value);
            }
        }

        /// <summary>
        /// Provider-agnostic abstraction for the mock instance.
        /// </summary>
        public IFastMock FastMock { get; internal set; }

        /// <summary>
        /// The mocked instance (object under test substitute) from the provider abstraction.
        /// </summary>
        public object Instance => FastMock.Instance;

        /// <summary>
        /// Native provider object used to arrange or inspect behavior with the active mocking library.
        /// For Moq this is a <see cref="Mock"/>; for providers like NSubstitute or Reflection this is typically the provider-native instance.
        /// </summary>
        public object NativeMock => FastMock.NativeMock;

        /// <summary>
        /// Indicates whether the mock was created allowing non-public constructors.
        /// </summary>
        public bool NonPublic { get; set; }

        /// <summary>
        /// Mocked type exposed by the current <see cref="IFastMock"/> instance.
        /// </summary>
        public virtual Type Type { get; }

        #endregion

        #region Construction

        /// <summary>
        /// Provider-first constructor (preferred). Accepts an <see cref="IFastMock"/> created by a provider.
        /// Attempts to hydrate the legacy Moq <see cref="Mock"/> property when the underlying provider is Moq.
        /// </summary>
        internal MockModel(IFastMock fastMock, bool nonPublic = false)
        {
            FastMock = fastMock ?? throw new ArgumentNullException(nameof(fastMock));
            Type = fastMock.MockedType ?? throw new ArgumentNullException(nameof(fastMock.MockedType));
            NonPublic = nonPublic;
            // Legacy hydration deferred until first access to Mock (lazy) for performance / provider agnosticism.
        }

        /// <summary>
        /// Legacy constructor used while migration is in progress.
        /// Wraps the provided legacy mock through the registered provider infrastructure.
        /// </summary>
        internal MockModel(Type type, Mock mock, bool nonPublic = false)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            legacyMock = mock ?? throw new ArgumentNullException(nameof(mock));
            NonPublic = nonPublic;
            FastMock = MockingProviderRegistry.WrapLegacy(mock, type);
        }

        /// <summary>
        /// Refresh FastMock wrapper from current legacy Mock (used when the legacy mock instance is replaced).
        /// </summary>
        internal void RefreshFastMockFromLegacy()
        {
            if (legacyMock != null)
            {
                FastMock = MockingProviderRegistry.WrapLegacy(legacyMock, Type);
            }
        }

        internal void SetLegacyMock(Mock mock)
        {
            legacyMock = mock ?? throw new ArgumentNullException(nameof(mock));
            RefreshFastMockFromLegacy();
        }

        internal bool TryGetLegacyMock([NotNullWhen(true)] out Mock? mock)
        {
            if (legacyMock == null)
            {
                TryAssignLegacyMockFromAdapter();
            }

            mock = legacyMock;
            return mock != null;
        }

        /// <summary>
        /// Attempts to populate the legacy <see cref="Mock"/> property from the provider adapter (Moq only).
        /// </summary>
        private void TryAssignLegacyMockFromAdapter()
        {
            if (legacyMock != null)
            {
                return;
            }

            if (FastMock.NativeMock is Mock nativeMock)
            {
                legacyMock = nativeMock;
                return;
            }

            try
            {
                // Common adapter property names.
                var adapterType = FastMock.GetType();
                var innerProp = adapterType.GetProperty("Inner", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance) ??
                                 adapterType.GetProperty("InnerMock", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (innerProp?.GetValue(FastMock) is Mock m)
                {
                    legacyMock = m; // hydrate legacy surface
                }
            }
            catch
            {
                // Ignore – provider not Moq or structure changed.
            }
        }
        #endregion

        #region Equality / Comparison
        public override bool Equals(object? obj) => IsEqual(this, obj as MockModel);

        [ExcludeFromCodeCoverage]
        public override int GetHashCode() => Type.GetHashCode();

        public static bool IsEqual<TModel>(TModel? x, TModel? y) where TModel : MockModel =>
            ReferenceEquals(x, y) || (!IsOneNull(x, y) && IsMockTypeNameEqual(x, y));

        [ExcludeFromCodeCoverage]
        public static bool operator ==(MockModel? a, MockModel? b) => IsEqual(a, b);
        [ExcludeFromCodeCoverage]
        public static bool operator !=(MockModel? a, MockModel? b) => !(a == b);

        public override string ToString() => Type.Name;

        internal static bool IsMockTypeNameEqual<TModel>(TModel? x, TModel? y) where TModel : MockModel =>
            x?.Type.Name.Equals(y?.Type.Name, StringComparison.OrdinalIgnoreCase) ?? false;

        internal static bool IsOneNull<TModel>(TModel? x, TModel? y) where TModel : MockModel => x as object is null || y as object is null;
        #endregion

        #region IComparable / IEquatable / IEqualityComparer
        public virtual int CompareTo(object? obj) =>
            obj is MockModel mockModel ? CompareTo(mockModel) : throw new ArgumentException("Not a MockModel instance");

        public int CompareTo(MockModel? other) => string.Compare(Type.FullName, other?.Type.FullName, StringComparison.OrdinalIgnoreCase);

        [ExcludeFromCodeCoverage]
        public bool Equals(MockModel? x, MockModel? y) => IsEqual(x, y);
        [ExcludeFromCodeCoverage]
        public int GetHashCode(MockModel obj) => GetHashCode();
        [ExcludeFromCodeCoverage]
        public bool Equals(MockModel? other) => IsEqual(this, other);
        #endregion
    }
}

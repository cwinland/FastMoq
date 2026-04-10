using FastMoq.Providers;
using System.Diagnostics.CodeAnalysis;

namespace FastMoq.Models
{
    /// <summary>
    ///     Contains provider-agnostic mock and Type information.
    /// </summary>
    public class MockModel : IComparable<MockModel>, IComparable, IEquatable<MockModel>, IEqualityComparer<MockModel>
    {
        #region Fields / Backing
        private Mock? legacyMock; // lazy hydrated legacy mock
        private readonly ObservableExceptionLog? exceptionLog;
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
                    throw new NotSupportedException(ProviderSelectionDiagnostics.BuildProviderMismatchMessage(
                        "moq",
                        Type,
                        FastMock.NativeMock,
                        FastMock.Instance,
                        "Mock",
                        "FastMock, Instance, or GetOrCreateMock(...) for provider-neutral access"));
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

        internal ObservableExceptionLog? ExceptionLog => exceptionLog;

        #endregion

        #region Construction

        /// <summary>
        /// Provider-first constructor (preferred). Accepts an <see cref="IFastMock"/> created by a provider.
        /// Attempts to hydrate the legacy Moq <see cref="Mock"/> property when the underlying provider is Moq.
        /// </summary>
        internal MockModel(IFastMock fastMock, bool nonPublic = false, ObservableExceptionLog? exceptionLog = null)
        {
            FastMock = fastMock ?? throw new ArgumentNullException(nameof(fastMock));
            Type = fastMock.MockedType ?? throw new ArgumentNullException(nameof(fastMock.MockedType));
            NonPublic = nonPublic;
            this.exceptionLog = exceptionLog;
            // Legacy hydration deferred until first access to Mock (lazy) for performance / provider agnosticism.
        }

        /// <summary>
        /// Legacy constructor used while migration is in progress.
        /// Wraps the provided legacy mock through the registered provider infrastructure.
        /// </summary>
        internal MockModel(Type type, Mock mock, bool nonPublic = false, ObservableExceptionLog? exceptionLog = null)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            legacyMock = mock ?? throw new ArgumentNullException(nameof(mock));
            NonPublic = nonPublic;
            this.exceptionLog = exceptionLog;
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
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                exceptionLog?.Add($"Failed to hydrate legacy Moq surface for tracked mock type {Type.FullName}: {message}");
            }
        }
        #endregion

        #region Equality / Comparison
        /// <summary>
        /// Determines whether the current mock model represents the same mocked type as another object.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true"/> when <paramref name="obj"/> is a <see cref="MockModel"/> for the same mocked type; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj) => IsEqual(this, obj as MockModel);

        /// <summary>
        /// Returns a hash code based on the mocked type represented by this model.
        /// </summary>
        /// <returns>A hash code for the mocked type.</returns>
        [ExcludeFromCodeCoverage]
        public override int GetHashCode() => Type.GetHashCode();

        /// <summary>
        /// Determines whether two mock models should be considered equal based on their mocked type names.
        /// </summary>
        /// <typeparam name="TModel">The concrete mock model type being compared.</typeparam>
        /// <param name="x">The first model to compare.</param>
        /// <param name="y">The second model to compare.</param>
        /// <returns><see langword="true"/> when both models describe the same mocked type; otherwise, <see langword="false"/>.</returns>
        public static bool IsEqual<TModel>(TModel? x, TModel? y) where TModel : MockModel =>
            ReferenceEquals(x, y) || (!IsOneNull(x, y) && IsMockTypeNameEqual(x, y));

        /// <summary>
        /// Determines whether two mock models describe the same mocked type.
        /// </summary>
        /// <param name="a">The first model to compare.</param>
        /// <param name="b">The second model to compare.</param>
        /// <returns><see langword="true"/> when the models are equal; otherwise, <see langword="false"/>.</returns>
        [ExcludeFromCodeCoverage]
        public static bool operator ==(MockModel? a, MockModel? b) => IsEqual(a, b);

        /// <summary>
        /// Determines whether two mock models do not describe the same mocked type.
        /// </summary>
        /// <param name="a">The first model to compare.</param>
        /// <param name="b">The second model to compare.</param>
        /// <returns><see langword="true"/> when the models are not equal; otherwise, <see langword="false"/>.</returns>
        [ExcludeFromCodeCoverage]
        public static bool operator !=(MockModel? a, MockModel? b) => !(a == b);

        /// <summary>
        /// Returns the mocked type name for display purposes.
        /// </summary>
        /// <returns>The simple name of the mocked type.</returns>
        public override string ToString() => Type.Name;

        internal static bool IsMockTypeNameEqual<TModel>(TModel? x, TModel? y) where TModel : MockModel =>
            x?.Type.Name.Equals(y?.Type.Name, StringComparison.OrdinalIgnoreCase) ?? false;

        internal static bool IsOneNull<TModel>(TModel? x, TModel? y) where TModel : MockModel => x as object is null || y as object is null;
        #endregion

        #region IComparable / IEquatable / IEqualityComparer
        /// <summary>
        /// Compares the current model with another object by using the mocked type full name.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>A value indicating the relative sort order of the compared objects.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="obj"/> is not a <see cref="MockModel"/>.</exception>
        public virtual int CompareTo(object? obj) =>
            obj is MockModel mockModel ? CompareTo(mockModel) : throw new ArgumentException("Not a MockModel instance");

        /// <summary>
        /// Compares the current model with another model by using the mocked type full name.
        /// </summary>
        /// <param name="other">The other model to compare against.</param>
        /// <returns>A value indicating the relative sort order of the compared models.</returns>
        public int CompareTo(MockModel? other) => string.Compare(Type.FullName, other?.Type.FullName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether two supplied mock models are equal.
        /// </summary>
        /// <param name="x">The first model to compare.</param>
        /// <param name="y">The second model to compare.</param>
        /// <returns><see langword="true"/> when the models are equal; otherwise, <see langword="false"/>.</returns>
        [ExcludeFromCodeCoverage]
        public bool Equals(MockModel? x, MockModel? y) => IsEqual(x, y);

        /// <summary>
        /// Returns a hash code for the supplied mock model.
        /// </summary>
        /// <param name="obj">The model whose hash code should be returned.</param>
        /// <returns>A hash code for <paramref name="obj"/>.</returns>
        [ExcludeFromCodeCoverage]
        public int GetHashCode(MockModel obj) => GetHashCode();

        /// <summary>
        /// Determines whether the current model equals another model.
        /// </summary>
        /// <param name="other">The other model to compare against.</param>
        /// <returns><see langword="true"/> when the models are equal; otherwise, <see langword="false"/>.</returns>
        [ExcludeFromCodeCoverage]
        public bool Equals(MockModel? other) => IsEqual(this, other);
        #endregion
    }
}

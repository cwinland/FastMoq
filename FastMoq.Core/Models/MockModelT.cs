using FastMoq.Providers;
using System.Diagnostics.CodeAnalysis;

namespace FastMoq.Models
{
    /// <summary>
    ///     Generic mock model wrapper (provider-first, Moq legacy compatible).
    /// </summary>
    public class MockModel<T> : MockModel, IComparable<MockModel<T>>, IEquatable<MockModel<T>>, IEqualityComparer<MockModel<T>> where T : class
    {
        #region Properties
        /// <summary>
        /// Legacy typed Moq mock surface.
        /// Prefer using <see cref="TypedFastMock"/> or <see cref="Instance"/> for provider-neutral access.
        /// </summary>
        [Obsolete("Use TypedFastMock / Instance instead. Will be removed in a future major version.")]
        public new Mock<T> Mock
        {
            get => (Mock<T>) base.Mock;
            internal set
            {
                base.Mock = value;
                // Re-hydrate adapter from legacy mock
                RefreshFastMockFromLegacy();
            }
        }

        /// <summary>
        /// Gets the provider-agnostic typed fast mock wrapper for <typeparamref name="T"/>.
        /// </summary>
        public IFastMock<T> TypedFastMock => (IFastMock<T>) FastMock;

        /// <summary>
        /// Gets the mocked type represented by this model.
        /// </summary>
        public override Type Type => typeof(T);

        /// <summary>
        /// Gets the typed mocked instance produced by the active provider.
        /// </summary>
        public new T Instance => TypedFastMock.Instance;
        #endregion

        #region Construction
        internal MockModel(IFastMock<T> fastMock, bool nonPublic = false) : base(fastMock, nonPublic) { }
        internal MockModel(Mock mock) : base(typeof(T), mock)
        {
            if (mock is not Mock<T> typedMock)
            {
                throw new ArgumentException($"Expected a Mock<{typeof(T).Name}> instance.", nameof(mock));
            }

            FastMock = MockingProviderRegistry.WrapLegacy(typedMock, typeof(T));
        }
        internal MockModel(MockModel baseModel) : base(baseModel.FastMock, baseModel.NonPublic, baseModel.ExceptionLog) { }
        #endregion

        #region Comparison / Equality
        /// <summary>
        /// Compares the current model with another object by using the mocked type full name.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>A value indicating the relative sort order of the compared objects.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="obj"/> is not a <see cref="MockModel{T}"/>.</exception>
        public override int CompareTo(object? obj) =>
            obj is MockModel<T> mockModel ? CompareTo(mockModel) : throw new ArgumentException($"Not a MockModel<{typeof(T).Name}> instance");

        /// <summary>
        /// Determines whether the current typed mock model represents the same mocked type as another object.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true"/> when <paramref name="obj"/> is a matching <see cref="MockModel{T}"/>; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj) => IsEqual(this, obj as MockModel<T>);

        /// <summary>
        /// Returns a hash code based on the mocked type represented by this model.
        /// </summary>
        /// <returns>A hash code for the mocked type.</returns>
        [ExcludeFromCodeCoverage]
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// Determines whether two typed mock models describe the same mocked type.
        /// </summary>
        /// <param name="a">The first model to compare.</param>
        /// <param name="b">The second model to compare.</param>
        /// <returns><see langword="true"/> when the models are equal; otherwise, <see langword="false"/>.</returns>
        [ExcludeFromCodeCoverage]
        public static bool operator ==(MockModel<T>? a, MockModel<T>? b) => IsEqual(a, b);

        /// <summary>
        /// Determines whether two typed mock models do not describe the same mocked type.
        /// </summary>
        /// <param name="a">The first model to compare.</param>
        /// <param name="b">The second model to compare.</param>
        /// <returns><see langword="true"/> when the models are not equal; otherwise, <see langword="false"/>.</returns>
        [ExcludeFromCodeCoverage]
        public static bool operator !=(MockModel<T>? a, MockModel<T>? b) => !IsEqual(a, b);

        /// <summary>
        /// Compares the current typed model with another typed model by using the mocked type full name.
        /// </summary>
        /// <param name="other">The other model to compare against.</param>
        /// <returns>A value indicating the relative sort order of the compared models.</returns>
        [ExcludeFromCodeCoverage]
        public int CompareTo(MockModel<T>? other) => base.CompareTo(other);

        /// <summary>
        /// Determines whether two supplied typed mock models are equal.
        /// </summary>
        /// <param name="x">The first model to compare.</param>
        /// <param name="y">The second model to compare.</param>
        /// <returns><see langword="true"/> when the models are equal; otherwise, <see langword="false"/>.</returns>
        [ExcludeFromCodeCoverage]
        public bool Equals(MockModel<T>? x, MockModel<T>? y) => IsEqual(x, y);

        /// <summary>
        /// Returns a hash code for the supplied typed mock model.
        /// </summary>
        /// <param name="obj">The model whose hash code should be returned.</param>
        /// <returns>A hash code for <paramref name="obj"/>.</returns>
        [ExcludeFromCodeCoverage]
        public int GetHashCode(MockModel<T> obj) => base.GetHashCode(obj);

        /// <summary>
        /// Determines whether the current typed model equals another typed model.
        /// </summary>
        /// <param name="other">The other model to compare against.</param>
        /// <returns><see langword="true"/> when the models are equal; otherwise, <see langword="false"/>.</returns>
        [ExcludeFromCodeCoverage]
        public bool Equals(MockModel<T>? other) => IsEqual(this, other);
        #endregion
    }
}

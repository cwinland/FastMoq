using Moq;
using System.Diagnostics.CodeAnalysis;

namespace FastMoq.Models
{
    /// <summary>
    ///     Contains Mock and Type information.
    /// </summary>
    /// <inheritdoc cref="IComparable{MockModel}" />
    /// <inheritdoc cref="IComparable" />
    /// <inheritdoc cref="IEquatable{MockModel}" />
    /// <inheritdoc cref="IEqualityComparer{MockModel}" />
    public class MockModel : IComparable<MockModel>, IComparable, IEquatable<MockModel>, IEqualityComparer<MockModel>
    {
        #region Properties

        /// <summary>
        ///     Gets or sets the mock.
        /// </summary>
        /// <value>The mock.</value>
        public Mock Mock { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether [non public].
        /// </summary>
        /// <value><c>true</c> if [non public]; otherwise, <c>false</c>.</value>
        public bool NonPublic { get; set; }

        /// <summary>
        ///     Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public virtual Type Type { get; }

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockModel" /> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="mock">The mock.</param>
        /// <param name="nonPublic">if set to <c>true</c> [non public].</param>
        /// <exception cref="System.ArgumentNullException">type</exception>
        /// <exception cref="System.ArgumentNullException">mock</exception>
        internal MockModel(Type type, Mock mock, bool nonPublic = false)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Mock = mock ?? throw new ArgumentNullException(nameof(mock));
            NonPublic = nonPublic;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => IsEqual(this, obj as MockModel);

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override int GetHashCode() => Type.GetHashCode();

        /// <summary>
        ///     Determines whether the specified x is equal.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns><c>true</c> if the specified x is equal; otherwise, <c>false</c>.</returns>
        public static bool IsEqual<T>(T? x, T? y) where T : MockModel =>

            // If both are null, or both are same instance, return true.
            ReferenceEquals(x, y) ||

            // If one is null, but not both, return false.
            (!IsOneNull(x, y) &&

             // Return true if the fields match:
             IsMockTypeNameEqual(x, y));

        /// <summary>
        ///     Implements the == operator.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        [ExcludeFromCodeCoverage]
        public static bool operator ==(MockModel? a, MockModel? b) => IsEqual(a, b);

        /// <summary>
        ///     Implements the != operator.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        [ExcludeFromCodeCoverage]
        public static bool operator !=(MockModel? a, MockModel? b) => !(a == b);

        /// <inheritdoc />
        public override string ToString() => Type.Name;

        internal static bool IsMockTypeNameEqual<T>(T? x, T? y) where T : MockModel =>
            x?.Type.Name.Equals(y?.Type.Name, StringComparison.OrdinalIgnoreCase) ?? false;

        /// <summary>
        ///     Determines whether [is one null] [the specified x].
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns><c>true</c> if [is one null] [the specified x]; otherwise, <c>false</c>.</returns>

        // Safe cast prevents from calling overloaded equality.
        // ReSharper disable SafeCastIsUsedAsTypeCheck
        internal static bool IsOneNull<T>(T? x, T? y) where T : MockModel => x as object is null || y as object is null;

        #region IComparable

        /// <inheritdoc />
        public virtual int CompareTo(object? obj) =>
            obj is MockModel mockModel ? CompareTo(mockModel) : throw new ArgumentException("Not a MockModel instance");

        #endregion

        #region IComparable<MockModel>

        /// <inheritdoc />
        public int CompareTo(MockModel? other) => string.Compare(Type.FullName, other?.Type.FullName, StringComparison.OrdinalIgnoreCase);

        #endregion

        #region IEqualityComparer<MockModel>

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public bool Equals(MockModel? x, MockModel? y) => IsEqual(x, y);

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public int GetHashCode(MockModel obj) => GetHashCode();

        #endregion

        #region IEquatable<MockModel>

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public bool Equals(MockModel? other) => IsEqual(this, other);

        #endregion
    }
}

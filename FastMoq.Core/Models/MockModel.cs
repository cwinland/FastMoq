using Moq;
using System.Diagnostics.CodeAnalysis;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class MockModel.
    ///     Implements the <see cref="MockModel" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="MockModel" />
    /// <inheritdoc cref="MockModel" />
    [ExcludeFromCodeCoverage]
    public class MockModel<T> : MockModel, IComparable<MockModel<T>>, IEquatable<MockModel<T>>, IEqualityComparer<MockModel<T>> where T : class
    {
        #region Properties

        /// <summary>
        ///     Gets or sets the mock.
        /// </summary>
        /// <value>The mock.</value>
        public new Mock<T> Mock
        {
            get => (Mock<T>) base.Mock;
            set => base.Mock = value;
        }

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockModel{T}" /> class.
        /// </summary>
        /// <param name="mock">The mock.</param>
        /// <inheritdoc />
        internal MockModel(Mock mock) : base(typeof(T), mock) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.MockModel`1" /> class.
        /// </summary>
        /// <param name="mockModel">The mock model.</param>
        /// <inheritdoc />
        internal MockModel(MockModel mockModel) : base(mockModel.Type, mockModel.Mock) { }

        /// <inheritdoc />
        public override int CompareTo(object? obj) =>
            obj is MockModel<T> mockModel ? CompareTo(mockModel) : throw new Exception($"Not a MockModel<{typeof(T)}> instance");

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as MockModel<T>);

        /// <inheritdoc />
        public override int GetHashCode() => Type.GetHashCode();

        /// <summary>
        ///     Implements the == operator.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(MockModel<T> a, MockModel<T> b) => object.Equals(a, b);

        /// <summary>
        ///     Implements the != operator.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(MockModel<T> a, MockModel<T> b) => !(a == b);

        #region IComparable<MockModel<T>>

        /// <inheritdoc />
        public int CompareTo(MockModel<T>? other) => base.CompareTo(other);

        #endregion

        #region IEqualityComparer<MockModel<T>>

        /// <summary>
        ///     Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object of type <paramref name="T" /> to compare.</param>
        /// <param name="y">The second object of type <paramref name="T" /> to compare.</param>
        /// <returns><see langword="true" /> if the specified objects are equal; otherwise, <see langword="false" />.</returns>
        public bool Equals(MockModel<T>? x, MockModel<T>? y)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (x as object is null || y as object is null)
            {
                return false;
            }

            // Return true if the fields match:
            return x.Type.Name.Equals(y.Type.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public int GetHashCode(MockModel<T> obj) => obj.Type.GetHashCode();

        #endregion

        #region IEquatable<MockModel<T>>

        /// <summary>
        ///     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///     <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public bool Equals(MockModel<T>? other) => Equals(this, other);

        #endregion
    }

    /// <summary>
    ///     Contains Mock and Type information.
    /// </summary>
    /// <inheritdoc cref="IComparable{T}" />
    /// <inheritdoc cref="IComparable" />
    /// <inheritdoc cref="IEquatable{T}" />
    /// <inheritdoc cref="IEqualityComparer{T}" />
    [ExcludeFromCodeCoverage]
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
        public Type Type { get; }

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockModel" /> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="mock">The mock.</param>
        /// <param name="nonPublic">if set to <c>true</c> [non public].</param>
        /// <exception cref="ArgumentNullException">type</exception>
        /// <exception cref="ArgumentNullException">mock</exception>
        internal MockModel(Type type, Mock mock, bool nonPublic = false)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Mock = mock ?? throw new ArgumentNullException(nameof(mock));
            NonPublic = nonPublic;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as MockModel);

        /// <inheritdoc />
        public override int GetHashCode() => Type.GetHashCode();

        /// <summary>
        ///     Implements the == operator.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(MockModel? a, MockModel? b) => object.Equals(a, b);

        /// <summary>
        ///     Implements the != operator.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(MockModel? a, MockModel? b) => !(a == b);

        /// <inheritdoc />
        public override string ToString() => Type.Name;

        #region IComparable

        /// <inheritdoc />
        public virtual int CompareTo(object? obj) =>
            obj is MockModel mockModel ? CompareTo(mockModel) : throw new Exception("Not a MockModel instance");

        #endregion

        #region IComparable<MockModel>

        /// <inheritdoc />
        public int CompareTo(MockModel? other) => string.Compare(Type.FullName, other?.Type.FullName, StringComparison.OrdinalIgnoreCase);

        #endregion

        #region IEqualityComparer<MockModel>

        /// <summary>
        ///     Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object of type <paramref name="T" /> to compare.</param>
        /// <param name="y">The second object of type <paramref name="T" /> to compare.</param>
        /// <returns><see langword="true" /> if the specified objects are equal; otherwise, <see langword="false" />.</returns>
        public bool Equals(MockModel? x, MockModel? y)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (x as object is null || y as object is null)
            {
                return false;
            }

            // Return true if the fields match:
            return x.Type.Name.Equals(y.Type.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public int GetHashCode(MockModel obj) => obj.Type.GetHashCode();

        #endregion

        #region IEquatable<MockModel>

        /// <summary>
        ///     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///     <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public bool Equals(MockModel? other) => Equals(this, other);

        #endregion
    }
}

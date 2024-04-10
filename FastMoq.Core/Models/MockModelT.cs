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

        /// <inheritdoc />
        public override Type Type => typeof(T);

        #endregion

        /// <inheritdoc />
        internal MockModel(Mock mock) : base(typeof(T), mock) { }

        /// <inheritdoc />
        internal MockModel(MockModel mockModel) : base(mockModel.Type, mockModel.Mock) { }

        /// <inheritdoc />
        public override int CompareTo(object? obj) =>
            obj is MockModel<T> mockModel ? CompareTo(mockModel) : throw new ArgumentException($"Not a MockModel<{typeof(T)}> instance");

        /// <inheritdoc />
        public override bool Equals(object? obj) => IsEqual(this, obj as MockModel<T>);

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        ///     Implements the == operator.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        [ExcludeFromCodeCoverage]
        public static bool operator ==(MockModel<T> a, MockModel<T> b) => IsEqual(a, b);

        /// <summary>
        ///     Implements the != operator.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        [ExcludeFromCodeCoverage]
        public static bool operator !=(MockModel<T> a, MockModel<T> b) => !IsEqual(a, b);

        #region IComparable<MockModel<T>>

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public int CompareTo(MockModel<T>? other) => base.CompareTo(other);

        #endregion

        #region IEqualityComparer<MockModel<T>>

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public bool Equals(MockModel<T>? x, MockModel<T>? y) => IsEqual(x, y);

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public int GetHashCode(MockModel<T> obj) => base.GetHashCode(obj);

        #endregion

        #region IEquatable<MockModel<T>>

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public bool Equals(MockModel<T>? other) => IsEqual(this, other);

        #endregion
    }
}

using Moq;

namespace FastMoq
{
    /// <summary>
    ///     Class MockModel.
    /// Implements the <see cref="FastMoq.MockModel" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="FastMoq.MockModel" />
    public class MockModel<T> : MockModel where T : class
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
        ///     Initializes a new instance of the <see cref="MockModel{T}"/> class.
        /// </summary>
        /// <param name="mock">The mock.</param>
        internal MockModel(Mock mock) : base(typeof(T), mock) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockModel{T}"/> class.
        /// </summary>
        /// <param name="mockModel">The mock model.</param>
        internal MockModel(MockModel mockModel) : base(mockModel.Type, mockModel.Mock) { }
    }

    /// <summary>
    ///     Contains Mock and Type information.
    /// </summary>
    public class MockModel
    {
        #region Properties

        /// <summary>
        ///     Gets or sets the mock.
        /// </summary>
        /// <value>The mock.</value>
        public Mock Mock { get; set; }

        /// <summary>
        ///     Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public Type Type { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether [non public].
        /// </summary>
        /// <value><c>true</c> if [non public]; otherwise, <c>false</c>.</value>
        public bool NonPublic { get; set; } = false;

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockModel"/> class.
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
    }
}
using Moq;

namespace FastMoq
{
    /// <summary>
    /// Class MockModel.
    /// </summary>
    public class MockModel
    {
        #region Properties

        /// <summary>
        /// Gets or sets the mock.
        /// </summary>
        /// <value>The mock.</value>
        public Mock Mock { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public Type Type { get; set; }

        #endregion

        internal MockModel(Type type, Mock mock)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Mock = mock ?? throw new ArgumentNullException(nameof(mock));
        }
    }
}

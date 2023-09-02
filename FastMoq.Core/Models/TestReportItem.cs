using System.Reflection;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class TestReportItem.
    /// </summary>
        /// <inheritdoc />
    public class TestReportItem : ITestReportItem
    {
        /// <summary>
        ///     Gets or sets the method.
        /// </summary>
        /// <value>The method.</value>
        /// <inheritdoc />
        public MethodBase Method { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is error thrown.
        /// </summary>
        /// <value><c>true</c> if this instance is error thrown; otherwise, <c>false</c>.</value>
        /// <inheritdoc />
        public bool IsErrorThrown { get; set; }

        /// <inheritdoc />
        /// <summary>
        ///     Gets or sets the error.
        /// </summary>
        /// <value>The error.</value>
        public Exception? Error { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TestReportItem"/> class.
        /// </summary>
        /// <param name="method">The method.</param>
        public TestReportItem(MethodBase method) => Method = method;
    }

    /// <summary>
    ///     Interface ITestReportItem
    /// </summary>
    public interface ITestReportItem
    {
        /// <summary>
        ///     Gets or sets the method.
        /// </summary>
        /// <value>The method.</value>
        public MethodBase Method { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is error thrown.
        /// </summary>
        /// <value><c>true</c> if this instance is error thrown; otherwise, <c>false</c>.</value>
        bool IsErrorThrown { get; set; }
        /// <summary>
        ///     Gets or sets the error.
        /// </summary>
        /// <value>The error.</value>
        Exception? Error { get; set; }
    }
}

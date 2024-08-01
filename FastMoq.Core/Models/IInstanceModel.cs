namespace FastMoq.Models
{
    /// <inheritdoc />
    /// <summary>
    ///     Interface IInstanceModel
    /// </summary>
    public interface IInstanceModel : IHistoryModel
    {
        #region Properties

        /// <summary>
        ///     Gets the type.
        /// </summary>
        /// <value>The type.</value>
        Type Type { get; }

        /// <summary>
        ///     Gets the create function.
        /// </summary>
        /// <value>The create function.</value>
        InstanceFunction? CreateFunc { get; }

        /// <summary>
        ///     Gets the type of the instance.
        /// </summary>
        /// <value>The type of the instance.</value>
        Type InstanceType { get; }

        /// <summary>
        ///     Gets the arguments.
        /// </summary>
        /// <value>The arguments.</value>
        List<object?> Arguments { get; }

        #endregion
    }
}
namespace FastMoq.Models
{
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
        Func<Mocker, object>? CreateFunc { get; }

        /// <summary>
        ///     Gets the type of the instance.
        /// </summary>
        /// <value>The type of the instance.</value>
        Type InstanceType { get; }

        #endregion
    }
}
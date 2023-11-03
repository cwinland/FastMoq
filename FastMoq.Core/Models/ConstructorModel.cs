using System.Reflection;

namespace FastMoq.Models
{
    /// <inheritdoc />
    /// <summary>
    ///     Class ConstructorModel.
    /// </summary>
    public class ConstructorModel : IHistoryModel
    {
        #region Properties

        /// <summary>
        ///     Gets the constructor information.
        /// </summary>
        /// <value>The constructor information.</value>
        public ConstructorInfo? ConstructorInfo { get; }
        /// <summary>
        ///     Gets the parameter list.
        /// </summary>
        /// <value>The parameter list.</value>
        public object?[] ParameterList { get; }

        #endregion

        internal ConstructorModel(ConstructorInfo? constructorInfo, List<object?> parameterList)
        {
            ConstructorInfo = constructorInfo;
            ParameterList = parameterList.ToArray();
        }

        internal ConstructorModel(KeyValuePair<ConstructorInfo, List<object?>> kvp) : this(kvp.Key, kvp.Value) { }
    }
}

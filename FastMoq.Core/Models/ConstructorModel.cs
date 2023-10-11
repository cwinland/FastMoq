using System.Reflection;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class ConstructorModel.
    /// </summary>
    public class ConstructorModel : IHistoryModel
    {
        #region Properties

        public ConstructorInfo? ConstructorInfo { get; }
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

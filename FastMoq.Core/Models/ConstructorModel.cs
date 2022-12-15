using System.Reflection;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class ConstructorModel.
    /// </summary>
    internal class ConstructorModel
    {
        #region Properties

        internal ConstructorInfo? ConstructorInfo { get; }
        internal object?[] ParameterList { get; }

        #endregion

        internal ConstructorModel(ConstructorInfo? constructorInfo, List<object?> parameterList)
        {
            ConstructorInfo = constructorInfo;
            ParameterList = parameterList.ToArray();
        }

        internal ConstructorModel(KeyValuePair<ConstructorInfo, List<object?>> kvp) : this(kvp.Key, kvp.Value) { }
    }
}

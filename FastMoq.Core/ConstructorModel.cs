using System.Reflection;

namespace FastMoq
{
    internal class ConstructorModel
    {
        internal ConstructorInfo? ConstructorInfo { get; }
        internal object?[] ParameterList { get; }

        internal ConstructorModel(ConstructorInfo? constructorInfo, List<object?> parameterList)
        {
            ConstructorInfo = constructorInfo;
            ParameterList = parameterList.ToArray();
        }

        internal ConstructorModel(KeyValuePair<ConstructorInfo, List<object?>> kvp) : this(kvp.Key, kvp.Value)
        {

        }
    }
}

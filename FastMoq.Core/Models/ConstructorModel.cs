using System.Reflection;

namespace FastMoq.Models
{
    /// <inheritdoc />
    /// <summary>
    ///     Class ConstructorModel.
    /// </summary>
    public class ConstructorModel : IHistoryModel, IEquatable<ConstructorModel>
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

        #region Overrides of Object

        /// <inheritdoc />
        public override string ToString() => $"{ConstructorInfo}-{ParameterString}";

        #endregion

        internal string ParameterString => string.Join(':', ParameterList);

        internal ConstructorModel(ConstructorInfo? constructorInfo, IEnumerable<object?> parameterList)
        {
            ConstructorInfo = constructorInfo;
            ParameterList = parameterList?.ToArray() ?? new object?[] { };
        }

        internal ConstructorModel(KeyValuePair<ConstructorInfo, List<object?>> kvp) : this(kvp.Key, kvp.Value) { }

        #region Equality members

        /// <inheritdoc />
        public bool Equals(ConstructorModel? other)
        {
            return other is not null && (ReferenceEquals(this, other) || Equals(ToString(), other.ToString()));
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is not null && (ReferenceEquals(this, obj) || (obj.GetType() == GetType() && Equals((ConstructorModel) obj)));
        }

        /// <inheritdoc />
        public override int GetHashCode() => ToString().GetHashCode();

        #endregion
    }
}

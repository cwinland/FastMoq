using System.Diagnostics.CodeAnalysis;

namespace FastMoq.Models
{
    /// <inheritdoc />
    /// <summary>
    ///     Class InstanceModel.
    ///     Implements the <see cref="T:FastMoq.InstanceModel" />
    /// </summary>
    /// <typeparam name="TClass">The type of the t class.</typeparam>
    /// <seealso cref="T:FastMoq.InstanceModel" />
    [ExcludeFromCodeCoverage]
    public class InstanceModel<TClass> : InstanceModel where TClass : class
    {
        #region Properties

        /// <inheritdoc />
        public override Type InstanceType => typeof(TClass);

        /// <summary>
        ///     Gets or sets the create function.
        /// </summary>
        /// <value>The create function.</value>
        public new Func<Mocker, TClass>? CreateFunc
        {
            get => (Func<Mocker, TClass>?) base.CreateFunc;
            set => base.CreateFunc = value;
        }

        #endregion

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.InstanceModel`1" /> class.
        /// </summary>
        public InstanceModel() : this(default(Func<Mocker, TClass>)) { }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.InstanceModel`1" /> class.
        /// </summary>
        /// <param name="createFunc">The create function.</param>
        public InstanceModel(Func<Mocker, TClass>? createFunc) : base(typeof(TClass), typeof(TClass), createFunc) { }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="InstanceModel{TClass}" /> class.
        /// </summary>
        /// <param name="createFunc">The create function.</param>
        /// <param name="arguments">The arguments.</param>
        public InstanceModel(Func<Mocker, TClass>? createFunc, List<object?> arguments) : this(createFunc) =>
            Arguments = arguments;

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.Models.InstanceModel{T}" /> class.
        /// </summary>
        /// <param name="model">The model.</param>
        public InstanceModel(InstanceModel model) : this(model.CreateFunc as Func<Mocker, TClass>, model.Arguments) { }
    }
}

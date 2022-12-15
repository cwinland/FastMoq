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

        /// <summary>
        ///     Gets or sets the create function.
        /// </summary>
        /// <value>The create function.</value>
        public new Func<Mocker, TClass>? CreateFunc
        {
            get => (Func<Mocker, TClass>?)base.CreateFunc;
            set => base.CreateFunc = value;
        }

        #endregion

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.InstanceModel`1" /> class.
        /// </summary>
        public InstanceModel() : this(null) { }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.InstanceModel`1" /> class.
        /// </summary>
        /// <param name="createFunc">The create function.</param>
        public InstanceModel(Func<Mocker, TClass>? createFunc) : base(typeof(TClass), createFunc) { }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="InstanceModel{TClass}"/> class.
        /// </summary>
        /// <param name="createFunc">The create function.</param>
        /// <param name="arguments">The arguments.</param>
        public InstanceModel(Func<Mocker, TClass>? createFunc, List<object> arguments) : this(createFunc) =>
            Arguments = arguments;
    }

    /// <summary>
    ///     Class InstanceModel.
    /// Implements the <see cref="InstanceModel" />
    /// </summary>
    /// <seealso cref="InstanceModel" />
    [ExcludeFromCodeCoverage]
    public class InstanceModel
    {
        #region Properties

        /// <summary>
        ///     Gets or sets the type of the instance.
        /// </summary>
        /// <value>The type of the instance.</value>
        public Type InstanceType { get; }

        /// <summary>
        ///     Gets or sets the create function.
        /// </summary>
        /// <value>The create function.</value>
        public Func<Mocker, object>? CreateFunc { get; internal set; }

        /// <summary>
        ///     Gets the arguments.
        /// </summary>
        /// <value>The arguments.</value>
        public List<object> Arguments { get; internal set; } = new();

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="InstanceModel" /> class.
        /// </summary>
        /// <param name="instanceType">Type of the instance.</param>
        /// <exception cref="ArgumentNullException">instanceType</exception>
        internal InstanceModel(Type instanceType) =>
            InstanceType = instanceType ?? throw new ArgumentNullException(nameof(instanceType));

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.InstanceModel" /> class.
        /// </summary>
        /// <param name="instanceType">Type of the instance.</param>
        /// <param name="createFunc"></param>
        /// <exception cref="T:System.ArgumentNullException">arguments</exception>
        internal InstanceModel(Type instanceType, Func<Mocker, object>? createFunc) : this(instanceType) => CreateFunc = createFunc;

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.InstanceModel" /> class.
        /// </summary>
        /// <param name="instanceType">Type of the instance.</param>
        /// <param name="createFunc"></param>
        /// <param name="arguments">The arguments.</param>
        /// <exception cref="T:System.ArgumentNullException">arguments</exception>
        internal InstanceModel(Type instanceType, Func<Mocker, object>? createFunc, List<object> arguments) : this(instanceType, createFunc) => Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
    }
}
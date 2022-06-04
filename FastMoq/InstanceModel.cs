namespace FastMoq
{
    /// <inheritdoc />
    /// <summary>
    ///     Class InstanceModel.
    ///     Implements the <see cref="T:FastMoq.InstanceModel" />
    /// </summary>
    /// <typeparam name="TClass">The type of the t class.</typeparam>
    /// <seealso cref="T:FastMoq.InstanceModel" />
    public class InstanceModel<TClass> : InstanceModel where TClass : class
    {
        #region Properties

        /// <summary>
        /// Gets or sets the create function.
        /// </summary>
        /// <value>The create function.</value>
        public new Func<Mocker, TClass>? CreateFunc
        {
            get => (Func<Mocker, TClass>?) base.CreateFunc;
            set => base.CreateFunc = value;
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceModel{TClass}" /> class.
        /// </summary>
        public InstanceModel() : base(typeof(TClass))
        {
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.InstanceModel`1" /> class.
        /// </summary>
        /// <param name="createFunc">The create function.</param>
        public InstanceModel(Func<Mocker, TClass>? createFunc) : this() => CreateFunc = createFunc;
    }

    /// <summary>
    /// Class InstanceModel.
    /// Implements the <see cref="FastMoq.InstanceModel" />
    /// </summary>
    /// <seealso cref="FastMoq.InstanceModel" />
    public class InstanceModel
    {
        #region Properties

        /// <summary>
        /// Gets or sets the type of the instance.
        /// </summary>
        /// <value>The type of the instance.</value>
        public Type InstanceType { get; }

        /// <summary>
        /// Gets or sets the create function.
        /// </summary>
        /// <value>The create function.</value>
        public Func<Mocker, object>? CreateFunc { get; internal set; }

        #endregion

        internal InstanceModel(Type instanceType) =>
            InstanceType = instanceType ?? throw new ArgumentNullException(nameof(instanceType));
    }
}

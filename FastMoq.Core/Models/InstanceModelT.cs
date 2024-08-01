using System.Diagnostics.CodeAnalysis;

namespace FastMoq.Models
{
    /// <inheritdoc />
    /// <summary>
    ///     Class InstanceModel{TClass} represents a type and how to create that type's instance.
    ///     Implements the <see cref="InstanceModel" />
    /// </summary>
    /// <typeparam name="TClass">The type of the t class.</typeparam>
    /// <seealso cref="InstanceModel" />
    [ExcludeFromCodeCoverage]
    public class InstanceModel<TClass> : InstanceModel
    {
        #region Properties

        /// <inheritdoc />
        public override Type InstanceType => typeof(TClass);

        /// <summary>
        ///     Gets or sets the create function.
        /// </summary>
        /// <value>The create function.</value>
        public new InstanceFunction? CreateFunc
        {
            get => base.CreateFunc;
            set => base.CreateFunc = value;
        }

        #endregion

        /// <inheritdoc />
        public InstanceModel() : this(default(Func<Mocker, TClass>)) { }

        public InstanceModel(Func<Mocker, object?, TClass>? createFunc) : base(typeof(TClass), typeof(TClass), createFunc) { }

        /// <inheritdoc />
        public InstanceModel(Func<Mocker, object?, TClass>? createFunc, List<object?> arguments) : this(createFunc) =>
            Arguments = arguments;

        /// <inheritdoc />
        public InstanceModel(Func<Mocker, TClass>? createFunc) : base(typeof(TClass), typeof(TClass), createFunc) { }

        /// <inheritdoc />
        public InstanceModel(Func<Mocker, TClass>? createFunc, List<object?> arguments) : this(createFunc) =>
            Arguments = arguments;

        /// <inheritdoc />
        public InstanceModel(IInstanceModel model) : this(model?.CreateFunc as Func<Mocker, TClass>, model?.Arguments ?? []) { }
    }
}

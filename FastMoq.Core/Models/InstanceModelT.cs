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

        /// <summary>
        /// Initializes a new instance model that creates <typeparamref name="TClass"/> values by using a factory delegate that can inspect the current <see cref="Mocker"/> and an optional context value.
        /// </summary>
        /// <param name="createFunc">The factory delegate used to create instances, or <see langword="null"/> to rely on constructor-based resolution.</param>
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

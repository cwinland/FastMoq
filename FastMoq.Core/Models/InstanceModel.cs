﻿using System.Diagnostics.CodeAnalysis;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class InstanceModel represents a type and how to create that type's instance.
    ///     Implements the <see cref="InstanceModel" />
    /// </summary>
    /// <inheritdoc cref="IHistoryModel" />
    /// <inheritdoc cref="IInstanceModel" />
    /// <seealso cref="InstanceModel" />
    [ExcludeFromCodeCoverage]
    public class InstanceModel : IInstanceModel
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InstanceModel" /> class.
        /// </summary>
        /// <param name="originalType">Type of the original.</param>
        /// <param name="instanceType">Type of the instance.</param>
        /// <exception cref="System.ArgumentNullException">instanceType</exception>
        internal InstanceModel(Type originalType, Type instanceType)
        {
            Type = originalType;
            InstanceType = instanceType ?? throw new ArgumentNullException(nameof(instanceType));
        }

        internal InstanceModel(Type originalType, Type instanceType, Delegate? createFunc) : this(originalType, instanceType)
        {
            if (createFunc != null)
            {
                CreateFunc = new InstanceFunction(instanceType) { Function = createFunc };
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="InstanceModel" /> class.
        /// </summary>
        /// <param name="originalType">Type of the original.</param>
        /// <param name="instanceType">Type of the instance.</param>
        /// <param name="createFunc">The create function.</param>
        /// <param name="arguments">The arguments.</param>
        /// <exception cref="ArgumentNullException">arguments</exception>
        /// <inheritdoc />
        internal InstanceModel(Type originalType, Type instanceType, Func<Mocker, object>? createFunc, IReadOnlyList<object?> arguments)
            : this(originalType, instanceType, createFunc) =>
            Arguments = [..arguments ?? throw new ArgumentNullException(nameof(arguments))];

        /// <summary>
        ///     Initializes a new instance of the <see cref="InstanceModel" /> class.
        /// </summary>
        /// <param name="originalType">Type of the original.</param>
        /// <param name="instanceType">Type of the instance.</param>
        /// <param name="createFunc">The create function.</param>
        /// <param name="arguments">The arguments.</param>
        /// <exception cref="ArgumentNullException">arguments</exception>
        /// <inheritdoc />
        internal InstanceModel(Type originalType, Type instanceType, Func<Mocker, object, object>? createFunc, IReadOnlyList<object?> arguments)
            : this(originalType, instanceType, createFunc) =>
            Arguments = [..arguments ?? throw new ArgumentNullException(nameof(arguments))];

        #region IInstanceModel

        /// <inheritdoc />
        public List<object?> Arguments { get; internal set; } = [];

        /// <inheritdoc />
        public InstanceFunction? CreateFunc { get; internal set; }

        /// <inheritdoc />
        public virtual Type InstanceType { get; }

        /// <inheritdoc />
        public virtual Type Type { get; }

        #endregion
    }
}

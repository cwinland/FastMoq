﻿using System.Diagnostics.CodeAnalysis;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class InstanceModel.
    /// Implements the <see cref="InstanceModel" />
    /// </summary>
    /// <inheritdoc cref="IHistoryModel" />
    /// <inheritdoc cref="IInstanceModel" />
    /// <seealso cref="InstanceModel" />
    [ExcludeFromCodeCoverage]
    public class InstanceModel : IInstanceModel
    {
        #region Properties

        /// <inheritdoc />
        public virtual Type Type { get; }

        /// <inheritdoc />
        public virtual Type InstanceType { get; }

        /// <inheritdoc />
        public Func<Mocker, object>? CreateFunc { get; internal set; }

        /// <inheritdoc />
        public List<object?> Arguments { get; internal set; } = new();

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="InstanceModel" /> class.
        /// </summary>
        /// <param name="originalType">Type of the original.</param>
        /// <param name="instanceType">Type of the instance.</param>
        internal InstanceModel(Type originalType, Type instanceType)
        {
            Type = originalType;
            InstanceType = instanceType ?? throw new ArgumentNullException(nameof(instanceType));
        }

        /// <inheritdoc />
        internal InstanceModel(Type originalType, Type instanceType, Func<Mocker, object>? createFunc) : this(originalType, instanceType)
            => CreateFunc = createFunc;

        /// <summary>
        ///     Initializes a new instance of the <see cref="InstanceModel"/> class.
        /// </summary>
        /// <param name="originalType">Type of the original.</param>
        /// <param name="instanceType">Type of the instance.</param>
        /// <param name="createFunc">The create function.</param>
        /// <param name="arguments">The arguments.</param>
        /// <exception cref="ArgumentNullException">arguments</exception>
        /// <inheritdoc />
        internal InstanceModel(Type originalType, Type instanceType, Func<Mocker, object>? createFunc, List<object?> arguments)
            : this(originalType, instanceType, createFunc) =>
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
    }
}
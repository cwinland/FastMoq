using FastMoq.Extensions;

namespace FastMoq.Models
{
    /// <summary>
    /// Stores a late-bound factory delegate that can create an instance of a specific runtime type.
    /// </summary>
    /// <param name="type">The runtime type produced by the stored factory delegate.</param>
    public class InstanceFunction(Type type)
    {
        /// <summary>
        /// Gets the runtime type that this factory produces.
        /// </summary>
        public Type InstanceType { get; } = type;

        /// <summary>
        /// Gets or sets the factory delegate used to create the instance.
        /// </summary>
        public Delegate? Function { get; set; }

        /// <summary>
        /// Assigns the factory delegate used to create the instance.
        /// </summary>
        /// <param name="function">The delegate to store for later invocation.</param>
        public void SetFunction(Delegate function)
        {
            this.Function = function;
        }

        /// <summary>
        /// Invokes the stored factory delegate with the supplied <see cref="Mocker"/> and optional parameter values.
        /// </summary>
        /// <param name="mocker">The active mocker used as the first delegate argument.</param>
        /// <param name="parameters">Optional extra parameters passed to delegates that accept a second argument.</param>
        /// <returns>The created instance, or <see langword="null"/> when the stored delegate returns <see langword="null"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no compatible delegate has been assigned.</exception>
        public object? Invoke(Mocker mocker, params object?[]? parameters)
        {
            Function.RaiseIfNull();
            var functionType = Function.GetType();
            var genericArguments = functionType.GenericTypeArguments;

            return genericArguments.Length switch
            {
                2 => InvokeSingleParamFunction(mocker),
                3 => InvokeDoubleParamFunction(mocker, parameters),
                _ => throw new InvalidOperationException("No valid function provided or incorrect number of parameters.")
            };
        }

        private object? InvokeSingleParamFunction(Mocker mocker)
        {
            Function.RaiseIfNull();
            var method = Function.GetType().GetMethod("Invoke");
            if (method != null)
            {
                return method.Invoke(Function, [mocker]);
            }

            throw new InvalidOperationException("Function is not a valid single parameter function.");
        }

        private object? InvokeDoubleParamFunction(Mocker mocker, object?[]? parameters)
        {
            Function.RaiseIfNull();
            var method = Function.GetType().GetMethod("Invoke");
            if (method != null)
            {
                return method.Invoke(Function, [mocker, parameters?[0] ?? null]);
            }

            throw new InvalidOperationException("Function is not a valid double parameter function.");
        }

        /// <summary>
        /// Creates a new <see cref="InstanceFunction"/> placeholder for the specified runtime type.
        /// </summary>
        /// <param name="type">The runtime type the instance function should describe.</param>
        /// <returns>A new <see cref="InstanceFunction"/> for <paramref name="type"/>.</returns>
        public static InstanceFunction CreateInstance(Type type)
        {
            return new InstanceFunction(type);
        }
    }

    /// <summary>
    /// Stores a strongly typed factory delegate that creates instances of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The runtime type produced by the stored factory delegate.</typeparam>
    public class InstanceFunction<T> : InstanceFunction
    {
        /// <summary>
        /// Initializes an empty instance function for <typeparamref name="T"/>.
        /// </summary>
        public InstanceFunction() : base(typeof(T)) { }

        /// <summary>
        /// Initializes the instance function with a factory that accepts only the current <see cref="Mocker"/>.
        /// </summary>
        /// <param name="singleParamFunc">The factory delegate to invoke when no extra parameter value is supplied.</param>
        public InstanceFunction(Func<Mocker, T?> singleParamFunc) : base(typeof(T))
        {
            Function = singleParamFunc;
        }

        /// <summary>
        /// Initializes the instance function with a factory that accepts the current <see cref="Mocker"/> and one extra parameter value.
        /// </summary>
        /// <param name="doubleParamFunc">The factory delegate to invoke when one extra parameter value is supplied.</param>
        public InstanceFunction(Func<Mocker, object?, T?> doubleParamFunc) : base(typeof(T))
        {
            Function = doubleParamFunc;
        }

        /// <summary>
        /// Invokes the stored strongly typed factory delegate.
        /// </summary>
        /// <param name="mocker">The active mocker used as the first delegate argument.</param>
        /// <param name="parameters">Optional extra parameters passed to delegates that accept a second argument.</param>
        /// <returns>The created instance, or <see langword="null"/> when the stored delegate returns <see langword="null"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no compatible delegate has been assigned.</exception>
        public new T? Invoke(Mocker mocker, params object?[]? parameters)
        {
            if (Function is Func<Mocker, T?> singleParamFunc && (parameters == null || parameters.Length == 0))
            {
                return singleParamFunc(mocker);
            }

            if (Function is Func<Mocker, object?, T?> doubleParamFunc && parameters?.Length == 1)
            {
                return doubleParamFunc(mocker, parameters[0]);
            }

            throw new InvalidOperationException("No valid function provided or incorrect number of parameters.");
        }
    }
}

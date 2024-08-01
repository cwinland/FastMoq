namespace FastMoq.Models
{
    public class InstanceFunction
    {
        public Type InstanceType { get; }

        public InstanceFunction(Type type)
        {
            this.InstanceType = type;
        }

        // Property to hold the function
        public Delegate? Function { get; set; }

        // Method to set the function
        public void SetFunction(Delegate function)
        {
            this.Function = function;
        }

        public object? Invoke(Mocker mocker, params object?[]? parameters)
        {
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
            var method = Function.GetType().GetMethod("Invoke");
            if (method != null)
            {
                return method.Invoke(Function, new object[] { mocker });
            }

            throw new InvalidOperationException("Function is not a valid single parameter function.");
        }

        private object? InvokeDoubleParamFunction(Mocker mocker, object?[]? parameters)
        {
            var method = Function.GetType().GetMethod("Invoke");
            if (method != null)
            {
                return method.Invoke(Function, new object[] { mocker, parameters?[0] ?? null });
            }

            throw new InvalidOperationException("Function is not a valid double parameter function.");
        }


        // Overloaded CreateInstance for singleParamFunc
        public static InstanceFunction CreateInstance(Type type)
        {
            return new InstanceFunction(type);
        }
    }

    public class InstanceFunction<T> : InstanceFunction
    {
        // Constructor
        public InstanceFunction() : base(typeof(T)) { }

        public InstanceFunction(Func<Mocker, T?> singleParamFunc) : base(typeof(T))
        {
            Function = singleParamFunc;
        }

        public InstanceFunction(Func<Mocker, object?, T?> doubleParamFunc) : base(typeof(T))
        {
            Function = doubleParamFunc;
        }

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

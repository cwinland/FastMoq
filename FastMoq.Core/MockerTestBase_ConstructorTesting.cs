using System.Reflection;

namespace FastMoq
{
    /// <inheritdoc />
    public partial class MockerTestBase<TComponent>
    {
        /// <summary>
        ///     Gets the constructor.
        /// </summary>
        /// <returns>ConstructorInfo of the constructor.</returns>
        /// <exception cref="TypeAccessException">Error finding the constructor used to create the component.</exception>
        protected ConstructorInfo GetConstructor() => Mocks.ConstructorHistory.GetConstructor(typeof(TComponent)) ??
                                                      throw new TypeAccessException("Error finding the constructor used to create the component.");

        /// <summary>
        ///     Tests all constructor parameters.
        /// </summary>
        /// <param name="createAction">The action used for each parameter of each constructor.</param>
        /// <param name="defaultValue">The default value used when testing the parameter.</param>
        /// <param name="validValue">The valid value used when not testing the parameter.</param>
        /// <param name="bindingFlags">The binding flags.</param>
        protected void TestAllConstructorParameters(Action<Action, string, string> createAction, Func<ParameterInfo, object?>? defaultValue = null,
            Func<ParameterInfo, object?>? validValue = null, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        {
            var constructorList = typeof(TComponent).GetConstructors(bindingFlags);

            foreach (var constructorInfo in constructorList)
            {
                TestConstructorParameters(constructorInfo, createAction, defaultValue, validValue);
            }
        }

        /// <summary>
        /// Tests the active constructor parameters for the current component and lets the test assert what should happen when each parameter is replaced with an invalid value.
        /// </summary>
        /// <param name="createAction">The create action.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="validValue">The valid value.</param>
        /// <example>
        /// <para>Use <see cref="TestConstructorParameters(Action{Action, string, string}, Func{ParameterInfo, object?}?, Func{ParameterInfo, object?}?)"/> to verify guard clauses on the constructor that FastMoq actually selected for <typeparamref name="TComponent"/>.</para>
        /// <code language="csharp"><![CDATA[
        /// [Fact]
        /// public void CheckoutService_ShouldThrowForNullDependencies() => TestConstructorParameters(
        ///     (action, constructorName, parameterName) =>
        ///     {
        ///         action
        ///             .Should()
        ///             .Throw<ArgumentNullException>()
        ///             .WithMessage($"*{parameterName}*");
        ///     });
        /// ]]></code>
        /// <para>Provide explicit invalid and valid values when constructor arguments need type-specific test data instead of the default FastMoq resolution behavior.</para>
        /// <code language="csharp"><![CDATA[
        /// [Fact]
        /// public void ImportJob_ShouldValidateConstructorArguments() => TestConstructorParameters(
        ///     (action, constructorName, parameterName) =>
        ///     {
        ///         action
        ///             .Should()
        ///             .Throw<ArgumentException>()
        ///             .WithMessage($"*{parameterName}*");
        ///     },
        ///     info => info.Name switch
        ///     {
        ///         "tenant" => string.Empty,
        ///         "batchSize" => 0,
        ///         _ => default,
        ///     },
        ///     info => info.Name switch
        ///     {
        ///         "tenant" => "contoso",
        ///         "batchSize" => 50,
        ///         _ => Mocks.GetObject(info.ParameterType),
        ///     });
        /// ]]></code>
        /// </example>
        protected void TestConstructorParameters(Action<Action, string, string> createAction, Func<ParameterInfo, object?>? defaultValue = null, Func<ParameterInfo, object?>? validValue = null) =>
            TestConstructorParameters(GetConstructor(), createAction, defaultValue, validValue);

        /// <summary>
        ///     Tests the constructor parameters.
        /// </summary>
        /// <param name="constructorInfo">The constructor information.</param>
        /// <param name="createAction">The create action.</param>
        /// <param name="defaultValue">The value replaced when testing a parameter.</param>
        /// <param name="validValue">The valid value.</param>
        protected void TestConstructorParameters(ConstructorInfo constructorInfo, Action<Action, string, string> createAction,
            Func<ParameterInfo, object?>? defaultValue = null, Func<ParameterInfo, object?>? validValue = null)
        {
            var parameters = constructorInfo.GetParameters();
            var constructorName = GetMethodName(constructorInfo);
            defaultValue ??= _ => default;
            validValue ??= info => Mocks.GetObject(info.ParameterType);
            for (var paramIndex = 0; paramIndex < parameters.Length; paramIndex++)
            {
                var paramName = parameters[paramIndex].Name ?? string.Empty;
                createAction?.Invoke(() =>
                    {
                        try
                        {
                            constructorInfo.Invoke(parameters
                                .Select((t, i) => paramIndex == i ? defaultValue.Invoke(t) : validValue.Invoke(t)).ToArray()
                            );
                        }
                        catch (TargetInvocationException tie)
                        {
                            if (tie.InnerException != null)
                            {
                                throw tie.InnerException;
                            }

                            throw;
                        }
                    },
                    constructorName,
                    paramName
                );
            }
        }

        private static string GetMethodName(MethodBase constructorInfo)
        {
            var parameters = constructorInfo.GetParameters();
            var ctrParams = string.Join(", ", parameters.Select(GetParamNameInfo));
            return $"{constructorInfo.Name}({ctrParams})";

            static string GetParamNameInfo(ParameterInfo info) => $"{info.ParameterType.Name} {info.Name}";
        }

    }
}

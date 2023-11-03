using FastMoq.Models;
using System.Reflection;

namespace FastMoq
{
    /// <summary>
    ///     Class MockerTestBase.
    ///     Implements the <see cref="IDisposable" />
    /// </summary>
    /// <typeparam name="TComponent">The type of the t component.</typeparam>
    /// <inheritdoc />
    /// <seealso cref="IDisposable" />
    public partial class MockerTestBase<TComponent>
    {
        /// <summary>
        ///     Gets the constructor.
        /// </summary>
        /// <returns>ConstructorInfo of the constructor.</returns>
        /// <exception cref="TypeAccessException">Error finding the constructor used to create the component.</exception>
        protected ConstructorInfo GetConstructor() => Mocks.ConstructorHistory
                                                          .First(x => x.Key.Name == typeof(TComponent).Name)
                                                          .SelectMany(x => x).OfType<ConstructorModel>().Select(x => x.ConstructorInfo)
                                                          .LastOrDefault() ??
                                                      throw new TypeAccessException("Error finding the constructor used to create the component.");

        /// <summary>
        ///     Tests all constructor parameters.
        /// </summary>
        /// <param name="createAction">The create action.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="validValue">The valid value.</param>
        protected void TestAllConstructorParameters(Action<Action, string, string> createAction, Func<ParameterInfo, object?>? defaultValue = null, Func<ParameterInfo, object?>? validValue = null)
        {
            var constructorList = typeof(TComponent).GetConstructors();

            foreach (var constructorInfo in constructorList)
            {
                TestConstructorParameters(constructorInfo, createAction, defaultValue, validValue);
            }
        }

        /// <summary>
        ///     Tests the constructor parameters.
        /// </summary>
        /// <param name="createAction">The create action.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="validValue">The valid value.</param>
        /// <example>
        /// CreateComponent allows creating the component when desired, instead of in the base class constructor.
        /// <code><![CDATA[
        /// [Fact]
        /// public void Service_NullArgChecks() => TestConstructorParameters((action, constructorName, parameterName) =>
        /// {
        ///     output?.WriteLine($"Testing {constructorName}\n - {parameterName}");
        /// 
        ///     action
        ///         .Should()
        ///         .Throw<ArgumentNullException>()
        ///         .WithMessage($"*{parameterName}*");
        /// });
        /// 
        /// [Fact]
        /// public void Service_NullArgChecks() => TestConstructorParameters((action, constructorName, parameterName) =>
        ///     {
        ///         output?.WriteLine($"Testing {constructorName}\n - {parameterName}");
        /// 
        ///         action
        ///             .Should()
        ///             .Throw<ArgumentNullException>()
        ///             .WithMessage($"*{parameterName}*");
        ///     },
        ///     info =>
        ///     {
        ///         return info switch
        ///         {
        ///             { ParameterType: { Name: "string" }} => string.Empty,
        ///             { ParameterType: { Name: "int" }} => -1,
        ///             _ => default,
        ///         };
        ///     },
        ///     info =>
        ///     {
        ///         return info switch
        ///         {
        ///             { ParameterType: { Name: "string" }} => "Valid Value",
        ///             { ParameterType: { Name: "int" }} => 22,
        ///             _ => Mocks.GetObject(info.ParameterType),
        ///         };
        ///     }
        /// );
        /// ]]></code></example>
        protected void TestConstructorParameters(Action<Action, string, string> createAction, Func<ParameterInfo, object?>? defaultValue = null, Func<ParameterInfo, object?>? validValue = null) => TestConstructorParameters(GetConstructor(), createAction, defaultValue, validValue);

        /// <summary>
        ///     Tests the constructor parameters.
        /// </summary>
        /// <param name="constructorInfo">The constructor information.</param>
        /// <param name="createAction">The create action.</param>
        /// <param name="defaultValue">The value replaced when testing a parameter.</param>
        /// <param name="validValue">The valid value.</param>
        protected void TestConstructorParameters(ConstructorInfo constructorInfo, Action<Action, string, string> createAction, Func<ParameterInfo, object?>? defaultValue = null, Func<ParameterInfo, object?>? validValue = null)
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

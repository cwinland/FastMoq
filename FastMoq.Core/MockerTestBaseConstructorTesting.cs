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
        protected void TestAllConstructorParameters(Action<Action, string, string> createAction)
        {
            ConstructorInfo[] constructorList = typeof(TComponent).GetConstructors();

            foreach (var constructorInfo in constructorList)
            {
                TestConstructorParameters(constructorInfo, createAction);
            }
        }

        /// <summary>
        ///     Tests the constructor parameters.
        /// </summary>
        /// <param name="createAction">The create action.</param>
        protected void TestConstructorParameters(Action<Action, string, string> createAction) => TestConstructorParameters(GetConstructor(), createAction);

        /// <summary>
        ///     Tests the constructor parameters.
        /// </summary>
        /// <param name="constructorInfo">The constructor information.</param>
        /// <param name="createAction">The create action.</param>
        protected void TestConstructorParameters(ConstructorInfo constructorInfo, Action<Action, string, string> createAction)
        {
            ParameterInfo[] parameters = constructorInfo.GetParameters();

            for (var paramIndex = 0; paramIndex < parameters.Length; paramIndex++)
            {
                createAction?.Invoke(() =>
                    constructorInfo.Invoke(parameters
                        .Select((t, i) => paramIndex == i ? null : Mocks.GetObject(t.ParameterType)).ToArray()
                    ),
                    constructorInfo.ToString(),
                    parameters[paramIndex].Name
                );
            }
        }
    }
}

namespace FastMoq.Generators
{
    /// <summary>
    /// Marks a partial <c>MockerTestBase&lt;TComponent&gt;</c> test base for FastMoq's explicit compile-time harness generation path.
    /// </summary>
    /// <remarks>
    /// <para>Apply this attribute only to partial test-base types that derive from <c>MockerTestBase&lt;TComponent&gt;</c>.</para>
    /// <para>The first generator slice keeps opt-in explicit and emits constructor-signature metadata for the selected component path rather than enabling blanket automatic generation for every eligible type in a project.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class FastMoqGeneratedTestTargetAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FastMoqGeneratedTestTargetAttribute" /> class.
        /// </summary>
        /// <param name="componentType">The component under test that the generated harness path should target.</param>
        /// <param name="constructorParameterTypes">An optional explicit constructor signature to use for the generated harness bootstrap.</param>
        public FastMoqGeneratedTestTargetAttribute(Type componentType, params Type[] constructorParameterTypes)
        {
            ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
            ConstructorParameterTypes = constructorParameterTypes ?? throw new ArgumentNullException(nameof(constructorParameterTypes));
        }

        /// <summary>
        /// Gets the component under test that the generated harness path should target.
        /// </summary>
        public Type ComponentType { get; }

        /// <summary>
        /// Gets the optional explicit constructor signature that the generator should use.
        /// </summary>
        public IReadOnlyList<Type> ConstructorParameterTypes { get; }
    }
}
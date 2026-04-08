namespace FastMoq.Providers
{
    /// <summary>
    /// Declares the default FastMoq provider for the current assembly.
    /// </summary>
    /// <remarks>
    /// <para>Use this on test assemblies that intentionally standardize on a provider-specific FastMoq surface.</para>
    /// <para>This does not replace narrow per-test overrides such as <c>MockingProviderRegistry.Push("moq")</c>.</para>
    /// </remarks>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// using FastMoq.Providers;
    ///
    /// [assembly: FastMoqDefaultProvider("moq")]
    /// ]]></code>
    /// </example>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class FastMoqDefaultProviderAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FastMoqDefaultProviderAttribute" /> class.
        /// </summary>
        /// <param name="providerName">The provider name to treat as the assembly default.</param>
        public FastMoqDefaultProviderAttribute(string providerName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
            ProviderName = providerName;
        }

        /// <summary>
        /// Gets the declared provider name.
        /// </summary>
        public string ProviderName { get; }
    }
}
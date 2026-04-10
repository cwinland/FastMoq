namespace FastMoq.Providers
{
    /// <summary>
    /// Registers a FastMoq provider for the current assembly.
    /// </summary>
    /// <remarks>
    /// <para>Use this when the provider must be registered before selection, for example for custom providers, aliases, or explicit first-party package bootstrap.</para>
    /// <para>Set <see cref="SetAsDefault" /> to <see langword="true" /> to make the registered provider the assembly default.</para>
    /// </remarks>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// using FastMoq.Providers;
    /// using FastMoq.Providers.MoqProvider;
    ///
    /// [assembly: FastMoqRegisterProvider("moq", typeof(MoqMockingProvider), SetAsDefault = true)]
    /// ]]></code>
    /// </example>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class FastMoqRegisterProviderAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FastMoqRegisterProviderAttribute" /> class.
        /// </summary>
        /// <param name="providerName">The provider name to register.</param>
        /// <param name="providerType">The provider implementation type.</param>
        public FastMoqRegisterProviderAttribute(string providerName, Type providerType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
            ArgumentNullException.ThrowIfNull(providerType);

            ProviderName = providerName;
            ProviderType = providerType;
        }

        /// <summary>
        /// Gets the provider name to register.
        /// </summary>
        public string ProviderName { get; }

        /// <summary>
        /// Gets the provider implementation type.
        /// </summary>
        public Type ProviderType { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this registration should also become the assembly default.
        /// </summary>
        public bool SetAsDefault { get; set; }
    }
}
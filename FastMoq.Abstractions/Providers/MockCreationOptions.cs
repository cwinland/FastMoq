namespace FastMoq.Providers
{
    /// <summary>
    /// Options to influence mock creation in a provider agnostic manner.
    /// </summary>
    public sealed record MockCreationOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockCreationOptions"/> record.
        /// </summary>
        /// <param name="Strict">True to request strict mock behavior; otherwise false.</param>
        /// <param name="CallBase">True to allow base implementation calls when the provider supports it; otherwise false.</param>
        /// <param name="ConstructorArgs">Optional constructor arguments to use when creating the mock target.</param>
        /// <param name="AllowNonPublic">True to allow non-public constructors during mock creation; otherwise false.</param>
        public MockCreationOptions(
            bool Strict = false,
            bool CallBase = true,
            object?[]? ConstructorArgs = null,
            bool AllowNonPublic = false)
        {
            this.Strict = Strict;
            this.CallBase = CallBase;
            this.ConstructorArgs = ConstructorArgs;
            this.AllowNonPublic = AllowNonPublic;
        }

        /// <summary>
        /// Gets a value indicating whether strict mock behavior should be used.
        /// </summary>
        public bool Strict { get; init; }

        /// <summary>
        /// Gets a value indicating whether base implementation calls are allowed when supported.
        /// </summary>
        public bool CallBase { get; init; }

        /// <summary>
        /// Gets the constructor arguments to use when the provider creates the mock target.
        /// </summary>
        public object?[]? ConstructorArgs { get; init; }

        /// <summary>
        /// Gets a value indicating whether non-public constructors may be used during mock creation.
        /// </summary>
        public bool AllowNonPublic { get; init; }
    }
}
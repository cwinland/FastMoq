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
        /// <param name="strict">True to request strict mock behavior; otherwise false.</param>
        /// <param name="callBase">True to allow base implementation calls when the provider supports it; otherwise false.</param>
        /// <param name="constructorArgs">Optional constructor arguments to use when creating the mock target.</param>
        /// <param name="allowNonPublic">True to allow non-public constructors during mock creation; otherwise false.</param>
        public MockCreationOptions(
            bool strict = false,
            bool callBase = true,
            object?[]? constructorArgs = null,
            bool allowNonPublic = false)
        {
            Strict = strict;
            CallBase = callBase;
            ConstructorArgs = constructorArgs;
            AllowNonPublic = allowNonPublic;
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
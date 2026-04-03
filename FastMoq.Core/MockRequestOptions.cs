namespace FastMoq
{
    /// <summary>
    /// Options that control tracked mock retrieval and creation.
    /// </summary>
    public sealed class MockRequestOptions
    {
        /// <summary>
        /// Optional DI-style service key used for keyed registrations or keyed tracked mocks.
        /// </summary>
        public object? ServiceKey { get; set; }

        /// <summary>
        /// Indicates whether non-public constructors may be used when creating a concrete mock.
        /// When not set, FastMoq uses the current mock-request default for the active <see cref="Mocker"/>.
        /// </summary>
        public bool? AllowNonPublicConstructors { get; set; }

        /// <summary>
        /// Optional constructor arguments for concrete mock creation.
        /// </summary>
        public object?[] ConstructorArgs { get; set; } = [];
    }
}
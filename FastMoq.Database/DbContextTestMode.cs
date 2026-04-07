namespace FastMoq
{
    /// <summary>
    /// Selects how FastMoq should provision a DbContext test double.
    /// </summary>
    public enum DbContextTestMode
    {
        /// <summary>
        /// Use the existing DbContextMock and DbSet mock behavior.
        /// </summary>
        MockedSets = 0,

        /// <summary>
        /// Create a real DbContext backed by EF Core's in-memory provider.
        /// </summary>
        RealInMemory = 1,
    }
}
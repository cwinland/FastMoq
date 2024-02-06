namespace FastMoq.Models
{
    /// <summary>
    ///     Interface IDbSetMock
    /// </summary>
    public interface IDbSetMock
    {
        /// <summary>
        ///     Create Mock Setup for asynchronous methods to return data.
        /// </summary>
        void SetupAsyncListMethods();

        /// <summary>
        ///     Create Mock Setup for methods to return data.
        /// </summary>
        void SetupListMethods();

        /// <summary>
        ///     Create Mock Setup for synchronous and asynchronous methods to return data.
        /// </summary>
        void SetupMockMethods();
    }
}

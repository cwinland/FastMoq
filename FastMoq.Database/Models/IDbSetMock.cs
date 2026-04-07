namespace FastMoq.Models
{
    public interface IDbSetMock
    {
        void SetupAsyncListMethods();
        void SetupListMethods();
        void SetupMockMethods();
    }
}
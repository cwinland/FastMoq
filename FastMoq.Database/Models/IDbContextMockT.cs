using Microsoft.EntityFrameworkCore;

namespace FastMoq.Models
{
    public interface IDbContextMock<TEntity> : IDbContextMock where TEntity : DbContext
    {
        DbContextMock<TEntity> SetupDbSets(Mocker mocks);
    }
}
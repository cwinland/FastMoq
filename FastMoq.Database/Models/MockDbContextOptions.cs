using Microsoft.EntityFrameworkCore;
using Moq;

namespace FastMoq.Models
{
    public class MockDbContextOptions<T> : Mock<DbContextOptions<T>> where T : DbContext
    {
    }
}
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class MockDbContextOptions.
    ///     Implements the <see cref="Mock{DbContextOptions}" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <inheritdoc />
    public class MockDbContextOptions<T> : Mock<DbContextOptions<T>> where T : DbContext
    {
        // Left Blank.
    }
}

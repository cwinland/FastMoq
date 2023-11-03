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
    /// <seealso cref="Mock{DbContextOptions{T}}" />
    public class MockDbContextOptions<T> : Mock<DbContextOptions<T>> where T : DbContext { }
}

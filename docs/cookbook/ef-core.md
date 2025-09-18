# Entity Framework Core Testing with FastMoq

## 📋 Scenario

Testing data access layers that use Entity Framework Core, including repositories, unit of work patterns, and complex queries. This recipe covers both in-memory database testing and DbContext mocking approaches.

## 🎯 Goals

- Test repository implementations with automatic DbContext injection
- Use SQLite in-memory databases for integration-style tests
- Mock DbContext for isolated unit tests
- Test complex queries, transactions, and bulk operations
- Verify change tracking and concurrency scenarios

## 🔧 Implementation

### Entity and DbContext Setup

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<Product> Products { get; set; } = new();
}

public class ShopContext : DbContext
{
    public ShopContext(DbContextOptions<ShopContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });
    }
}
```

### Repository Implementation

```csharp
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
    Task<IEnumerable<Product>> SearchAsync(string searchTerm);
    Task<Product> CreateAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int id);
    Task<int> GetCountAsync();
    Task<bool> ExistsAsync(int id);
}

public class ProductRepository : IProductRepository
{
    private readonly ShopContext _context;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(ShopContext context, ILogger<ProductRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving product {ProductId}", id);
        
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
    {
        _logger.LogInformation("Retrieving products for category {CategoryId}", categoryId);
        
        return await _context.Products
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<Product>();

        _logger.LogInformation("Searching products with term: {SearchTerm}", searchTerm);

        return await _context.Products
            .Where(p => p.Name.Contains(searchTerm) && p.IsActive)
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product> CreateAsync(Product product)
    {
        _logger.LogInformation("Creating new product: {ProductName}", product.Name);
        
        product.CreatedAt = DateTime.UtcNow;
        _context.Products.Add(product);
        
        await _context.SaveChangesAsync();
        
        // Reload with includes
        return await GetByIdAsync(product.Id) ?? product;
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        _logger.LogInformation("Updating product {ProductId}", product.Id);
        
        _context.Entry(product).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        
        return product;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("Deleting product {ProductId}", id);
        
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found for deletion", id);
            return false;
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Products.CountAsync(p => p.IsActive);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Products.AnyAsync(p => p.Id == id);
    }
}
```

## 🔧 FastMoq Testing - In-Memory Database Approach

FastMoq automatically configures SQLite in-memory databases for DbContext testing:

```csharp
using FastMoq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

public class ProductRepositoryTests : MockerTestBase<ProductRepository>
{
    // FastMoq automatically creates an in-memory SQLite database
    // and injects it into the ShopContext constructor

    public class GetByIdAsync : ProductRepositoryTests
    {
        [Fact]
        public async Task ShouldReturnProduct_WhenProductExists()
        {
            // Arrange
            var category = new Category { Id = 1, Name = "Electronics" };
            var product = new Product 
            { 
                Id = 1, 
                Name = "Laptop", 
                Price = 999.99m, 
                CategoryId = 1,
                Category = category,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Add test data to the in-memory database
            var context = Mocks.GetObject<ShopContext>();
            context.Categories.Add(category);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            // Act
            var result = await Component.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("Laptop");
            result.Category.Should().NotBeNull();
            result.Category.Name.Should().Be("Electronics");

            // Verify logging
            Mocks.VerifyLogger<ProductRepository>(
                LogLevel.Information,
                "Retrieving product 1");
        }

        [Fact]
        public async Task ShouldReturnNull_WhenProductDoesNotExist()
        {
            // Act
            var result = await Component.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }
    }

    public class GetByCategoryAsync : ProductRepositoryTests
    {
        [Fact]
        public async Task ShouldReturnActiveProductsOnly()
        {
            // Arrange
            var context = Mocks.GetObject<ShopContext>();
            var category = new Category { Id = 1, Name = "Books" };
            
            var products = new[]
            {
                new Product { Id = 1, Name = "Active Book", CategoryId = 1, IsActive = true },
                new Product { Id = 2, Name = "Inactive Book", CategoryId = 1, IsActive = false },
                new Product { Id = 3, Name = "Another Active Book", CategoryId = 1, IsActive = true }
            };

            context.Categories.Add(category);
            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // Act
            var result = await Component.GetByCategoryAsync(1);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(p => p.IsActive);
            result.Should().BeInAscendingOrder(p => p.Name);
        }
    }

    public class CreateAsync : ProductRepositoryTests
    {
        [Fact]
        public async Task ShouldCreateProduct_WithCorrectTimestamp()
        {
            // Arrange
            var context = Mocks.GetObject<ShopContext>();
            var category = new Category { Id = 1, Name = "Electronics" };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var product = new Product
            {
                Name = "New Product",
                Price = 199.99m,
                CategoryId = 1,
                IsActive = true
            };

            var beforeCreation = DateTime.UtcNow;

            // Act
            var result = await Component.CreateAsync(product);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.Name.Should().Be("New Product");
            result.CreatedAt.Should().BeOnOrAfter(beforeCreation);
            result.Category.Should().NotBeNull();

            // Verify it's actually in the database
            var fromDb = await context.Products.FindAsync(result.Id);
            fromDb.Should().NotBeNull();
            fromDb!.Name.Should().Be("New Product");
        }
    }

    public class SearchAsync : ProductRepositoryTests
    {
        [Fact]
        public async Task ShouldReturnMatchingProducts_CaseInsensitive()
        {
            // Arrange
            var context = Mocks.GetObject<ShopContext>();
            var category = new Category { Id = 1, Name = "Books" };
            
            var products = new[]
            {
                new Product { Id = 1, Name = "C# Programming", CategoryId = 1, IsActive = true },
                new Product { Id = 2, Name = "Java Development", CategoryId = 1, IsActive = true },
                new Product { Id = 3, Name = "Python Cookbook", CategoryId = 1, IsActive = true },
                new Product { Id = 4, Name = "Inactive C# Book", CategoryId = 1, IsActive = false }
            };

            context.Categories.Add(category);
            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // Act
            var result = await Component.SearchAsync("C#");

            // Assert
            result.Should().HaveCount(1);
            result.First().Name.Should().Be("C# Programming");
        }

        [Fact]
        public async Task ShouldReturnEmpty_WhenSearchTermIsEmpty()
        {
            // Act
            var result = await Component.SearchAsync("");

            // Assert
            result.Should().BeEmpty();
        }
    }

    public class DeleteAsync : ProductRepositoryTests
    {
        [Fact]
        public async Task ShouldDeleteProduct_WhenProductExists()
        {
            // Arrange
            var context = Mocks.GetObject<ShopContext>();
            var product = new Product 
            { 
                Id = 1, 
                Name = "To Delete", 
                Price = 100m, 
                CategoryId = 1 
            };
            
            context.Products.Add(product);
            await context.SaveChangesAsync();

            // Act
            var result = await Component.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();

            // Verify it's gone from database
            var fromDb = await context.Products.FindAsync(1);
            fromDb.Should().BeNull();

            // Verify logging
            Mocks.VerifyLogger<ProductRepository>(
                LogLevel.Information,
                "Deleting product 1");
        }

        [Fact]
        public async Task ShouldReturnFalse_WhenProductDoesNotExist()
        {
            // Act
            var result = await Component.DeleteAsync(999);

            // Assert
            result.Should().BeFalse();

            // Verify warning logged
            Mocks.VerifyLogger<ProductRepository>(
                LogLevel.Warning,
                "Product 999 not found for deletion");
        }
    }
}
```

## 🔧 FastMoq Testing - Mocked DbContext Approach

For pure unit tests without database operations:

```csharp
public class ProductRepositoryMockTests : MockerTestBase<ProductRepository>
{
    public ProductRepositoryMockTests() : base(ConfigureMockedDbContext) { }

    private static void ConfigureMockedDbContext(Mocker mocker)
    {
        // Create mock DbContext with mocked DbSets
        var mockContext = mocker.GetMock<ShopContext>();
        
        // Note: Mocking DbContext is complex due to EF Core's internal structure
        // This approach is best for testing business logic, not EF operations
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLogCorrectly_WhenCalled()
    {
        // Arrange
        var productId = 123;
        
        // Mock the context to throw an exception to test error handling
        Mocks.GetMock<ShopContext>()
            .Setup(x => x.Products)
            .Throws(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var act = async () => await Component.GetByIdAsync(productId);
        
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Verify logging occurred
        Mocks.VerifyLogger<ProductRepository>(
            LogLevel.Information,
            $"Retrieving product {productId}");
    }
}
```

## 🚀 Advanced Patterns

### Testing with Transactions

```csharp
public class TransactionalService
{
    private readonly ShopContext _context;
    private readonly ILogger<TransactionalService> _logger;

    public TransactionalService(ShopContext context, ILogger<TransactionalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> TransferProductsAsync(int fromCategoryId, int toCategoryId, params int[] productIds)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            _logger.LogInformation("Starting product transfer from category {FromId} to {ToId}", 
                fromCategoryId, toCategoryId);

            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id) && p.CategoryId == fromCategoryId)
                .ToListAsync();

            if (products.Count != productIds.Length)
            {
                _logger.LogWarning("Not all products found for transfer");
                return false;
            }

            // Verify target category exists
            var targetCategory = await _context.Categories.FindAsync(toCategoryId);
            if (targetCategory == null)
            {
                _logger.LogError("Target category {CategoryId} not found", toCategoryId);
                return false;
            }

            // Update products
            foreach (var product in products)
            {
                product.CategoryId = toCategoryId;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully transferred {Count} products", products.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during product transfer");
            await transaction.RollbackAsync();
            return false;
        }
    }
}

public class TransactionalServiceTests : MockerTestBase<TransactionalService>
{
    [Fact]
    public async Task TransferProductsAsync_ShouldSucceed_WhenAllProductsExist()
    {
        // Arrange
        var context = Mocks.GetObject<ShopContext>();
        
        var sourceCategory = new Category { Id = 1, Name = "Books" };
        var targetCategory = new Category { Id = 2, Name = "Electronics" };
        
        var products = new[]
        {
            new Product { Id = 1, Name = "Product 1", CategoryId = 1 },
            new Product { Id = 2, Name = "Product 2", CategoryId = 1 }
        };

        context.Categories.AddRange(sourceCategory, targetCategory);
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Act
        var result = await Component.TransferProductsAsync(1, 2, 1, 2);

        // Assert
        result.Should().BeTrue();

        // Verify products were moved
        var updatedProducts = await context.Products.Where(p => p.Id == 1 || p.Id == 2).ToListAsync();
        updatedProducts.Should().AllSatisfy(p => p.CategoryId.Should().Be(2));

        // Verify logging
        Mocks.VerifyLogger<TransactionalService>(
            LogLevel.Information,
            "Successfully transferred 2 products");
    }

    [Fact]
    public async Task TransferProductsAsync_ShouldFail_WhenTargetCategoryNotFound()
    {
        // Arrange
        var context = Mocks.GetObject<ShopContext>();
        
        var sourceCategory = new Category { Id = 1, Name = "Books" };
        var product = new Product { Id = 1, Name = "Product 1", CategoryId = 1 };

        context.Categories.Add(sourceCategory);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Act
        var result = await Component.TransferProductsAsync(1, 999, 1);

        // Assert
        result.Should().BeFalse();

        // Verify product wasn't moved
        var unchangedProduct = await context.Products.FindAsync(1);
        unchangedProduct!.CategoryId.Should().Be(1);

        // Verify error logging
        Mocks.VerifyLogger<TransactionalService>(
            LogLevel.Error,
            "Target category 999 not found");
    }
}
```

### Bulk Operations Testing

```csharp
public class BulkProductService
{
    private readonly ShopContext _context;
    private readonly ILogger<BulkProductService> _logger;

    public BulkProductService(ShopContext context, ILogger<BulkProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> BulkUpdatePricesAsync(int categoryId, decimal multiplier)
    {
        _logger.LogInformation("Bulk updating prices for category {CategoryId} with multiplier {Multiplier}", 
            categoryId, multiplier);

        var updatedCount = await _context.Products
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .ExecuteUpdateAsync(p => p.SetProperty(x => x.Price, x => x.Price * multiplier));

        _logger.LogInformation("Updated {Count} product prices", updatedCount);
        return updatedCount;
    }

    public async Task<int> BulkDeactivateAsync(params int[] productIds)
    {
        _logger.LogInformation("Bulk deactivating {Count} products", productIds.Length);

        var updatedCount = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ExecuteUpdateAsync(p => p.SetProperty(x => x.IsActive, false));

        _logger.LogInformation("Deactivated {Count} products", updatedCount);
        return updatedCount;
    }
}

public class BulkProductServiceTests : MockerTestBase<BulkProductService>
{
    [Fact]
    public async Task BulkUpdatePricesAsync_ShouldUpdateAllActiveProducts()
    {
        // Arrange
        var context = Mocks.GetObject<ShopContext>();
        
        var category = new Category { Id = 1, Name = "Electronics" };
        var products = new[]
        {
            new Product { Id = 1, Name = "Product 1", CategoryId = 1, Price = 100m, IsActive = true },
            new Product { Id = 2, Name = "Product 2", CategoryId = 1, Price = 200m, IsActive = true },
            new Product { Id = 3, Name = "Product 3", CategoryId = 1, Price = 300m, IsActive = false }, // Should not update
            new Product { Id = 4, Name = "Product 4", CategoryId = 2, Price = 400m, IsActive = true } // Different category
        };

        context.Categories.Add(category);
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Act
        var result = await Component.BulkUpdatePricesAsync(1, 1.1m);

        // Assert
        result.Should().Be(2);

        // Verify prices were updated
        var updatedProducts = await context.Products.Where(p => p.CategoryId == 1 && p.IsActive).ToListAsync();
        updatedProducts.Should().HaveCount(2);
        updatedProducts.Should().Contain(p => p.Price == 110m); // 100 * 1.1
        updatedProducts.Should().Contain(p => p.Price == 220m); // 200 * 1.1

        // Verify inactive and different category products unchanged
        var unchangedProduct = await context.Products.FindAsync(3);
        unchangedProduct!.Price.Should().Be(300m);
    }
}
```

## ✅ Verification Patterns

### Database State Verification
```csharp
// Verify entity exists in database
var entity = await context.Entities.FindAsync(id);
entity.Should().NotBeNull();

// Verify entity was removed
var deletedEntity = await context.Entities.FindAsync(id);
deletedEntity.Should().BeNull();

// Verify specific properties
entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
entity.Status.Should().Be(EntityStatus.Active);

// Verify relationships loaded
entity.RelatedEntities.Should().NotBeNull();
entity.RelatedEntities.Should().HaveCountGreaterThan(0);
```

### Query Result Verification
```csharp
// Verify collection results
result.Should().HaveCount(expectedCount);
result.Should().OnlyContain(item => item.IsActive);
result.Should().BeInAscendingOrder(item => item.Name);

// Verify complex queries
result.Should().OnlyContain(p => 
    p.Price >= minPrice && 
    p.Price <= maxPrice &&
    p.Category.Name == expectedCategory);
```

## 💡 Tips & Tricks

### Test Data Builders
```csharp
public class ProductBuilder
{
    private Product _product = new();

    public static ProductBuilder Create() => new();

    public ProductBuilder WithId(int id)
    {
        _product.Id = id;
        return this;
    }

    public ProductBuilder WithName(string name)
    {
        _product.Name = name;
        return this;
    }

    public ProductBuilder WithPrice(decimal price)
    {
        _product.Price = price;
        return this;
    }

    public ProductBuilder InCategory(int categoryId)
    {
        _product.CategoryId = categoryId;
        return this;
    }

    public ProductBuilder Active(bool isActive = true)
    {
        _product.IsActive = isActive;
        return this;
    }

    public Product Build() => _product;

    public static implicit operator Product(ProductBuilder builder) => builder.Build();
}

// Usage in tests
var product = ProductBuilder.Create()
    .WithName("Test Product")
    .WithPrice(99.99m)
    .InCategory(1)
    .Active();
```

### Database Seeding for Complex Tests
```csharp
public abstract class DatabaseTestBase<T> : MockerTestBase<T> where T : class
{
    protected async Task SeedDataAsync(params object[] entities)
    {
        var context = Mocks.GetObject<ShopContext>();
        context.AddRange(entities);
        await context.SaveChangesAsync();
        
        // Clear change tracker to simulate fresh context
        context.ChangeTracker.Clear();
    }

    protected async Task<TEntity?> FindAsync<TEntity>(object id) where TEntity : class
    {
        var context = Mocks.GetObject<ShopContext>();
        return await context.FindAsync<TEntity>(id);
    }
}
```

---

**Next Recipe:** [Background Services Testing](background-services.md)
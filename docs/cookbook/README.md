# FastMoq Cookbook

This cookbook contains practical recipes for common testing scenarios using FastMoq. Each recipe includes complete, runnable examples that you can adapt to your specific needs.

## Table of Contents

1. [API Controller Testing](#api-controller-testing)
2. [Entity Framework Core Testing](#entity-framework-core-testing)
3. [Background Services Testing](#background-services-testing)
4. [HttpClient and External API Testing](#httpclient-and-external-api-testing)
5. [Configuration and Options Testing](#configuration-and-options-testing)
6. [Logging Verification](#logging-verification)
7. [Azure Services Testing](#azure-services-testing)
8. [Fluent Validation Testing](#fluent-validation-testing)
9. [Event-Driven Architecture Testing](#event-driven-architecture-testing)
10. [File System Operations](#file-system-operations)

---

## API Controller Testing

Testing ASP.NET Core controllers with dependency injection and various scenarios.

### Basic Controller Test

**Controller Example**

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        try
        {
            var user = await _userService.GetUserAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(500);
        }
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userService.CreateUserAsync(request);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
}
```

**Test Example**

```csharp
using FastMoq;
using FastMoq.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class UsersControllerTests : MockerTestBase<UsersController>
{
    [Fact]
    public async Task GetUser_ShouldReturnOkResult_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var expectedUser = new UserDto { Id = userId, Name = "John Doe" };
        
        Mocks.GetMock<IUserService>()
            .Setup(x => x.GetUserAsync(userId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await Component.GetUser(userId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedUser);
    }

    [Fact]
    public async Task GetUser_ShouldReturnNotFound_WhenUserNotFound()
    {
        // Arrange
        Mocks.GetMock<IUserService>()
            .Setup(x => x.GetUserAsync(It.IsAny<int>()))
            .ReturnsAsync((UserDto)null);

        // Act
        var result = await Component.GetUser(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUser_ShouldReturnInternalServerError_WhenServiceThrows()
    {
        // Arrange
        Mocks.GetMock<IUserService>()
            .Setup(x => x.GetUserAsync(It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await Component.GetUser(1);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        
        // Verify logging
        Mocks.GetMock<ILogger<UsersController>>()
            .VerifyLogger(LogLevel.Error, Times.Once());
    }

    [Fact]
    public async Task CreateUser_ShouldReturnCreatedResult_WithValidModel()
    {
        // Arrange
        var request = new CreateUserRequest { Name = "Jane Doe", Email = "jane@example.com" };
        var createdUser = new UserDto { Id = 2, Name = request.Name, Email = request.Email };
        
        Mocks.GetMock<IUserService>()
            .Setup(x => x.CreateUserAsync(request))
            .ReturnsAsync(createdUser);

        // Act
        var result = await Component.CreateUser(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().Be(createdUser);
        createdResult.ActionName.Should().Be(nameof(UsersController.GetUser));
    }
}
```

### Testing Controller with Authorization

FastMoq provides built-in **Identity Helper Extensions** that simplify controller authorization testing. These helpers eliminate the boilerplate code for setting up authenticated users, claims, and HTTP contexts.

#### Using FastMoq Identity Helpers (Recommended)

FastMoq includes `IdentityHelperExtensions.cs` with convenient methods for authorization testing:

```csharp
using FastMoq;
using FastMoq.Extensions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class UsersControllerAuthTests : MockerTestBase<UsersController>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        // Use FastMoq's identity helpers to create claims and principal
        var userIdClaim = IdentityHelperExtensions.CreateClaim(ClaimTypes.NameIdentifier, "user123");
        var roleClaim = IdentityHelperExtensions.CreateClaim(ClaimTypes.Role, "User");
        
        var principal = IdentityHelperExtensions.CreatePrincipal(new[] { userIdClaim, roleClaim }, "TestAuth");
        
        // Set up HTTP context with the principal using FastMoq helper
        var httpContext = Mocks.CreateHttpContext();
        httpContext.SetUser(principal); // Extension method from FastMoq
        
        mocker.CreateInstance<UsersController>().ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    };

    [Fact]
    public void GetCurrentUser_ShouldReturnUserFromClaims_WithBuiltInHelpers()
    {
        // Act
        var userId = Component.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isInRole = Component.HttpContext.User.IsInRole("User");

        // Assert
        userId.Should().Be("user123");
        isInRole.Should().BeTrue();
    }

    [Fact]
    public void AdminAction_ShouldAllowAccess_WithMultipleRoles()
    {
        // Arrange - Create multiple role claims using FastMoq helpers
        var claims = new[]
        {
            IdentityHelperExtensions.CreateClaim(ClaimTypes.NameIdentifier, "admin123"),
            IdentityHelperExtensions.CreateClaim(ClaimTypes.Role, "Admin"),
            IdentityHelperExtensions.CreateClaim(ClaimTypes.Role, "User"),
            IdentityHelperExtensions.CreateClaim(ClaimTypes.Role, "Manager")
        };
        
        var principal = IdentityHelperExtensions.CreatePrincipal(claims, "TestAuth");
        Component.HttpContext.SetUser(principal);

        // Act & Assert
        Component.HttpContext.User.IsInRole("Admin").Should().BeTrue();
        Component.HttpContext.User.IsInRole("Manager").Should().BeTrue();
        Component.HttpContext.User.HasClaim(ClaimTypes.NameIdentifier, "admin123").Should().BeTrue();
    }

    [Fact]
    public void GetCurrentUser_ShouldReturnAnonymous_WhenNotAuthenticated()
    {
        // Arrange - Create unauthenticated principal
        var principal = IdentityHelperExtensions.CreatePrincipal(Array.Empty<Claim>());
        Component.HttpContext.SetUser(principal);

        // Act & Assert
        Component.HttpContext.User.Identity.IsAuthenticated.Should().BeFalse();
    }
}
```

#### Advanced Identity Helper Usage

FastMoq's identity helpers support custom claims and complex authorization scenarios:

```csharp
public class UsersControllerAdvancedAuthTests : MockerTestBase<UsersController>
{
    [Fact]
    public void GetUserProfile_ShouldIncludeCustomClaims()
    {
        // Arrange - Setup user with custom claims using FastMoq helpers
        var claims = new[]
        {
            IdentityHelperExtensions.CreateClaim(ClaimTypes.NameIdentifier, "user123"),
            IdentityHelperExtensions.CreateClaim("department", "Engineering", allowCustomType: true),
            IdentityHelperExtensions.CreateClaim("employee_id", "E12345", allowCustomType: true),
            IdentityHelperExtensions.CreateClaim("security_clearance", "Level2", allowCustomType: true),
            IdentityHelperExtensions.CreateClaim(ClaimTypes.Role, "Employee"),
            IdentityHelperExtensions.CreateClaim(ClaimTypes.Role, "Developer")
        };
        
        var principal = IdentityHelperExtensions.CreatePrincipal(claims, "CustomAuth");
        Component.HttpContext.SetUser(principal);

        // Act
        var department = Component.HttpContext.User.FindFirst("department")?.Value;
        var employeeId = Component.HttpContext.User.FindFirst("employee_id")?.Value;

        // Assert
        department.Should().Be("Engineering");
        employeeId.Should().Be("E12345");
        Component.HttpContext.User.IsInRole("Developer").Should().BeTrue();
    }

    [Fact]
    public void AuthorizeAction_ShouldValidateJwtClaims()
    {
        // Arrange - Setup JWT-style claims using FastMoq helpers
        var jwtClaims = new[]
        {
            IdentityHelperExtensions.CreateClaim("sub", "user123", allowCustomType: true),
            IdentityHelperExtensions.CreateClaim("iss", "https://auth.mycompany.com", allowCustomType: true),
            IdentityHelperExtensions.CreateClaim("aud", "myapp-api", allowCustomType: true),
            IdentityHelperExtensions.CreateClaim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(), allowCustomType: true),
            IdentityHelperExtensions.CreateClaim(ClaimTypes.Role, "ApiUser")
        };

        var principal = IdentityHelperExtensions.CreatePrincipal(jwtClaims, "JwtAuth");
        Component.HttpContext.SetUser(principal);

        // Act & Assert
        Component.HttpContext.User.FindFirst("sub")?.Value.Should().Be("user123");
        Component.HttpContext.User.FindFirst("iss")?.Value.Should().Be("https://auth.mycompany.com");
        Component.HttpContext.User.IsInRole("ApiUser").Should().BeTrue();
    }
}
```

#### Setup Actions with Identity Helpers

You can also use identity helpers in setup actions for consistent test configuration:

```csharp
public class UsersControllerAuthTests : MockerTestBase<UsersController>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        // Use FastMoq identity helpers in setup
        var defaultClaims = new[]
        {
            IdentityHelperExtensions.CreateClaim(ClaimTypes.NameIdentifier, "default-user"),
            IdentityHelperExtensions.CreateClaim(ClaimTypes.Role, "User")
        };
        
        var principal = IdentityHelperExtensions.CreatePrincipal(defaultClaims, "DefaultAuth");
        
        // Set up controller with authenticated user using helper
        var controller = mocker.CreateInstance<UsersController>();
        var httpContext = Mocks.CreateHttpContext();
        httpContext.SetUser(principal);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        // Additional mock setup...
        mocker.GetMock<IUserService>()
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(new UserDto { Id = 1, Name = "Default User" });
    };

    [Fact]
    public void GetCurrentUser_ShouldUseDefaultSetup()
    {
        // Test automatically uses the user setup from SetupMocksAction
        var user = Component.HttpContext.User;
        user.Identity.IsAuthenticated.Should().BeTrue();
        user.IsInRole("User").Should().BeTrue();
    }
    
    [Fact] 
    public void AdminAction_ShouldOverrideDefaultUser()
    {
        // Arrange - Override the default user setup for this specific test
        var adminClaims = new[]
        {
            IdentityHelperExtensions.CreateClaim(ClaimTypes.NameIdentifier, "admin456"),
            IdentityHelperExtensions.CreateClaim(ClaimTypes.Role, "Admin")
        };
        
        var adminPrincipal = IdentityHelperExtensions.CreatePrincipal(adminClaims, "AdminAuth");
        Component.HttpContext.SetUser(adminPrincipal);

        // Act & Assert
        Component.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("admin456");
        Component.HttpContext.User.IsInRole("Admin").Should().BeTrue();
        Component.HttpContext.User.IsInRole("User").Should().BeFalse(); // Previous setup is replaced
    }
}
```

#### Manual Setup vs FastMoq Helpers (Comparison)

For comparison, here's how you would set up authorization manually vs using FastMoq helpers:

```csharp
// ❌ Manual approach (more verbose, error-prone)
public class UsersControllerManualAuthTests : MockerTestBase<UsersController>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        // Manual approach - creating everything from scratch
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Role, "User")
        }, "mock"));

        mocker.CreateInstance<UsersController>().ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    };
}

// ✅ FastMoq helper approach (cleaner, safer)
public class UsersControllerHelperAuthTests : MockerTestBase<UsersController>
{
    [Fact]
    public void TestAction_WithFastMoqHelpers()
    {
        // Arrange - Using FastMoq identity helpers
        var claims = new[]
        {
            IdentityHelperExtensions.CreateClaim(ClaimTypes.NameIdentifier, "user123"),
            IdentityHelperExtensions.CreateClaim(ClaimTypes.Role, "User")
        };
        
        var principal = IdentityHelperExtensions.CreatePrincipal(claims, "TestAuth");
        Component.HttpContext.SetUser(principal);
        
        // Test implementation is cleaner and less error-prone...
        Component.HttpContext.User.IsInRole("User").Should().BeTrue();
    }
}
```

#### Available Identity Helper Methods

FastMoq provides these built-in identity helper methods from `IdentityHelperExtensions.cs`:

| Method | Description | Example Usage |
|--------|-------------|---------------|
| `CreateClaim(type, value, properties, allowCustomType)` | Creates a validated claim | `IdentityHelperExtensions.CreateClaim(ClaimTypes.NameIdentifier, "user123")` |
| `CreatePrincipal(claims, authenticationType)` | Creates a ClaimsPrincipal from claims | `IdentityHelperExtensions.CreatePrincipal(claims, "TestAuth")` |
| `SetUser(context, principal)` | Sets user on HttpContext | `context.SetUser(principal)` |
| `SetUser(context, identity)` | Sets user from ClaimsIdentity | `context.SetUser(identity)` |
| `IsValidClaimType(type)` | Validates if claim type exists in ClaimTypes | `IdentityHelperExtensions.IsValidClaimType(ClaimTypes.Role)` |

#### Key Benefits of FastMoq Identity Helpers

- **Type Safety**: `CreateClaim` validates claim types against `ClaimTypes` constants
- **Custom Claims**: Use `allowCustomType: true` for JWT or custom claim types
- **Cleaner Code**: Eliminates manual `new Claim()` and `new ClaimsPrincipal()` construction
- **Error Prevention**: Built-in validation prevents common claim setup mistakes
- **Consistent API**: All helpers follow FastMoq's extension method patterns

**Note**: These identity helpers are part of FastMoq's `IdentityHelperExtensions.cs` and provide type-safe, validated claim creation while significantly reducing boilerplate code for authorization testing.

---

## Entity Framework Core Testing

FastMoq provides excellent support for testing with Entity Framework Core using in-memory databases.

### DbContext Setup

```csharp
public class BlogContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }

    public BlogContext(DbContextOptions<BlogContext> options) : base(options) { }

    // Internal constructor for testing
    internal BlogContext() { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>()
            .HasMany(b => b.Posts)
            .WithOne(p => p.Blog)
            .HasForeignKey(p => p.BlogId);
    }
}

public class BlogService
{
    private readonly BlogContext _context;
    private readonly ILogger<BlogService> _logger;

    public BlogService(BlogContext context, ILogger<BlogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Blog> CreateBlogAsync(string title, string description)
    {
        var blog = new Blog
        {
            Title = title,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Blogs.Add(blog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created blog {BlogId} with title {Title}", blog.Id, blog.Title);
        return blog;
    }

    public async Task<List<Blog>> GetBlogsWithPostsAsync()
    {
        return await _context.Blogs
            .Include(b => b.Posts)
            .Where(b => b.Posts.Any())
            .ToListAsync();
    }
}
```

### Testing with FastMoq DbContext

```csharp
public class BlogServiceTests : MockerTestBase<BlogService>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        var dbContextMock = mocker.GetMockDbContext<BlogContext>();
        mocker.AddType(_ => dbContextMock.Object);
    };

    [Fact]
    public async Task CreateBlog_ShouldAddBlogToDatabase()
    {
        // Arrange
        var title = "Test Blog";
        var description = "Test Description";

        // Act
        var result = await Component.CreateBlogAsync(title, description);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(title);
        result.Description.Should().Be(description);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        // Verify database interaction
        var dbContext = Mocks.GetRequiredObject<BlogContext>();
        dbContext.Blogs.Should().Contain(result);
        
        // Verify logging
        Mocks.GetMock<ILogger<BlogService>>()
            .VerifyLogger(LogLevel.Information, 
            "Created blog {BlogId} with title {Title}", Times.Once());
    }

    [Fact]
    public async Task GetBlogsWithPosts_ShouldReturnOnlyBlogsWithPosts()
    {
        // Arrange
        var dbContext = Mocks.GetRequiredObject<BlogContext>();
        
        var blogWithPosts = new Blog { Id = 1, Title = "Blog with Posts" };
        var blogWithoutPosts = new Blog { Id = 2, Title = "Empty Blog" };
        var post = new Post { Id = 1, Title = "Test Post", BlogId = 1, Blog = blogWithPosts };
        
        blogWithPosts.Posts = new List<Post> { post };
        
        dbContext.Blogs.AddRange(blogWithPosts, blogWithoutPosts);
        dbContext.Posts.Add(post);
        dbContext.SaveChanges();

        // Act
        var result = await Component.GetBlogsWithPostsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(blogWithPosts);
        result[0].Posts.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateBlog_ShouldThrowException_WhenDatabaseFails()
    {
        // Arrange
        var dbContext = Mocks.GetRequiredObject<BlogContext>();
        dbContext.Dispose(); // Simulate database error

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            Component.CreateBlogAsync("Test", "Description"));
    }
}
```

### Advanced DbContext Scenarios

```csharp
public class BlogServiceAdvancedTests : MockerTestBase<BlogService>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        // Custom DbContext with specific behavior
        var dbContextMock = mocker.GetMockDbContext<BlogContext>();
        
        // Setup specific DbSet behavior if needed
        var blogSet = dbContextMock.Object.Set<Blog>();
        // Additional setup...
        
        mocker.AddType(_ => dbContextMock.Object);
    };

    [Fact]
    public async Task BulkOperation_ShouldHandleLargeDataSets()
    {
        // Arrange
        var dbContext = Mocks.GetRequiredObject<BlogContext>();
        var blogs = GenerateTestBlogs(1000); // Generate test data
        
        dbContext.Blogs.AddRange(blogs);
        dbContext.SaveChanges();

        // Act & Assert
        var result = await Component.GetBlogsWithPostsAsync();
        // Verify bulk operation behavior
    }
}
```

---

## Background Services Testing

Testing hosted services, background tasks, and workers.

### Background Service Implementation

```csharp
public class EmailProcessingService : BackgroundService
{
    private readonly IEmailQueue _emailQueue;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailProcessingService> _logger;

    public EmailProcessingService(
        IEmailQueue emailQueue,
        IEmailSender emailSender,
        ILogger<EmailProcessingService> logger)
    {
        _emailQueue = emailQueue;
        _emailSender = emailSender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var emailMessage = await _emailQueue.DequeueAsync(stoppingToken);
                if (emailMessage != null)
                {
                    await ProcessEmailAsync(emailMessage);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email queue");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task ProcessEmailAsync(EmailMessage message)
    {
        _logger.LogInformation("Processing email to {Recipient}", message.To);
        
        await _emailSender.SendAsync(message);
        
        _logger.LogInformation("Email sent successfully to {Recipient}", message.To);
    }
}
```

### Testing Background Service

```csharp
public class EmailProcessingServiceTests : MockerTestBase<EmailProcessingService>
{
    [Fact]
    public async Task ExecuteAsync_ShouldProcessQueuedEmails()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Test Body"
        };

        var cancellationTokenSource = new CancellationTokenSource();
        
        Mocks.GetMock<IEmailQueue>()
            .SetupSequence(x => x.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailMessage)
            .ReturnsAsync((EmailMessage)null); // Stop after one message

        Mocks.GetMock<IEmailSender>()
            .Setup(x => x.SendAsync(emailMessage))
            .Returns(Task.CompletedTask);

        // Act
        var executeTask = Component.StartAsync(cancellationTokenSource.Token);
        
        // Give it time to process
        await Task.Delay(100);
        cancellationTokenSource.Cancel();
        
        await executeTask;

        // Assert
        Mocks.GetMock<IEmailSender>()
            .Verify(x => x.SendAsync(emailMessage), Times.Once);
            
        Mocks.GetMock<ILogger<EmailProcessingService>>()
            .VerifyLogger(LogLevel.Information,
            "Processing email to {Recipient}", Times.Once());
        Mocks.GetMock<ILogger<EmailProcessingService>>()
            .VerifyLogger(LogLevel.Information,
            "Email sent successfully to {Recipient}", Times.Once());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogErrorAndContinue_WhenEmailSenderFails()
    {
        // Arrange
        var emailMessage = new EmailMessage { To = "test@example.com" };
        var cancellationTokenSource = new CancellationTokenSource();

        Mocks.GetMock<IEmailQueue>()
            .Setup(x => x.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailMessage);

        Mocks.GetMock<IEmailSender>()
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
            .ThrowsAsync(new InvalidOperationException("SMTP error"));

        // Act
        var executeTask = Component.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(100);
        cancellationTokenSource.Cancel();
        await executeTask;

        // Assert
        Mocks.GetMock<ILogger<EmailProcessingService>>()
            .VerifyLogger(LogLevel.Error, Times.AtLeastOnce());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopGracefully_WhenCancellationRequested()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        
        Mocks.GetMock<IEmailQueue>()
            .Setup(x => x.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async token =>
            {
                await Task.Delay(50, token);
                return null;
            });

        // Act
        var executeTask = Component.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(25); // Let it start
        cancellationTokenSource.Cancel();
        await executeTask;

        // Assert - Should complete without throwing
        executeTask.IsCompletedSuccessfully.Should().BeTrue();
    }
}
```

---

## HttpClient and External API Testing

Testing services that make HTTP calls to external APIs.

### Service Implementation

```csharp
public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private readonly IOptions<WeatherApiOptions> _options;

    public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger, IOptions<WeatherApiOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options;
    }

    public async Task<WeatherData> GetWeatherAsync(string city)
    { 
        try
        {
            var url = $"weather?q={city}&appid={_options.Value.ApiKey}";
            _logger.LogInformation("Fetching weather for {City}", city);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var weatherData = JsonSerializer.Deserialize<WeatherData>(content);
            
            _logger.LogInformation("Successfully retrieved weather for {City}", city);
            return weatherData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch weather for {City}", city);
            throw new WeatherServiceException($"Unable to get weather for {city}", ex);
        }
    }
}
```

### Testing with FastMoq HttpClient Support

FastMoq provides built-in **HTTP extension helpers** from `MockerHttpExtensions.cs` that simplify HTTP client testing. There are two main approaches:

#### Quick Setup with CreateHttpClient (Best for Simple Scenarios)

```csharp
using FastMoq;
using FastMoq.Extensions;

public class WeatherServiceQuickTests : MockerTestBase<WeatherService>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        // Setup options
        var options = new WeatherApiOptions { ApiKey = "test-api-key" };
        mocker.GetMock<IOptions<WeatherApiOptions>>()
            .Setup(x => x.Value)
            .Returns(options);
        
        // ✅ EASIEST - CreateHttpClient with defaults (auto-registers HttpClient)
        var httpClient = mocker.CreateHttpClient(
            clientName: "WeatherApiClient",
            baseAddress: "https://api.openweathermap.org/data/2.5/",
            statusCode: HttpStatusCode.OK,
            stringContent: JsonSerializer.Serialize(new { temperature = 20.5, humidity = 65, description = "Partly cloudy" })
        );
        
        // HttpClient is automatically available for dependency injection
    };

    [Fact]
    public async Task GetWeatherAsync_ShouldReturnWeatherData_WithCreateHttpClient()
    {
        // Arrange
        var city = "London";

        // Act - Component automatically gets the configured HttpClient
        var result = await Component.GetWeatherAsync(city);

        // Assert
        result.Should().NotBeNull();
        result.Temperature.Should().Be(20.5);
        result.Description.Should().Be("Partly cloudy");

        // Verify HttpClient was used with correct base address
        Mocks.GetMock<HttpMessageHandler>()
            .Protected()
            .Verify("SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString().StartsWith("https://api.openweathermap.org/data/2.5/")
                ),
                ItExpr.IsAny<CancellationToken>()
            );
    }

    [Fact]
    public async Task GetWeatherAsync_ShouldUseBuiltInHttpClient_WhenNoCustomSetup()
    {
        // Arrange - FastMoq provides default HttpClient with JSON response [{'id':1}]
        var city = "Paris";

        // Act - Uses Mocker's built-in HttpClient (http://localhost with default response)
        var httpResponse = await Mocks.HttpClient.GetAsync($"weather?q={city}");

        // Assert - Use FastMoq's GetStringContent helper
        var content = await Mocks.GetStringContent(httpResponse.Content);
        content.Should().Be("[{'id':1}]"); // Default response from CreateHttpClient
        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

#### Advanced Setup with SetupHttpMessage (Best for Complex Scenarios)

```csharp
public class WeatherServiceAdvancedTests : MockerTestBase<WeatherService>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        // Setup options
        var options = new WeatherApiOptions { ApiKey = "test-api-key" };
        mocker.GetMock<IOptions<WeatherApiOptions>>()
            .Setup(x => x.Value)
            .Returns(options);
    };

    [Fact]
    public async Task GetWeatherAsync_ShouldReturnWeatherData_WithSetupHttpMessage()
    {
        // Arrange
        var city = "London";
        var expectedWeatherData = new WeatherData
        {
            Temperature = 20.5,
            Humidity = 65,
            Description = "Partly cloudy"
        };

        var responseContent = JsonSerializer.Serialize(expectedWeatherData);
        
        // ✅ ADVANCED - SetupHttpMessage for fine-grained control
        Mocks.SetupHttpMessage(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json"),
            Headers = { { "X-API-Version", "1.0" } }
        });

        // Act
        var result = await Component.GetWeatherAsync(city);

        // Assert
        result.Should().BeEquivalentTo(expectedWeatherData);
        
        // Verify logging
        Mocks.GetMock<ILogger<WeatherService>>()
            .VerifyLogger(LogLevel.Information, "Fetching weather for {City}", Times.Once());
        Mocks.GetMock<ILogger<WeatherService>>()
            .VerifyLogger(LogLevel.Information, "Successfully retrieved weather for {City}", Times.Once());
    }

    [Fact]
    public async Task GetWeatherAsync_ShouldThrowException_WhenApiReturnsError()
    {
        // Arrange
        var city = "InvalidCity";
        
        // Override setup for error scenario
        Mocks.SetupHttpMessage(() => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("City not found", Encoding.UTF8, "text/plain")
        });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<WeatherServiceException>(() =>
            Component.GetWeatherAsync(city));
            
        exception.Message.Should().Contain(city);
        
        Mocks.GetMock<ILogger<WeatherService>>()
            .VerifyLogger(LogLevel.Error, Times.Once());
    }

    [Fact]
    public async Task GetWeatherAsync_ShouldExtractContent_UsingFastMoqHelpers()
    {
        // Arrange
        var city = "Tokyo";
        var expectedJson = JsonSerializer.Serialize(new { temperature = 18, description = "Cloudy" });
        
        Mocks.SetupHttpMessage(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedJson, Encoding.UTF8, "application/json")
        });

        // Act
        var response = await Mocks.HttpClient.GetAsync($"weather?q={city}&appid=test-api-key");
        
        // Assert - Demonstrate FastMoq's GetStringContent helper
        var content = await Mocks.GetStringContent(response.Content);
        content.Should().Be(expectedJson);
    }
}
```

### Advanced HTTP Testing Scenarios

#### Combining CreateHttpClient with IHttpClientFactory

```csharp
public class WeatherServiceFactoryTests : MockerTestBase<WeatherService>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        // Setup options
        var options = new WeatherApiOptions { ApiKey = "test-api-key" };
        mocker.GetMock<IOptions<WeatherApiOptions>>()
            .Setup(x => x.Value)
            .Returns(options);
        
        // ✅ CreateHttpClient automatically sets up IHttpClientFactory
        mocker.CreateHttpClient(
            clientName: "WeatherApiClient",
            baseAddress: "https://api.openweathermap.org/data/2.5/",
            statusCode: HttpStatusCode.OK,
            stringContent: JsonSerializer.Serialize(new { temperature = 25, description = "Sunny" })
        );
        
        // IHttpClientFactory is now available and configured
    };

    [Fact]
    public async Task GetWeatherAsync_ShouldUseNamedHttpClient_WhenIHttpClientFactoryProvided()
    {
        // Arrange
        var city = "Miami";
        
        // Act - Service can use named HttpClient from factory
        var factory = Mocks.GetObject<IHttpClientFactory>();
        var httpClient = factory!.CreateClient("WeatherApiClient");
        
        var response = await httpClient.GetAsync($"weather?q={city}&appid=test-api-key");
        var content = await Mocks.GetStringContent(response.Content);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("temperature");
        httpClient.BaseAddress.Should().Be("https://api.openweathermap.org/data/2.5/");
    }
}
```

#### Multiple HTTP Responses and Content Helpers

```csharp
public class WeatherServiceContentTests : MockerTestBase<WeatherService>
{
    [Fact]
    public async Task GetWeatherAsync_ShouldHandleDifferentContentTypes()
    {
        // Arrange
        var city = "London";
        var weatherData = new WeatherData { Temperature = 20 };
        var responseContent = JsonSerializer.Serialize(weatherData);

        // Test JSON content
        Mocks.SetupHttpMessage(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        });

        // Act
        var result = await Component.GetWeatherAsync(city);

        // Assert
        result.Should().BeEquivalentTo(weatherData);
    }

    [Fact]
    public async Task GetWeatherAsync_WithBinaryContent_ShouldUseContentHelpers()
    {
        // Arrange
        var binaryData = Encoding.UTF8.GetBytes("Binary weather data");
        
        Mocks.SetupHttpMessage(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(binaryData)
        });

        // Act
        var response = await Mocks.HttpClient.GetAsync("weather");
        
        // Assert - Use FastMoq's content helpers from MockerHttpExtensions
        var contentBytes = await response.Content.GetContentBytesAsync();
        var contentStream = await response.Content.GetContentStreamAsync();
        
        contentBytes.Should().BeEquivalentTo(binaryData);
        contentStream.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWeatherAsync_WithCustomSetup_ShouldAllowMultipleResponses()
    {
        // Arrange
        var city = "London";
        var weatherData = new WeatherData { Temperature = 20 };
        var responseContent = JsonSerializer.Serialize(weatherData);

        // Setup sequence of responses using SetupHttpMessage
        var callCount = 0;
        Mocks.SetupHttpMessage(() =>
        {
            callCount++;
            return callCount == 1 
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError) // First call fails
                : new HttpResponseMessage(HttpStatusCode.OK) // Second call succeeds
                {
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                };
        });

        // Act & Assert - This would need retry logic in the actual service
        var firstResponse = await Mocks.HttpClient.GetAsync($"weather?q={city}");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        
        var secondResponse = await Mocks.HttpClient.GetAsync($"weather?q={city}");
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await Mocks.GetStringContent(secondResponse.Content);
        content.Should().Be(responseContent);
    }
}
```

#### FastMoq HTTP Extensions Summary

FastMoq provides several convenient methods for HTTP testing:

| Method | Purpose | Best For |
|--------|---------|----------|
| `CreateHttpClient()` | Quick setup with defaults | Simple scenarios with standard responses |
| `SetupHttpMessage()` | Fine-grained response control | Complex scenarios, custom headers, error cases |
| `GetStringContent()` | Extract string from HttpContent | Reading response content in tests |
| `GetContentBytesAsync()` | Extract bytes from HttpContent | Binary content testing |
| `GetContentStreamAsync()` | Extract stream from HttpContent | Stream-based content testing |

**Key Benefits:**

- **Auto-Registration**: `CreateHttpClient` automatically registers `HttpClient` and `IHttpClientFactory`
- **Default Values**: Provides sensible defaults (localhost, OK status, JSON response)
- **Flexible Setup**: `SetupHttpMessage` allows custom response creation
- **Content Helpers**: Built-in methods for content extraction
- **Built-in HttpClient**: `Mocks.HttpClient` always available with default configuration

**Pattern Recommendations:**

- Use `CreateHttpClient` for simple test setups with consistent responses
- Use `SetupHttpMessage` when you need different responses per test or custom headers
- Combine both approaches: `CreateHttpClient` in setup, `SetupHttpMessage` for specific test cases
- Always use `GetStringContent()` and other helpers instead of manual content reading

---

---

## Configuration and Options Testing

Testing services that depend on configuration and options patterns.

### Service with Configuration

```csharp
public class EmailOptions
{
    public string SmtpHost { get; set; }
    public int SmtpPort { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public bool EnableSsl { get; set; } = true;
}

public class EmailService
{
    private readonly IOptions<EmailOptions> _emailOptions;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(IOptions<EmailOptions> emailOptions, ILogger<EmailService> logger, IConfiguration configuration)
    {
        _emailOptions = emailOptions;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        var options = _emailOptions.Value;
        
        if (string.IsNullOrEmpty(options.SmtpHost))
        {
            _logger.LogError("SMTP host not configured");
            return false;
        }

        var timeout = _configuration.GetValue<int>("Email:TimeoutSeconds", 30);
        
        _logger.LogInformation("Sending email to {Recipient} via {SmtpHost}:{SmtpPort}", 
            to, options.SmtpHost, options.SmtpPort);

        // Simulate sending email
        await Task.Delay(100);
        
        _logger.LogInformation("Email sent successfully to {Recipient}", to);
        return true;
    }

    public string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection");
    }
}
```

### Testing with Configuration

```csharp
public class EmailServiceTests : MockerTestBase<EmailService>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        // Setup email options
        var emailOptions = new EmailOptions
        {
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            Username = "test@example.com",
            Password = "password",
            EnableSsl = true
        };

        mocker.GetMock<IOptions<EmailOptions>>()
            .Setup(x => x.Value)
            .Returns(emailOptions);

        // Setup configuration
        var configurationData = new Dictionary<string, string>
        {
            ["Email:TimeoutSeconds"] = "45",
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=TestDb"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        mocker.AddType<IConfiguration>(() => configuration);
    };

    [Fact]
    public async Task SendEmailAsync_WithValidConfiguration_ShouldSendEmail()
    {
        // Arrange
        var to = "recipient@example.com";
        var subject = "Test Subject";
        var body = "Test Body";

        // Act
        var result = await Component.SendEmailAsync(to, subject, body);

        // Assert
        result.Should().BeTrue();
        
        Mocks.VerifyLogger<EmailService>(LogLevel.Information,
            "Sending email to {Recipient} via {SmtpHost}:{SmtpPort}", Times.Once());
        Mocks.VerifyLogger<EmailService>(LogLevel.Information,
            "Email sent successfully to {Recipient}", Times.Once());
    }

    [Fact]
    public async Task SendEmailAsync_WithMissingSmtpHost_ShouldReturnFalse()
    {
        // Arrange - Override options with empty SMTP host
        var emailOptions = new EmailOptions { SmtpHost = "" };
        Mocks.GetMock<IOptions<EmailOptions>>()
            .Setup(x => x.Value)
            .Returns(emailOptions);

        // Act
        var result = await Component.SendEmailAsync("test@example.com", "Subject", "Body");

        // Assert
        result.Should().BeFalse();
        Mocks.VerifyLogger<EmailService>(LogLevel.Error, "SMTP host not configured", Times.Once());
    }

    [Fact]
    public void GetConnectionString_ShouldReturnConfiguredValue()
    {
        // Act
        var connectionString = Component.GetConnectionString();

        // Assert
        connectionString.Should().Be("Server=localhost;Database=TestDb");
    }

    [Fact]
    public async Task SendEmailAsync_ShouldUseConfiguredTimeout()
    {
        // Arrange
        var configMock = Mocks.GetMock<IConfiguration>();
        configMock.Setup(x => x.GetValue<int>("Email:TimeoutSeconds", 30))
            .Returns(60);

        // Act
        await Component.SendEmailAsync("test@example.com", "Subject", "Body");

        // Assert
        configMock.Verify(x => x.GetValue<int>("Email:TimeoutSeconds", 30), Times.Once);
    }
}
```

### Testing with IOptionsMonitor

```csharp
public class EmailServiceWithMonitorTests : MockerTestBase<EmailService>
{
    [Fact]
    public async Task SendEmailAsync_WhenOptionsChange_ShouldUseNewOptions()
    {
        // Arrange
        var optionsMonitor = Mocks.GetMock<IOptionsMonitor<EmailOptions>>();
        var initialOptions = new EmailOptions { SmtpHost = "old-smtp.com", SmtpPort = 25 };
        var updatedOptions = new EmailOptions { SmtpHost = "new-smtp.com", SmtpPort = 587 };

        optionsMonitor.Setup(x => x.CurrentValue)
            .Returns(initialOptions);

        // Act - First call
        await Component.SendEmailAsync("test@example.com", "Subject", "Body");

        // Change options
        optionsMonitor.Setup(x => x.CurrentValue)
            .Returns(updatedOptions);

        // Act - Second call
        await Component.SendEmailAsync("test@example.com", "Subject", "Body");

        // Assert
        Mocks.VerifyLogger<EmailService>(LogLevel.Information,
            "Sending email to {Recipient} via {SmtpHost}:{SmtpPort}",
            Times.Exactly(2));
    }
}
```

---

## Logging Verification

FastMoq provides powerful helpers for testing logging behavior through the `FastMoq.Extensions` namespace.

### Proper Logger Verification Pattern

Always use the `VerifyLogger` extension method on the mock logger:

```csharp
// ✅ CORRECT - Use extension method on mock
Mocks.GetMock<ILogger<MyService>>()
    .VerifyLogger(LogLevel.Information, "Processing complete", Times.Once());

// ✅ CORRECT - With exception verification
Mocks.GetMock<ILogger<MyService>>()
    .VerifyLogger(LogLevel.Error, "Error occurred", exception, Times.Once());

// ❌ INCORRECT - Old pattern (deprecated)
Mocks.VerifyLogger<MyService>(LogLevel.Information, "Processing complete", Times.Once());
```

### Service with Logging

```csharp
public class OrderProcessingService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<OrderProcessingService> _logger;

    public OrderProcessingService(
        IOrderRepository orderRepository,
        IPaymentService paymentService,
        ILogger<OrderProcessingService> logger)
    {
        _orderRepository = orderRepository;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<ProcessOrderResult> ProcessOrderAsync(int orderId)
    {
        using var scope = _logger.BeginScope("Processing order {OrderId}", orderId);
        
        try
        {
            _logger.LogInformation("Starting order processing for order {OrderId}", orderId);

            var order = await _orderRepository.GetOrderAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", orderId);
                return ProcessOrderResult.NotFound();
            }

            _logger.LogDebug("Order {OrderId} retrieved: {OrderTotal:C}", orderId, order.Total);

            var paymentResult = await _paymentService.ProcessPaymentAsync(order.Total);
            if (!paymentResult.Success)
            {
                _logger.LogError("Payment failed for order {OrderId}: {ErrorMessage}", 
                    orderId, paymentResult.ErrorMessage);
                return ProcessOrderResult.PaymentFailed(paymentResult.ErrorMessage);
            }

            order.Status = OrderStatus.Completed;
            await _orderRepository.UpdateOrderAsync(order);

            _logger.LogInformation("Order {OrderId} processed successfully", orderId);
            return ProcessOrderResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing order {OrderId}", orderId);
            throw;
        }
    }
}
```

### Comprehensive Logging Tests

```csharp
public class OrderProcessingServiceTests : MockerTestBase<OrderProcessingService>
{
    [Fact]
    public async Task ProcessOrderAsync_WhenSuccessful_ShouldLogCorrectSequence()
    {
        // Arrange
        var orderId = 123;
        var order = new Order { Id = orderId, Total = 99.99m };
        var paymentResult = new PaymentResult { Success = true };

        Mocks.GetMock<IOrderRepository>()
            .Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync(order);

        Mocks.GetMock<IPaymentService>()
            .Setup(x => x.ProcessPaymentAsync(order.Total))
            .ReturnsAsync(paymentResult);

        // Act
        var result = await Component.ProcessOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify logging sequence
        Mocks.VerifyLogger<OrderProcessingService>(LogLevel.Information,
            "Starting order processing for order {OrderId}", Times.Once());
        
        Mocks.VerifyLogger<OrderProcessingService>(LogLevel.Debug,
            "Order {OrderId} retrieved: {OrderTotal:C}", Times.Once());
            
        Mocks.VerifyLogger<OrderProcessingService>(LogLevel.Information,
            "Order {OrderId} processed successfully", Times.Once());

        // Verify no error logging
        Mocks.VerifyLogger<OrderProcessingService>(LogLevel.Error, Times.Never());
    }

    [Fact]
    public async Task ProcessOrderAsync_WhenOrderNotFound_ShouldLogWarning()
    {
        // Arrange
        var orderId = 999;
        
        Mocks.GetMock<IOrderRepository>()
            .Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync((Order)null);

        // Act
        var result = await Component.ProcessOrderAsync(orderId);

        // Assert
        result.IsNotFound.Should().BeTrue();
        
        Mocks.VerifyLogger<OrderProcessingService>(LogLevel.Warning,
            "Order {OrderId} not found", Times.Once());
    }

    [Fact]
    public async Task ProcessOrderAsync_WhenPaymentFails_ShouldLogError()
    {
        // Arrange
        var orderId = 123;
        var order = new Order { Id = orderId, Total = 99.99m };
        var paymentResult = new PaymentResult 
        { 
            Success = false, 
            ErrorMessage = "Insufficient funds" 
        };

        Mocks.GetMock<IOrderRepository>()
            .Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync(order);

        Mocks.GetMock<IPaymentService>()
            .Setup(x => x.ProcessPaymentAsync(order.Total))
            .ReturnsAsync(paymentResult);

        // Act
        var result = await Component.ProcessOrderAsync(orderId);

        // Assert
        result.IsPaymentFailed.Should().BeTrue();
        
        Mocks.VerifyLogger<OrderProcessingService>(LogLevel.Error,
            "Payment failed for order {OrderId}: {ErrorMessage}", Times.Once());
    }

    [Fact]
    public async Task ProcessOrderAsync_WhenExceptionThrown_ShouldLogErrorWithException()
    {
        // Arrange
        var orderId = 123;
        var expectedException = new InvalidOperationException("Database connection failed");

        Mocks.GetMock<IOrderRepository>()
            .Setup(x => x.GetOrderAsync(orderId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Component.ProcessOrderAsync(orderId));

        thrownException.Should().Be(expectedException);
        
        Mocks.VerifyLogger<OrderProcessingService>(LogLevel.Error,
            "Unexpected error processing order {OrderId}", 
            expectedException, Times.Once());
    }

    [Fact]
    public async Task ProcessOrderAsync_ShouldUseLoggingScope()
    {
        // Arrange
        var orderId = 123;
        var order = new Order { Id = orderId, Total = 99.99m };

        Mocks.GetMock<IOrderRepository>()
            .Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync(order);

        // Setup logging callback to capture scope information
        var logEntries = new List<(LogLevel Level, string Message, object[] Args)>();
        
        Mocks.SetupLoggerCallback<ILogger<OrderProcessingService>>((level, eventId, message) =>
        {
            logEntries.Add((level, message, new object[0]));
        });

        // Act
        await Component.ProcessOrderAsync(orderId);

        // Assert
        logEntries.Should().NotBeEmpty();
        // Verify scope was used (implementation depends on logging framework)
    }
}
```

### Advanced Logging Scenarios

```csharp
public class LoggingAdvancedTests : MockerTestBase<OrderProcessingService>
{
    [Fact]
    public async Task ProcessOrderAsync_ShouldLogStructuredData()
    {
        // Arrange
        var orderId = 123;
        var order = new Order { Id = orderId, Total = 99.99m, CustomerId = 456 };

        Mocks.GetMock<IOrderRepository>()
            .Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync(order);

        // Setup to capture structured logging data
        var loggedProperties = new Dictionary<string, object>();
        
        Mocks.GetMock<ILogger<OrderProcessingService>>()
            .Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()))
            .Callback<LogLevel, EventId, object, Exception, Delegate>((level, eventId, state, exception, formatter) =>
            {
                if (state is IEnumerable<KeyValuePair<string, object>> properties)
                {
                    foreach (var prop in properties)
                    {
                        loggedProperties[prop.Key] = prop.Value;
                    }
                }
            });

        // Act
        await Component.ProcessOrderAsync(orderId);

        // Assert
        loggedProperties.Should().ContainKey("OrderId");
        loggedProperties["OrderId"].Should().Be(orderId);
    }

    [Fact]
    public async Task ProcessOrderAsync_WithHighVolumeLogging_ShouldPerformWell()
    {
        // Arrange
        var orders = Enumerable.Range(1, 100)
            .Select(i => new Order { Id = i, Total = i * 10m })
            .ToList();

        foreach (var order in orders)
        {
            Mocks.GetMock<IOrderRepository>()
                .Setup(x => x.GetOrderAsync(order.Id))
                .ReturnsAsync(order);
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        var tasks = orders.Select(order => Component.ProcessOrderAsync(order.Id));
        await Task.WhenAll(tasks);
        
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Performance assertion
        
        Mocks.VerifyLogger<OrderProcessingService>(LogLevel.Information, Times.Exactly(200)); // 2 info logs per order
    }
}
```

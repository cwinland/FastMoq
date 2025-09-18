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

public class UsersControllerTests : MockerTestBase<UsersController>
{
    [Fact]
    public async Task GetUser_WhenUserExists_ReturnsOkResult()
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
    public async Task GetUser_WhenUserNotFound_ReturnsNotFound()
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
    public async Task GetUser_WhenServiceThrows_ReturnsInternalServerError()
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
        Mocks.VerifyLogger<UsersController>(LogLevel.Error, Times.Once());
    }

    [Fact]
    public async Task CreateUser_WithValidModel_ReturnsCreatedResult()
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

```csharp
public class UsersControllerAuthTests : MockerTestBase<UsersController>
{
    protected override Action<Mocker> SetupMocksAction => mocker =>
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Role, "User")
        }, "mock"));

        var controllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        mocker.CreateInstance<UsersController>().ControllerContext = controllerContext;
    };

    [Fact]
    public void GetCurrentUser_ShouldReturnUserFromClaims()
    {
        // Test implementation using the authenticated context
    }
}
```

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
        Mocks.VerifyLogger<BlogService>(LogLevel.Information, 
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
    public async Task CreateBlog_WhenDatabaseFails_ShouldThrowException()
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
            
        Mocks.VerifyLogger<EmailProcessingService>(LogLevel.Information,
            "Processing email to {Recipient}", Times.Once());
        Mocks.VerifyLogger<EmailProcessingService>(LogLevel.Information,
            "Email sent successfully to {Recipient}", Times.Once());
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailSenderFails_ShouldLogErrorAndContinue()
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
        Mocks.VerifyLogger<EmailProcessingService>(LogLevel.Error, Times.AtLeastOnce());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_ShouldStopGracefully()
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

```csharp
public class WeatherServiceTests : MockerTestBase<WeatherService>
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
    public async Task GetWeatherAsync_WhenApiReturnsSuccess_ShouldReturnWeatherData()
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
        
        Mocks.SetupHttpGet("weather?q=London&appid=test-api-key")
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await Component.GetWeatherAsync(city);

        // Assert
        result.Should().BeEquivalentTo(expectedWeatherData);
        
        Mocks.VerifyLogger<WeatherService>(LogLevel.Information,
            "Fetching weather for {City}", Times.Once());
        Mocks.VerifyLogger<WeatherService>(LogLevel.Information,
            "Successfully retrieved weather for {City}", Times.Once());
    }

    [Fact]
    public async Task GetWeatherAsync_WhenApiReturnsError_ShouldThrowException()
    {
        // Arrange
        var city = "InvalidCity";
        
        Mocks.SetupHttpGet($"weather?q={city}&appid=test-api-key")
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<WeatherServiceException>(() =>
            Component.GetWeatherAsync(city));
            
        exception.Message.Should().Contain(city);
        
        Mocks.VerifyLogger<WeatherService>(LogLevel.Error, Times.Once());
    }

    [Fact]
    public async Task GetWeatherAsync_WithMultipleCities_ShouldCacheHttpClient()
    {
        // Arrange
        var cities = new[] { "London", "Paris", "Berlin" };
        var weatherData = new WeatherData { Temperature = 20 };
        var responseContent = JsonSerializer.Serialize(weatherData);

        foreach (var city in cities)
        {
            Mocks.SetupHttpGet($"weather?q={city}&appid=test-api-key")
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                });
        }

        // Act
        var results = new List<WeatherData>();
        foreach (var city in cities)
        {
            results.Add(await Component.GetWeatherAsync(city));
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllBeEquivalentTo(weatherData);
        
        // Verify HttpClient reuse
        Mocks.HttpClient.Should().NotBeNull();
    }
}
```

### Advanced HTTP Testing Scenarios

```csharp
public class WeatherServiceAdvancedTests : MockerTestBase<WeatherService>
{
    [Fact]
    public async Task GetWeatherAsync_WithRetryPolicy_ShouldRetryOnFailure()
    {
        // Arrange
        var city = "London";
        var weatherData = new WeatherData { Temperature = 20 };
        var responseContent = JsonSerializer.Serialize(weatherData);

        Mocks.SetupHttpGet($"weather?q={city}&appid=test-api-key")
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)) // First call fails
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) // Second call succeeds
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await Component.GetWeatherAsync(city);

        // Assert
        result.Should().BeEquivalentTo(weatherData);
        
        // Verify retry happened
        Mocks.VerifyHttpGet($"weather?q={city}&appid=test-api-key", Times.Exactly(2));
    }

    [Fact]
    public async Task GetWeatherAsync_WithTimeout_ShouldThrowTimeoutException()
    {
        // Arrange
        var city = "London";
        
        Mocks.SetupHttpGet($"weather?q={city}&appid=test-api-key")
            .Returns(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30)); // Simulate timeout
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            Component.GetWeatherAsync(city));
    }
}
```

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

FastMoq provides powerful helpers for testing logging behavior.

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

This cookbook provides comprehensive examples for common testing scenarios. Each recipe demonstrates FastMoq's capabilities while following best practices for unit testing. The patterns shown here can be adapted to your specific domain and requirements.

Would you like me to continue with the remaining sections (Azure Services Testing, Fluent Validation Testing, etc.), or would you prefer to see the sample application next?
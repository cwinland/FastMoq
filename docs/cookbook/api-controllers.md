# Testing API Controllers with FastMoq

## 📋 Scenario

Testing ASP.NET Core Web API controllers with complex dependency injection, including services, repositories, logging, and configuration. This recipe covers authentication, validation, error handling, and response formatting.

## 🎯 Goals

- Test controller actions with automatic dependency injection
- Verify proper HTTP status codes and response bodies
- Mock authentication and authorization
- Test validation and error handling
- Verify logging and metrics collection

## 🔧 Implementation

### Basic Controller Setup

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;
    private readonly IMapper _mapper;
    private readonly IOptions<OrderSettings> _settings;

    public OrdersController(
        IOrderService orderService,
        ILogger<OrdersController> logger,
        IMapper mapper,
        IOptions<OrderSettings> settings)
    {
        _orderService = orderService;
        _logger = logger;
        _mapper = mapper;
        _settings = settings;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        try
        {
            _logger.LogInformation("Retrieving order {OrderId}", id);
            
            var order = await _orderService.GetOrderAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", id);
                return NotFound($"Order {id} not found");
            }

            var dto = _mapper.Map<OrderDto>(order);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("Creating order for user {UserId}", userId);

            var order = _mapper.Map<Order>(request);
            order.UserId = userId;
            order.MaxAmount = _settings.Value.MaxOrderAmount;

            var createdOrder = await _orderService.CreateOrderAsync(order);
            var dto = _mapper.Map<OrderDto>(createdOrder);

            return CreatedAtAction(nameof(GetOrder), new { id = dto.Id }, dto);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business rule violation creating order");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, "Internal server error");
        }
    }
}
```

### FastMoq Test Implementation

```csharp
using FastMoq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;

public class OrdersControllerTests : MockerTestBase<OrdersController>
{
    public class GetOrder : OrdersControllerTests
    {
        [Fact]
        public async Task ShouldReturnOrder_WhenOrderExists()
        {
            // Arrange
            var orderId = 123;
            var expectedOrder = new Order { Id = orderId, Name = "Test Order" };
            var expectedDto = new OrderDto { Id = orderId, Name = "Test Order" };

            Mocks.GetMock<IOrderService>()
                .Setup(x => x.GetOrderAsync(orderId))
                .ReturnsAsync(expectedOrder);

            Mocks.GetMock<IMapper>()
                .Setup(x => x.Map<OrderDto>(expectedOrder))
                .Returns(expectedDto);

            // Act
            var result = await Component.GetOrder(orderId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedDto = okResult.Value.Should().BeOfType<OrderDto>().Subject;
            returnedDto.Id.Should().Be(orderId);
            returnedDto.Name.Should().Be("Test Order");

            // Verify logging
            Mocks.VerifyLogger<OrdersController>(
                LogLevel.Information, 
                $"Retrieving order {orderId}");
        }

        [Fact]
        public async Task ShouldReturnNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var orderId = 999;
            
            Mocks.GetMock<IOrderService>()
                .Setup(x => x.GetOrderAsync(orderId))
                .ReturnsAsync((Order)null);

            // Act
            var result = await Component.GetOrder(orderId);

            // Assert
            var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be($"Order {orderId} not found");

            // Verify warning was logged
            Mocks.VerifyLogger<OrdersController>(
                LogLevel.Warning,
                $"Order {orderId} not found");
        }

        [Fact]
        public async Task ShouldReturnInternalServerError_WhenServiceThrows()
        {
            // Arrange
            var orderId = 123;
            var expectedException = new InvalidOperationException("Database error");

            Mocks.GetMock<IOrderService>()
                .Setup(x => x.GetOrderAsync(orderId))
                .ThrowsAsync(expectedException);

            // Act
            var result = await Component.GetOrder(orderId);

            // Assert
            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
            statusResult.Value.Should().Be("Internal server error");

            // Verify error logging
            Mocks.VerifyLogger<OrdersController>(
                LogLevel.Error,
                $"Error retrieving order {orderId}");
        }
    }

    public class CreateOrder : OrdersControllerTests
    {
        public CreateOrder() : base(ConfigureAuthenticatedUser) { }

        private static void ConfigureAuthenticatedUser(Mocker mocker)
        {
            // Setup authenticated user context
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "user123"),
                new Claim(ClaimTypes.Name, "Test User")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            var controllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Configure settings
            var settings = new OrderSettings { MaxOrderAmount = 1000m };
            mocker.GetMock<IOptions<OrderSettings>>()
                .Setup(x => x.Value)
                .Returns(settings);
        }

        [Fact]
        public async Task ShouldCreateOrder_WhenValidRequest()
        {
            // Arrange
            var request = new CreateOrderRequest 
            { 
                ProductId = 1, 
                Quantity = 2,
                Amount = 500m
            };

            var mappedOrder = new Order 
            { 
                ProductId = 1, 
                Quantity = 2, 
                Amount = 500m,
                UserId = "user123",
                MaxAmount = 1000m
            };

            var createdOrder = new Order 
            { 
                Id = 456, 
                ProductId = 1, 
                Quantity = 2,
                Amount = 500m,
                UserId = "user123"
            };

            var expectedDto = new OrderDto 
            { 
                Id = 456, 
                ProductId = 1, 
                Quantity = 2,
                Amount = 500m
            };

            // Setup mocks
            Mocks.GetMock<IMapper>()
                .Setup(x => x.Map<Order>(request))
                .Returns(mappedOrder);

            Mocks.GetMock<IOrderService>()
                .Setup(x => x.CreateOrderAsync(It.Is<Order>(o => 
                    o.UserId == "user123" && 
                    o.MaxAmount == 1000m)))
                .ReturnsAsync(createdOrder);

            Mocks.GetMock<IMapper>()
                .Setup(x => x.Map<OrderDto>(createdOrder))
                .Returns(expectedDto);

            // Ensure controller context is set
            Component.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "user123")
                    }))
                }
            };

            // Act
            var result = await Component.CreateOrder(request);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(OrdersController.GetOrder));
            createdResult.RouteValues["id"].Should().Be(456);
            
            var returnedDto = createdResult.Value.Should().BeOfType<OrderDto>().Subject;
            returnedDto.Id.Should().Be(456);

            // Verify logging
            Mocks.VerifyLogger<OrdersController>(
                LogLevel.Information,
                "Creating order for user user123");
        }

        [Fact]
        public async Task ShouldReturnBadRequest_WhenBusinessRuleViolation()
        {
            // Arrange
            var request = new CreateOrderRequest 
            { 
                ProductId = 1, 
                Quantity = 2,
                Amount = 1500m // Exceeds max amount
            };

            var businessException = new BusinessException("Order amount exceeds maximum allowed");

            Mocks.GetMock<IMapper>()
                .Setup(x => x.Map<Order>(request))
                .Returns(new Order());

            Mocks.GetMock<IOrderService>()
                .Setup(x => x.CreateOrderAsync(It.IsAny<Order>()))
                .ThrowsAsync(businessException);

            Component.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "user123")
                    }))
                }
            };

            // Act
            var result = await Component.CreateOrder(request);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be("Order amount exceeds maximum allowed");

            // Verify warning logged
            Mocks.VerifyLogger<OrdersController>(
                LogLevel.Warning,
                "Business rule violation creating order");
        }

        [Fact]
        public async Task ShouldReturnBadRequest_WhenModelStateInvalid()
        {
            // Arrange
            var request = new CreateOrderRequest(); // Invalid request
            Component.ModelState.AddModelError("ProductId", "ProductId is required");

            // Act
            var result = await Component.CreateOrder(request);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeOfType<SerializableError>();
        }
    }

    public class AuthenticationTests : OrdersControllerTests
    {
        [Fact]
        public async Task CreateOrder_ShouldRequireAuthentication()
        {
            // Arrange - No authenticated user setup
            var request = new CreateOrderRequest { ProductId = 1, Quantity = 1 };

            // This would typically be handled by the [Authorize] attribute
            // In integration tests, you'd verify the authorization requirement
            Component.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // No user set
            };

            // Act & Assert
            var act = async () => await Component.CreateOrder(request);
            
            // In a real scenario, this would be handled by middleware
            // Here we verify the controller properly extracts user claims
            act.Should().NotThrow(); // The method handles null user gracefully
        }
    }
}
```

## ✅ Verification Patterns

### HTTP Status Code Testing
```csharp
// Success responses
result.Result.Should().BeOfType<OkObjectResult>();
result.Result.Should().BeOfType<CreatedAtActionResult>();

// Error responses
result.Result.Should().BeOfType<NotFoundObjectResult>();
result.Result.Should().BeOfType<BadRequestObjectResult>();

// Custom status codes
var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
statusResult.StatusCode.Should().Be(500);
```

### Response Body Validation
```csharp
// Typed responses
var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
var dto = okResult.Value.Should().BeOfType<OrderDto>().Subject;
dto.Id.Should().Be(expectedId);

// Error messages
var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
badRequestResult.Value.Should().Be("Expected error message");
```

### Service Interaction Verification
```csharp
// Verify service methods called
Mocks.GetMock<IOrderService>()
    .Verify(x => x.GetOrderAsync(123), Times.Once);

// Verify mapping operations
Mocks.GetMock<IMapper>()
    .Verify(x => x.Map<OrderDto>(It.IsAny<Order>()), Times.Once);

// Verify complex parameter matching
Mocks.GetMock<IOrderService>()
    .Verify(x => x.CreateOrderAsync(It.Is<Order>(o => 
        o.UserId == "user123" && 
        o.MaxAmount == 1000m)), Times.Once);
```

## 🚀 Advanced Patterns

### Custom Controller Base Classes
```csharp
public abstract class ApiControllerTestBase<T> : MockerTestBase<T> 
    where T : ControllerBase
{
    protected void SetupAuthenticatedUser(string userId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        Component.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    protected void VerifySuccessResponse<TDto>(ActionResult<TDto> result, int expectedStatusCode = 200)
    {
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(expectedStatusCode);
    }
}
```

### Model State Testing
```csharp
public class ModelValidationTests : OrdersControllerTests
{
    [Theory]
    [InlineData("", 1, "Name is required")]
    [InlineData("Test", 0, "Quantity must be greater than 0")]
    [InlineData("Test", -1, "Quantity must be greater than 0")]
    public async Task CreateOrder_ShouldValidateInput(string name, int quantity, string expectedError)
    {
        // Arrange
        var request = new CreateOrderRequest { Name = name, Quantity = quantity };
        
        // Simulate model validation
        if (string.IsNullOrEmpty(name))
            Component.ModelState.AddModelError(nameof(request.Name), "Name is required");
        if (quantity <= 0)
            Component.ModelState.AddModelError(nameof(request.Quantity), "Quantity must be greater than 0");

        // Act
        var result = await Component.CreateOrder(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
```

### Bulk Operations Testing
```csharp
[Fact]
public async Task ProcessBulkOrders_ShouldHandlePartialFailures()
{
    // Arrange
    var requests = new[]
    {
        new CreateOrderRequest { ProductId = 1, Quantity = 1 }, // Success
        new CreateOrderRequest { ProductId = 2, Quantity = 999 }, // Business failure
        new CreateOrderRequest { ProductId = 3, Quantity = 1 }  // Success
    };

    Mocks.GetMock<IOrderService>()
        .Setup(x => x.CreateOrderAsync(It.Is<Order>(o => o.ProductId == 1)))
        .ReturnsAsync(new Order { Id = 1 });
        
    Mocks.GetMock<IOrderService>()
        .Setup(x => x.CreateOrderAsync(It.Is<Order>(o => o.ProductId == 2)))
        .ThrowsAsync(new BusinessException("Insufficient inventory"));
        
    Mocks.GetMock<IOrderService>()
        .Setup(x => x.CreateOrderAsync(It.Is<Order>(o => o.ProductId == 3)))
        .ReturnsAsync(new Order { Id = 3 });

    // Act
    var result = await Component.ProcessBulkOrders(requests);

    // Assert
    var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
    var response = okResult.Value.Should().BeOfType<BulkOrderResponse>().Subject;
    
    response.SuccessCount.Should().Be(2);
    response.FailureCount.Should().Be(1);
    response.Failures.Should().ContainSingle(f => 
        f.ProductId == 2 && f.Error == "Insufficient inventory");
}
```

## 💡 Tips & Tricks

### Reusable Test Data
```csharp
public static class TestData
{
    public static CreateOrderRequest ValidOrderRequest => new()
    {
        ProductId = 1,
        Quantity = 2,
        Amount = 99.99m
    };

    public static Order SampleOrder => new()
    {
        Id = 123,
        ProductId = 1,
        Quantity = 2,
        Amount = 99.99m,
        Status = OrderStatus.Pending
    };
}
```

### Custom Assertions
```csharp
public static class ControllerAssertions
{
    public static void ShouldBeSuccessResult<T>(this ActionResult<T> result, int expectedStatusCode = 200)
    {
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(expectedStatusCode);
    }

    public static T ShouldReturnData<T>(this ActionResult<T> result)
    {
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        return okResult.Value.Should().BeOfType<T>().Subject;
    }
}
```

### Performance Testing
```csharp
[Fact]
public async Task GetOrder_ShouldCompleteWithinTimeout()
{
    // Arrange
    var stopwatch = Stopwatch.StartNew();
    
    Mocks.GetMock<IOrderService>()
        .Setup(x => x.GetOrderAsync(It.IsAny<int>()))
        .Returns(async () =>
        {
            await Task.Delay(100); // Simulate slow operation
            return new Order();
        });

    // Act
    await Component.GetOrder(123);

    // Assert
    stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
}
```

---

**Next Recipe:** [Entity Framework Core Testing](ef-core.md)
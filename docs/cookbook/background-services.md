# Testing Background Services with FastMoq

## 📋 Scenario

Testing ASP.NET Core hosted services, background workers, and scheduled jobs that run outside the HTTP request pipeline. This includes message processors, data synchronization services, and periodic maintenance tasks.

## 🎯 Goals

- Test hosted service lifecycle (start/stop/execute)
- Mock external dependencies in background processes
- Test cancellation token handling
- Verify logging and error handling
- Test scheduled and recurring operations

## 🔧 Implementation

### Background Service Example

```csharp
public class OrderProcessingService : BackgroundService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentService _paymentService;
    private readonly IEmailService _emailService;
    private readonly ILogger<OrderProcessingService> _logger;
    private readonly IOptions<ProcessingSettings> _settings;

    public OrderProcessingService(
        IOrderRepository orderRepository,
        IPaymentService paymentService,
        IEmailService emailService,
        ILogger<OrderProcessingService> logger,
        IOptions<ProcessingSettings> settings)
    {
        _orderRepository = orderRepository;
        _paymentService = paymentService;
        _emailService = emailService;
        _logger = logger;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order processing service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingOrdersAsync(stoppingToken);
                await Task.Delay(_settings.Value.ProcessingIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Order processing service stopped");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in order processing cycle");
                await Task.Delay(5000, stoppingToken); // Wait before retry
            }
        }
    }

    private async Task ProcessPendingOrdersAsync(CancellationToken cancellationToken)
    {
        var pendingOrders = await _orderRepository.GetPendingOrdersAsync(cancellationToken);
        
        if (!pendingOrders.Any())
        {
            _logger.LogDebug("No pending orders to process");
            return;
        }

        _logger.LogInformation("Processing {Count} pending orders", pendingOrders.Count());

        foreach (var order in pendingOrders)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessSingleOrderAsync(order, cancellationToken);
        }
    }

    private async Task ProcessSingleOrderAsync(Order order, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing order {OrderId}", order.Id);

        try
        {
            // Process payment
            var paymentResult = await _paymentService.ProcessPaymentAsync(
                order.PaymentDetails, 
                cancellationToken);

            if (!paymentResult.Success)
            {
                _logger.LogWarning("Payment failed for order {OrderId}: {Reason}", 
                    order.Id, paymentResult.FailureReason);
                
                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason = paymentResult.FailureReason;
            }
            else
            {
                order.Status = OrderStatus.Completed;
                order.ProcessedAt = DateTime.UtcNow;

                // Send confirmation email
                await _emailService.SendOrderConfirmationAsync(order, cancellationToken);
                
                _logger.LogInformation("Successfully processed order {OrderId}", order.Id);
            }

            // Update order status
            await _orderRepository.UpdateOrderAsync(order, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process order {OrderId}", order.Id);
            
            order.Status = OrderStatus.ProcessingFailed;
            order.FailureReason = ex.Message;
            await _orderRepository.UpdateOrderAsync(order, cancellationToken);
        }
    }
}

public class ProcessingSettings
{
    public int ProcessingIntervalMs { get; set; } = 30000; // 30 seconds
    public int MaxConcurrentOrders { get; set; } = 10;
    public int RetryDelayMs { get; set; } = 5000;
}
```

### Message Queue Worker

```csharp
public class EmailQueueWorker : BackgroundService
{
    private readonly IServiceBusReceiver _receiver;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailQueueWorker> _logger;

    public EmailQueueWorker(
        IServiceBusReceiver receiver,
        IEmailService emailService,
        ILogger<EmailQueueWorker> logger)
    {
        _receiver = receiver;
        _emailService = emailService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email queue worker started");

        await foreach (var message in _receiver.ReceiveMessagesAsync(stoppingToken))
        {
            try
            {
                var emailRequest = JsonSerializer.Deserialize<EmailRequest>(message.Body);
                
                _logger.LogInformation("Processing email request {MessageId}", message.MessageId);

                await _emailService.SendAsync(
                    emailRequest.To, 
                    emailRequest.Subject, 
                    emailRequest.Body,
                    stoppingToken);

                await _receiver.CompleteMessageAsync(message, stoppingToken);
                
                _logger.LogInformation("Successfully processed email {MessageId}", message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process email message {MessageId}", message.MessageId);
                await _receiver.AbandonMessageAsync(message, stoppingToken);
            }
        }
    }
}
```

## 🧪 FastMoq Testing Implementation

### Background Service Tests

```csharp
using FastMoq;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public class OrderProcessingServiceTests : MockerTestBase<OrderProcessingService>
{
    public class ExecuteAsync : OrderProcessingServiceTests
    {
        public ExecuteAsync() : base(ConfigureSettings) { }

        private static void ConfigureSettings(Mocker mocker)
        {
            var settings = new ProcessingSettings 
            { 
                ProcessingIntervalMs = 100 // Fast for testing
            };
            
            mocker.GetMock<IOptions<ProcessingSettings>>()
                .Setup(x => x.Value)
                .Returns(settings);
        }

        [Fact]
        public async Task ShouldProcessPendingOrders_WhenOrdersExist()
        {
            // Arrange
            var orders = new[]
            {
                new Order { Id = 1, Status = OrderStatus.Pending, PaymentDetails = new() },
                new Order { Id = 2, Status = OrderStatus.Pending, PaymentDetails = new() }
            };

            Mocks.GetMock<IOrderRepository>()
                .Setup(x => x.GetPendingOrdersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(orders);

            Mocks.GetMock<IPaymentService>()
                .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentDetails>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentResult { Success = true });

            Mocks.GetMock<IEmailService>()
                .Setup(x => x.SendOrderConfirmationAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Mocks.GetMock<IOrderRepository>()
                .Setup(x => x.UpdateOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using var cts = new CancellationTokenSource();
            
            // Act - Run for a short time then cancel
            var executeTask = Component.StartAsync(cts.Token);
            await Task.Delay(250); // Let it run for 250ms
            cts.Cancel();
            
            try
            {
                await executeTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation occurs
            }

            // Assert
            Mocks.GetMock<IOrderRepository>()
                .Verify(x => x.GetPendingOrdersAsync(It.IsAny<CancellationToken>()), 
                       Times.AtLeastOnce);

            Mocks.GetMock<IPaymentService>()
                .Verify(x => x.ProcessPaymentAsync(It.IsAny<PaymentDetails>(), It.IsAny<CancellationToken>()), 
                       Times.Exactly(2));

            Mocks.GetMock<IEmailService>()
                .Verify(x => x.SendOrderConfirmationAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), 
                       Times.Exactly(2));

            // Verify logging
            Mocks.VerifyLogger<OrderProcessingService>(
                LogLevel.Information, 
                "Order processing service started");
                
            Mocks.VerifyLogger<OrderProcessingService>(
                LogLevel.Information, 
                "Processing 2 pending orders");
        }

        [Fact]
        public async Task ShouldHandlePaymentFailure_Gracefully()
        {
            // Arrange
            var order = new Order 
            { 
                Id = 1, 
                Status = OrderStatus.Pending, 
                PaymentDetails = new() 
            };

            Mocks.GetMock<IOrderRepository>()
                .SetupSequence(x => x.GetPendingOrdersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { order })
                .ReturnsAsync(Array.Empty<Order>()); // No more orders after first cycle

            Mocks.GetMock<IPaymentService>()
                .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentDetails>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentResult 
                { 
                    Success = false, 
                    FailureReason = "Insufficient funds" 
                });

            Mocks.GetMock<IOrderRepository>()
                .Setup(x => x.UpdateOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using var cts = new CancellationTokenSource();

            // Act
            var executeTask = Component.StartAsync(cts.Token);
            await Task.Delay(250);
            cts.Cancel();
            
            try { await executeTask; } catch (OperationCanceledException) { }

            // Assert
            Mocks.GetMock<IOrderRepository>()
                .Verify(x => x.UpdateOrderAsync(
                    It.Is<Order>(o => 
                        o.Status == OrderStatus.PaymentFailed && 
                        o.FailureReason == "Insufficient funds"), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);

            // Verify email service was NOT called
            Mocks.GetMock<IEmailService>()
                .Verify(x => x.SendOrderConfirmationAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), 
                       Times.Never);

            // Verify warning logged
            Mocks.VerifyLogger<OrderProcessingService>(
                LogLevel.Warning, 
                "Payment failed for order 1: Insufficient funds");
        }

        [Fact]
        public async Task ShouldContinueProcessing_WhenSingleOrderFails()
        {
            // Arrange
            var orders = new[]
            {
                new Order { Id = 1, Status = OrderStatus.Pending },
                new Order { Id = 2, Status = OrderStatus.Pending }
            };

            Mocks.GetMock<IOrderRepository>()
                .SetupSequence(x => x.GetPendingOrdersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(orders)
                .ReturnsAsync(Array.Empty<Order>());

            // First order fails, second succeeds
            Mocks.GetMock<IPaymentService>()
                .SetupSequence(x => x.ProcessPaymentAsync(It.IsAny<PaymentDetails>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Payment gateway error"))
                .ReturnsAsync(new PaymentResult { Success = true });

            Mocks.GetMock<IOrderRepository>()
                .Setup(x => x.UpdateOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using var cts = new CancellationTokenSource();

            // Act
            var executeTask = Component.StartAsync(cts.Token);
            await Task.Delay(250);
            cts.Cancel();
            
            try { await executeTask; } catch (OperationCanceledException) { }

            // Assert - Both orders should be updated
            Mocks.GetMock<IOrderRepository>()
                .Verify(x => x.UpdateOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), 
                       Times.Exactly(2));

            // First order should be marked as failed
            Mocks.GetMock<IOrderRepository>()
                .Verify(x => x.UpdateOrderAsync(
                    It.Is<Order>(o => 
                        o.Id == 1 && 
                        o.Status == OrderStatus.ProcessingFailed), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);

            // Second order should be completed
            Mocks.GetMock<IOrderRepository>()
                .Verify(x => x.UpdateOrderAsync(
                    It.Is<Order>(o => 
                        o.Id == 2 && 
                        o.Status == OrderStatus.Completed), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);

            // Verify error logged for failed order
            Mocks.VerifyLogger<OrderProcessingService>(
                LogLevel.Error, 
                "Failed to process order 1");
        }
    }

    public class CancellationHandling : OrderProcessingServiceTests
    {
        [Fact]
        public async Task ShouldStopGracefully_WhenCancellationRequested()
        {
            // Arrange
            Mocks.GetMock<IOrderRepository>()
                .Setup(x => x.GetPendingOrdersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Order>());

            using var cts = new CancellationTokenSource();

            // Act
            var executeTask = Component.StartAsync(cts.Token);
            await Task.Delay(50); // Let it start
            cts.Cancel(); // Request cancellation
            
            // Should complete without throwing
            await executeTask;

            // Assert
            Mocks.VerifyLogger<OrderProcessingService>(
                LogLevel.Information, 
                "Order processing service stopped");
        }

        [Fact]
        public async Task ShouldRespectCancellation_DuringOrderProcessing()
        {
            // Arrange
            var orders = Enumerable.Range(1, 100)
                .Select(i => new Order { Id = i, Status = OrderStatus.Pending })
                .ToArray();

            Mocks.GetMock<IOrderRepository>()
                .Setup(x => x.GetPendingOrdersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(orders);

            // Make payment processing slow
            Mocks.GetMock<IPaymentService>()
                .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentDetails>(), It.IsAny<CancellationToken>()))
                .Returns(async (PaymentDetails details, CancellationToken ct) =>
                {
                    await Task.Delay(100, ct); // Simulate slow processing
                    return new PaymentResult { Success = true };
                });

            using var cts = new CancellationTokenSource();

            // Act
            var executeTask = Component.StartAsync(cts.Token);
            await Task.Delay(150); // Let it process a few orders
            cts.Cancel();
            
            await executeTask;

            // Assert - Should not have processed all 100 orders
            Mocks.GetMock<IPaymentService>()
                .Verify(x => x.ProcessPaymentAsync(It.IsAny<PaymentDetails>(), It.IsAny<CancellationToken>()), 
                       Times.LessThan(100));
        }
    }
}
```

### Message Queue Worker Tests

```csharp
public class EmailQueueWorkerTests : MockerTestBase<EmailQueueWorker>
{
    [Fact]
    public async Task ShouldProcessMessage_AndCompleteSuccessfully()
    {
        // Arrange
        var emailRequest = new EmailRequest
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body"
        };

        var message = new ServiceBusReceivedMessage
        {
            MessageId = "msg-123",
            Body = BinaryData.FromObjectAsJson(emailRequest)
        };

        var messages = new[] { message }.ToAsyncEnumerable();

        Mocks.GetMock<IServiceBusReceiver>()
            .Setup(x => x.ReceiveMessagesAsync(It.IsAny<CancellationToken>()))
            .Returns(messages);

        Mocks.GetMock<IEmailService>()
            .Setup(x => x.SendAsync("test@example.com", "Test Subject", "Test Body", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mocks.GetMock<IServiceBusReceiver>()
            .Setup(x => x.CompleteMessageAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = Component.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        
        try { await executeTask; } catch (OperationCanceledException) { }

        // Assert
        Mocks.GetMock<IEmailService>()
            .Verify(x => x.SendAsync("test@example.com", "Test Subject", "Test Body", It.IsAny<CancellationToken>()), 
                   Times.Once);

        Mocks.GetMock<IServiceBusReceiver>()
            .Verify(x => x.CompleteMessageAsync(message, It.IsAny<CancellationToken>()), 
                   Times.Once);

        // Verify logging
        Mocks.VerifyLogger<EmailQueueWorker>(
            LogLevel.Information, 
            "Successfully processed email msg-123");
    }

    [Fact]
    public async Task ShouldAbandonMessage_WhenEmailSendingFails()
    {
        // Arrange
        var emailRequest = new EmailRequest
        {
            To = "invalid@example.com",
            Subject = "Test Subject",
            Body = "Test Body"
        };

        var message = new ServiceBusReceivedMessage
        {
            MessageId = "msg-456",
            Body = BinaryData.FromObjectAsJson(emailRequest)
        };

        var messages = new[] { message }.ToAsyncEnumerable();

        Mocks.GetMock<IServiceBusReceiver>()
            .Setup(x => x.ReceiveMessagesAsync(It.IsAny<CancellationToken>()))
            .Returns(messages);

        Mocks.GetMock<IEmailService>()
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP server unavailable"));

        Mocks.GetMock<IServiceBusReceiver>()
            .Setup(x => x.AbandonMessageAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = Component.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        
        try { await executeTask; } catch (OperationCanceledException) { }

        // Assert
        Mocks.GetMock<IServiceBusReceiver>()
            .Verify(x => x.AbandonMessageAsync(message, It.IsAny<CancellationToken>()), 
                   Times.Once);

        // Should NOT complete the message
        Mocks.GetMock<IServiceBusReceiver>()
            .Verify(x => x.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()), 
                   Times.Never);

        // Verify error logged
        Mocks.VerifyLogger<EmailQueueWorker>(
            LogLevel.Error, 
            "Failed to process email message msg-456");
    }
}
```

## 🚀 Advanced Patterns

### Testing Scheduled Services

```csharp
public class DataSyncService : IHostedService
{
    private readonly Timer _timer;
    private readonly IDataSyncRepository _repository;
    private readonly IExternalApiClient _apiClient;
    private readonly ILogger<DataSyncService> _logger;

    public DataSyncService(
        IDataSyncRepository repository,
        IExternalApiClient apiClient,
        ILogger<DataSyncService> logger)
    {
        _repository = repository;
        _apiClient = apiClient;
        _logger = logger;
        _timer = new Timer(ExecuteSync, null, Timeout.Infinite, Timeout.Infinite);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Data sync service starting");
        _timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(30));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Data sync service stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void ExecuteSync(object? state)
    {
        try
        {
            _logger.LogInformation("Starting data synchronization");
            
            var lastSyncTime = await _repository.GetLastSyncTimeAsync();
            var updates = await _apiClient.GetUpdatesAsync(lastSyncTime);
            
            foreach (var update in updates)
            {
                await _repository.ApplyUpdateAsync(update);
            }
            
            await _repository.UpdateLastSyncTimeAsync(DateTime.UtcNow);
            
            _logger.LogInformation("Data synchronization completed. Applied {Count} updates", updates.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data synchronization failed");
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

public class DataSyncServiceTests : MockerTestBase<DataSyncService>
{
    [Fact]
    public async Task StartAsync_ShouldLogStartMessage()
    {
        // Act
        await Component.StartAsync(CancellationToken.None);

        // Assert
        Mocks.VerifyLogger<DataSyncService>(
            LogLevel.Information, 
            "Data sync service starting");
    }

    [Fact]  
    public async Task StopAsync_ShouldLogStopMessage()
    {
        // Act
        await Component.StopAsync(CancellationToken.None);

        // Assert
        Mocks.VerifyLogger<DataSyncService>(
            LogLevel.Information, 
            "Data sync service stopping");
    }

    [Fact]
    public async Task ExecuteSync_ShouldSyncData_WhenUpdatesAvailable()
    {
        // This test would require exposing the sync method or using integration testing
        // For true unit testing, consider refactoring to inject ITimer or similar
        
        // Arrange
        var lastSync = DateTime.UtcNow.AddHours(-1);
        var updates = new[] 
        { 
            new DataUpdate { Id = 1, Content = "Update 1" },
            new DataUpdate { Id = 2, Content = "Update 2" }
        };

        Mocks.GetMock<IDataSyncRepository>()
            .Setup(x => x.GetLastSyncTimeAsync())
            .ReturnsAsync(lastSync);

        Mocks.GetMock<IExternalApiClient>()
            .Setup(x => x.GetUpdatesAsync(lastSync))
            .ReturnsAsync(updates);

        Mocks.GetMock<IDataSyncRepository>()
            .Setup(x => x.ApplyUpdateAsync(It.IsAny<DataUpdate>()))
            .Returns(Task.CompletedTask);

        Mocks.GetMock<IDataSyncRepository>()
            .Setup(x => x.UpdateLastSyncTimeAsync(It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        // Act - Would need to trigger the timer or expose the sync method
        // await Component.ExecuteSyncAsync(); // If refactored to be testable

        // Assert - Verify the expected interactions would occur
        // This demonstrates the testing approach, but the service would need
        // architectural changes to be fully unit testable
    }
}
```

### Testing with Dependency Injection Integration

```csharp
public class HostedServiceIntegrationTests : MockerTestBase<IHost>
{
    public HostedServiceIntegrationTests() : base(ConfigureHost) { }

    private static void ConfigureHost(Mocker mocker)
    {
        var services = new ServiceCollection();
        
        // Add your hosted service
        services.AddHostedService<OrderProcessingService>();
        
        // Add mocked dependencies
        services.AddSingleton(mocker.GetObject<IOrderRepository>());
        services.AddSingleton(mocker.GetObject<IPaymentService>());
        services.AddSingleton(mocker.GetObject<IEmailService>());
        services.AddSingleton(mocker.GetObject<ILogger<OrderProcessingService>>());
        
        // Add configuration
        services.Configure<ProcessingSettings>(options =>
        {
            options.ProcessingIntervalMs = 100;
        });

        var host = new HostBuilder()
            .ConfigureServices((context, services) => services.AddRange(services))
            .Build();

        mocker.AddType(host);
    }

    [Fact]
    public async Task HostedService_ShouldStartAndProcessOrders()
    {
        // Arrange
        var orders = new[]
        {
            new Order { Id = 1, Status = OrderStatus.Pending }
        };

        Mocks.GetMock<IOrderRepository>()
            .Setup(x => x.GetPendingOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        Mocks.GetMock<IPaymentService>()
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult { Success = true });

        // Act
        using var cts = new CancellationTokenSource();
        await Component.StartAsync(cts.Token);
        
        await Task.Delay(250); // Let the service run
        
        cts.Cancel();
        await Component.StopAsync(CancellationToken.None);

        // Assert
        Mocks.GetMock<IPaymentService>()
            .Verify(x => x.ProcessPaymentAsync(It.IsAny<PaymentDetails>(), It.IsAny<CancellationToken>()), 
                   Times.AtLeastOnce);
    }
}
```

## ✅ Verification Patterns

### Service Lifecycle Verification
```csharp
// Verify service started
Mocks.VerifyLogger<MyService>(LogLevel.Information, "Service started");

// Verify graceful shutdown
Mocks.VerifyLogger<MyService>(LogLevel.Information, "Service stopped");

// Verify cancellation handling
Mocks.GetMock<IRepository>()
    .Verify(x => x.SaveAsync(It.IsAny<object>(), It.Is<CancellationToken>(ct => ct.IsCancellationRequested)), 
           Times.Never);
```

### Processing Verification
```csharp
// Verify batch processing
Mocks.GetMock<IProcessor>()
    .Verify(x => x.ProcessAsync(It.IsAny<object>()), Times.Exactly(expectedCount));

// Verify error handling continues processing
Mocks.GetMock<IProcessor>()
    .Verify(x => x.ProcessAsync(It.IsAny<object>()), Times.AtLeast(minExpectedCount));

// Verify retry behavior
Mocks.GetMock<IExternalService>()
    .Verify(x => x.CallAsync(It.IsAny<object>()), Times.Between(2, 5, Moq.Range.Inclusive));
```

## 💡 Tips & Tricks

### Testable Background Service Design
```csharp
// Instead of Timer-based services, consider:
public abstract class TestableBackgroundService : BackgroundService
{
    protected virtual TimeSpan DelayBetweenRuns => TimeSpan.FromMinutes(5);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DoWorkAsync(stoppingToken);
            await Task.Delay(DelayBetweenRuns, stoppingToken);
        }
    }
    
    protected abstract Task DoWorkAsync(CancellationToken cancellationToken);
}

// Then test DoWorkAsync directly:
[Fact]
public async Task DoWorkAsync_ShouldProcessData()
{
    await Component.DoWorkAsync(CancellationToken.None);
    // Verify behavior
}
```

### Mock Async Enumerable
```csharp
// For testing message receivers:
public static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        this IEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield(); // Allow other tasks to run
        }
    }
}
```

### Cancellation Testing Helpers
```csharp
protected async Task<T> RunWithTimeout<T>(Func<CancellationToken, Task<T>> operation, int timeoutMs = 1000)
{
    using var cts = new CancellationTokenSource(timeoutMs);
    return await operation(cts.Token);
}

protected async Task RunForDuration(Func<CancellationToken, Task> operation, int durationMs)
{
    using var cts = new CancellationTokenSource();
    var task = operation(cts.Token);
    await Task.Delay(durationMs);
    cts.Cancel();
    
    try
    {
        await task;
    }
    catch (OperationCanceledException)
    {
        // Expected
    }
}
```

---

**Next Recipe:** [Azure Service Bus Testing](azure-service-bus.md)
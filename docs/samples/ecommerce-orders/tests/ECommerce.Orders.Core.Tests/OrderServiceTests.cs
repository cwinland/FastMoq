using ECommerce.Orders.Core.Models;
using ECommerce.Orders.Core.Services;
using ECommerce.Orders.Core.Interfaces;
using FastMoq;
using FastMoq.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Orders.Core.Tests;

/// <summary>
/// Example test class demonstrating FastMoq usage in a real-world e-commerce scenario.
/// This showcases how FastMoq simplifies testing complex services with multiple dependencies.
/// </summary>
public class OrderServiceTests : MockerTestBase<OrderService>
{
    [Fact]
    public async Task CreateOrderAsync_ShouldCreateOrderSuccessfully_WithValidItems()
    {
        // Arrange
        var customerId = 123;
        var orderItems = new List<OrderItem>
        {
            new() { ProductId = 1, Quantity = 2, Price = 29.99m },
            new() { ProductId = 2, Quantity = 1, Price = 49.99m }
        };

        Mocks.GetMock<IInventoryService>()
            .Setup(x => x.CheckStockAsync(It.IsAny<List<(int ProductId, int Quantity)>>()))
            .ReturnsAsync(true);

        Mocks.GetMock<IOrderRepository>()
            .Setup(x => x.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order order) => 
            {
                order.Id = 1;
                return order;
            });

        // Act
        var result = await Component.CreateOrderAsync(customerId, orderItems);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.CustomerId.Should().Be(customerId);
        result.TotalAmount.Should().Be(109.97m);
        result.Status.Should().Be(OrderStatus.Pending);
        result.Items.Should().HaveCount(2);

        // Verify interactions
        Mocks.GetMock<IInventoryService>()
            .Verify(x => x.CheckStockAsync(It.IsAny<List<(int, int)>>()), Times.Once);
        
        Mocks.GetMock<IOrderRepository>()
            .Verify(x => x.CreateAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowException_WhenInsufficientStock()
    {
        // Arrange
        var customerId = 123;
        var orderItems = new List<OrderItem>
        {
            new() { ProductId = 1, Quantity = 10, Price = 29.99m }
        };

        Mocks.GetMock<IInventoryService>()
            .Setup(x => x.CheckStockAsync(It.IsAny<List<(int ProductId, int Quantity)>>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Component.CreateOrderAsync(customerId, orderItems));

        // Verify repository was never called
        Mocks.GetMock<IOrderRepository>()
            .Verify(x => x.CreateAsync(It.IsAny<Order>()), Times.Never);
    }
}

// Note: These interfaces and services would be implemented in the actual sample project
// This demonstrates the testing patterns without requiring the full implementation
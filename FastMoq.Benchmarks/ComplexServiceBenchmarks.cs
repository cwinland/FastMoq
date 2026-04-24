using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FastMoq.Benchmarks;

/// <summary>
/// Benchmarks a larger dependency graph to compare raw Moq setup against FastMoq provider-first construction and verification.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ComplexServiceBenchmarks : MoqProviderBenchmarkBase
{
    private static readonly ComplexOrderRequest Request = new(
        OrderId: "order-42",
        CustomerId: "cust-42",
        Sku: "SKU-RED-CHAIR",
        Quantity: 2,
        TotalAmount: 149.90m,
        ReceiptEmailAddress: "orders@example.test");

    private static readonly ComplexOrderOptions Options = new()
    {
        SendReceiptNotifications = true,
        AuditChannel = "benchmarks",
    };

    /// <summary>
    /// Measures direct Moq setup, subject construction, execution, and verification for a larger service graph.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<bool> DirectMoq()
    {
        var inventoryGateway = new Mock<IComplexInventoryGateway>();
        var paymentGateway = new Mock<IComplexPaymentGateway>();
        var shipmentGateway = new Mock<IShipmentGateway>();
        var orderRepository = new Mock<IComplexOrderRepository>();
        var receiptNotificationGateway = new Mock<IReceiptNotificationGateway>();
        var workflowAuditSink = new Mock<IWorkflowAuditSink>();
        var options = new Mock<IOptions<ComplexOrderOptions>>();
        var logger = new Mock<ILogger<ComplexOrderWorkflowService>>();

        inventoryGateway
            .Setup(x => x.ReserveAsync(Request.Sku, Request.Quantity, CancellationToken.None))
            .ReturnsAsync(true);
        paymentGateway
            .Setup(x => x.ChargeAsync(Request.CustomerId, Request.TotalAmount, CancellationToken.None))
            .ReturnsAsync("pay-12345");
        shipmentGateway
            .Setup(x => x.CreateShipmentAsync(Request.OrderId, Request.ReceiptEmailAddress, CancellationToken.None))
            .ReturnsAsync("ship-67890");
        orderRepository
            .Setup(x => x.SaveAsync(Request.OrderId, "pay-12345", "ship-67890", CancellationToken.None))
            .Returns(Task.CompletedTask);
        receiptNotificationGateway
            .Setup(x => x.SendReceiptAsync(Request.ReceiptEmailAddress, Request.OrderId, CancellationToken.None))
            .Returns(Task.CompletedTask);
        workflowAuditSink
            .Setup(x => x.WriteAsync("benchmarks:order-42", CancellationToken.None))
            .Returns(Task.CompletedTask);
        options
            .SetupGet(x => x.Value)
            .Returns(Options);

        var service = new ComplexOrderWorkflowService(
            inventoryGateway.Object,
            paymentGateway.Object,
            shipmentGateway.Object,
            orderRepository.Object,
            receiptNotificationGateway.Object,
            workflowAuditSink.Object,
            options.Object,
            logger.Object);

        var processed = await service.ProcessAsync(Request, CancellationToken.None).ConfigureAwait(false);

        inventoryGateway.Verify(x => x.ReserveAsync(Request.Sku, Request.Quantity, CancellationToken.None), Times.Once);
        paymentGateway.Verify(x => x.ChargeAsync(Request.CustomerId, Request.TotalAmount, CancellationToken.None), Times.Once);
        shipmentGateway.Verify(x => x.CreateShipmentAsync(Request.OrderId, Request.ReceiptEmailAddress, CancellationToken.None), Times.Once);
        orderRepository.Verify(x => x.SaveAsync(Request.OrderId, "pay-12345", "ship-67890", CancellationToken.None), Times.Once);
        receiptNotificationGateway.Verify(x => x.SendReceiptAsync(Request.ReceiptEmailAddress, Request.OrderId, CancellationToken.None), Times.Once);
        workflowAuditSink.Verify(x => x.WriteAsync("benchmarks:order-42", CancellationToken.None), Times.Once);
        return processed;
    }

    /// <summary>
    /// Measures the equivalent larger test flow using FastMoq tracked mocks and provider-neutral verification.
    /// </summary>
    [Benchmark]
    public async Task<bool> FastMoqProviderFirst()
    {
        using var mocker = new Mocker();

        mocker.GetOrCreateMock<IComplexInventoryGateway>()
            .Setup(x => x.ReserveAsync(Request.Sku, Request.Quantity, CancellationToken.None))
            .ReturnsAsync(true);
        mocker.GetOrCreateMock<IComplexPaymentGateway>()
            .Setup(x => x.ChargeAsync(Request.CustomerId, Request.TotalAmount, CancellationToken.None))
            .ReturnsAsync("pay-12345");
        mocker.GetOrCreateMock<IShipmentGateway>()
            .Setup(x => x.CreateShipmentAsync(Request.OrderId, Request.ReceiptEmailAddress, CancellationToken.None))
            .ReturnsAsync("ship-67890");
        mocker.GetOrCreateMock<IComplexOrderRepository>()
            .Setup(x => x.SaveAsync(Request.OrderId, "pay-12345", "ship-67890", CancellationToken.None))
            .Returns(Task.CompletedTask);
        mocker.GetOrCreateMock<IReceiptNotificationGateway>()
            .Setup(x => x.SendReceiptAsync(Request.ReceiptEmailAddress, Request.OrderId, CancellationToken.None))
            .Returns(Task.CompletedTask);
        mocker.GetOrCreateMock<IWorkflowAuditSink>()
            .Setup(x => x.WriteAsync("benchmarks:order-42", CancellationToken.None))
            .Returns(Task.CompletedTask);
        mocker.GetOrCreateMock<IOptions<ComplexOrderOptions>>()
            .SetupGet(x => x.Value)
            .Returns(Options);

        var service = mocker.CreateInstance<ComplexOrderWorkflowService>()
            ?? throw new InvalidOperationException("FastMoq did not create ComplexOrderWorkflowService for the benchmark.");
        var processed = await service.ProcessAsync(Request, CancellationToken.None).ConfigureAwait(false);

        mocker.Verify<IComplexInventoryGateway>(x => x.ReserveAsync(Request.Sku, Request.Quantity, CancellationToken.None), TimesSpec.Once);
        mocker.Verify<IComplexPaymentGateway>(x => x.ChargeAsync(Request.CustomerId, Request.TotalAmount, CancellationToken.None), TimesSpec.Once);
        mocker.Verify<IShipmentGateway>(x => x.CreateShipmentAsync(Request.OrderId, Request.ReceiptEmailAddress, CancellationToken.None), TimesSpec.Once);
        mocker.Verify<IComplexOrderRepository>(x => x.SaveAsync(Request.OrderId, "pay-12345", "ship-67890", CancellationToken.None), TimesSpec.Once);
        mocker.Verify<IReceiptNotificationGateway>(x => x.SendReceiptAsync(Request.ReceiptEmailAddress, Request.OrderId, CancellationToken.None), TimesSpec.Once);
        mocker.Verify<IWorkflowAuditSink>(x => x.WriteAsync("benchmarks:order-42", CancellationToken.None), TimesSpec.Once);
        return processed;
    }
}
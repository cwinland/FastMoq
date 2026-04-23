using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FastMoq.Benchmarks;

/// <summary>
/// Measures the steady-state cost of invoking an already constructed larger service graph.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[ShortRunJob(RuntimeMoniker.Net80)]
public class ComplexInvocationOnlyBenchmarks
{
    private static readonly ComplexOrderRequest Request = new(
        OrderId: "order-42",
        CustomerId: "cust-42",
        Sku: "SKU-RED-CHAIR",
        Quantity: 2,
        TotalAmount: 149.90m,
        ReceiptEmailAddress: "orders@example.test");

    private static readonly ComplexOrderOptions ScenarioOptions = new()
    {
        SendReceiptNotifications = true,
        AuditChannel = "benchmarks",
    };

    private IDisposable? _providerScope;
    private Mocker? _fastMoqMocker;
    private ComplexOrderWorkflowService? _directService;
    private ComplexOrderWorkflowService? _fastMoqService;

    /// <summary>
    /// Number of business invocations performed per benchmark operation.
    /// </summary>
    [Params(1, 10, 100)]
    public int InvocationCount { get; set; }

    /// <summary>
    /// Builds the direct Moq and FastMoq-backed services once so only invocation cost is measured.
    /// </summary>
    [GlobalSetup]
    public void SetupServices()
    {
        _providerScope = MockingProviderRegistry.Push("moq");
        _directService = CreateDirectService();
        _fastMoqService = CreateFastMoqService(out var mocker);
        _fastMoqMocker = mocker;
    }

    /// <summary>
    /// Disposes the shared FastMoq state after the benchmark run.
    /// </summary>
    [GlobalCleanup]
    public void CleanupServices()
    {
        _fastMoqMocker?.Dispose();
        _fastMoqMocker = null;
        _providerScope?.Dispose();
        _providerScope = null;
    }

    /// <summary>
    /// Measures repeated business invocations on a prebuilt direct Moq service.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<int> DirectMoqInvokeOnly()
    {
        var successes = 0;
        for (var i = 0; i < InvocationCount; i++)
        {
            if (await _directService!.ProcessAsync(Request, CancellationToken.None).ConfigureAwait(false))
            {
                successes++;
            }
        }

        return successes;
    }

    /// <summary>
    /// Measures repeated business invocations on a prebuilt FastMoq-backed service.
    /// </summary>
    [Benchmark]
    public async Task<int> FastMoqInvokeOnly()
    {
        var successes = 0;
        for (var i = 0; i < InvocationCount; i++)
        {
            if (await _fastMoqService!.ProcessAsync(Request, CancellationToken.None).ConfigureAwait(false))
            {
                successes++;
            }
        }

        return successes;
    }

    private static ComplexOrderWorkflowService CreateDirectService()
    {
        var inventoryGateway = new Mock<IComplexInventoryGateway>();
        var paymentGateway = new Mock<IComplexPaymentGateway>();
        var shipmentGateway = new Mock<IShipmentGateway>();
        var orderRepository = new Mock<IComplexOrderRepository>();
        var receiptNotificationGateway = new Mock<IReceiptNotificationGateway>();
        var workflowAuditSink = new Mock<IWorkflowAuditSink>();

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

        return new ComplexOrderWorkflowService(
            inventoryGateway.Object,
            paymentGateway.Object,
            shipmentGateway.Object,
            orderRepository.Object,
            receiptNotificationGateway.Object,
            workflowAuditSink.Object,
            Options.Create(ScenarioOptions),
            NullLogger<ComplexOrderWorkflowService>.Instance);
    }

    private static ComplexOrderWorkflowService CreateFastMoqService(out Mocker mocker)
    {
        mocker = new Mocker()
            .AddType<IOptions<ComplexOrderOptions>>(Options.Create(ScenarioOptions))
            .AddType<Microsoft.Extensions.Logging.ILogger<ComplexOrderWorkflowService>>(NullLogger<ComplexOrderWorkflowService>.Instance);

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

        return mocker.CreateInstance<ComplexOrderWorkflowService>()
            ?? throw new InvalidOperationException("FastMoq did not create ComplexOrderWorkflowService for the invocation-only benchmark.");
    }
}
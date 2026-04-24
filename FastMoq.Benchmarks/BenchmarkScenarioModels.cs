using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FastMoq.Benchmarks;

internal sealed record RegistrationCommand(string EmailAddress, string DisplayName);

internal interface IUserDirectory
{
    Task SaveAsync(RegistrationCommand command, CancellationToken cancellationToken = default);
}

internal interface IWelcomeMessageGateway
{
    Task SendWelcomeAsync(string emailAddress, CancellationToken cancellationToken = default);
}

internal interface IRegistrationAuditSink
{
    Task WriteAsync(string emailAddress, CancellationToken cancellationToken = default);
}

internal sealed class UserRegistrationService
{
    private readonly IUserDirectory _userDirectory;
    private readonly IWelcomeMessageGateway _welcomeMessageGateway;
    private readonly IRegistrationAuditSink _registrationAuditSink;
    private readonly ILogger<UserRegistrationService> _logger;

    public UserRegistrationService(
        IUserDirectory userDirectory,
        IWelcomeMessageGateway welcomeMessageGateway,
        IRegistrationAuditSink registrationAuditSink,
        ILogger<UserRegistrationService> logger)
    {
        _userDirectory = userDirectory;
        _welcomeMessageGateway = welcomeMessageGateway;
        _registrationAuditSink = registrationAuditSink;
        _logger = logger;
    }

    public async Task<bool> RegisterAsync(RegistrationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _userDirectory.SaveAsync(command, cancellationToken).ConfigureAwait(false);
        await _welcomeMessageGateway.SendWelcomeAsync(command.EmailAddress, cancellationToken).ConfigureAwait(false);
        await _registrationAuditSink.WriteAsync(command.EmailAddress, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Registered user {EmailAddress}", command.EmailAddress);
        return true;
    }
}

internal sealed record ComplexOrderRequest(
    string OrderId,
    string CustomerId,
    string Sku,
    int Quantity,
    decimal TotalAmount,
    string ReceiptEmailAddress);

internal sealed class ComplexOrderOptions
{
    public bool SendReceiptNotifications { get; init; }

    public string AuditChannel { get; init; } = string.Empty;
}

internal interface IComplexInventoryGateway
{
    Task<bool> ReserveAsync(string sku, int quantity, CancellationToken cancellationToken = default);
}

internal interface IComplexPaymentGateway
{
    Task<string> ChargeAsync(string customerId, decimal totalAmount, CancellationToken cancellationToken = default);
}

internal interface IShipmentGateway
{
    Task<string> CreateShipmentAsync(string orderId, string receiptEmailAddress, CancellationToken cancellationToken = default);
}

internal interface IComplexOrderRepository
{
    Task SaveAsync(string orderId, string paymentReference, string shipmentId, CancellationToken cancellationToken = default);
}

internal interface IReceiptNotificationGateway
{
    Task SendReceiptAsync(string receiptEmailAddress, string orderId, CancellationToken cancellationToken = default);
}

internal interface IWorkflowAuditSink
{
    Task WriteAsync(string payload, CancellationToken cancellationToken = default);
}

internal sealed class ComplexOrderWorkflowService
{
    private readonly IComplexInventoryGateway _inventoryGateway;
    private readonly IComplexPaymentGateway _paymentGateway;
    private readonly IShipmentGateway _shipmentGateway;
    private readonly IComplexOrderRepository _orderRepository;
    private readonly IReceiptNotificationGateway _receiptNotificationGateway;
    private readonly IWorkflowAuditSink _workflowAuditSink;
    private readonly IOptions<ComplexOrderOptions> _options;
    private readonly ILogger<ComplexOrderWorkflowService> _logger;

    public ComplexOrderWorkflowService(
        IComplexInventoryGateway inventoryGateway,
        IComplexPaymentGateway paymentGateway,
        IShipmentGateway shipmentGateway,
        IComplexOrderRepository orderRepository,
        IReceiptNotificationGateway receiptNotificationGateway,
        IWorkflowAuditSink workflowAuditSink,
        IOptions<ComplexOrderOptions> options,
        ILogger<ComplexOrderWorkflowService> logger)
    {
        _inventoryGateway = inventoryGateway;
        _paymentGateway = paymentGateway;
        _shipmentGateway = shipmentGateway;
        _orderRepository = orderRepository;
        _receiptNotificationGateway = receiptNotificationGateway;
        _workflowAuditSink = workflowAuditSink;
        _options = options;
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(ComplexOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = _options.Value;
        var reserved = await _inventoryGateway.ReserveAsync(request.Sku, request.Quantity, cancellationToken).ConfigureAwait(false);
        if (!reserved)
        {
            _logger.LogWarning("Inventory reservation failed for {Sku}", request.Sku);
            return false;
        }

        var paymentReference = await _paymentGateway.ChargeAsync(request.CustomerId, request.TotalAmount, cancellationToken).ConfigureAwait(false);
        var shipmentId = await _shipmentGateway.CreateShipmentAsync(request.OrderId, request.ReceiptEmailAddress, cancellationToken).ConfigureAwait(false);

        await _orderRepository.SaveAsync(request.OrderId, paymentReference, shipmentId, cancellationToken).ConfigureAwait(false);

        if (options.SendReceiptNotifications)
        {
            await _receiptNotificationGateway.SendReceiptAsync(request.ReceiptEmailAddress, request.OrderId, cancellationToken).ConfigureAwait(false);
        }

        await _workflowAuditSink.WriteAsync($"{options.AuditChannel}:{request.OrderId}", cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Processed order {OrderId}", request.OrderId);
        return true;
    }
}

internal interface IAuditChannel
{
    Task WriteAsync(string payload, CancellationToken cancellationToken = default);
}

internal sealed class DualAuditForwarder
{
    private readonly IAuditChannel _primary;
    private readonly IAuditChannel _secondary;

    public DualAuditForwarder(IAuditChannel primary, IAuditChannel secondary)
    {
        _primary = primary;
        _secondary = secondary;
    }

    public async Task DispatchAsync(string payload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        await _primary.WriteAsync($"{payload}:primary", cancellationToken).ConfigureAwait(false);
        await _secondary.WriteAsync($"{payload}:secondary", cancellationToken).ConfigureAwait(false);
    }
}

internal interface IProviderMatrixPrimarySink
{
    void Publish(string payload);
}

internal interface IProviderMatrixSecondarySink
{
    void Publish(string payload);
}

internal sealed class ProviderMatrixInteractionService
{
    private readonly IProviderMatrixPrimarySink _primarySink;
    private readonly IProviderMatrixSecondarySink _secondarySink;

    public ProviderMatrixInteractionService(IProviderMatrixPrimarySink primarySink, IProviderMatrixSecondarySink secondarySink)
    {
        _primarySink = primarySink;
        _secondarySink = secondarySink;
    }

    public bool Run()
    {
        _primarySink.Publish("alpha");
        _secondarySink.Publish("beta");
        return true;
    }
}

internal interface ILookupProbe<TMarker>
{
}

internal sealed class LookupMarker01;
internal sealed class LookupMarker02;
internal sealed class LookupMarker03;
internal sealed class LookupMarker04;
internal sealed class LookupMarker05;
internal sealed class LookupMarker06;
internal sealed class LookupMarker07;
internal sealed class LookupMarker08;
internal sealed class LookupMarker09;
internal sealed class LookupMarker10;
internal sealed class LookupMarker11;
internal sealed class LookupMarker12;
internal sealed class LookupMarker13;
internal sealed class LookupMarker14;
internal sealed class LookupMarker15;
internal sealed class LookupMarker16;
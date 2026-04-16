using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace FastMoq.TestingExample
{
    public sealed class OrderRequest
    {
        public required string CustomerId { get; init; }
        public required string Sku { get; init; }
        public required int Quantity { get; init; }
        public required decimal TotalAmount { get; init; }
    }

    public sealed class OrderRecord
    {
        public required Guid OrderId { get; init; }
        public required string CustomerId { get; init; }
        public required string Sku { get; init; }
        public required int Quantity { get; init; }
        public required decimal TotalAmount { get; init; }
        public required string PaymentReference { get; init; }
    }

    public sealed class OrderPlacementResult
    {
        public bool Success { get; init; }
        public Guid? OrderId { get; init; }
        public string? FailureReason { get; init; }

        public static OrderPlacementResult Placed(Guid orderId) => new()
        {
            Success = true,
            OrderId = orderId,
        };

        public static OrderPlacementResult Failed(string reason) => new()
        {
            Success = false,
            FailureReason = reason,
        };
    }

    public interface IInventoryGateway
    {
        Task<bool> ReserveAsync(string sku, int quantity, CancellationToken cancellationToken = default);
    }

    public interface IPaymentGateway
    {
        Task<string> ChargeAsync(string customerId, decimal amount, CancellationToken cancellationToken = default);
    }

    public interface IOrderRepository
    {
        Task SaveAsync(OrderRecord order, CancellationToken cancellationToken = default);
    }

    public interface IOrderSubmissionChannel
    {
        string? Mode { get; set; }

        Task SubmitAsync(string orderId, CancellationToken cancellationToken = default);
    }

    public sealed class OrderProcessingService
    {
        private readonly IInventoryGateway _inventoryGateway;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrderProcessingService> _logger;

        public OrderProcessingService(
            IInventoryGateway inventoryGateway,
            IPaymentGateway paymentGateway,
            IOrderRepository orderRepository,
            ILogger<OrderProcessingService> logger)
        {
            _inventoryGateway = inventoryGateway;
            _paymentGateway = paymentGateway;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task<OrderPlacementResult> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var reserved = await _inventoryGateway.ReserveAsync(request.Sku, request.Quantity, cancellationToken).ConfigureAwait(false);
            if (!reserved)
            {
                _logger.LogWarning("Inventory reservation failed for {Sku}", request.Sku);
                return OrderPlacementResult.Failed("InventoryUnavailable");
            }

            var paymentReference = await _paymentGateway.ChargeAsync(request.CustomerId, request.TotalAmount, cancellationToken).ConfigureAwait(false);
            var order = new OrderRecord
            {
                OrderId = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                Sku = request.Sku,
                Quantity = request.Quantity,
                TotalAmount = request.TotalAmount,
                PaymentReference = paymentReference,
            };

            await _orderRepository.SaveAsync(order, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Placed order {OrderId} for {CustomerId}", order.OrderId, order.CustomerId);
            return OrderPlacementResult.Placed(order.OrderId);
        }
    }

    public sealed class OrderSubmissionService
    {
        private readonly IOrderSubmissionChannel _submissionChannel;

        public OrderSubmissionService(IOrderSubmissionChannel submissionChannel)
        {
            _submissionChannel = submissionChannel;
        }

        public async Task SubmitAsync(string orderId, bool expedited, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(orderId);

            _submissionChannel.Mode = expedited ? "fast" : "standard";
            await _submissionChannel.SubmitAsync(orderId, cancellationToken).ConfigureAwait(false);
        }
    }

    public sealed class CustomerImportRow
    {
        public required string CustomerId { get; init; }
        public required string EmailAddress { get; init; }
        public required string Segment { get; init; }
    }

    public interface ICustomerCsvParser
    {
        IReadOnlyList<CustomerImportRow> Parse(string csv);
    }

    public interface ICustomerRepository
    {
        Task UpsertAsync(IReadOnlyList<CustomerImportRow> customers, CancellationToken cancellationToken = default);
    }

    public sealed class CustomerImportService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ICustomerCsvParser _parser;
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<CustomerImportService> _logger;

        public CustomerImportService(
            IFileSystem fileSystem,
            ICustomerCsvParser parser,
            ICustomerRepository customerRepository,
            ILogger<CustomerImportService> logger)
        {
            _fileSystem = fileSystem;
            _parser = parser;
            _customerRepository = customerRepository;
            _logger = logger;
        }

        public async Task<int> ImportAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            if (!_fileSystem.File.Exists(filePath))
            {
                _logger.LogWarning("Import file was not found: {Path}", filePath);
                return 0;
            }

            var csv = _fileSystem.File.ReadAllText(filePath);
            var rows = _parser.Parse(csv);
            await _customerRepository.UpsertAsync(rows, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Imported {Count} customers from {Path}", rows.Count, filePath);
            return rows.Count;
        }
    }

    public sealed class PastDueInvoice
    {
        public required string InvoiceNumber { get; init; }
        public required string RecipientEmail { get; init; }
        public required decimal AmountDue { get; init; }
    }

    public interface IInvoiceRepository
    {
        Task<IReadOnlyList<PastDueInvoice>> GetPastDueAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    }

    public interface IEmailGateway
    {
        Task SendReminderAsync(string emailAddress, decimal amountDue, CancellationToken cancellationToken = default);
    }

    public sealed class InvoiceReminderService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IEmailGateway _emailGateway;
        private readonly ILogger<InvoiceReminderService> _logger;

        public InvoiceReminderService(
            IInvoiceRepository invoiceRepository,
            IEmailGateway emailGateway,
            ILogger<InvoiceReminderService> logger)
        {
            _invoiceRepository = invoiceRepository;
            _emailGateway = emailGateway;
            _logger = logger;
        }

        public async Task<int> SendRemindersAsync(DateTime utcNow, CancellationToken cancellationToken = default)
        {
            var invoices = await _invoiceRepository.GetPastDueAsync(utcNow, cancellationToken).ConfigureAwait(false);
            foreach (var invoice in invoices)
            {
                await _emailGateway.SendReminderAsync(invoice.RecipientEmail, invoice.AmountDue, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Sent {Count} invoice reminders", invoices.Count);
            return invoices.Count;
        }
    }

    public sealed class OptionalDependencyReportService
    {
        public OptionalDependencyReportService(
            ILogger<OptionalDependencyReportService>? logger = null,
            IFileSystem? fileSystem = null)
        {
            Logger = logger;
            FileSystem = fileSystem;
        }

        public ILogger<OptionalDependencyReportService>? Logger { get; }

        public IFileSystem? FileSystem { get; }
    }

    public sealed class OptionalDependencyProbe
    {
        public OptionalDependencyProbe(ILogger? logger, IFileSystem? fileSystem)
        {
            Logger = logger;
            FileSystem = fileSystem;
        }

        public ILogger? Logger { get; }

        public IFileSystem? FileSystem { get; }
    }

    public sealed class OptionalDependencyProbeFactory
    {
        public OptionalDependencyProbe Create(ILogger? logger = null, IFileSystem? fileSystem = null)
        {
            return new OptionalDependencyProbe(logger, fileSystem);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;
using FastMoq.Extensions;
using FastMoq.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FastMoq.TestingExample
{
    public class OrderProcessingServiceExamples : MockerTestBase<OrderProcessingService>
    {
        [Fact]
        public async Task PlaceOrderAsync_ShouldPersistAndLog_WhenReservationAndPaymentSucceed()
        {
            var request = new OrderRequest
            {
                CustomerId = "cust-42",
                Sku = "SKU-RED-CHAIR",
                Quantity = 2,
                TotalAmount = 149.90m,
            };

            Mocks.GetMock<IInventoryGateway>()
                .Setup(x => x.ReserveAsync(request.Sku, request.Quantity, CancellationToken.None))
                .ReturnsAsync(true);
            Mocks.GetMock<IPaymentGateway>()
                .Setup(x => x.ChargeAsync(request.CustomerId, request.TotalAmount, CancellationToken.None))
                .ReturnsAsync("pay_12345");

            var result = await Component.PlaceOrderAsync(request, CancellationToken.None);

            result.Success.Should().BeTrue();
            result.OrderId.Should().NotBeNull();
            Mocks.GetMock<IOrderRepository>()
                .Verify(x => x.SaveAsync(
                    It.Is<OrderRecord>(order =>
                        order.CustomerId == request.CustomerId &&
                        order.Sku == request.Sku &&
                        order.Quantity == request.Quantity &&
                        order.TotalAmount == request.TotalAmount &&
                        order.PaymentReference == "pay_12345"),
                    CancellationToken.None),
                    Times.Once);
            Mocks.GetMock<ILogger<OrderProcessingService>>()
                .VerifyLogger(LogLevel.Information, "Placed order", 1);
        }

        [Fact]
        public async Task PlaceOrderAsync_ShouldShortCircuit_WhenInventoryReservationFails()
        {
            var request = new OrderRequest
            {
                CustomerId = "cust-99",
                Sku = "SKU-NO-STOCK",
                Quantity = 1,
                TotalAmount = 24.99m,
            };

            Mocks.GetMock<IInventoryGateway>()
                .Setup(x => x.ReserveAsync(request.Sku, request.Quantity, CancellationToken.None))
                .ReturnsAsync(false);

            var result = await Component.PlaceOrderAsync(request, CancellationToken.None);

            result.Success.Should().BeFalse();
            result.FailureReason.Should().Be("InventoryUnavailable");
            Mocks.GetMock<IPaymentGateway>()
                .Verify(x => x.ChargeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
            Mocks.GetMock<IOrderRepository>()
                .Verify(x => x.SaveAsync(It.IsAny<OrderRecord>(), It.IsAny<CancellationToken>()), Times.Never);
            Mocks.GetMock<ILogger<OrderProcessingService>>()
                .VerifyLogger(LogLevel.Warning, "Inventory reservation failed", 1);
        }
    }

    public class CustomerImportServiceExamples : MockerTestBase<CustomerImportService>
    {
        [Fact]
        public async Task ImportAsync_ShouldUseBuiltInFileSystemAndPersistParsedRows()
        {
            const string filePath = @"c:\imports\customers.csv";
            const string csv = "customerId,email,segment\nC-100,ada@example.test,VIP\nC-200,grace@example.test,Standard";
            var parsedRows = new List<CustomerImportRow>
            {
                new() { CustomerId = "C-100", EmailAddress = "ada@example.test", Segment = "VIP" },
                new() { CustomerId = "C-200", EmailAddress = "grace@example.test", Segment = "Standard" },
            };

            Mocks.fileSystem.AddFile(filePath, new MockFileData(csv));
            Mocks.GetMock<ICustomerCsvParser>()
                .Setup(x => x.Parse(csv))
                .Returns(parsedRows);

            var importedCount = await Component.ImportAsync(filePath, CancellationToken.None);

            importedCount.Should().Be(2);
            Mocks.GetMock<ICustomerRepository>()
                .Verify(x => x.UpsertAsync(parsedRows, CancellationToken.None), Times.Once);
            Mocks.GetMock<ILogger<CustomerImportService>>()
                .VerifyLogger(LogLevel.Information, "Imported 2 customers", 1);
        }

        [Fact]
        public async Task ImportAsync_ShouldReturnZeroAndLogWarning_WhenFileDoesNotExist()
        {
            const string filePath = @"c:\imports\missing.csv";

            var importedCount = await Component.ImportAsync(filePath, CancellationToken.None);

            importedCount.Should().Be(0);
            Mocks.GetMock<ICustomerCsvParser>()
                .Verify(x => x.Parse(It.IsAny<string>()), Times.Never);
            Mocks.GetMock<ICustomerRepository>()
                .Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyList<CustomerImportRow>>(), It.IsAny<CancellationToken>()), Times.Never);
            Mocks.GetMock<ILogger<CustomerImportService>>()
                .VerifyLogger(LogLevel.Warning, "Import file was not found", 1);
        }
    }

    public class InvoiceReminderServiceScenarioExamples : MockerTestBase<InvoiceReminderService>
    {
        [Fact]
        public void SendRemindersAsync_ShouldSupportFluentScenarioStyle()
        {
            var now = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
            var invoices = new List<PastDueInvoice>
            {
                new() { InvoiceNumber = "INV-1001", RecipientEmail = "ap@contoso.test", AmountDue = 125m },
                new() { InvoiceNumber = "INV-1002", RecipientEmail = "finance@fabrikam.test", AmountDue = 310m },
            };
            var reminderCount = 0;

            Mocks.Scenario(Component)
                .With((mocks, service) =>
                {
                    mocks.GetMock<IInvoiceRepository>()
                        .Setup(x => x.GetPastDueAsync(now, CancellationToken.None))
                        .ReturnsAsync(invoices);
                })
                .When(async (mocks, service) => reminderCount = await service.SendRemindersAsync(now, CancellationToken.None))
                .Then((mocks, service) => reminderCount.Should().Be(2))
                .Verify<IInvoiceRepository>(x => x.GetPastDueAsync(now, CancellationToken.None), TimesSpec.Once)
                .Verify<IEmailGateway>(x => x.SendReminderAsync("ap@contoso.test", 125m, CancellationToken.None), TimesSpec.Once)
                .Verify<IEmailGateway>(x => x.SendReminderAsync("finance@fabrikam.test", 310m, CancellationToken.None), TimesSpec.Once)
                .Execute();

            Mocks.GetMock<ILogger<InvoiceReminderService>>()
                .VerifyLogger(LogLevel.Information, "Sent 2 invoice reminders", 1);
        }
    }
}
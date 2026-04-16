using FastMoq.Extensions;
using FastMoq.Providers;
using FastMoq.Providers.MoqProvider;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;
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

            var inventoryGateway = Mocks.GetOrCreateMock<IInventoryGateway>();
            var paymentGateway = Mocks.GetOrCreateMock<IPaymentGateway>();
            var orderRepository = Mocks.GetOrCreateMock<IOrderRepository>();

            inventoryGateway
                .Setup(x => x.ReserveAsync(request.Sku, request.Quantity, CancellationToken.None))
                .ReturnsAsync(true);
            paymentGateway
                .Setup(x => x.ChargeAsync(request.CustomerId, request.TotalAmount, CancellationToken.None))
                .ReturnsAsync("pay_12345");

            var result = await Component.PlaceOrderAsync(request, CancellationToken.None);

            result.Success.Should().BeTrue();
            result.OrderId.Should().NotBeNull();
            orderRepository.AsMoq()
                .Verify(x => x.SaveAsync(
                    It.Is<OrderRecord>(order =>
                        order.CustomerId == request.CustomerId &&
                        order.Sku == request.Sku &&
                        order.Quantity == request.Quantity &&
                        order.TotalAmount == request.TotalAmount &&
                        order.PaymentReference == "pay_12345"),
                    CancellationToken.None),
                    Times.Once);
            Mocks.VerifyLogged(LogLevel.Information, "Placed order", 1);
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

            var inventoryGateway = Mocks.GetOrCreateMock<IInventoryGateway>();
            var paymentGateway = Mocks.GetOrCreateMock<IPaymentGateway>();
            var orderRepository = Mocks.GetOrCreateMock<IOrderRepository>();

            inventoryGateway
                .Setup(x => x.ReserveAsync(request.Sku, request.Quantity, CancellationToken.None))
                .ReturnsAsync(false);

            var result = await Component.PlaceOrderAsync(request, CancellationToken.None);

            result.Success.Should().BeFalse();
            result.FailureReason.Should().Be("InventoryUnavailable");
            paymentGateway.AsMoq()
                .Verify(x => x.ChargeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
            orderRepository.AsMoq()
                .Verify(x => x.SaveAsync(It.IsAny<OrderRecord>(), It.IsAny<CancellationToken>()), Times.Never);
            Mocks.VerifyLogged(LogLevel.Warning, "Inventory reservation failed", 1);
        }
    }

    public class OrderSubmissionServiceExamples : MockerTestBase<OrderSubmissionService>
    {
        [Fact]
        public async Task SubmitAsync_ShouldPreserveAssignedMode_WithAddPropertyState()
        {
            var submissionChannel = Mocks.GetOrCreateMock<IOrderSubmissionChannel>();
            submissionChannel
                .Setup(x => x.SubmitAsync("order-42", CancellationToken.None))
                .Returns(Task.CompletedTask);

            var channel = Mocks.AddPropertyState<IOrderSubmissionChannel>();
            CreateComponent();

            await Component.SubmitAsync("order-42", expedited: true, CancellationToken.None);

            channel.Mode.Should().Be("fast");
            Mocks.Verify<IOrderSubmissionChannel>(x => x.SubmitAsync("order-42", CancellationToken.None), TimesSpec.Once);
        }

        [Fact]
        public async Task SubmitAsync_ShouldCaptureAssignedMode_WithAddPropertySetterCapture()
        {
            var submissionChannel = Mocks.GetOrCreateMock<IOrderSubmissionChannel>();
            submissionChannel
                .Setup(x => x.SubmitAsync("order-42", CancellationToken.None))
                .Returns(Task.CompletedTask);

            var modeCapture = Mocks.AddPropertySetterCapture<IOrderSubmissionChannel, string?>(x => x.Mode);
            CreateComponent();

            await Component.SubmitAsync("order-42", expedited: true, CancellationToken.None);

            modeCapture.Value.Should().Be("fast");
            Mocks.Verify<IOrderSubmissionChannel>(x => x.SubmitAsync("order-42", CancellationToken.None), TimesSpec.Once);
        }
    }

    public class WidgetScopeRunnerExamples : MockerTestBase<WidgetScopeRunner>
    {
        [Fact]
        public void RunScope_ShouldResolveScopedGraph_WithTypedServiceProviderHelper()
        {
            Mocks.AddServiceProvider(services => services.AddScoped<ScopedWidgetContext>(), replace: true, includeMockerFallback: true);
            CreateComponent();

            var first = Component.RunScope();
            var second = Component.RunScope();

            first.Should().NotBe(Guid.Empty);
            second.Should().NotBe(Guid.Empty);
            first.Should().NotBe(second);
        }
    }

    public class CustomerImportServiceExamples : MockerTestBase<CustomerImportService>
    {
        [Fact]
        public async Task ImportAsync_ShouldUseBuiltInFileSystemAndPersistParsedRows()
        {
            const string FILE_PATH = @"c:\imports\customers.csv";
            const string CSV = "customerId,email,segment\nC-100,ada@example.test,VIP\nC-200,grace@example.test,Standard";
            var parsedRows = new List<CustomerImportRow>
            {
                new() { CustomerId = "C-100", EmailAddress = "ada@example.test", Segment = "VIP" },
                new() { CustomerId = "C-200", EmailAddress = "grace@example.test", Segment = "Standard" },
            };

            var customerCsvParser = Mocks.GetOrCreateMock<ICustomerCsvParser>();
            var customerRepository = Mocks.GetOrCreateMock<ICustomerRepository>();

            Mocks.fileSystem.AddFile(FILE_PATH, new MockFileData(CSV));
            customerCsvParser
                .Setup(x => x.Parse(CSV))
                .Returns(parsedRows);

            var importedCount = await Component.ImportAsync(FILE_PATH, CancellationToken.None);

            importedCount.Should().Be(2);
            customerRepository.AsMoq()
                .Verify(x => x.UpsertAsync(parsedRows, CancellationToken.None), Times.Once);
            Mocks.VerifyLogged(LogLevel.Information, "Imported 2 customers", 1);
        }

        [Fact]
        public async Task ImportAsync_ShouldReturnZeroAndLogWarning_WhenFileDoesNotExist()
        {
            const string FILE_PATH = @"c:\imports\missing.csv";
            var customerCsvParser = Mocks.GetOrCreateMock<ICustomerCsvParser>();
            var customerRepository = Mocks.GetOrCreateMock<ICustomerRepository>();

            var importedCount = await Component.ImportAsync(FILE_PATH, CancellationToken.None);

            importedCount.Should().Be(0);
            customerCsvParser.AsMoq()
                .Verify(x => x.Parse(It.IsAny<string>()), Times.Never);
            customerRepository.AsMoq()
                .Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyList<CustomerImportRow>>(), It.IsAny<CancellationToken>()), Times.Never);
            Mocks.VerifyLogged(LogLevel.Warning, "Import file was not found", 1);
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

            Scenario
                .With(() =>
                {
                    Mocks.GetOrCreateMock<IInvoiceRepository>()
                        .Setup(x => x.GetPastDueAsync(now, CancellationToken.None))
                        .ReturnsAsync(invoices);
                })
                .When(async () => reminderCount = await Component.SendRemindersAsync(now, CancellationToken.None))
                .Then(() => reminderCount.Should().Be(2))
                .Verify<IInvoiceRepository>(x => x.GetPastDueAsync(now, CancellationToken.None), TimesSpec.Once)
                .Verify<IEmailGateway>(x => x.SendReminderAsync("ap@contoso.test", 125m, CancellationToken.None), TimesSpec.Once)
                .Verify<IEmailGateway>(x => x.SendReminderAsync("finance@fabrikam.test", 310m, CancellationToken.None), TimesSpec.Once)
                .Execute();

            Mocks.VerifyLogged(LogLevel.Information, "Sent 2 invoice reminders", 1);
        }

        [Fact]
        public void SendRemindersAsync_ShouldSurfaceDependencyException_InFluentScenario()
        {
            var now = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
            var invoices = new List<PastDueInvoice>
            {
                new() { InvoiceNumber = "INV-ERR-1001", RecipientEmail = "ap@contoso.test", AmountDue = 125m },
            };
            var afterFailureAssertionRan = false;

            Scenario
                .With(() =>
                {
                    Mocks.GetOrCreateMock<IInvoiceRepository>()
                        .Setup(x => x.GetPastDueAsync(now, CancellationToken.None))
                        .ReturnsAsync(invoices);
                    Mocks.GetOrCreateMock<IEmailGateway>()
                        .Setup(x => x.SendReminderAsync("ap@contoso.test", 125m, CancellationToken.None))
                        .ThrowsAsync(new InvalidOperationException("SMTP unavailable"));
                })
                .WhenThrows<InvalidOperationException>(() => Component.SendRemindersAsync(now, CancellationToken.None))
                .Then(() => afterFailureAssertionRan = true)
                .Execute();

            afterFailureAssertionRan.Should().BeTrue();

            Mocks.GetOrCreateMock<IInvoiceRepository>().AsMoq()
                .Verify(x => x.GetPastDueAsync(now, CancellationToken.None), Times.Once);
            Mocks.GetOrCreateMock<IEmailGateway>().AsMoq()
                .Verify(x => x.SendReminderAsync("ap@contoso.test", 125m, CancellationToken.None), Times.Once);
            Mocks.VerifyLogged(LogLevel.Information, "Sent 1 invoice reminders", 0);
        }
    }

    public class OptionalParameterResolutionExamples : MockerTestBase<OptionalDependencyReportService>
    {
        protected override InstanceCreationFlags ComponentCreationFlags => InstanceCreationFlags.ResolveOptionalParametersViaMocker;

        [Fact]
        public void ComponentCreationFlags_ShouldResolveOptionalConstructorDependencies()
        {
            Component.Logger.Should().NotBeNull();
            Component.FileSystem.Should().NotBeNull();
        }

        [Fact]
        public void CallMethod_ShouldResolveOptionalMethodDependencies_WhenInvocationOptionsRequestIt()
        {
            var factory = new OptionalDependencyProbeFactory();

            var probe = Mocks.CallMethod<OptionalDependencyProbe>(
                new InvocationOptions
                {
                    OptionalParameterResolution = OptionalParameterResolutionMode.ResolveViaMocker,
                },
                (Func<ILogger?, IFileSystem?, OptionalDependencyProbe>) factory.Create);

            probe.Logger.Should().NotBeNull();
            probe.FileSystem.Should().NotBeNull();
        }
    }
}
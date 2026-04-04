# Executable Testing Examples

This page mirrors the executable scenarios in the `FastMoq.TestingExample` project.

Use it when you want examples that are both:

- realistic enough to explain how FastMoq fits into an actual service workflow
- backed by tests in the repository instead of static documentation snippets

The source of truth for these examples is the test project itself:

- `FastMoq.TestingExample/RealWorldExampleServices.cs`
- `FastMoq.TestingExample/RealWorldExampleTests.cs`
- `FastMoq.TestingExample/README.md`

## What these examples cover

The current executable examples are designed to exercise the provider-first FastMoq surface rather than only legacy Moq-style entry points.

They demonstrate:

- `MockerTestBase<TComponent>` for normal component creation and dependency auto-injection
- `Mocks.GetMock<T>()` for arrange and verify flows
- built-in `IFileSystem` behavior via the predefined `MockFileSystem`
- `VerifyLogged(...)` for provider-safe structured logging assertions
- `Scenario.With(...).When(...).Then(...).Verify(...)` for the fluent scenario style inside `MockerTestBase<TComponent>`
- provider-first verification through `TimesSpec.Once`, `TimesSpec.Exactly(...)`, `TimesSpec.AtLeast(...)`, `TimesSpec.AtMost(...)`, and `TimesSpec.Never()`

## Example 1: Order Processing Workflow

The order-processing example models a typical service that coordinates several collaborators:

- inventory reservation
- payment authorization
- repository persistence
- operational logging

```csharp
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
        Mocks.GetMock<IOrderRepository>()
            .Verify(x => x.SaveAsync(It.IsAny<OrderRecord>(), CancellationToken.None), Times.Once);
        Mocks.VerifyLogged(LogLevel.Information, "Placed order", 1);
    }
}
```

## Example 2: File Import With Built-In Known Types

The customer-import example uses FastMoq's built-in `IFileSystem` behavior plus a parser and repository.

```csharp
public class CustomerImportServiceExamples : MockerTestBase<CustomerImportService>
{
    [Fact]
    public async Task ImportAsync_ShouldUseBuiltInFileSystemAndPersistParsedRows()
    {
        const string filePath = @"c:\imports\customers.csv";
        const string csv = "customerId,email,segment\nC-100,ada@example.test,VIP";

        Mocks.fileSystem.AddFile(filePath, new MockFileData(csv));
        Mocks.GetMock<ICustomerCsvParser>()
            .Setup(x => x.Parse(csv))
            .Returns(new List<CustomerImportRow>
            {
                new() { CustomerId = "C-100", EmailAddress = "ada@example.test", Segment = "VIP" },
            });

        var importedCount = await Component.ImportAsync(filePath, CancellationToken.None);

        importedCount.Should().Be(1);
        Mocks.GetMock<ICustomerRepository>()
            .Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyList<CustomerImportRow>>(), CancellationToken.None), Times.Once);
    }
}
```

## Example 3: Fluent Scenario Builder

The invoice-reminder example demonstrates the scenario-builder API and provider-first verification.

```csharp
Scenario
    .With(() =>
    {
        Mocks.GetMock<IInvoiceRepository>()
            .Setup(x => x.GetPastDueAsync(now, CancellationToken.None))
            .ReturnsAsync(invoices);
    })
    .When(async () => reminderCount = await Component.SendRemindersAsync(now, CancellationToken.None))
    .Then(() => reminderCount.Should().Be(2))
    .Verify<IInvoiceRepository>(x => x.GetPastDueAsync(now, CancellationToken.None), TimesSpec.Once)
    .Verify<IEmailGateway>(x => x.SendReminderAsync("ap@contoso.test", 125m, CancellationToken.None), TimesSpec.Once)
    .Execute();
```

`TimesSpec` supports `TimesSpec.Once`, `TimesSpec.Exactly(count)`, `TimesSpec.AtLeast(count)`, `TimesSpec.AtMost(count)`, and `TimesSpec.Never()` for provider-safe interaction verification.

For failure-path scenarios, prefer one of these two patterns:

```csharp
Scenario
    .WhenThrows<InvalidOperationException>(() => Component.SendRemindersAsync(now, CancellationToken.None))
    .Then(() => auditTrail.Should().ContainSingle())
    .Execute();
```

```csharp
var exception = await Scenario
    .When(() => Component.SendRemindersAsync(now, CancellationToken.None))
    .ExecuteThrowsAsync<InvalidOperationException>();
```

## Which example to start with

1. Start with the order-processing example for the most typical service-test pattern.
2. Read the customer-import example for built-in helper behavior like `IFileSystem`.
3. Read the invoice-reminder example for `Scenario`, `TimesSpec.Once`, `WhenThrows`, and `ExecuteThrows`.

## Release note

These examples are intended to match the current v4 release line. For the release delta relative to the last public `3.0.0` package, see [What's New Since 3.0.0](../whats-new/README.md).

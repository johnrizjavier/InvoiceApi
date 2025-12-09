using FluentAssertions;
using InvoiceApi.Core.DTOs;
using InvoiceApi.Core.Enums;
using InvoiceApi.Core.Interfaces;
using InvoiceApi.Core.Services;
using InvoiceApi.Tests.Fixtures;
using InvoiceApi.Tests.Mocks;

namespace InvoiceApi.Tests.Integration;

public class InvoiceServiceTests : IDisposable
{
    private readonly MockInvoiceRepository _repository;
    private readonly MockPaymentService _paymentService;
    private readonly MockEmailService _emailService;
    private readonly MockSmsService _smsService;
    private readonly InvoiceService _invoiceService;

    public InvoiceServiceTests()
    {
        _repository = new MockInvoiceRepository();
        _paymentService = new MockPaymentService();
        _emailService = new MockEmailService();
        _smsService = new MockSmsService();

        _invoiceService = new InvoiceService(
            _repository,
            _paymentService,
            _emailService,
            _smsService);
    }

    public void Dispose()
    {
        _repository.Clear();
        _paymentService.Reset();
        _emailService.Reset();
        _smsService.Reset();
    }

    #region CreateInvoice Tests

    [Fact]
    public async Task CreateInvoiceAsync_WithValidRequest_ShouldCreateInvoice()
    {
        // Arrange
        var request = TestFixture.CreateValidInvoiceRequest();

        // Act
        var invoice = await _invoiceService.CreateInvoiceAsync(request);

        // Assert
        invoice.Should().NotBeNull();
        invoice.Id.Should().NotBeNullOrEmpty();
        invoice.InvoiceNumber.Should().StartWith("INV-");
        invoice.Client.Name.Should().Be(request.Client.Name);
        invoice.Client.Email.Should().Be(request.Client.Email);
        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.LineItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateInvoiceAsync_ShouldCalculateTotalsCorrectly()
    {
        // Arrange
        var request = TestFixture.CreateValidInvoiceRequest(taxRate: 10m);

        // Act
        var invoice = await _invoiceService.CreateInvoiceAsync(request);

        // Assert
        // 40 * 150 + 20 * 100 = 6000 + 2000 = 8000
        invoice.Subtotal.Should().Be(8000m);
        invoice.TaxRate.Should().Be(10m);
        invoice.TaxAmount.Should().Be(800m); // 10% of 8000
        invoice.Total.Should().Be(8800m);
    }

    [Fact]
    public async Task CreateInvoiceAsync_ShouldPersistToRepository()
    {
        // Arrange
        var request = TestFixture.CreateValidInvoiceRequest();

        // Act
        var invoice = await _invoiceService.CreateInvoiceAsync(request);

        // Assert
        _repository.Count.Should().Be(1);
        var retrieved = await _repository.GetByIdAsync(invoice.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(invoice.Id);
    }

    #endregion

    #region GetInvoice Tests

    [Fact]
    public async Task GetInvoiceAsync_WithExistingId_ShouldReturnInvoice()
    {
        // Arrange
        var testInvoice = TestFixture.CreateTestInvoice();
        _repository.Add(testInvoice);

        // Act
        var result = await _invoiceService.GetInvoiceAsync(testInvoice.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(testInvoice.Id);
    }

    [Fact]
    public async Task GetInvoiceAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _invoiceService.GetInvoiceAsync("non-existing-id");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetInvoices Tests

    [Fact]
    public async Task GetInvoicesAsync_ShouldReturnAllInvoices()
    {
        // Arrange
        _repository.Add(TestFixture.CreateTestInvoice());
        _repository.Add(TestFixture.CreateTestInvoice());
        _repository.Add(TestFixture.CreateTestInvoice());

        // Act
        var results = await _invoiceService.GetInvoicesAsync();

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetInvoicesAsync_WithStatusFilter_ShouldFilterByStatus()
    {
        // Arrange
        _repository.Add(TestFixture.CreateTestInvoice(status: InvoiceStatus.Draft));
        _repository.Add(TestFixture.CreateTestInvoice(status: InvoiceStatus.Sent));
        _repository.Add(TestFixture.CreateTestInvoice(status: InvoiceStatus.Paid));

        // Act
        var filter = new InvoiceFilter { Status = InvoiceStatus.Sent };
        var results = await _invoiceService.GetInvoicesAsync(filter);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InvoiceStatus.Sent);
    }

    [Fact]
    public async Task GetInvoicesAsync_WithClientEmailFilter_ShouldFilterByEmail()
    {
        // Arrange
        _repository.Add(TestFixture.CreateTestInvoice(clientEmail: "john@example.com"));
        _repository.Add(TestFixture.CreateTestInvoice(clientEmail: "jane@example.com"));
        _repository.Add(TestFixture.CreateTestInvoice(clientEmail: "john@example.com"));

        // Act
        var filter = new InvoiceFilter { ClientEmail = "john@example.com" };
        var results = await _invoiceService.GetInvoicesAsync(filter);

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(i => i.Client.Email.Should().Be("john@example.com"));
    }

    #endregion

    #region UpdateInvoice Tests

    [Fact]
    public async Task UpdateInvoiceAsync_WithValidRequest_ShouldUpdateInvoice()
    {
        // Arrange
        var testInvoice = TestFixture.CreateTestInvoice();
        _repository.Add(testInvoice);
        var updateRequest = TestFixture.CreateUpdateRequest(
            status: InvoiceStatus.Sent,
            notes: "Updated notes");

        // Act
        var result = await _invoiceService.UpdateInvoiceAsync(testInvoice.Id, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(InvoiceStatus.Sent);
        result.Notes.Should().Be("Updated notes");
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateInvoiceAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var updateRequest = TestFixture.CreateUpdateRequest(notes: "Test");

        // Act
        var result = await _invoiceService.UpdateInvoiceAsync("non-existing-id", updateRequest);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteInvoice Tests

    [Fact]
    public async Task DeleteInvoiceAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var testInvoice = TestFixture.CreateTestInvoice();
        _repository.Add(testInvoice);

        // Act
        var result = await _invoiceService.DeleteInvoiceAsync(testInvoice.Id);

        // Assert
        result.Should().BeTrue();
        _repository.Count.Should().Be(0);
    }

    [Fact]
    public async Task DeleteInvoiceAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = await _invoiceService.DeleteInvoiceAsync("non-existing-id");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CreatePaymentLink Tests

    [Fact]
    public async Task CreatePaymentLinkAsync_WithValidInvoice_ShouldCreateLink()
    {
        // Arrange
        var testInvoice = TestFixture.CreateTestInvoice(status: InvoiceStatus.Sent);
        _repository.Add(testInvoice);

        // Act
        var result = await _invoiceService.CreatePaymentLinkAsync(testInvoice.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.PaymentLinkUrl.Should().NotBeNullOrEmpty();
        result.PaymentLinkId.Should().NotBeNullOrEmpty();

        // Verify invoice was updated
        var updated = await _repository.GetByIdAsync(testInvoice.Id);
        updated!.StripePaymentLinkUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreatePaymentLinkAsync_WithNonExistingInvoice_ShouldFail()
    {
        // Act
        var result = await _invoiceService.CreatePaymentLinkAsync("non-existing-id");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invoice not found");
    }

    [Fact]
    public async Task CreatePaymentLinkAsync_WithPaidInvoice_ShouldFail()
    {
        // Arrange
        var testInvoice = TestFixture.CreateTestInvoice(status: InvoiceStatus.Paid);
        _repository.Add(testInvoice);

        // Act
        var result = await _invoiceService.CreatePaymentLinkAsync(testInvoice.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invoice is already paid");
    }

    [Fact]
    public async Task CreatePaymentLinkAsync_WhenPaymentServiceFails_ShouldReturnError()
    {
        // Arrange
        var testInvoice = TestFixture.CreateTestInvoice();
        _repository.Add(testInvoice);
        _paymentService.ShouldSucceed = false;

        // Act
        var result = await _invoiceService.CreatePaymentLinkAsync(testInvoice.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region SendInvoice Tests

    [Fact]
    public async Task SendInvoiceAsync_WithValidInvoice_ShouldSendEmail()
    {
        // Arrange
        var testInvoice = TestFixture.CreateTestInvoice();
        _repository.Add(testInvoice);

        // Act
        var result = await _invoiceService.SendInvoiceAsync(testInvoice.Id);

        // Assert
        result.Success.Should().BeTrue();
        _emailService.SentEmails.Should().HaveCount(1);
        _emailService.SentEmails[0].Type.Should().Be(EmailType.Invoice);
        _emailService.SentEmails[0].ToEmail.Should().Be(testInvoice.Client.Email);
    }

    [Fact]
    public async Task SendInvoiceAsync_ShouldUpdateStatusToSent()
    {
        // Arrange
        var testInvoice = TestFixture.CreateTestInvoice(status: InvoiceStatus.Draft);
        _repository.Add(testInvoice);

        // Act
        await _invoiceService.SendInvoiceAsync(testInvoice.Id);

        // Assert
        var updated = await _repository.GetByIdAsync(testInvoice.Id);
        updated!.Status.Should().Be(InvoiceStatus.Sent);
        updated.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SendInvoiceAsync_WithNonExistingInvoice_ShouldFail()
    {
        // Act
        var result = await _invoiceService.SendInvoiceAsync("non-existing-id");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invoice not found");
    }

    #endregion

    #region SendPaymentReminder Tests

    [Fact]
    public async Task SendPaymentReminderAsync_WithValidInvoice_ShouldSendSms()
    {
        // Arrange
        var testInvoice = TestFixture.CreateTestInvoice(clientPhone: "+1234567890");
        _repository.Add(testInvoice);

        // Act
        var result = await _invoiceService.SendPaymentReminderAsync(testInvoice.Id);

        // Assert
        result.Success.Should().BeTrue();
        _smsService.SentMessages.Should().HaveCount(1);
        _smsService.SentMessages[0].Type.Should().Be(SmsType.PaymentReminder);
    }

    [Fact]
    public async Task SendPaymentReminderAsync_WithoutPhoneNumber_ShouldFail()
    {
        // Arrange
        var testInvoice = TestFixture.CreateTestInvoice(clientPhone: null);
        _repository.Add(testInvoice);

        // Act
        var result = await _invoiceService.SendPaymentReminderAsync(testInvoice.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Client phone number not available");
    }

    #endregion

    #region HandlePaymentWebhook Tests

    [Fact]
    public async Task HandlePaymentWebhookAsync_WithValidEvent_ShouldMarkInvoiceAsPaid()
    {
        // Arrange
        var testInvoice = TestFixture.CreateTestInvoice(status: InvoiceStatus.Sent);
        _repository.Add(testInvoice);

        var webhookEvent = new WebhookEvent
        {
            Type = "checkout.session.completed",
            InvoiceId = testInvoice.Id,
            AmountPaid = testInvoice.Total
        };

        // Act
        await _invoiceService.HandlePaymentWebhookAsync(webhookEvent);

        // Assert
        var updated = await _repository.GetByIdAsync(testInvoice.Id);
        updated!.Status.Should().Be(InvoiceStatus.Paid);
        updated.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task HandlePaymentWebhookAsync_ShouldSendConfirmationNotifications()
    {
        // Arrange
        var testInvoice = TestFixture.CreateTestInvoice(clientPhone: "+1234567890");
        _repository.Add(testInvoice);

        var webhookEvent = new WebhookEvent
        {
            Type = "checkout.session.completed",
            InvoiceId = testInvoice.Id,
            AmountPaid = testInvoice.Total
        };

        // Act
        await _invoiceService.HandlePaymentWebhookAsync(webhookEvent);

        // Assert
        _emailService.SentEmails.Should().ContainSingle(e => e.Type == EmailType.PaymentConfirmation);
        _smsService.SentMessages.Should().ContainSingle(s => s.Type == SmsType.PaymentConfirmation);
    }

    [Fact]
    public async Task HandlePaymentWebhookAsync_WithoutPhoneNumber_ShouldOnlySendEmail()
    {
        // Arrange
        var testInvoice = TestFixture.CreateTestInvoice(clientPhone: null);
        _repository.Add(testInvoice);

        var webhookEvent = new WebhookEvent
        {
            Type = "checkout.session.completed",
            InvoiceId = testInvoice.Id
        };

        // Act
        await _invoiceService.HandlePaymentWebhookAsync(webhookEvent);

        // Assert
        _emailService.SentEmails.Should().HaveCount(1);
        _smsService.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task HandlePaymentWebhookAsync_WithUnknownEventType_ShouldDoNothing()
    {
        // Arrange
        var testInvoice = TestFixture.CreateTestInvoice();
        _repository.Add(testInvoice);

        var webhookEvent = new WebhookEvent
        {
            Type = "unknown.event",
            InvoiceId = testInvoice.Id
        };

        // Act
        await _invoiceService.HandlePaymentWebhookAsync(webhookEvent);

        // Assert
        var invoice = await _repository.GetByIdAsync(testInvoice.Id);
        invoice!.Status.Should().Be(InvoiceStatus.Draft);
        _emailService.SentEmails.Should().BeEmpty();
    }

    #endregion
}

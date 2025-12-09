using InvoiceApi.Core.Entities;
using InvoiceApi.Core.Interfaces;

namespace InvoiceApi.Tests.Mocks;

public class MockEmailService : IEmailService
{
    public bool ShouldSucceed { get; set; } = true;
    public List<EmailRecord> SentEmails { get; } = new();

    public Task<EmailResult> SendInvoiceAsync(Invoice invoice)
    {
        var record = new EmailRecord
        {
            Type = EmailType.Invoice,
            ToEmail = invoice.Client.Email,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            SentAt = DateTime.UtcNow
        };
        SentEmails.Add(record);

        return Task.FromResult(CreateResult());
    }

    public Task<EmailResult> SendPaymentConfirmationAsync(Invoice invoice)
    {
        var record = new EmailRecord
        {
            Type = EmailType.PaymentConfirmation,
            ToEmail = invoice.Client.Email,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            SentAt = DateTime.UtcNow
        };
        SentEmails.Add(record);

        return Task.FromResult(CreateResult());
    }

    public Task<EmailResult> SendPaymentReminderAsync(Invoice invoice)
    {
        var record = new EmailRecord
        {
            Type = EmailType.PaymentReminder,
            ToEmail = invoice.Client.Email,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            SentAt = DateTime.UtcNow
        };
        SentEmails.Add(record);

        return Task.FromResult(CreateResult());
    }

    private EmailResult CreateResult()
    {
        if (!ShouldSucceed)
        {
            return new EmailResult
            {
                Success = false,
                ErrorMessage = "Email service error"
            };
        }

        return new EmailResult
        {
            Success = true,
            MessageId = Guid.NewGuid().ToString()
        };
    }

    public void Reset()
    {
        ShouldSucceed = true;
        SentEmails.Clear();
    }
}

public class EmailRecord
{
    public EmailType Type { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string InvoiceId { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public enum EmailType
{
    Invoice,
    PaymentConfirmation,
    PaymentReminder
}

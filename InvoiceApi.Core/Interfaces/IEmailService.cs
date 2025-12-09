using InvoiceApi.Core.Entities;

namespace InvoiceApi.Core.Interfaces;

public interface IEmailService
{
    Task<EmailResult> SendInvoiceAsync(Invoice invoice);
    Task<EmailResult> SendPaymentConfirmationAsync(Invoice invoice);
    Task<EmailResult> SendPaymentReminderAsync(Invoice invoice);
}

public class EmailResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
}

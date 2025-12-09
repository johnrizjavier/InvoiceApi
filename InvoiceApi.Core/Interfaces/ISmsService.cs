using InvoiceApi.Core.Entities;

namespace InvoiceApi.Core.Interfaces;

public interface ISmsService
{
    Task<SmsResult> SendPaymentReminderAsync(Invoice invoice);
    Task<SmsResult> SendPaymentConfirmationAsync(Invoice invoice);
}

public class SmsResult
{
    public bool Success { get; set; }
    public string? MessageSid { get; set; }
    public string? ErrorMessage { get; set; }
}

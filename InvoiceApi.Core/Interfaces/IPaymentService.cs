using InvoiceApi.Core.Entities;

namespace InvoiceApi.Core.Interfaces;

public interface IPaymentService
{
    Task<PaymentLinkResult> CreatePaymentLinkAsync(Invoice invoice);
    Task<bool> ValidateWebhookAsync(string payload, string signature);
    Task<WebhookEvent?> ParseWebhookAsync(string payload);
}

public class PaymentLinkResult
{
    public bool Success { get; set; }
    public string? PaymentLinkId { get; set; }
    public string? PaymentLinkUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class WebhookEvent
{
    public string Type { get; set; } = string.Empty;
    public string? InvoiceId { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? CheckoutSessionId { get; set; }
    public decimal? AmountPaid { get; set; }
}

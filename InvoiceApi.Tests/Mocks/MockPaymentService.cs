using InvoiceApi.Core.Entities;
using InvoiceApi.Core.Interfaces;

namespace InvoiceApi.Tests.Mocks;

public class MockPaymentService : IPaymentService
{
    public bool ShouldSucceed { get; set; } = true;
    public string? LastInvoiceId { get; private set; }
    public int CreatePaymentLinkCallCount { get; private set; }

    public Task<PaymentLinkResult> CreatePaymentLinkAsync(Invoice invoice)
    {
        CreatePaymentLinkCallCount++;
        LastInvoiceId = invoice.Id;

        if (!ShouldSucceed)
        {
            return Task.FromResult(new PaymentLinkResult
            {
                Success = false,
                ErrorMessage = "Payment service error"
            });
        }

        return Task.FromResult(new PaymentLinkResult
        {
            Success = true,
            PaymentLinkId = $"cs_test_{Guid.NewGuid():N}",
            PaymentLinkUrl = $"https://checkout.stripe.com/pay/cs_test_{invoice.Id}"
        });
    }

    public Task<bool> ValidateWebhookAsync(string payload, string signature)
    {
        // For testing, validate if signature starts with "valid_"
        return Task.FromResult(signature.StartsWith("valid_"));
    }

    public Task<WebhookEvent?> ParseWebhookAsync(string payload)
    {
        // Simple mock parsing for tests
        if (payload.Contains("checkout.session.completed"))
        {
            return Task.FromResult<WebhookEvent?>(new WebhookEvent
            {
                Type = "checkout.session.completed",
                InvoiceId = ExtractInvoiceId(payload),
                CheckoutSessionId = $"cs_test_{Guid.NewGuid():N}",
                AmountPaid = 100.00m
            });
        }

        return Task.FromResult<WebhookEvent?>(null);
    }

    private static string? ExtractInvoiceId(string payload)
    {
        // Simple extraction for test payloads
        var start = payload.IndexOf("invoice_id\":\"");
        if (start == -1) return null;
        start += 13;
        var end = payload.IndexOf("\"", start);
        return end > start ? payload[start..end] : null;
    }

    public void Reset()
    {
        ShouldSucceed = true;
        LastInvoiceId = null;
        CreatePaymentLinkCallCount = 0;
    }
}

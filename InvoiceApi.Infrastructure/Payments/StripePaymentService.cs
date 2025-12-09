using InvoiceApi.Core.Interfaces;
using Stripe;
using Stripe.Checkout;
using Invoice = InvoiceApi.Core.Entities.Invoice;

namespace InvoiceApi.Infrastructure.Payments;

public class StripePaymentService : IPaymentService
{
    private readonly string _webhookSecret;

    public StripePaymentService(string secretKey, string webhookSecret)
    {
        StripeConfiguration.ApiKey = secretKey;
        _webhookSecret = webhookSecret;
    }

    public async Task<PaymentLinkResult> CreatePaymentLinkAsync(Invoice invoice)
    {
        try
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = invoice.LineItems.Select(li => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = (long)(li.UnitPrice * 100), // Stripe uses cents
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = li.Description
                        }
                    },
                    Quantity = li.Quantity
                }).ToList(),
                Mode = "payment",
                SuccessUrl = $"https://yourapp.com/payment/success?invoice_id={invoice.Id}",
                CancelUrl = $"https://yourapp.com/payment/cancel?invoice_id={invoice.Id}",
                Metadata = new Dictionary<string, string>
                {
                    { "invoice_id", invoice.Id },
                    { "invoice_number", invoice.InvoiceNumber }
                },
                CustomerEmail = invoice.Client.Email
            };

            // Add tax if applicable
            if (invoice.TaxRate > 0)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = (long)(invoice.TaxAmount * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Tax ({invoice.TaxRate}%)"
                        }
                    },
                    Quantity = 1
                });
            }

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return new PaymentLinkResult
            {
                Success = true,
                PaymentLinkId = session.Id,
                PaymentLinkUrl = session.Url
            };
        }
        catch (StripeException ex)
        {
            return new PaymentLinkResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public Task<bool> ValidateWebhookAsync(string payload, string signature)
    {
        try
        {
            EventUtility.ConstructEvent(payload, signature, _webhookSecret);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<WebhookEvent?> ParseWebhookAsync(string payload)
    {
        try
        {
            var stripeEvent = EventUtility.ParseEvent(payload);

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session is null) return Task.FromResult<WebhookEvent?>(null);

                return Task.FromResult<WebhookEvent?>(new WebhookEvent
                {
                    Type = stripeEvent.Type,
                    InvoiceId = session.Metadata.TryGetValue("invoice_id", out var invoiceId) ? invoiceId : null,
                    CheckoutSessionId = session.Id,
                    PaymentIntentId = session.PaymentIntentId,
                    AmountPaid = session.AmountTotal.HasValue ? session.AmountTotal.Value / 100m : null
                });
            }

            return Task.FromResult<WebhookEvent?>(null);
        }
        catch
        {
            return Task.FromResult<WebhookEvent?>(null);
        }
    }
}

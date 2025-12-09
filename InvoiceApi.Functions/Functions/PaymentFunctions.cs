using System.Net;
using InvoiceApi.Core.DTOs;
using InvoiceApi.Core.Interfaces;
using InvoiceApi.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace InvoiceApi.Functions.Functions;

public class PaymentFunctions
{
    private readonly InvoiceService _invoiceService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentFunctions> _logger;

    public PaymentFunctions(
        InvoiceService invoiceService,
        IPaymentService paymentService,
        ILogger<PaymentFunctions> logger)
    {
        _invoiceService = invoiceService;
        _paymentService = paymentService;
        _logger = logger;
    }

    [Function("CreatePaymentLink")]
    public async Task<HttpResponseData> CreatePaymentLink(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invoices/{id}/payment-link")] HttpRequestData req,
        string id)
    {
        try
        {
            var result = await _invoiceService.CreatePaymentLinkAsync(id);

            if (!result.Success)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
                return errorResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                paymentLinkUrl = result.PaymentLinkUrl,
                paymentLinkId = result.PaymentLinkId
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment link for invoice {InvoiceId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Failed to create payment link"
            });
            return response;
        }
    }

    [Function("StripeWebhook")]
    public async Task<HttpResponseData> StripeWebhook(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhooks/stripe")] HttpRequestData req)
    {
        try
        {
            var payload = await new StreamReader(req.Body).ReadToEndAsync();

            // Validate webhook signature
            if (req.Headers.TryGetValues("Stripe-Signature", out var signatureValues))
            {
                var signature = signatureValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(signature))
                {
                    var isValid = await _paymentService.ValidateWebhookAsync(payload, signature);
                    if (!isValid)
                    {
                        _logger.LogWarning("Invalid Stripe webhook signature");
                        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                        await badResponse.WriteAsJsonAsync(new { error = "Invalid signature" });
                        return badResponse;
                    }
                }
            }

            // Parse and handle the webhook event
            var webhookEvent = await _paymentService.ParseWebhookAsync(payload);

            if (webhookEvent is not null)
            {
                _logger.LogInformation(
                    "Processing Stripe webhook: {EventType} for invoice {InvoiceId}",
                    webhookEvent.Type,
                    webhookEvent.InvoiceId);

                await _invoiceService.HandlePaymentWebhookAsync(webhookEvent);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { received = true });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = "Webhook processing failed" });
            return response;
        }
    }
}

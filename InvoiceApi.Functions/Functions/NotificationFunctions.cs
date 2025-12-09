using System.Net;
using InvoiceApi.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace InvoiceApi.Functions.Functions;

public class NotificationFunctions
{
    private readonly InvoiceService _invoiceService;
    private readonly ILogger<NotificationFunctions> _logger;

    public NotificationFunctions(InvoiceService invoiceService, ILogger<NotificationFunctions> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    [Function("SendInvoice")]
    public async Task<HttpResponseData> SendInvoice(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invoices/{id}/send")] HttpRequestData req,
        string id)
    {
        try
        {
            var result = await _invoiceService.SendInvoiceAsync(id);

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
                message = "Invoice sent successfully",
                messageId = result.MessageId
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invoice {InvoiceId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Failed to send invoice"
            });
            return response;
        }
    }

    [Function("SendReminder")]
    public async Task<HttpResponseData> SendReminder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invoices/{id}/remind")] HttpRequestData req,
        string id)
    {
        try
        {
            var result = await _invoiceService.SendPaymentReminderAsync(id);

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
                message = "Payment reminder sent successfully",
                messageSid = result.MessageSid
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminder for invoice {InvoiceId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Failed to send reminder"
            });
            return response;
        }
    }
}

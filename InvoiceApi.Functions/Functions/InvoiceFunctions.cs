using System.Net;
using InvoiceApi.Core.DTOs;
using InvoiceApi.Core.Enums;
using InvoiceApi.Core.Interfaces;
using InvoiceApi.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace InvoiceApi.Functions.Functions;

public class InvoiceFunctions
{
    private readonly InvoiceService _invoiceService;
    private readonly ILogger<InvoiceFunctions> _logger;

    public InvoiceFunctions(InvoiceService invoiceService, ILogger<InvoiceFunctions> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    [Function("CreateInvoice")]
    public async Task<HttpResponseData> CreateInvoice(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invoices")] HttpRequestData req)
    {
        try
        {
            var request = await req.ReadFromJsonAsync<CreateInvoiceRequest>();
            if (request is null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            var invoice = await _invoiceService.CreateInvoiceAsync(request);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(ApiResponse<InvoiceResponse>.Ok(
                InvoiceResponse.FromInvoice(invoice),
                "Invoice created successfully"));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to create invoice");
        }
    }

    [Function("GetInvoices")]
    public async Task<HttpResponseData> GetInvoices(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "invoices")] HttpRequestData req)
    {
        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

            var filter = new InvoiceFilter
            {
                Status = Enum.TryParse<InvoiceStatus>(query["status"], true, out var status) ? status : null,
                FromDate = DateTime.TryParse(query["fromDate"], out var fromDate) ? fromDate : null,
                ToDate = DateTime.TryParse(query["toDate"], out var toDate) ? toDate : null,
                ClientEmail = query["clientEmail"],
                Skip = int.TryParse(query["skip"], out var skip) ? skip : 0,
                Take = int.TryParse(query["take"], out var take) ? take : 50
            };

            var invoices = await _invoiceService.GetInvoicesAsync(filter);
            var responses = invoices.Select(InvoiceResponse.FromInvoice).ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ApiResponse<List<InvoiceResponse>>.Ok(responses));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to retrieve invoices");
        }
    }

    [Function("GetInvoice")]
    public async Task<HttpResponseData> GetInvoice(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "invoices/{id}")] HttpRequestData req,
        string id)
    {
        try
        {
            var invoice = await _invoiceService.GetInvoiceAsync(id);
            if (invoice is null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Invoice not found");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ApiResponse<InvoiceResponse>.Ok(InvoiceResponse.FromInvoice(invoice)));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice {InvoiceId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to retrieve invoice");
        }
    }

    [Function("UpdateInvoice")]
    public async Task<HttpResponseData> UpdateInvoice(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "invoices/{id}")] HttpRequestData req,
        string id)
    {
        try
        {
            var request = await req.ReadFromJsonAsync<UpdateInvoiceRequest>();
            if (request is null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            var invoice = await _invoiceService.UpdateInvoiceAsync(id, request);
            if (invoice is null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Invoice not found");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ApiResponse<InvoiceResponse>.Ok(
                InvoiceResponse.FromInvoice(invoice),
                "Invoice updated successfully"));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice {InvoiceId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to update invoice");
        }
    }

    [Function("DeleteInvoice")]
    public async Task<HttpResponseData> DeleteInvoice(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "invoices/{id}")] HttpRequestData req,
        string id)
    {
        try
        {
            var deleted = await _invoiceService.DeleteInvoiceAsync(id);
            if (!deleted)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Invoice not found");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { success = true, message = "Invoice deleted successfully" });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice {InvoiceId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to delete invoice");
        }
    }

    private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { success = false, message });
        return response;
    }
}

using InvoiceApi.Core.DTOs;
using InvoiceApi.Core.Entities;
using InvoiceApi.Core.Enums;
using InvoiceApi.Core.Interfaces;

namespace InvoiceApi.Core.Services;

public class InvoiceService
{
    private readonly IInvoiceRepository _repository;
    private readonly IPaymentService _paymentService;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;

    public InvoiceService(
        IInvoiceRepository repository,
        IPaymentService paymentService,
        IEmailService emailService,
        ISmsService smsService)
    {
        _repository = repository;
        _paymentService = paymentService;
        _emailService = emailService;
        _smsService = smsService;
    }

    public async Task<Invoice> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = GenerateInvoiceNumber(),
            Client = new Client
            {
                Name = request.Client.Name,
                Email = request.Client.Email,
                Phone = request.Client.Phone,
                Company = request.Client.Company,
                Address = request.Client.Address is null ? null : new Address
                {
                    Street = request.Client.Address.Street,
                    City = request.Client.Address.City,
                    State = request.Client.Address.State,
                    PostalCode = request.Client.Address.PostalCode,
                    Country = request.Client.Address.Country
                }
            },
            LineItems = request.LineItems.Select(li => new LineItem
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice
            }).ToList(),
            TaxRate = request.TaxRate,
            DueDate = request.DueDate,
            Notes = request.Notes,
            Status = InvoiceStatus.Draft
        };

        return await _repository.CreateAsync(invoice);
    }

    public async Task<Invoice?> GetInvoiceAsync(string id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesAsync(InvoiceFilter? filter = null)
    {
        return await _repository.GetAllAsync(filter);
    }

    public async Task<Invoice?> UpdateInvoiceAsync(string id, UpdateInvoiceRequest request)
    {
        var invoice = await _repository.GetByIdAsync(id);
        if (invoice is null) return null;

        if (request.Client is not null)
        {
            invoice.Client = new Client
            {
                Name = request.Client.Name,
                Email = request.Client.Email,
                Phone = request.Client.Phone,
                Company = request.Client.Company,
                Address = request.Client.Address is null ? null : new Address
                {
                    Street = request.Client.Address.Street,
                    City = request.Client.Address.City,
                    State = request.Client.Address.State,
                    PostalCode = request.Client.Address.PostalCode,
                    Country = request.Client.Address.Country
                }
            };
        }

        if (request.LineItems is not null)
        {
            invoice.LineItems = request.LineItems.Select(li => new LineItem
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice
            }).ToList();
        }

        if (request.TaxRate.HasValue)
            invoice.TaxRate = request.TaxRate.Value;

        if (request.DueDate.HasValue)
            invoice.DueDate = request.DueDate.Value;

        if (request.Status.HasValue)
            invoice.Status = request.Status.Value;

        if (request.Notes is not null)
            invoice.Notes = request.Notes;

        invoice.UpdatedAt = DateTime.UtcNow;

        return await _repository.UpdateAsync(invoice);
    }

    public async Task<bool> DeleteInvoiceAsync(string id)
    {
        return await _repository.DeleteAsync(id);
    }

    public async Task<PaymentLinkResult> CreatePaymentLinkAsync(string invoiceId)
    {
        var invoice = await _repository.GetByIdAsync(invoiceId);
        if (invoice is null)
        {
            return new PaymentLinkResult
            {
                Success = false,
                ErrorMessage = "Invoice not found"
            };
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return new PaymentLinkResult
            {
                Success = false,
                ErrorMessage = "Invoice is already paid"
            };
        }

        var result = await _paymentService.CreatePaymentLinkAsync(invoice);

        if (result.Success)
        {
            invoice.StripePaymentLinkId = result.PaymentLinkId;
            invoice.StripePaymentLinkUrl = result.PaymentLinkUrl;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(invoice);
        }

        return result;
    }

    public async Task<EmailResult> SendInvoiceAsync(string invoiceId)
    {
        var invoice = await _repository.GetByIdAsync(invoiceId);
        if (invoice is null)
        {
            return new EmailResult
            {
                Success = false,
                ErrorMessage = "Invoice not found"
            };
        }

        var result = await _emailService.SendInvoiceAsync(invoice);

        if (result.Success)
        {
            invoice.Status = InvoiceStatus.Sent;
            invoice.SentAt = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(invoice);
        }

        return result;
    }

    public async Task<SmsResult> SendPaymentReminderAsync(string invoiceId)
    {
        var invoice = await _repository.GetByIdAsync(invoiceId);
        if (invoice is null)
        {
            return new SmsResult
            {
                Success = false,
                ErrorMessage = "Invoice not found"
            };
        }

        if (string.IsNullOrEmpty(invoice.Client.Phone))
        {
            return new SmsResult
            {
                Success = false,
                ErrorMessage = "Client phone number not available"
            };
        }

        return await _smsService.SendPaymentReminderAsync(invoice);
    }

    public async Task HandlePaymentWebhookAsync(WebhookEvent webhookEvent)
    {
        if (webhookEvent.Type != "checkout.session.completed" || string.IsNullOrEmpty(webhookEvent.InvoiceId))
            return;

        var invoice = await _repository.GetByIdAsync(webhookEvent.InvoiceId);
        if (invoice is null) return;

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(invoice);

        // Send confirmation notifications
        await _emailService.SendPaymentConfirmationAsync(invoice);

        if (!string.IsNullOrEmpty(invoice.Client.Phone))
        {
            await _smsService.SendPaymentConfirmationAsync(invoice);
        }
    }

    private static string GenerateInvoiceNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        return $"INV-{timestamp}-{random}";
    }
}

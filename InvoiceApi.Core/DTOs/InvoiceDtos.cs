using InvoiceApi.Core.Entities;
using InvoiceApi.Core.Enums;

namespace InvoiceApi.Core.DTOs;

public record CreateInvoiceRequest
{
    public required ClientDto Client { get; init; }
    public required List<LineItemDto> LineItems { get; init; }
    public decimal TaxRate { get; init; }
    public DateTime DueDate { get; init; }
    public string? Notes { get; init; }
}

public record UpdateInvoiceRequest
{
    public ClientDto? Client { get; init; }
    public List<LineItemDto>? LineItems { get; init; }
    public decimal? TaxRate { get; init; }
    public DateTime? DueDate { get; init; }
    public InvoiceStatus? Status { get; init; }
    public string? Notes { get; init; }
}

public record ClientDto
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? Company { get; init; }
    public AddressDto? Address { get; init; }
}

public record AddressDto
{
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
}

public record LineItemDto
{
    public required string Description { get; init; }
    public int Quantity { get; init; } = 1;
    public required decimal UnitPrice { get; init; }
}

public record InvoiceResponse
{
    public required string Id { get; init; }
    public required string InvoiceNumber { get; init; }
    public required ClientDto Client { get; init; }
    public required List<LineItemDto> LineItems { get; init; }
    public decimal Subtotal { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal Total { get; init; }
    public InvoiceStatus Status { get; init; }
    public DateTime IssueDate { get; init; }
    public DateTime DueDate { get; init; }
    public string? PaymentLinkUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PaidAt { get; init; }
    public string? Notes { get; init; }

    public static InvoiceResponse FromInvoice(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        Client = new ClientDto
        {
            Name = invoice.Client.Name,
            Email = invoice.Client.Email,
            Phone = invoice.Client.Phone,
            Company = invoice.Client.Company,
            Address = invoice.Client.Address is null ? null : new AddressDto
            {
                Street = invoice.Client.Address.Street,
                City = invoice.Client.Address.City,
                State = invoice.Client.Address.State,
                PostalCode = invoice.Client.Address.PostalCode,
                Country = invoice.Client.Address.Country
            }
        },
        LineItems = invoice.LineItems.Select(li => new LineItemDto
        {
            Description = li.Description,
            Quantity = li.Quantity,
            UnitPrice = li.UnitPrice
        }).ToList(),
        Subtotal = invoice.Subtotal,
        TaxRate = invoice.TaxRate,
        TaxAmount = invoice.TaxAmount,
        Total = invoice.Total,
        Status = invoice.Status,
        IssueDate = invoice.IssueDate,
        DueDate = invoice.DueDate,
        PaymentLinkUrl = invoice.StripePaymentLinkUrl,
        CreatedAt = invoice.CreatedAt,
        PaidAt = invoice.PaidAt,
        Notes = invoice.Notes
    };
}

public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public List<string>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Errors = errors
    };
}

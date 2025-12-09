using InvoiceApi.Core.DTOs;
using InvoiceApi.Core.Entities;
using InvoiceApi.Core.Enums;

namespace InvoiceApi.Tests.Fixtures;

public static class TestFixture
{
    public static CreateInvoiceRequest CreateValidInvoiceRequest(
        string? clientName = null,
        string? clientEmail = null,
        decimal? taxRate = null)
    {
        return new CreateInvoiceRequest
        {
            Client = new ClientDto
            {
                Name = clientName ?? "John Doe",
                Email = clientEmail ?? "john@example.com",
                Phone = "+1234567890",
                Company = "Acme Inc",
                Address = new AddressDto
                {
                    Street = "123 Main St",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10001",
                    Country = "USA"
                }
            },
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    Description = "Web Development",
                    Quantity = 40,
                    UnitPrice = 150.00m
                },
                new LineItemDto
                {
                    Description = "UI Design",
                    Quantity = 20,
                    UnitPrice = 100.00m
                }
            },
            TaxRate = taxRate ?? 10m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Notes = "Payment due within 30 days"
        };
    }

    private const string DefaultPhone = "__DEFAULT_PHONE__";

    public static Invoice CreateTestInvoice(
        string? id = null,
        InvoiceStatus? status = null,
        string? clientEmail = null,
        string? clientPhone = DefaultPhone)
    {
        return new Invoice
        {
            Id = id ?? Guid.NewGuid().ToString(),
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",
            Client = new Client
            {
                Name = "Test Client",
                Email = clientEmail ?? "test@example.com",
                Phone = clientPhone == DefaultPhone ? "+1234567890" : clientPhone,
                Company = "Test Company"
            },
            LineItems = new List<LineItem>
            {
                new LineItem
                {
                    Description = "Test Service",
                    Quantity = 10,
                    UnitPrice = 100.00m
                }
            },
            TaxRate = 10m,
            Status = status ?? InvoiceStatus.Draft,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static UpdateInvoiceRequest CreateUpdateRequest(
        InvoiceStatus? status = null,
        decimal? taxRate = null,
        string? notes = null)
    {
        return new UpdateInvoiceRequest
        {
            Status = status,
            TaxRate = taxRate,
            Notes = notes
        };
    }
}

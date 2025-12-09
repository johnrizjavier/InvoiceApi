using InvoiceApi.Core.Enums;

namespace InvoiceApi.Core.Entities;

public class Invoice
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string InvoiceNumber { get; set; } = string.Empty;
    public Client Client { get; set; } = new();
    public List<LineItem> LineItems { get; set; } = new();

    public decimal Subtotal => LineItems.Sum(x => x.Total);
    public decimal TaxRate { get; set; }
    public decimal TaxAmount => Subtotal * (TaxRate / 100);
    public decimal Total => Subtotal + TaxAmount;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }

    public string? StripePaymentLinkId { get; set; }
    public string? StripePaymentLinkUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? SentAt { get; set; }

    public string? Notes { get; set; }
}

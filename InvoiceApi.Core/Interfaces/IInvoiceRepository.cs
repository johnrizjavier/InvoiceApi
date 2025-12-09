using InvoiceApi.Core.Entities;
using InvoiceApi.Core.Enums;

namespace InvoiceApi.Core.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(string id);
    Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);
    Task<IEnumerable<Invoice>> GetAllAsync(InvoiceFilter? filter = null);
    Task<Invoice> CreateAsync(Invoice invoice);
    Task<Invoice> UpdateAsync(Invoice invoice);
    Task<bool> DeleteAsync(string id);
}

public class InvoiceFilter
{
    public InvoiceStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? ClientEmail { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

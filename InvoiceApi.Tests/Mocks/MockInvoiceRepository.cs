using InvoiceApi.Core.Entities;
using InvoiceApi.Core.Interfaces;

namespace InvoiceApi.Tests.Mocks;

public class MockInvoiceRepository : IInvoiceRepository
{
    private readonly Dictionary<string, Invoice> _invoices = new();

    public Task<Invoice?> GetByIdAsync(string id)
    {
        _invoices.TryGetValue(id, out var invoice);
        return Task.FromResult(invoice);
    }

    public Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
    {
        var invoice = _invoices.Values.FirstOrDefault(i => i.InvoiceNumber == invoiceNumber);
        return Task.FromResult(invoice);
    }

    public Task<IEnumerable<Invoice>> GetAllAsync(InvoiceFilter? filter = null)
    {
        var query = _invoices.Values.AsEnumerable();

        if (filter is not null)
        {
            if (filter.Status.HasValue)
                query = query.Where(i => i.Status == filter.Status.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(i => i.IssueDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(i => i.IssueDate <= filter.ToDate.Value);

            if (!string.IsNullOrEmpty(filter.ClientEmail))
                query = query.Where(i => i.Client.Email == filter.ClientEmail);

            query = query.Skip(filter.Skip).Take(filter.Take);
        }

        return Task.FromResult(query);
    }

    public Task<Invoice> CreateAsync(Invoice invoice)
    {
        _invoices[invoice.Id] = invoice;
        return Task.FromResult(invoice);
    }

    public Task<Invoice> UpdateAsync(Invoice invoice)
    {
        _invoices[invoice.Id] = invoice;
        return Task.FromResult(invoice);
    }

    public Task<bool> DeleteAsync(string id)
    {
        return Task.FromResult(_invoices.Remove(id));
    }

    // Helper methods for testing
    public void Clear() => _invoices.Clear();
    public int Count => _invoices.Count;
    public void Add(Invoice invoice) => _invoices[invoice.Id] = invoice;
}

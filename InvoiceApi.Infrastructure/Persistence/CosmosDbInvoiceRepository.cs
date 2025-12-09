using InvoiceApi.Core.Entities;
using InvoiceApi.Core.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace InvoiceApi.Infrastructure.Persistence;

public class CosmosDbInvoiceRepository : IInvoiceRepository
{
    private readonly Container _container;

    public CosmosDbInvoiceRepository(CosmosClient cosmosClient, string databaseName, string containerName)
    {
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task<Invoice?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<Invoice>(id, new PartitionKey(id));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
    {
        var query = _container.GetItemLinqQueryable<Invoice>()
            .Where(i => i.InvoiceNumber == invoiceNumber)
            .ToFeedIterator();

        if (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            return response.FirstOrDefault();
        }

        return null;
    }

    public async Task<IEnumerable<Invoice>> GetAllAsync(InvoiceFilter? filter = null)
    {
        var queryable = _container.GetItemLinqQueryable<Invoice>().AsQueryable();

        if (filter is not null)
        {
            if (filter.Status.HasValue)
                queryable = queryable.Where(i => i.Status == filter.Status.Value);

            if (filter.FromDate.HasValue)
                queryable = queryable.Where(i => i.IssueDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                queryable = queryable.Where(i => i.IssueDate <= filter.ToDate.Value);

            if (!string.IsNullOrEmpty(filter.ClientEmail))
                queryable = queryable.Where(i => i.Client.Email == filter.ClientEmail);

            queryable = queryable.Skip(filter.Skip).Take(filter.Take);
        }

        var iterator = queryable.ToFeedIterator();
        var results = new List<Invoice>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        var response = await _container.CreateItemAsync(invoice, new PartitionKey(invoice.Id));
        return response.Resource;
    }

    public async Task<Invoice> UpdateAsync(Invoice invoice)
    {
        var response = await _container.UpsertItemAsync(invoice, new PartitionKey(invoice.Id));
        return response.Resource;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            await _container.DeleteItemAsync<Invoice>(id, new PartitionKey(id));
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}

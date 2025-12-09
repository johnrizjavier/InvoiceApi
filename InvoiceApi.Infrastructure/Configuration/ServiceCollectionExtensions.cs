using InvoiceApi.Core.Interfaces;
using InvoiceApi.Core.Services;
using InvoiceApi.Infrastructure.Notifications;
using InvoiceApi.Infrastructure.Payments;
using InvoiceApi.Infrastructure.Persistence;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceApi.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInvoiceApiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // CosmosDB
        services.AddSingleton(sp =>
        {
            var endpoint = configuration["CosmosDb:Endpoint"]
                ?? throw new InvalidOperationException("CosmosDb:Endpoint is not configured");
            var key = configuration["CosmosDb:Key"]
                ?? throw new InvalidOperationException("CosmosDb:Key is not configured");

            return new CosmosClient(endpoint, key, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });
        });

        services.AddSingleton<IInvoiceRepository>(sp =>
        {
            var cosmosClient = sp.GetRequiredService<CosmosClient>();
            var databaseName = configuration["CosmosDb:DatabaseName"] ?? "InvoiceDb";
            var containerName = configuration["CosmosDb:ContainerName"] ?? "Invoices";
            return new CosmosDbInvoiceRepository(cosmosClient, databaseName, containerName);
        });

        // Stripe
        services.AddSingleton<IPaymentService>(sp =>
        {
            var secretKey = configuration["Stripe:SecretKey"]
                ?? throw new InvalidOperationException("Stripe:SecretKey is not configured");
            var webhookSecret = configuration["Stripe:WebhookSecret"]
                ?? throw new InvalidOperationException("Stripe:WebhookSecret is not configured");
            return new StripePaymentService(secretKey, webhookSecret);
        });

        // SendGrid
        services.AddSingleton<IEmailService>(sp =>
        {
            var apiKey = configuration["SendGrid:ApiKey"]
                ?? throw new InvalidOperationException("SendGrid:ApiKey is not configured");
            var fromEmail = configuration["SendGrid:FromEmail"]
                ?? throw new InvalidOperationException("SendGrid:FromEmail is not configured");
            var fromName = configuration["SendGrid:FromName"] ?? "Invoice System";
            return new SendGridEmailService(apiKey, fromEmail, fromName);
        });

        // Twilio
        services.AddSingleton<ISmsService>(sp =>
        {
            var accountSid = configuration["Twilio:AccountSid"]
                ?? throw new InvalidOperationException("Twilio:AccountSid is not configured");
            var authToken = configuration["Twilio:AuthToken"]
                ?? throw new InvalidOperationException("Twilio:AuthToken is not configured");
            var fromNumber = configuration["Twilio:FromNumber"]
                ?? throw new InvalidOperationException("Twilio:FromNumber is not configured");
            return new TwilioSmsService(accountSid, authToken, fromNumber);
        });

        // Services
        services.AddScoped<InvoiceService>();

        return services;
    }

    public static async Task InitializeCosmosDbAsync(this IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "InvoiceDb";
        var containerName = configuration["CosmosDb:ContainerName"] ?? "Invoices";

        var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
        await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");
    }
}

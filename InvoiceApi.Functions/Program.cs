using InvoiceApi.Functions.Middleware;
using InvoiceApi.Infrastructure.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register middleware
builder.UseMiddleware<ApiKeyAuthMiddleware>();

// Add infrastructure services (CosmosDB, Stripe, SendGrid, Twilio)
builder.Services.AddInvoiceApiInfrastructure(builder.Configuration);

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var app = builder.Build();

// Initialize CosmosDB database and container
await app.Services.InitializeCosmosDbAsync(builder.Configuration);

app.Run();

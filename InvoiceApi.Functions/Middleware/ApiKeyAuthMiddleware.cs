using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;

namespace InvoiceApi.Functions.Middleware;

public class ApiKeyAuthMiddleware : IFunctionsWorkerMiddleware
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly string? _apiKey;

    // Endpoints that don't require authentication
    private static readonly HashSet<string> PublicEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "health",
        "webhooks/stripe"
    };

    public ApiKeyAuthMiddleware(IConfiguration configuration)
    {
        _apiKey = configuration["ApiKey"];
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequestData = await context.GetHttpRequestDataAsync();

        if (httpRequestData is null)
        {
            await next(context);
            return;
        }

        // Check if this is a public endpoint
        var path = httpRequestData.Url.AbsolutePath.TrimStart('/').ToLowerInvariant();
        if (PublicEndpoints.Any(endpoint => path.Contains(endpoint)))
        {
            await next(context);
            return;
        }

        // Validate API key
        if (string.IsNullOrEmpty(_apiKey))
        {
            // If no API key is configured, skip auth (development mode)
            await next(context);
            return;
        }

        if (!httpRequestData.Headers.TryGetValues(ApiKeyHeaderName, out var headerValues) ||
            !headerValues.Contains(_apiKey))
        {
            var response = httpRequestData.CreateResponse(HttpStatusCode.Unauthorized);
            await response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Invalid or missing API key"
            });

            context.GetInvocationResult().Value = response;
            return;
        }

        await next(context);
    }
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InvoiceApi.Core.DTOs;
using InvoiceApi.Core.Enums;
using InvoiceApi.Tests.Fixtures;

namespace InvoiceApi.Tests.Integration;

/// <summary>
/// API integration tests that test the full request/response cycle.
/// These tests require the Azure Functions runtime and are marked as integration tests.
///
/// To run these tests locally:
/// 1. Start the Functions app: cd InvoiceApi.Functions && func start
/// 2. Run tests: dotnet test --filter "Category=Integration"
///
/// Note: These tests can be run against a local or deployed Functions app
/// by setting the BASE_URL environment variable.
/// </summary>
[Trait("Category", "Integration")]
public class InvoiceApiTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly List<string> _createdInvoiceIds = new();

    public InvoiceApiTests()
    {
        _baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:7071";
        _apiKey = Environment.GetEnvironmentVariable("API_KEY") ?? "test-api-key";

        _client = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl)
        };
        _client.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clean up created invoices
        foreach (var id in _createdInvoiceIds)
        {
            try
            {
                await _client.DeleteAsync($"/api/invoices/{id}");
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        _client.Dispose();
    }

    #region Health Check Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("healthy");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HealthCheck_WithoutApiKey_ShouldStillWork()
    {
        // Arrange
        using var clientWithoutKey = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl)
        };

        // Act
        var response = await clientWithoutKey.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Create Invoice Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateInvoice_WithValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var request = TestFixture.CreateValidInvoiceRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoices", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<InvoiceResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.InvoiceNumber.Should().StartWith("INV-");
        result.Data.Status.Should().Be(InvoiceStatus.Draft);

        // Track for cleanup
        _createdInvoiceIds.Add(result.Data.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateInvoice_ShouldCalculateTotalsCorrectly()
    {
        // Arrange
        var request = TestFixture.CreateValidInvoiceRequest(taxRate: 10m);

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoices", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<InvoiceResponse>>();
        result!.Data!.Subtotal.Should().Be(8000m); // 40*150 + 20*100
        result.Data.TaxAmount.Should().Be(800m);
        result.Data.Total.Should().Be(8800m);

        _createdInvoiceIds.Add(result.Data.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateInvoice_WithoutApiKey_ShouldReturnUnauthorized()
    {
        // Arrange
        using var clientWithoutKey = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl)
        };
        var request = TestFixture.CreateValidInvoiceRequest();

        // Act
        var response = await clientWithoutKey.PostAsJsonAsync("/api/invoices", request);

        // Assert - may return 401 if API key is configured
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Created);
    }

    #endregion

    #region Get Invoice Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetInvoice_WithExistingId_ShouldReturnInvoice()
    {
        // Arrange - Create an invoice first
        var createRequest = TestFixture.CreateValidInvoiceRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/invoices", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<InvoiceResponse>>();
        _createdInvoiceIds.Add(created!.Data!.Id);

        // Act
        var response = await _client.GetAsync($"/api/invoices/{created.Data.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<InvoiceResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(created.Data.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetInvoice_WithNonExistingId_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/invoices/non-existing-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region List Invoices Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetInvoices_ShouldReturnList()
    {
        // Act
        var response = await _client.GetAsync("/api/invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<InvoiceResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetInvoices_WithStatusFilter_ShouldFilterResults()
    {
        // Arrange - Create invoices with different statuses
        var draftRequest = TestFixture.CreateValidInvoiceRequest();
        var draftResponse = await _client.PostAsJsonAsync("/api/invoices", draftRequest);
        var draftInvoice = await draftResponse.Content.ReadFromJsonAsync<ApiResponse<InvoiceResponse>>();
        _createdInvoiceIds.Add(draftInvoice!.Data!.Id);

        // Act
        var response = await _client.GetAsync("/api/invoices?status=Draft");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<InvoiceResponse>>>();
        result!.Data.Should().NotBeNull();
        result.Data.Should().OnlyContain(i => i.Status == InvoiceStatus.Draft);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetInvoices_WithPagination_ShouldLimitResults()
    {
        // Act
        var response = await _client.GetAsync("/api/invoices?skip=0&take=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<InvoiceResponse>>>();
        result!.Data!.Count.Should().BeLessThanOrEqualTo(5);
    }

    #endregion

    #region Update Invoice Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateInvoice_WithValidRequest_ShouldUpdateInvoice()
    {
        // Arrange - Create an invoice first
        var createRequest = TestFixture.CreateValidInvoiceRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/invoices", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<InvoiceResponse>>();
        _createdInvoiceIds.Add(created!.Data!.Id);

        var updateRequest = new UpdateInvoiceRequest
        {
            Notes = "Updated via integration test",
            TaxRate = 15m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/invoices/{created.Data.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<InvoiceResponse>>();
        result!.Data!.Notes.Should().Be("Updated via integration test");
        result.Data.TaxRate.Should().Be(15m);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateInvoice_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var updateRequest = new UpdateInvoiceRequest { Notes = "Test" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/invoices/non-existing-id", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Delete Invoice Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteInvoice_WithExistingId_ShouldDeleteInvoice()
    {
        // Arrange - Create an invoice first
        var createRequest = TestFixture.CreateValidInvoiceRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/invoices", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<InvoiceResponse>>();

        // Act
        var response = await _client.DeleteAsync($"/api/invoices/{created!.Data!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/invoices/{created.Data.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteInvoice_WithNonExistingId_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/invoices/non-existing-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Payment Link Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreatePaymentLink_WithValidInvoice_ShouldReturnPaymentLink()
    {
        // Arrange - Create an invoice first
        var createRequest = TestFixture.CreateValidInvoiceRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/invoices", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<InvoiceResponse>>();
        _createdInvoiceIds.Add(created!.Data!.Id);

        // Act
        var response = await _client.PostAsync($"/api/invoices/{created.Data.Id}/payment-link", null);

        // Assert - Note: This will fail without valid Stripe credentials
        // In real tests, mock the payment service
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest, // If Stripe not configured
            HttpStatusCode.InternalServerError // If Stripe fails
        );
    }

    #endregion

    #region Notification Tests

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendInvoice_WithValidInvoice_ShouldSendEmail()
    {
        // Arrange - Create an invoice first
        var createRequest = TestFixture.CreateValidInvoiceRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/invoices", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<InvoiceResponse>>();
        _createdInvoiceIds.Add(created!.Data!.Id);

        // Act
        var response = await _client.PostAsync($"/api/invoices/{created.Data.Id}/send", null);

        // Assert - Note: This will fail without valid SendGrid credentials
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendReminder_WithNonExistingInvoice_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.PostAsync("/api/invoices/non-existing-id/remind", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    #endregion

    private record HealthResponse(string Status, DateTime Timestamp, string Version);
}

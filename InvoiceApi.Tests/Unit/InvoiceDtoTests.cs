using FluentAssertions;
using InvoiceApi.Core.DTOs;
using InvoiceApi.Core.Entities;
using InvoiceApi.Core.Enums;

namespace InvoiceApi.Tests.Unit;

public class InvoiceDtoTests
{
    [Fact]
    public void InvoiceResponse_FromInvoice_ShouldMapAllProperties()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = "test-id",
            InvoiceNumber = "INV-001",
            Client = new Client
            {
                Name = "John Doe",
                Email = "john@example.com",
                Phone = "+1234567890",
                Company = "Acme Inc",
                Address = new Address
                {
                    Street = "123 Main St",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10001",
                    Country = "USA"
                }
            },
            LineItems = new List<LineItem>
            {
                new() { Description = "Service", Quantity = 1, UnitPrice = 100m }
            },
            TaxRate = 10m,
            Status = InvoiceStatus.Sent,
            IssueDate = new DateTime(2024, 1, 1),
            DueDate = new DateTime(2024, 2, 1),
            StripePaymentLinkUrl = "https://stripe.com/pay/123",
            CreatedAt = new DateTime(2024, 1, 1),
            PaidAt = null,
            Notes = "Test notes"
        };

        // Act
        var response = InvoiceResponse.FromInvoice(invoice);

        // Assert
        response.Id.Should().Be("test-id");
        response.InvoiceNumber.Should().Be("INV-001");
        response.Client.Name.Should().Be("John Doe");
        response.Client.Email.Should().Be("john@example.com");
        response.Client.Phone.Should().Be("+1234567890");
        response.Client.Company.Should().Be("Acme Inc");
        response.Client.Address.Should().NotBeNull();
        response.Client.Address!.City.Should().Be("New York");
        response.LineItems.Should().HaveCount(1);
        response.Subtotal.Should().Be(100m);
        response.TaxRate.Should().Be(10m);
        response.TaxAmount.Should().Be(10m);
        response.Total.Should().Be(110m);
        response.Status.Should().Be(InvoiceStatus.Sent);
        response.PaymentLinkUrl.Should().Be("https://stripe.com/pay/123");
        response.Notes.Should().Be("Test notes");
    }

    [Fact]
    public void InvoiceResponse_FromInvoice_WithNullAddress_ShouldHandleGracefully()
    {
        // Arrange
        var invoice = new Invoice
        {
            Client = new Client
            {
                Name = "John",
                Email = "john@example.com",
                Address = null
            },
            LineItems = new List<LineItem>()
        };

        // Act
        var response = InvoiceResponse.FromInvoice(invoice);

        // Assert
        response.Client.Address.Should().BeNull();
    }

    [Fact]
    public void ApiResponse_Ok_ShouldSetSuccessTrue()
    {
        // Act
        var response = ApiResponse<string>.Ok("data", "Success message");

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().Be("data");
        response.Message.Should().Be("Success message");
        response.Errors.Should().BeNull();
    }

    [Fact]
    public void ApiResponse_Fail_ShouldSetSuccessFalse()
    {
        // Act
        var errors = new List<string> { "Error 1", "Error 2" };
        var response = ApiResponse<string>.Fail("Failed", errors);

        // Assert
        response.Success.Should().BeFalse();
        response.Data.Should().BeNull();
        response.Message.Should().Be("Failed");
        response.Errors.Should().BeEquivalentTo(errors);
    }
}

using FluentAssertions;
using InvoiceApi.Core.Entities;
using InvoiceApi.Core.Enums;

namespace InvoiceApi.Tests.Unit;

public class InvoiceEntityTests
{
    [Fact]
    public void Invoice_Subtotal_ShouldCalculateFromLineItems()
    {
        // Arrange
        var invoice = new Invoice
        {
            LineItems = new List<LineItem>
            {
                new() { Description = "Item 1", Quantity = 2, UnitPrice = 100m },
                new() { Description = "Item 2", Quantity = 3, UnitPrice = 50m }
            }
        };

        // Act & Assert
        invoice.Subtotal.Should().Be(350m); // 2*100 + 3*50
    }

    [Fact]
    public void Invoice_TaxAmount_ShouldCalculateFromSubtotalAndRate()
    {
        // Arrange
        var invoice = new Invoice
        {
            LineItems = new List<LineItem>
            {
                new() { Description = "Item", Quantity = 1, UnitPrice = 1000m }
            },
            TaxRate = 10m
        };

        // Act & Assert
        invoice.TaxAmount.Should().Be(100m); // 10% of 1000
    }

    [Fact]
    public void Invoice_Total_ShouldBeSubtotalPlusTax()
    {
        // Arrange
        var invoice = new Invoice
        {
            LineItems = new List<LineItem>
            {
                new() { Description = "Item", Quantity = 1, UnitPrice = 1000m }
            },
            TaxRate = 8.5m
        };

        // Act & Assert
        invoice.Subtotal.Should().Be(1000m);
        invoice.TaxAmount.Should().Be(85m);
        invoice.Total.Should().Be(1085m);
    }

    [Fact]
    public void Invoice_WithNoLineItems_ShouldHaveZeroTotals()
    {
        // Arrange
        var invoice = new Invoice
        {
            LineItems = new List<LineItem>(),
            TaxRate = 10m
        };

        // Act & Assert
        invoice.Subtotal.Should().Be(0m);
        invoice.TaxAmount.Should().Be(0m);
        invoice.Total.Should().Be(0m);
    }

    [Fact]
    public void Invoice_DefaultStatus_ShouldBeDraft()
    {
        // Arrange & Act
        var invoice = new Invoice();

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public void Invoice_DefaultId_ShouldBeGuid()
    {
        // Arrange & Act
        var invoice = new Invoice();

        // Assert
        invoice.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(invoice.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void LineItem_Total_ShouldBeQuantityTimesUnitPrice()
    {
        // Arrange
        var lineItem = new LineItem
        {
            Description = "Test",
            Quantity = 5,
            UnitPrice = 25.50m
        };

        // Act & Assert
        lineItem.Total.Should().Be(127.50m);
    }
}
